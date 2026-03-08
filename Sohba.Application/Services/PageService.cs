using AutoMapper;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class PageService : IPageService
    {
        private readonly IPageDomainService _pageDomainService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PageService(
            IPageDomainService pageDomainService,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _pageDomainService = pageDomainService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PageResponseDto>> CreatePageAsync(Guid adminId, PageCreateDto dto)
        {
            var domainDecision = _pageDomainService.CanCreatePage(dto?.Name);
            if (domainDecision.IsFailure)
                return Result<PageResponseDto>.Failure(domainDecision.Error);

            var page = new Page
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Pages.Add(page);

            var follower = new PageFollower
            {
                UserId = adminId,
                PageId = page.Id,
                FollowedAt = DateTime.UtcNow
            };
            _unitOfWork.Pages.AddFollower(follower);
            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<PageResponseDto>(page);
            return Result<PageResponseDto>.Success(response);
        }

        public async Task<Result> FollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);            
            var followedPages = await _unitOfWork.Pages.GetPagesByFollowerIdAsync(userId);
            var alreadyFollowing = followedPages.Any(p => p.Id == pageId);

            
            var domainDecision = _pageDomainService.CanFollowPage(userId, page, alreadyFollowing);
            if (domainDecision.IsFailure)
                return domainDecision;

            var follower = new PageFollower
            {
                UserId = userId,
                PageId = pageId,
                FollowedAt = DateTime.UtcNow
            };

            
            _unitOfWork.Pages.AddFollower(follower);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> UnfollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null)
                return Result.Failure("Page not found");

            
            var followedPages = await _unitOfWork.Pages.GetPagesByFollowerIdAsync(userId);
            var alreadyFollowing = followedPages.Any(p => p.Id == pageId);
            
            var domainDecision = _pageDomainService.CanUnfollowPage(alreadyFollowing);
            if (domainDecision.IsFailure)
                return domainDecision;

            _unitOfWork.Pages.RemoveFollower(userId, pageId);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result<PageResponseDto>> GetPageByIdAsync(Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null)
                return Result<PageResponseDto>.Failure("Page not found");

            var dto = _mapper.Map<PageResponseDto>(page);
            return Result<PageResponseDto>.Success(dto);
        }

        public async Task<Result<IEnumerable<PageResponseDto>>> GetUserFollowedPagesAsync(Guid userId)
        {
            var pages = await _unitOfWork.Pages.GetPagesByFollowerIdAsync(userId);
            var dtos = _mapper.Map<IEnumerable<PageResponseDto>>(pages);

            return Result<IEnumerable<PageResponseDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<PageResponseDto>>> GetAllPagesAsync()
        {
            var pages = await _unitOfWork.Pages.GetAllAsync();

            var dtos = _mapper.Map<IEnumerable<PageResponseDto>>(pages);

            return Result<IEnumerable<PageResponseDto>>.Success(dtos);
        }

        public async Task<Result> DeletePageAsync(Guid adminId, Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);

            if (page == null)
                return Result.Failure("Page not found");

            if (page.AdminId != adminId)
                return Result.Failure("You are not authorized to delete this page.");

            _unitOfWork.Pages.Delete(page);

            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result<bool>> ToggleFollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null)
                return Result<bool>.Failure("Page not found");

            // Check if already following
            var isFollowing = await IsFollowingAsync(userId, pageId);

            if (isFollowing.Value)
            {
                // Unfollow
                await UnfollowPageAsync(userId, pageId);
                return Result<bool>.Success(false);
            }
            else
            {
                // Follow
                await FollowPageAsync(userId, pageId);
                return Result<bool>.Success(true);
            }
        }

        public async Task<Result<bool>> IsFollowingAsync(Guid userId, Guid pageId)
        {
            var followedPages = await _unitOfWork.Pages.GetPagesByFollowerIdAsync(userId);
            var isFollowing = followedPages.Any(p => p.Id == pageId);
            return Result<bool>.Success(isFollowing);
        }

        public async Task<Result<int>> GetFollowersCountAsync(Guid pageId)
        {
            var count = await _unitOfWork.Pages.GetFollowersCountAsync(pageId);
            return Result<int>.Success(count);
        }

        public async Task<Result<IEnumerable<PageFollowerDto>>> GetFollowersAsync(Guid pageId, int page = 1, int pageSize = 20)
        {
            var followers = await _unitOfWork.Pages.GetFollowersAsync(pageId, page, pageSize);
            var dtos = _mapper.Map<IEnumerable<PageFollowerDto>>(followers);
            return Result<IEnumerable<PageFollowerDto>>.Success(dtos);
        }

        public async Task<Result<PageResponseDto>> UpdatePageAsync(PageUpdateDto updateDto, Guid userId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(updateDto.Id);
            if (page == null)
                return Result<PageResponseDto>.Failure("Page not found.");

            if (page.AdminId != userId)
                return Result<PageResponseDto>.Failure("You are not authorized to edit this page.");

            // Update properties
            page.Name = updateDto.Name;
            page.Description = updateDto.Description;
            page.ImageUrl = updateDto.ImageUrl ?? page.ImageUrl;

            _unitOfWork.Pages.Update(page);
            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<PageResponseDto>(page);
            return Result<PageResponseDto>.Success(response);
        }
    }

}
