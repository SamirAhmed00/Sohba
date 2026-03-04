using Microsoft.AspNetCore.Mvc;
using Sohba.Application.Interfaces;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.ViewModels.Search;

namespace Sohba.Controllers
{
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
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return View(new SearchViewModel { Query = q });
            }

            var userId = GetCurrentUserId();
            var result = await _searchService.GlobalSearchAsync(q, userId);

            var viewModel = new SearchViewModel
            {
                Query = q,
                Results = result.Value,
                ActiveTab = tab
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> QuickSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(new { success = false });
            }

            var userId = GetCurrentUserId();
            var result = await _searchService.GlobalSearchAsync(q, userId);

            return Json(new
            {
                success = true,
                results = new
                {
                    posts = result.Value.Posts.Take(3),
                    users = result.Value.Users.Take(3),
                    groups = result.Value.Groups.Take(3),
                    pages = result.Value.Pages.Take(3),
                    total = result.Value.TotalCount
                }
            });
        }
    }

}
