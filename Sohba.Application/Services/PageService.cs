using AutoMapper;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class PageService : IPageService
    {
        private readonly IPageRepository _pageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PageService(
            IPageRepository pageRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _pageRepository = pageRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PageResponseDto>> CreatePageAsync(Guid adminId, PageCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return Result<PageResponseDto>.Failure("Invalid page data");

            var page = new Page
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _pageRepository.Add(page);
            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<PageResponseDto>(page);

            return Result<PageResponseDto>.Success(response);
        }

        public async Task<Result> FollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null)
                return Result.Failure("Page not found");

            var alreadyFollowing =
                (await _pageRepository.GetPagesByFollowerIdAsync(userId))
                .Any(p => p.Id == pageId);

            if (alreadyFollowing)
                return Result.Failure("Already following this page");

            var follower = new PageFollower
            {
                UserId = userId,
                PageId = pageId,
                FollowedAt = DateTime.UtcNow
            };

            _pageRepository.AddFollower(follower);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> UnfollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null)
                return Result.Failure("Page not found");

            _pageRepository.RemoveFollower(userId, pageId);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result<PageResponseDto>> GetPageByIdAsync(Guid pageId)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null)
                return Result<PageResponseDto>.Failure("Page not found");

            var dto = _mapper.Map<PageResponseDto>(page);

            return Result<PageResponseDto>.Success(dto);
        }

        public async Task<Result<IEnumerable<PageResponseDto>>> GetUserFollowedPagesAsync(Guid userId)
        {
            var pages = await _pageRepository.GetPagesByFollowerIdAsync(userId);

            var dtos = _mapper.Map<IEnumerable<PageResponseDto>>(pages);

            return Result<IEnumerable<PageResponseDto>>.Success(dtos);
        }
    }

}
