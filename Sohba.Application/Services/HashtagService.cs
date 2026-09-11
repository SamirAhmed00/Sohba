using AutoMapper;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class HashtagService : IHashtagService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IInteractionService _interactionService;
        private readonly IMapper _mapper;
        private readonly IPostService _postService;

        public HashtagService(IUnitOfWork unitOfWork, IInteractionService interactionService, IMapper mapper, IPostService postService)
        {
            _unitOfWork = unitOfWork;
            _interactionService = interactionService;
            _mapper = mapper;
            _postService = postService;
        }

        public async Task<Result<IEnumerable<HashtagDto>>> GetTrendingHashtagsAsync(int count = 10)
        {
            var hashtags = await _unitOfWork.Hashtags.GetTrendingHashtagsAsync(count);
            var dtos = _mapper.Map<IEnumerable<HashtagDto>>(hashtags);
            return Result<IEnumerable<HashtagDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<PostResponseDto>>> GetPostsByHashtagAsync(string tag, Guid currentUserId)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return Result<IEnumerable<PostResponseDto>>.Failure("Tag is required");

            var cleanTag = tag.Trim().TrimStart('#').ToLowerInvariant();
            var posts = await _unitOfWork.Posts.GetPostsByHashtagAsync(cleanTag, currentUserId);

            var result = await _postService.MapPostsWithInteractions(posts, currentUserId);
            return result;
        }

        public async Task<Result<PagedResult<HashtagDto>>> GetTrendingHashtagsPagedAsync(int page = 1, int pageSize = 5)
        {
            var (items, totalCount) = await _unitOfWork.Hashtags.GetTrendingHashtagsPagedAsync(page, pageSize);
            var dtos = _mapper.Map<IEnumerable<HashtagDto>>(items);

            return Result<PagedResult<HashtagDto>>.Success(new PagedResult<HashtagDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

    }

}
