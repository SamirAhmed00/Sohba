using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _memoryCache;

        public HashtagService(IUnitOfWork unitOfWork, IInteractionService interactionService, IMapper mapper, IPostService postService, IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _interactionService = interactionService;
            _mapper = mapper;
            _postService = postService;
            _memoryCache = memoryCache;
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
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var cacheKey = $"trending-hashtags:{page}:{pageSize}";

            if (_memoryCache.TryGetValue(cacheKey, out var cached) &&
                cached is PagedResult<HashtagDto> cachedResult)
            {
                return Result<PagedResult<HashtagDto>>.Success(cachedResult);
            }

            var (items, totalCount) =
                await _unitOfWork.Hashtags.GetTrendingHashtagsPagedAsync(page, pageSize);

            var dtos = _mapper.Map<IEnumerable<HashtagDto>>(items);

            var pagedResult = new PagedResult<HashtagDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            var entry = _memoryCache.CreateEntry(cacheKey);
            entry.SetValue(pagedResult);
            entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

            return Result<PagedResult<HashtagDto>>.Success(pagedResult);
        }

    }

}
