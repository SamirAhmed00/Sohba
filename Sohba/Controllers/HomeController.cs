using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Domain.Common;
using Sohba.Models;
using Sohba.ViewModels;
using System.Diagnostics;

namespace Sohba.Controllers
{
    [Authorize]
    [EnableRateLimiting("Feed")]
    public class HomeController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IStoryService _storyService;
        private readonly IHashtagService _hashtagService;
        private readonly ICompositeViewEngine _viewEngine;
        public HomeController(IPostService postService, IStoryService storyService, IHashtagService hashtagService, ICompositeViewEngine viewEngine)
        {
            _postService = postService;
            _storyService = storyService;
            _hashtagService = hashtagService;
            _viewEngine = viewEngine;
        }


        // Get posts as HTML partial
        [HttpGet]
        public async Task<IActionResult> GetPostCards(int page = 2, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.GetFeedAsync(userId, page, pageSize);

            if (result.IsFailure)
                return Json(new { success = false, error = result.Error });

            // Render the _PostCard partial with the posts
            var html = await RenderPartialViewToString("Partials/_PostCard", result.Value.Items);

            return Json(new
            {
                success = true,
                html = html,
                hasMore = result.Value.HasNextPage,
                currentPage = result.Value.Page,
                totalPages = result.Value.TotalPages
            });
        }

        // Helper method to render partial view to string
        private async Task<string> RenderPartialViewToString(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.ActionDescriptor.ActionName;

            ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);

                if (viewResult.View == null)
                    throw new ArgumentNullException($"{viewName} does not match any available view");

                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }


        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();

            //  Get paginated posts
            var feedResult = await _postService.GetFeedAsync(userId, page, pageSize);

            var storiesResult = await _storyService.GetStoriesForFeedAsync(userId);
            var trendingHashtags = await _hashtagService.GetTrendingHashtagsAsync(5);

            ViewBag.TrendingHashtags = trendingHashtags.Value;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            if (feedResult.IsFailure)
            {
                ViewBag.ErrorMessage = feedResult.Error;
                return View(new HomeViewModel
                {
                    Posts = new List<PostResponseDto>(),
                    Stories = storiesResult.Value ?? new List<StoryResponseDto>(),
                    PagedResult = new PagedResult<PostResponseDto>()
                });
            }

            var viewModel = new HomeViewModel
            {
                Posts = feedResult.Value.Items,
                Stories = storiesResult.Value ?? new List<StoryResponseDto>(),
                PagedResult = feedResult.Value
            };

            return View(viewModel);
        }

        //  NEW: Load more posts via AJAX (for infinite scroll)
        [HttpGet]
        public async Task<IActionResult> LoadMore(int page = 2, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.GetFeedAsync(userId, page, pageSize);

            if (result.IsFailure)
                return Json(new { success = false, error = result.Error });

            return Json(new
            {
                success = true,
                posts = result.Value.Items,
                hasMore = result.Value.HasNextPage,
                currentPage = result.Value.Page,
                totalPages = result.Value.TotalPages
            });
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
