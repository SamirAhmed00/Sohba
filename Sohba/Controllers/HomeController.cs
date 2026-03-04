using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.Domain.Common;
using Sohba.Models;
using Sohba.ViewModels;
using System.Diagnostics;

namespace Sohba.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IStoryService _storyService;
        private readonly IHashtagService _hashtagService;
        public HomeController(IPostService postService, IStoryService storyService, IHashtagService hashtagService)
        {
            _postService = postService;
            _storyService = storyService;
            _hashtagService = hashtagService;
        }

        // We Need To Call The ApplicationService With Post Method To Get The Data From The Database And Show It On The Home Page
        public async Task<IActionResult> Index()
        {
            Guid AdminID = GetCurrentUserId();

            var FeedPosts = await _postService.GetFeedAsync(AdminID);

            var storiesResult = await _storyService.GetStoriesForFeedAsync(AdminID);
            var trendingHashtags = await _hashtagService.GetTrendingHashtagsAsync(5);
            ViewBag.TrendingHashtags = trendingHashtags.Value;


            if (FeedPosts.IsFailure)
            {
                ViewBag.ErrorMessage = FeedPosts.Error;
                return View(new HomeViewModel
                {
                    Posts = new List<PostResponseDto>(),
                    Stories = storiesResult.Value ?? new List<StoryResponseDto>(),
                });
            }

            var viewModel = new HomeViewModel
            {
                Posts = FeedPosts.Value,
                Stories = storiesResult.Value ?? new List<StoryResponseDto>(),                
            };

            return View(viewModel);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
