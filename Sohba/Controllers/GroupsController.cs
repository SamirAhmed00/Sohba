using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.ViewModels.Group;

namespace Sohba.Controllers
{
    public class GroupsController : Controller
    {
        private readonly IGroupService _groupService;

        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroupCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            var dto = new GroupCreateDto
            {
                Name = model.Name,
                Description = model.Description,
                AdminId = userId
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

        public async Task<IActionResult> Details(Guid id)
        {
            var membersResult = await _groupService.GetGroupMembersAsync(id);
            return View(membersResult.Value);
        }

        private Guid GetCurrentUserId()
        {
            //var userIdStr = HttpContext.Session.GetString("UserId");
            //return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);
            return new Guid("36FF9501-0409-F111-9291-902B34AC4276");

        }
    }
}
