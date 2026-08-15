# Sohba — Fixesv4 Implementation Guide

<br>
<br>
<br>

**Document Name:** Fixesv4.md

**Purpose:** Complete implementation guide for the three blocking issues discovered while
testing the frontend test plan.

**Scope:** This document ONLY addresses the three blocking issues listed in the request:

1. **Issue 1 — Newly Created Comment/Reply Missing Current User Name** (full-stack
   investigation + complete implementation).
2. **Issue 2 — Duplicate Key Exception in `MapPostsToResponse`** (root-cause investigation +
   complete implementation).
3. **Issue 3 — Duplicate Key Exception in `MapPostsWithInteractions`** (root-cause
   investigation + complete implementation).

**Author Role:** Senior Software Architect / Senior ASP.NET Core MVC Engineer / Senior .NET
Backend Engineer / Senior Frontend Engineer / Code Reviewer / QA Engineer.

**Stack:** ASP.NET Core MVC · Clean Architecture (Domain / Application / Infrastructure /
Presentation) · Repository Pattern · Dependency Injection · Entity Framework Core ·
JavaScript (Vanilla) · AJAX · AutoMapper.

**Important:** No project source file was modified while writing this document. This is a
guide only.

<br>
<br>

---

<br>

# TABLE OF CONTENTS

