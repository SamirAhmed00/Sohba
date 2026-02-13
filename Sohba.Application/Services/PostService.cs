using AutoMapper;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Interfaces;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Text;

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

        public async Task<Result<IEnumerable<PostResponseDto>>> GetFeedAsync(Guid userId)
        {
            var posts = await _unitOfWork.Posts.GetTimelineAsync(userId);
            var response = _mapper.Map<IEnumerable<PostResponseDto>>(posts);

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
