# Sohba — Fixesv5 Implementation Guide

<br>
<br>
<br>

**Document Name:** Fixesv5.md

**Purpose:** Complete implementation guide for the seven additional issues discovered while
continuing the frontend test plan after `FixesV4.md` was applied.

**Scope:** This document ONLY addresses the seven issues listed in the request:

1. **Issue 1 — Save Button Does Not Toggle Saved State** (frontend + backend).
2. **Issue 2 — Group Join Button Does Not Update After Joining** (frontend + backend).
3. **Issue 3 — Accept / Decline Friend Request Returns "No pending friend request found"**
   (frontend).
4. **Issue 4 — Home Search Button UI Position** (view/CSS only).
5. **Issue 5 — Notification Dropdown Must Be Fully Clickable** (frontend only).
6. **Issue 6 — Deleted Reply Does Not Immediately Disappear** (frontend only).
7. **Issue 7 — Replies Must Support Nested Replies With Maximum Depth 4** (frontend +
   backend/domain).

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
3. [Issue 1 — Save Button Does Not Toggle Saved State](#issue-1--save-button-does-not-toggle-saved-state)
4. [Issue 2 — Group Join Button Does Not Update After Joining](#issue-2--group-join-button-does-not-update-after-joining)
5. [Issue 3 — Accept / Decline Friend Request Returns "No pending friend request found"](#issue-3--accept--decline-friend-request-returns-no-pending-friend-request-found)
6. [Issue 4 — Home Search Button UI Position](#issue-4--home-search-button-ui-position)
7. [Issue 5 — Notification Dropdown Must Be Fully Clickable](#issue-5--notification-dropdown-must-be-fully-clickable)
8. [Issue 6 — Deleted Reply Does Not Immediately Disappear](#issue-6--deleted-reply-does-not-immediately-disappear)
9. [Issue 7 — Replies Must Support Nested Replies With Maximum Depth 4](#issue-7--replies-must-support-nested-replies-with-maximum-depth-4)
10. [Appendix — Full File Inventory](#appendix--full-file-inventory)

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

# Issue 1 — Save Button Does Not Toggle Saved State

## Issue

After successfully saving a post to a collection, clicking the **Save** button on the same
post again does not remove the post. The collection selection UI appears again and asks
where to save the post, even though the post is already saved.

## Related Feature

- **Feature Name:** Post Actions — Save Post / Favorites.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.10 (Save & Favorites).

## Expected Behaviour

The Save button must behave as a toggle:

```text
Not Saved
    ↓
Click Save
    ↓
Select Collection
    ↓
Saved Successfully
    ↓
Click Save Again
    ↓
Remove From Saved
```

- If `IsSaved = true`, clicking Save should remove the existing saved state instead of
  opening the collection selector.
- The collection selection behavior must only happen when `IsSaved = false`.
- Removing from Saved must NOT remove the post from Favorites (preserve the
  collection + Favorites independence).

## Current Behaviour

- `_PostCard.cshtml` always calls `SohbaApp.openSavePostModal('@post.Id')` when the Save
  button is clicked, regardless of `post.IsSaved`.
- `IsSaved` is only used to change styling/text, never behavior.
- Clicking Save on an already-saved post re-opens the collection modal instead of removing
  the post.

## Root Cause

**Frontend:** `Sohba/Views/Shared/Partials/_PostCard.cshtml` line 142:

```csharp
<button data-save-button="@post.Id"
        onclick="SohbaApp.openSavePostModal('@post.Id')"
```

There is no JavaScript `toggleSavePost` behavior. The click handler unconditionally opens
the collection modal. The `IsSaved` value is available in the Razor model but is only used
for the icon/text and amber styling, not for the click behavior.

**Backend:** There is no endpoint that removes a post from the user's collections while
preserving the Favorites membership. The existing `RemoveSavedPostAsync` removes only the
FIRST matching `SavedPost` row (via `GetSavedPostAsync` → `FirstOrDefaultAsync`). To remove
the post from all collections while keeping it in Favorites, a new method is required.

## Execution Flow

```
User opens post menu on an already-saved post
    → _PostCard.cshtml renders data-save-button with post.IsSaved = true
    → User clicks "Save Post"
        → onclick="SohbaApp.openSavePostModal('{postId}')"   ← always, even when saved
        → modal opens again
    → Expected: onclick should call toggleSavePost, which:
        → IsSaved == true  → POST /Posts/RemoveFromSaved → removes non-Favorite rows
        → IsSaved == false → openSavePostModal(postId)
```

## Related Files

- `Sohba/Views/Shared/Partials/_PostCard.cshtml`
- `Sohba/wwwroot/js/sohba-posts.js`
- `Sohba/Controllers/PostsController.cs`
- `Sohba.Application/Services/InteractionService.cs`
- `Sohba.Application/Interfaces/IInteractionService.cs`
- `Sohba.Domain/Entities/PostAggregate/SavedPost.cs`

## Affected Components

- View — `_PostCard.cshtml`
- JavaScript — `sohba-posts.js`
- Controller — `PostsController.cs`
- Application Service — `InteractionService.cs`
- Application Interface — `IInteractionService.cs`

## Files That Need Modification

1. `Sohba.Application/Interfaces/IInteractionService.cs`
2. `Sohba.Application/Services/InteractionService.cs`
3. `Sohba/Controllers/PostsController.cs`
4. `Sohba/wwwroot/js/sohba-posts.js`
5. `Sohba/Views/Shared/Partials/_PostCard.cshtml`

## Implementation Plan

### Step 1 — Add `RemoveSavedPostsFromCollectionsAsync` to `IInteractionService`

A method that removes ALL `SavedPost` rows for the given user+post where `Tag != Favorite`.
This preserves the Favorites membership independently while un-saving from all named
collections.

### Step 2 — Implement it in `InteractionService`

Query all saved rows for the user+post (not just the first), remove every row whose
`Tag != SavedTag.Favorite`, and `CompleteAsync`.

### Step 3 — Add `RemoveFromSaved` action in `PostsController`

`POST /Posts/RemoveFromSaved` accepting `{ postId }`, calling the new service method.

### Step 4 — Add `SohbaApp.toggleSavePost` in `sohba-posts.js`

If `isSaved` is true → call `/Posts/RemoveFromSaved`, then `updateSaveFavoriteButtons(postId, false, isFavorite)`.
If `isSaved` is false → call `SohbaApp.openSavePostModal(postId)`.

### Step 5 — Update `_PostCard.cshtml` Save button

Change the click handler from `openSavePostModal` to `toggleSavePost` and pass the current
`post.IsSaved` state.

## Code Changes

### File: Sohba.Application/Interfaces/IInteractionService.cs

<div style="color:red"><b>REMOVE — the current Saved Posts section end:</b></div>

```csharp
        Task<Result<IEnumerable<SavedPostsGroupedDto>>> GetSavedPostsGroupedAsync(Guid userId);
```

<div style="color:green"><b>REPLACE WITH — add the new un-save method:</b></div>

```csharp
        Task<Result<IEnumerable<SavedPostsGroupedDto>>> GetSavedPostsGroupedAsync(Guid userId);

        // Removes the post from ALL the user's collections but KEEPS it in Favorites.
        Task<Result> RemoveSavedPostsFromCollectionsAsync(Guid userId, Guid postId);
```

### File: Sohba.Application/Services/InteractionService.cs

<div style="color:red"><b>REMOVE — end of RemoveSavedPostAsync:</b></div>

```csharp
        public async Task<Result> RemoveSavedPostAsync(Guid userId, Guid postId)
        {
            var existingSave = await _unitOfWork.Interactions.GetSavedPostAsync(userId, postId);
            if (existingSave == null) return Result.Failure("Post is not saved.");

            _unitOfWork.Interactions.RemoveSavedPost(existingSave);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
```

<div style="color:green"><b>REPLACE WITH — add the new method after it:</b></div>

```csharp
        public async Task<Result> RemoveSavedPostAsync(Guid userId, Guid postId)
        {
            var existingSave = await _unitOfWork.Interactions.GetSavedPostAsync(userId, postId);
            if (existingSave == null) return Result.Failure("Post is not saved.");

            _unitOfWork.Interactions.RemoveSavedPost(existingSave);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        // Removes the post from ALL the user's collections but KEEPS the Favorites membership.
        public async Task<Result> RemoveSavedPostsFromCollectionsAsync(Guid userId, Guid postId)
        {
            var savedPosts = (await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId))
                .Where(s => s.PostId == postId && s.Tag != SavedTag.Favorite)
                .ToList();

            if (savedPosts.Count == 0)
                return Result.Success(); // Nothing to remove from collections (still favorited).

            foreach (var savedPost in savedPosts)
            {
                _unitOfWork.Interactions.RemoveSavedPost(savedPost);
            }

            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
```

### File: Sohba/Controllers/PostsController.cs

<div style="color:red"><b>REMOVE — the current RemoveSavedPost action:</b></div>

```csharp
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
```

<div style="color:green"><b>REPLACE WITH — keep the legacy action and add the new one:</b></div>

```csharp
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
```

### File: Sohba/wwwroot/js/sohba-posts.js

<div style="color:red"><b>REMOVE — the old unconditional openSavePostModal usage (the function below remains for creating saves):</b></div>

```javascript
window.SohbaApp.openSavePostModal = async function (postId) {
```

<div style="color:green"><b>ADD — a new toggle function BEFORE openSavePostModal:</b></div>

```javascript
// Toggle Save behaviour: if already saved -> remove from collections; otherwise -> open the picker.
window.SohbaApp.toggleSavePost = async function (postId, isSaved) {
    if (isSaved) {
        try {
            const result = await window.SohbaApp.post('/Posts/RemoveFromSaved', { postId });

            if (result.success) {
                const favBtn = document.querySelector(`[data-fav-button="${postId}"]`);
                const isFavorite = favBtn && favBtn.classList.contains('text-pink-600');
                updateSaveFavoriteButtons(postId, false, isFavorite);
                window.SohbaApp.toast('Removed from saved', 'success');
            } else {
                window.SohbaApp.toast(result.error || 'Failed to remove from saved', 'error');
            }
        } catch (error) {
            console.error('Remove saved error:', error);
            window.SohbaApp.toast('Network error', 'error');
        }
    } else {
        window.SohbaApp.openSavePostModal(postId);
    }
};
```

### File: Sohba/Views/Shared/Partials/_PostCard.cshtml

<div style="color:red"><b>REMOVE — the Save button that always opens the modal:</b></div>

```csharp
                                    <button data-save-button="@post.Id"
                                            onclick="SohbaApp.openSavePostModal('@post.Id')"
                                            class="w-full flex items-center gap-3 px-4 py-2.5 hover:bg-slate-50 text-slate-700 text-sm @(post.IsSaved ? "text-amber-600 bg-amber-50" : "")">
```

<div style="color:green"><b>REPLACE WITH — a toggle that checks the saved state:</b></div>

```csharp
                                    <button data-save-button="@post.Id"
                                            onclick="SohbaApp.toggleSavePost('@post.Id', @(post.IsSaved ? "true" : "false"))"
                                            class="w-full flex items-center gap-3 px-4 py-2.5 hover:bg-slate-50 text-slate-700 text-sm @(post.IsSaved ? "text-amber-600 bg-amber-50" : "")">
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:**
    1. Login → Home feed → open a post menu → click "Save Post" → select a collection.
    2. Toast "Post saved to collection!" appears and button text becomes "Saved".
    3. Open the same menu again → click "Save Post" (now labelled "Saved").
- **Expected Results:**
    - The collection modal does NOT open; instead the post is removed from the collection.
    - Toast "Removed from saved" appears; button text returns to "Save Post".
    - The post no longer appears on `/Posts/SavedPosts` (unless favorited).
- **Failure Conditions:**
    - If the modal still opens when saved, `toggleSavePost` is not wired to the button.
    - If the post disappears from Favorites too, `RemoveSavedPostsFromCollectionsAsync`
      removed the Favorite row (it must NOT).
- **Edge Cases:**
    - Post saved to BOTH a collection and Favorites → clicking Save removes the collection
      row but keeps the Favorite row; `IsSaved` remains true (favorited), `IsFavorite` stays
      true.
    - Post only in Favorites (no collection row) → clicking Save is a no-op success, and
      `IsSaved`/`IsFavorite` remain true.

<br>
<br>

---

<br>

# Issue 2 — Group Join Button Does Not Update After Joining

## Issue

On `/Groups/Details`, when a user is already a member of a group, the UI still displays
**Join Group** instead of **Leave Group**. The same problem occurs immediately after a
successful join.

## Related Feature

- **Feature Name:** Groups — Details / Membership.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Groups section.

## Expected Behaviour

- Non-member → **Join Group**.
- Member → **Leave Group**.
- Admin/Owner → show the existing admin/owner state (Edit Group + Leave Group or the
  appropriate current design) — NEVER "Join" for an admin.
- The UI must update immediately after a successful join without reloading/reopening.

## Current Behaviour

- `Details.cshtml` renders the button based on `Model.Group.IsCurrentUserMember`.
- `GroupService.GetGroupByIdAsync` never sets `IsCurrentUserMember`, so it is always
  `false` by default → the view always renders the "Join Group" branch.
- After joining, `GroupsController.Join` only returns `{ success }`; there is no local
  `joinGroup` in `Details.cshtml` that swaps the button. The page relies on `_Sidebar`'s
  `joinGroup` (which reloads the page), so even a reload can still show "Join" if
  `IsCurrentUserMember` stays false.

## Root Cause

**Backend:** `Sohba.Application/Services/GroupService.cs`, `GetGroupByIdAsync` (line 126):

```csharp
public async Task<Result<GroupResponseDto>> GetGroupByIdAsync(Guid groupId)
{
    var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
    if (group == null) return Result<GroupResponseDto>.Failure("Group not found.");

    var response = _mapper.Map<GroupResponseDto>(group);
    return Result<GroupResponseDto>.Success(response);
}
```

It never computes `IsCurrentUserMember`. The repository `GetByIdAsync` already includes
`GroupMembers` (verified in `GroupRepository.GetByIdAsync` → `.Include(g => g.GroupMembers)`
and `.ThenInclude(m => m.User)`), so the data is available — the service just doesn't use it.

**Controller:** `GroupsController.Details` (line 109) calls `GetGroupByIdAsync(id)` without
the current user id, so the service cannot compute membership even if it wanted to.

**Frontend:** `Details.cshtml` has `joinGroup('@Model.Group.Id')` in the inline handler
(line 56) but the `joinGroup` function referenced is the one in `_Sidebar.cshtml`, which
does a full page reload and is not tailored to swap the Details button.

## Execution Flow

```
GET /Groups/Details/{id}
    → GroupsController.Details
        → _groupService.GetGroupByIdAsync(id)          // NO currentUserId passed
            → IsCurrentUserMember = false (default)    ← BUG
        → ViewModel.Group.IsCurrentUserMember = false
    → Details.cshtml renders "Join Group" even for members/admins

User clicks Join Group
    → joinGroup(groupId) from _Sidebar (global)
        → POST /Groups/Join
            → JoinGroupAsync → adds GroupMember row → success
        → location.reload()
    → Details reloads → GetGroupByIdAsync still returns IsCurrentUserMember=false
        → still shows "Join Group"                      ← BUG
```

## Related Files

- `Sohba/Controllers/GroupsController.cs`
- `Sohba.Application/Services/GroupService.cs`
- `Sohba.Application/DTOs/GroupAndPageAggregate/GroupResponseDto.cs`
- `Sohba.Infrastructure/Repositories/GroupRepository.cs`
- `Sohba/Views/Groups/Details.cshtml`
- `Sohba/Views/Shared/Partials/_Sidebar.cshtml`

## Affected Components

- Controller — `GroupsController.cs`
- Application Service — `GroupService.cs`
- DTO — `GroupResponseDto.cs` (already has `IsCurrentUserMember` — no change needed)
- Repository — `GroupRepository.cs` (already loads GroupMembers — no change needed)
- View — `Details.cshtml`

## Files That Need Modification

1. `Sohba.Application/Services/GroupService.cs`
2. `Sohba/Controllers/GroupsController.cs`
3. `Sohba/Views/Groups/Details.cshtml`

## Implementation Plan

### Step 1 — Add an overload `GetGroupByIdAsync(Guid groupId, Guid currentUserId)`

Set `IsCurrentUserMember` from `group.GroupMembers`:

```csharp
dto.IsCurrentUserMember = currentUserId != Guid.Empty &&
                         group.GroupMembers != null &&
                         group.GroupMembers.Any(m => m.UserId == currentUserId);
```

Keep the existing parameterless-current-user overload for backward compatibility (it can
delegate to the new one with `Guid.Empty`).

### Step 2 — Update `GroupsController.Details`

Pass `GetCurrentUserId()` to the new overload.

### Step 3 — Add a local `joinGroup` in `Details.cshtml`

Swap the button in place to "Leave Group" after a successful join, plus update the members
count preview. Do NOT rely on the sidebar's global function for the Details page.

## Code Changes

### File: Sohba.Application/Services/GroupService.cs

<div style="color:red"><b>REMOVE — the current GetGroupByIdAsync:</b></div>

```csharp
        public async Task<Result<GroupResponseDto>> GetGroupByIdAsync(Guid groupId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null) return Result<GroupResponseDto>.Failure("Group not found.");

            var response = _mapper.Map<GroupResponseDto>(group);
            return Result<GroupResponseDto>.Success(response);
        }
```

<div style="color:green"><b>REPLACE WITH — the overload that computes IsCurrentUserMember:</b></div>

```csharp
        public async Task<Result<GroupResponseDto>> GetGroupByIdAsync(Guid groupId)
        {
            return await GetGroupByIdAsync(groupId, Guid.Empty);
        }

        public async Task<Result<GroupResponseDto>> GetGroupByIdAsync(Guid groupId, Guid currentUserId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null) return Result<GroupResponseDto>.Failure("Group not found.");

            var response = _mapper.Map<GroupResponseDto>(group);

            response.AdminName = group.Admin?.Name ?? "System Admin";
            response.MembersCount = group.GroupMembers?.Count ?? 0;
            response.IsCurrentUserMember = currentUserId != Guid.Empty &&
                                           group.GroupMembers != null &&
                                           group.GroupMembers.Any(m => m.UserId == currentUserId);

            return Result<GroupResponseDto>.Success(response);
        }
```

### File: Sohba/Controllers/GroupsController.cs

<div style="color:red"><b>REMOVE — the Details action:</b></div>

```csharp
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
            ViewBag.CurrentUserId = GetCurrentUserId();

            return View(viewModel);
        }
```

<div style="color:green"><b>REPLACE WITH — pass the current user id:</b></div>

```csharp
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var groupResult = await _groupService.GetGroupByIdAsync(id, currentUserId);
            if (groupResult.IsFailure)
                return NotFound();

            var membersResult = await _groupService.GetGroupMembersAsync(id);
            var viewModel = new GroupDetailsViewModel
            {
                Group = groupResult.Value,
                Members = membersResult.Value ?? new List<GroupMemberDto>()
            };
            ViewBag.CurrentUserId = currentUserId;

            return View(viewModel);
        }
```

### File: Sohba/Views/Groups/Details.cshtml

<div style="color:red"><b>REMOVE — the Join button that relies on the sidebar's global joinGroup:</b></div>

```csharp
                else
                {
                    <button onclick="joinGroup('@Model.Group.Id')"
                            class="px-5 py-2.5 bg-[#345e69] text-white font-bold rounded-xl hover:bg-[#2a4b55] transition-all">
                        Join Group
                    </button>
                }
```

<div style="color:green"><b>REPLACE WITH — a self-contained join button that swaps in place:</b></div>

```csharp
                else
                {
                    <button id="groupJoinBtn" onclick="joinGroup('@Model.Group.Id', this)"
                            class="px-5 py-2.5 bg-[#345e69] text-white font-bold rounded-xl hover:bg-[#2a4b55] transition-all">
                        Join Group
                    </button>
                }
```

<div style="color:green"><b>ADD — inside the existing `@section Scripts` block (before leaveGroup):</b></div>

```javascript
        async function joinGroup(groupId, button) {
            try {
                const result = await SohbaApp.post('/Groups/Join', { id: groupId });

                if (result.success) {
                    SohbaApp.toast('Joined group successfully!', 'success');

                    // Swap the button to "Leave Group" in place — no reload required.
                    if (button) {
                        button.outerHTML = `
                            <button onclick="leaveGroup('${groupId}')"
                                    class="px-5 py-2.5 bg-red-50 text-red-600 font-bold rounded-xl hover:bg-red-100 transition-all">
                                Leave Group
                            </button>
                        `;
                    }
                } else {
                    SohbaApp.toast(result.error || 'Failed to join group', 'error');
                }
            } catch (error) {
                console.error('Error joining group:', error);
                SohbaApp.toast('Network error', 'error');
            }
        }
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:**
    1. Login → `/Groups/Details/{id}` for a group Mohammed is NOT a member of.
    2. Click **Join Group**.
    3. Open `/Groups/Details/{id}` where Mohammed IS a member.
    4. Open a group where Mohammed is the admin.
- **Expected Results:**
    - After joining, the button changes to **Leave Group** immediately (no reload needed).
    - Members page reload shows the correct membership.
    - A member sees "Leave Group" on page load.
    - The admin sees the existing admin state (Edit Group + Leave Group), never "Join".
- **Failure Conditions:**
    - If "Join Group" still shows for a member, `GetGroupByIdAsync` is not computing
      `IsCurrentUserMember` or the controller is not passing the user id.
- **Edge Cases:**
    - `GroupDetailsViewModel` already member + not admin → Leave only.
    - Admin → Edit + Leave (existing behavior, now correct because `IsCurrentUserMember`
      is true for admins too).
    - Join fails (banned) → toast error; button unchanged.

<br>
<br>

---

<br>

# Issue 3 — Accept / Decline Friend Request Returns "No pending friend request found"

## Issue

User `mohammed@sohba.com` opens Friend Requests, sees Khaled's pending request, clicks
Accept or Decline, and gets the toast **"No pending friend request found."**

## Related Feature

- **Feature Name:** Friends — Pending Requests.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Friends section.

## Expected Behaviour

- Accept → the request is accepted; the row disappears from the pending list.
- Decline → the request is removed; the row disappears from the pending list.

## Current Behaviour

- The request is visibly present in the pending list.
- Accept/Decline sends the request to the backend but the backend reports
  "No pending friend request found."
- The row never disappears.

## Root Cause

**Frontend/ViewModel mismatch in the ID passed to the backend.**

`FriendshipService.GetPendingRequestsAsync` builds the DTO as:

```csharp
var dtos = requests.Select(f => new FriendDto
{
    UserId = userId,          // ← the CURRENT user's id (Mohammed)
    FriendUserId = f.UserId,  // ← the actual SENDER id (Khaled)
    ...
```

But `Friends/Requests.cshtml` renders:

```csharp
<button onclick="acceptRequest('@request.UserId', this)">Accept</button>
<button onclick="rejectRequest('@request.UserId', this)">Decline</button>
```

So the frontend sends **Mohammed's own id** as `senderId`/`requesterId`. The controller calls
`AcceptFriendRequestAsync(model.senderId, currentUserId)` → i.e.
`(MohammedId, MohammedId)`. `HasPendingRequestAsync(senderId: MohammedId, receiverId:
MohammedId)` looks for a request from Mohammed to Mohammed — none exists — so the domain rule
fails with "No pending friend request found."

The **sender's id is `FriendUserId`**, not `UserId`, in the pending-request DTO.

## Execution Flow

```
GET /Friends/Requests
    → GetPendingRequestsAsync(userId)
        → FriendDto { UserId = currentUserId, FriendUserId = senderId }  ← for pending rows
    → Requests.cshtml renders onclick="acceptRequest('@request.UserId', ...)"
        → passes CURRENT USER'S id instead of the sender's id      ← BUG

Click Accept
    → friends.js acceptRequest(userId = MohammedId)
        → POST /Friends/AcceptRequest { senderId: MohammedId }
            → AcceptFriendRequestAsync(MohammedId, MohammedId)
                → HasPendingRequestAsync(MohammedId, MohammedId) → false
                → "No pending friend request found."
```

## Related Files

- `Sohba/Views/Friends/Requests.cshtml`
- `Sohba/wwwroot/js/features/friends.js`
- `Sohba/Controllers/FriendsController.cs`
- `Sohba.Application/Services/FriendshipService.cs`
- `Sohba.Application/DTOs/UserAggregate/FriendDto.cs`
- `Sohba.Infrastructure/Repositories/FriendshipRepository.cs`

## Affected Components

- View — `Requests.cshtml`
- JavaScript — `features/friends.js` (already sends the passed id — no change needed)
- Controller — `FriendsController.cs` (already passes the id it receives — no change needed)
- Application Service — `FriendshipService.cs` (DTO shape — no change required, but the
  mismatch is here)
- DTO — `FriendDto.cs`

## Files That Need Modification

1. `Sohba/Views/Friends/Requests.cshtml`

## Implementation Plan

### Step 1 — Use the sender id in the pending-request rendering

Change the three uses of `@request.UserId` in the pending tab to `@request.FriendUserId`:

- `data-request-id="@request.FriendUserId"`
- `acceptRequest('@request.FriendUserId', this)`
- `rejectRequest('@request.FriendUserId', this)`

`friends.js` already sends the value it receives as `senderId` / `requesterId`, and the
controller/service already interpret it as the sender — so no JS/controller/service change
is needed.

## Code Changes

### File: Sohba/Views/Friends/Requests.cshtml

<div style="color:red"><b>REMOVE — the pending request row using the wrong id:</b></div>

```csharp
                    <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-4 hover-lift" data-request-id="@request.UserId">
                    <div class="flex items-center justify-between">
                        <div class="flex items-center gap-4">
                            <img src="@(request.ProfilePictureUrl ?? $"https://ui-avatars.com/api/?name={request.FriendName}&background=345e69&color=fff")"
                                 class="w-14 h-14 rounded-2xl object-cover border-2 border-slate-100" />
                            <div>
                                <h3 class="font-bold text-gray-900">@request.FriendName</h3>
                                <p class="text-sm text-gray-500">Wants to connect with you</p>
                            </div>
                        </div>
                        <div class="flex gap-2">
                                <button onclick="acceptRequest('@request.UserId', this)"
                                        class="px-5 py-2 bg-[#345e69] text-white font-semibold rounded-xl hover:bg-[#2a4b55] transition-colors">
                                Accept
                            </button>
                                <button onclick="rejectRequest('@request.UserId', this)"
                                    class="px-5 py-2 border border-gray-200 text-gray-600 font-semibold rounded-xl hover:bg-gray-50 transition-colors">
                                Decline
                            </button>
                        </div>
                    </div>
                </div>
```

<div style="color:green"><b>REPLACE WITH — use FriendUserId (the actual sender):</b></div>

```csharp
                    <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-4 hover-lift" data-request-id="@request.FriendUserId">
                    <div class="flex items-center justify-between">
                        <div class="flex items-center gap-4">
                            <img src="@(request.ProfilePictureUrl ?? $"https://ui-avatars.com/api/?name={request.FriendName}&background=345e69&color=fff")"
                                 class="w-14 h-14 rounded-2xl object-cover border-2 border-slate-100" />
                            <div>
                                <h3 class="font-bold text-gray-900">@request.FriendName</h3>
                                <p class="text-sm text-gray-500">Wants to connect with you</p>
                            </div>
                        </div>
                        <div class="flex gap-2">
                                <button onclick="acceptRequest('@request.FriendUserId', this)"
                                        class="px-5 py-2 bg-[#345e69] text-white font-semibold rounded-xl hover:bg-[#2a4b55] transition-colors">
                                Accept
                            </button>
                                <button onclick="rejectRequest('@request.FriendUserId', this)"
                                    class="px-5 py-2 border border-gray-200 text-gray-600 font-semibold rounded-xl hover:bg-gray-50 transition-colors">
                                Decline
                            </button>
                        </div>
                    </div>
                </div>
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com` (receiver), `khaled@sohba.com` (sender).
- **Required data:** A pending request row exists with `UserId = Mohammed`, `FriendUserId = Khaled`.
- **Navigation:** Login as Mohammed → `/Friends/Requests`.
- **Expected Results:**
    - Clicking Accept → toast "Friend request accepted!"; row disappears.
    - After refresh, the two users are friends (`/Friends`).
    - Clicking Decline on a second request → toast "Friend request declined"; row disappears.
- **Failure Conditions:**
    - If the toast still says "No pending friend request found.", the view still passes
      `@request.UserId`.
- **Edge Cases:**
    - Multiple pending requests → each row uses its own `FriendUserId`.
    - Sent tab (`data-request-id="@request.FriendUserId"`) is already correct — do not change.

<br>
<br>

---

<br>

# Issue 4 — Home Search Button UI Position

## Issue

On the Home page header, the Search button is visually floating on top of the search bar
instead of sitting after/beside the search input.

## Related Feature

- **Feature Name:** Global Header Search.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Home / Search.

## Expected Behaviour

- The Search button appears after/beside the search input, aligned with it.
- It must not overlap the input text.
- Search functionality must be preserved.
- Responsive behavior must remain intact.

## Current Behaviour

- The Search button is `position: absolute; right: 1.5` inside the same relative container
  as the input.
- The input only has `pr-3` (right padding), so the button overlaps the typing area.

## Root Cause

`Sohba/Views/Shared/Partials/_Header.cshtml` lines 39-51:

```csharp
<input type="text"
       id="searchInput"
       class="block w-full pl-10 pr-3 py-2 ..."
       placeholder="Search for posts, people, groups, pages..."
       autocomplete="off">
<button id="globalSearchBtn"
        type="button"
        class="absolute right-1.5 top-1/2 -translate-y-1/2 px-3 py-1.5 bg-[#345e69] ...">
    Search
</button>
```

The button is absolutely positioned over the input's right edge, but the input's right
padding (`pr-3` = 12px) is far smaller than the button's width. The button therefore
overlaps the text area.

## Execution Flow

```
Page renders _Header.cshtml
    → input has class "pl-10 pr-3"     // pr-3 leaves only 12px of right padding
    → button is absolute right-1.5     // button floats over the input's right side
    → typing long text is hidden under the button   ← visual bug
```

## Related Files

- `Sohba/Views/Shared/Partials/_Header.cshtml`

## Affected Components

- View — `_Header.cshtml` (search bar markup/CSS classes)
- JavaScript — `features/header.js` (unchanged; search logic preserved)

## Files That Need Modification

1. `Sohba/Views/Shared/Partials/_Header.cshtml`

## Implementation Plan

### Step 1 — Give the input enough right padding for the button

Change `pr-3` to `pr-28` on the desktop search input. This reserves ~7rem on the right so
the absolutely-positioned Search button (≈ 70px wide) never overlaps typed text.

No JS or layout change is required.

## Code Changes

### File: Sohba/Views/Shared/Partials/_Header.cshtml

<div style="color:red"><b>REMOVE — the input with insufficient right padding:</b></div>

```csharp
                        <input type="text"
                               id="searchInput"
                               class="block w-full pl-10 pr-3 py-2 border border-solid border-gray-200 rounded-2xl bg-gray-50 leading-5 placeholder-gray-400
                          transition-all duration-300 ease-out
                          focus:outline-none focus:bg-white focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69]
                          focus:scale-[1.02] focus:shadow-[0_4px_20px_-4px_rgba(52,94,105,0.15)] origin-center sm:text-sm"
                               placeholder="Search for posts, people, groups, pages..."
                               autocomplete="off">
```

<div style="color:green"><b>REPLACE WITH — the input with reserved right space for the button:</b></div>

```csharp
                        <input type="text"
                               id="searchInput"
                               class="block w-full pl-10 pr-28 py-2 border border-solid border-gray-200 rounded-2xl bg-gray-50 leading-5 placeholder-gray-400
                          transition-all duration-300 ease-out
                          focus:outline-none focus:bg-white focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69]
                          focus:scale-[1.02] focus:shadow-[0_4px_20px_-4px_rgba(52,94,105,0.15)] origin-center sm:text-sm"
                               placeholder="Search for posts, people, groups, pages..."
                               autocomplete="off">
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:** Home (logged in) → inspect the header search bar.
- **Expected Results:**
    - The "Search" button is fully inside the input's right edge (not clipped, not
      overlapping).
    - Typing a long query is visible; the text is not hidden under the button.
    - Clicking Search or pressing Enter still submits `/Search?q=...` and the quick-results
      dropdown still works.
- **Failure Conditions:**
    - If the button still overlaps, `pr-28` was not applied.
- **Edge Cases:**
    - Mobile (< `sm`) — the desktop search bar is hidden; the mobile search container is
      unchanged and unaffected.
    - Very long placeholder text is still truncated by the input (unchanged).

<br>
<br>

---

<br>

# Issue 5 — Notification Dropdown Must Be Fully Clickable

## Issue

The notification dropdown displays notifications, but the notification items are not
clickable. Each notification should navigate to the correct destination based on its type.

## Related Feature

- **Feature Name:** Notifications — Dropdown & Full Page.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Notifications.

## Expected Behaviour

- Each notification item is clickable and navigates based on its type:

```text
Reaction notification   (PostLike)     → Post Details       (/Posts/Details/{TargetId})
Comment notification    (PostComment)  → Post Details       (/Posts/Details/{TargetId})
Friend Request          (FriendRequest)→ Friend Requests    (/Friends/Requests)
Group notification      (GroupInvitation) → Group Details  (/Groups/Details/{TargetId})
Page notification       (SystemAlert)  → Notifications page (/Notifications/Index)
```

- The full notifications page (`/Notifications/Index`) should also list actual notifications
  (currently it shows a "Coming Soon" placeholder even when the model has data).

## Current Behaviour

- `header.js` `loadNotifications()` renders each item as a plain `<div>` with no `<a href>`.
- `Notifications/Index.cshtml` renders a static "Real-Time Notification Coming Soon" hero and
  never renders `@Model` items.

## Root Cause

**Frontend — dropdown:** `Sohba/wwwroot/js/features/header.js` `loadNotifications()`
(lines 55-71) builds rows as `<div>` without any click target. The DTO already provides what
is needed:

- `NotificationResponseDto.TargetId` — set by `NotificationService.CreateNotificationAsync`
  as `targetId` = PostId (PostLike/PostComment), GroupId (GroupInvitation), null
  (FriendRequest).
- `NotificationResponseDto.NotificationType` — the enum name.

No backend change is required: all the data needed to build a navigation URL already exists.

**View — full page:** `Sohba/Views/Notifications/Index.cshtml` accepts a model but the body
only renders the static hero and the empty-state; it never iterates `@Model`.

## Execution Flow

```
Notification created (e.g., PostLike)
    → NotificationService.CreateNotificationAsync(... targetId = postId, type = PostLike)
    → NotificationResponseDto { TargetId = postId, NotificationType = "PostLike" }

User opens header bell
    → header.js loadNotifications()
        → renders <div> without onClick / <a>           ← BUG: not clickable
    → Notifications/Index renders "Coming Soon" hero     ← BUG: model ignored
```

## Related Files

- `Sohba/wwwroot/js/features/header.js`
- `Sohba/Views/Notifications/Index.cshtml`
- `Sohba.Application/DTOs/UserAggregate/NotificationResponseDto.cs`
- `Sohba.Application/Services/NotificationService.cs`
- `Sohba.Domain/Enums/NotificationType.cs`

## Affected Components

- JavaScript — `features/header.js`
- View — `Notifications/Index.cshtml`
- DTO — `NotificationResponseDto.cs` (already has TargetId + NotificationType — no change)
- Application Service — `NotificationService.cs` (already sets TargetId — no change)

## Files That Need Modification

1. `Sohba/wwwroot/js/features/header.js`
2. `Sohba/Views/Notifications/Index.cshtml`

## Implementation Plan

### Step 1 — Add a URL resolver in `header.js`

Add `getNotificationUrl(notif)`:

- `PostLike` / `PostComment` → `/Posts/Details/${notif.targetId}`
- `GroupInvitation` → `/Groups/Details/${notif.targetId}`
- `FriendRequest` → `/Friends/Requests`
- `SystemAlert` / default → `/Notifications/Index`

### Step 2 — Render each dropdown item as an `<a href=...>`

Wrap each notification row in an anchor. Keep the "Mark read" button as a nested element
with `stopPropagation` so clicking it does not navigate.

### Step 3 — Render the full page list in `Notifications/Index.cshtml`

Replace the static hero/empty block with a loop over `@Model` rendering each notification as
a clickable link using the same URL mapping (Razor side), plus a `markAllRead` script backed
by `/Notifications/MarkAllAsRead`.

## Code Changes

### File: Sohba/wwwroot/js/features/header.js

<div style="color:red"><b>REMOVE — the loadNotifications mapping that renders plain <div> rows:</b></div>

```javascript
            list.innerHTML = result.data.map(notif => `
                <div class="flex items-start gap-3 px-4 py-3 hover:bg-gray-50 transition-colors border-b border-gray-50 ${notif.isRead ? 'opacity-60' : 'bg-blue-50/30'}">
                    <div class="w-10 h-10 rounded-full bg-[#345e69]/10 flex items-center justify-center flex-shrink-0">
                        <span class="text-[#345e69]">${getNotificationIcon(notif.notificationType)}</span>
                    </div>
                    <div class="flex-1 min-w-0">
                        <p class="text-sm text-gray-800">${notif.message}</p>
                        <p class="text-xs text-gray-400 mt-0.5">${notif.timeAgo}</p>
                    </div>
                    ${!notif.isRead ? `
                        <button onclick="markNotificationAsRead('${notif.id}')"
                                class="text-xs text-[#345e69] hover:underline self-start mt-1">
                            Mark read
                        </button>
                    ` : ''}
                </div>
            `).join('');
```

<div style="color:green"><b>REPLACE WITH — clickable rows using the URL resolver:</b></div>

```javascript
            list.innerHTML = result.data.map(notif => `
                <a href="${getNotificationUrl(notif)}"
                   class="flex items-start gap-3 px-4 py-3 hover:bg-gray-50 transition-colors border-b border-gray-50 ${notif.isRead ? 'opacity-60' : 'bg-blue-50/30'}">
                    <div class="w-10 h-10 rounded-full bg-[#345e69]/10 flex items-center justify-center flex-shrink-0">
                        <span class="text-[#345e69]">${getNotificationIcon(notif.notificationType)}</span>
                    </div>
                    <div class="flex-1 min-w-0">
                        <p class="text-sm text-gray-800">${notif.message}</p>
                        <p class="text-xs text-gray-400 mt-0.5">${notif.timeAgo}</p>
                    </div>
                    ${!notif.isRead ? `
                        <button onclick="event.preventDefault(); event.stopPropagation(); markNotificationAsRead('${notif.id}')"
                                class="text-xs text-[#345e69] hover:underline self-start mt-1">
                            Mark read
                        </button>
                    ` : ''}
                </a>
            `).join('');
```

<div style="color:green"><b>ADD — the URL resolver function after getNotificationIcon:</b></div>

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

<div style="color:green"><b>ADD — one global click handler so 'Mark read' clicks do not navigate (after the notifDropdown click handler):</b></div>

```javascript
    notifDropdown.addEventListener('click', function (e) {
        e.stopPropagation();
        const markReadBtn = e.target.closest('button');
        if (markReadBtn) {
            e.preventDefault(); // Do not follow the parent <a> when clicking "Mark read".
        }
    });
```

### File: Sohba/Views/Notifications/Index.cshtml

<div style="color:red"><b>REMOVE — the static "Real-Time Notification Coming Soon" hero block and the empty-state-only body:</b></div>

```csharp
    <!-- Notifications List -->
    <div class="space-y-2">
        <div class="relative overflow-hidden rounded-3xl bg-gradient-to-br  from-[#345e69] via-[#4a8291] to-[#345e69] p-12 text-center">
            
            <div class="absolute inset-0 bg-grid-white/10 [mask-image:radial-gradient(ellipse_at_center,white,transparent_75%)]"></div>

            <div class="relative z-10">
                <div class="w-28 h-28 mx-auto bg-white/10 backdrop-blur-xl rounded-3xl flex items-center justify-center mb-6 border border-white/20">
                    <svg class="w-14 h-14 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                    </svg>
                </div>

                <h3 class="text-4xl font-black text-white mb-2">Real-Time Notification</h3>
                <p class="text-xl text-white/80 mb-8">Coming Soon with SignalR</p>

                <div class="flex justify-center gap-3">
                    <span class="px-4 py-2 bg-white/10 backdrop-blur-xl rounded-xl text-white font-semibold border border-white/20">⚡ Live</span>
                    <span class="px-4 py-2 bg-white/10 backdrop-blur-xl rounded-xl text-white font-semibold border border-white/20">💬 Instant</span>
                    <span class="px-4 py-2 bg-white/10 backdrop-blur-xl rounded-xl text-white font-semibold border border-white/20">🔄 Real-time</span>
                </div>
            </div>
        </div>
    </div>

    @if (!Model.Any())
    {
        <div class="text-center py-20">
            <div class="bg-slate-50 w-24 h-24 rounded-full flex items-center justify-center mx-auto mb-4">
                <svg class="w-12 h-12 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                </svg>
            </div>
            <h3 class="text-lg font-bold text-gray-900">No notifications</h3>
            <p class="text-gray-500 mt-2">You're all caught up!</p>
        </div>
    }
```

<div style="color:green"><b>REPLACE WITH — a real clickable list backed by the model:</b></div>

```csharp
    @functions {
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
    }

    <!-- Notifications List -->
    @if (Model != null && Model.Any())
    {
        <div class="space-y-3">
            @foreach (var notification in Model)
            {
                <a href="@GetNotificationUrl(notification)"
                   class="flex items-start gap-4 bg-white rounded-2xl shadow-sm border border-slate-100 p-4 hover:shadow-md transition-shadow @(notification.IsRead ? "opacity-60" : "border-l-4 border-l-[#345e69]")">
                    <div class="w-12 h-12 rounded-full bg-[#345e69]/10 flex items-center justify-center flex-shrink-0">
                        <span class="text-xl">@GetNotificationIcon(notification.NotificationType)</span>
                    </div>
                    <div class="flex-1 min-w-0">
                        <p class="text-gray-800 text-sm">@notification.Message</p>
                        <p class="text-xs text-gray-400 mt-1">@notification.TimeAgo</p>
                    </div>
                    @if (!notification.IsRead)
                    {
                        <span class="w-2.5 h-2.5 rounded-full bg-[#345e69] flex-shrink-0 mt-2"></span>
                    }
                </a>
            }
        </div>
    }
    else
    {
        <div class="text-center py-20">
            <div class="bg-slate-50 w-24 h-24 rounded-full flex items-center justify-center mx-auto mb-4">
                <svg class="w-12 h-12 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                </svg>
            </div>
            <h3 class="text-lg font-bold text-gray-900">No notifications</h3>
            <p class="text-gray-500 mt-2">You're all caught up!</p>
        </div>
    }
```

<div style="color:red"><b>REMOVE — the commented-out markAllRead script:</b></div>

```csharp
@section Scripts {
    @* <script>
        async function markAllRead() {
            // Implement API call
            SohbaApp.toast('All notifications marked as read', 'success');
        }
    </script> *@
}
```

<div style="color:green"><b>REPLACE WITH — a working markAllRead script:</b></div>

```csharp
@section Scripts {
    <script>
        async function markAllRead() {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const response = await fetch('/Notifications/MarkAllAsRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                }
            });
            const result = await response.json();
            if (result.success) {
                SohbaApp.toast('All notifications marked as read', 'success');
                window.location.reload();
            } else {
                SohbaApp.toast(result.error || 'Failed to mark all as read', 'error');
            }
        }
    </script>
}
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com` (needs at least one PostLike and one FriendRequest
  notification).
- **Navigation:**
    1. Login → click the header bell → dropdown opens.
    2. Click a "liked your post" notification.
    3. Click a "friend request" notification.
    4. Open `/Notifications/Index`.
- **Expected Results:**
    - PostLike/PostComment → navigates to `/Posts/Details/{id}`.
    - FriendRequest → navigates to `/Friends/Requests`.
    - GroupInvitation → navigates to `/Groups/Details/{id}`.
    - SystemAlert → navigates to `/Notifications/Index`.
    - The full page lists actual notifications (not "Coming Soon").
    - "Mark read" button does NOT navigate; it only marks the row read.
- **Failure Conditions:**
    - If the dropdown item is still a non-clickable `<div>`, the `header.js` mapping was not
      replaced.
    - If the full page still shows the hero, `Index.cshtml` was not replaced.
- **Edge Cases:**
    - `TargetId` is null for FriendRequest → the URL resolver ignores it and uses
      `/Friends/Requests`.
    - Unknown type → default `/Notifications/Index`.

<br>
<br>

---

<br>

# Issue 6 — Deleted Reply Does Not Immediately Disappear

## Issue

When a reply is deleted from the open Post Modal, it does not disappear immediately. The
reply only disappears after closing and reopening the modal.

## Related Feature

- **Feature Name:** Post Details / Comments — Delete Reply.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.5 (Comments — Reply & Delete).

## Expected Behaviour

```text
Delete Reply
    ↓
Backend confirms deletion
    ↓
Reply immediately disappears from current Post Modal
```

No modal refresh/reopen should be required. The DOM element must be removed directly after
successful deletion.

## Current Behaviour

- The backend delete succeeds (the reply row is removed in the DB).
- The reply element remains visible in the modal until the modal is reopened.

## Root Cause

`Sohba/wwwroot/js/sohba-modal.js` renders reply rows WITHOUT any id that
`deleteComment()` looks for:

- Top-level comments get `id="comment-${c.id}"` (line 95) → deletion works.
- Replies are rendered in the `c.replies.map` block (lines 67-84) with NO
  `id="comment-${reply.id}"` and NO `data-comment-id` attribute.

`deleteComment()` in `Sohba/wwwroot/js/features/comments.js` (lines 25-30) looks for:

```javascript
const commentElement = document.getElementById(`comment-${commentId}`) || 
                       document.querySelector(`[data-comment-id="${commentId}"]`);
```

For a reply, neither selector matches → `commentElement` is null → the DOM removal block is
skipped. Only the backend deletion occurs; the UI row stays.

## Execution Flow

```
User clicks Delete on a reply
    → SohbaApp.deleteComment(replyId, postId)
        → POST /Comments/Delete { id: replyId } → success (DB row removed)
        → const commentElement = document.getElementById(`comment-${replyId}`)
              // NOT found: reply markup has no id="comment-..."       ← BUG
        → const wrapperElement = commentElement.closest(...)
              // commentElement is null → crashes or is skipped
        → toast "Comment deleted successfully!" but the DOM row remains
```

## Related Files

- `Sohba/wwwroot/js/sohba-modal.js`
- `Sohba/wwwroot/js/features/comments.js`
- `Sohba/Controllers/CommentsController.cs`
- `Sohba.Application/Services/InteractionService.cs`

## Affected Components

- JavaScript — `sohba-modal.js` (reply rendering)
- JavaScript — `features/comments.js` (deleteComment — already correct, no change needed)
- Controller — `CommentsController.cs` (already correct — no change needed)

## Files That Need Modification

1. `Sohba/wwwroot/js/sohba-modal.js`

## Implementation Plan

### Step 1 — Give every reply a `comment-{id}` element id

In the replies map inside `sohba-modal.js`, add `id="comment-${reply.id}"` to the reply's
content element. This makes `deleteComment()` find it.

### Step 2 — (Optional but recommended) give replies `data-comment-id` on the row

Add `data-comment-id="${reply.id}"` to the reply row `<div>` so both selectors work and the
row itself can be removed (the existing `deleteComment` uses
`closest('.flex.items-start')` to find the wrapper — with the row `div` carrying the id, the
closest-match still resolves correctly).

## Code Changes

### File: Sohba/wwwroot/js/sohba-modal.js

<div style="color:red"><b>REMOVE — the reply rendering block without a deletable id:</b></div>

```javascript
                                ${c.replies.map(reply => `
                                    <div class="flex items-start gap-3">
                                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(reply.userName)}&background=random" 
                                             class="w-7 h-7 rounded-full flex-shrink-0">
                                        <div>
                                            <span class="font-semibold text-sm text-gray-900">${reply.userName}</span>
                                            <p class="text-sm text-gray-700">${reply.content}</p>
                                            <span class="text-xs text-gray-400">${new Date(reply.createdAt).toLocaleString()}</span>
                        
                                            ${reply.isAuthor ? `
                                                <button onclick="SohbaApp.deleteComment('${reply.id}', '${reply.postId}')"
                                                        class="text-xs text-red-500 hover:underline font-medium ml-2">
                                                    Delete
                                                </button>
                                            ` : ''}
                                        </div>
                                    </div>
                                `).join('')}
```

<div style="color:green"><b>REPLACE WITH — reply rows with data-comment-id + id so deletion works:</b></div>

```javascript
                                ${c.replies.map(reply => `
                                    <div class="flex items-start gap-3" data-comment-id="${reply.id}">
                                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(reply.userName)}&background=random" 
                                             class="w-7 h-7 rounded-full flex-shrink-0">
                                        <div>
                                            <span class="font-semibold text-sm text-gray-900">${reply.userName}</span>
                                            <p id="comment-${reply.id}" class="text-sm text-gray-700">${reply.content}</p>
                                            <span class="text-xs text-gray-400">${new Date(reply.createdAt).toLocaleString()}</span>
                        
                                            ${reply.isAuthor ? `
                                                <button onclick="SohbaApp.deleteComment('${reply.id}', '${reply.postId}')"
                                                        class="text-xs text-red-500 hover:underline font-medium ml-2">
                                                    Delete
                                                </button>
                                            ` : ''}
                                        </div>
                                    </div>
                                `).join('')}
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com` (author of the reply).
- **Required data:** A reply authored by Mohammed inside a comment's replies container.
- **Navigation:** Login → Home feed → open the post modal → find Mohammed's reply.
- **Expected Results:**
    - Click Delete → confirm → toast "Comment deleted successfully!".
    - The reply row disappears from the open modal immediately (no reopen needed).
- **Failure Conditions:**
    - If the reply still remains visible, the reply markup was not updated with
      `id="comment-{id}"` / `data-comment-id`.
- **Edge Cases:**
    - Top-level comments already work (they have `id="comment-{c.id}"`); do not regress them.
    - Non-author sees no Delete button (unchanged).

<br>
<br>

---

<br>

# Issue 7 — Replies Must Support Nested Replies With Maximum Depth 4

## Issue

Currently only `Comment → Reply` works. Replies themselves must be replyable, with a strict
maximum of **4 levels total**:

```text
Level 1 = Comment
Level 2 = Reply
Level 3 = Reply on Reply
Level 4 = Reply on Reply on Reply
```

No level-5 reply may be created — enforced on BOTH frontend and backend.

## Related Feature

- **Feature Name:** Post Details / Comments — Nested Replies.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.5 (Comments — Reply & Delete).

## Expected Behaviour

- A user can click Reply on a comment, on a reply, and on a reply-to-a-reply (levels 1-3).
- A level-4 item does NOT display an active Reply action.
- The backend rejects attempts to create a level-5 reply.
- Nested replies remain visually clean with indentation, expand/collapse, reply toggle, and
  correct delete controls.

## Current Behaviour

- The frontend renders replies as one flat list (`c.replies.map`) — a reply cannot be
  replied to in the UI.
- The backend has no depth validation: `CanReplyToComment` does not accept a depth parameter
  and `AddCommentAsync` never computes the parent's depth.
- `GetCommentsByPostIdAsync` only builds ONE level of nesting (top-level comments + their
  replies). Replies-of-replies are never nested in the tree.
- `CommentResponseDto` has no `Depth` property.

## Root Cause

**Backend — domain:** `Sohba.Domain/Domain Rules/Logic/InteractionDomainService.cs`,
`CanReplyToComment`:

```csharp
public Result CanReplyToComment(Guid userId, bool isCommentDeleted, bool isThreadLocked)
{
    if (isCommentDeleted) return Result.Failure("Cannot reply to a deleted comment.");
    if (isThreadLocked) return Result.Failure("This discussion thread is locked.");
    return Result.Success();
}
```

No depth limit exists. The rule signature has no `currentDepth` parameter.

**Backend — service:** `Sohba.Application/Services/InteractionService.cs`,
`AddCommentAsync` validates that the parent exists and belongs to the post, but never checks
how deep the parent is. `GetCommentsByPostIdAsync` builds the tree with only one level
(comments + `c.Replies`), so replies-of-replies are present in the flat list but not nested.

**DTO:** `CommentResponseDto` has `Replies`, `ReplyCount`, `ParentCommentId` but no `Depth`.

**Controller:** `PostsController.GetPostDetails` projects comments and their `replies` (one
level), but does not recurse into reply-of-reply.

**Frontend:** `sohba-modal.js` renders `c.replies.map` only — no recursion, no depth-based
reply visibility, no indentation for depth, and a level-4 item still shows a Reply button.

## Execution Flow

```
User posts a comment (level 1)
User replies → parent level 1 → created as level 2
User replies to that reply → parent level 2 → created as level 3
User replies again → parent level 3 → created as level 4
User replies again → parent level 4 → MUST be rejected (max depth 4)

Current flow (level 4 → 5):
    → PostsController.Comment { parentCommentId = level4CommentId }
        → InteractionService.AddCommentAsync
            → parent exists & belongs to post → passes       ← no depth check
            → CanReplyToComment(userId, false, false) → Success  ← no depth check
        → creates level-5 reply                               ← BUG
```

## Related Files

- `Sohba.Domain/Domain Rules/Logic/InteractionDomainService.cs`
- `Sohba.Domain/Domain Rules/Interface/IInteractionDomainService.cs`
- `Sohba.Application/Services/InteractionService.cs`
- `Sohba.Application/Interfaces/IInteractionService.cs`
- `Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs`
- `Sohba/Controllers/PostsController.cs`
- `Sohba/wwwroot/js/sohba-modal.js`
- `Sohba/wwwroot/js/sohba-posts.js` (submitReply — caller)
- `Sohba.Infrastructure/Repositories/InteractionRepository.cs`

## Affected Components

- Domain Domain-Rule — `InteractionDomainService.cs` + `IInteractionDomainService.cs`
- Application Service — `InteractionService.cs`
- Application DTO — `CommentResponseDto.cs`
- Controller — `PostsController.cs` (GetPostDetails projection)
- JavaScript — `sohba-modal.js` (recursive rendering)
- JavaScript — `sohba-posts.js` (submitReply flow — unchanged; it already posts
  `parentCommentId`)

## Files That Need Modification

1. `Sohba.Domain/Domain Rules/Interface/IInteractionDomainService.cs`
2. `Sohba.Domain/Domain Rules/Logic/InteractionDomainService.cs`
3. `Sohba.Application/Services/InteractionService.cs`
4. `Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs`
5. `Sohba/Controllers/PostsController.cs`
6. `Sohba/wwwroot/js/sohba-modal.js`

## Implementation Plan

### Step 1 — Add depth to `CommentResponseDto`

Add `public int Depth { get; set; } = 1;`. Level 1 = top-level comment.

### Step 2 — Extend `CanReplyToComment` with a depth parameter

Change the signature to `Result CanReplyToComment(Guid userId, bool isCommentDeleted, bool
isThreadLocked, int currentDepth)` and reject `currentDepth >= 4` with
"Maximum reply depth reached (4 levels)." This enforces the limit in the domain layer.

### Step 3 — Compute and enforce depth in `AddCommentAsync`

When `parentCommentId` is provided:

1. Load the parent comment.
2. Walk up `ParentCommentId` (via a small helper) to compute `parentDepth`.
3. Pass `parentDepth` to `CanReplyToComment` — if `parentDepth >= 4`, reject.
4. Set `comment.Depth = parentDepth + 1`.

### Step 4 — Recursively build the comment tree with Depth

Rewrite `GetCommentsByPostIdAsync` to:

- Map all comments for the post.
- Group by `ParentCommentId`.
- Recursively attach replies and set `Depth` (1 for root, parent.Depth + 1 for children).
- Keep `IsAuthor` set on every node.

### Step 5 — Recursively project nested replies in `PostsController.GetPostDetails`

Replace the one-level `replies` projection with a recursive helper.

### Step 6 — Recursive nested reply rendering in `sohba-modal.js`

Replace `c.replies.map` with a recursive `renderComment(c, depth)`:

- Indent by `depth` (inline margin/pl classes).
- Show the Reply button only when `depth < 4`.
- Give every node `id="comment-{id}"` and `data-comment-id` so delete works at all levels.
- Keep expand/collapse via the existing `replies-{id}` container and `toggleReplies`.

## Code Changes

### File: Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs

<div style="color:red"><b>REMOVE — the current reply properties:</b></div>

```csharp
        // Reply 
        public Guid? ParentCommentId { get; set; }
        public List<CommentResponseDto> Replies { get; set; } = new List<CommentResponseDto>();
        public int ReplyCount { get; set; }
```

<div style="color:green"><b>REPLACE WITH — add Depth:</b></div>

```csharp
        // Reply 
        public Guid? ParentCommentId { get; set; }
        public List<CommentResponseDto> Replies { get; set; } = new List<CommentResponseDto>();
        public int ReplyCount { get; set; }

        // Nesting depth: 1 = top-level comment, 2 = reply, 3 = reply-on-reply, max 4.
        public int Depth { get; set; } = 1;
```

### File: Sohba.Domain/Domain Rules/Interface/IInteractionDomainService.cs

<div style="color:red"><b>REMOVE — the current reply rule:</b></div>

```csharp
        Result CanReplyToComment(Guid userId, bool isCommentDeleted, bool isThreadLocked);
```

<div style="color:green"><b>REPLACE WITH — the depth-aware signature:</b></div>

```csharp
        Result CanReplyToComment(Guid userId, bool isCommentDeleted, bool isThreadLocked, int currentDepth);
```

### File: Sohba.Domain/Domain Rules/Logic/InteractionDomainService.cs

<div style="color:red"><b>REMOVE — the current CanReplyToComment:</b></div>

```csharp
        public Result CanReplyToComment(Guid userId, bool isCommentDeleted, bool isThreadLocked)
        {
            if (isCommentDeleted)
                return Result.Failure("Cannot reply to a deleted comment.");

            if (isThreadLocked)
                return Result.Failure("This discussion thread is locked.");

            return Result.Success();
        }
```

<div style="color:green"><b>REPLACE WITH — the depth-aware implementation:</b></div>

```csharp
        private const int MaxReplyDepth = 4;

        public Result CanReplyToComment(Guid userId, bool isCommentDeleted, bool isThreadLocked, int currentDepth)
        {
            if (isCommentDeleted)
                return Result.Failure("Cannot reply to a deleted comment.");

            if (isThreadLocked)
                return Result.Failure("This discussion thread is locked.");

            if (currentDepth >= MaxReplyDepth)
                return Result.Failure($"Maximum reply depth reached ({MaxReplyDepth} levels).");

            return Result.Success();
        }
```

### File: Sohba.Application/Services/InteractionService.cs

<div style="color:red"><b>REMOVE — the parent-comment validation block inside AddCommentAsync:</b></div>

```csharp
            if (parentCommentId.HasValue)
            {
                var parentComment = await _unitOfWork.Interactions.GetCommentByIdAsync(parentCommentId.Value);
                if (parentComment == null)
                    return Result.Failure("Parent comment not found.");

                if (parentComment.PostId != postId)
                    return Result.Failure("Parent comment does not belong to this post.");
            }


            var canComment = _interactionDomainService.CanAddComment(userId, content, post.IsDeleted, isBlockedByOwner: false);
            if (!canComment.IsSuccess)
            {
                _logger.LogWarning("Comment rejected for user {UserId} on post {PostId}: {Reason}", userId, postId, canComment.Error);
                return canComment;
            }

            var comment = new Comment
            {
                UserId = userId,
                PostId = postId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                ParentCommentId = parentCommentId
            };
```

<div style="color:green"><b>REPLACE WITH — depth computation + domain depth rule:</b></div>

```csharp
            int parentDepth = 0;
            if (parentCommentId.HasValue)
            {
                var parentComment = await _unitOfWork.Interactions.GetCommentByIdAsync(parentCommentId.Value);
                if (parentComment == null)
                    return Result.Failure("Parent comment not found.");

                if (parentComment.PostId != postId)
                    return Result.Failure("Parent comment does not belong to this post.");

                parentDepth = await GetCommentDepthAsync(parentCommentId.Value);
                var canReplyDepth = _interactionDomainService.CanReplyToComment(userId, false, false, parentDepth);
                if (!canReplyDepth.IsSuccess)
                    return canReplyDepth;
            }


            var canComment = _interactionDomainService.CanAddComment(userId, content, post.IsDeleted, isBlockedByOwner: false);
            if (!canComment.IsSuccess)
            {
                _logger.LogWarning("Comment rejected for user {UserId} on post {PostId}: {Reason}", userId, postId, canComment.Error);
                return canComment;
            }

            var comment = new Comment
            {
                UserId = userId,
                PostId = postId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                ParentCommentId = parentCommentId,
                Depth = parentDepth + 1
            };
```

<div style="color:green"><b>ADD — the depth helper method (place after DeleteCommentAsync):</b></div>

```csharp
        // Walks up ParentCommentId to compute how deep a comment is (1 = top-level comment).
        private async Task<int> GetCommentDepthAsync(Guid commentId)
        {
            var depth = 1;
            var current = await _unitOfWork.Interactions.GetCommentByIdAsync(commentId);

            while (current?.ParentCommentId != null)
            {
                depth++;
                current = await _unitOfWork.Interactions.GetCommentByIdAsync(current.ParentCommentId.Value);
            }

            return depth;
        }
```

<div style="color:red"><b>REMOVE — the current one-level tree builder in GetCommentsByPostIdAsync:</b></div>

```csharp
        public async Task<IEnumerable<CommentResponseDto>> GetCommentsByPostIdAsync(Guid postId , Guid currentUserId)
        {
            var comments = await _unitOfWork.Interactions.GetCommentsByPostIdAsync(postId);

            // Build comment tree (top-level comments with their replies)
            var commentDtos = _mapper.Map<IEnumerable<CommentResponseDto>>(comments).ToList();

            // Group replies by parent comment ID
            var replyLookup = commentDtos
                .Where(c => c.ParentCommentId.HasValue)
                .GroupBy(c => c.ParentCommentId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build the tree: only return top-level comments (no parent)
            var result = new List<CommentResponseDto>();



            // Do I Delete This Or What???????????????????????????????????????
            foreach (var comment in commentDtos.Where(c => !c.ParentCommentId.HasValue))
            {
                comment.Replies = replyLookup.ContainsKey(comment.Id)
                    ? replyLookup[comment.Id]
                    : new List<CommentResponseDto>();
                comment.ReplyCount = comment.Replies.Count;
                result.Add(comment);
            }

            foreach (var comment in result)
            {
                comment.IsAuthor = comment.UserId == currentUserId;
                foreach (var reply in comment.Replies)
                {
                    reply.IsAuthor = reply.UserId == currentUserId;
                }
            }
            return result.OrderByDescending(c => c.CreatedAt).ToList();
        }
```

<div style="color:green"><b>REPLACE WITH — a recursive tree builder that sets Depth and IsAuthor:</b></div>

```csharp
        public async Task<IEnumerable<CommentResponseDto>> GetCommentsByPostIdAsync(Guid postId , Guid currentUserId)
        {
            var comments = await _unitOfWork.Interactions.GetCommentsByPostIdAsync(postId);

            var commentDtos = _mapper.Map<IEnumerable<CommentResponseDto>>(comments).ToList();

            var replyLookup = commentDtos
                .Where(c => c.ParentCommentId.HasValue)
                .GroupBy(c => c.ParentCommentId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            CommentResponseDto AssignTree(CommentResponseDto node, int depth)
            {
                node.Depth = depth;
                node.IsAuthor = node.UserId == currentUserId;

                if (replyLookup.ContainsKey(node.Id))
                {
                    node.Replies = replyLookup[node.Id]
                        .Select(r => AssignTree(r, depth + 1))
                        .OrderByDescending(r => r.CreatedAt)
                        .ToList();
                }
                else
                {
                    node.Replies = new List<CommentResponseDto>();
                }

                node.ReplyCount = node.Depth < 4 ? node.Replies.Count : 0;
                return node;
            }

            var result = commentDtos
                .Where(c => !c.ParentCommentId.HasValue)
                .Select(c => AssignTree(c, 1))
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return result;
        }
```

### File: Sohba/Controllers/PostsController.cs

<div style="color:red"><b>REMOVE — the one-level replies projection in GetPostDetails:</b></div>

```csharp
                comments = comments.Select(c => new
                {
                    id = c.Id,
                    postId = c.PostId,
                    content = c.Content,
                    userName = c.UserName,
                    createdAt = c.CreatedAt,
                    parentCommentId = c.ParentCommentId,
                    replyCount = c.ReplyCount,
                    isAuthor = c.IsAuthor,
                    replies = (c.Replies ?? new List<CommentResponseDto>()).Select(r => new
                    {
                        id = r.Id,
                        postId = r.PostId,
                        content = r.Content,
                        userName = r.UserName,
                        createdAt = r.CreatedAt,
                        parentCommentId = r.ParentCommentId,
                        isAuthor = r.IsAuthor
                    })
                })
```

<div style="color:green"><b>REPLACE WITH — a recursive projection including depth:</b></div>

```csharp
                comments = comments.Select(c => new
                {
                    id = c.Id,
                    postId = c.PostId,
                    content = c.Content,
                    userName = c.UserName,
                    createdAt = c.CreatedAt,
                    parentCommentId = c.ParentCommentId,
                    replyCount = c.ReplyCount,
                    isAuthor = c.IsAuthor,
                    depth = c.Depth,
                    replies = (c.Replies ?? new List<CommentResponseDto>()).Select(r => new
                    {
                        id = r.Id,
                        postId = r.PostId,
                        content = r.Content,
                        userName = r.UserName,
                        createdAt = r.CreatedAt,
                        parentCommentId = r.ParentCommentId,
                        isAuthor = r.IsAuthor,
                        depth = r.Depth,
                        replyCount = r.ReplyCount,
                        replies = (r.Replies ?? new List<CommentResponseDto>()).Select(r2 => new
                        {
                            id = r2.Id,
                            postId = r2.PostId,
                            content = r2.Content,
                            userName = r2.UserName,
                            createdAt = r2.CreatedAt,
                            parentCommentId = r2.ParentCommentId,
                            isAuthor = r2.IsAuthor,
                            depth = r2.Depth,
                            replyCount = r2.ReplyCount,
                            replies = (r2.Replies ?? new List<CommentResponseDto>()).Select(r3 => new
                            {
                                id = r3.Id,
                                postId = r3.PostId,
                                content = r3.Content,
                                userName = r3.UserName,
                                createdAt = r3.CreatedAt,
                                parentCommentId = r3.ParentCommentId,
                                isAuthor = r3.IsAuthor,
                                depth = r3.Depth,
                                replyCount = r3.ReplyCount,
                                replies = new List<CommentResponseDto>()
                            })
                        })
                    })
                })
```

> **Note:** This nested projection supports up to 4 levels (comment + 3 reply projections).
> It is intentionally kept explicit to avoid creating an application-layer helper for a
> single controller action. The `r3` (level 4) node sets `replies = new
> List<CommentResponseDto>()` because level 5 is prohibited by the domain rule.

### File: Sohba/wwwroot/js/sohba-modal.js

<div style="color:red"><b>REMOVE — the flat replies block and the flat comments map inside openPostModal:</b></div>

```javascript
        // ============================================================
        // BUILD COMMENTS WITH REPLIES
        // ============================================================
        if (data.comments && data.comments.length > 0) {
            const commentsHtml = data.comments.map(c => {
                const commentId = `comment-${c.id}`;
                const fullContent = c.content;
                const maxLength = 100;
                const shouldTruncate = fullContent.length > maxLength;
                const shortContent = shouldTruncate ? fullContent.substring(0, maxLength) + '...' : fullContent;

                // Build replies HTML if any
                let repliesHtml = '';

                if (c.replies && c.replies.length > 0) {
                    repliesHtml = `
                            <div id="replies-${c.id}" class="mt-3 pl-4 border-l-2 border-slate-200 space-y-3">
                                ${c.replies.map(reply => `
                                    <div class="flex items-start gap-3" data-comment-id="${reply.id}">
                                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(reply.userName)}&background=random" 
                                             class="w-7 h-7 rounded-full flex-shrink-0">
                                        <div>
                                            <span class="font-semibold text-sm text-gray-900">${reply.userName}</span>
                                            <p id="comment-${reply.id}" class="text-sm text-gray-700">${reply.content}</p>
                                            <span class="text-xs text-gray-400">${new Date(reply.createdAt).toLocaleString()}</span>
                        
                                            ${reply.isAuthor ? `
                                                <button onclick="SohbaApp.deleteComment('${reply.id}', '${reply.postId}')"
                                                        class="text-xs text-red-500 hover:underline font-medium ml-2">
                                                    Delete
                                                </button>
                                            ` : ''}
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        `;
                }

                return `
                    <div class="flex items-start gap-3 mb-3">
                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(c.userName)}&background=random" 
                             class="w-8 h-8 rounded-full flex-shrink-0">
                        <div class="flex-1 min-w-0">
                            <span class="font-semibold text-sm text-gray-900">${c.userName}</span>
                            <div id="${commentId}" class="text-sm text-gray-700 break-words">
                                ${shouldTruncate ? shortContent : fullContent}
                            </div>
                            ${shouldTruncate ? `
                                <button class="text-blue-600 hover:underline text-xs mt-1 toggle-comment-btn"
                                        onclick="SohbaApp.toggleComment('${commentId}', '${fullContent.replace(/'/g, "\\'")}', '${shortContent.replace(/'/g, "\\'")}')">
                                    See more
                                </button>
                            ` : ''}
                            <div class="flex items-center gap-3 mt-1">
                                <span class="text-xs text-gray-400">${new Date(c.createdAt).toLocaleString()}</span>
                                
                                <!-- Reply button -->
                                <button onclick="SohbaApp.showReplyForm('${c.id}', '${c.userName}')" 
                                        class="text-xs text-[#345e69] hover:underline font-medium">
                                    Reply
                                </button>
                                
                                <!-- Show replies count -->
                                ${c.replyCount > 0 ? `
                                    <button onclick="SohbaApp.toggleReplies('${c.id}')" 
                                            class="text-xs text-gray-500 hover:text-[#345e69]">
                                        View ${c.replyCount} replies
                                    </button>
                                ` : ''}

                                <!-- Delete button -->
                                 ${c.isAuthor ? `
                                    <button onclick="SohbaApp.deleteComment('${c.id}', '${c.postId}')"
                                            class="text-xs text-red-500 hover:underline font-medium ml-2">
                                        Delete
                                    </button>
                                ` : ''}

                            </div>
                            
                            <!-- Reply form (hidden by default) -->
                            <div id="replyForm-${c.id}" class="mt-2 hidden">
                                <div class="flex items-start gap-3">
                                    <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(c.userName)}&background=345e69&color=fff" 
                                         class="w-7 h-7 rounded-full flex-shrink-0">
                                    <div class="flex-1">
                                        <input type="text" 
                                               id="replyInput-${c.id}" 
                                               placeholder="Write a reply..."
                                               class="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#345e69]/20">
                                        <div class="flex gap-2 mt-2">
                                            <button onclick="SohbaApp.submitReply('${c.id}', '${c.postId}')" 
                                                    class="px-4 py-1.5 bg-[#345e69] text-white text-sm font-semibold rounded-lg hover:bg-[#2a4b55]">
                                                Reply
                                            </button>
                                            <button onclick="SohbaApp.hideReplyForm('${c.id}')" 
                                                    class="px-4 py-1.5 text-sm text-gray-500 hover:text-gray-700">
                                                Cancel
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            
                            <!-- Replies container -->
                            ${repliesHtml}
                        </div>
                    </div>
                `;
            }).join('');
            document.getElementById('modalComments').innerHTML = commentsHtml;
        } else {
            document.getElementById('modalComments').innerHTML = '<p class="text-slate-400 text-sm italic">No comments yet.</p>';
        }
```

<div style="color:green"><b>REPLACE WITH — a recursive renderComment supporting depth 1-4 with indentation and per-level Reply visibility:</b></div>

```javascript
        // ============================================================
        // BUILD COMMENTS WITH NESTED REPLIES (max depth 4)
        // ============================================================
        if (data.comments && data.comments.length > 0) {
            function renderComment(c, depth) {
                const commentId = `comment-${c.id}`;
                const fullContent = c.content;
                const maxLength = 100;
                const shouldTruncate = fullContent.length > maxLength;
                const shortContent = shouldTruncate ? fullContent.substring(0, maxLength) + '...' : fullContent;
                const canReply = depth < 4;
                const indent = Math.min(depth - 1, 3); // max 3 levels of indent

                const replies = (c.replies || [])
                    .map(r => renderComment(r, depth + 1))
                    .join('');

                return `
                    <div class="flex items-start gap-3" data-comment-id="${c.id}">
                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(c.userName)}&background=random" 
                             class="w-${depth === 1 ? 8 : 7} h-${depth === 1 ? 8 : 7} rounded-full flex-shrink-0">
                        <div class="flex-1 min-w-0">
                            <span class="font-semibold text-sm text-gray-900">${c.userName}</span>
                            <div id="${commentId}" class="text-sm text-gray-700 break-words">
                                ${shouldTruncate ? shortContent : fullContent}
                            </div>
                            ${shouldTruncate ? `
                                <button class="text-blue-600 hover:underline text-xs mt-1 toggle-comment-btn"
                                        onclick="SohbaApp.toggleComment('${commentId}', '${fullContent.replace(/'/g, "\\'")}', '${shortContent.replace(/'/g, "\\'")}')">
                                    See more
                                </button>
                            ` : ''}
                            <div class="flex items-center gap-3 mt-1">
                                <span class="text-xs text-gray-400">${new Date(c.createdAt).toLocaleString()}</span>

                                ${canReply ? `
                                    <button onclick="SohbaApp.showReplyForm('${c.id}', '${c.userName}')" 
                                            class="text-xs text-[#345e69] hover:underline font-medium">
                                        Reply
                                    </button>
                                ` : ''}

                                ${c.replyCount > 0 ? `
                                    <button onclick="SohbaApp.toggleReplies('${c.id}')" 
                                            class="text-xs text-gray-500 hover:text-[#345e69]">
                                        View ${c.replyCount} replies
                                    </button>
                                ` : ''}

                                ${c.isAuthor ? `
                                    <button onclick="SohbaApp.deleteComment('${c.id}', '${c.postId}')"
                                            class="text-xs text-red-500 hover:underline font-medium ml-2">
                                        Delete
                                    </button>
                                ` : ''}
                            </div>

                            ${canReply ? `
                                <div id="replyForm-${c.id}" class="mt-2 hidden">
                                    <div class="flex items-start gap-3">
                                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(c.userName)}&background=345e69&color=fff" 
                                             class="w-7 h-7 rounded-full flex-shrink-0">
                                        <div class="flex-1">
                                            <input type="text" 
                                                   id="replyInput-${c.id}" 
                                                   placeholder="Reply to ${c.userName}..."
                                                   class="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#345e69]/20">
                                            <div class="flex gap-2 mt-2">
                                                <button onclick="SohbaApp.submitReply('${c.id}', '${c.postId}')" 
                                                        class="px-4 py-1.5 bg-[#345e69] text-white text-sm font-semibold rounded-lg hover:bg-[#2a4b55]">
                                                    Reply
                                                </button>
                                                <button onclick="SohbaApp.hideReplyForm('${c.id}')" 
                                                        class="px-4 py-1.5 text-sm text-gray-500 hover:text-gray-700">
                                                    Cancel
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            ` : ''}

                            ${replies ? `
                                <div id="replies-${c.id}" class="mt-3 ml-${indent + 2} border-l-2 border-slate-200 space-y-3 pl-3">
                                    ${replies}
                                </div>
                            ` : ''}
                        </div>
                    </div>
                `;
            }

            const commentsHtml = data.comments
                .map(c => renderComment(c, c.depth || 1))
                .join('');

            document.getElementById('modalComments').innerHTML = commentsHtml;
        } else {
            document.getElementById('modalComments').innerHTML = '<p class="text-slate-400 text-sm italic">No comments yet.</p>';
        }
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Required data:** One comment (level 1) under a post.
- **Navigation:** Home feed → open the post modal.
- **Expected Results:**
    - Reply on the comment creates level 2; Reply on that creates level 3; Reply on that
      creates level 4.
    - The level-4 item shows NO Reply button (frontend) — `canReply` is false.
    - Attempting to force a level-5 reply via the API returns
      "Maximum reply depth reached (4 levels)." (backend domain rule).
    - Nested replies are indented; expand/collapse still works via
      `SohbaApp.toggleReplies('{id}')`.
    - Delete works at every level (every node has `id="comment-{id}"` and
      `data-comment-id`).
- **Failure Conditions:**
    - If a level-5 reply is created, the domain depth check is not enforced.
    - If level-4 still shows Reply, the `canReply = depth < 4` guard is missing in
      `sohba-modal.js`.
- **Edge Cases:**
    - A reply whose `ParentCommentId` points to a deleted parent → `GetCommentDepthAsync`
      walks until null and returns the last existing depth (no infinite loop).
    - The `replyCount` on a level-4 node renders 0 (no further replies allowed).
    - Existing one-level replies must still render and delete correctly (the Issue 6 fix is
      preserved — every node carries `data-comment-id`).

<br>
<br>

---

<br>

# Appendix — Full File Inventory

| Layer | Path |
|-------|------|
| View | `Sohba/Views/Shared/Partials/_PostCard.cshtml` |
| View | `Sohba/Views/Shared/Partials/_Header.cshtml` |
| View | `Sohba/Views/Groups/Details.cshtml` |
| View | `Sohba/Views/Friends/Requests.cshtml` |
| View | `Sohba/Views/Notifications/Index.cshtml` |
| Controller | `Sohba/Controllers/PostsController.cs` |
| Controller | `Sohba/Controllers/GroupsController.cs` |
| Controller | `Sohba/Controllers/CommentsController.cs` |
| Application Service | `Sohba.Application/Services/InteractionService.cs` |
| Application Service | `Sohba.Application/Services/GroupService.cs` |
| Application Interface | `Sohba.Application/Interfaces/IInteractionService.cs` |
| Application DTO | `Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs` |
| Application DTO | `Sohba.Application/DTOs/UserAggregate/NotificationResponseDto.cs` |
| Application DTO | `Sohba.Application/DTOs/UserAggregate/FriendDto.cs` |
| Application DTO | `Sohba.Application/DTOs/GroupAndPageAggregate/GroupResponseDto.cs` |
| Domain Domain-Rule | `Sohba.Domain/Domain Rules/Logic/InteractionDomainService.cs` |
| Domain Domain-Rule Interface | `Sohba.Domain/Domain Rules/Interface/IInteractionDomainService.cs` |
| Domain Entity | `Sohba.Domain/Entities/UserAggregate/Notification.cs` |
| Domain Enum | `Sohba.Domain/Enums/NotificationType.cs` |
| Infrastructure Repository | `Sohba.Infrastructure/Repositories/InteractionRepository.cs` |
| Infrastructure Repository | `Sohba.Infrastructure/Repositories/GroupRepository.cs` |
| Infrastructure Repository | `Sohba.Infrastructure/Repositories/FriendshipRepository.cs` |
| JS | `Sohba/wwwroot/js/sohba-posts.js` |
| JS | `Sohba/wwwroot/js/sohba-modal.js` |
| JS | `Sohba/wwwroot/js/features/comments.js` |
| JS | `Sohba/wwwroot/js/features/friends.js` |
| JS | `Sohba/wwwroot/js/features/header.js` |
| JS | `Sohba/wwwroot/js/features/search.js` |

<br>
<br>

---

<br>

# Additional Notes

1. **Issue 1 preserves the collection model.** `RemoveSavedPostsFromCollectionsAsync`
   removes only non-Favorite rows. A post saved to both a collection and Favorites keeps
   its Favorite row after "Unsave from collection" — this is required by the existing
   FixesV4 model and the "Remove from Favorites independently" behavior.

2. **Issue 3 is a view-only fix.** The backend (`FriendshipService`, `FriendshipRepository`,
   `FriendsController`) is correct; only the view passes the wrong id. Do NOT change
   `GetPendingRequestsAsync` — `UserId = current user` and `FriendUserId = sender` is the
   intended shape for the pending list.

3. **Issue 5 needs no backend change.** `NotificationResponseDto.TargetId` and
   `NotificationType` already hold everything required to build the destination URL. Only
   the dropdown JS and the full-page view need to render clickable links.

4. **Issue 6 is fixed by adding an id to reply markup.** `deleteComment()` in
   `features/comments.js` is already correct; the bug is that reply rows never carried
   `id="comment-{id}"` or `data-comment-id`.

5. **Issue 7 — depth is enforced twice.** The frontend hides the Reply button at depth 4
   (`canReply = depth < 4`), and the backend domain rule rejects any attempt to reply to a
   depth-4 comment (`CanReplyToComment(..., currentDepth >= 4)`). Both are required by the
   task: "Do not rely only on JavaScript to enforce the limit."

6. **No migration is required** for any of the seven fixes. `Comment.Depth` is a
   computed value kept in memory for response shaping; no database column is added.

7. **The existing `AddReplyAsync` flow** (delegating to `AddCommentAsync`) automatically
   inherits the new depth validation because `AddReplyAsync` calls `AddCommentAsync` with
   the `parentCommentId`.

<br>
<br>

---

<br>

# End Of Document

This document is a complete implementation guide for the seven issues listed above. No
project source files were modified while producing it.