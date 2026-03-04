using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.ViewModels.Group;

namespace Sohba.Controllers
{
    public class GroupsController : BaseController
    {
        private readonly IGroupService _groupService;
        private readonly IPostService _postService;

        public GroupsController(IGroupService groupService, IPostService postService)
        {
            _groupService = groupService;
            _postService = postService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var result = await _groupService.GetAllGroupsAsync(userId != Guid.Empty ? userId : null);
            return View(result.Value ?? new List<GroupResponseDto>());
        }
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroupCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Auth");

            string imageUrl = null;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "groups");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(model.ImageFile.FileName)}{Path.GetExtension(model.ImageFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                imageUrl = $"/uploads/groups/{uniqueFileName}";
            }

            var dto = new GroupCreateDto
            {
                Name = model.Name,
                Description = model.Description,
                ImageUrl = imageUrl 
            };

            var result = await _groupService.CreateGroupAsync(dto, userId);

            if (result.IsSuccess)
                return RedirectToAction("Index");

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Join(Guid groupId)
        {
            var userId = GetCurrentUserId();
            var result = await _groupService.JoinGroupAsync(groupId, userId);
            return Json(new { success = result.IsSuccess });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var groupResult = await _groupService.GetGroupByIdAsync(id);
            if (groupResult.IsFailure)
                return NotFound();

            var membersResult = await _groupService.GetGroupMembersAsync(id);
            var viewModel = new GroupDetailsViewModel
            {
                Group = groupResult.Value,
                Members = membersResult.Value ?? new List<GroupMemberDto>()
            };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetCurrentUserId();
            var groupResult = await _groupService.GetGroupByIdAsync(id);
            if (groupResult.IsFailure)
                return NotFound();

            //if (groupResult.Value.AdminName != GetCurrentUserName()) 
              //  return Forbid();

            var viewModel = new GroupEditViewModel
            {
                Id = groupResult.Value.Id,
                Name = groupResult.Value.Name,
                Description = groupResult.Value.Description,
                ImageUrl = groupResult.Value.ImageUrl
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GroupEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();
            string imageUrl = model.ImageUrl;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "groups");
                Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(model.ImageFile.FileName)}{Path.GetExtension(model.ImageFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                    await model.ImageFile.CopyToAsync(fileStream);
                imageUrl = $"/uploads/groups/{uniqueFileName}";
            }

            var updateDto = new GroupUpdateDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                ImageUrl = imageUrl
            };

            var result = await _groupService.UpdateGroupAsync(updateDto, userId);
            if (result.IsSuccess)
                return RedirectToAction("Details", new { id = model.Id });

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupPosts(Guid groupId)
        {
            var userId = GetCurrentUserId();
            var postsResult = await _postService.GetGroupPostsAsync(groupId, userId); 

            if (postsResult.IsSuccess)
            {
                return PartialView("Partials/_PostCard", postsResult.Value);
            }

            return Content("No posts yet");
        }

        [HttpGet]
        public async Task<IActionResult> GetTabContent(Guid groupId, string tab)
        {
            var userId = GetCurrentUserId();

            if (tab == "members")
            {
                var membersResult = await _groupService.GetGroupMembersAsync(groupId);
                ViewBag.GroupId = groupId;
                return PartialView("_MembersTab", membersResult.Value);
            }
            else if (tab == "about")
            {
                var groupResult = await _groupService.GetGroupByIdAsync(groupId);
                var membersResult = await _groupService.GetGroupMembersAsync(groupId);
                ViewBag.PostsCount = 0;
                ViewBag.GroupId = groupId;
                return PartialView("_AboutTab", new { Group = groupResult.Value, Members = membersResult.Value });
            }

            return Content("");
        }



        // Helper Methods
        //private Guid GetCurrentUserId()
        //{
        //    //var userIdStr = HttpContext.Session.GetString("UserId");
        //    //return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);
        //    return new Guid("36FF9501-0409-F111-9291-902B34AC4276");

        //}

        //private string GetCurrentUserName()
        //{
        //    return User.Identity?.Name ?? string.Empty;
        //}
    }
}
