using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.PostAggregate.Requests;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;

using Sohba.Domain.Common;
using Sohba.Domain.Enums;
using Sohba.ViewModels.Post;

namespace Sohba.Controllers
{
    [Authorize]
    [EnableRateLimiting("Api")]

    public class PostsController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IReportingService _reportingService;
        private readonly IInteractionService _interactionService;
        private readonly IHashtagService _hashtagService;
        private readonly IFileStorageService _fileStorage;

        public PostsController(
            IPostService postService,
            IInteractionService interactionService,
            IReportingService reportingService,
            IHashtagService hashtagService,
            IFileStorageService fileStorage)
        {
            _postService = postService;
            _interactionService = interactionService;
            _reportingService = reportingService;
            _hashtagService = hashtagService;
            _fileStorage = fileStorage;
        }

        [HttpGet]
        public IActionResult Create(Guid? groupId = null, Guid? pageId = null)
        {
            ViewBag.GroupId = groupId;
            ViewBag.PageId = pageId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostCreateViewModel model, Guid? groupId = null, Guid? pageId = null)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }
                return View(model);
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Auth");

            string imageUrl = null;
            var imageUrls = new List<string>();

            if (model.ImageFiles != null && model.ImageFiles.Any(f => f != null && f.Length > 0))
            {
                foreach (var file in model.ImageFiles.Where(f => f != null && f.Length > 0))
                {
                    var uploadResult = await _fileStorage.SaveFileAsync(file, "posts");
                    if (!uploadResult.IsSuccess)
                    {
                        ModelState.AddModelError("ImageFiles", uploadResult.Error);
                        return View(model);
                    }
                    if (uploadResult.Value != null)
                    imageUrls.Add(uploadResult.Value);
                }
            }
            else if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveFileAsync(model.ImageFile, "posts");
                if (!uploadResult.IsSuccess)
                {
                    ModelState.AddModelError("ImageFile", uploadResult.Error);
                    return View(model);
                }
                imageUrl = uploadResult.Value;
                if (imageUrl != null) imageUrls.Add(imageUrl);
            }

            var isGroupOrPagePost = groupId.HasValue || pageId.HasValue;

            var dto = new PostCreateDto
            {
                Title = model.Title,
                Content = model.Content,
                ImageUrl = imageUrl ?? imageUrls.FirstOrDefault(),
                ImageUrls = imageUrls,
                Privacy = isGroupOrPagePost
                    ? PostPrivacy.Public
                    : (model.IsPrivate ? PostPrivacy.Private : model.Privacy)
            };

            if (groupId.HasValue)
            {
                dto.SourceType = PostSourceType.Group;
                dto.SourceId = groupId.Value;
            }
            else if (pageId.HasValue)
            {
                dto.SourceType = PostSourceType.Page;
                dto.SourceId = pageId.Value;
            }

            var result = await _postService.CreatePostAsync(dto, userId);

            if (result.IsSuccess)
            {
                if (groupId.HasValue)
                    return RedirectToAction("Details", "Groups", new { id = groupId.Value });
                else if (pageId.HasValue)
                    return RedirectToAction("Details", "Pages", new { id = pageId.Value });
                else
                    return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPostDetails(Guid postId)
        {
            var userId = GetCurrentUserId();

            var postResult = await _postService.GetPostByIdAsync(postId, userId);
            if (postResult.IsFailure)
                return NotFound(new { success = false, error = postResult.Error });

            var comments = await _interactionService.GetCommentsByPostIdAsync(postId, userId);         

            return Json(new
            {
                success = true,
                post = postResult.Value,
                comments = comments
            });
        }


        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = GetCurrentUserId();

            var result = await _postService.GetPostByIdAsync(id, userId);

            if (result.IsFailure || result.Value == null)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.GetPostByIdAsync(id, userId);

            if (result.IsFailure || result.Value == null)
            {
                return NotFound();
            }

            var post = result.Value;

            if (!post.IsAuthor)
                return Forbid();

            //return Json(BaseResponseDto<PostResponseDto>.SuccessResponse(PostUpdateDto));
            // In an ideal scenario, AutoMapper should map PostResponseDto to PostEditViewModel
            var vm = new PostEditViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                Privacy = post.Privacy
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PostEditViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(BaseResponseDto<object>.FailureResponse("Invalid form data submitted."));

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Json(BaseResponseDto<object>.FailureResponse("User not authenticated."));

            string imageUrl = model.ImageUrl;
            string previousImageUrl = model.ImageUrl;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveFileAsync(model.ImageFile, "posts");
                if (!uploadResult.IsSuccess)
                    return Json(BaseResponseDto<object>.FailureResponse(uploadResult.Error));

                if (uploadResult.Value != null)
                    imageUrl = uploadResult.Value;
            }

            var updateDto = new PostUpdateDto
            {
                Id = model.Id,
                Title = model.Title,
                Content = model.Content,
                ImageUrl = imageUrl,
                Privacy = model.Privacy
            };

            var result = await _postService.UpdatePostAsync(model.Id, updateDto, userId);

            if (result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(previousImageUrl) &&
                    !string.Equals(previousImageUrl, imageUrl, StringComparison.OrdinalIgnoreCase))
                {
                    await _fileStorage.DeleteFileAsync(previousImageUrl);
                }

                var updatedPost = await _postService.GetPostByIdAsync(model.Id, userId);
                return Json(BaseResponseDto<PostResponseDto>.SuccessResponse(updatedPost.Value));
            }

            return Json(BaseResponseDto<object>.FailureResponse(result.Error));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] DeletePostModel model)
        {
            if (model == null || model.id == Guid.Empty)
                return Json(BaseResponseDto<object>.FailureResponse("Invalid post ID."));

            var userId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");
            var result = await _postService.DeletePostAsync(model.id, userId, isAdmin);
            if (result.IsSuccess)
                return Json(BaseResponseDto<object>.SuccessResponse(null));

            return Json(BaseResponseDto<object>.FailureResponse(result.Error));
        }
        public class DeletePostModel
        {
            public Guid id { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> React([FromBody] ReactionRequestDto request)
        {
            if (request == null || request.PostId == Guid.Empty || string.IsNullOrWhiteSpace(request.ReactionType))
                return BadRequest(new { success = false, error = "Invalid request data." });

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { success = false, error = "User not authenticated." });

            if (!Enum.TryParse<ReactionType>(request.ReactionType, true, out var type))
                return BadRequest(new { success = false, error = "Invalid reaction type." });

            var existingReaction = await _interactionService.GetUserReactionAsync(userId, request.PostId);

            if (existingReaction != null)
            {
                var removeResult = await _interactionService.RemoveReactionAsync(userId, request.PostId);

                if (!removeResult.IsSuccess)
                    return Json(new { success = false, error = removeResult.Error });

                var newCount = await _interactionService.GetReactionCountAsync(request.PostId);

                return Json(new
                {
                    success = true,
                    action = "removed",
                    newCount
                });
            }
            else
            {
                var addResult = await _interactionService.AddReactionAsync(userId, request.PostId, type);

                if (!addResult.IsSuccess)
                    return Json(new { success = false, error = addResult.Error });

                var newCount = await _interactionService.GetReactionCountAsync(request.PostId);

                return Json(new
                {
                    success = true,
                    action = "added",
                    newCount,
                    reactionType = request.ReactionType
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment([FromBody] CommentRequestDto request)
        {
            if (request == null || request.PostId == Guid.Empty || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { success = false, error = "Invalid data." });

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { success = false, error = "Unauthorized." });

            var result = await _interactionService.AddCommentAsync(
                userId,
                request.PostId,
                request.Content,
                request.ParentCommentId
            );

            if (!result.IsSuccess)
                return Json(new { success = false, error = result.Error });
            var latestCommentId = result.Value;
            var comments = await _interactionService.GetCommentsByPostIdAsync(request.PostId, userId);

            // Find the newly created comment anywhere in the recursive tree (levels 1-4).
            CommentResponseDto latest = null;

            foreach (var topLevel in comments)
            {
                latest = FindCommentNode(topLevel, latestCommentId);
                if (latest != null) break;
            }

            if (latest == null)
                return Json(new { success = false, error = "Comment created but could not be retrieved." });


            return Json(new
            {
                success = true,
                comment = latest
            });
        }

        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.GetFavoritePostsAsync(userId);
            return View(result.Value ?? new List<PostResponseDto>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSavePost([FromBody] ToggleSaveRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { success = false, error = "User not authenticated." });

            var tag = request.IsFavorite ? SavedTag.Favorite : SavedTag.General;

            var existingSave = await _interactionService.GetSavedPostAsync(userId, request.PostId);

            if (existingSave != null)
            {
                var removeResult = await _interactionService.RemoveSavedPostAsync(userId, request.PostId);
                if (removeResult.IsSuccess)
                    return Json(new { success = true, saved = false, message = "Post removed from saved." });
                else
                    return Json(new { success = false, error = removeResult.Error });
            }
            else
            {
                var saveResult = await _interactionService.SavePostAsync(userId, request.PostId, tag);
                if (saveResult.IsSuccess)
                    return Json(new { success = true, saved = true, message = "Post saved.", data = saveResult.Value });
                else
                    return Json(new { success = false, error = saveResult.Error });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetUserCollections()
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.GetUserCollectionsAsync(userId);
            return Json(BaseResponseDto<IEnumerable<SavedCollectionDto>>.SuccessResponse(result.Value));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCollection([FromBody] CreateSavedCollectionDto request)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(request?.Name))
                return Json(BaseResponseDto.FailureResponse("Collection name is required."));

            var result = await _interactionService.CreateCollectionAsync(userId, request.Name.Trim());

            if (!result.IsSuccess)
                return Json(BaseResponseDto.FailureResponse(result.Error));

            return Json(BaseResponseDto<SavedCollectionDto>.SuccessResponse(result.Value));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveToCollection([FromBody] SaveToCollectionDto request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.PostId == Guid.Empty || request.CollectionId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request."));

            var result = await _interactionService.SavePostToCollectionAsync(userId, request.PostId, request.CollectionId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite([FromBody] SaveToCollectionDto request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.PostId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request."));

            var result = await _interactionService.SavePostToFavoritesAsync(userId, request.PostId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportPost([FromBody] PostReportRequestDto request)
        {
            if (request == null || request.PostId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { success = false, error = "Invalid request data." });

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { success = false, error = "User not authenticated." });

            var result = await _reportingService.ReportPostWithDetailsAsync(request, userId);

            if (result.IsSuccess)
            {
                return Json(new
                {
                    success = true,
                    message = "Post reported successfully.",
                    report = result.Value,
                    postId = request.PostId
                });
            }

            return Json(new { success = false, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeSavedPostTag([FromBody] ChangeTagRequestDto request)
        {
            var userId = GetCurrentUserId();

            if (!Enum.TryParse<SavedTag>(request.Tag, true, out var tag))
                return Json(new { success = false, error = "Invalid tag" });

            var result = await _interactionService.SavePostAsync(userId, request.PostId, tag);

            if (result.IsSuccess)
                return Json(new { success = true });

            return Json(new { success = false, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSavedPost([FromBody] RemoveSavedRequestDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.RemoveSavedPostAsync(userId, request.PostId);

            if (result.IsSuccess)
                return Json(new { success = true });

            return Json(new { success = false, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromSaved([FromBody] RemoveSavedRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.PostId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request."));

            var result = await _interactionService.RemoveSavedPostsFromCollectionsAsync(userId, request.PostId);

            if (!result.IsSuccess)
                return Json(BaseResponseDto.FailureResponse(result.Error));

            return Json(new { success = true });
        }


        [HttpGet]
        public async Task<IActionResult> SavedPosts(int page = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.GetSavedPostsGroupedPagedAsync(userId, page, pageSize);

            if (result.IsFailure)
                return View(new PagedResult<SavedPostsGroupedDto>());

            ViewBag.Page = result.Value.Page;
            ViewBag.PageSize = result.Value.PageSize;
            ViewBag.TotalPages = result.Value.TotalPages;

            return View(result.Value);
        }


        [HttpGet]
        public async Task<IActionResult> Hashtag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return RedirectToAction("Index", "Home");

            var userId = GetCurrentUserId();
            var result = await _hashtagService.GetPostsByHashtagAsync(tag, userId);

            ViewBag.Hashtag = tag;
            return View(result.Value ?? new List<PostResponseDto>());
        }

        [HttpGet]
        public async Task<IActionResult> SearchByHashtag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return Json(new { success = false, error = "Tag is required" });

            var userId = GetCurrentUserId();
            var result = await _hashtagService.GetPostsByHashtagAsync(tag, userId);

            return Json(new { success = true, posts = result.Value });
        }



        // -- Helper 

        // Local function: recursively find a comment node by id in the reply tree.
        static CommentResponseDto FindCommentNode(CommentResponseDto node, Guid id)
        {
            if (node.Id == id) return node;

            if (node.Replies != null)
            {
                foreach (var reply in node.Replies)
                {
                    var match = FindCommentNode(reply, id);
                    if (match != null) return match;
                }
            }

            return null;
        }

    }
}