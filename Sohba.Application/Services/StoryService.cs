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

        public StoryService(IUnitOfWork unitOfWork, IMapper mapper, IStoryDomainService storyDomainService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _storyDomainService = storyDomainService;
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

            string mediaUrl = storyDto.MediaUrl;
            string mediaType = null;

            if (storyDto.MediaFile != null)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "stories");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(storyDto.MediaFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await storyDto.MediaFile.CopyToAsync(stream);
                }

                mediaUrl = "/uploads/stories/" + fileName;
                mediaType = "image";
            }



            var story = new Story
            {
                UserId = userId,
                Content = storyDto.Content,
                MediaUrl = mediaUrl,
                MediaType = mediaType,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
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
            var stories = await _unitOfWork.Stories.GetStoriesForFeedAsync(userId);
            var groupedStories = stories
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
            
            var canView = _storyDomainService.CanViewStory(
                currentUserId,
                story.UserId,
                false, // TODO: Check if friends
                story.CreatedAt);

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

            // منع تسجيل المشاهدة لو صاحب الstory نفسه
            if (story.UserId == userId)
                return Result.Success();

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
    }
}
