using AutoMapper;
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

            var posts = await _unitOfWork.Posts.GetPostsByHashtagAsync(tag);

            var result = await _postService.MapPostsWithInteractions(posts, currentUserId); 
            return result;
        }

    }

}
