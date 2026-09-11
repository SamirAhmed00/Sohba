using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.SearchAggregate;
using Sohba.Application.Interfaces;

using Sohba.ViewModels.Search;

namespace Sohba.Controllers
{
    [Authorize]
    [EnableRateLimiting("Search")]


    public class SearchController : BaseController
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string q, string tab = "all")
        {
            // Guard: return the Results view with an empty model for blank/short queries.
            // Explicitly naming "Results" prevents the default Index.cshtml lookup which
            // caused a 404 because only Results.cshtml exists in Views/Search/.
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return View("Results", new SearchViewModel { Query = q, ActiveTab = tab });

            var userId = GetCurrentUserId();
            // Full search results page loads all categories up to 20 items so client-side tabs function seamlessly.
            var result = await _searchService.GlobalSearchAsync(q, userId, scope: "all", limit: 20);

            var viewModel = new SearchViewModel
            {
                Query = q,
                Results = result.IsSuccess && result.Value != null ? result.Value : new SearchResultDto(),
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "all" : tab.ToLowerInvariant()
            };

            return View("Results", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> QuickSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(BaseResponseDto<SearchResultDto>.FailureResponse("Query too short or empty."));
            }

            var userId = GetCurrentUserId();
            var result = await _searchService.GlobalSearchAsync(q, userId, "all", 10);

            if (!result.IsSuccess)
                return Json(BaseResponseDto<SearchResultDto>.FailureResponse(result.Error));

            var originalTotal = result.Value.TotalCount;
            var data = new SearchResultDto
            {
                Posts = result.Value.Posts.Take(3).ToList(),
                Users = result.Value.Users.Take(3).ToList(),
                Groups = result.Value.Groups.Take(3).ToList(),
                Pages = result.Value.Pages.Take(3).ToList(),
                TotalCount = originalTotal
            };

            return Json(BaseResponseDto<SearchResultDto>.SuccessResponse(data));
        }
    }

}
