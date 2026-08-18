using AutoMapper;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Sohba.Application.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPostDomainService _postDomainService;

        private readonly INotificationService _notificationService; 
        private readonly IUserService _userService;

        private readonly ILogger<PostService> _logger;

        public PostService(IUnitOfWork unitOfWork, IMapper mapper, IPostDomainService postDomainService, INotificationService notificationService, IUserService userService, ILogger<PostService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _postDomainService = postDomainService;
            _notificationService = notificationService;
            _userService = userService;
            _logger = logger;
        }



        public async Task<Result<PagedResult<PostResponseDto>>> GetFeedAsync(
         Guid userId,
         int page = 1,
         int pageSize = 10)
        {
            //  Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            //  Get paginated posts from repository
            var (posts, totalCount) = await _unitOfWork.Posts.GetTimelineAsync(userId, page, pageSize);

            //  Map posts to DTOs with interactions
            var mappedResult = await MapPostsWithInteractions(posts, userId);

            if (mappedResult.IsFailure)
                return Result<PagedResult<PostResponseDto>>.Failure(mappedResult.Error);

            // Create paged result
            var pagedResult = new PagedResult<PostResponseDto>
            {
                Items = mappedResult.Value,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Result<PagedResult<PostResponseDto>>.Success(pagedResult);
        }




        public async Task<Result<PostResponseDto>> CreatePostAsync(PostCreateDto postDto, Guid userId)
        {
            var validation = _postDomainService.CanCreatePost(userId, postDto.Content, !string.IsNullOrEmpty(postDto.ImageUrl));
            if (!validation.IsSuccess)
            {
                _logger.LogWarning("Post creation rejected for user {UserId}: {Reason}", userId, validation.Error);
                return Result<PostResponseDto>.Failure(validation.Error);
            }

            // --- Access Control for Group/Page Posts ---
            Guid? groupId = null;
            Guid? pageId = null;
            if (postDto.SourceId.HasValue)
            {
                if (postDto.SourceType == PostSourceType.Group)
                {
                    // Rule: Only active, non-banned group members can post in a group
                    var isMember = await _unitOfWork.Groups.IsMemberAsync(userId, postDto.SourceId.Value);
                    if (!isMember)
                        return Result<PostResponseDto>.Failure(
                            "Access denied: You must be an active member of this group to post in it.");
                    groupId = postDto.SourceId;
                }
                else if (postDto.SourceType == PostSourceType.Page)
                {
                    // Rule: Only the page admin can post on a page
                    var page = await _unitOfWork.Pages.GetByIdAsync(postDto.SourceId.Value);
                    if (page == null)
                        return Result<PostResponseDto>.Failure("Page not found.");

                    if (page.AdminId != userId)
                        return Result<PostResponseDto>.Failure(
                            "Access denied: Only the page administrator can post on this page.");
                    pageId = postDto.SourceId;
                }
            }
            // --- End Access Control ---

            var post = _mapper.Map<Post>(postDto);
            post.UserId = userId;
            post.CreatedAt = DateTime.UtcNow;

            if (postDto.ImageUrls != null && postDto.ImageUrls.Any())
            {
                post.ImageUrls = JsonSerializer.Serialize(postDto.ImageUrls);
                if (string.IsNullOrEmpty(post.ImageUrl))
                   post.ImageUrl = postDto.ImageUrls.First();
            }

            if (postDto.SourceId.HasValue)
            {
                post.SourceType = postDto.SourceType;
                post.SourceId = postDto.SourceId;

                if (postDto.SourceType == PostSourceType.Group)
                    post.GroupId = postDto.SourceId;
                else if (postDto.SourceType == PostSourceType.Page)
                    post.PageId = postDto.SourceId;
            }

            // Extract hashtags from content
            var extractedTags = ExtractHashtags(postDto.Content).ToList();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Posts.Add(post);
                await _unitOfWork.CompleteAsync();
                
                if (extractedTags.Any())
                {
                    string userLocation = "Egypt";
                    await _unitOfWork.Posts.AddHashtagsToPostAsync(post.Id, extractedTags, userLocation);
                    await _unitOfWork.CompleteAsync();
                }
                
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            //  Send notifications based on post type
            await SendPostNotifications(post, userId, groupId, pageId);

            _logger.LogInformation("Post created: {PostId} by user {UserId}, source type {SourceType}", post.Id, userId, postDto.SourceType);
            return Result<PostResponseDto>.Success(_mapper.Map<PostResponseDto>(post));
        }
        
        
        public async Task<Result<PostResponseDto>> GetPostByIdAsync(Guid postId, Guid currentUserId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);

            if (post == null || post.IsDeleted)
                return Result<PostResponseDto>.Failure("Post not found or has been deleted.");


            // PRIVACY CHECK: Verify user can view this post
            var isFriend = await _unitOfWork.Friendships.AreFriendsAsync(currentUserId, post.UserId);
            var canView = _postDomainService.CanViewPost(
                currentUserId,
                post.UserId,
                post.Privacy,
                isFriend
            );

            if (!canView.IsSuccess)
                return Result<PostResponseDto>.Failure(canView.Error);

            var ids = new List<Guid> { postId };
            var counts = await _unitOfWork.Posts.GetPostsCountsAsync(ids);

            var userReaction = await _unitOfWork.Interactions.GetReactionAsync(currentUserId, postId);




            var savedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(currentUserId);

            // A post is "saved" only when it is in a NON-Favorite collection.
            var isSaved = savedPosts.Any(s => s.PostId == postId && s.Tag != SavedTag.Favorite);
            var isFavorite = savedPosts.Any(s => s.PostId == postId && s.Tag == SavedTag.Favorite);

            var response = _mapper.Map<PostResponseDto>(post);
            

            if (counts.TryGetValue(postId, out var countData))
            {
                response.CommentsCount = countData.comments;
                response.ReactionsCount = countData.reactions;
            }

            response.CurrentUserReaction = userReaction?.Type.ToString();
            response.IsSaved = isSaved;
            response.IsFavorite = isFavorite;
            response.IsAuthor = post.UserId == currentUserId;

            return Result<PostResponseDto>.Success(response);
        }

        public async Task<Result> UpdatePostAsync(Guid postId, PostUpdateDto postDto, Guid userId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeleted)
                return Result.Failure("Post not found.");

            // 1. Delegate permission check to Domain Service
            var canUpdate = _postDomainService.CanUpdatePost(userId, post.UserId, post.UserId, post.IsDeleted);
            if (!canUpdate.IsSuccess)
                return canUpdate;

            // 2. Map updated values
            _mapper.Map(postDto, post);
            post.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Posts.Update(post);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> DeletePostAsync(Guid postId, Guid userId, bool isAdmin = false)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post deletion failed: post {PostId} not found", postId);
                return Result.Failure("Post not found.");
            }

            // 1. Check permission via Domain Service
            var result = _postDomainService.CanDeletePost(userId, postId, post.UserId, isAdmin);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Post deletion rejected for user {UserId} on post {PostId}: {Reason}", userId, postId, result.Error);
                return result;
            }

            // 2. Apply Soft Delete
            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Posts.Update(post);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Post {PostId} soft-deleted by user {UserId} (isAdmin={IsAdmin})", postId, userId, isAdmin);
            return Result.Success();
        }

        public async Task<Result<IEnumerable<PostResponseDto>>> GetGroupPostsAsync(Guid groupId, Guid currentUserId)
        {
            var posts = await _unitOfWork.Posts.GetGroupPostsAsync(groupId);
            return await MapPostsWithInteractions(posts, currentUserId);
        }

        public async Task<Result<IEnumerable<PostResponseDto>>> GetPagePostsAsync(Guid pageId, Guid currentUserId)
        {
            var posts = await _unitOfWork.Posts.GetPagePostsAsync(pageId);
            return await MapPostsWithInteractions(posts, currentUserId);
        }

        public async Task<Result<IEnumerable<PostResponseDto>>> GetUserPostsAsync(Guid userId, Guid currentUserId)
        {
            var posts = await _unitOfWork.Posts.GetUserPostsAsync(userId);
            return await MapPostsWithInteractions(posts, currentUserId);
        }

        public async Task<Result<IEnumerable<PostResponseDto>>> GetAllPostsAsync()
        {
            var posts = await _unitOfWork.Posts.GetAllAsync();
            var dtos = posts.Select(p => _mapper.Map<PostResponseDto>(p)).ToList();
            return Result<IEnumerable<PostResponseDto>>.Success(dtos);
        }


        public async Task<Result> HidePostAsync(Guid postId, Guid userId, bool isAdmin = false)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null)
                return Result.Failure("Post not found");

            if (!isAdmin && post.UserId != userId)
                return Result.Failure("You are not authorized to hide this post.");

            post.IsHidden = true; 
            _unitOfWork.Posts.Update(post);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }


        // Helper Method
        public async Task<Result<IEnumerable<PostResponseDto>>> MapPostsWithInteractions(IEnumerable<Post> posts, Guid currentUserId)
        {
            var postList = posts.ToList();
            if (!postList.Any())
                return Result<IEnumerable<PostResponseDto>>.Success(new List<PostResponseDto>());

            var friendIds = await _unitOfWork.Friendships.GetFriendIdsSetAsync(currentUserId);

            //  PRIVACY CHECK: Filter posts based on privacy settings
            var filteredPosts = new List<Post>();

            foreach (var post in postList)
            {
                // Owner always sees their own posts
                if (post.UserId == currentUserId)
                {
                    filteredPosts.Add(post);
                    continue;
                }

                // Check friendship status
                var isFriend = friendIds.Contains(post.UserId);

                // Apply privacy rules
                var canView = _postDomainService.CanViewPost(
                    currentUserId,
                    post.UserId,
                    post.Privacy,
                    isFriend
                );

                if (canView.IsSuccess)
                {
                    filteredPosts.Add(post);
                }
                else
                {
                    _logger.LogWarning("Privacy check: user {UserId} denied view of post {PostId} (owner {OwnerId}, isFriend {IsFriend})",
                        currentUserId, post.Id, post.UserId, isFriend);
                }
            }

            postList = filteredPosts;


            var ids = postList.Select(p => p.Id).ToList();
            var counts = await _unitOfWork.Posts.GetPostsCountsAsync(ids);
            var userReactions = await _unitOfWork.Interactions.GetUserReactionsForPostsAsync(currentUserId, ids);
            var userSavedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(currentUserId);

            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            // A post can be saved to multiple collections (e.g. a named collection AND Favorites).
            // Group by PostId and collect all tags so we don't throw on duplicate keys.
            var savedDict = userSavedPosts
                .GroupBy(s => s.PostId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Tag).ToList());

            var response = postList.Select(p =>
            {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;
                dto.IsSaved = savedDict.ContainsKey(p.Id);

                if (savedDict.TryGetValue(p.Id, out var tags))
                {
                    dto.IsFavorite = tags.Contains(SavedTag.Favorite);
                    dto.SavedTag = dto.IsFavorite ? SavedTag.Favorite.ToString() : tags.First().ToString();
                }
                dto.IsAuthor = p.UserId == currentUserId;
                if (reactionDict.TryGetValue(p.Id, out var reaction))
                    dto.CurrentUserReaction = reaction;

                return dto;
            }).ToList();

            return Result<IEnumerable<PostResponseDto>>.Success(response);
        }
        private IEnumerable<string> ExtractHashtags(string content)
        {
            if (string.IsNullOrEmpty(content)) return new List<string>();
            var regex = new Regex(@"#\w+");
            return regex.Matches(content).Select(m => m.Value.Replace("#", "").ToLower()).Distinct();
        }


        // Helper method to send notifications for post creation
        private async Task SendPostNotifications(Post post, Guid userId, Guid? groupId, Guid? pageId)
        {
            var user = await _userService.GetProfileAsync(userId);
            var userName = user.Value?.Name ?? "Someone";

            // 1. If posted in a group, notify group admin
            if (groupId.HasValue)
            {
                var group = await _unitOfWork.Groups.GetByIdAsync(groupId.Value);
                if (group != null && group.AdminId != userId)
                {
                    await _notificationService.CreateNotificationAsync(
                        receiverId: group.AdminId,
                        message: $"{userName} posted in your group '{group.Name}'",
                        type: NotificationType.SystemAlert,
                        senderId: userId,
                        targetId: post.Id
                    );
                }

                // Notify group members (optional - but we'll skip to avoid spam)
            }

            // 2. If posted on a page, notify page admin
            if (pageId.HasValue)
            {
                var page = await _unitOfWork.Pages.GetByIdAsync(pageId.Value);
                if (page != null && page.AdminId != userId)
                {
                    await _notificationService.CreateNotificationAsync(
                        receiverId: page.AdminId,
                        message: $"{userName} posted on your page '{page.Name}'",
                        type: NotificationType.SystemAlert,
                        senderId: userId,
                        targetId: post.Id
                    );
                }
            }

            // 3. If user has friends, notify them (optional - can be skipped)
            // This is a "friend activity" notification - we'll implement it later
        }

        public async Task<Result<int>> GetPostsCountAsync()
        {
            var count = await _unitOfWork.Posts.CountAsync();
            return Result<int>.Success(count);
        }


        /// <summary>
        /// Returns the most recent non-deleted posts (admin dashboard widget).
        /// For the user feed, use GetFeedAsync (paged + privacy-filtered).
        /// </summary>
        public async Task<Result<IEnumerable<PostResponseDto>>> GetRecentPostsAsync(int count)
        {
            var posts = await _unitOfWork.Posts.GetRecentAsync(count);
            var dtos = _mapper.Map<IEnumerable<PostResponseDto>>(posts);
            return Result<IEnumerable<PostResponseDto>>.Success(dtos);
        }

        
    }
}

