using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Domain.Common;
using Sohba.Models;
using System.Diagnostics;

namespace Sohba.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPostService _postService;
        public HomeController(IPostService postService) 
        {
            _postService = postService; 
        }

        // We Need To Call The ApplicationService With Post Method To Get The Data From The Database And Show It On The Home Page
        public async Task<IActionResult> Index()
        {
            // Assume We Logged In With Admin User And We Have The User Id 
            // GUid From The Database For The Admin User : 36FF9501-0409-F111-9291-902B34AC4276
            Guid AdminID = new Guid("36FF9501-0409-F111-9291-902B34AC4276");
            var FeedPosts = await _postService.GetFeedAsync(AdminID);
            if (FeedPosts.IsFailure)
            {
                ViewBag.ErrorMessage = FeedPosts.Error;
                return View(new List<PostResponseDto>());
            }
            return View(FeedPosts.Value);
        }

        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
