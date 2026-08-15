# Sohba — AlternativeClaudeResult Verification Report

<br>
<br>
<br>

**Document Name:** AlternativeClaudeResult.md

**Purpose:** Verification report of the fixes applied from `AlternativeClaude.md`, including
retesting results, remaining issues, and required corrections.

**Author Role:** Senior Software Architect / Senior ASP.NET Core MVC Engineer / Senior .NET
Backend Engineer / Senior Frontend Engineer / Senior JavaScript Engineer / Code Reviewer /
QA Engineer.

**Scope:** This document verifies the current project state against the fixes from
`AlternativeClaude.md`. It does NOT re-analyze unrelated parts of the app.

**Important:** No project source file was modified while writing this document. This is a
verification and correction guide only.

<br>
<br>

---

<br>

# TABLE OF CONTENTS

1. [Executive Summary](#executive-summary)
2. [Applied Changes Verification](#applied-changes-verification)
3. [Not Yet Applied / Partially Applied Changes](#not-yet-applied--partially-applied-changes)
4. [Found Issues During Retesting](#found-issues-during-retesting)
5. [Additional Issues Found](#additional-issues-found)
6. [Missing Files / Missing Methods / Missing DTOs](#missing-files--missing-methods--missing-dtos)
7. [Required Fixes](#required-fixes)
8. [Regression Checklist](#regression-checklist)
9. [Final Notes](#final-notes)

<br>
<br>

---

<br>

# Executive Summary

The fixes from `AlternativeClaude.md` were partially applied. The user stopped at Issue 5.1
and tested the app. The following is the overall status:

| Area | Status |
|------|--------|
| Issue 3.1 — Create Post Modal | ✅ FIXED |
| Issue 3.2 — Infinite Scroll / Profile duplication | ⚠️ PARTIALLY FIXED |
| Issue 3.5 — Reply / Delete Comment | ❌ STILL BROKEN |
| Issue 3.10 — Save Post / Add To Favorites | ❌ STILL BROKEN (EF tracking exception) |
| Issue 4.2 — Story Viewer | ✅ FIXED (viewer) |
| Issue 4.2 — Story Creation | ⚠️ RUNTIME ISSUE (server not running) |
| Seeder duplication | ✅ FIXED |
| FriendshipRepository reversed direction | ✅ FIXED |
| tailwind.css 404 | ✅ FIXED |
| GetPostDetails comment tree | ✅ FIXED |
| Settings checkboxes | ✅ FIXED |
| Groups/Details conditional actions | ✅ FIXED |
| JS namespace aliases | ✅ FIXED |
| Profile page post loop | ✅ FIXED |
| feed.js dedupe + debounce | ✅ FIXED |

**Bottom line:** 11 fixes are confirmed applied. 4 issues remain broken (3.2 partially,
3.5, 3.10, 4.2 runtime). 11 additional issues remain unaddressed.

<br>
<br>

---

<br>

# Applied Changes Verification

The following fixes from `AlternativeClaude.md` are **confirmed applied** in the current
project state.

<br>

## 1. Issue 3.1 — Create Post Modal Typo Fixed

**File:** `Sohba/Views/Shared/Partials/_CreatePost.cshtml`

**Location:** Line 165

**Verified:** The typo `f (file.size > 5 * 1024 * 1024)` is now `if (file.size > 5 * 1024 * 1024)`.

**Status:** ✅ FIXED

<br>

## 2. Seeder Duplication Fixed

**File:** `Sohba.Infrastructure/DBInitializer/DBInitializer.cs`

**Methods:** `CreateGroupAsync`, `CreatePageAsync`

**Verified:** Both methods now check `Name == name` before creating a new row and return the
existing row if found.

**Status:** ✅ FIXED

<br>

## 3. FriendshipRepository Reversed Direction Fixed

**File:** `Sohba.Infrastructure/Repositories/FriendshipRepository.cs`

**Methods:** `GetByUsersAsync` (line 74), `HasPendingRequestAsync` (line 118)

**Verified:** Both methods now return the reversed-direction lookup result.

**Status:** ✅ FIXED

<br>

## 4. tailwind.css 404 Fixed

**File:** `Sohba/Views/Shared/_AppLayout.cshtml`

**Verified:** The `<link rel="stylesheet" href="~/css/tailwind.css" />` line is removed.

**Status:** ✅ FIXED

<br>

## 5. GetPostDetails Comment Tree Fixed

**File:** `Sohba/Controllers/PostsController.cs`

**Method:** `GetPostDetails` (line 114)

**Verified:** The response now includes `parentCommentId`, `replyCount`, `isAuthor`, and
`replies` for each comment.

**Status:** ✅ FIXED

<br>

## 6. Settings Checkboxes Fixed

**File:** `Sohba/Views/Profile/Settings.cshtml`

**Verified:** The notification checkboxes now use `asp-for="EmailNotifications"`,
`asp-for="PushNotifications"`, and `asp-for="WeeklyDigest"`.

**Status:** ✅ FIXED

<br>

## 7. Groups/Details Conditional Actions Fixed

**File:** `Sohba/Views/Groups/Details.cshtml`

**Verified:** Edit Group is shown only when `ViewBag.CurrentUserId == Model.Group.AdminId`.
Leave Group is shown only when `Model.Group.IsCurrentUserMember`. Join Group is shown for
non-members.

**Status:** ✅ FIXED

<br>

## 8. JS Namespace Aliases Fixed

**File:** `Sohba/wwwroot/js/sohba-posts.js`

**Location:** End of file

**Verified:** The aliases exist:

```javascript
window.SohbaApp.showReplyForm = window.showReplyForm;
window.SohbaApp.hideReplyForm  = window.hideReplyForm;
window.SohbaApp.submitReply    = window.submitReply;
window.SohbaApp.toggleReplies  = window.toggleReplies;
```

**Status:** ✅ FIXED

<br>

## 9. Story Viewer DTO Unwrap Fixed

**File:** `Sohba/wwwroot/js/sohba-stories.js`

**Method:** `openStoryViewer`

**Verified:** The code now unwraps the `BaseResponseDto`:

```javascript
const stories = payload.data ?? payload.Data ?? (Array.isArray(payload) ? payload : []);
```

**Status:** ✅ FIXED

<br>

## 10. Profile Page Post Loop Fixed

**File:** `Sohba/Views/Profile/Index.cshtml`

**Location:** Line 157

**Verified:** The `@foreach` loop was replaced with a single render:

```html
<partial name="Partials/_PostCard" model="Model.Posts" />
```

**Status:** ✅ FIXED

<br>

## 11. feed.js Dedupe + Debounce Fixed

**File:** `Sohba/wwwroot/js/features/feed.js`

**Verified:** The `renderedPostIds` Set and `requestAnimationFrame` scroll throttle are
present. The `loadMorePosts` function filters out already-rendered post IDs.

**Status:** ✅ FIXED

<br>
<br>

---

<br>

# Not Yet Applied / Partially Applied Changes

The following fixes from `AlternativeClaude.md` are **NOT applied** or **only partially
applied**.

<br>

## 1. Issue 3.2 — Report/Share Modals Still Inside _PostCard

**File:** `Sohba/Views/Shared/Partials/_PostCard.cshtml`

**Location:** Lines 366-441 (`reportModal`), Lines 448-505 (`shareModal`)

**Status:** ❌ NOT APPLIED

**Problem:** The `postModal` was extracted to `_PostModal.cshtml`, but `reportModal` and
`shareModal` are still inside the partial. Every post card renders a full copy of both
modals, causing duplicate `id="reportModal"` and `id="shareModal"` in the DOM. This
contributes to the duplication issue.

<br>

## 2. Issue 3.5 — Reply/Delete Comment

**File:** `Sohba/wwwroot/js/sohba-modal.js`

**Location:** Line 79

**Status:** ❌ STILL BROKEN

**Problem:** The `reply.isAuthor` block is placed OUTSIDE the `c.replies.map(reply => ...)`
block. The `reply` variable is out of scope, causing `ReferenceError: reply is not defined`.

<br>

## 3. Issue 3.10 — SavedPost EF Configuration

**File:** `Sohba.Infrastructure/Data/Configurations/SavedPostConfiguration.cs`

**Location:** Line 15

**Status:** ❌ STILL BROKEN

**Problem:** The entity was changed to use `Id` as PK, but the EF configuration still uses
the composite key `(UserId, PostId)`. This causes the tracking exception.

<br>

## 4. Issue 4.2 — Story Creation

**File:** `Sohba/Views/Shared/Partials/_CreateStoryModal.cshtml`

**Status:** ⚠️ RUNTIME ISSUE

**Problem:** `ERR_CONNECTION_REFUSED` means the server was not running when the browser made
the request. The code itself is correct.

<br>

## 5. HomeController.LoadMore Dead Code

**File:** `Sohba/Controllers/HomeController.cs`

**Location:** Line 127

**Status:** ❌ NOT APPLIED

**Problem:** The `LoadMore` action is still present but never called by `feed.js`.

<br>

## 6. GetTimeAgo Timezone Bug

**File:** `Sohba/Views/Shared/Partials/_PostCard.cshtml`

**Location:** Line 9

**Status:** ❌ NOT APPLIED

**Problem:** `var timeSpan = DateTime.UtcNow - createdAt.ToLocalTime();` mixes timezones.

<br>

## 7. Groups/Details ViewBag.Posts

**File:** `Sohba/Views/Groups/Details.cshtml`

**Location:** Line 89

**Status:** ❌ NOT APPLIED

**Problem:** `<partial name="Partials/_PostCard" model="ViewBag.Posts" />` references
`ViewBag.Posts` but the controller never sets it.

<br>

## 8. Pages/Details Edit Button

**File:** `Sohba/Views/Pages/Details.cshtml`

**Location:** Lines 38-47

**Status:** ❌ NOT APPLIED

**Problem:** The Edit Page button is shown unconditionally. The `@if` guard is still
commented out.

<br>

## 9. CommentsController Anti-Forgery

**File:** `Sohba/Controllers/CommentsController.cs`

**Method:** `Delete`

**Status:** ❌ NOT APPLIED

**Problem:** No `[ValidateAntiForgeryToken]` attribute on the Delete action.

<br>

## 10. Sidebar joinGroupFromSidebar Payload

**File:** `Sohba/Views/Shared/Partials/_Sidebar.cshtml`

**Method:** `joinGroupFromSidebar`

**Status:** ❌ NOT APPLIED

**Problem:** Uses `{ groupId }` but the `Join` endpoint expects `{ id: groupId }`.

<br>

## 11. FriendRequest Rate Limit

**File:** `Sohba/Program.cs`

**Location:** Line 148-154

**Status:** ❌ NOT APPLIED

**Problem:** `PermitLimit = 10` is still too low for UI retries.

<br>
<br>

---

<br>

# Found Issues During Retesting

The following issues were reported by the user during retesting. Each is verified with its
root cause.

<br>

## Issue 3.2 — Posts Duplicate In Infinite Scroll & On Profile Pages

### Reported

Posts still duplicate.

### Verified Root Cause

The `postModal` was extracted, but `reportModal` and `shareModal` remain inside
`_PostCard.cshtml`. Every post card renders a full copy of both modals. When the infinite
scroll appends new cards, the DOM accumulates multiple `id="reportModal"` and
`id="shareModal"` elements.

Additionally, the server-side pagination in `PostRepository.GetTimelineAsync` still uses
`Skip((page - 1) * pageSize)` — offset-based pagination. If a new post is created between
page loads, the offset shifts and posts repeat. The client-side dedupe masks this, but the
underlying issue remains.

### Files

- `Sohba/Views/Shared/Partials/_PostCard.cshtml`
- `Sohba.Infrastructure/Repositories/PostRepository.cs`

### Status

⚠️ PARTIALLY FIXED

<br>

## Issue 3.5 — Reply / Delete Comment

### Reported

- `ReferenceError: reply is not defined`
- Clicking Reply shows toast: `Failed to load post`
- After that, the post modal cannot be opened again
- The avatar of the reply author is shown as the post owner instead of the current user
- The Delete Comment button does not appear immediately after posting a comment

### Verified Root Causes

**Root Cause 1 — `reply` out of scope:**

`Sohba/wwwroot/js/sohba-modal.js` line 79:

```javascript
${reply.isAuthor ? `...` : ''}
```

This is placed OUTSIDE the `c.replies.map(reply => ...)` block (lines 66-76). The `reply`
variable is undefined at that point. When a comment has replies, this throws
`ReferenceError`, which aborts the entire `openPostModal` render. The catch block shows
"Failed to load post" and closes the modal. The modal cannot be reopened because the error
occurs every time.

**Root Cause 2 — avatar shows post owner:**

`Sohba/wwwroot/js/sohba-posts.js` `submitComment` function. The reply form avatar is
hard-coded:

```javascript
<img src="https://ui-avatars.com/api/?name=You&background=345e69&color=fff" ...>
```

It should use the current user's actual name.

**Root Cause 3 — Delete button not immediate:**

`submitComment` appends the comment HTML client-side, but the `isAuthor` flag is not set in
the client-side template. The Delete button only appears after reopening the modal (when the
server returns `isAuthor: true`).

### Files

- `Sohba/wwwroot/js/sohba-modal.js`
- `Sohba/wwwroot/js/sohba-posts.js`

### Status

❌ STILL BROKEN

<br>

## Issue 3.10 — Save Post / Add To Favorites

### Reported

- Save Post works.
- Add To Favorites works only if the post is not already saved.
- If the post is already in Saved and I click Add To Favorites, I get:

```
System.InvalidOperationException
The instance of entity type 'SavedPost' cannot be tracked because another instance with the same key value for {'UserId', 'PostId'} is already being tracked.
```

### Verified Root Cause

**File:** `Sohba.Infrastructure/Data/Configurations/SavedPostConfiguration.cs` line 15:

```csharp
builder.HasKey(sp => new { sp.UserId, sp.PostId });
```

The entity was changed to use `Id` as the primary key (per FixesV1.md), but the EF
configuration was **never updated**. The migration `20260806085753_AddSavedCollections`
also still has `b.HasKey("UserId", "PostId")`.

**Execution flow of the exception:**

```
User clicks Add To Favorites on an already-saved post
    → PostsController.ToggleFavorite
        → InteractionService.SavePostToFavoritesAsync(userId, postId)
            → GetCollectionsByUserAsync(userId)
                → InteractionRepository.GetCollectionsByUserAsync
                    → .Include(c => c.SavedPosts)   ← loads existing SavedPost rows into tracker
            → GetSavedPostByCollectionAsync(userId, postId, favorites.Id)
                → returns null (post is in "Saved" collection, not "Favorites")
            → new SavedPost { Id = Guid.NewGuid(), UserId, PostId, CollectionId = favorites.Id }
            → AddSavedPost(savedPost)
                → EF sees another tracked SavedPost with same (UserId, PostId) composite key
                → InvalidOperationException
```

### Files

- `Sohba.Infrastructure/Data/Configurations/SavedPostConfiguration.cs`
- `Sohba.Infrastructure/Migrations/20260806085753_AddSavedCollections.cs`
- `Sohba.Application/Services/InteractionService.cs` (`SavePostToFavoritesAsync`)

### Status

❌ STILL BROKEN

<br>

## Issue 4.2 — Story Creation

### Reported

```
POST /Stories/Create
ERR_CONNECTION_REFUSED
Failed to fetch
```

### Verified Root Cause

This is a **runtime/environment issue**, not a code bug. `ERR_CONNECTION_REFUSED` means the
server was not running when the browser made the request. The `StoriesController.Create`
action and the `_CreateStoryModal.cshtml` submit logic are correct.

The story viewer DTO unwrap fix is confirmed applied in `sohba-stories.js`.

### Status

⚠️ RUNTIME ISSUE — restart the server and retest.

<br>
<br>

---

<br>

# Additional Issues Found

The following issues were verified in the current project state.

<br>

## 1. HomeController.LoadMore Is Dead Code

**File:** `Sohba/Controllers/HomeController.cs`

**Method:** `LoadMore` (line 127)

**Status:** ❌ STILL PRESENT

**Impact:** Dead endpoint, wasted surface area.

<br>

## 2. GetTimeAgo Uses Mixed Timezone Math

**File:** `Sohba/Views/Shared/Partials/_PostCard.cshtml`

**Method:** `GetTimeAgo` (line 9)

**Status:** ❌ STILL BROKEN

**Code:**

```csharp
var timeSpan = DateTime.UtcNow - createdAt.ToLocalTime();
```

**Impact:** Incorrect relative timestamps.

<br>

## 3. Groups/Details References ViewBag.Posts That Is Never Set

**File:** `Sohba/Views/Groups/Details.cshtml`

**Location:** Line 89

**Status:** ❌ STILL PRESENT

**Code:**

```html
<partial name="Partials/_PostCard" model="ViewBag.Posts" />
```

**Impact:** Empty initial render; posts load via AJAX.

<br>

## 4. Pages/Details Edit Button Shown Unconditionally

**File:** `Sohba/Views/Pages/Details.cshtml`

**Location:** Lines 38-47

**Status:** ❌ STILL BROKEN

**Impact:** Non-admin users see "Edit Page" and get a 403 on click.

<br>

## 5. CommentsController.Delete Missing Anti-Forgery

**File:** `Sohba/Controllers/CommentsController.cs`

**Method:** `Delete`

**Status:** ❌ STILL MISSING

**Impact:** CSRF risk.

<br>

## 6. Sidebar joinGroupFromSidebar Uses Wrong Payload

**File:** `Sohba/Views/Shared/Partials/_Sidebar.cshtml`

**Method:** `joinGroupFromSidebar`

**Status:** ❌ STILL BROKEN

**Code:**

```javascript
const result = await SohbaApp.post('/Groups/Join', { groupId });
```

**Should be:**

```javascript
const result = await SohbaApp.post('/Groups/Join', { id: groupId });
```

**Impact:** Joining groups from the sidebar fails with "Invalid group ID".

<br>

## 7. FriendRequest Rate Limit Still Too Low

**File:** `Sohba/Program.cs`

**Location:** Lines 148-154

**Status:** ❌ STILL 10/min

**Impact:** 429 responses on rapid accept/reject clicks.

<br>

## 8. Dashboard Inline Scripts Not Consolidated

**Files:**

- `Sohba/Views/Dashboard/Users.cshtml`
- `Sohba/Views/Dashboard/Posts.cshtml`
- `Sohba/Views/Dashboard/Reports.cshtml`

**Status:** ❌ STILL INLINE

**Impact:** Duplicate logic with `features/dashboard.js`.

<br>

## 9. Profile Links On Names Not Clickable

**Files:**

- `Sohba/Views/Shared/Partials/_PostCard.cshtml`
- `Sohba/wwwroot/js/sohba-modal.js`
- `Sohba/Views/Groups/Details.cshtml`
- `Sohba/Views/Friends/Index.cshtml`

**Status:** ❌ NOT IMPLEMENTED

**Impact:** Users cannot click a name to open a profile.

<br>

## 10. Banner / Cover Image Support Missing

**Files:**

- `Sohba/Views/Groups/Details.cshtml`
- `Sohba/Views/Pages/Details.cshtml`
- `Sohba/Views/Profile/Index.cshtml`

**Status:** ❌ NOT IMPLEMENTED

**Impact:** Feature request missing.

<br>

## 11. Search Links Inconsistent

**File:** `Sohba/wwwroot/js/features/search.js`

**Status:** ❌ INCONSISTENT

**Impact:** `/Search?q=` vs `/Search/Index?q=`.

<br>
<br>

---

<br>

# Missing Files / Missing Methods / Missing DTOs

The following are missing or incorrect in the current project state.

<br>

## 1. SavedPost EF Configuration Key Mismatch

**File:** `Sohba.Infrastructure/Data/Configurations/SavedPostConfiguration.cs`

**Missing:** The `Id` primary key configuration and the `CollectionId` foreign key
configuration.

**Current (wrong):**

```csharp
builder.HasKey(sp => new { sp.UserId, sp.PostId });
```

**Required:**

```csharp
builder.HasKey(sp => sp.Id);
```

<br>

## 2. Missing Shared Partial For Report Modal

**File:** `Sohba/Views/Shared/Partials/_ReportModal.cshtml`

**Status:** ❌ MISSING

**Required:** Extract the `reportModal` markup from `_PostCard.cshtml` into this new partial.

<br>

## 3. Missing Shared Partial For Share Modal

**File:** `Sohba/Views/Shared/Partials/_ShareModal.cshtml`

**Status:** ❌ MISSING

**Required:** Extract the `shareModal` markup from `_PostCard.cshtml` into this new partial.

<br>

## 4. Missing Migration For SavedPost Key Change

**File:** `Sohba.Infrastructure/Migrations/` (new migration required)

**Status:** ❌ MISSING

**Required:** A new migration that changes the `SavedPost` primary key from
`(UserId, PostId)` to `Id` and adds the `CollectionId` foreign key.

<br>

## 5. Missing Current User Name In Reply Form Avatar

**File:** `Sohba/wwwroot/js/sohba-posts.js`

**Method:** `submitComment`

**Missing:** The current user's name for the reply form avatar. Currently hard-coded as
`name=You`.

<br>

## 6. Missing isAuthor Flag In Client-Side Comment Template

**File:** `Sohba/wwwroot/js/sohba-posts.js`

**Method:** `submitComment`

**Missing:** The `isAuthor: true` flag in the client-side comment HTML so the Delete button
appears immediately.

<br>
<br>

---

<br>

# Required Fixes

The following fixes are required. Each includes the exact file path, method name, location,
and the code change.

<br>

## Fix 1 — Issue 3.2: Extract Report/Share Modals From _PostCard

### File: Sohba/Views/Shared/Partials/_PostCard.cshtml

🔴 REMOVED — the entire `reportModal` block (lines 366-441) and the entire `shareModal`
block (lines 448-505):

```html
    @Html.AntiForgeryToken()
    <!-- Report Post Modal -->
    <div id="reportModal" class="fixed inset-0 z-[100] hidden">
        ...
    </div>

    <!-- Share Post Modal -->
    <div id="shareModal" class="fixed inset-0 z-[100] hidden">
        ...
    </div>
```

### File: Sohba/Views/Shared/Partials/_ReportModal.cshtml

🟢 ADDED — new file (entire content):

```html
@Html.AntiForgeryToken()
<!-- Report Post Modal -->
<div id="reportModal" class="fixed inset-0 z-[100] hidden">
    <!-- Overlay -->
    <div class="absolute inset-0 bg-black/60" onclick="SohbaApp.closeReportModal()"></div>

    <!-- Modal Container -->
    <div class="absolute inset-0 flex items-center justify-center p-4">
        <div class="bg-white w-full max-w-md rounded-2xl shadow-2xl overflow-hidden">
            <!-- Header -->
            <div class="flex items-center justify-between p-4 border-b">
                <h3 class="text-lg font-bold text-gray-900">Report Post</h3>
                <button onclick="SohbaApp.closeReportModal()" class="p-1 text-gray-400 hover:text-gray-600">
                    <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>

            <!-- Content -->
            <div class="p-4">
                <p class="text-sm text-gray-600 mb-4">Why are you reporting this post? Your report is anonymous.</p>

                <div class="space-y-3">
                    <label class="flex items-center p-3 border rounded-xl cursor-pointer hover:bg-slate-50 transition-colors">
                        <input type="radio" name="reportReason" value="Spam" class="w-4 h-4 text-[#345e69] focus:ring-[#345e69]">
                        <span class="ml-3 text-sm font-medium text-gray-700">Spam</span>
                    </label>

                    <label class="flex items-center p-3 border rounded-xl cursor-pointer hover:bg-slate-50 transition-colors">
                        <input type="radio" name="reportReason" value="Harassment" class="w-4 h-4 text-[#345e69] focus:ring-[#345e69]">
                        <span class="ml-3 text-sm font-medium text-gray-700">Harassment or bullying</span>
                    </label>

                    <label class="flex items-center p-3 border rounded-xl cursor-pointer hover:bg-slate-50 transition-colors">
                        <input type="radio" name="reportReason" value="InappropriateContent" class="w-4 h-4 text-[#345e69] focus:ring-[#345e69]">
                        <span class="ml-3 text-sm font-medium text-gray-700">Inappropriate content</span>
                    </label>

                    <label class="flex items-center p-3 border rounded-xl cursor-pointer hover:bg-slate-50 transition-colors">
                        <input type="radio" name="reportReason" value="Violence" class="w-4 h-4 text-[#345e69] focus:ring-[#345e69]">
                        <span class="ml-3 text-sm font-medium text-gray-700">Violence or dangerous organizations</span>
                    </label>

                    <label class="flex items-center p-3 border rounded-xl cursor-pointer hover:bg-slate-50 transition-colors">
                        <input type="radio" name="reportReason" value="Other" class="w-4 h-4 text-[#345e69] focus:ring-[#345e69]">
                        <span class="ml-3 text-sm font-medium text-gray-700">Something else</span>
                    </label>
                </div>

                <div id="otherReasonContainer" class="mt-4 hidden">
                    <textarea id="otherReasonText" rows="3" placeholder="Please describe the issue..."
                              class="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69] transition-all"></textarea>
                </div>
            </div>

            <!-- Footer -->
            <div class="flex gap-3 p-4 border-t bg-slate-50">
                <button onclick="SohbaApp.closeReportModal()"
                        class="flex-1 py-2.5 border border-gray-200 text-gray-600 font-semibold rounded-xl hover:bg-gray-100 transition-colors">
                    Cancel
                </button>
                <button onclick="SohbaApp.submitReport()"
                        class="flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-xl shadow-lg shadow-red-600/30 transition-colors">
                    Submit Report
                </button>
            </div>
        </div>
    </div>
</div>
```

### File: Sohba/Views/Shared/Partials/_ShareModal.cshtml

🟢 ADDED — new file (entire content):

```html
<!-- Share Post Modal -->
<div id="shareModal" class="fixed inset-0 z-[100] hidden">
    <!-- Overlay -->
    <div class="absolute inset-0 bg-black/60" onclick="SohbaApp.closeShareModal()"></div>

    <!-- Modal Container -->
    <div class="absolute inset-0 flex items-center justify-center p-4">
        <div class="bg-white w-full max-w-md rounded-2xl shadow-2xl overflow-hidden">
            <!-- Header -->
            <div class="flex items-center justify-between p-4 border-b">
                <h3 class="text-lg font-bold text-gray-900">Share Post</h3>
                <button onclick="SohbaApp.closeShareModal()" class="p-1 text-gray-400 hover:text-gray-600">
                    <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>

            <!-- Content -->
            <div class="p-6">
                <p class="text-sm text-gray-600 mb-3">Share this post with others</p>

                <!-- Post URL -->
                <div class="flex items-center gap-2 mb-4">
                    <input id="sharePostUrl" type="text" readonly
                           class="flex-1 px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none"
                           value="">
                    <button onclick="SohbaApp.copyShareLink()"
                            class="px-4 py-3 bg-[#345e69] text-white font-semibold rounded-xl hover:bg-[#2a4b55] transition-colors flex items-center gap-2">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3" />
                        </svg>
                        Copy
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>
```

### File: Sohba/Views/Shared/_AppLayout.cshtml

🟢 ADDED — include the new modals once (next to the other global modals):

```html
    <!-- Confirm Modal -->
    <partial name="Partials/_ConfirmModal" />

    <!-- Post Modal (single global instance) -->
    <partial name="Partials/_PostModal" />

    <!-- Report Modal (single global instance) -->
    <partial name="Partials/_ReportModal" />

    <!-- Share Modal (single global instance) -->
    <partial name="Partials/_ShareModal" />

    <!-- Save Post Modal -->
    <partial name="Partials/_SavePostModal" />
```

<br>
<br>

---

<br>

## Fix 2 — Issue 3.5: Fix `reply` Out Of Scope In sohba-modal.js

### File: Sohba/wwwroot/js/sohba-modal.js

🔴 REMOVED — the misplaced `reply.isAuthor` block (lines 78-84):

```javascript
                            <!-- Delete Reply Button -->
                            ${reply.isAuthor ? `
                                    <button onclick="SohbaApp.deleteComment('${reply.id}', '${reply.postId}')"
                                            class="text-xs text-red-500 hover:underline font-medium ml-2">
                                        Delete
                                    </button>
                                ` : ''}
```

🟢 ADDED — move the Delete Reply button INSIDE the `c.replies.map(reply => ...)` block,
after the timestamp span:

```javascript
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
```

<br>
<br>

---

<br>

## Fix 3 — Issue 3.5: Fix Reply Author Avatar

### File: Sohba/wwwroot/js/sohba-posts.js

🔴 REMOVED — the hard-coded avatar in `submitComment`:

```javascript
                                <img src="https://ui-avatars.com/api/?name=You&background=345e69&color=fff"
                                     class="w-7 h-7 rounded-full flex-shrink-0">
```

🟢 ADDED — use the current user's name from the server response:

```javascript
                                <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(result.comment.userName)}&background=345e69&color=fff"
                                     class="w-7 h-7 rounded-full flex-shrink-0">
```

<br>
<br>

---

<br>

## Fix 4 — Issue 3.5: Show Delete Button Immediately After Posting

### File: Sohba/wwwroot/js/sohba-posts.js

🔴 REMOVED — the comment template in `submitComment` that lacks the `isAuthor` flag:

```javascript
                        <div class="flex items-center gap-3 mt-1">
                            <span class="text-xs text-gray-400">${new Date(result.comment.createdAt).toLocaleString()}</span>
                            <button onclick="SohbaApp.showReplyForm('${result.comment.id}', '${result.comment.userName}')"
                                    class="text-xs text-[#345e69] hover:underline font-medium">
                                Reply
                            </button>
                        </div>
```

🟢 ADDED — include the Delete button with `isAuthor: true`:

```javascript
                        <div class="flex items-center gap-3 mt-1">
                            <span class="text-xs text-gray-400">${new Date(result.comment.createdAt).toLocaleString()}</span>
                            <button onclick="SohbaApp.showReplyForm('${result.comment.id}', '${result.comment.userName}')"
                                    class="text-xs text-[#345e69] hover:underline font-medium">
                                Reply
                            </button>
                            <button onclick="SohbaApp.deleteComment('${result.comment.id}', '${result.comment.postId}')"
                                    class="text-xs text-red-500 hover:underline font-medium ml-2">
                                Delete
                            </button>
                        </div>
```

<br>
<br>

---

<br>

## Fix 5 — Issue 3.10: Fix SavedPost EF Configuration

### File: Sohba.Infrastructure/Data/Configurations/SavedPostConfiguration.cs

🔴 REMOVED — the composite key configuration:

```csharp
            builder.HasKey(sp => new { sp.UserId, sp.PostId });
```

🟢 ADDED — the Id primary key and CollectionId foreign key:

```csharp
            builder.HasKey(sp => sp.Id);

            builder.HasOne(sp => sp.User)
                   .WithMany(u => u.SavedPosts)
                   .HasForeignKey(sp => sp.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sp => sp.Post)
                   .WithMany()
                   .HasForeignKey(sp => sp.PostId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sp => sp.Collection)
                   .WithMany(c => c.SavedPosts)
                   .HasForeignKey(sp => sp.CollectionId)
                   .OnDelete(DeleteBehavior.Cascade);
```

### File: CLI (run in Sohba project directory)

🟢 ADDED — create and apply the new migration:

```bash
cd Sohba
dotnet ef migrations add FixSavedPostPrimaryKey
dotnet ef database update
```

<br>
<br>

---

<br>

## Fix 6 — GetTimeAgo Timezone Bug

### File: Sohba/Views/Shared/Partials/_PostCard.cshtml

🔴 REMOVED — the mixed timezone math:

```csharp
        var timeSpan = DateTime.UtcNow - createdAt.ToLocalTime();
```

🟢 ADDED — use UTC consistently:

```csharp
        var timeSpan = DateTime.UtcNow - createdAt;
```

<br>
<br>

---

<br>

## Fix 7 — Groups/Details ViewBag.Posts

### File: Sohba/Views/Groups/Details.cshtml

🔴 REMOVED — the empty initial partial:

```html
            <div id="group-content-area">
                <partial name="Partials/_PostCard" model="ViewBag.Posts" />
            </div>
```

🟢 ADDED — an empty placeholder that the AJAX call will replace:

```html
            <div id="group-content-area">
                <div class="text-center py-10 text-gray-500">Loading posts...</div>
            </div>
```

<br>
<br>

---

<br>

## Fix 8 — Pages/Details Edit Button

### File: Sohba/Views/Pages/Details.cshtml

🔴 REMOVED — the unconditional Edit Page button:

```html
                @* @if (User.Identity?.Name == Model.AdminName)
                { *@
                    <a asp-action="Edit" asp-route-id="@Model.Id"
                       class="px-5 py-2.5 bg-slate-100 text-gray-700 font-bold rounded-xl hover:bg-slate-200 transition-all flex items-center gap-2">
                        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                        Edit Page
                    </a>
                @* } *@
```

🟢 ADDED — the conditional Edit Page button:

```html
                @if (ViewBag.CurrentUserId == Model.AdminId)
                {
                    <a asp-action="Edit" asp-route-id="@Model.Id"
                       class="px-5 py-2.5 bg-slate-100 text-gray-700 font-bold rounded-xl hover:bg-slate-200 transition-all flex items-center gap-2">
                        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                        Edit Page
                    </a>
                }
```

### File: Sohba/Controllers/PagesController.cs

🟢 ADDED — set ViewBag.CurrentUserId in the Details action:

```csharp
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _pageService.GetPageByIdAsync(id);

            if (result.IsFailure)
                return NotFound();

            ViewBag.CurrentUserId = GetCurrentUserId();
            return View(result.Value);
        }
```

<br>
<br>

---

<br>

## Fix 9 — CommentsController Anti-Forgery

### File: Sohba/Controllers/CommentsController.cs

🔴 REMOVED — the Delete action without anti-forgery:

```csharp
        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] IdRequestDto request)
```

🟢 ADDED — the Delete action with anti-forgery:

```csharp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] IdRequestDto request)
```

<br>
<br>

---

<br>

## Fix 10 — Sidebar joinGroupFromSidebar Payload

### File: Sohba/Views/Shared/Partials/_Sidebar.cshtml

🔴 REMOVED — the wrong payload:

```javascript
    async function joinGroupFromSidebar(groupId, buttonElement) {
        const result = await SohbaApp.post('/Groups/Join', { groupId });
```

🟢 ADDED — the correct payload:

```javascript
    async function joinGroupFromSidebar(groupId, buttonElement) {
        const result = await SohbaApp.post('/Groups/Join', { id: groupId });
```

<br>
<br>

---

<br>

## Fix 11 — FriendRequest Rate Limit

### File: Sohba/Program.cs

🔴 REMOVED — the low limit:

```csharp
                    options.AddFixedWindowLimiter("FriendRequest", opt =>
                    {
                        opt.PermitLimit = 10;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = 0;
                    });
```

🟢 ADDED — the raised limit with queue:

```csharp
                    options.AddFixedWindowLimiter("FriendRequest", opt =>
                    {
                        opt.PermitLimit = 30;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = 2;
                    });
```

<br>
<br>

---

<br>

# Regression Checklist

Use the following checklist to verify each fix after applying it.

<br>

## Issue 3.2 — Posts Duplication

- [ ] Login as `mohammed@sohba.com`.
- [ ] Open Home feed.
- [ ] Scroll to bottom repeatedly.
- [ ] Verify each post appears exactly once.
- [ ] Open DevTools and verify there is exactly ONE `#reportModal` and ONE `#shareModal`.
- [ ] Open own profile page.
- [ ] Verify each post appears exactly once.

<br>

## Issue 3.5 — Reply / Delete Comment

- [ ] Login as `mohammed@sohba.com`.
- [ ] Open a post that has a comment with replies.
- [ ] Verify NO `ReferenceError: reply is not defined` in the console.
- [ ] Click Reply on a comment.
- [ ] Verify the inline reply form appears.
- [ ] Submit a reply.
- [ ] Verify the reply appears under the parent comment.
- [ ] Verify the reply author avatar shows the current user's name.
- [ ] Post a new comment.
- [ ] Verify the Delete button appears IMMEDIATELY (without reopening the modal).
- [ ] Click Delete and verify the confirmation modal appears.

<br>

## Issue 3.10 — Save Post / Add To Favorites

- [ ] Login as `mohammed@sohba.com`.
- [ ] Save a post to a collection.
- [ ] Click Add To Favorites on the SAME post.
- [ ] Verify NO `InvalidOperationException`.
- [ ] Verify the post appears on `/Posts/Favorites`.
- [ ] Click Add To Favorites again.
- [ ] Verify the post is removed from Favorites.

<br>

## Issue 4.2 — Story Creation

- [ ] Start the server (`dotnet run`).
- [ ] Login as `mohammed@sohba.com`.
- [ ] Click "Add Story".
- [ ] Upload an image.
- [ ] Click "Share to Story".
- [ ] Verify the story appears in the story rail.
- [ ] Click the story card.
- [ ] Verify the story viewer opens.

<br>

## Additional Fixes

- [ ] Verify `GetTimeAgo` shows correct relative timestamps.
- [ ] Verify Groups/Details shows "Loading posts..." initially, then loads via AJAX.
- [ ] Verify Pages/Details hides Edit Page for non-admins.
- [ ] Verify `joinGroupFromSidebar` works from the sidebar.
- [ ] Verify Accept/Reject friend requests do not hit 429.
- [ ] Verify `CommentsController.Delete` rejects requests without anti-forgery token.

<br>
<br>

---

<br>

# Final Notes

1. **Apply Fix 5 (SavedPost EF Configuration) FIRST** — it is the root cause of the
   `InvalidOperationException` in Issue 3.10. Without it, the Favorites feature cannot work.

2. **Apply Fix 2 (reply out of scope) SECOND** — it is the root cause of the
   `ReferenceError` and the "Failed to load post" toast in Issue 3.5. Without it, the post
   modal cannot be reopened when a comment has replies.

3. **Apply Fix 1 (extract report/share modals) THIRD** — it resolves the remaining
   duplication in Issue 3.2.

4. **Issue 4.2 Story Creation** is a runtime issue — restart the server and retest. The code
   is correct.

5. **No project source file was modified while writing this document.** Every code change
   above is a recommendation for the implementing developer.

<br>
<br>

---

<br>

# End Of Document

This document is a verification report and correction guide for the implementation of
`AlternativeClaude.md`. No project source files were modified while producing it.