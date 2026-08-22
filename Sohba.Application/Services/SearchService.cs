using AutoMapper;
using Sohba.Application.DTOs.SearchAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class SearchService : ISearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SearchService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<SearchResultDto>> GlobalSearchAsync(string query, Guid currentUserId)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Result<SearchResultDto>.Success(new SearchResultDto());

            var term = query.Trim();
            List<PostSearchResultDto> postDtos;

            if (term.StartsWith("#") && term.Length > 1)
            {
                var tag = term.TrimStart('#').Trim();
                var hashtagPosts = await _unitOfWork.Posts.GetPostsByHashtagAsync(tag);
                postDtos = _mapper.Map<List<PostSearchResultDto>>(hashtagPosts.Take(5));
                term = tag;
            }
            else
            {
                var posts = await _unitOfWork.Posts.SearchPostsAsync(term, currentUserId, 5);
                postDtos = _mapper.Map<List<PostSearchResultDto>>(posts);
            }

            var users = await _unitOfWork.Users.SearchUsersAsync(term, currentUserId, 5);
            var groups = await _unitOfWork.Groups.SearchGroupsAsync(term, 5);
            var pages = await _unitOfWork.Pages.SearchPagesAsync(term, 5);

            var result = new SearchResultDto
            {
                Posts = postDtos,
                Users = _mapper.Map<List<UserSearchResultDto>>(users),
                Groups = _mapper.Map<List<GroupSearchResultDto>>(groups),
                Pages = _mapper.Map<List<PageSearchResultDto>>(pages)
            };

            return Result<SearchResultDto>.Success(result);
        }

        public async Task<Result<List<PostSearchResultDto>>> SearchPostsAsync(string query, Guid currentUserId)
        {
            var posts = await _unitOfWork.Posts.SearchPostsAsync(query, currentUserId);
            var dtos = _mapper.Map<List<PostSearchResultDto>>(posts);
            return Result<List<PostSearchResultDto>>.Success(dtos);
        }

        public async Task<Result<List<UserSearchResultDto>>> SearchUsersAsync(string query, Guid currentUserId)
        {
            var users = await _unitOfWork.Users.SearchUsersAsync(query, currentUserId);
            var dtos = _mapper.Map<List<UserSearchResultDto>>(users);
            return Result<List<UserSearchResultDto>>.Success(dtos);
        }

        public async Task<Result<List<GroupSearchResultDto>>> SearchGroupsAsync(string query)
        {
            var groups = await _unitOfWork.Groups.SearchGroupsAsync(query);
            var dtos = _mapper.Map<List<GroupSearchResultDto>>(groups);
            return Result<List<GroupSearchResultDto>>.Success(dtos);
        }

        public async Task<Result<List<PageSearchResultDto>>> SearchPagesAsync(string query)
        {
            var pages = await _unitOfWork.Pages.SearchPagesAsync(query);
            var dtos = _mapper.Map<List<PageSearchResultDto>>(pages);
            return Result<List<PageSearchResultDto>>.Success(dtos);
        }
    }

}
