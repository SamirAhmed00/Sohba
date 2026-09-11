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

        public async Task<Result<SearchResultDto>> GlobalSearchAsync(string query, Guid currentUserId, string scope = "all", int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Result<SearchResultDto>.Success(new SearchResultDto());

            var term = query.Trim();
            var effectiveScope = (scope ?? "all").ToLowerInvariant();
            var postDtos = new List<PostSearchResultDto>();
            var userDtos = new List<UserSearchResultDto>();
            var groupDtos = new List<GroupSearchResultDto>();
            var pageDtos = new List<PageSearchResultDto>();

            // Hashtag queries map strictly to Posts.
            // When an incompatible scope is requested (users, groups, pages), 0 results are returned.
            if (term.StartsWith("#") && term.Length > 1)
            {
                if (effectiveScope == "all" || effectiveScope == "posts")
                {
                    var tag = term.TrimStart('#').Trim();
                    var hashtagPosts = await _unitOfWork.Posts.GetPostsByHashtagAsync(tag, currentUserId);
                    postDtos = _mapper.Map<List<PostSearchResultDto>>(hashtagPosts.Take(limit));
                }

                return Result<SearchResultDto>.Success(new SearchResultDto
                {
                    Posts = postDtos
                });
            }

            if (effectiveScope == "all" || effectiveScope == "posts")
            {
                var posts = await _unitOfWork.Posts.SearchPostsAsync(term, currentUserId, limit);
                postDtos = _mapper.Map<List<PostSearchResultDto>>(posts);
            }

            if (effectiveScope == "all" || effectiveScope == "people" || effectiveScope == "users")
            {
                var users = await _unitOfWork.Users.SearchUsersAsync(term, currentUserId, limit);
                userDtos = _mapper.Map<List<UserSearchResultDto>>(users);
            }

            if (effectiveScope == "all" || effectiveScope == "groups")
            {
                var groups = await _unitOfWork.Groups.SearchGroupsAsync(term, limit);
                groupDtos = _mapper.Map<List<GroupSearchResultDto>>(groups);
            }

            if (effectiveScope == "all" || effectiveScope == "pages")
            {
                var pages = await _unitOfWork.Pages.SearchPagesAsync(term, limit);
                pageDtos = _mapper.Map<List<PageSearchResultDto>>(pages);
            }

            var result = new SearchResultDto
            {
                Posts = postDtos,
                Users = userDtos,
                Groups = groupDtos,
                Pages = pageDtos
            };

            return Result<SearchResultDto>.Success(result);
        }


        public async Task<Result<List<PostSearchResultDto>>> SearchPostsAsync(string query, Guid currentUserId)
        {
            var result = await GlobalSearchAsync(query, currentUserId, scope: "posts", limit: 20);
            return result.IsSuccess
                ? Result<List<PostSearchResultDto>>.Success(result.Value.Posts)
                : Result<List<PostSearchResultDto>>.Failure(result.Error);
        }

        public async Task<Result<List<UserSearchResultDto>>> SearchUsersAsync(string query, Guid currentUserId)
        {
            var result = await GlobalSearchAsync(query, currentUserId, scope: "people", limit: 20);
            return result.IsSuccess
                ? Result<List<UserSearchResultDto>>.Success(result.Value.Users)
                : Result<List<UserSearchResultDto>>.Failure(result.Error);
        }

        public async Task<Result<List<GroupSearchResultDto>>> SearchGroupsAsync(string query)
        {
            var result = await GlobalSearchAsync(query, Guid.Empty, scope: "groups", limit: 20);
            return result.IsSuccess
                ? Result<List<GroupSearchResultDto>>.Success(result.Value.Groups)
                : Result<List<GroupSearchResultDto>>.Failure(result.Error);
        }

        public async Task<Result<List<PageSearchResultDto>>> SearchPagesAsync(string query)
        {
            var result = await GlobalSearchAsync(query, Guid.Empty, scope: "pages", limit: 20);
            return result.IsSuccess
                ? Result<List<PageSearchResultDto>>.Success(result.Value.Pages)
                : Result<List<PageSearchResultDto>>.Failure(result.Error);
        }
    }

}
