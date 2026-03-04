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

        public HashtagService(IUnitOfWork unitOfWork, IInteractionService interactionService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _interactionService = interactionService;
            _mapper = mapper;
        }

        public async Task<Result<IEnumerable<HashtagDto>>> GetTrendingHashtagsAsync(int count = 10)
        {
            var hashtags = await _unitOfWork.Hashtags.GetTrendingHashtagsAsync(count);
            var dtos = _mapper.Map<IEnumerable<HashtagDto>>(hashtags);
            return Result<IEnumerable<HashtagDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<PostResponseDto>>> GetPostsByHashtagAsync(string tag, Guid currentUserId)
        {
            // TODO: Implement getting posts by hashtag
            // This will need a method in PostRepository to get posts by hashtag
            return Result<IEnumerable<PostResponseDto>>.Success(new List<PostResponseDto>());
        }

    }

}
