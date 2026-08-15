# Sohba — FixesV6 Implementation Guide

<br>
<br>
<br>

**Document Name:** FixesV6.md

**Purpose:** Complete implementation guide for the next set of blocking issues discovered
while continuing the frontend test plan after `FixesV5.md` was applied.

**Scope:** This document ONLY addresses the eleven issues listed in the request:

1. **Issue 1 — Save Button Still Does Not Toggle Correctly** (backend + frontend).
2. **Issue 2 — Saved and Favorite Must Stay Fully Independent** (backend + frontend).
3. **Issue 3 — Saved Posts Page Needs Better Structure and Pagination** (backend + view).
4. **Issue 4 — Leave Group Throws EF Tracking Exception** (repository/service).
5. **Issue 5 — Friend Requests Tab Count Must Update** (frontend only).
6. **Issue 6 — Friends Page Needs Notification Icon For Requests** (frontend only).
7. **Issue 7 — Friends Page Search Bar Does Not Work** (frontend only).
8. **Issue 8 — Home Search Button Still Floats Over The Search Bar** (view only).
9. **Issue 9 — Notification Dropdown Must Route To The Correct Destination** (frontend +
   routing logic).
10. **Issue 10 — Home Feed Post Duplication Came Back** (frontend + repository).
11. **Issue 11 — Reply On Reply Returns Retrieval Error** (controller).

**Author Role:** Senior Software Architect / Senior ASP.NET Core MVC Engineer / Senior .NET
Backend Engineer / Senior Frontend Engineer / Code Reviewer / QA Engineer.

**Stack:** ASP.NET Core MVC · Clean Architecture (Domain / Application / Infrastructure /
Presentation) · Repository Pattern · Dependency Injection · Entity Framework Core ·
JavaScript (Vanilla) · AJAX · AutoMapper · Tailwind CSS.

**Important:** No project source file was modified while writing this document. This is a
guide only.

<br>
<br>

---

<br>

# TABLE OF CONTENTS

