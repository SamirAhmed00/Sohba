# Phase 3 & 4 Implementation Plan: Secure Post/Comment Management

This plan coordinates the execution of our newly defined high-severity targets. It centralizes stringent security definitions per `RULES.md` and aligns user orchestration closely with `UI_RULES.md`.

## User Review Required

> [!WARNING]
> Changing `IFileStorageService` to return `Result<string>` instead of a bare `string` is a breaking contract change across the system. It dictates that all endpoints uploading files (`StoriesController`, `PostsController`, `PagesController`, `GroupsController`) must be updated to unpack the `Result` envelope securely.

## Proposed Changes

### Component 1: Centralized Architecture Security (Phase 3)

#### [MODIFY] [IFileStorageService.cs](file:///c:/Users/basmf/source/repos/SohbaANTII/Sohba.Application/Interfaces/IFileStorageService.cs) & [LocalFileStorageService.cs](file:///c:/Users/basmf/source/repos/SohbaANTII/Sohba.Infrastructure/LocalFileStorageService.cs)
- Refactor the interface from `Task<string>` to `Task<Result<string>>`.
- Eradicate system-level exceptions during upload validation. Swap `throw new InvalidOperationException()` with native domain `Result.Failure()`.
- Confirm strict adherence to 5MB and specific extensions (`.jpg`, `.jpeg`, `.png`, `.gif`).

#### [MODIFY] Controllers using IFileStorageService
- Unpack `Result.Value` inside `StoriesController`, `PostsController`, `PagesController`, and `GroupsController`. Propagate `Result.Error` to `BaseResponseDto` smoothly per the standard.

### Component 2: Logic Validation (Phase 3)

#### [NEW] Sohba.Application/Validators/CommentCreateDtoValidator.cs
- Introduce FluentValidation. Enforce minimum constraints and an absolute constraint of 500 characters maximum for comments to resolve UI blockings.

#### [NEW] Sohba.Application/Validators/PostCreateDtoValidator.cs
- Introduce FluentValidation.
- Mandate checking that exactly one of the `Privacy` enum variants (Public, Friends, Private) is legally constructed.

#### [MODIFY] [PostService.cs](file:///c:/Users/basmf/source/repos/SohbaANTII/Sohba.Application/Services/PostService.cs)
- Patch `GetTimelineAsync` and subsequent Feed logic structures to actively check relations over the `Privacy` logic (Public / Friends Only / Private to self) ensuring that Posts strictly honor data access governance.

### Component 3: Frontend Standard Governance (Phase 4)

#### [MODIFY] [wwwroot/js/features/posts.js](file:///c:/Users/basmf/source/repos/SohbaANTII/Sohba/wwwroot/js/features/posts.js)
- Gut native `confirm()`. Refactor `deletePost(postId)` to asynchronously trigger `window.showConfirmModal()`.
- Enhance the callback payload. Upon receiving a positive JSON token from `BaseResponseDto`, execute visual dismantling: select `#post-${postId}`, enforce opacity 0, execute 300ms fade transition, invoke `.remove()`. 
- Bind identical behavior for `.editPost`.

#### [MODIFY] [wwwroot/js/features/comments.js](file:///c:/Users/basmf/source/repos/SohbaANTII/Sohba/wwwroot/js/features/comments.js)
- Wire `deleteComment(commentId)` bridging onto `window.showConfirmModal()`.
- Read DOM bindings: Subtract `1` dynamically from post comment counter `(e.g., #comment-count-${postId})` upon success via innerText injection ensuring no browser reloads occur per the `UI_RULES.md` policy.

### Component 4: Administrative Features (Phase 4)

#### [MODIFY] [InteractionService.cs](file:///c:/Users/basmf/source/repos/SohbaANTII/Sohba.Application/Services/InteractionService.cs) & [CommentsController.cs](file:///c:/Users/basmf/source/repos/SohbaANTII/Sohba/Controllers/CommentsController.cs)
- Pass dynamic authentication checks (`User.IsInRole("Admin")`) deeply into the Delete logic allowing Administrators and Moderators total sovereignty to purge malicious replies. 

## Open Questions
- To utilize FluentValidation elegantly across the pipeline, I will register an automated Filter/Middleware that maps all `ValidationException` occurrences to `BaseResponseDto.FailureResponse` ensuring clean JSON. Is adding this middleware permissible, or should we evaluate FluentValidation `ValidateAsync` loops explicitly inside the Controller actions?

## Verification Plan

### Automated Tests
- I'll locally trigger `dotnet build` confirming that the API layer successfully absorbs the new `IFileStorageService` signature.

### Manual Verification
- Testing the creation of a massive comment to confirm validation bounces the request seamlessly via UI Toast message displaying "Limit: 500 characters".
- Uploading a mock file `.exe` to trigger `Result.Failure` inside the Controller logic.
