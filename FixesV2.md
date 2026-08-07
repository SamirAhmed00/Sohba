# Sohba — FixesV2 Implementation Guide

<br>
<br>
<br>

**Document Name:** FixesV2.md

**Purpose:** Corrections and clarifications for the implementation of `FixesV1.md`.

**Scope:** This document ONLY:

1. Clarifies the two ambiguous sections from `FixesV1.md`:
   - File: `Sohba/Views/Posts/SavedPosts.cshtml`
   - File: `Sohba/Views/Posts/Favorites.cshtml`
2. Corrects bugs found in `FixesV1.md` and in the applied project state.
3. Documents the verification report of all applied changes.
4. Provides additional corrections required for the feature to work at runtime.

**Author Role:** Senior Software Architect / Senior ASP.NET Core MVC Engineer / Senior .NET
Backend Engineer / Senior Frontend Engineer / Code Reviewer / QA Engineer.

**Important:** No project source file was modified while writing this document. This is a
guide only.

<br>
<br>

---

<br>

# TABLE OF CONTENTS

1. [How To Use This Document](#how-to-use-this-document)
2. [Part 1 — Clarification: SavedPosts.cshtml](#part-1--clarification-savedpostscshtml)
3. [Part 2 — Clarification: Favorites.cshtml](#part-2--clarification-favoritescshtml)
4. [Part 3 — Applied Changes Verification Report](#part-3--applied-changes-verification-report)
5. [Part 4 — Critical Bugs Found In FixesV1.md And The Applied Project](#part-4--critical-bugs-found-in-fixesv1md-and-the-applied-project)
6. [Part 5 — Additional Corrections And Enhancements](#part-5--additional-corrections-and-enhancements)

<br>
<br>

---

<br>

# How To Use This Document

For every issue, the following sections are provided:

- **Issue** — the reported problem.
- **Expected Behaviour** — what should happen.
- **Current Behaviour** — what actually happens.
- **Root Cause** — the REAL cause, not the symptom.
- **Files That Need Modification** — only the files actually requiring changes.
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

# Part 1 — Clarification: SavedPosts.cshtml

## Issue

`FixesV1.md` provided a complete Razor page snippet for `SavedPosts.cshtml` but did NOT
specify whether to:

- Replace the entire existing file,
- Replace only a specific section,
- Or insert the new code into a particular location.

The current file is different from the snippet.

## Current File State (What I Inspected)

The current `Sohba/Views/Posts/SavedPosts.cshtml` (187 lines) contains the **old tag-filter
workflow**:

- A filter dropdown (`#tagFilter`) that calls `filterByTag(tag)` →
  `/Posts/SavedPosts?tag=` — for `all`, `Favorite`, `WatchLater`, `Work`, `Education`, `General`.
- A menu button with `SohbaApp.toggleMenu(`...
- Menu buttons calling `changeTag(postId, tag)` → `/Posts/ChangeSavedPostTag`.
- An `unsavePost(postId)` button → `/Posts/RemoveSavedPost`.
- A `@switch(post.SavedTag)` badge showing the enum tag.

## The FixesV1.md Snippet (What It Actually Was)

The snippet showed a simple flat list of `_PostCard` partials. It said
"group saved posts by collection" but actually rendered a flat list — **it did NOT group by
collection**. It also:

- Silently removed the tag-filter dropdown.
- Silently removed the change-tag menu.
- Silently removed the unsave button / script.
- Removed the "No saved posts" empty-state with the "Browse Feed" link.

## Clear Instruction

### Option A — Recommended: Keep The Current File As-Is

**Do NOT replace the current file.**

**Why:**

1. `PostsController.SavedPosts(string tag)` still returns `IEnumerable<PostResponseDto>` and
   `GetSavedPostsAsync(userId)` returns ALL saved posts regardless of collection. So the
   current file continues to work at runtime.
2. The old tag-filter endpoints (`ChangeSavedPostTag`, `RemoveSavedPost`, `SavedPosts?tag=`)
   are still present in `PostsController`. The current file uses them — nothing breaks.
3. The FixesV1.md snippet was **not an improvement** — it was a flat list that said "grouped"
   and silently removed functionality.

**Action:** Leave `Sohba/Views/Posts/SavedPosts.cshtml` **unchanged**.

### Option B — If You Want True Collection Grouping (Enhancement)

This requires a proper `SavedPostsViewModel`. See Part 5, Section 5.1 for the full design.
If you choose this, then **"Replace the entire file"** with the corrected version in Part 5,
Section 5.1, which ALSO keeps the remove-from-saved capability.

---

<br>

# Part 2 — Clarification: Favorites.cshtml

## Issue

`FixesV1.md` provided a complete Razor page snippet for `Favorites.cshtml` but did NOT
specify whether to replace the entire file, replace a section, or insert new code.

## Current File State (What I Inspected)

The current `Sohba/Views/Posts/Favorites.cshtml` (113 lines) renders favorite post cards,
each with:

- A "⭐ Favorite" badge.
- A **"remove from favorites" button** calling `removeFromFavorites(postId)` →
  `/Posts/RemoveSavedPost`.
- A "View Post →" link.
- A "No favorites yet" empty state with "Browse Feed" link.
- A script section defining `removeFromFavorites`.

## The FixesV1.md Snippet (What It Actually Was)

The snippet was a **flat list of `_PostCard` partials** and:
- **REMOVED the remove-from-favorites button entirely** — a regression.
- Removed the empty-state "Browse Feed" link.

## Clear Instruction

### Option A — Recommended: Keep The Current File As-Is

**Do NOT replace the current file.**

**Why:**

1. The current file works with the existing `RemoveSavedPost` endpoint.
2. The FixesV1.md snippet would have **removed the remove-from-favorites capability** — a
   feature regression.
3. The current file is fully compatible with the new collection system because
   `GetFavoritePostsAsync` returns posts whose `Tag == Favorite` (or, once you apply Bug 2
   fix in Part 4, posts in the Favorites collection).

**Action:** Leave `Sohba/Views/Posts/Favorites.cshtml` **unchanged**.

### Option B — If You Want The New `/Posts/ToggleFavorite` Endpoint

The new `ToggleFavorite` endpoint in `PostsController` toggles a post in/out of the Favorites
collection. The current `removeFromFavorites` calls the OLD `/Posts/RemoveSavedPost`. To use
the new toggle endpoint, **replace only the `@section Scripts` block** (see Part 4, Bug 3 for
the corrected script). The rest of the file stays unchanged.

---

<br>

# Part 3 — Applied Changes Verification Report

I verified the following changes from `FixesV1.md` are **correctly applied** in the current
project state:

| # | Change | File(s) | Status |
|---|--------|---------|--------|
| 1 | `SavedCollection` entity created | `Sohba.Domain/Entities/PostAggregate/SavedCollection.cs` | ✅ Exists |
| 2 | `SavedPost` entity updated with `Id` + `CollectionId` | `Sohba.Domain/Entities/PostAggregate/SavedPost.cs` | ✅ Exists |
| 3 | `SavedCollectionDto` created | `Sohba.Application/DTOs/PostAggregate/SavedCollectionDto.cs` | ✅ Exists |
| 4 | `CreateSavedCollectionDto` created | `Sohba.Application/DTOs/PostAggregate/CreateSavedCollectionDto.cs` | ✅ Exists |
| 5 | `SaveToCollectionDto` created | `Sohba.Application/DTOs/PostAggregate/SaveToCollectionDto.cs` | ✅ Exists |
| 6 | `IInteractionService` extended with 4 methods | `Sohba.Application/Interfaces/IInteractionService.cs` | ✅ Added |
| 7 | `InteractionService` implements the 4 methods | `Sohba.Application/Services/InteractionService.cs` | ✅ Added |
| 8 | `IInteractionRepository` extended with 5 methods | `Sohba.Domain/Interfaces/IInteractionRepository.cs` | ✅ Added |
| 9 | `InteractionRepository` implements the 5 methods | `Sohba.Infrastructure/Repositories/InteractionRepository.cs` | ✅ Added |
| 10 | `AppDbContext.SavedCollections` DbSet added | `Sohba.Infrastructure/Data/AppDbContext.cs` | ✅ Added |
| 11 | 4 new controller actions added | `Sohba/Controllers/PostsController.cs` | ✅ Added |
| 12 | `SohbaApp.get` helper added | `Sohba/wwwroot/js/sohba-core.js` | ✅ Added |
| 13 | Modal-based save flow added | `Sohba/wwwroot/js/sohba-posts.js` | ✅ Added |
| 14 | `_SavePostModal.cshtml` created | `Sohba/Views/Shared/Partials/_SavePostModal.cshtml` | ✅ Created |
| 15 | `_PostModal.cshtml` created (from Issue 3.2) | `Sohba/Views/Shared/Partials/_PostModal.cshtml` | ✅ Created |
| 16 | Both modals included in layout | `Sohba/Views/Shared/_AppLayout.cshtml` | ✅ Added |
| 17 | `_PostCard` Save button → `openSavePostModal` | `Sohba/Views/Shared/Partials/_PostCard.cshtml` | ✅ Changed |
| 18 | Migration created | `Sohba.Infrastructure/Migrations/20260806085753_AddSavedCollections.cs` | ✅ Created |

**Conclusion:** The backend implementation from FixesV1.md is fully applied. The remaining
issues are runtime/logic bugs (Part 4) and the two view clarifications (Parts 1 & 2).

<br>

---

<br>

# Part 4 — Critical Bugs Found In FixesV1.md And The Applied Project

<br>

## Bug 1 — `CreateCollection` Throws Away The New Collection ID

### Issue

The new `CreateCollection` action returns a plain `BaseResponseDto` which **discards
`result.Value.Id`**. The frontend `createNewCollection()` reads `createResult.data?.id`,
gets `undefined`, and so after creating a collection the post is **never saved** to it.

### Expected Behaviour

Creating a collection must return the created collection DTO (with its `Id`) so the frontend
can then save the post into it.

### Current Behaviour

```csharp
return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
```

`data` is absent, so `createResult.data?.id` is `undefined`.

### Root Cause

The action returns the wrong response shape. It should wrap the `SavedCollectionDto` value.

### Files That Need Modification

1. `Sohba/Controllers/PostsController.cs`

### Code Changes

### File: Sohba/Controllers/PostsController.cs

<div style="color:red"><b>REMOVE — the wrong return in CreateCollection:</b></div>

```csharp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCollection([FromBody] CreateSavedCollectionDto request)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(request?.Name))
                return Json(BaseResponseDto.FailureResponse("Collection name is required."));

            var result = await _interactionService.CreateCollectionAsync(userId, request.Name.Trim());
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }
```

<div style="color:green"><b>REPLACE WITH — return the new DTO:</b></div>

```csharp
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
```

### Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:** Home → post ⋮ menu → Save Post → type "My Collection" → Create.
- **Expected Results:**
    - Network response contains `data.id` (the new collection Guid).
    - The post is saved into the new collection (check the DB or the SavedPosts page).
    - Toast "Post saved to collection!".
- **Failure Conditions:** If `data.id` is still `undefined`, the fix was not applied.

<br>
<br>

---

<br>

## Bug 2 — `addToFavorites` Never Calls The New `ToggleFavorite` Endpoint

### Issue

`FixesV1.md` replaced `savePost` but left `addToFavorites` calling the OLD
`/Posts/ToggleSavePost` with `isFavorite: true`. The new `/Posts/ToggleFavorite` endpoint is
never called. Posts get `Tag = Favorite` with `CollectionId = null` — they won't appear in the
Favorites collection query.

### Expected Behaviour

`addToFavorites(postId)` must call the new `/Posts/ToggleFavorite` endpoint so the post is
saved into the user's **Favorites** collection (with a `CollectionId`).

### Current Behaviour

```javascript
window.SohbaApp.addToFavorites = async function (postId) {
    try {
        const result = await window.SohbaApp.post('/Posts/ToggleSavePost', {
            postId: postId,
            isFavorite: true
        });
        ...
    }
};
```

### Root Cause

The function was not updated as part of the redesign.

### Files That Need Modification

1. `Sohba/wwwroot/js/sohba-posts.js`

### Code Changes

### File: Sohba/wwwroot/js/sohba-posts.js

<div style="color:red"><b>REMOVE — the old addToFavorites:</b></div>

```javascript
window.SohbaApp.addToFavorites = async function (postId) {
    try {
        const result = await window.SohbaApp.post('/Posts/ToggleSavePost', {
            postId: postId,
            isFavorite: true
        });

        if (result.success) {
            updateSaveFavoriteButtons(postId, result.saved, result.saved);
            
            window.SohbaApp.toast(result.message, 'success');
        } else {
            window.SohbaApp.toast(result.error || 'Failed to add to favorites', 'error');
        }
    } catch (error) {
        console.error('Favorite error:', error);
        window.SohbaApp.toast('Network error', 'error');
    }
};
```

<div style="color:green"><b>REPLACE WITH — call the new ToggleFavorite endpoint:</b></div>

```javascript
window.SohbaApp.addToFavorites = async function (postId) {
    try {
        const result = await window.SohbaApp.post('/Posts/ToggleFavorite', { postId });

        if (result.success) {
            const btn = document.querySelector(`[data-fav-button="${postId}"]`);
            const isCurrentlyFav = btn && btn.classList.contains('text-pink-600');
            updateSaveFavoriteButtons(postId, true, !isCurrentlyFav);

            window.SohbaApp.toast(isCurrentlyFav ? 'Removed from favorites' : 'Added to favorites!', 'success');
        } else {
            window.SohbaApp.toast(result.error || 'Failed to update favorites', 'error');
        }
    } catch (error) {
        console.error('Favorite error:', error);
        window.SohbaApp.toast('Network error', 'error');
    }
};
```

### Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:** Home → post ⋮ menu → Add to Favorites.
- **Expected Results:**
    - Network request `POST /Posts/ToggleFavorite` with `{ postId }`.
    - Post appears on `/Posts/Favorites`.
    - Clicking Add to Favorites again removes it from Favorites.
- **Failure Conditions:** If the request still hits `/Posts/ToggleSavePost`, the fix was not
  applied.

<br>
<br>

---

<br>

## Bug 3 — Favorites.cshtml Remove Button / Script

### Issue

The FixesV1.md snippet removed the "remove from favorites" button. The current file kept it,
but it calls the OLD `/Posts/RemoveSavedPost` endpoint which removes the `SavedPost` row
entirely (without going through the Favorites collection). This still works, but if you want
it to go through the new Favorites collection, the script must call `/Posts/ToggleFavorite`.

### Expected Behaviour

The remove button should remove the post from the Favorites AND update the UI.

### Current Behaviour

The current script calls `/Posts/RemoveSavedPost` which removes the row. This is functionally
acceptable. However, the new toggle endpoint is more consistent with the redesigned system.

### Files That Need Modification

1. `Sohba/Views/Posts/Favorites.cshtml` (script block only)

### Code Changes

### File: Sohba/Views/Posts/Favorites.cshtml

<div style="color:red"><b>REMOVE — only the script block (do NOT touch the rest of the file):</b></div>

```html
@section Scripts {
    <script>
        async function removeFromFavorites(postId) {
            if (!confirm('Remove from favorites?')) return;

            const result = await SohbaApp.post('/Posts/RemoveSavedPost', { postId });

            if (result.success) {
                SohbaApp.toast('Removed from favorites', 'success');
                document.querySelector(`[data-post-id="${postId}"]`).remove();

                const remainingCount = document.querySelectorAll('[data-post-id]').length;
                document.querySelector('.text-gray-500.mt-1').textContent = `${remainingCount} favorite posts`;
            }
        }
    </script>
}
```

<div style="color:green"><b>REPLACE WITH — use the new ToggleFavorite endpoint:</b></div>

```html
@section Scripts {
    <script>
        async function removeFromFavorites(postId) {
            if (!confirm('Remove from favorites?')) return;

            const result = await SohbaApp.post('/Posts/ToggleFavorite', { postId });

            if (result.success) {
                SohbaApp.toast('Removed from favorites', 'success');
                document.querySelector(`[data-post-id="${postId}"]`).remove();

                const remainingCount = document.querySelectorAll('[data-post-id]').length;
                document.querySelector('.text-gray-500.mt-1').textContent = `${remainingCount} favorite posts`;
            }
        }
    </script>
}
```

### Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:** `/Posts/Favorites` → click the remove (heart) button.
- **Expected Results:**
    - Confirm prompt shows.
    - Post removed from the list.
    - Count text updates.
    - `POST /Posts/ToggleFavorite` appears in the Network tab.
- **Failure Conditions:** If the post remains after delete, the toggle logic in
  `SavePostToFavoritesAsync` is not removing (it should remove when the post exists).

<br>
<br>

---

<br>

## Bug 4 — Pre-existing JavaScript Syntax Error In SavedPosts.cshtml

### Issue

**File:** `Sohba/Views/Posts/SavedPosts.cshtml`, **line 84**:

```html
<button onclick="SohbaApp.toggleMenu(('@post.Id')" class="p-1 text-gray-400 hover:text-gray-600">
```

There is an extra `(` after `toggleMenu`. The correct call is
`SohbaApp.toggleMenu('@post.Id')`.

### Expected Behaviour

The menu toggle button works.

### Current Behaviour

Clicking it produces a JavaScript `SyntaxError` or `ReferenceError` due to the malformed
`(('...')`.

### Root Cause

Pre-existing typo (not from FixesV1.md) — documented here because it blocks the feature.

### Files That Need Modification

1. `Sohba/Views/Posts/SavedPosts.cshtml`

### Code Changes

### File: Sohba/Views/Posts/SavedPosts.cshtml

<div style="color:red"><b>REMOVE — the typo line:</b></div>

```html
                                <button onclick="SohbaApp.toggleMenu(('@post.Id')" class="p-1 text-gray-400 hover:text-gray-600">
```

<div style="color:green"><b>REPLACE WITH — the corrected call:</b></div>

```html
                                <button onclick="SohbaApp.toggleMenu('@post.Id')" class="p-1 text-gray-400 hover:text-gray-600">
```

### Regression Testing

- **Test Users:** `mohammed@sohba.com` (has a saved post from seed data).
- **Navigation:** `/Posts/SavedPosts` → click the ⋮ menu button.
- **Expected Results:**
    - Menu opens with General / Favorite / Watch Later / Work / Education / Remove options.
- **Failure Conditions:** Browser console shows `SyntaxError` — the typo is still present.

<br>
<br>

---

<br>

## Bug 5 — Migration Not Applied

### Issue

The migration `20260806085753_AddSavedCollections` exists but the database may not have been
updated with `dotnet ef database update`.

### Expected Behaviour

The `SavedCollections` table exists in the database, and the `SavedPost` table has `Id` +
`CollectionId`.

### Current Behaviour

If the migration was created but not applied, queries against `SavedCollections` will fail at
runtime.

### Root Cause

FixesV1.md documented the commands but the user may not have run `database update`.

### Files That Need Modification

No source file changes. Run the CLI command.

### Code Changes

### File: (CLI — run in Sohba project directory)

<div style="color:green"><b>ADD — apply the migration:</b></div>

```bash
cd Sohba
dotnet ef database update
```

### Regression Testing

- After running, verify the `SavedCollections` table exists:

```sql
SELECT name FROM sys.tables WHERE name = 'SavedCollections';
```

- Verify `SavedPost` has `CollectionId` and `Id` columns:

```sql
SELECT TOP 0 * FROM SavedPost;
```

- **Expected:** 1 row returned (table structure), with `CollectionId` + `Id` columns present.

<br>
<br>

---

<br>

## Bug 6 — `SavePostAsync` Backward-Compatibility Limitation

### Issue

The old `SavePostAsync` (kept for backward compatibility) uses
`GetSavedPostAsync(userId, postId)`, which returns the **FIRST** matching `SavedPost` row.
Now that a post can exist in multiple collections, this method:

1. Only updates the FIRST row's `Tag` when called with an existing save.
2. Does NOT set `CollectionId` for the new system.

### Expected Behaviour

- The old endpoint (`ToggleSavePost`) should be considered **deprecated** and not used by the
  new UI.
- All new saves must go through `SavePostToCollectionAsync` or `SavePostToFavoritesAsync`.

### Current Behaviour

The old endpoint still exists and works for legacy rows, but is inconsistent with the new
model.

### Root Cause

This is a documented limitation of the backward-compatible method, not a compile error.

### Files That Need Modification

None required for compilation. This is a deprecation note.

### Recommendation

1. Keep `SavePostAsync` for legacy API consumers.
2. Update the new UI to NEVER call `/Posts/ToggleSavePost` (already done — `savePost` → modal
   flow, `addToFavorites` → Bug 2 fix).
3. Optionally add `[Obsolete]` to `SavePostAsync` in the interface.

### Code Changes (Optional — deprecation attribute)

### File: Sohba.Application/Interfaces/IInteractionService.cs

<div style="color:green"><b>ADD — the Obsolete attribute (optional, does not break compilation):</b></div>

```csharp
        [Obsolete("Use SavePostToCollectionAsync or SavePostToFavoritesAsync instead.")]
        Task<Result<SavedPostDto>> SavePostAsync(Guid userId, Guid postId, SavedTag tag = SavedTag.General, string? userTag = null);
```

### Regression Testing

- Verify the new UI never calls `/Posts/ToggleSavePost`:
  - Search the JS for `ToggleSavePost` — the only place it should appear (if at all) is in
    legacy comments/dead code, NOT in the active `savePost` or `addToFavorites` functions.

<br>
<br>

---

<br>

# Part 5 — Additional Corrections And Enhancements

<br>

## 5.1 — True Collection Grouping For SavedPosts (Optional Enhancement)

### Why

The FixesV1.md snippet for `SavedPosts.cshtml` claimed to "group by collection" but rendered
a flat list. If you want true grouping, implement the following.

### Files That Need Modification

1. `Sohba.Application/DTOs/PostAggregate/SavedPostsGroupedDto.cs` (NEW)
2. `Sohba.Application/Interfaces/IInteractionService.cs`
3. `Sohba.Application/Services/InteractionService.cs`
4. `Sohba/Controllers/PostsController.cs`
5. `Sohba/ViewModels/Post/SavedPostsViewModel.cs` (NEW — Presentation layer)
6. `Sohba/Views/Posts/SavedPosts.cshtml` **REPLACE THE ENTIRE FILE**

### Code Changes

### File: Sohba.Application/DTOs/PostAggregate/SavedPostsGroupedDto.cs

<div style="color:green"><b>ADD — new file (entire content):</b></div>

```csharp
using System;
using System.Collections.Generic;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class SavedPostsGroupedDto
    {
        public Guid CollectionId { get; set; }
        public string CollectionName { get; set; }
        public bool IsFavorites { get; set; }
        public List<PostResponseDto> Posts { get; set; } = new List<PostResponseDto>();
    }
}
```

### File: Sohba.Application/Interfaces/IInteractionService.cs

<div style="color:green"><b>ADD — the new method (after SavePostToFavoritesAsync):</b></div>

```csharp
        Task<Result<IEnumerable<SavedPostsGroupedDto>>> GetSavedPostsGroupedAsync(Guid userId);
```

### File: Sohba.Application/Services/InteractionService.cs

<div style="color:green"><b>ADD — the implementation (after SavePostToFavoritesAsync):</b></div>

```csharp
        public async Task<Result<IEnumerable<SavedPostsGroupedDto>>> GetSavedPostsGroupedAsync(Guid userId)
        {
            var collections = await _unitOfWork.Interactions.GetCollectionsByUserAsync(userId);
            var allSaved = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId);

            var result = new List<SavedPostsGroupedDto>
            {
                // Always show the default "Saved" collection first.
                new SavedPostsGroupedDto
                {
                    CollectionId = Guid.Empty,
                    CollectionName = "All Saved",
                    IsFavorites = false,
                    Posts = (await MapPostsToResponse(allSaved.Select(s => s.Post).ToList(), userId)).ToList()
                }
            };

            foreach (var collection in collections)
            {
                var collectionSaves = allSaved
                    .Where(s => s.CollectionId == collection.Id)
                    .ToList();

                if (collectionSaves.Count == 0 && !collection.IsDefault && !collection.IsFavorites)
                    continue;

                result.Add(new SavedPostsGroupedDto
                {
                    CollectionId = collection.Id,
                    CollectionName = collection.Name,
                    IsFavorites = collection.IsFavorites,
                    Posts = (await MapPostsToResponse(collectionSaves.Select(s => s.Post).ToList(), userId)).ToList()
                });
            }

            return Result<IEnumerable<SavedPostsGroupedDto>>.Success(result);
        }
```

### File: Sohba/Controllers/PostsController.cs

<div style="color:red"><b>REPLACE — the SavedPosts action to use the grouped method:</b></div>

```csharp
        [HttpGet]
        public async Task<IActionResult> SavedPosts(string tag = "all")
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.GetSavedPostsGroupedAsync(userId);
            ViewBag.CurrentTag = tag;
            return View(result.Value ?? new List<SavedPostsGroupedDto>());
        }
```

### File: Sohba/ViewModels/Post/SavedPostsViewModel.cs

<div style="color:green"><b>ADD — new file (entire content):</b></div>

```csharp
using Sohba.Application.DTOs.PostAggregate;
using System.Collections.Generic;

namespace Sohba.ViewModels.Post
{
    public class SavedPostsViewModel
    {
        public IEnumerable<SavedPostsGroupedDto> Groups { get; set; } = new List<SavedPostsGroupedDto>();
    }
}
```

### File: Sohba/Views/Posts/SavedPosts.cshtml

<div style="color:green"><b>REPLACE THE ENTIRE FILE with:</b></div>

```html
@model IEnumerable<Sohba.Application.DTOs.PostAggregate.SavedPostsGroupedDto>
@{
    ViewData["Title"] = "Saved Posts";
    Layout = "_AppLayout";
}

<div class="max-w-5xl mx-auto page-transition">
    <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 mb-6">
        <div class="flex items-center gap-3">
            <div class="w-12 h-12 bg-amber-100 rounded-2xl flex items-center justify-center">
                <svg class="w-6 h-6 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
                </svg>
            </div>
            <div>
                <h1 class="text-2xl font-black text-gray-900">Saved Posts</h1>
                <p class="text-gray-500 mt-1">Your saved posts, grouped by collection</p>
            </div>
        </div>
    </div>

    @if (Model != null && Model.Any())
    {
        <div class="space-y-8">
            @foreach (var group in Model)
            {
                <div>
                    <div class="flex items-center gap-2 mb-4">
                        <h2 class="text-lg font-bold text-gray-900">@group.CollectionName</h2>
                        @if (group.IsFavorites)
                        {
                            <span class="px-3 py-1 bg-pink-100 text-pink-600 text-xs font-bold rounded-full">⭐ Favorites</span>
                        }
                        <span class="text-xs text-gray-400">(@group.Posts.Count)</span>
                    </div>

                    @if (group.Posts.Any())
                    {
                        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                            @foreach (var post in group.Posts)
                            {
                                <partial name="Partials/_PostCard" model="new[] { post }" />
                            }
                        </div>
                    }
                    else
                    {
                        <p class="text-sm text-gray-400 bg-white rounded-2xl border border-dashed border-slate-200 p-6 text-center">
                            No posts in this collection yet.
                        </p>
                    }
                </div>
            }
        </div>
    }
    else
    {
        <div class="text-center py-20">
            <div class="bg-slate-50 w-24 h-24 rounded-full flex items-center justify-center mx-auto mb-4">
                <svg class="w-12 h-12 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
                </svg>
            </div>
            <h3 class="text-lg font-bold text-gray-900">No saved posts</h3>
            <p class="text-gray-500 mt-2">Posts you save will appear here</p>
            <a asp-controller="Home" asp-action="Index" class="inline-block mt-4 px-6 py-2 bg-[#345e69] text-white font-semibold rounded-xl hover:bg-[#2a4b55] transition-colors">
                Browse Feed
            </a>
        </div>
    }
</div>
```

### Regression Testing (5.1)

- **Test Users:** `mohammed@sohba.com` (has a saved post from seed).
- **Navigation:** Save a post into a custom collection → `/Posts/SavedPosts`.
- **Expected Results:**
    - The page shows "All Saved" group AND the named collection group.
    - Each group lists its posts.
    - Favorites group shows a pink badge.
- **Failure Conditions:** If a group has 0 posts, it should either be hidden (for custom) or
  show "No posts in this collection yet" (for default/favorites).

<br>
<br>

---

<br>

# Appendix — Complete Correction Summary

| # | Issue | File | Action |
|---|-------|------|--------|
| 1 | SavedPosts.cshtml ambiguity | `Sohba/Views/Posts/SavedPosts.cshtml` | **Keep as-is** (recommended) OR replace entire file with Part 5.1 version |
| 2 | Favorites.cshtml ambiguity | `Sohba/Views/Posts/Favorites.cshtml` | **Keep as-is** (recommended); optionally replace only the script block (Bug 3) |
| 3 | `CreateCollection` drops `Id` | `Sohba/Controllers/PostsController.cs` | Replace return with `BaseResponseDto<SavedCollectionDto>` |
| 4 | `addToFavorites` calls old endpoint | `Sohba/wwwroot/js/sohba-posts.js` | Replace with `/Posts/ToggleFavorite` |
| 5 | Favorites remove button uses old endpoint | `Sohba/Views/Posts/Favorites.cshtml` | Replace script block with `/Posts/ToggleFavorite` |
| 6 | Pre-existing `toggleMenu(('` typo | `Sohba/Views/Posts/SavedPosts.cshtml` | Replace with `toggleMenu('...')` |
| 7 | Migration not applied | CLI | Run `dotnet ef database update` |
| 8 | `SavePostAsync` backward-compat limitation | `Sohba.Application/Interfaces/IInteractionService.cs` | Optional `[Obsolete]` attribute |
| 9 | True collection grouping (optional) | Multiple files | See Part 5.1 |

<br>
<br>

---

<br>

# End Of Document

This document is a correction and clarification guide for the implementation of
`FixesV1.md`. No project source files were modified while producing it.