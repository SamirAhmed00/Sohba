using AutoMapper;
using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities;
using Sohba.Domain.Entities.StoryAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class StoryService : IStoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStoryDomainService _storyDomainService;
        private readonly INotificationService _notificationService;

        public StoryService(IUnitOfWork unitOfWork, IMapper mapper, IStoryDomainService storyDomainService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _storyDomainService = storyDomainService;
            _notificationService = notificationService;
        }

        public async Task<Result<StoryResponseDto>> CreateStoryAsync(StoryCreateDto storyDto, Guid userId)
        {
            var activeStories = await _unitOfWork.Stories.GetActiveStoriesAsync(userId);
            int currentStoryCount = activeStories.Count();

            var validation = _storyDomainService.CanCreateStory(
                userId,
                storyDto.MediaFile != null || !string.IsNullOrEmpty(storyDto.MediaUrl),
                10,
                currentStoryCount);

            if (!validation.IsSuccess)
                return Result<StoryResponseDto>.Failure(validation.Error);

            // MediaUrl is resolved by the controller via IFileStorageService before this call.
            // StoryService must NOT perform any file I/O (Application layer cannot touch Infrastructure).
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            var story = new Story
            {
                UserId = userId,
                Content = storyDto.Content,
                MediaUrl = storyDto.MediaUrl,
                MediaType = storyDto.MediaType ?? (storyDto.MediaUrl != null ? "image" : null),
                CreatedAt = now,
                ExpiresAt = now.AddHours(24),
                IsDeleted = false,
                Privacy = storyDto.Privacy == "FriendsOnly" ? StoryPrivacy.FriendsOnly : StoryPrivacy.Public
            };

            _unitOfWork.Stories.Add(story);
            await _unitOfWork.CompleteAsync();

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var response = new StoryResponseDto
            {
                Id = story.Id,
                Content = story.Content,
                MediaUrl = story.MediaUrl,
                MediaType = story.MediaType,
                UserName = user.Name,
                UserProfilePicture = user.ProfilePictureUrl,
                CreatedAt = story.CreatedAt,
                ExpiresAt = story.ExpiresAt,
                ViewersCount = 0,
                HasUserViewed = false,
                Privacy = story.Privacy.ToString()
            };

            return Result<StoryResponseDto>.Success(response);
        }

        public async Task<Result<IEnumerable<StoryResponseDto>>> GetStoriesForFeedAsync(Guid userId)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            var friendIds = await _unitOfWork.Stories.GetFriendIdsAsync(userId);
            var friendIdsList = friendIds.ToList();

            var stories = await _unitOfWork.Stories.GetStoriesForFeedAsync(userId);

            //  PRIVACY CHECK: Enhanced filtering
            var filteredStories = stories.Where(s =>
                s.CreatedAt >= cutoffTime &&
                !s.IsDeleted &&
                (
                    // Owner always sees their own stories
                    s.UserId == userId ||

                    // Public stories from friends
                    (s.Privacy == StoryPrivacy.Public && friendIdsList.Contains(s.UserId)) ||

                    // Friends-only stories from friends
                    (s.Privacy == StoryPrivacy.FriendsOnly && friendIdsList.Contains(s.UserId))
                ))
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            var groupedStories = filteredStories
                .GroupBy(s => s.UserId)
                .Select(g => g.OrderBy(s => s.CreatedAt).ToList())
                .ToList();

            var result = new List<StoryResponseDto>();

            foreach (var userStories in groupedStories)
            {
                foreach (var story in userStories)
                {
                    var viewersCount = await _unitOfWork.Stories.GetViewersCountAsync(story.Id);
                    var hasViewed = await _unitOfWork.Stories.HasUserViewedStoryAsync(story.Id, userId);

                    result.Add(new StoryResponseDto
                    {
                        Id = story.Id,
                        UserId = story.UserId,
                        Content = story.Content,
                        MediaUrl = story.MediaUrl,
                        MediaType = story.MediaType,
                        UserName = story.User?.Name,
                        UserProfilePicture = story.User?.ProfilePictureUrl,
                        CreatedAt = story.CreatedAt,
                        ExpiresAt = story.ExpiresAt,
                        ViewersCount = viewersCount,
                        HasUserViewed = hasViewed,
                        Privacy = story.Privacy.ToString()
                    });
                }
            }

            return Result<IEnumerable<StoryResponseDto>>.Success(result);
        }

        public async Task<Result<StoryResponseDto>> GetStoryByIdAsync(Guid storyId, Guid currentUserId)
        {
            var story = await _unitOfWork.Stories.GetByIdAsync(storyId);

            if (story == null || story.IsDeleted || story.ExpiresAt < DateTime.UtcNow)
                return Result<StoryResponseDto>.Failure("Story not found or expired.");


            // PRIVACY CHECK: Check if user is friends with story creator
            var isFriend = await _unitOfWork.Friendships.AreFriendsAsync(currentUserId, story.UserId);

            var owner = await _unitOfWork.Users.GetByIdAsync(story.UserId);
            var isOwnerAccountPrivate = owner?.IsPrivateAccount ?? false;

            var canView = _storyDomainService.CanViewStory(
                    currentUserId, story.UserId, story.Privacy, isOwnerAccountPrivate, isFriend, story.CreatedAt);


            if (!canView.IsSuccess)
                return Result<StoryResponseDto>.Failure(canView.Error);


            var viewersCount = await _unitOfWork.Stories.GetViewersCountAsync(storyId);
            var hasViewed = await _unitOfWork.Stories.HasUserViewedStoryAsync(storyId, currentUserId);

            var response = new StoryResponseDto
            {
                Id = story.Id,
                Content = story.Content,
                MediaUrl = story.MediaUrl,
                MediaType = story.MediaType,
                UserName = story.User?.Name,
                UserProfilePicture = story.User?.ProfilePictureUrl,
                CreatedAt = story.CreatedAt,
                ExpiresAt = story.ExpiresAt,
                ViewersCount = viewersCount,
                HasUserViewed = hasViewed,
                Privacy = story.Privacy.ToString()
            };

            return Result<StoryResponseDto>.Success(response);
        }

        public async Task<Result> MarkStoryAsViewedAsync(Guid storyId, Guid userId)
        {
            var story = await _unitOfWork.Stories.GetByIdAsync(storyId);

            if (story == null || story.IsDeleted || story.ExpiresAt < DateTime.UtcNow)
                return Result.Failure("Story not found or expired.");

            if (story.UserId == userId)
            {
                await _notificationService.MarkNotificationsByTargetAsReadAsync(userId, storyId);
                return Result.Success();
            }

            var alreadyViewed = await _unitOfWork.Stories.HasUserViewedStoryAsync(storyId, userId);
            if (!alreadyViewed)
            {
                await _unitOfWork.Stories.AddViewerAsync(storyId, userId);
                await _unitOfWork.CompleteAsync();
            }

            return Result.Success();
        }

        public async Task<Result> DeleteStoryAsync(Guid storyId, Guid userId)
        {
            var story = await _unitOfWork.Stories.GetByIdAsync(storyId);

            if (story == null)
                return Result.Failure("Story not found.");

            if (story.UserId != userId)
                return Result.Failure("You are not authorized to delete this story.");

            story.IsDeleted = true;
            _unitOfWork.Stories.Update(story);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result<IEnumerable<StoryResponseDto>>> GetUserStoriesAsync(Guid userId, Guid currentUserId)
        {
            var stories = await _unitOfWork.Stories.GetUserStoriesAsync(userId, currentUserId);

            var result = new List<StoryResponseDto>();
            foreach (var story in stories)
            {
                var viewersCount = await _unitOfWork.Stories.GetViewersCountAsync(story.Id);
                var hasViewed = await _unitOfWork.Stories.HasUserViewedStoryAsync(story.Id, currentUserId);

                var reactionsCount = await _unitOfWork.Stories.GetReactionCountAsync(story.Id);
                var userReaction = await _unitOfWork.Stories.GetReactionAsync(story.Id, currentUserId);


                result.Add(new StoryResponseDto
                {
                    Id = story.Id,
                    UserId = story.UserId,
                    Content = story.Content,
                    MediaUrl = story.MediaUrl,
                    MediaType = story.MediaType,
                    UserName = story.User?.Name,
                    UserProfilePicture = story.User?.ProfilePictureUrl,
                    CreatedAt = story.CreatedAt,
                    ExpiresAt = story.ExpiresAt,
                    ViewersCount = viewersCount,
                    HasUserViewed = hasViewed,
                    Privacy = story.Privacy.ToString(),
                    ReactionsCount = reactionsCount,
                    CurrentUserReacted = userReaction != null
                });
            }

            return Result<IEnumerable<StoryResponseDto>>.Success(result);
        }


        public async Task<Result<(bool Added, int NewCount)>> ToggleStoryReactionAsync(Guid userId, Guid storyId, ReactionType type)
        {
            var story = await _unitOfWork.Stories.GetByIdAsync(storyId);
            if (story == null || story.IsDeleted || story.ExpiresAt < DateTime.UtcNow)
                return Result<(bool, int)>.Failure("Story not found or expired.");

            // Reuse the same visibility check as viewing — you cannot react to a story you
            // are not authorized to see.
            var isFriend = await _unitOfWork.Friendships.AreFriendsAsync(userId, story.UserId);
            var owner = await _unitOfWork.Users.GetByIdAsync(story.UserId);
            var canView = _storyDomainService.CanViewStory(
                userId, story.UserId, story.Privacy, owner?.IsPrivateAccount ?? false, isFriend, story.CreatedAt);
            if (!canView.IsSuccess)
                return Result<(bool, int)>.Failure(canView.Error);

            var existing = await _unitOfWork.Stories.GetReactionAsync(storyId, userId);
            bool added;

            if (existing != null)
            {
                _unitOfWork.Stories.RemoveReaction(existing);
                added = false;
            }
            else
            {
                _unitOfWork.Stories.AddReaction(new StoryReaction
                {
                    Id = Guid.NewGuid(),
                    StoryId = storyId,
                    UserId = userId,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                });
                added = true;
            }

            await _unitOfWork.CompleteAsync();
            var newCount = await _unitOfWork.Stories.GetReactionCountAsync(storyId);

            if (added && story.UserId != userId)
            {
                var reactorName = owner != null ? (await _unitOfWork.Users.GetByIdAsync(userId))?.Name ?? "Someone" : "Someone";
                await _notificationService.CreateNotificationAsync(
                    receiverId: story.UserId,
                    message: $"{reactorName} reacted to your story",
                    type: NotificationType.StoryLike,
                    senderId: userId,
                    targetId: storyId);
            }

            return Result<(bool, int)>.Success((added, newCount));
        }

        public async Task<Result<IEnumerable<StoryViewerDto>>> GetStoryViewersAsync(Guid storyId, Guid currentUserId)
        {
            var story = await _unitOfWork.Stories.GetByIdAsync(storyId);
            if (story == null)
                return Result<IEnumerable<StoryViewerDto>>.Failure("Story not found.");

            // Backend-enforced ownership check — this is the actual authorization boundary,
            // not just a UI decision.
            if (story.UserId != currentUserId)
                return Result<IEnumerable<StoryViewerDto>>.Failure("Only the story owner can view this information.");

            var viewers = await _unitOfWork.Stories.GetViewersAsync(storyId);
            var dtos = viewers.Select(v => new StoryViewerDto
            {
                UserId = v.UserId,
                UserName = v.User?.Name ?? "Unknown",
                ProfilePictureUrl = v.User?.ProfilePictureUrl,
                ViewedAt = v.ViewedAt
            });

            return Result<IEnumerable<StoryViewerDto>>.Success(dtos);
        }
    }
}