1. [How To Use This Document](#how-to-use-this-document)
2. [Architecture Rules (Mandatory)](#architecture-rules-mandatory)
3. [Issue 1 — Save Button Still Does Not Toggle Correctly](#issue-1--save-button-still-does-not-toggle-correctly)
4. [Issue 2 — Saved and Favorite Must Stay Fully Independent](#issue-2--saved-and-favorite-must-stay-fully-independent)
5. [Issue 3 — Saved Posts Page Needs Better Structure and Pagination](#issue-3--saved-posts-page-needs-better-structure-and-pagination)
6. [Issue 4 — Leave Group Throws EF Tracking Exception](#issue-4--leave-group-throws-ef-tracking-exception)
7. [Issue 5 — Friend Requests Tab Count Must Update](#issue-5--friend-requests-tab-count-must-update)
8. [Issue 6 — Friends Page Needs Notification Icon For Requests](#issue-6--friends-page-needs-notification-icon-for-requests)
9. [Issue 7 — Friends Page Search Bar Does Not Work](#issue-7--friends-page-search-bar-does-not-work)
10. [Issue 8 — Home Search Button Still Floats Over The Search Bar](#issue-8--home-search-button-still-floats-over-the-search-bar)
11. [Issue 9 — Notification Dropdown Must Route To The Correct Destination](#issue-9--notification-dropdown-must-route-to-the-correct-destination)
12. [Issue 10 — Home Feed Post Duplication Came Back](#issue-10--home-feed-post-duplication-came-back)
13. [Issue 11 — Reply On Reply Returns Retrieval Error](#issue-11--reply-on-reply-returns-retrieval-error)
14. [Appendix — Full File Inventory](#appendix--full-file-inventory)

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

# Issue 1 — Save Button Still Does Not Toggle Correctly

## Issue

The Save button still saves posts successfully, but it does **not** work as a proper toggle.
When a post is already saved, clicking Save again should remove it from Saved. That is not
happening.

## Related Feature

- **Feature Name:** Post Actions — Save Post / Favorites.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.10 (Save & Favorites).

## Expected Behaviour

- Saving a post works (opens collection picker → saves to collection).
- Clicking Save again when `IsSaved = true` removes the post from Saved.
- Save and Favorite remain fully separate.
- Favorite must NOT automatically mean Saved.

## Current Behaviour

- Saving works.
- When `IsSaved = true`, clicking Save calls `toggleSavePost(postId, true)` → calls
  `/Posts/RemoveFromSaved` → which removes only non-Favorite rows.
- If the post is saved ONLY via Favorites, `RemoveFromSaved` finds no non-Favorite row and
  is a no-op — but because the backend reports `IsSaved = true` for a favorited post, the
  button stays in the "Saved"/"Favorited" state and the user sees no visual toggle.

## Root Cause

The backend reports `IsSaved = true` whenever the post exists in ANY `SavedPost` row —
**including the Favorites row**. Two places compute it incorrectly:

**1. `Sohba.Application/Services/InteractionService.cs` — `MapPostsToResponse`:**

```csharp
dto.IsSaved = savedDict.ContainsKey(p.Id);
```

`savedDict` is keyed on every `SavedPost` row for the user, including rows whose
`Tag == SavedTag.Favorite`. So a post that is only favorited still gets `IsSaved = true`.

**2. `Sohba.Application/Services/PostService.cs` — `GetPostByIdAsync`:**

```csharp
var isSaved = savedPosts.Any(s => s.PostId == postId);
```

Same problem — `Any(...)` is true when the only row is the Favorites row.

Because `IsSaved` is `true` for a favorited-only post, the frontend renders the Save button
as active and `toggleSavePost(postId, true)` cannot meaningfully remove it (the removal only
applies to non-Favorite rows).

## Execution Flow

```
User saves a post to a collection
    → SaveToCollection → SavedPost { PostId, CollectionId, Tag = General }
    → MapPostsToResponse → savedDict contains PostId → IsSaved = true      (correct)

User adds the SAME post to Favorites
    → ToggleFavorite → SavedPost { PostId, CollectionId = Favorites, Tag = Favorite }
    → MapPostsToResponse → savedDict contains PostId → IsSaved = true      (correct)

User favorites a DIFFERENT post (never saved to a collection)
    → ToggleFavorite → SavedPost { PostId, Tag = Favorite } only
    → MapPostsToResponse → savedDict contains PostId → IsSaved = true      (WRONG — not saved)
    → _PostCard renders Save button as "Favorited"/"Saved" (post.IsSaved = true)
    → User clicks Save → toggleSavePost(postId, true) → RemoveFromSaved
        → removes non-Favorite rows only → NONE exist → no-op
        → UI stays unchanged → "toggle does not work"                       ← BUG
```

## Related Files

- `Sohba.Application/Services/InteractionService.cs`
- `Sohba.Application/Services/PostService.cs`
- `Sohba/wwwroot/js/sohba-posts.js`
- `Sohba/Views/Shared/Partials/_PostCard.cshtml`

## Affected Components

- Application Service — `InteractionService.cs`
- Application Service — `PostService.cs`
- JavaScript — `sohba-posts.js`
- View — `_PostCard.cshtml`

## Files That Need Modification

1. `Sohba.Application/Services/InteractionService.cs`
2. `Sohba.Application/Services/PostService.cs`
3. `Sohba/wwwroot/js/sohba-posts.js`
4. `Sohba/Views/Shared/Partials/_PostCard.cshtml` (label logic only — optional)

## Implementation Plan

### Step 1 — Decouple `IsSaved` from Favorites

`IsSaved` must be `true` only when the post is in at least one **non-Favorite** collection —
i.e. `Tag != SavedTag.Favorite`.

- In `InteractionService.MapPostsToResponse`, compute `IsSaved` from the tags list:
  `IsSaved = tags.Any(t => t != SavedTag.Favorite)`.
- In `PostService.GetPostByIdAsync`, change `isSaved` to
  `savedPosts.Any(s => s.PostId == postId && s.Tag != SavedTag.Favorite)`.

### Step 2 — Fix the frontend button label logic

In `_PostCard.cshtml`, the Save button text/icon should reflect `IsSaved` (collection) and
NOT `IsFavorite`. When a post is favorited but not saved, the Save button should still read
"Save Post".

### Step 3 — Fix `updateSaveFavoriteButtons` coupling

In `sohba-posts.js`, `addToFavorites` currently calls
`updateSaveFavoriteButtons(postId, true, !isCurrentlyFav)` which forces `isSaved = true` on
the Save button whenever Favorites toggles. Change the Save-button state to depend only on
the actual saved state, not on the favorite toggle.

## Code Changes

### File: Sohba.Application/Services/InteractionService.cs

<div style="color:red"><b>REMOVE — the IsSaved computation that includes Favorites:</b></div>

```csharp
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

<div style="color:green"><b>REPLACE WITH — IsSaved based on non-Favorite collections only:</b></div>

```csharp
            return postList.Select(p => {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;

                if (savedDict.TryGetValue(p.Id, out var tags))
                {
                    // A post is "saved" only when it is in a NON-Favorite collection.
                    // Favorites alone does NOT imply Saved.
                    dto.IsSaved = tags.Any(t => t != SavedTag.Favorite);
                    dto.IsFavorite = tags.Contains(SavedTag.Favorite);
                    dto.SavedTag = dto.IsFavorite ? SavedTag.Favorite.ToString() : tags.First().ToString();
                }
                else
                {
                    dto.IsSaved = false;
                    dto.IsFavorite = false;
                }

                dto.CurrentUserReaction = reactionDict.GetValueOrDefault(p.Id);
                return dto;
            });
```

### File: Sohba.Application/Services/PostService.cs

<div style="color:red"><b>REMOVE — the isSaved computation that includes Favorites:</b></div>

```csharp
            var savedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(currentUserId);
            var isSaved = savedPosts.Any(s => s.PostId == postId);
            var isFavorite = savedPosts.Any(s => s.PostId == postId && s.Tag == SavedTag.Favorite);
```

<div style="color:green"><b>REPLACE WITH — Favorites must not imply Saved:</b></div>

```csharp
            var savedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(currentUserId);
            // A post is "saved" only when it is in a NON-Favorite collection.
            var isSaved = savedPosts.Any(s => s.PostId == postId && s.Tag != SavedTag.Favorite);
            var isFavorite = savedPosts.Any(s => s.PostId == postId && s.Tag == SavedTag.Favorite);
```

### File: Sohba/wwwroot/js/sohba-posts.js

<div style="color:red"><b>REMOVE — addToFavorites forcing isSaved = true on the Save button:</b></div>

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

<div style="color:green"><b>REPLACE WITH — only the Favorite button is updated by the favorite action:</b></div>

```javascript
window.SohbaApp.addToFavorites = async function (postId) {
    try {
        const result = await window.SohbaApp.post('/Posts/ToggleFavorite', { postId });

        if (result.success) {
            const btn = document.querySelector(`[data-fav-button="${postId}"]`);
            const isCurrentlyFav = btn && btn.classList.contains('text-pink-600');
            const newFavState = !isCurrentlyFav;

            // Only update the Favorite button; the Save button state is unchanged.
            updateSaveFavoriteButtons(postId, null, newFavState);

            window.SohbaApp.toast(newFavState ? 'Added to favorites!' : 'Removed from favorites', 'success');
        } else {
            window.SohbaApp.toast(result.error || 'Failed to update favorites', 'error');
        }
    } catch (error) {
        console.error('Favorite error:', error);
        window.SohbaApp.toast('Network error', 'error');
    }
};
```

<div style="color:red"><b>REMOVE — the updateSaveFavoriteButtons that couples isSaved with isFavorite:</b></div>

```javascript
function updateSaveFavoriteButtons(postId, isSaved, isFavorite) {
    const saveBtn = document.querySelector(`[data-save-button="${postId}"]`);
    const favBtn = document.querySelector(`[data-fav-button="${postId}"]`);
    
        if (saveBtn) {
                const icon = saveBtn.querySelector('svg');
                const text = saveBtn.querySelector('.btn-text');
                if (isSaved) {
                        saveBtn.classList.add('text-amber-600', 'bg-amber-50');
                        icon.setAttribute('fill', 'currentColor');
                        text.textContent = isFavorite ? 'Favorited' : 'Saved';
                    } else {
                        saveBtn.classList.remove('text-amber-600', 'bg-amber-50');
                        icon.setAttribute('fill', 'none');
                        text.textContent = 'Save Post';
                    }
        }
        if (favBtn) {
            const icon = favBtn.querySelector('svg');
            const text = favBtn.querySelector('.btn-text');
            if (isFavorite) {
                    favBtn.classList.add('text-pink-600', 'bg-pink-50');
                    icon.setAttribute('fill', 'currentColor');
                    text.textContent = 'Favorited';
            } else {
                    favBtn.classList.remove('text-pink-600', 'bg-pink-50');
                    icon.setAttribute('fill', 'none');
                    text.textContent = 'Add to Favorites';
            }
        }
}
```

<div style="color:green"><b>REPLACE WITH — null-safe, fully independent state updates:</b></div>

```javascript
function updateSaveFavoriteButtons(postId, isSaved, isFavorite) {
    const saveBtn = document.querySelector(`[data-save-button="${postId}"]`);
    const favBtn = document.querySelector(`[data-fav-button="${postId}"]`);

    if (saveBtn && isSaved !== null && isSaved !== undefined) {
        const icon = saveBtn.querySelector('svg');
        const text = saveBtn.querySelector('.btn-text');
        if (isSaved) {
            saveBtn.classList.add('text-amber-600', 'bg-amber-50');
            icon.setAttribute('fill', 'currentColor');
            text.textContent = 'Saved';
        } else {
            saveBtn.classList.remove('text-amber-600', 'bg-amber-50');
            icon.setAttribute('fill', 'none');
            text.textContent = 'Save Post';
        }
    }
    if (favBtn && isFavorite !== null && isFavorite !== undefined) {
        const icon = favBtn.querySelector('svg');
        const text = favBtn.querySelector('.btn-text');
        if (isFavorite) {
            favBtn.classList.add('text-pink-600', 'bg-pink-50');
            icon.setAttribute('fill', 'currentColor');
            text.textContent = 'Favorited';
        } else {
            favBtn.classList.remove('text-pink-600', 'bg-pink-50');
            icon.setAttribute('fill', 'none');
            text.textContent = 'Add to Favorites';
        }
    }
}
```

### File: Sohba/Views/Shared/Partials/_PostCard.cshtml

<div style="color:red"><b>REMOVE — the Save button label logic that shows "Favorited" when IsFavorite:</b></div>

```csharp
                                        <span class="btn-text">
                                            @if (post.IsSaved)
                                            {
                                                @(post.IsFavorite ? "Favorited" : "Saved")
                                            }
                                            else
                                            {
                                                @("Save Post")
                                            }
                                        </span>
```

<div style="color:green"><b>REPLACE WITH — the Save button only reflects collection-Saved state:</b></div>

```csharp
                                        <span class="btn-text">
                                            @(post.IsSaved ? "Saved" : "Save Post")
                                        </span>
```

> **Note:** Keep the icon block unchanged, but remove the inner `@if (post.IsFavorite)`
> branch that renders the pink heart for a favorited-only post on the Save button. If the
> post is saved to a non-Favorite collection, the amber bookmark icon shows. Favorited-only
> posts now render "Save Post" on the Save button (the Favorite button shows the heart).

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:** Home → post menu → Save Post / Add to Favorites.
- **Expected Results:**
    - Save a post to a collection → `IsSaved = true` → clicking Save again removes it from
      collections (`RemoveFromSaved`), button returns to "Save Post".
    - Favorite a post that is NOT saved → Save button remains "Save Post" (no coupling).
    - Save + Favorite the same post → both buttons active; removing from Saved keeps the
      Favorite; removing from Favorites keeps the Saved.
- **Failure Conditions:**
    - If a favorited-only post still shows the Save button as active, the backend
      `IsSaved` computation still includes Favorites.
    - If toggling Favorites changes the Save button text, the JS coupling fix was not applied.
- **Edge Cases:**
    - Post in Favorites only: `IsSaved = false`, `IsFavorite = true`.
    - Post in a collection only: `IsSaved = true`, `IsFavorite = false`.
    - Post in both: `IsSaved = true`, `IsFavorite = true`.
    - Post in neither: `IsSaved = false`, `IsFavorite = false`.

<br>
<br>

---

<br>

# Issue 2 — Saved and Favorite Must Stay Fully Independent

## Issue

The save and favorite logic must remain separated completely. A post being favorited must
**not** imply it is in Saved.

## Related Feature

- **Feature Name:** Post Actions — Save Post / Favorites.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.10 (Save & Favorites).

## Expected Behaviour

A post can be:

- Saved only
- Favorited only
- Saved and Favorited
- Neither

Removing from Saved must not remove it from Favorite, and vice versa.

## Current Behaviour

The frontend fix for Issue 1 (this document) removes the visible coupling, but the backend
still computes `IsSaved` in a coupled way (see Issue 1 root cause). Additionally,
`InteractionService.GetSavedPostsByTagAsync` unconditionally sets:

```csharp
dto.IsSaved = true;
dto.IsFavorite = tag == SavedTag.Favorite;
```

Which means a request for `SavedTag.Favorite` returns posts with `IsSaved = true` — coupling
Favorite row existence with the Saved flag even when the data is loaded from a Favorite-only
query.

## Root Cause

The coupling exists in **three backend locations**:

1. `InteractionService.MapPostsToResponse` → `IsSaved = savedDict.ContainsKey(p.Id)`
   (includes Favorites rows) — fixed in Issue 1.
2. `PostService.GetPostByIdAsync` → `isSaved = savedPosts.Any(s => s.PostId == postId)`
   (includes Favorites rows) — fixed in Issue 1.
3. `InteractionService.GetSavedPostsByTagAsync` → force-sets `dto.IsSaved = true` for ALL
   rows returned, including when the requested tag is `Favorite`.

## Execution Flow

```
User opens Favorites page
    → PostsController.Favorites
        → GetFavoritePostsAsync(userId)  → uses GetSavedPostsByUserAndTagAsync(Favorite)
        → MapPostsToResponse  → IsSaved = savedDict.ContainsKey(postId) → true for favorites
    → Favorite-only post shows IsSaved = true on the Saved page / post card

User opens Posts/SavedPosts?tag=Favorite (legacy route)
    → GetSavedPostsByTagAsync(Favorite)
        → foreach dto: IsSaved = true   ← force-coupled even though this is the Favorites tag
```

## Related Files

- `Sohba.Application/Services/InteractionService.cs`
- `Sohba.Application/Services/PostService.cs`

## Affected Components

- Application Service — `InteractionService.cs`
- Application Service — `PostService.cs`

## Files That Need Modification

1. `Sohba.Application/Services/InteractionService.cs`
2. `Sohba.Application/Services/PostService.cs`

## Implementation Plan

### Step 1 — Apply the Issue 1 decoupling to both services

The same changes described in Issue 1 fully decouple `IsSaved` from Favorites in
`MapPostsToResponse` and `GetPostByIdAsync`.

### Step 2 — Fix `GetSavedPostsByTagAsync`

Remove the unconditional `dto.IsSaved = true`. Instead, when `tag == SavedTag.Favorite`,
the DTOs should have `IsFavorite = true` and `IsSaved = false` (unless a row with a
non-Favorite tag also exists — but the query here filters by a single tag, so it is safe to
set `IsSaved = tag != Favorite`).

## Code Changes

### File: Sohba.Application/Services/InteractionService.cs

<div style="color:red"><b>REMOVE — the forced IsSaved coupling in GetSavedPostsByTagAsync:</b></div>

```csharp
            foreach (var dto in dtos)
            {
                dto.IsSaved = true;
                dto.IsFavorite = tag == SavedTag.Favorite;
            }
```

<div style="color:green"><b>REPLACE WITH — tag-consistent independent flags:</b></div>

```csharp
            foreach (var dto in dtos)
            {
                // Favorite rows are NOT "saved to a collection". The flags must stay independent.
                dto.IsSaved = tag != SavedTag.Favorite;
                dto.IsFavorite = tag == SavedTag.Favorite;
            }
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:** Favorites page, SavedPosts page, Home feed.
- **Expected Results:**
    - A post in Favorites only: `IsSaved = false`, `IsFavorite = true`.
    - A post in a collection only: `IsSaved = true`, `IsFavorite = false`.
    - Removing from Saved leaves the Favorite row untouched; the post still appears on the
      Favorites page.
    - Removing from Favorites leaves the collection row untouched; the post still appears on
      SavedPosts.
- **Failure Conditions:**
    - If `Posts/SavedPosts?tag=Favorite` DTOs still have `IsSaved = true`, the
      `GetSavedPostsByTagAsync` fix was not applied.
- **Edge Cases:**
    - Tag "General"/"WatchLater"/"Work"/"Education" → `IsSaved = true`, `IsFavorite = false`.
    - Tag "Favorite" → `IsSaved = false`, `IsFavorite = true`.

<br>
<br>

---

<br>

# Issue 3 — Saved Posts Page Needs Better Structure and Pagination

## Issue

The page `/Posts/SavedPosts` needs cleanup and modernization: better UI structure, better
organization, pagination, clean/modern layout, and proper handling for larger saved-post
lists.

## Related Feature

- **Feature Name:** Saved Posts.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.10 (Save & Favorites).

## Expected Behaviour

- Clean and modern layout with grouping by collection.
- Pagination for large saved lists (not loading every saved post at once).
- Proper backend pagination logic + a paged route/query design.

## Current Behaviour

- `GetSavedPostsGroupedAsync(userId)` loads ALL saved rows and ALL collections with NO paging.
- `PostsController.SavedPosts(string tag = "all")` passes the full list to the view.
- The view (`SavedPosts.cshtml`) renders every group and every post in one request — no
  pagination, no page navigation.

## Root Cause

**Backend:** `InteractionService.GetSavedPostsGroupedAsync` iterates over all collections
and all saved posts with no `Skip/Take`. There is no paged variant.

**Controller:** `SavedPosts` accepts a `tag` parameter but never uses it to paginate; the
`tag` is only stored in `ViewBag.CurrentTag`.

**View:** `SavedPosts.cshtml` has no pagination controls and no dependency on a page number.

## Execution Flow

```
GET /Posts/SavedPosts
    → PostsController.SavedPosts
        → GetSavedPostsGroupedAsync(userId)
            → GetCollectionsByUserAsync → ALL collections
            → GetSavedPostsByUserAsync  → ALL saved rows
            → MapPostsToResponse on ALL posts       ← no paging
        → View(all groups, all posts)
    → Razor renders every post                      ← grows unboundedly
```

## Related Files

- `Sohba.Application/Services/InteractionService.cs`
- `Sohba.Application/Interfaces/IInteractionService.cs`
- `Sohba/Controllers/PostsController.cs`
- `Sohba/Views/Posts/SavedPosts.cshtml`
- `Sohba.Application/DTOs/PostAggregate/SavedPostsGroupedDto.cs`
- `Sohba.Application/DTOs/Common/PagedResult.cs`

## Affected Components

- Application Service — `InteractionService.cs`
- Application Interface — `IInteractionService.cs`
- Controller — `PostsController.cs`
- View — `SavedPosts.cshtml`

## Files That Need Modification

1. `Sohba.Application/Interfaces/IInteractionService.cs`
2. `Sohba.Application/Services/InteractionService.cs`
3. `Sohba/Controllers/PostsController.cs`
4. `Sohba/Views/Posts/SavedPosts.cshtml`

## Implementation Plan

### Step 1 — Add a paged grouped method to `IInteractionService`

Add:

```csharp
Task<Result<PagedResult<SavedPostsGroupedDto>>> GetSavedPostsGroupedPagedAsync(
    Guid userId, int page = 1, int pageSize = 10);
```

### Step 2 — Implement it in `InteractionService`

- Build the groups exactly as `GetSavedPostsGroupedAsync` does.
- Paginate the **posts inside each group** (or, for simplicity and the stated scope,
  paginate the top-level list of groups if the group count is small — but the requirement is
  "proper handling for larger saved-post lists", so paginate each group's posts).
- Return a `PagedResult<SavedPostsGroupedDto>`.

**Recommended approach:** Paginate each collection's post list independently. Each group
contains at most `pageSize` posts; a `PageIndex` per group is derived from the same `page`
parameter. For the default "All Saved" group, paginate the combined list.

### Step 3 — Update `PostsController.SavedPosts`

Accept `page` and `pageSize` query parameters and pass them to the new method; store
`ViewBag.Page`, `ViewBag.PageSize`, `ViewBag.TotalPages`.

### Step 4 — Update `SavedPosts.cshtml`

Add pagination controls (Prev / Next, page X of Y) and keep the grouped layout.

## Code Changes

### File: Sohba.Application/Interfaces/IInteractionService.cs

<div style="color:red"><b>REMOVE — the current grouped method declaration:</b></div>

```csharp
        Task<Result<IEnumerable<SavedPostsGroupedDto>>> GetSavedPostsGroupedAsync(Guid userId);
```

<div style="color:green"><b>REPLACE WITH — keep it and add the paged variant:</b></div>

```csharp
        Task<Result<IEnumerable<SavedPostsGroupedDto>>> GetSavedPostsGroupedAsync(Guid userId);
        Task<Result<PagedResult<SavedPostsGroupedDto>>> GetSavedPostsGroupedPagedAsync(
            Guid userId, int page = 1, int pageSize = 10);
```

### File: Sohba.Application/Services/InteractionService.cs

<div style="color:green"><b>ADD — the paged implementation after GetSavedPostsGroupedAsync:</b></div>

```csharp
        public async Task<Result<PagedResult<SavedPostsGroupedDto>>> GetSavedPostsGroupedPagedAsync(
            Guid userId, int page = 1, int pageSize = 10)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var collections = (await _unitOfWork.Interactions.GetCollectionsByUserAsync(userId)).ToList();
            var allSaved = (await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId))
                .OrderByDescending(s => s.SavedAt)
                .ToList();

            var result = new List<SavedPostsGroupedDto>();

            // "All Saved" group — paginated over ALL non-Favorite rows, plus favorite rows
            // are included only if they are also saved to a collection. For the grouped page,
            // the all-saved group paginates the union of posts.
            var allPosts = allSaved
                .Where(s => s.Tag != SavedTag.Favorite || s.CollectionId != null)
                .Select(s => s.Post)
                .DistinctBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var allMapped = await MapPostsToResponse(allPosts, userId);
            result.Add(new SavedPostsGroupedDto
            {
                CollectionId = Guid.Empty,
                CollectionName = "All Saved",
                IsFavorites = false,
                Posts = allMapped.ToList()
            });

            foreach (var collection in collections)
            {
                var collectionSaves = allSaved
                    .Where(s => s.CollectionId == collection.Id)
                    .OrderByDescending(s => s.SavedAt)
                    .ToList();

                if (collectionSaves.Count == 0 && !collection.IsDefault && !collection.IsFavorites)
                    continue;

                var pagedSaves = collectionSaves
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var mapped = await MapPostsToResponse(pagedSaves.Select(s => s.Post).ToList(), userId);
                result.Add(new SavedPostsGroupedDto
                {
                    CollectionId = collection.Id,
                    CollectionName = collection.Name,
                    IsFavorites = collection.IsFavorites,
                    Posts = mapped.ToList()
                });
            }

            var totalPosts = allPosts.Count; // used only for the paging summary
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalPosts / pageSize));

            return Result<PagedResult<SavedPostsGroupedDto>>.Success(new PagedResult<SavedPostsGroupedDto>
            {
                Items = result,
                TotalCount = totalPosts,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }
```

> **Note:** `DistinctBy` requires .NET 6+. The project targets .NET 10 (from the log
> `Version=10.0.0.0`), so `DistinctBy` is available. If the target framework differs, replace
> with `GroupBy(p => p.Id).Select(g => g.First())`.

### File: Sohba/Controllers/PostsController.cs

<div style="color:red"><b>REMOVE — the current SavedPosts action:</b></div>

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

<div style="color:green"><b>REPLACE WITH — a paged version:</b></div>

```csharp
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
```

### File: Sohba/Views/Posts/SavedPosts.cshtml

<div style="color:red"><b>REMOVE — the model line and the header only:</b></div>

```csharp
@model IEnumerable<Sohba.Application.DTOs.PostAggregate.SavedPostsGroupedDto>
```

<div style="color:green"><b>REPLACE WITH — a paged model:</b></div>

```csharp
@model Sohba.Application.DTOs.Common.PagedResult<Sohba.Application.DTOs.PostAggregate.SavedPostsGroupedDto>
```

<div style="color:red"><b>REMOVE — the group iteration over the raw model:</b></div>

```csharp
    @if (Model != null && Model.Any())
    {
        <div class="space-y-8">
            @foreach (var group in Model)
            {
```

<div style="color:green"><b>REPLACE WITH — iterate over Model.Items:</b></div>

```csharp
    @if (Model != null && Model.Items != null && Model.Items.Any())
    {
        <div class="space-y-8">
            @foreach (var group in Model.Items)
            {
```

<div style="color:green"><b>ADD — pagination controls before the closing div of the page:</b></div>

```csharp
    @if (Model != null && Model.TotalPages > 1)
    {
        <div class="flex items-center justify-center gap-4 mt-8">
            @if (Model.HasPreviousPage)
            {
                <a href="/Posts/SavedPosts?page=@(Model.Page - 1)&pageSize=@Model.PageSize"
                   class="px-5 py-2.5 bg-[#345e69] text-white font-bold rounded-xl hover:bg-[#2a4b55] transition-all">
                    Prev
                </a>
            }
            <span class="text-sm text-gray-500 font-semibold">
                Page @Model.Page of @Model.TotalPages
            </span>
            @if (Model.HasNextPage)
            {
                <a href="/Posts/SavedPosts?page=@(Model.Page + 1)&pageSize=@Model.PageSize"
                   class="px-5 py-2.5 bg-[#345e69] text-white font-bold rounded-xl hover:bg-[#2a4b55] transition-all">
                    Next
                </a>
            }
        </div>
    }
</div>
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com` (needs > 10 saved posts to test pagination).
- **Navigation:** `/Posts/SavedPosts?page=1&pageSize=10`.
- **Expected Results:**
    - The page renders grouped collections ("All Saved" + each named collection / Favorites).
    - Pagination links appear when TotalPages > 1.
    - Clicking Next loads the next page without errors.
- **Failure Conditions:**
    - If all posts still render at once, the controller is not using the paged method.
    - If a group's post count exceeds pageSize, the in-group paging is not applied.
- **Edge Cases:**
    - 0 saved posts → empty state as today.
    - A single collection with > pageSize posts → paginated inside that group.
    - Invalid page (e.g. 99) → `Math.Max(1, page)` safely handles it.

<br>
<br>

---

<br>

# Issue 4 — Leave Group Throws EF Tracking Exception

## Issue

Clicking **Leave Group** throws:

```text
System.InvalidOperationException
The instance of entity type 'User' cannot be tracked because another instance with the same key value for {'Id'} is already being tracked.
```

The stack trace points to:

```text
Sohba.Infrastructure.Repositories.GroupRepository.RemoveMember(GroupMember member)
```

and specifically:

```csharp
_context.Set<GroupMember>().Remove(member);
```

## Related Feature

- **Feature Name:** Groups — Leave Group.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Groups section.

## Expected Behaviour

- A member can leave a group without an exception.
- The member row is removed and the page reflects the new state.

## Current Behaviour

- Clicking Leave Group throws `InvalidOperationException` inside `RemoveMember`.
- The group member is NOT removed.

## Root Cause

`GroupRepository.GetByIdAsync` loads the `Group` graph with:

```csharp
.Include(g => g.Admin)
.Include(g => g.GroupMembers).ThenInclude(m => m.User)
.AsNoTrackingWithIdentityResolution()
```

When the group admin is ALSO a member (the normal case for a group creator who joined as
admin), the **same `User`** appears twice in the result graph as two separate detached
instances:

- One as `group.Admin` (`Guid` = admin user id).
- One as `member.User` for the member row where `member.UserId == admin user id`.

`AsNoTrackingWithIdentityResolution` does identity resolution **within a single query**, but
the same entity can still be materialized once and shared within the query result. However,
the real conflict arises because `BasicController.OnActionExecutionAsync` runs before every
action (including `Groups/Details`): it calls `GroupService.GetRecommendedGroupsAsync` →
`GroupRepository.GetRecommendedGroupsAsync` which ALSO loads `GroupMembers`/`Admin` with
`AsNoTracking`. Those entities are attached to the SAME scoped `AppDbContext`.

Then `LeaveGroupAsync`:

1. Loads the group again via `GetByIdAsync` (detached graph).
2. Finds `member` in `group.GroupMembers`.
3. Calls `_unitOfWork.Groups.RemoveMember(member)` → `_context.Set<GroupMember>().Remove(member)`.
4. EF discovers the detached graph (including `member.User`, `member.Group`, and the group's
   `Admin`/`GroupMembers`) and attempts to attach ALL of them.
5. The same `User` id is already tracked (from the earlier `GetRecommendedGroupsAsync`
   query that loaded that user as a member/admin of another or the same group) → EF throws
   "cannot be tracked because another instance with the same key value ... is already being tracked."

In short: the entity passed into `Remove` comes from a **detached graph with duplicate
navigation references**, and the EF context already has tracked instances from a prior query
in the same scoped context.

## Execution Flow

```
POST /Groups/Leave { groupId }
    → BaseController.OnActionExecutionAsync
        → GetRecommendedGroupsAsync → loads GroupMembers + User (tracked)   ← prior tracked user
    → GroupsController.Leave
        → GroupService.LeaveGroupAsync
            → GetByIdAsync(groupId)   // detached graph: Admin + GroupMembers.User
                // if admin == user (self-created group): same User appears twice
            → member = group.GroupMembers.FirstOrDefault(m => m.UserId == userId)
            → _unitOfWork.Groups.RemoveMember(member)
                → _context.Set<GroupMember>().Remove(member)
                → EF auto-attaches member.User / member.Group / group.Admin / group.GroupMembers
                → User already tracked from OnActionExecutionAsync
                → InvalidOperationException: "The instance of entity type 'User'
                   cannot be tracked because another instance with the same key value ..."  ← BUG
```

## Related Files

- `Sohba.Infrastructure/Repositories/GroupRepository.cs`
- `Sohba.Application/Services/GroupService.cs`
- `Sohba/Controllers/GroupsController.cs`
- `Sohba/Controllers/BaseController.cs` (OnActionExecutionAsync → recommended groups)
- `Sohba.Domain/Entities/GroupAndPage/GroupMember.cs`

## Affected Components

- Repository — `GroupRepository.cs`
- Application Service — `GroupService.cs`
- Controller — `BaseController.cs` (contributing tracked entities — no change needed)
- Entity — `GroupMember.cs`

## Files That Need Modification

1. `Sohba.Infrastructure/Repositories/GroupRepository.cs`
2. `Sohba.Application/Services/GroupService.cs`

## Implementation Plan

### Step 1 — Load the member as a tracked entity for removal

Add a repository method `GetMemberByUserAndGroupAsync(Guid groupId, Guid userId)` that
queries `_context.Set<GroupMember>()` directly (tracked, no navigation includes). Use the
returned member in `RemoveMember`.

### Step 2 — Null out the navigation properties before removal (defensive)

Even with a tracked query, setting `member.User = null; member.Group = null;` before
`Remove` prevents EF from cascading the whole graph re-attach. This is the standard EF
cure for "cannot be tracked because another instance ... is already being tracked".

### Step 3 — Update `LeaveGroupAsync` and `KickMemberAsync`

Use the new repository method instead of pulling the member from the detached
`group.GroupMembers` collection.

## Code Changes

### File: Sohba.Infrastructure/Repositories/GroupRepository.cs

<div style="color:red"><b>REMOVE — the current RemoveMember usage context is fine; ADD — a tracked lookup method before it:</b></div>

```csharp
        public void RemoveMember(GroupMember member)
        {
            _context.Set<GroupMember>().Remove(member);
        }
```

<div style="color:green"><b>REPLACE WITH — a tracked lookup + safe removal:</b></div>

```csharp
        // Loads the member as a TRACKED entity WITHOUT navigation properties.
        // This avoids the duplicate-User tracking conflict seen when removing a member
        // pulled from a detached group graph.
        public async Task<GroupMember?> GetMemberByUserAndGroupAsync(Guid groupId, Guid userId)
        {
            return await _context.Set<GroupMember>()
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        }

        public void RemoveMember(GroupMember member)
        {
            if (member == null) return;

            // Detach navigation properties so EF does not re-attach duplicate User/Group
            // instances that may already be tracked by the shared DbContext.
            member.User = null;
            member.Group = null;

            _context.Set<GroupMember>().Remove(member);
        }
```

### File: Sohba.Application/Services/GroupService.cs — LeaveGroupAsync

<div style="color:red"><b>REMOVE — the detached-graph member lookup:</b></div>

```csharp
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null)
                return Result<bool>.Failure("Group not found.");

            var member = group.GroupMembers.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
                return Result<bool>.Failure("You are not a member of this group.");

            var isAdmin = member.Role == GroupRole.Admin;
            var adminCount = group.GroupMembers.Count(m => m.Role == GroupRole.Admin);

            var validation = _groupDomainService.CanLeaveGroup(userId, groupId, isAdmin, adminCount);
            if (!validation.IsSuccess)
                return Result<bool>.Failure(validation.Error);

            _unitOfWork.Groups.RemoveMember(member);
            var affectedRows = await _unitOfWork.CompleteAsync();
```

<div style="color:green"><b>REPLACE WITH — a tracked member lookup:</b></div>

```csharp
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null)
                return Result<bool>.Failure("Group not found.");

            // Load the member as a TRACKED entity to avoid the EF tracking conflict.
            var member = await _unitOfWork.Groups.GetMemberByUserAndGroupAsync(groupId, userId);
            if (member == null)
                return Result<bool>.Failure("You are not a member of this group.");

            var isAdmin = member.Role == GroupRole.Admin;
            var adminCount = group.GroupMembers.Count(m => m.Role == GroupRole.Admin);

            var validation = _groupDomainService.CanLeaveGroup(userId, groupId, isAdmin, adminCount);
            if (!validation.IsSuccess)
                return Result<bool>.Failure(validation.Error);

            _unitOfWork.Groups.RemoveMember(member);
            var affectedRows = await _unitOfWork.CompleteAsync();
```

### File: Sohba.Application/Services/GroupService.cs — KickMemberAsync

<div style="color:red"><b>REMOVE — the detached-graph member lookup:</b></div>

```csharp
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            var memberToKick = group.GroupMembers.FirstOrDefault(m => m.UserId == targetUserId);
            if (memberToKick != null)
            {
                _unitOfWork.Groups.RemoveMember(memberToKick);
                await _unitOfWork.CompleteAsync();
            }
```

<div style="color:green"><b>REPLACE WITH — a tracked member lookup:</b></div>

```csharp            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            var memberToKick = await _unitOfWork.Groups.GetMemberByUserAndGroupAsync(groupId, targetUserId);
            if (memberToKick != null)
            {
                _unitOfWork.Groups.RemoveMember(memberToKick);
                await _unitOfWork.CompleteAsync();
            }
```

> **Note:** `IGroupRepository` must declare the new method. The `RemoveMember(GroupMember)`
> signature in the interface remains unchanged — only its implementation detaches
> navigation properties.

### File: Sohba.Domain/Interfaces/IGroupRepository.cs

<div style="color:green"><b>ADD — the new repository method declaration:</b></div>

```csharp
        Task<GroupMember?> GetMemberByUserAndGroupAsync(Guid groupId, Guid userId);
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com` (member), `admin@sohba.com` (group creator/admin).
- **Required data:** A group where the admin is also a member; a group where a non-admin
  member exists.
- **Navigation:** `/Groups/Details/{id}` → click "Leave Group" → confirm.
- **Expected Results:**
    - No `InvalidOperationException`.
    - The member row is removed; the user is redirected to `/Groups`.
    - If the admin leaves (adminCount > 1), the group still has an admin.
- **Failure Conditions:**
    - If the exception still occurs, the member is still being pulled from the detached
      `group.GroupMembers` graph.
- **Edge Cases:**
    - Group where admin == single member (self-created) — the `member.User == group.Admin`
      duplicate is the exact case that triggered this bug.
    - Kicking a member who is also referenced in `group.Admin` (should never happen, but the
      tracked-lookup path is safe).

<br>
<br>

---

<br>

# Issue 5 — Friend Requests Tab Count Must Update

## Issue

On `/Friends/Requests`, accepting or declining a request changes the request list, but the
UI count stays stale. The number shown in the tab/badge must match the actual remaining
requests.

## Related Feature

- **Feature Name:** Friends — Pending Requests.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Friends section.

## Expected Behaviour

- Accepting or declining updates the request count immediately.
- The tab shows the actual remaining request count.
- No manual page refresh required.

## Current Behaviour

- The row is removed from the list (after the FixesV5 ID fix), but the tab label
  `Received (@Model.PendingCount)` never changes.
- The badge in the tab header stays stale.

## Root Cause

`Sohba/wwwroot/js/features/friends.js` — `acceptRequest` and `rejectRequest` remove the DOM
row but never update the tab count label:

```javascript
if (result.success) {
    SohbaApp.toast('Friend request accepted!', 'success');
    const elem = document.querySelector(`[data-request-id="${userId}"]`);
    if (elem) elem.remove();
    // ← count never updated
}
```

The `PendingCount` was rendered server-side into the button text
`Received (@Model.PendingCount)` and is never decremented client-side.

## Execution Flow

```
GET /Friends/Requests
    → FriendsController.Requests
        → GetPendingRequestsCountAsync → N
    → Requests.cshtml renders tab: "Received (N)"

Click Accept
    → friends.js acceptRequest
        → POST /Friends/AcceptRequest → success
        → row removed from DOM
        → tab text STILL "Received (N)"          ← BUG (N not decremented)

Click Decline
    → friends.js rejectRequest
        → POST /Friends/RejectRequest → success
        → row removed from DOM
        → tab text STILL "Received (N)"          ← BUG
```

## Related Files

- `Sohba/wwwroot/js/features/friends.js`
- `Sohba/Views/Friends/Requests.cshtml`

## Affected Components

- JavaScript — `features/friends.js`
- View — `Requests.cshtml`

## Files That Need Modification

1. `Sohba/wwwroot/js/features/friends.js`

## Implementation Plan

### Step 1 — Decrement the tab count after a successful accept/decline

In `acceptRequest` and `rejectRequest`, after removing the row, find the "Received (N)" tab
button and decrement N. If N reaches 0, update the label to "Received (0)".

## Code Changes

### File: Sohba/wwwroot/js/features/friends.js

<div style="color:red"><b>REMOVE — acceptRequest success block without count update:</b></div>

```javascript
    if (result.success) {
        SohbaApp.toast('Friend request accepted!', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();
    } else {
        if (btn) { btn.disabled = false; }
        SohbaApp.toast(result.error || 'Failed to accept request', 'error');
    }
```

<div style="color:green"><b>REPLACE WITH — decrement the tab count:</b></div>

```javascript
    if (result.success) {
        SohbaApp.toast('Friend request accepted!', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();
        updatePendingRequestCount(-1);
    } else {
        if (btn) { btn.disabled = false; }
        SohbaApp.toast(result.error || 'Failed to accept request', 'error');
    }
```

<div style="color:red"><b>REMOVE — rejectRequest success block without count update:</b></div>

```javascript
    if (result.success) {
        SohbaApp.toast('Friend request declined', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();
    } else {
        if (btn) { btn.disabled = false; }
        SohbaApp.toast(result.error || 'Failed to decline request', 'error');
    }
```

<div style="color:green"><b>REPLACE WITH — decrement the tab count:</b></div>

```javascript
    if (result.success) {
        SohbaApp.toast('Friend request declined', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();
        updatePendingRequestCount(-1);
    } else {
        if (btn) { btn.disabled = false; }
        SohbaApp.toast(result.error || 'Failed to decline request', 'error');
    }
```

<div style="color:green"><b>ADD — the shared count updater helper before cancelRequest:</b></div>

```javascript
function updatePendingRequestCount(delta) {
    const tabBtn = document.querySelector('.tab-btn.active');
    const countMatch = tabBtn && tabBtn.textContent.match(/\(\s*(\d+)\s*\)/);
    if (!tabBtn || !countMatch) return;

    const newCount = Math.max(0, parseInt(countMatch[1], 10) + delta);
    tabBtn.textContent = tabBtn.textContent.replace(/\(\s*\d+\s*\)/, `(${newCount})`);
}
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Required data:** 2 pending requests.
- **Navigation:** `/Friends/Requests` → tab shows "Received (2)".
- **Expected Results:**
    - Accept one → tab shows "Received (1)" immediately.
    - Decline the other → tab shows "Received (0)" immediately.
- **Failure Conditions:**
    - If the tab still shows the old number, `updatePendingRequestCount` is not called.
- **Edge Cases:**
    - Rejecting the last request → "Received (0)" and the empty-state renders (the list is
      already empty after row removal).

<br>
<br>

---

<br>

# Issue 6 — Friends Page Needs Notification Icon For Requests

## Issue

On `/Friends`, the Requests button needs a visible notification indicator when there are
pending friend requests.

## Related Feature

- **Feature Name:** Friends — Pending Requests badge.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Friends section.

## Expected Behaviour

If there are pending requests, the Requests button shows a clear visual indicator (badge with
a number).

## Current Behaviour

The badge exists in `Index.cshtml` (`#pendingRequestsBadge`) but never appears because the
JS reads the wrong property from the response.

## Root Cause

`Sohba/Views/Friends/Index.cshtml` — `loadPendingRequestsCount`:

```javascript
const response = await fetch('/Friends/GetPendingRequestsCount');
const data = await response.json();
if (data.count > 0) { ... }
```

But `FriendsController.GetPendingRequestsCount` returns:

```csharp
return Json(BaseResponseDto<int>.SuccessResponse(result.Value));
```

`BaseResponseDto<T>` serializes as `{ success, data, error }`. The number lives in `data`,
NOT `data.count`. Therefore `data.count` is always `undefined`, the condition is false, and
the badge stays hidden.

## Execution Flow

```
GET /Friends
    → renders #pendingRequestsBadge (hidden by default)
    → script calls loadPendingRequestsCount() on load
        → fetch /Friends/GetPendingRequestsCount
        → returns { success: true, data: 3, error: null }
        → reads data.count           ← undefined
        → badge stays hidden          ← BUG
```

## Related Files

- `Sohba/Views/Friends/Index.cshtml`
- `Sohba/Controllers/FriendsController.cs`
- `Sohba.Application/DTOs/Common/BaseResponseDto.cs`

## Affected Components

- View — `Index.cshtml`

## Files That Need Modification

1. `Sohba/Views/Friends/Index.cshtml`

## Implementation Plan

### Step 1 — Read the correct property

Read `data.data` (the integer). Also handle the PascalCase serialization fallback
(`data.Data`) for robustness.

## Code Changes

### File: Sohba/Views/Friends/Index.cshtml

<div style="color:red"><b>REMOVE — the wrong property read:</b></div>

```javascript
        async function loadPendingRequestsCount() {
            const response = await fetch('/Friends/GetPendingRequestsCount');
            const data = await response.json();
            if (data.count > 0) {
                const badge = document.getElementById('pendingRequestsBadge');
                badge.textContent = data.count;
                badge.classList.remove('hidden');
            }
        }
```

<div style="color:green"><b>REPLACE WITH — read data.data:</b></div>

```javascript
        async function loadPendingRequestsCount() {
            const response = await fetch('/Friends/GetPendingRequestsCount');
            const data = await response.json();
            const count = data.data ?? data.Data ?? 0;
            const badge = document.getElementById('pendingRequestsBadge');
            if (count > 0 && badge) {
                badge.textContent = count > 99 ? '99+' : count;
                badge.classList.remove('hidden');
            }
        }
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com` with 1+ pending request.
- **Navigation:** `/Friends`.
- **Expected Results:**
    - The Requests button shows a red badge with the pending count.
- **Failure Conditions:**
    - If the badge never appears, the JS still reads `data.count`.
- **Edge Cases:**
    - 0 pending → badge stays hidden.
    - 120 pending → badge shows "99+".

<br>
<br>

---

<br>

# Issue 7 — Friends Page Search Bar Does Not Work

## Issue

The search bar on `/Friends` is not working — it should filter friends.

## Related Feature

- **Feature Name:** Friends — Search.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Friends section.

## Expected Behaviour

- Typing in the search box filters the visible friend cards by name.
- The current UI preserves functionality and behaves as designed.

## Current Behaviour

- Typing in the search box has no effect.
- No filtering occurs.

## Root Cause

`Sohba/wwwroot/js/features/friends.js` search logic binds to:

```javascript
const searchInput = document.getElementById('searchInput');
...
const userCards = document.querySelectorAll('.user-card');
const name = card.dataset.name;
```

But `Sohba/Views/Friends/Index.cshtml`:

- The search input has NO `id="searchInput"` (it has no `id` attribute at all).
- The friend cards have NO `class="user-card"` and NO `data-name` attribute.

So `searchInput` is `null`, `userCards` is empty, and the search handler never works.

## Execution Flow

```
friends.js DOMContentLoaded
    → searchInput = document.getElementById('searchInput')   → null (no id on the input)
    → search listener not attached
    → typing does nothing                                       ← BUG
```

## Related Files

- `Sohba/Views/Friends/Index.cshtml`
- `Sohba/wwwroot/js/features/friends.js`

## Affected Components

- View — `Index.cshtml`
- JavaScript — `features/friends.js` (already correct — no change needed)

## Files That Need Modification

1. `Sohba/Views/Friends/Index.cshtml`

## Implementation Plan

### Step 1 — Add the id to the search input

Add `id="searchInput"` to the search input.

### Step 2 — Add `user-card` + `data-name` to each friend card

Add `class="... user-card"` and `data-name="@friend.FriendName"` to the friend card
container so the existing `.user-card`/`data-name` filter in `friends.js` works.

## Code Changes

### File: Sohba/Views/Friends/Index.cshtml

<div style="color:red"><b>REMOVE — the search input without an id:</b></div>

```csharp
            <input type="text" placeholder="Search friends..." 
                   class="w-full pl-12 pr-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69] transition-all" />
```

<div style="color:green"><b>REPLACE WITH — the same input with id="searchInput":</b></div>

```csharp
            <input type="text" id="searchInput" placeholder="Search friends..." 
                   class="w-full pl-12 pr-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69] transition-all" />
```

<div style="color:red"><b>REMOVE — the friend card without user-card/data-name attributes:</b></div>

```csharp
            <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-5 hover-lift group">
```

<div style="color:green"><b>REPLACE WITH — add user-card class and data-name:</b></div>

```csharp
            <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-5 hover-lift group user-card" data-name="@friend.FriendName">
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com` (with several friends).
- **Navigation:** `/Friends`.
- **Expected Results:**
    - Typing a friend's name filters the grid to matching cards.
    - Clearing the input shows all friends again.
- **Failure Conditions:**
    - If typing does nothing, the input still lacks `id="searchInput"` or the cards lack
      `.user-card`/`data-name`.
- **Edge Cases:**
    - Case-insensitive matching (the existing JS uses `toLowerCase()`).
    - No results → the `#noResultsMessage` element is referenced by `friends.js`; if it is
      absent, the code only hides cards. Optionally add a `<div id="noResultsMessage"
      class="hidden ...">` for the empty state.

<br>
<br>

---

<br>

# Issue 8 — Home Search Button Still Floats Over The Search Bar

## Issue

On `/Home`, the Search button is still floating on top of the search bar instead of sitting
beside it.

## Related Feature

- **Feature Name:** Global Header Search.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Home / Search.

## Expected Behaviour

- The Search button is aligned properly.
- It does not overlap the input.
- The layout remains responsive.

## Current Behaviour

The previous fix (`pr-28`) only increased the input's right padding so text is no longer
hidden under the button. However, the button itself is still `position: absolute;
right-1.5` inside the input's container — it visually sits ON TOP of the input field, not
beside it.

## Root Cause

`Sohba/Views/Shared/Partials/_Header.cshtml` still uses the absolutely-positioned button:

```csharp
<button id="globalSearchBtn"
        type="button"
        class="absolute right-1.5 top-1/2 -translate-y-1/2 px-3 py-1.5 bg-[#345e69] ...">
    Search
</button>
```

The requirement is a button that appears **after/beside** the search input. That requires a
flex layout, not an absolutely-positioned overlay.

## Execution Flow

```
Header renders
    → <div class="relative group"> contains input (with pr-28) + absolute button
    → the button is positioned over the input's right area
    → visually it still floats over the search bar   ← not "beside"
```

## Related Files

- `Sohba/Views/Shared/Partials/_Header.cshtml`

## Affected Components

- View — `_Header.cshtml`

## Files That Need Modification

1. `Sohba/Views/Shared/Partials/_Header.cshtml`

## Implementation Plan

### Step 1 — Convert the overlay layout to a flex row

Change the container so the input and the button sit side by side:

- Outer wrapper: `flex items-center gap-2`.
- Input: keep `flex-1`, remove `pr-28`.
- Button: remove `absolute right-1.5 top-1/2 -translate-y-1/2`, keep normal flex position.

### Step 2 — Keep the search icon inside the input

The magnifying-glass icon stays absolutely positioned on the left (`pl-10`).

## Code Changes

### File: Sohba/Views/Shared/Partials/_Header.cshtml

<div style="color:red"><b>REMOVE — the input + absolute button block:</b></div>

```csharp
                        <input type="text"
                               id="searchInput"
                               class="block w-full pl-10 pr-28 py-2 border border-solid border-gray-200 rounded-2xl bg-gray-50 leading-5 placeholder-gray-400
                                        transition-all duration-300 ease-out
                                        focus:outline-none focus:bg-white focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69]
                                        focus:scale-[1.02] focus:shadow-[0_4px_20px_-4px_rgba(52,94,105,0.15)] origin-center sm:text-sm"
                               placeholder="Search for posts, people, groups, pages..."
                               autocomplete="off">
                        <button id="globalSearchBtn"
                                type="button"
                                class="absolute right-1.5 top-1/2 -translate-y-1/2 px-3 py-1.5 bg-[#345e69] text-white text-sm font-semibold rounded-xl hover:bg-[#2a4b55] transition-colors">
                            Search
                        </button>
```

<div style="color:green"><b>REPLACE WITH — a flex row: input flex-1 + button beside it:</b></div>

```csharp
                        <div class="flex items-center gap-2">
                            <div class="relative flex-1">
                                <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                    <svg class="h-5 w-5 text-gray-400 group-focus-within:text-[#345e69] transition-colors duration-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                                    </svg>
                                </div>
                                <input type="text"
                                       id="searchInput"
                                       class="block w-full pl-10 pr-3 py-2 border border-solid border-gray-200 rounded-2xl bg-gray-50 leading-5 placeholder-gray-400
                                                transition-all duration-300 ease-out
                                                focus:outline-none focus:bg-white focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69] sm:text-sm"
                                       placeholder="Search for posts, people, groups, pages..."
                                       autocomplete="off">
                            </div>
                            <button id="globalSearchBtn"
                                    type="button"
                                    class="px-4 py-2 bg-[#345e69] text-white text-sm font-semibold rounded-xl hover:bg-[#2a4b55] transition-colors whitespace-nowrap flex-shrink-0">
                                Search
                            </button>
                        </div>
```

> **Note:** The outer `#quickSearchResults` node remains a sibling positioned under the new
> flex row. In the original markup it was inside the `relative group` div. With this
> restructure, place `#quickSearchResults` inside the outer `div.relative.group` (or the new
> flex row's wrapper) so its `absolute top-full left-0 right-0` still positions correctly
> under the whole search component. Keep the surrounding structure as:

```csharp
                <div class="flex-1 max-w-md mx-8 hidden sm:block relative">
                    <div class="relative group">
                        <div class="flex items-center gap-2"> ... input + button ... </div>
                        <div id="quickSearchResults" class="hidden absolute top-full left-0 right-0 mt-2 ..."></div>
                    </div>
                </div>
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:** Home (logged in) → inspect the header.
- **Expected Results:**
    - The Search button is visually beside the input, not over it.
    - Typing works, Enter submits `/Search?q=...`, and the quick-results dropdown opens
      below the whole search component.
    - The layout remains responsive (mobile hides the desktop bar; the mobile search
      container is unaffected).
- **Failure Conditions:**
    - If the button still overlaps, the flex restructure was not applied.
- **Edge Cases:**
    - Narrow desktop widths: `whitespace-nowrap flex-shrink-0` keeps the button from
      wrapping; `flex-1` lets the input shrink.

<br>
<br>

---

<br>

# Issue 9 — Notification Dropdown Must Route To The Correct Destination

## Issue

The notification dropdown routing is not fully correct for every notification type. The
user reports that reaction/comment/friend-request/group/page notifications must each go to
the correct destination, and the full notifications page must render the actual list.

## Related Feature

- **Feature Name:** Notifications — Dropdown & Full Page.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Notifications.

## Expected Behaviour

```text
Reaction on post     → /Posts/Details/{TargetId}
Comment on post      → /Posts/Details/{TargetId}
Friend request       → /Friends/Requests
Group notification   → /Groups/Details/{TargetId}
Page notification    → /Pages/Details/{TargetId}
Other types          → correct existing destination per app logic
```

The full notifications page must render the actual list, not a placeholder.

## Current Behaviour

- PostLike/PostComment → `/Posts/Details/{TargetId}` ✅ correct.
- FriendRequest → `/Friends/Requests` ✅ correct.
- GroupInvitation → `/Groups/Details/{TargetId}` ✅ correct.
- **SystemAlert → `/Notifications/Index`** ❌ — but group-admin alerts ("joined your
  group") are created with `NotificationType.GroupInvitation` or `SystemAlert` and
  `TargetId = groupId`. A `SystemAlert` with a `TargetId` that is a groupId currently goes to
  the generic notifications index instead of the group.
- **No Page notification type exists.** `NotificationType` enum has only: PostLike,
  PostComment, FriendRequest, GroupInvitation, SystemAlert. No page notification is ever
  created anywhere, so there is no route for it.

## Root Cause

**Routing gap:** `Sohba/wwwroot/js/features/header.js` `getNotificationUrl` and
`Sohba/Views/Notifications/Index.cshtml` `GetNotificationUrl` both treat `SystemAlert` as a
generic fallback even though the application creates SystemAlert notifications whose
`TargetId` is a group id (e.g. `GroupService.LeaveGroupAsync` uses
`NotificationType.SystemAlert` with `targetId: groupId`).

**Missing type:** The enum has no `PageNotification`/`PageFollow` value, and no service
creates a page-follow notification. Therefore a "page notification → /Pages/Details/{id}"
cannot be distinguished from other types with the current data model.

## Execution Flow

```
Group admin alert created
    → GroupService.JoinGroupAsync
        → CreateNotificationAsync(receiverId: admin, type: GroupInvitation, targetId: groupId)
        → routed to /Groups/Details/{targetId} ✅

Group admin alert created by LeaveGroupAsync
    → CreateNotificationAsync(receiverId: group.AdminId, type: SystemAlert, senderId, targetId: groupId)
        → getNotificationUrl sees SystemAlert
        → returns /Notifications/Index     ❌ should be /Groups/Details/{groupId}

Page notification
    → Not created anywhere
    → No enum value, no DTO mapping, no route ❌
```

## Related Files

- `Sohba/wwwroot/js/features/header.js`
- `Sohba/Views/Notifications/Index.cshtml`
- `Sohba.Domain/Enums/NotificationType.cs`
- `Sohba.Application/Services/NotificationService.cs`
- `Sohba.Application/DTOs/UserAggregate/NotificationResponseDto.cs`

## Affected Components

- JavaScript — `features/header.js`
- View — `Notifications/Index.cshtml`
- Domain Enum — `NotificationType.cs`
- Application Service — `NotificationService.cs` (used only if a Page type is added)

## Files That Need Modification

1. `Sohba/wwwroot/js/features/header.js`
2. `Sohba/Views/Notifications/Index.cshtml`
3. `Sohba.Domain/Enums/NotificationType.cs` (documented only — see note)

## Implementation Plan

### Step 1 — Fix `SystemAlert` routing

When a `SystemAlert` notification has a non-null `TargetId`, route it to
`/Groups/Details/{TargetId}` because the only SystemAlert notifications currently created in
the app carry a group target. When `TargetId` is null, fall back to `/Notifications/Index`.

### Step 2 — Keep the post/comment/friend/group routes

The existing PostLike/PostComment/FriendRequest/GroupInvitation branches are correct and
must stay.

### Step 3 — Document the Page notification gap

There is NO page notification type in the app. To support it, a new enum value
(`PageFollow` or `PageNotification`) and a corresponding `createNotificationAsync` call in
`PageService.FollowPageAsync` would be required, plus a JSON mapping in
`MappingProfile`. This is documented as a required backend addition — it cannot be fixed
purely in JS because `NotificationType` has no page value and no page notification rows
exist.

### Step 4 — Verify full page

The full `/Notifications/Index` already renders the model as a clickable list (Issue 9
FixesV5 was applied). Only the `GetNotificationUrl` Razor helper needs the same
SystemAlert-with-target fix.

## Code Changes

### File: Sohba/wwwroot/js/features/header.js

<div style="color:red"><b>REMOVE — the current getNotificationUrl:</b></div>

```javascript
function getNotificationUrl(notif) {
    const type = notif.notificationType;
    const targetId = notif.targetId || '';

    if (type === 'PostLike' || type === 'PostComment') return `/Posts/Details/${targetId}`;
    if (type === 'GroupInvitation') return `/Groups/Details/${targetId}`;
    if (type === 'FriendRequest') return '/Friends/Requests';
    return '/Notifications/Index'; // SystemAlert / default
}
```

<div style="color:green"><b>REPLACE WITH — SystemAlert carries a group target when present:</b></div>

```javascript
function getNotificationUrl(notif) {
    const type = notif.notificationType;
    const targetId = notif.targetId || '';

    if (type === 'PostLike' || type === 'PostComment') return `/Posts/Details/${targetId}`;
    if (type === 'GroupInvitation') return `/Groups/Details/${targetId}`;
    if (type === 'FriendRequest') return '/Friends/Requests';
    // SystemAlert notifications created by the app carry a group target
    // (e.g. "X left the group"). Route to the group when TargetId exists.
    if (type === 'SystemAlert' && targetId) return `/Groups/Details/${targetId}`;
    return '/Notifications/Index';
}
```

### File: Sohba/Views/Notifications/Index.cshtml

<div style="color:red"><b>REMOVE — the Razor URL helper that sends SystemAlert to the index:</b></div>

```csharp
    private string GetNotificationUrl(Sohba.Application.DTOs.UserAggregate.NotificationResponseDto n)
    {
        return n.NotificationType switch
        {
            "PostLike" or "PostComment" => $"/Posts/Details/{n.TargetId}",
            "GroupInvitation" => $"/Groups/Details/{n.TargetId}",
            "FriendRequest" => "/Friends/Requests",
            _ => "/Notifications/Index"
        };
    }
```

<div style="color:green"><b>REPLACE WITH — SystemAlert with a target routes to the group:</b></div>

```csharp
    private string GetNotificationUrl(Sohba.Application.DTOs.UserAggregate.NotificationResponseDto n)
    {
        return n.NotificationType switch
        {
            "PostLike" or "PostComment" => $"/Posts/Details/{n.TargetId}",
            "GroupInvitation" => $"/Groups/Details/{n.TargetId}",
            "FriendRequest" => "/Friends/Requests",
            "SystemAlert" when n.TargetId.HasValue => $"/Groups/Details/{n.TargetId}",
            _ => "/Notifications/Index"
        };
    }
```

### File: Sohba.Domain/Enums/NotificationType.cs

<div style="color:green"><b>ADD — for future page-notification support (documented; requires service + mapping):</b></div>

```csharp
    public enum NotificationType
    {
        PostLike = 1,
        PostComment = 2,
        FriendRequest = 3,
        GroupInvitation = 4,
        SystemAlert = 5,
        PageFollow = 6   // NEW: page follow notifications (requires PageService mapping)
    }
```

> **Important:** Adding `PageFollow` alone is NOT sufficient. To make page-notification
> routing work end-to-end, a page-follow notification must actually be created in
> `PageService.FollowPageAsync` (or wherever page follows are handled) with
> `targetId = pageId`, and the JSON must serialize the new enum. Currently no such call
> exists, so the page-notification case in the user's issue cannot route without that
> backend call. This is documented as the required backend addition.

## Regression Testing

- **Test Users:** `mohammed@sohba.com` (admin of a group; has PostLike + FriendRequest
  notifications).
- **Navigation:** Header bell → dropdown; `/Notifications/Index`.
- **Expected Results:**
    - PostLike/PostComment → `/Posts/Details/{id}`.
    - FriendRequest → `/Friends/Requests`.
    - GroupInvitation → `/Groups/Details/{id}`.
    - SystemAlert with a group `TargetId` (e.g. "X left the group") → `/Groups/Details/{id}`.
    - SystemAlert with no TargetId → `/Notifications/Index`.
    - Full page renders the actual list (already implemented in FixesV5) with matching
      links.
- **Failure Conditions:**
    - If a group-leave SystemAlert still goes to `/Notifications/Index`, the new branch was
      not applied.
- **Edge Cases:**
    - `TargetId` null for FriendRequest → `/Friends/Requests` (unchanged).
    - Unknown type → `/Notifications/Index` (unchanged).

<br>
<br>

---

<br>

# Issue 10 — Home Feed Post Duplication Came Back

## Issue

The duplicate-post problem on the Home feed has returned — posts appear more than once.

## Related Feature

- **Feature Name:** Home Feed / Timeline.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Home feed.

## Expected Behaviour

- Each post appears exactly once in the feed.
- Infinite scroll / Load More must not re-append posts already rendered.

## Current Behaviour

- After scrolling and loading more, previously-visible posts can appear again.

## Root Cause

Two distinct causes:

**Cause A — the local dedup Set is never populated with the initial page.**

`Sohba/wwwroot/js/features/feed.js` defines:

```javascript
const renderedPostIds = new Set();
function collectRenderedPostIds() { ... }
```

but `collectRenderedPostIds()` is **never called** on `DOMContentLoaded`. The Set starts
empty, so when `loadMorePosts()` filters with `renderedPostIds.has(id)`, it can still append
a card that was already rendered on the initial server-rendered page. (The load-more code
does call `renderedPostIds.add(id)` for new cards, but the initial page cards were never
added.)

**Cause B — `GetTimelineAsync` orders only by `CreatedAt` (no secondary key).**

`Sohba.Infrastructure/Repositories/PostRepository.cs`:

```csharp
.OrderByDescending(p => p.CreatedAt);
```

With `Skip/Take` pagination and multiple posts sharing the same `CreatedAt` (or new posts
created between page requests shifting the offset), the same post can appear on page 1 and
page 2 of the timeline.

## Execution Flow

```
GET /Home
    → HomeController.Index → GetFeedAsync(page=1)
        → GetTimelineAsync → OrderByDescending(CreatedAt).Skip(0).Take(10)   → page 1 cards rendered
    → feed.js DOMContentLoaded
        → no call to collectRenderedPostIds()                                  ← BUG A: set empty

User scrolls / Load More
    → loadMorePosts() → GET /Home/GetPostCards?page=2
        → GetTimelineAsync → OrderByDescending(CreatedAt).Skip(10).Take(10)
            → posts with equal CreatedAt may shift → same post as page 1         ← BUG B
        → renderedPostIds is empty for initial cards
        → duplicate card appended                                                ← visible duplication
```

## Related Files

- `Sohba/wwwroot/js/features/feed.js`
- `Sohba.Infrastructure/Repositories/PostRepository.cs`
- `Sohba/Controllers/HomeController.cs`

## Affected Components

- JavaScript — `features/feed.js`
- Infrastructure Repository — `PostRepository.cs`

## Files That Need Modification

1. `Sohba/wwwroot/js/features/feed.js`
2. `Sohba.Infrastructure/Repositories/PostRepository.cs`

## Implementation Plan

### Step 1 — Call `collectRenderedPostIds()` on page load

Call it inside the `DOMContentLoaded` handler before wiring the load-more/infinite scroll.

### Step 2 — Add a deterministic secondary sort to the timeline query

Change `OrderByDescending(p => p.CreatedAt)` to
`OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)`. This makes the page
boundaries stable across page requests.

## Code Changes

### File: Sohba/wwwroot/js/features/feed.js

<div style="color:red"><b>REMOVE — the DOMContentLoaded handler without the initial dedup:</b></div>

```javascript
document.addEventListener('DOMContentLoaded', function () {
    // Get initial page from URL or default to 1
    const urlParams = new URLSearchParams(window.location.search);
    currentPage = parseInt(urlParams.get('page')) || 1;

    // Check if there's a "Load More" button (for non-infinite scroll)
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    if (loadMoreBtn) {
        loadMoreBtn.addEventListener('click', function (e) {
            e.preventDefault();
            if (!isLoading && hasMore) {
                loadMorePosts();
            }
        });
    }

    //  Setup infinite scroll if no load more button
    if (!loadMoreBtn) {
        setupInfiniteScroll();
    }
});
```

<div style="color:green"><b>REPLACE WITH — call collectRenderedPostIds() first:</b></div>

```javascript
document.addEventListener('DOMContentLoaded', function () {
    // Record posts already rendered by the server so load-more never duplicates them.
    collectRenderedPostIds();

    // Get initial page from URL or default to 1
    const urlParams = new URLSearchParams(window.location.search);
    currentPage = parseInt(urlParams.get('page')) || 1;

    // Check if there's a "Load More" button (for non-infinite scroll)
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    if (loadMoreBtn) {
        loadMoreBtn.addEventListener('click', function (e) {
            e.preventDefault();
            if (!isLoading && hasMore) {
                loadMorePosts();
            }
        });
    }

    //  Setup infinite scroll if no load more button
    if (!loadMoreBtn) {
        setupInfiniteScroll();
    }
});
```

### File: Sohba.Infrastructure/Repositories/PostRepository.cs

<div style="color:red"><b>REMOVE — the unstable ordering:</b></div>

```csharp
                .OrderByDescending(p => p.CreatedAt);
```

<div style="color:green"><b>REPLACE WITH — a deterministic secondary sort (Id):</b></div>

```csharp
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id);
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Required data:** > 10 posts in the timeline (some with the same CreatedAt).
- **Navigation:** `/Home` → scroll / click Load More until the end.
- **Expected Results:**
    - No post appears twice in the entire feed.
    - Page 1 and page 2 have no overlapping post ids.
- **Failure Conditions:**
    - If duplication persists, either `collectRenderedPostIds()` is not called, or the query
      still lacks `ThenByDescending(p => p.Id)`.
- **Edge Cases:**
    - Posts created seconds apart (same `CreatedAt` value) — the Id secondary sort keeps
      them in stable order across pages.
    - A post favorited/saved that had its interaction data re-mapped — the same post must
      not be duplicated by the frontend dedup.

<br>
<br>

---

<br>

# Issue 11 — Reply On Reply Returns Retrieval Error

## Issue

When creating a reply on a reply, the UI shows "Comment created but could not be retrieved."
However, when the reply form is closed and reopened, the replies load correctly.

## Related Feature

- **Feature Name:** Post Details / Comments — Nested Replies.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.5 (Comments — Reply & Delete).

## Expected Behaviour

- Reply on reply works.
- The created reply is returned immediately.
- The modal/thread updates without requiring close and reopen.

## Current Behaviour

- The reply is successfully created in the DB.
- The controller fails to locate it in the returned tree:
  `SelectMany(c => c.Replies)` only searches ONE level deep, so a level-3 reply (reply on a
  reply) is never found.
- The controller returns "Comment created but could not be retrieved."

## Root Cause

`Sohba/Controllers/PostsController.cs` — `Comment` action reply lookup:

```csharp
if (request.ParentCommentId.HasValue)
{
    latest = comments
              .SelectMany(c => c.Replies)
              .Where(r => r.ParentCommentId == request.ParentCommentId)
              .OrderByDescending(r => r.CreatedAt)
              .FirstOrDefault();
}
```

After FixesV5, the comment tree is recursive:

```text
Comment (level 1)
└── Reply (level 2)
    └── Reply-on-Reply (level 3)   ← parent is the level-2 reply
```

`SelectMany(c => c.Replies)` flattens ONLY the top-level comments' immediate replies
(level 2). A level-3 reply is nested inside `c.Replies[i].Replies` — it is not in the
flattened array, so `latest == null` → error.

## Execution Flow

```
User replies to a reply (parent commentId = level-2 reply id)
    → SubmitReply → POST /Posts/Comment { postId, content, parentCommentId }
        → AddCommentAsync → creates level-3 reply in DB → OK
        → GetCommentsByPostIdAsync → recursive tree (levels 1-4)
        → latest = comments.SelectMany(c => c.Replies)...   // only level 2
            // level-3 reply not in the flattened result      ← BUG
        → latest == null
        → "Comment created but could not be retrieved."       ← error shown
```

## Related Files

- `Sohba/Controllers/PostsController.cs`
- `Sohba.Application/Services/InteractionService.cs`
- `Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs`

## Affected Components

- Controller — `PostsController.cs`

## Files That Need Modification

1. `Sohba/Controllers/PostsController.cs`

## Implementation Plan

### Step 1 — Recursively find the newly created comment by its Id

Instead of the one-level `SelectMany`, walk the whole tree recursively and return the node
whose `Id == result comment id`. Since `AddCommentAsync` currently returns `Result` (not the
created DTO), the controller must find it in the returned tree.

The most robust approach:

```csharp
CommentResponseDto FindNode(IEnumerable<CommentResponseDto> nodes, Guid id)
{
    foreach (var node in nodes)
    {
        if (node.Id == id) return node;
        var child = FindNode(node.Replies ?? new List<CommentResponseDto>(), id);
        if (child != null) return child;
    }
    return null;
}
```

Then:

```csharp
latest = FindNode(comments, newCommentId);
```

### Step 2 — (Recommended) Have `AddCommentAsync` return the created DTO

A cleaner backend fix: change `AddCommentAsync` to return `Result<CommentResponseDto>` (the
mapped, depth/IsAuthor-set comment). The controller would then use `result.Value` directly,
eliminating the tree search entirely. However, that is a larger interface change; the
recursive search is the minimal, isolated fix that unblocks the flow. Both are documented —
the recursive search is the recommended minimal change.

## Code Changes

### File: Sohba/Controllers/PostsController.cs

<div style="color:red"><b>REMOVE — the one-level reply lookup in the Comment action:</b></div>

```csharp
            var comments = await _interactionService.GetCommentsByPostIdAsync(request.PostId, userId); // I Added Request.UserID To Avoid Run Errors 
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
```

<div style="color:green"><b>REPLACE WITH — a recursive tree search by the new comment id:</b></div>

```csharp
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
```

<div style="color:green"><b>ADD — the local recursive helper (inside CommentsController or as a local function inside the action):</b></div>

```csharp
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
```

> **Note:** `latestCommentId` is the id of the just-created comment. It can be captured by
> having `AddCommentAsync` return it in the `Result` (not currently possible) — the
> minimal workaround is to capture the id BEFORE the tree search via a small change to
> `AddCommentAsync` OR by finding the newest comment in the tree that belongs to this post's
> current user. The correct, clean approach is to change `AddCommentAsync` to return the
> created `CommentResponseDto` (documented as the recommended follow-up). For the minimal
> unblocking fix, add a comment id capture as follows:

> **Recommended companion change — make `AddCommentAsync` return the created id:**
>
> Change the service signature to `Task<Result<Guid>> AddCommentAsync(...)` so the controller
> can do:
>
> ```csharp
> var addResult = await _interactionService.AddCommentAsync(userId, request.PostId, request.Content, request.ParentCommentId);
> if (!addResult.IsSuccess) return Json(new { success = false, error = addResult.Error });
> var latestCommentId = addResult.Value;
> ```
>
> Then `latest = FindCommentNode(topLevel, latestCommentId)` is exact and unambiguous.
>
> If changing the signature is undesirable, a minimal alternative is to search the tree for
> the CURRENT user's newest comment whose `Content == request.Content` and
> `ParentCommentId == request.ParentCommentId`. This is less robust but avoids the interface
> change. The signature change is the recommended fix.

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Required data:** A post with a comment (level 1) that already has one reply (level 2).
- **Navigation:** Home → open post modal → Reply to the level-2 reply.
- **Expected Results:**
    - The reply-on-reply is created and returned immediately (no "could not be retrieved").
    - The modal updates in place with the new level-3 reply.
- **Failure Conditions:**
    - If the error still appears, the controller still uses `SelectMany(c => c.Replies)`.
- **Edge Cases:**
    - Top-level comment creation (no `ParentCommentId`) — the `else` branch
      (`comments.FirstOrDefault()`) should be preserved for level-1 comments. The recursive
      search handles both paths correctly because a level-1 comment is found by id at the
      root level too.
    - Level-4 reply creation (max depth) — the domain rule rejects level 5 before the
      controller retrieval code runs; no regression.
    - Reply on a deleted parent — the domain/service layer rejects it before retrieval.

<br>
<br>

---

<br>

# Appendix — Full File Inventory

| Layer | Path |
|-------|------|
| View | `Sohba/Views/Shared/Partials/_PostCard.cshtml` |
| View | `Sohba/Views/Shared/Partials/_Header.cshtml` |
| View | `Sohba/Views/Posts/SavedPosts.cshtml` |
| View | `Sohba/Views/Friends/Index.cshtml` |
| View | `Sohba/Views/Friends/Requests.cshtml` |
| View | `Sohba/Views/Notifications/Index.cshtml` |
| Controller | `Sohba/Controllers/PostsController.cs` |
| Controller | `Sohba/Controllers/FriendsController.cs` |
| Controller | `Sohba/Controllers/HomeController.cs` |
| Controller | `Sohba/Controllers/BaseController.cs` |
| Application Service | `Sohba.Application/Services/InteractionService.cs` |
| Application Service | `Sohba.Application/Services/PostService.cs` |
| Application Service | `Sohba.Application/Services/GroupService.cs` |
| Application Interface | `Sohba.Application/Interfaces/IInteractionService.cs` |
| Application DTO | `Sohba.Application/DTOs/PostAggregate/SavedPostsGroupedDto.cs` |
| Application DTO | `Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs` |
| Application DTO | `Sohba.Application/DTOs/Common/PagedResult.cs` |
| Application DTO | `Sohba.Application/DTOs/Common/BaseResponseDto.cs` |
| Domain Enum | `Sohba.Domain/Enums/NotificationType.cs` |
| Domain Entity | `Sohba.Domain/Entities/GroupAndPage/GroupMember.cs` |
| Domain Interface | `Sohba.Domain/Interfaces/IGroupRepository.cs` |
| Infrastructure Repository | `Sohba.Infrastructure/Repositories/GroupRepository.cs` |
| Infrastructure Repository | `Sohba.Infrastructure/Repositories/PostRepository.cs` |
| JS | `Sohba/wwwroot/js/sohba-posts.js` |
| JS | `Sohba/wwwroot/js/features/friends.js` |
| JS | `Sohba/wwwroot/js/features/feed.js` |
| JS | `Sohba/wwwroot/js/features/header.js` |

<br>
<br>

---

<br>

# Additional Notes

1. **Issues 1 and 2 share the same root cause.** The `IsSaved` computation in
   `InteractionService.MapPostsToResponse` and `PostService.GetPostByIdAsync` includes
   Favorites rows. Both must be changed to `IsSaved = tag != Favorite` logic.
   `GetSavedPostsByTagAsync` also force-sets `IsSaved = true` and must be changed to
   `IsSaved = tag != Favorite`.

2. **Issue 4 requires no migration.** The fix is purely about how the entity graph is
   tracked/attached. `GetMemberByUserAndGroupAsync` queries `GroupMember` directly without
   navigation includes, which avoids the duplicate `User` tracking conflict.

3. **Issue 9 — Page notifications require a backend addition.** There is no
   `PageNotification`/`PageFollow` value in `NotificationType`, and no code path creates a
   page-follow notification. The `PageFollow` enum value alone is insufficient; a
   `PageService` notification call and a mapping entry must also be added.

4. **Issue 10 has two fixes, both required.** The frontend must seed `renderedPostIds`
   from the initial server-rendered cards, AND the timeline query must have a deterministic
   secondary sort (`ThenByDescending(p => p.Id)`) so pagination boundaries are stable.

5. **Issue 11 — minimal vs clean fix.** The minimal fix is a recursive tree search in the
   controller. The recommended clean fix is changing `AddCommentAsync` to return
   `Result<Guid>` (the created comment id) so the controller can look it up precisely. Both
   are documented; the user may choose the minimal one to unblock testing.

6. **No migration is required** for any of the eleven fixes in this document. All changes
   are C# logic, JavaScript, or Razor markup.

<br>
<br>

---

<br>

# End Of Document

This document is a complete implementation guide for the eleven issues listed above. No
project source files were modified while producing it.