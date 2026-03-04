using AutoMapper;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Sohba.Application.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPostDomainService _postDomainService;

        public PostService(IUnitOfWork unitOfWork, IMapper mapper, IPostDomainService postDomainService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _postDomainService = postDomainService;
        }

        public async Task<Result<PostResponseDto>> CreatePostAsync(PostCreateDto postDto, Guid userId)
        {

            var validation = _postDomainService.CanCreatePost(userId, postDto.Content, !string.IsNullOrEmpty(postDto.ImageUrl));
            if (!validation.IsSuccess)
                return Result<PostResponseDto>.Failure(validation.Error);

            var post = _mapper.Map<Post>(postDto);
            post.UserId = userId;
            post.CreatedAt = DateTime.UtcNow;

            // Extract hashtags from content and combine with provided hashtags, ensuring uniqueness
            var extractedTags = ExtractHashtags(postDto.Content).ToList();

            _unitOfWork.Posts.Add(post);
            await _unitOfWork.CompleteAsync();

            // Extract hashtags and associate with post (if any)
            if (extractedTags.Any())
            {
                // TODO: مستقبلاً نجيب اللوكيشن من الـ User Profile
                // var user = await _unitOfWork.Users.GetByIdAsync(userId);
                // string userLocation = user.Country ?? "Global";

                string userLocation = "Egypt"; // قيمة افتراضية حالياً
                await _unitOfWork.Posts.AddHashtagsToPostAsync(post.Id, extractedTags, userLocation);
                await _unitOfWork.CompleteAsync();
            }
            return Result<PostResponseDto>.Success(_mapper.Map<PostResponseDto>(post));
        }


        public async Task<Result<IEnumerable<PostResponseDto>>> GetFeedAsync(Guid userId)
        {
            var posts = await _unitOfWork.Posts.GetTimelineAsync(userId);
            return await MapPostsWithInteractions(posts, userId);
        }

        
        public async Task<Result<PostResponseDto>> GetPostByIdAsync(Guid postId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);

            if (post == null || post.IsDeleted)
                return Result<PostResponseDto>.Failure("Post not found or has been deleted.");

            var response = _mapper.Map<PostResponseDto>(post);
            return Result<PostResponseDto>.Success(response);
        }

        public async Task<Result> UpdatePostAsync(Guid postId, PostCreateDto postDto, Guid userId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeleted)
                return Result.Failure("Post not found.");

            // 1. Delegate permission check to Domain Service
            var canUpdate = _postDomainService.CanUpdatePost(userId, post.UserId, post.IsDeleted);
            if (!canUpdate.IsSuccess)
                return canUpdate;

            // 2. Map updated values
            _mapper.Map(postDto, post);
            post.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Posts.Update(post);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> DeletePostAsync(Guid postId, Guid userId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null)
                return Result.Failure("Post not found.");

            // 1. Check permission via Domain Service (Admin: false for now)
            var result = _postDomainService.CanDeletePost(userId, postId, post.UserId, isAdmin: false);
            if (!result.IsSuccess)
                return result;

            // 2. Apply Soft Delete
            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Posts.Update(post);
            await _unitOfWork.CompleteAsync();

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
            return await MapPostsWithInteractions(posts, Guid.Empty);
        }





        // Helper Method
        private async Task<Result<IEnumerable<PostResponseDto>>> MapPostsWithInteractions(IEnumerable<Post> posts, Guid currentUserId)
        {
            var postList = posts.ToList();
            if (!postList.Any())
                return Result<IEnumerable<PostResponseDto>>.Success(new List<PostResponseDto>());

            var ids = postList.Select(p => p.Id).ToList();
            var counts = await _unitOfWork.Posts.GetPostsCountsAsync(ids);
            var userReactions = await _unitOfWork.Interactions.GetUserReactionsForPostsAsync(currentUserId, ids);
            var userSavedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(currentUserId);

            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            var savedDict = userSavedPosts.ToDictionary(s => s.PostId, s => s.Tag);

            var response = postList.Select(p =>
            {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;
                dto.IsSaved = savedDict.ContainsKey(p.Id);
                dto.IsFavorite = savedDict.TryGetValue(p.Id, out var tag) && tag == SavedTag.Favorite;

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
    }
}
