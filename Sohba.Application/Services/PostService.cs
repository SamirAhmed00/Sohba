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

            _unitOfWork.Posts.Add(post);

            // Extract hashtags and associate with post (if any)
            var hashtags = ExtractHashtags(post.Content);
            if (hashtags.Any())
            {
                await _unitOfWork.Posts.AddHashtagsToPostAsync(post.Id, hashtags);
            }
            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<PostResponseDto>(post);
            return Result<PostResponseDto>.Success(response);
        }

        // Sohba.Application.Services/PostService.cs
        public async Task<Result<IEnumerable<PostResponseDto>>> GetFeedAsync(Guid userId)
        {
            var posts = await _unitOfWork.Posts.GetTimelineAsync(userId);
            var postList = posts.ToList();

            if (!postList.Any())
                return Result<IEnumerable<PostResponseDto>>.Success(new List<PostResponseDto>());

            var ids = postList.Select(p => p.Id).ToList();

            var counts = await _unitOfWork.Posts.GetPostsCountsAsync(ids);
            var userReactions = await _unitOfWork.Interactions.GetUserReactionsForPostsAsync(userId, ids);
            var userSavedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId);

            
            var userReports = new List<PostReport>();
            foreach (var postId in ids)
            {
                var hasReported = await _unitOfWork.Reports.HasUserReportedEntityAsync(userId, postId);
                if (hasReported)
                    userReports.Add(new PostReport { PostId = postId });
            }
            var reportedPostIds = new HashSet<Guid>(userReports.Select(r => r.PostId));

            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            var savedDict = userSavedPosts.ToDictionary(s => s.PostId, s => s.Tag);

            var response = postList.Select(p =>
            {
                counts.TryGetValue(p.Id, out var countData);
                var dto = new PostResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    ImageUrl = p.ImageUrl,
                    CreatedAt = p.CreatedAt,
                    AuthorName = p.User?.Name,
                    CommentsCount = countData.comments,
                    ReactionsCount = countData.reactions,
                    IsSaved = savedDict.ContainsKey(p.Id),
                    IsFavorite = savedDict.TryGetValue(p.Id, out var tag) && tag == SavedTag.Favorite,
                    IsReportedByCurrentUser = reportedPostIds.Contains(p.Id)
                };

                if (reactionDict.TryGetValue(p.Id, out var reaction))
                    dto.CurrentUserReaction = reaction;

                return dto;
            }).ToList();

            return Result<IEnumerable<PostResponseDto>>.Success(response);
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

        // Helper Method
        private IEnumerable<string> ExtractHashtags(string content)
        {
            if (string.IsNullOrEmpty(content)) return new List<string>();

            // Regex to find words starting with #
            var regex = new Regex(@"#\w+");
            var matches = regex.Matches(content);

            return matches.Select(m => m.Value.TrimStart('#')).ToList();
        }
    }
}