1. [How To Use This Document](#how-to-use-this-document)
2. [Architecture Rules (Mandatory)](#architecture-rules-mandatory)
3. [Issue 1 — Newly Created Comment/Reply Missing Current User Name](#issue-1--newly-created-commentreply-missing-current-user-name)
4. [Issue 2 — Duplicate Key Exception in MapPostsToResponse](#issue-2--duplicate-key-exception-in-mappoststoresponse)
5. [Issue 3 — Duplicate Key Exception in MapPostsWithInteractions](#issue-3--duplicate-key-exception-in-mappostswithinteractions)
6. [Appendix — Full File Inventory](#appendix--full-file-inventory)

<br>
<br>

---

<br>

# How To Use This Document

For every issue, the following sections are provided:

- **Issue** — the reported problem.
- **Related Feature** — feature name and the related section from `Sohba_Frontend_Test_Plan.md`.
- **Expected Behaviour** — what should happen.
- **Current Behaviour** — what actually happens.
- **Root Cause** — the REAL cause, not the symptom.
- **Execution Flow** — full trace from frontend to database and back.
- **Related Files** — every file inspected, with full project paths.
- **Affected Components** — every component involved.
- **Files That Need Modification** — only the files actually requiring changes.
- **Implementation Plan** — step-by-step, preserving the existing architecture.
- **Code Changes** —
    - <span style="color:red">**RED** blocks = code to REMOVE / replace.</span>
    - <span style="color:green">**GREEN** blocks = code to ADD / keep.</span>
    - No `+` / `-` prefixes are used so code can be copied directly.
    - **Before EVERY code snippet, the exact full project file path is specified.**
- **Regression Testing** — test users, required data, navigation steps, expected results,
  failure conditions, edge cases.

<br>
<br>

---

<br>

# Architecture Rules (Mandatory)

These rules MUST be preserved in every fix:

1. Never bypass the Application Layer.
2. Never bypass the Repository Pattern.
3. Never bypass Dependency Injection.
4. Never bypass the Domain Rules / Domain Services.
5. Never bypass Authorization / `[Authorize]` attributes.
6. Never bypass FluentValidation / MVC validation.
7. Keep Clean Architecture layering:
   - Presentation (Controllers / Views / ViewModels / wwwroot JS) → Application → Domain ⇐ Infrastructure.
   - Infrastructure implements interfaces from Domain/Application; it is never referenced by Presentation.
8. All file I/O goes through `IFileStorageService`.
9. All AJAX responses should be standardised JSON (`{ success, data, error }`) or `BaseResponseDto<T>`.
10. All JavaScript global helpers belong on `window.SohbaApp` (single namespace) OR as `window.fn` —
    never mix both styles in the same HTML attribute.
11. No inline `<script>` blocks should remain in views (per the project's own `RULES.md §2`),
    but where they already exist we only patch them if required.

<br>
<br>

---

<br>

# Issue 1 — Newly Created Comment/Reply Missing Current User Name

## Issue

When creating a new comment or reply, the backend successfully creates it, but the returned
`CommentResponseDto` does not contain the current authenticated user's name correctly.

Existing comments contain the author's name, but the newly created comment/reply returned
immediately after creation does not.

The expected response must contain the correct:

```text
UserId
UserName
```

for the user who actually created the comment/reply.

The backend must use:

```csharp
var userId = GetCurrentUserId();
```

and must NOT trust `request.UserId` from the client.

The fix should allow the frontend to immediately display the current user's name/avatar for
the newly created comment or reply without another request.

This is blocking testing of:

```text
Issue 3.5 - Reply / Delete Comment
```

## Related Feature

- **Feature Name:** Post Details / Comments — Create Comment & Reply.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.5 (Comments — Reply & Delete).

## Expected Behaviour

- When the current user submits a new comment or reply, the returned `CommentResponseDto`
  must contain:
  - `UserId` = the authenticated user's id (from `GetCurrentUserId()`).
  - `UserName` = the authenticated user's display name.
  - `IsAuthor` = `true` for the current user.
- The frontend can immediately render the current user's name/avatar for the newly created
  comment or reply without another request.
- The Delete button (shown when `isAuthor` is true) appears immediately for the current
  user's own new comment/reply.

## Current Behaviour

- The comment/reply is created in the database correctly.
- The returned `CommentResponseDto` has `IsAuthor = false` and `UserId = Guid.Empty` for the
  newly created comment/reply.
- The frontend cannot correctly identify the new comment as belonging to the current user,
  so the Delete button / author styling is not applied correctly.

## Root Cause

In `Sohba/Controllers/PostsController.cs`, the `Comment` action passes `request.UserId`
(from the client) instead of `userId` (from `GetCurrentUserId()`) when re-fetching the
comment list to return the newly created comment:

```csharp
var comments = await _interactionService.GetCommentsByPostIdAsync(request.PostId, request.UserId); // I Added Request.UserID To Avoid Run Errors
```

The frontend `submitComment` sends only `{ postId, content }` — it does NOT send a `UserId`.
Therefore `request.UserId` is `Guid.Empty` (the default). When `GetCommentsByPostIdAsync`
runs, it sets:

```csharp
comment.IsAuthor = comment.UserId == currentUserId;   // currentUserId = Guid.Empty
```

So `IsAuthor` is always `false` for the newly created comment, and the returned DTO's
`UserId`/`IsAuthor` do not reflect the authenticated user.

The `UserName` itself is mapped from `src.User.Name` (the repository includes `c.User`), so
the name is technically present. But the `UserId` and `IsAuthor` semantics are wrong, which
breaks the frontend's ability to correctly render the current user's comment (e.g., Delete
button visibility in Issue 3.5).

## Execution Flow

```
User submits a new comment
    → SohbaApp.submitComment()  [Sohba/wwwroot/js/sohba-posts.js]
        → POST /Posts/Comment { postId, content }        // NO UserId sent
            → PostsController.Comment
                → userId = GetCurrentUserId()            // correct authenticated id
                → AddCommentAsync(userId, postId, content, parentCommentId)
                    → creates Comment row with UserId = userId → OK
                → GetCommentsByPostIdAsync(request.PostId, request.UserId)
                    // request.UserId = Guid.Empty  ← BUG
                    → IsAuthor = (comment.UserId == Guid.Empty) → false
                → returns { success, comment = latest }
                    // latest.UserId = Guid.Empty, latest.IsAuthor = false
    → frontend renders comment with wrong author identity
```

## Related Files

- `Sohba/Controllers/PostsController.cs`
- `Sohba.Application/Services/InteractionService.cs`
- `Sohba.Application/DTOs/PostAggregate/CommentRequestDto.cs`
- `Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs`
- `Sohba.Application/Mappings/MappingProfile.cs`
- `Sohba.Domain/Entities/PostAggregate/Comment.cs`
- `Sohba.Infrastructure/Repositories/InteractionRepository.cs`
- `Sohba/wwwroot/js/sohba-posts.js`

## Affected Components

- Controller — `PostsController.cs` (Comment action)
- Application Service — `InteractionService.cs` (GetCommentsByPostIdAsync — no change needed)
- DTO — `CommentResponseDto` (already has UserId/UserName/IsAuthor — no change needed)
- JavaScript — `sohba-posts.js` (submitComment — no change needed)

## Files That Need Modification

1. `Sohba/Controllers/PostsController.cs`

## Implementation Plan

### Step 1 — Use the authenticated user id, not the client-supplied id

In the `Comment` action, change the call to `GetCommentsByPostIdAsync` so it passes `userId`
(the value from `GetCurrentUserId()`) instead of `request.UserId`.

This is the ONLY change required for Issue 1. The rest of the pipeline
(`AddCommentAsync`, `GetCommentsByPostIdAsync`, `CommentResponseDto`, AutoMapper mapping,
repository `Include(c => c.User)`) already works correctly.

## Code Changes

### File: Sohba/Controllers/PostsController.cs

<div style="color:red"><b>REMOVE — the wrong call that trusts the client-supplied UserId:</b></div>

```csharp
            var comments = await _interactionService.GetCommentsByPostIdAsync(request.PostId, request.UserId); // I Added Request.UserID To Avoid Run Errors
```

<div style="color:green"><b>REPLACE WITH — use the authenticated user id from GetCurrentUserId():</b></div>

```csharp
            var comments = await _interactionService.GetCommentsByPostIdAsync(request.PostId, userId);
```

**Change type:** REPLACE (1 line).

**Context — the full `Comment` action after the fix:**

```csharp
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

            var comments = await _interactionService.GetCommentsByPostIdAsync(request.PostId, userId);
            //var latest = comments.FirstOrDefault(c => c.ParentCommentId == request.ParentCommentId) ?? comments.First();
            CommentResponseDto latest;
             if (request.ParentCommentId.HasValue)
             {
                   latest = comments
                             .SelectMany(c => c.Replies)
                             .Where(r => r.ParentCommentId == request.ParentCommentId)
                             .OrderByDescending(r => r.CreatedAt)
                             .FirstOrDefault();
             }
             else
                 {
                latest = comments.FirstOrDefault(); 
                 }
            
             if (latest == null)
                return Json(new { success = false, error = "Comment created but could not be retrieved." });


            return Json(new
            {
                success = true,
                comment = latest
            });
        }
```

## Regression Testing

- **Test Users:**
    - `mohammed@sohba.com` (post author).
    - `ahmed@sohba.com` (comment author).
- **Navigation:**
    - Login as Mohammed → Home feed → open a post → type a new comment → submit.
    - Open the same post → click Reply under an existing comment → submit a reply.
- **Expected Results:**
    - The newly created comment/reply appears immediately with Mohammed's name and avatar.
    - The Delete button is visible on Mohammed's own new comment/reply (because
      `isAuthor` is now `true`).
    - The network response for `POST /Posts/Comment` contains `comment.userId` = Mohammed's
      id and `comment.isAuthor` = `true`.
- **Failure Conditions:**
    - If `comment.isAuthor` is still `false` or `comment.userId` is `Guid.Empty`, the fix was
      not applied (the call still passes `request.UserId`).
- **Edge Cases:**
    - Creating a top-level comment (no `ParentCommentId`).
    - Creating a reply (with `ParentCommentId`).
    - Creating a comment when the user is not authenticated (should return 401).

<br>
<br>

---

<br>

# Issue 2 — Duplicate Key Exception in MapPostsToResponse

## Issue

The application throws:

```text
System.ArgumentException:
An item with the same key has already been added.
```

The exception occurs in:

```text
InteractionService.MapPostsToResponse
```

The relevant code is:

```csharp
var reactionDict = userReactions.ToDictionary(
    r => r.PostId,
    r => r.Type.ToString());

var savedDict = userSavedPosts.ToDictionary(
    s => s.PostId,
    s => s.Tag);
```

This appeared while testing:

```text
Issue 3.10 - Save Post / Add To Favorites
```

Specifically:

1. Save a post to a collection.
2. Add the SAME post to Favorites.
3. The application throws the duplicate-key exception.

## Related Feature

- **Feature Name:** Post Actions — Save Post / Favorites.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.10 (Save & Favorites).

## Expected Behaviour

The following must work independently, without throwing an exception:

```text
Save to collection
+
Add to Favorites
+
Remove from Favorites independently
```

- A post can be saved to a collection AND added to Favorites at the same time.
- Removing the post from Favorites must NOT remove it from the collection.
- The post card must show:
  - `IsSaved = true` if the post is in ANY collection (including Favorites).
  - `IsFavorite = true` if the post is in the Favorites collection.
  - `SavedTag = "Favorite"` if favorited, otherwise the first collection tag.

## Current Behaviour

- Saving a post to a collection creates a `SavedPost` row with `CollectionId` set.
- Adding the SAME post to Favorites creates a SECOND `SavedPost` row with the Favorites
  `CollectionId` and `Tag = Favorite`.
- `GetSavedPostsByUserAsync(userId)` returns BOTH rows (same `PostId`, different
  `CollectionId`).
- `MapPostsToResponse` calls `userSavedPosts.ToDictionary(s => s.PostId, ...)` which throws
  `ArgumentException: An item with the same key has already been added.` because two rows
  share the same `PostId`.

## Root Cause

The duplicate `PostId` is **EXPECTED and CORRECT** — it is the intended behavior of the new
collection model introduced in `FixesV1.md`. A post can legitimately exist in multiple
collections (e.g., a named collection AND Favorites). The save logic
(`SavePostToCollectionAsync` / `SavePostToFavoritesAsync`) correctly creates separate
`SavedPost` rows per collection, each with its own `CollectionId`.

The bug is in `MapPostsToResponse`: it uses `ToDictionary(s => s.PostId, ...)` which assumes
each `PostId` appears at most once. This assumption was valid in the OLD model (one
`SavedPost` row per user+post) but is now invalid.

**This is NOT a data corruption issue.** The duplicate rows are intentional. The fix must
NOT use `GroupBy`, `Distinct`, or arbitrary duplicate handling to hide the exception. It must
correctly aggregate the multiple saved states per post.

## Execution Flow

```
User saves a post to a collection
    → SohbaApp.saveToCollection(postId, collectionId)
        → POST /Posts/SaveToCollection { postId, collectionId }
            → SavePostToCollectionAsync
                → creates SavedPost { PostId, CollectionId = collectionId, Tag = General }
                → OK

User adds the SAME post to Favorites
    → SohbaApp.addToFavorites(postId)
        → POST /Posts/ToggleFavorite { postId }
            → SavePostToFavoritesAsync
                → creates SavedPost { PostId, CollectionId = favorites.Id, Tag = Favorite }
                → OK

User opens the Home feed (or SavedPosts / Favorites page)
    → GET /Home/Index (or /Posts/SavedPosts)
        → PostService / InteractionService
            → MapPostsToResponse(posts, userId)
                → userSavedPosts = GetSavedPostsByUserAsync(userId)
                    → returns 2 rows with the SAME PostId (different CollectionId)
                → savedDict = userSavedPosts.ToDictionary(s => s.PostId, s => s.Tag)
                    → System.ArgumentException: An item with the same key has already been added.
```

## Related Files

- `Sohba.Application/Services/InteractionService.cs`
- `Sohba.Application/DTOs/PostAggregate/PostResponseDto.cs`
- `Sohba.Domain/Entities/PostAggregate/SavedPost.cs`
- `Sohba.Domain/Enums/SavedTag.cs`
- `Sohba.Infrastructure/Repositories/InteractionRepository.cs`
- `Sohba/Controllers/PostsController.cs`

## Affected Components

- Application Service — `InteractionService.cs` (MapPostsToResponse)
- DTO — `PostResponseDto` (already has IsSaved/IsFavorite/SavedTag — no change needed)
- Domain Entity — `SavedPost` (already supports multiple collections — no change needed)
- Repository — `InteractionRepository.cs` (GetSavedPostsByUserAsync — no change needed)

## Files That Need Modification

1. `Sohba.Application/Services/InteractionService.cs`

## Implementation Plan

### Step 1 — Aggregate the multiple saved states per post

In `MapPostsToResponse`, replace the `ToDictionary` with a `GroupBy` that collects all tags
per `PostId`. This preserves the intended behavior (a post can be in multiple collections)
without throwing.

### Step 2 — Update the projection logic

Update the `postList.Select(...)` projection so that:

- `IsSaved` = `true` if the post is in ANY collection (i.e., the group exists).
- `IsFavorite` = `true` if any of the post's saved rows has `Tag == Favorite`.
- `SavedTag` = `"Favorite"` if favorited, otherwise the first tag in the group.

This preserves the intended behavior:

```text
Save to collection
+
Add to Favorites
+
Remove from Favorites independently
```

without throwing an exception.

## Code Changes

### File: Sohba.Application/Services/InteractionService.cs

<div style="color:red"><b>REMOVE — the ToDictionary that throws on duplicate PostId:</b></div>

```csharp
            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            var savedDict = userSavedPosts.ToDictionary(s => s.PostId, s => s.Tag);

            return postList.Select(p => {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;
                dto.IsSaved = savedDict.ContainsKey(p.Id);

                if (savedDict.TryGetValue(p.Id, out var tag))
                {
                    dto.SavedTag = tag.ToString(); 
                    dto.IsFavorite = tag == SavedTag.Favorite;
                }
                dto.CurrentUserReaction = reactionDict.GetValueOrDefault(p.Id);
                return dto;
            });
```

<div style="color:green"><b>REPLACE WITH — group by PostId and aggregate the saved states:</b></div>

```csharp
            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            // A post can be saved to multiple collections (e.g. a named collection AND Favorites).
            // Group by PostId and collect all tags so we don't throw on duplicate keys.
            var savedDict = userSavedPosts
                .GroupBy(s => s.PostId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Tag).ToList());

            return postList.Select(p => {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;
                dto.IsSaved = savedDict.ContainsKey(p.Id);

                if (savedDict.TryGetValue(p.Id, out var tags))
                {
                    dto.IsFavorite = tags.Contains(SavedTag.Favorite);
                    dto.SavedTag = dto.IsFavorite ? SavedTag.Favorite.ToString() : tags.First().ToString();
                }
                dto.CurrentUserReaction = reactionDict.GetValueOrDefault(p.Id);
                return dto;
            });
```

**Change type:** REPLACE (the `savedDict` declaration + the `postList.Select` projection).

**Context — the full `MapPostsToResponse` method after the fix:**

```csharp
        // Helper method to fill interaction data (Likes, Comments, ..etc)
        private async Task<IEnumerable<PostResponseDto>> MapPostsToResponse(IEnumerable<Post> posts, Guid userId)
        {
            var postList = posts.ToList();
            if (!postList.Any()) return new List<PostResponseDto>();

            var ids = postList.Select(p => p.Id).ToList();
            var counts = await _unitOfWork.Posts.GetPostsCountsAsync(ids);
            var userReactions = await _unitOfWork.Interactions.GetUserReactionsForPostsAsync(userId, ids);
            var userSavedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId);

            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            // A post can be saved to multiple collections (e.g. a named collection AND Favorites).
            // Group by PostId and collect all tags so we don't throw on duplicate keys.
            var savedDict = userSavedPosts
                .GroupBy(s => s.PostId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Tag).ToList());

            return postList.Select(p => {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;
                dto.IsSaved = savedDict.ContainsKey(p.Id);

                if (savedDict.TryGetValue(p.Id, out var tags))
                {
                    dto.IsFavorite = tags.Contains(SavedTag.Favorite);
                    dto.SavedTag = dto.IsFavorite ? SavedTag.Favorite.ToString() : tags.First().ToString();
                }
                dto.CurrentUserReaction = reactionDict.GetValueOrDefault(p.Id);
                return dto;
            });
        }
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:**
    1. Login as Mohammed → Home feed → open a post → Save it to a named collection.
    2. Add the SAME post to Favorites.
    3. Open the Home feed (or `/Posts/SavedPosts`).
    4. Remove the post from Favorites only.
    5. Open the Home feed again.
- **Expected Results:**
    - No `System.ArgumentException` is thrown.
    - The post card shows `IsSaved = true` (it is in the collection).
    - The post card shows `IsFavorite = true` while it is in Favorites.
    - After removing from Favorites, the post card shows `IsSaved = true` but
      `IsFavorite = false` (it is still in the collection).
    - The post still appears on `/Posts/SavedPosts` (in the collection) after being removed
      from Favorites.
- **Failure Conditions:**
    - If the exception still occurs, the `GroupBy` fix was not applied.
    - If `IsSaved` becomes `false` after removing from Favorites (when it should stay `true`
      because it is still in a collection), the projection logic is wrong.
- **Edge Cases:**
    - A post saved to multiple named collections (more than 2 rows with the same `PostId`).
    - A post saved to a collection only (no Favorites) — `IsFavorite` must be `false`.
    - A post in Favorites only (no named collection) — `IsSaved` must be `true`,
      `IsFavorite` must be `true`.
    - A post with no saved rows at all — `IsSaved` must be `false`, `IsFavorite` must be
      `false`.

<br>
<br>

---

<br>

# Issue 3 — Duplicate Key Exception in MapPostsWithInteractions

## Issue

The same duplicate-key exception still occurs at runtime, but in a different location:

```text
System.ArgumentException:
An item with the same key has already been added.
```

The exception occurs in:

```text
Sohba.Application.Services.PostService.MapPostsWithInteractions
```

The relevant code is:

```csharp
var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
var savedDict = userSavedPosts.ToDictionary(s => s.PostId, s => s.Tag);
```

This is the SECOND location with the same saved-post handling logic. The first fix
(Issue 2) was applied to `InteractionService.MapPostsToResponse`, but this duplicated flow
in `PostService.MapPostsWithInteractions` is still blocking `dotnet run`.

## Related Feature

- **Feature Name:** Post Actions — Save Post / Favorites; Post Feed / Timeline.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.10 (Save & Favorites) and
  the Home Feed / Timeline.

## Expected Behaviour

- The Home feed (timeline), group posts, page posts, and user posts must load without
  throwing `System.ArgumentException`.
- A post can be saved to a collection AND added to Favorites at the same time.
- Removing the post from Favorites must NOT remove it from the collection.
- The post card must show:
  - `IsSaved = true` if the post is in ANY collection (including Favorites).
  - `IsFavorite = true` if the post is in the Favorites collection.
  - `IsAuthor = true` if the post belongs to the current user.

## Current Behaviour

- `PostService.MapPostsWithInteractions` calls
  `GetSavedPostsByUserAsync(currentUserId)` which now returns multiple `SavedPost` rows with
  the same `PostId` (one per collection).
- It then calls `userSavedPosts.ToDictionary(s => s.PostId, s => s.Tag)`.
- Because two rows share the same `PostId`, `ToDictionary` throws
  `ArgumentException: An item with the same key has already been added.`
- This blocks `dotnet run` because the Home feed / timeline calls
  `MapPostsWithInteractions` via `GetFeedAsync`.

## Root Cause

The root cause is **identical** to Issue 2: the duplicate `PostId` is **EXPECTED and
CORRECT** — it is the intended behavior of the new collection model. A post can legitimately
exist in multiple collections (e.g., a named collection AND Favorites). 

`PostService.MapPostsWithInteractions` still uses the OLD assumption that each `PostId`
appears at most once in `userSavedPosts`. It calls `ToDictionary(s => s.PostId, ...)` which
throws on the duplicate key. This method was missed by the Issue 2 fix because it is a
separate, duplicated implementation in `PostService` (it does NOT call
`InteractionService.MapPostsToResponse`).

**This is NOT a data corruption issue and NOT a repository/query bug.** The repository
`GetSavedPostsByUserAsync` correctly returns all saved rows (one per collection). The bug is
purely in this method's in-memory aggregation logic.

## Execution Flow

```
User saves a post to a collection
    → SavePostToCollectionAsync → SavedPost { PostId, CollectionId, Tag = General } → OK

User adds the SAME post to Favorites
    → SavePostToFavoritesAsync → SavedPost { PostId, CollectionId = Favorites, Tag = Favorite } → OK

User opens the Home feed (timeline)
    → GET /Home/Index
        → PostService.GetFeedAsync(userId, page, pageSize)
            → MapPostsWithInteractions(posts, userId)
                → userSavedPosts = GetSavedPostsByUserAsync(userId)
                    → returns 2 rows with the SAME PostId (different CollectionId)
                → savedDict = userSavedPosts.ToDictionary(s => s.PostId, s => s.Tag)
                    → System.ArgumentException: An item with the same key has already been added.
```

## Why This Method Was Missed By The Issue 2 Fix

`InteractionService.MapPostsToResponse` and `PostService.MapPostsWithInteractions` are two
**separate, duplicated** implementations of the same post-mapping logic. They do NOT share a
common helper. The Issue 2 fix only modified `InteractionService.MapPostsToResponse`. The
`PostService` copy was left unchanged, so the identical bug remains there.

## Fix Approach Recommendation

**Recommended: Update this method only (Option A).**

Reasons:

1. The fix is already proven — the `InteractionService.MapPostsToResponse` version (Issue 2)
   now works.
2. It is a contained change in one method — no risk of breaking other callers.
3. No repository/query change is needed — the repository correctly returns all saved rows.
4. A shared refactoring (Option B) is architecturally cleaner but is a larger change; it can
   be done later as a follow-up. It is NOT required to unblock `dotnet run`.

**Why not fix the repository/query (Option C)?** The repository `GetSavedPostsByUserAsync`
correctly returns all saved rows per collection. Changing it to return unique posts would
break the intended "save to collection + add to Favorites independently" behavior. The bug is
in the in-memory aggregation, not the query.

## Related Files

- `Sohba.Application/Services/PostService.cs`
- `Sohba.Application/Services/InteractionService.cs` (already fixed — reference only)
- `Sohba.Application/DTOs/PostAggregate/PostResponseDto.cs`
- `Sohba.Domain/Entities/PostAggregate/SavedPost.cs`
- `Sohba.Domain/Enums/SavedTag.cs`
- `Sohba.Infrastructure/Repositories/InteractionRepository.cs`

## Affected Components

- Application Service — `PostService.cs` (MapPostsWithInteractions)
- Application Service — `InteractionService.cs` (already fixed — reference only)
- DTO — `PostResponseDto` (already has IsSaved/IsFavorite/SavedTag — no change needed)
- Domain Entity — `SavedPost` (already supports multiple collections — no change needed)
- Repository — `InteractionRepository.cs` (GetSavedPostsByUserAsync — no change needed)

## Files That Need Modification

1. `Sohba.Application/Services/PostService.cs`

## Implementation Plan

### Step 1 — Aggregate the multiple saved states per post

In `MapPostsWithInteractions`, replace the `ToDictionary` with a `GroupBy` that collects all
tags per `PostId`. This matches the already-fixed `InteractionService.MapPostsToResponse`.

### Step 2 — Update the projection logic

Update the `postList.Select(...)` projection so that:

- `IsSaved` = `true` if the post is in ANY collection (i.e., the group exists).
- `IsFavorite` = `true` if any of the post's saved rows has `Tag == Favorite`.
- `IsAuthor` = `true` if the post belongs to the current user (unchanged).

This preserves the intended behavior:

```text
Save to collection
+
Add to Favorites
+
Remove from Favorites independently
```

without throwing an exception.

## Code Changes

### File: Sohba.Application/Services/PostService.cs

<div style="color:red"><b>REMOVE — the ToDictionary that throws on duplicate PostId:</b></div>

```csharp
            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            var savedDict = userSavedPosts.ToDictionary(s => s.PostId, s => s.Tag);

            var response = postList.Select(p =>
            {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;
                dto.IsSaved = savedDict.ContainsKey(p.Id);
                dto.IsFavorite = savedDict.TryGetValue(p.Id, out var tag) && tag == SavedTag.Favorite;
                dto.IsAuthor = p.UserId == currentUserId;
                if (reactionDict.TryGetValue(p.Id, out var reaction))
                    dto.CurrentUserReaction = reaction;

                return dto;
            }).ToList();
```

<div style="color:green"><b>REPLACE WITH — group by PostId and aggregate the saved states:</b></div>

```csharp
            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            // A post can be saved to multiple collections (e.g. a named collection AND Favorites).
            // Group by PostId and collect all tags so we don't throw on duplicate keys.
            var savedDict = userSavedPosts
                .GroupBy(s => s.PostId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Tag).ToList());

            var response = postList.Select(p =>
            {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;
                dto.IsSaved = savedDict.ContainsKey(p.Id);

                if (savedDict.TryGetValue(p.Id, out var tags))
                {
                    dto.IsFavorite = tags.Contains(SavedTag.Favorite);
                    dto.SavedTag = dto.IsFavorite ? SavedTag.Favorite.ToString() : tags.First().ToString();
                }
                dto.IsAuthor = p.UserId == currentUserId;
                if (reactionDict.TryGetValue(p.Id, out var reaction))
                    dto.CurrentUserReaction = reaction;

                return dto;
            }).ToList();
```

**Change type:** REPLACE (the `savedDict` declaration + the `postList.Select` projection).

**Context — the full `MapPostsWithInteractions` method (from the saved-post query onward)
after the fix:**

```csharp
            var ids = postList.Select(p => p.Id).ToList();
            var counts = await _unitOfWork.Posts.GetPostsCountsAsync(ids);
            var userReactions = await _unitOfWork.Interactions.GetUserReactionsForPostsAsync(currentUserId, ids);
            var userSavedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(currentUserId);

            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            // A post can be saved to multiple collections (e.g. a named collection AND Favorites).
            // Group by PostId and collect all tags so we don't throw on duplicate keys.
            var savedDict = userSavedPosts
                .GroupBy(s => s.PostId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Tag).ToList());

            var response = postList.Select(p =>
            {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;
                dto.IsSaved = savedDict.ContainsKey(p.Id);

                if (savedDict.TryGetValue(p.Id, out var tags))
                {
                    dto.IsFavorite = tags.Contains(SavedTag.Favorite);
                    dto.SavedTag = dto.IsFavorite ? SavedTag.Favorite.ToString() : tags.First().ToString();
                }
                dto.IsAuthor = p.UserId == currentUserId;
                if (reactionDict.TryGetValue(p.Id, out var reaction))
                    dto.CurrentUserReaction = reaction;

                return dto;
            }).ToList();

            return Result<IEnumerable<PostResponseDto>>.Success(response);
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:**
    1. Login as Mohammed → Home feed → open a post → Save it to a named collection.
    2. Add the SAME post to Favorites.
    3. Open the Home feed (timeline).
    4. Open a group page with posts.
    5. Open a user profile with posts.
- **Expected Results:**
    - `dotnet run` starts without `System.ArgumentException`.
    - The Home feed / group / profile pages load without error.
    - The post card shows `IsSaved = true` (it is in the collection).
    - The post card shows `IsFavorite = true` while it is in Favorites.
    - After removing from Favorites, the post card shows `IsSaved = true` but
      `IsFavorite = false` (it is still in the collection).
- **Failure Conditions:**
    - If the exception still occurs, the `GroupBy` fix was not applied to
      `PostService.MapPostsWithInteractions`.
    - If `IsSaved` becomes `false` after removing from Favorites (when it should stay `true`
      because it is still in a collection), the projection logic is wrong.
- **Edge Cases:**
    - A post saved to multiple named collections (more than 2 rows with the same `PostId`).
    - A post saved to a collection only (no Favorites) — `IsFavorite` must be `false`.
    - A post in Favorites only (no named collection) — `IsSaved` must be `true`,
      `IsFavorite` must be `true`.
    - A post with no saved rows at all — `IsSaved` must be `false`, `IsFavorite` must be
      `false`.
    - `GetAllPostsAsync` calls `MapPostsWithInteractions(posts, Guid.Empty)` — with
      `currentUserId = Guid.Empty`, `GetSavedPostsByUserAsync(Guid.Empty)` returns no rows,
      so `savedDict` is empty; the projection must not throw (it won't, because the group is
      empty and `ContainsKey` returns `false`).

<br>
<br>

---

<br>

# Appendix — Full File Inventory

| Layer | Path |
|-------|------|
| Controller | `Sohba/Controllers/PostsController.cs` |
| Application Service | `Sohba.Application/Services/InteractionService.cs` |
| Application Service | `Sohba.Application/Services/PostService.cs` |
| Application DTO | `Sohba.Application/DTOs/PostAggregate/CommentRequestDto.cs` |
| Application DTO | `Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs` |
| Application DTO | `Sohba.Application/DTOs/PostAggregate/PostResponseDto.cs` |
| Application Mapping | `Sohba.Application/Mappings/MappingProfile.cs` |
| Domain Entity | `Sohba.Domain/Entities/PostAggregate/Comment.cs` |
| Domain Entity | `Sohba.Domain/Entities/PostAggregate/SavedPost.cs` |
| Domain Enum | `Sohba.Domain/Enums/SavedTag.cs` |
| Infrastructure Repository | `Sohba.Infrastructure/Repositories/InteractionRepository.cs` |
| JS | `Sohba/wwwroot/js/sohba-posts.js` |

<br>
<br>

---

<br>

# Additional Notes

1. **Issue 1 is a one-line fix.** The only change is in
   `Sohba/Controllers/PostsController.cs` — pass `userId` instead of `request.UserId` to
   `GetCommentsByPostIdAsync`. No other file needs modification.

2. **Issue 2 is a two-part fix in one method.** The only change is in
   `Sohba.Application/Services/InteractionService.cs` — replace the `ToDictionary` with a
   `GroupBy` and update the projection logic. No other file needs modification.

3. **The duplicate `PostId` is expected.** It is the intended behavior of the new collection
   model. Do NOT use `Distinct()` or arbitrary duplicate handling to hide the exception.

4. **No migration is required** for any of the three fixes. All fixes are pure C# logic
   changes.

5. **The `CommentRequestDto.UserId` property** is now effectively unused by the backend
   (the controller uses `GetCurrentUserId()`). It can remain for backwards compatibility but
   must NOT be trusted as the source of truth for the authenticated user.

6. **Issue 3 is the same bug as Issue 2, in a second duplicated method.**
   `InteractionService.MapPostsToResponse` and `PostService.MapPostsWithInteractions` are two
   separate implementations of the same post-mapping logic. Both must apply the same
   `GroupBy` fix. A future enhancement could extract a shared helper to avoid this
   duplication, but that is NOT required to unblock `dotnet run`.

7. **The repository/query does NOT need fixing.** `GetSavedPostsByUserAsync` correctly
   returns all saved rows per collection. The bug is purely in the in-memory
   `ToDictionary`/`GroupBy` logic in the two mapping methods.

<br>
<br>

---

<br>

# End Of Document

This document is a complete implementation guide for the three blocking issues. No project
source files were modified while producing it.
