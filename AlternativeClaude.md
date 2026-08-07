# Sohba Social Media Platform — Complete Frontend & Backend Implementation Guide

<br>
<br>
<br>

**Document Name:** AlternativeClaude.md

**Purpose:** Complete implementation guide for fixing every REAL issue discovered during manual
execution of `Sohba_Frontend_Test_Plan.md`.

**Author Role:** Senior Software Architect / Senior ASP.NET Core MVC Engineer / Senior .NET Backend
Engineer / Senior Frontend Engineer / Code Reviewer / QA Engineer.

**Scope:** No source file was modified while writing this document. This document is a guide only.

**Stack:** ASP.NET Core MVC · Clean Architecture (Domain / Application / Infrastructure / Presentation) ·
Repository Pattern · Dependency Injection · Entity Framework Core · JavaScript (Vanilla) · AJAX ·
SignalR · Identity.

<br>
<br>

---

<br>

# TABLE OF CONTENTS

1. [How To Use This Document](#how-to-use-this-document)
2. [Architecture Rules (Mandatory)](#architecture-rules-mandatory)
3. [Issue 3.1 — Create Post Modal Does Not Open](#issue-31--create-post-modal-does-not-open)
4. [Issue 3.2 — Posts Duplicate In Infinite Scroll & On Profile Pages](#issue-32--posts-duplicate-in-infinite-scroll--on-profile-pages)
5. [Issue 3.5 — No Delete Comment Button & Reply Button Broken](#issue-35--no-delete-comment-button--reply-button-broken)
6. [Issue 3.6 — Delete Post Button Broken](#issue-36--delete-post-button-broken)
7. [Issue 3.10 — Save Post / Add To Favorites Logic Redesign](#issue-310--save-post--add-to-favorites-logic-redesign)
8. [Console Error — SyntaxError At Home:771](#console-error--syntaxerror-at-home771)
9. [Console Error — POST Friends/GetFriendSuggestions 405](#console-error--post-friendsgetfriendsuggestions-405)
10. [Issue 4.2 — Story Viewer Never Opens](#issue-42--story-viewer-never-opens)
11. [Issue 5.1 — Groups Appear Duplicated](#issue-51--groups-appear-duplicated)
12. [Issue 5.2 — Edit Group / Leave Visible To Non-Members](#issue-52--edit-group--leave-visible-to-non-members)
13. [Issue 5.4 — Group Action Button Not Working](#issue-54--group-action-button-not-working)
14. [Issue 6.2 — Pages: No Images, No Preview, No Redirect](#issue-62--pages-no-images-no-preview-no-redirect)
15. [Issue 7.2 — Search Not Working At All](#issue-72--search-not-working-at-all)
16. [Issue 7.4 & 7.5 — Accept / Reject Friend Request Fails With 429](#issue-74--75--accept--reject-friend-request-fails-with-429)
17. [Issue 7.6 — Profile Page: checkFriendshipStatus & blockUserFromProfile Undefined](#issue-76--profile-page-checkfriendshipstatus--blockuserfromprofile-undefined)
18. [Additional Profile Request — Add-Friend Shows Even When Already Friends](#additional-profile-request--add-friend-shows-even-when-already-friends)
19. [Issue 8.1 — Profile Edit Page Missing](#issue-81--profile-edit-page-missing)
20. [Issue 8.2 — Settings Page: Save / Danger Zone Not Functional + UI](#issue-82--settings-page-save--danger-zone-not-functional--ui)
21. [Sidebar Duplication + _RightSidebar Suggestions Broken](#sidebar-duplication--rightsidebar-suggestions-broken)
22. [Dashboard — Make Everything Clickable](#dashboard--make-everything-clickable)
23. [Issue 9.1 — Dashboard Users: All Buttons Broken](#issue-91--dashboard-users-all-buttons-broken)
24. [Issue 9.2 — Dashboard Posts: All Buttons Broken](#issue-92--dashboard-posts-all-buttons-broken)
25. [Issue 9.3 — Dashboard Reports: All Buttons Broken](#issue-93--dashboard-reports-all-buttons-broken)
26. [Additional Issues Found](#additional-issues-found)
27. [Cross-Cutting Fix: The Real Cause Of `showConfirmModal is not a function`](#cross-cutting-fix-the-real-cause-of-showconfirmmodal-is-not-a-function)

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

# Issue 3.1 — Create Post Modal Does Not Open

## Issue

Clicking the "What's on your mind" input or any of the Create Post buttons does nothing.
The Console shows:

```
Home:592 Uncaught TypeError: SohbaApp.openCreatePostModal is not a function
    at HTMLInputElement.onclick (Home:592:38)
```

## Related Feature

- **Feature Name:** Post Creation — Create Post Modal.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.1 (Posts — Create Post flow).

## Expected Behaviour

Clicking the Create Post input / button opens the LinkedIn/Facebook-style modal
(`#createPostModal`) with the title, content textarea, image picker and Post button.

## Current Behaviour

Nothing happens. The browser throws:
`Uncaught TypeError: SohbaApp.openCreatePostModal is not a function`

## Root Cause

A **typo** in `Sohba/Views/Shared/Partials/_CreatePost.cshtml` at **line 165**:

```javascript
f (file.size > 5 * 1024 * 1024) {
```

The letter `i` is missing from `if`. This is a **SyntaxError**:

```
Uncaught SyntaxError: Unexpected token '{' (at Home:771:49)
```

Because this typo is inside the page-level `<script>` block that defines
`window.SohbaApp.openCreatePostModal`, `closeCreatePostModal`, `handleImageSelect`,
`removeSelectedImage` and `submitCreatePost` — the **entire script block fails to parse**.
None of those functions are ever registered on `window.SohbaApp`.

This also explains the second console error reported:

```
Uncaught SyntaxError: Unexpected token '{' (at Home:771:49)
```

It is the SAME bug (the browser reports the position of the first `{` that confused the parser).

## Execution Flow

```
User clicks input
    → HTML attribute: onclick="SohbaApp.openCreatePostModal()"
        → Browser looks up window.SohbaApp.openCreatePostModal
            → NOT FOUND (script block failed to parse due to SyntaxError)
        → TypeError is thrown
    → No modal opens
```

## Related Files

- `Sohba/Views/Shared/Partials/_CreatePost.cshtml`  ← **the buggy file**
- `Sohba/Views/Home/Index.cshtml` (renders `_CreatePost`)
- `Sohba/Views/Shared/_AppLayout.cshtml` (loads `sohba-core.js`, `sohba-posts.js`, `sohba-modal.js`)
- `Sohba/wwwroot/js/sohba-core.js` (defines `SohbaApp` namespace)
- `Sohba/Views/Profile/Index.cshtml` (also renders `_CreatePost`)

## Affected Components

- View — `_CreatePost.cshtml`
- JavaScript — inline `<script>` block inside `_CreatePost.cshtml`

## Files That Need Modification

1. `Sohba/Views/Shared/Partials/_CreatePost.cshtml`

## Implementation Plan

1. Open `Sohba/Views/Shared/Partials/_CreatePost.cshtml`.
2. Go to line 165.
3. Change `f (` to `if (`.
4. Verify the whole `<script>` block parses (Browser DevTools → no SyntaxError).
5. Verify all five functions are registered on `window.SohbaApp` and the modal opens.

## Code Changes

<div style="color:red"><b>REMOVE (old, buggy line):</b></div>

```javascript
                 f (file.size > 5 * 1024 * 1024) {
                     window.SohbaApp.toast('Image must be smaller than 5MB', 'error');
                     event.target.value = '';
                     return;
                }
```

<div style="color:green"><b>REPLACE WITH (corrected code):</b></div>

```javascript
                 if (file.size > 5 * 1024 * 1024) {
                     window.SohbaApp.toast('Image must be smaller than 5MB', 'error');
                     event.target.value = '';
                     return;
                }
```

## Regression Testing

- **Test Users:** any normal user (e.g. `mohammed@sohba.com` / `Mohammed123!`).
- **Navigation:** Login → Home Feed → click the Create Post input.
- **Expected Results:**
    - No console `SyntaxError`.
    - Modal opens with empty title / content / image preview.
    - Uploading an image > 5MB shows the toast and clears the file.
    - Clicking the image picker shows the preview; the remove button clears it.
    - Clicking "Post" with no title shows "Title is required".
    - Filling title + content + optional image and submitting redirects to Home feed.
- **Failure Conditions:**
    - Any remaining SyntaxError means the script still fails — re-open and fix.
- **Edge Cases:**
    - Test on Blog/Profile page (Profile/Index.cshtml also renders `_CreatePost`).
    - Test on a page where `_AppLayout` is the layout (all authenticated pages).

<br>
<br>

---

<br>

# Issue 3.2 — Posts Duplicate In Infinite Scroll & On Profile Pages

## Issue

- Posts keep repeating on the Home feed when using Infinite Scroll / Load More.
- Posts also repeat (infinite loop / duplicated posts) on the user's own profile page.

## Related Feature

- **Feature Name:** Home Feed — Infinite Scroll / Load More + Profile Feed.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.2 (Infinite scroll of posts).

## Expected Behaviour

- Home feed loads page 1, then page 2, page 3 ... with each post appearing **exactly once**.
- Profile page shows each of the user's posts **exactly once**.

## Current Behaviour

- Posts from later pages are appended but the same posts appear again later.
- On the profile page the same posts repeat endlessly.

## Root Cause

There are THREE root causes that combine:

### Root Cause A — The global post modal is duplicated with every partial render

`Sohba/Views/Shared/Partials/_PostCard.cshtml` contains the **entire post modal markup
(`#postModal`, `#modalComments`, ...) inside the partial file** (lines 363+).

- `Home/Index.cshtml` renders `_PostCard` once → includes the modal markup.
- When `HomeController.GetPostCards` returns the SAME partial as HTML for infinite scroll,
  the HTML returned **includes another full copy of `#postModal`**.
- The AJAX result is appended with `container.insertAdjacentHTML('beforeend', result.html)`.
- Now the DOM has **two elements with `id="postModal"`**, two with `id="modalComments"`, etc.
- `sohba-modal.js` operates on `document.getElementById('postModal')` — it only ever sees
  the first one. Injecting the same post cards again can therefore render stale/duplicated
  content, and duplicated IDs break JS event behaviour.

The same problem exists on `Profile/Index.cshtml`:
it loops `@foreach (var post in Model.Posts)` and renders `<partial name="Partials/_PostCard"
model="new[] { post }" />` **for every post**. Each partial render includes the full `#postModal`
markup. With N posts the DOM ends up with N copies of `#postModal`, N copies of
`#createPostModal`? — No, `_CreatePost` is rendered once, but the `_PostCard` modal is
duplicated N times. That is why "infinite loop and duplicated" is observed visually.

### Root Cause B — No debouncing in the infinite-scroll handler

`Sohba/wwwroot/js/features/feed.js`:

```javascript
window.addEventListener('scroll', function () {
    if (isLoading || !hasMore) return;
    ...
    if (scrollTop + clientHeight >= scrollHeight - 200) {
        loadMorePosts();
    }
});
```

- The scroll event can fire several times in one frame; `loadMorePosts` sets `isLoading = true`
  only *after* the synchronous part, and the fetch is async. In fast scrolling, the event can
  fire again before `isLoading` flips, causing duplicate concurrent requests for the same page.

### Root Cause C — Pagination is offset-based and shifts when data changes

`PostRepository.GetTimelineAsync` uses `Skip((page-1)*pageSize)` — offset-based pagination.
If a new post is created between page loads, the next `Skip` shifts by one and a post already
rendered on page 1 appears again on page 2. There is no server-side de-duplication and no
client-side de-duplication of already-rendered post IDs.

## Execution Flow

```
Home/Index.cshtml
    ├─ render _StoryRail
    ├─ render _CreatePost                           ← single copy of create modal
    └─ render _PostCard (Model.Posts)               ← includes #postModal markup (copy #1)

User scrolls to bottom
    → feed.js loadMorePosts()
        → GET /Home/GetPostCards?page=2
            → HomeController.GetPostCards
                → PostService.GetFeedAsync(page 2)      ← offset Skip(10)
                → render _PostCard(Partial)              ← includes #postModal markup AGAIN (copy #2 + posts)
        → insertAdjacentHTML beforeend                  ← duplicate #postModal in DOM
```

Profile duplication:

```
Profile/Index.cshtml
    └─ @foreach(post) → <partial _PostCard model="new[]{post}"/>
        ├─ post 1 → renders #postModal (copy #1) + post card 1
        ├─ post 2 → renders #postModal (copy #2) + post card 2
        └─ ... duplicate #postModal per post
```

## Related Files

- `Sohba/Views/Home/Index.cshtml`
- `Sohba/Views/Profile/Index.cshtml`
- `Sohba/Views/Shared/Partials/_PostCard.cshtml`
- `Sohba/wwwroot/js/features/feed.js`
- `Sohba/Controllers/HomeController.cs` (`GetPostCards`, `Index`, `LoadMore`)
- `Sohba.Application/Services/PostService.cs` (`GetFeedAsync`)
- `Sohba.Infrastructure/Repositories/PostRepository.cs` (`GetTimelineAsync`)

## Affected Components

- View — `Home/Index.cshtml`
- View — `Profile/Index.cshtml`
- Partial View — `_PostCard.cshtml`
- JavaScript — `features/feed.js`
- Controller — `HomeController.cs`
- Application Service — `PostService.cs`
- Repository — `PostRepository.cs`

## Files That Need Modification

1. `Sohba/Views/Shared/Partials/_PostCard.cshtml`  (move modal OUT of the partial)
2. `Sohba/Views/Shared/_AppLayout.cshtml`           (include the modal once)
3. `Sohba/Views/Home/Index.cshtml`                  (if needed, remove redundant modal depends)
4. `Sohba/Views/Profile/Index.cshtml`               (remove duplicate modal render)
5. `Sohba/wwwroot/js/features/feed.js`              (add debounce + client-side dedupe)

## Implementation Plan

1. **Extract the modal markup** from `_PostCard.cshtml`:
   - Cut the entire `<div id="postModal" ...> ... </div>` block (currently lines 363+ of
     `_PostCard.cshtml`) and paste it into a NEW shared partial
     `Sohba/Views/Shared/Partials/_PostModal.cshtml`.
   - Include the new partial **once** in `_AppLayout.cshtml`, right before
     `@await RenderSectionAsync("Scripts", required: false)` (with the other global modals).
2. **Remove the modal block** from `_PostCard.cshtml` so the partial is purely a post card.
3. **Profile page**: replace the `@foreach` + repeated partial with a single render:
   `<partial name="Partials/_PostCard" model="Model.Posts" />` (pass the whole list once).
   (Keep the `_CreatePost` partial as-is.)
4. **feed.js — debounce the scroll handler** and keep `isLoading` truly blocking:

   ```javascript
   let scrollTicking = false;
   window.addEventListener('scroll', function () {
       if (scrollTicking) return;
       scrollTicking = true;
       requestAnimationFrame(() => {
           scrollTicking = false;
           if (isLoading || !hasMore) return;
           const scrollHeight = document.documentElement.scrollHeight;
           const scrollTop = document.documentElement.scrollTop || document.body.scrollTop;
           const clientHeight = document.documentElement.clientHeight;
           if (scrollTop + clientHeight >= scrollHeight - 300) {
               loadMorePosts();
           }
       });
   });
   ```

5. **feed.js — client-side ID dedupe**: keep a `Set` of already-rendered post IDs, and skip
   post cards that already exist in the DOM:

   ```javascript
   const renderedPostIds = new Set();
   document.querySelectorAll('#postsContainer [data-post-id]').forEach(el => {
       renderedPostIds.add(el.dataset.postId);
   });
   ```

   Then inside `loadMorePosts`, after receiving `result.html`, create a temporary container,
   parse it, remove any card whose `data-post-id` is already in the Set, and only append the
   remaining cards. Add any new IDs to the Set.

6. **Pagination robustness (optional but recommended):** Add server-side de-duplication in
   `PostRepository.GetTimelineAsync` by using keyset pagination:
   `WHERE CreatedAt < @lastCreatedAt` instead of `Skip`. This is a bigger change; the
   client-side dedupe is the minimum fix.

## Code Changes

<div style="color:red"><b>REMOVE (from _PostCard.cshtml) — the ENTIRE modal block starting at line 363:</b></div>

```html
    <!-- Modern Post Modal (Instagram Style) -->
    <div id="postModal" class="fixed inset-0 z-50 hidden">
        <!-- Overlay -->
        <div class="absolute inset-0 bg-black/60" onclick="SohbaApp.closePostModal()"></div>
        ...
        <!-- rest of modal ... -->
    </div>
```

<div style="color:green"><b>ADD — create new file Sohba/Views/Shared/Partials/_PostModal.cshtml with the extracted modal markup (single copy).</b></div>

```html
<!-- Modern Post Modal (Instagram Style) — SINGLE INSTANCE, GLOBAL -->
<div id="postModal" class="fixed inset-0 z-50 hidden">
    <!-- Overlay -->
    <div class="absolute inset-0 bg-black/60" onclick="SohbaApp.closePostModal()"></div>
    <!-- ... paste the full modal markup exactly once ... -->
</div>
```

<div style="color:green"><b>ADD — in _AppLayout.cshtml, include the global modal once (next to the Confirm Modal):</b></div>

```html
    <!-- Confirm Modal -->
    <partial name="Partials/_ConfirmModal" />

    <!-- Post Modal (single global instance) -->
    <partial name="Partials/_PostModal" />
```

<div style="color:red"><b>REMOVE (from Profile/Index.cshtml) — the duplicate nested partial loop:</b></div>

```html
            @foreach (var post in Model.Posts)
            {
                <partial name="Partials/_PostCard" model="new[] { post }" />
            }
```

<div style="color:green"><b>REPLACE WITH — render the list once:</b></div>

```html
            <partial name="Partials/_PostCard" model="Model.Posts" />
```

<div style="color:red"><b>REMOVE (from feed.js) — the raw scroll listener:</b></div>

```javascript
function setupInfiniteScroll() {
    // Detect when user scrolls near bottom
    window.addEventListener('scroll', function () {
        if (isLoading || !hasMore) return;

        const scrollHeight = document.documentElement.scrollHeight;
        const scrollTop = document.documentElement.scrollTop || document.body.scrollTop;
        const clientHeight = document.documentElement.clientHeight;

        // Load more when user is 200px from bottom
        if (scrollTop + clientHeight >= scrollHeight - 200) {
            loadMorePosts();
        }
    });
}
```

<div style="color:green"><b>REPLACE WITH — debounced + deduped version:</b></div>

```javascript
const renderedPostIds = new Set();

function collectRenderedPostIds() {
    document.querySelectorAll('#postsContainer [data-post-id]').forEach(el => {
        renderedPostIds.add(el.dataset.postId);
    });
}

function setupInfiniteScroll() {
    let scrollTicking = false;

    window.addEventListener('scroll', function () {
        if (scrollTicking) return;
        scrollTicking = true;

        requestAnimationFrame(() => {
            scrollTicking = false;
            if (isLoading || !hasMore) return;

            const scrollHeight = document.documentElement.scrollHeight;
            const scrollTop = document.documentElement.scrollTop || document.body.scrollTop;
            const clientHeight = document.documentElement.clientHeight;

            if (scrollTop + clientHeight >= scrollHeight - 300) {
                loadMorePosts();
            }
        });
    });
}
```

<div style="color:green"><b>REPLACE IN feed.js loadMorePosts — dedupe before appending:</b></div>

```javascript
        if (result.success) {
            const container = document.getElementById('postsContainer');
            if (container && result.html) {
                const temp = document.createElement('div');
                temp.innerHTML = result.html;

                const uniqueCards = Array.from(temp.querySelectorAll('[data-post-id]')).filter(card => {
                    const id = card.dataset.postId;
                    if (!id || renderedPostIds.has(id)) return false;
                    renderedPostIds.add(id);
                    return true;
                });

                if (uniqueCards.length > 0) {
                    uniqueCards.forEach(card => container.appendChild(card));
                }
            }
            currentPage = result.currentPage ?? nextPage;
            hasMore = result.hasMore ?? false;

            if (!hasMore) {
                hideLoadMoreButton();
            }
        }
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com` (has many posts + friends with posts).
- **Required Data:** Multiple existing posts (> 12) from at least 3 users so pagination is real.
- **Navigation (Home):** Login → Home → scroll to bottom repeatedly.
- **Expected Results:**
    - Posts appear once; no duplicate `id="postModal"` in DevTools.
    - `Load More` / infinite scroll appends only new posts.
    - Only one `#postModal` element exists in the DOM at any time.
- **Navigation (Profile):** Login as Mohammed → go to own profile.
- **Expected Results:** Each post card appears exactly once; there is exactly one `#postModal`.
- **Failure Conditions:** If duplicates still occur, check that `GetTimelineAsync` pagination
  is stable (consider keyset pagination).
- **Edge Cases:**
    - Very fast scrolling (double-fire removed by debounce).
    - A new post created while scrolling (offset shift) — client-side dedupe must mask it.
    - Posts with images vs without — the modal must correctly switch layout.

<br>
<br>

---

<br>

# Issue 3.5 — No Delete Comment Button & Reply Button Broken

## Issue

- There is **no Delete Comment button** anywhere.
- The **Reply** button throws:

```
22222222-2222-2222-2222-222222222222:1 Uncaught TypeError: SohbaApp.showReplyForm is not a function
    at HTMLButtonElement.onclick (22222222-...:1:10)
```

## Related Feature

- **Feature Name:** Post Details / Comments — Reply to Comment + Delete Comment.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.5 (Comments — Reply & Delete).

## Expected Behaviour

- Clicking "Reply" under a comment shows an inline reply input.
- Submitting the reply adds the reply under the parent comment.
- If the current user authored the comment (or is Admin / post owner), a Delete button is
  visible and deleting it removes the comment.

## Current Behaviour

- Reply button → `ReferenceError/TypeError`: `SohbaApp.showReplyForm is not a function`.
- No delete button is rendered at all.

## Root Cause

There are THREE distinct root causes:

### Root Cause A — Namespace mismatch for `showReplyForm`

- `Sohba/wwwroot/js/sohba-posts.js` defines:

```javascript
window.showReplyForm = function (commentId, userName) { ... };
window.hideReplyForm = function (commentId) { ... };
window.submitReply = async function (commentId, postId) { ... };
```

- But the HTML generated in `sohba-modal.js` and `sohba-posts.js` calls:

```javascript
onclick="SohbaApp.showReplyForm('...', '...')"
onclick="SohbaApp.submitReply('...', '...')"
onclick="SohbaApp.hideReplyForm('...')"
```

`SohbaApp.showReplyForm` is never defined — only `window.showReplyForm` is. The functions are
called with the wrong namespace.

### Root Cause B — `features/comments.js` (the delete-comment handler) is never loaded

`Sohba/wwwroot/js/features/comments.js` defines `deleteComment(commentId, postId)`.
It is **not referenced anywhere**: `_AppLayout.cshtml` loads
`sohba-core.js`, `sohba-posts.js`, `sohba-modal.js`, `sohba-stories.js`, `features/stories.js`,
`features/groups.js` — but NOT `features/comments.js`, `features/modal.js`, `features/friends.js`,
`features/dashboard.js`, `features/header.js` (header is loaded in `_Header.cshtml`),
`features/sidebar.js` (loaded in Home), or `features/search.js` (loaded in Search view).

### Root Cause C — The comments JSON from the server does not include replies or an `isAuthor` flag

`PostsController.GetPostDetails` projects comments like this:

```csharp
comments = comments.Select(c => new
{
    id = c.Id,
    content = c.Content,
    userName = c.UserName,
    createdAt = c.CreatedAt
})
```

- `Replies` are NOT included, so replies never display in the modal even though the JS renders
  a "View N replies" button.
- No `isAuthor` flag is included, so the UI cannot decide whether to render a Delete button.

## Execution Flow

```
User clicks "Reply"
    → onclick="SohbaApp.showReplyForm(commentId, userName)"
        → window.SohbaApp.showReplyForm -> undefined  → TypeError

Delete Comment button
    → does not exist in the rendered HTML
        → because GetPostDetails JSON has no isAuthor flag
        → and features/comments.js (deleteComment) is not loaded
```

## Related Files

- `Sohba/Views/Shared/Partials/_PostCard.cshtml`
- `Sohba/wwwroot/js/sohba-posts.js`
- `Sohba/wwwroot/js/sohba-modal.js`
- `Sohba/wwwroot/js/features/comments.js`
- `Sohba/Views/Shared/_AppLayout.cshtml`
- `Sohba/Controllers/PostsController.cs` (`GetPostDetails`)
- `Sohba.Application/Services/InteractionService.cs` (`GetCommentsByPostIdAsync`)
- `Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs` (check/confirm `IsAuthor` field)
- `Sohba/Controllers/CommentsController.cs` (`Delete`)

## Affected Components

- JavaScript — `sohba-posts.js`
- JavaScript — `sohba-modal.js`
- JavaScript — `features/comments.js` (unloaded)
- View — `_AppLayout.cshtml` (script loading)
- Controller — `PostsController.cs` (GetPostDetails projection)
- Application Service — `InteractionService.cs` (comment DTO mapping)
- DTO — `CommentResponseDto`

## Files That Need Modification

1. `Sohba/wwwroot/js/sohba-posts.js`
2. `Sohba/wwwroot/js/sohba-modal.js`
3. `Sohba/Views/Shared/_AppLayout.cshtml`
4. `Sohba/Controllers/PostsController.cs`
5. `Sohba.Application/Services/InteractionService.cs`
6. `Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs` (add `IsAuthor` if missing)

## Implementation Plan

1. **Add the functions to the `SohbaApp` namespace** in `sohba-posts.js` so all callers work:

   ```javascript
   window.SohbaApp.showReplyForm = window.showReplyForm;
   window.SohbaApp.hideReplyForm  = window.hideReplyForm;
   window.SohbaApp.submitReply    = window.submitReply;
   window.SohbaApp.toggleReplies  = window.toggleReplies;
   ```

2. **Register the remaining comment helpers in `sohba-modal.js`** so the modal-generated HTML
   can call them reliably:
   - `SohbaApp.toggleComment` already exists (in `sohba-posts.js`).
   - Make sure `window.SohbaApp.showReplyForm` / `hideReplyForm` / `submitReply` / `toggleReplies`
     all resolve (see step 1).

3. **Load `features/comments.js` globally** in `_AppLayout.cshtml`.

4. **Fix the server-side projection** in `PostsController.GetPostDetails`: stop projecting
   anonymous types; return the full `CommentResponseDto` list (which already includes `Replies`,
   `ReplyCount`, `ParentCommentId`) AND include an `IsAuthor` field.

5. **Add `IsAuthor` to `CommentResponseDto`** (if missing) and populate it in
   `InteractionService.GetCommentsByPostIdAsync`:

   ```csharp
   // inside GetCommentsByPostIdAsync, after building the tree:
   foreach (var comment in result)
   {
       comment.IsAuthor = comment.UserId == currentUserId; // requires adding currentUserId param
       foreach (var reply in comment.Replies)
       {
           reply.IsAuthor = reply.UserId == currentUserId;
       }
   }
   ```

   `GetCommentsByPostIdAsync` currently takes only `postId` — change the signature to
   `GetCommentsByPostIdAsync(Guid postId, Guid currentUserId)` and update the caller in
   `PostsController.GetPostDetails`.

6. **Render the Delete button conditionally** in `sohba-modal.js` comment template:

   ```javascript
   ${c.isAuthor ? `
       <button onclick="SohbaApp.deleteComment('${c.id}', '${c.postId}')"
               class="text-xs text-red-500 hover:underline font-medium ml-2">
           Delete
       </button>` : ''}
   ```

   And expose `SohbaApp.deleteComment = deleteComment` in `features/comments.js`.

7. **Fix `features/comments.js`** to use the correct DOM selector for the count
   (`comments-count-{postId}` exists in `_PostCard`; the code currently looks for
   `comment-count-{postId}` — see Issue Additional Notes).

## Code Changes

<div style="color:green"><b>ADD — at the end of Sohba/wwwroot/js/sohba-posts.js (after window.submitReply definition):</b></div>

```javascript
// ---- Namespace aliases: HTML attributes call SohbaApp.* ----
window.SohbaApp.showReplyForm = window.showReplyForm;
window.SohbaApp.hideReplyForm  = window.hideReplyForm;
window.SohbaApp.submitReply    = window.submitReply;
window.SohbaApp.toggleReplies  = window.toggleReplies;
window.SohbaApp.deleteComment  = window.deleteComment;
```

<div style="color:green"><b>ADD — in Sohba/Views/Shared/_AppLayout.cshtml, load comments.js next to the other feature scripts:</b></div>

```html
    <script src="~/js/features/stories.js" asp-append-version="true"></script>
    <script src="~/js/features/groups.js" asp-append-version="true"></script>
    <script src="~/js/features/comments.js" asp-append-version="true"></script>
    <script src="~/js/features/modal.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
```

<div style="color:red"><b>REMOVE — from PostsController.GetPostDetails — the anonymous projection:</b></div>

```csharp
            var comments = await _interactionService.GetCommentsByPostIdAsync(postId);

            return Json(new
            {
                success = true,
                post = new
                {
                    id = postResult.Value.Id,
                    title = postResult.Value.Title,
                    content = postResult.Value.Content,
                    imageUrl = postResult.Value.ImageUrl,
                    authorName = postResult.Value.AuthorName,
                    createdAt = postResult.Value.CreatedAt,
                    commentsCount = postResult.Value.CommentsCount,
                    reactionsCount = postResult.Value.ReactionsCount,
                    currentUserReaction = postResult.Value.CurrentUserReaction,
                    isSaved = postResult.Value.IsSaved,
                    isFavorite = postResult.Value.IsFavorite
                },
                comments = comments.Select(c => new
                {
                    id = c.Id,
                    content = c.Content,
                    userName = c.UserName,
                    createdAt = c.CreatedAt
                })
            });
```

<div style="color:green"><b>REPLACE WITH — full DTO + isAuthor:</b></div>

```csharp
            var comments = await _interactionService.GetCommentsByPostIdAsync(postId, userId);

            return Json(new
            {
                success = true,
                post = new
                {
                    id = postResult.Value.Id,
                    title = postResult.Value.Title,
                    content = postResult.Value.Content,
                    imageUrl = postResult.Value.ImageUrl,
                    authorName = postResult.Value.AuthorName,
                    createdAt = postResult.Value.CreatedAt,
                    commentsCount = postResult.Value.CommentsCount,
                    reactionsCount = postResult.Value.ReactionsCount,
                    currentUserReaction = postResult.Value.CurrentUserReaction,
                    isSaved = postResult.Value.IsSaved,
                    isFavorite = postResult.Value.IsFavorite
                },
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
            });
```

<div style="color:red"><b>REMOVE — in InteractionService.GetCommentsByPostIdAsync — the signature and body without currentUserId:</b></div>

```csharp
        public async Task<IEnumerable<CommentResponseDto>> GetCommentsByPostIdAsync(Guid postId)
```

<div style="color:green"><b>REPLACE WITH — signature with currentUserId and IsAuthor population:</b></div>

```csharp
        public async Task<IEnumerable<CommentResponseDto>> GetCommentsByPostIdAsync(Guid postId, Guid currentUserId)
```

Then inside the method, after building `result`, add:

```csharp
            foreach (var comment in result)
            {
                comment.IsAuthor = comment.UserId == currentUserId;
                foreach (var reply in comment.Replies)
                {
                    reply.IsAuthor = reply.UserId == currentUserId;
                }
            }
```

<div style="color:green"><b>ADD — in Sohba/wwwroot/js/sohba-modal.js — Delete button in comment + reply templates:</b></div>

```javascript
                            <div class="flex items-center gap-3 mt-1">
                                <span class="text-xs text-gray-400">${new Date(c.createdAt).toLocaleString()}</span>

                                <button onclick="SohbaApp.showReplyForm('${c.id}', '${c.userName}')"
                                        class="text-xs text-[#345e69] hover:underline font-medium">
                                    Reply
                                </button>

                                ${c.isAuthor ? `
                                    <button onclick="SohbaApp.deleteComment('${c.id}', '${c.postId}')"
                                            class="text-xs text-red-500 hover:underline font-medium ml-2">
                                        Delete
                                    </button>
                                ` : ''}
                                ...
```

And for replies (inside `c.replies.map(reply => ...)`) add the same delete button:

```javascript
                                ${reply.isAuthor ? `
                                    <button onclick="SohbaApp.deleteComment('${reply.id}', '${reply.postId}')"
                                            class="text-xs text-red-500 hover:underline font-medium ml-2">
                                        Delete
                                    </button>
                                ` : ''}
```

<div style="color:red"><b>REMOVE — in features/comments.js — the wrong count element id (line ~38):</b></div>

```javascript
                        const countEl = document.getElementById(`comment-count-${postId}`);
```

<div style="color:green"><b>REPLACE WITH — the actual element id used in _PostCard.cshtml:</b></div>

```javascript
                        const countEl = document.getElementById(`comments-count-${postId}`);
```

## Regression Testing

- **Test Users:**
    - `mohammed@sohba.com` (post author, has a comment from Ahmed on "Welcome to Sohba").
    - `ahmed@sohba.com` (comment author).
    - `admin@sohba.com` (Admin).
- **Navigation:**
    - Login as Mohammed → Home feed → open a post that Ahmed commented on.
- **Expected Results:**
    - Ahmed's comment shows a Reply button; Mohammed's reply shows a Reply button.
    - Clicking Reply shows the inline form; submitting appends the reply under the parent.
    - The original commenter (Ahmed) sees a Delete button on his own comment; the post author
      (Mohammed) should also be able to delete (per domain rule).
    - Deleting removes the comment from the modal and decrements the comments count.
- **Failure Conditions:**
    - Reply click still throws — the alias is not registered; check script load order.
    - Deleting shows "showConfirmModal is not a function" — apply the Cross-Cutting Fix
      (load `features/modal.js`).
- **Edge Cases:**
    - Replies on replies (nested).
    - Deleting a parent comment that has replies.
    - User + post author + admin permission matrix.

<br>
<br>

---

<br>

# Issue 3.6 — Delete Post Button Broken

## Issue

The Delete Post button inside the post menu throws:

```
sohba-posts.js:336 Uncaught TypeError: window.showConfirmModal is not a function
    at window.SohbaApp.deletePost (sohba-posts.js:336:12)
    at HTMLButtonElement.onclick (22222222-...:915:216)
```

## Related Feature

- **Feature Name:** Post Actions — Delete Post.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.6 (Delete post flow).

## Expected Behaviour

Clicking Delete Post shows a confirmation modal. Confirming deletes the post and removes the
card from the feed.

## Current Behaviour

Nothing happens — `window.showConfirmModal is not a function`.

## Root Cause

`window.showConfirmModal` is defined in TWO places, and BOTH are effectively missing:

1. `Sohba/Views/Shared/Partials/_ConfirmModal.cshtml` — the `<script>` block defining
   `showConfirmModal` / `closeConfirmModal` is **fully commented out** (lines 40-101).
2. `Sohba/wwwroot/js/features/modal.js` — a proper IIFE defining
   `window.showConfirmModal` / `window.closeConfirmModal` EXISTS, but the file is
   **never loaded in `_AppLayout.cshtml`**.

This single root cause also breaks:
- Delete Post (`sohba-posts.js`)
- Dashboard Users / Posts / Reports 9.1 / 9.2 / 9.3
- Cancel friend request (`friends.js`)
- Block user (`friends.js`)
- Leave Group (`Groups/Details.cshtml`)
- Delete comment (`features/comments.js`)

## Execution Flow

```
Click Delete Post
    → SohbaApp.deletePost(postId)
        → window.showConfirmModal({...})  → undefined function → TypeError
```

## Related Files

- `Sohba/Views/Shared/Partials/_ConfirmModal.cshtml`
- `Sohba/wwwroot/js/features/modal.js`
- `Sohba/Views/Shared/_AppLayout.cshtml`
- `Sohba/wwwroot/js/sohba-posts.js`
- `Sohba/wwwroot/js/features/dashboard.js`
- `Sohba/wwwroot/js/features/friends.js`
- `Sohba/wwwroot/js/features/comments.js`
- `Sohba/Views/Groups/Details.cshtml`
- `Sohba/Views/Dashboard/Users.cshtml`, `Posts.cshtml`, `Reports.cshtml`

## Affected Components

- JavaScript — global `window.showConfirmModal`
- View — `_AppLayout.cshtml` (script loading)
- Partial View — `_ConfirmModal.cshtml`

## Files That Need Modification

1. `Sohba/Views/Shared/_AppLayout.cshtml` (load `features/modal.js`)

## Implementation Plan

1. Load `Sohba/wwwroot/js/features/modal.js` in `_AppLayout.cshtml`, after
   `sohba-post.js` and before the `@section Scripts` render.
2. Keep the commented-out `<script>` in `_ConfirmModal.cshtml` commented (it would duplicate
   `window.showConfirmModal` if both were active).
3. Verify `window.showConfirmModal` and `window.closeConfirmModal` are now defined globally.

## Code Changes

<div style="color:green"><b>ADD — in Sohba/Views/Shared/_AppLayout.cshtml (script section):</b></div>

```html
    <script src="~/js/features/groups.js" asp-append-version="true"></script>
    <script src="~/js/features/modal.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com` (post author), `admin@sohba.com` (Admin).
- **Navigation:** Login → Home → open a post's ⋮ menu → Delete Post.
- **Expected Results:**
    - Confirmation modal appears.
    - Cancel closes without change.
    - Confirm deletes the post, shows a success toast, and removes the card.
- **Failure Conditions:** `showConfirmModal is not a function` must NEVER appear again on
  any page (Home, Profile, Groups, Dashboard, Friends).
- **Edge Cases:** Deleting a post from the Profile page and from Group/Page feeds.

<br>
<br>

---

<br>

# Issue 3.10 — Save Post / Add To Favorites Logic Redesign

## Issue

The current "Save Post" and "Add To Favorites" behave like a simple toggle (works),
but the user wants a redesign:

> "Let Save Post be a general thing where you save posts, but Add To Favorites saves them into
> playlists/categories. There must be a default or adding with a custom name. Every user has his
> own private list of categories (never shared), with a default tag for all."

## Related Feature

- **Feature Name:** Post Actions — Save Post / Favorites.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.10 (Save & Favorites).

## Expected Behaviour (Desired)

1. **Save Post** — a general "saved posts" collection. Clicking Save opens a modal to:
   - Pick an existing personal category/collection, OR
   - Create a new named category, OR
   - Save to the default "Saved" collection.
2. **Add To Favorites** — a fixed, special collection (the favourites) that holds
   favourite posts.
3. Every user has their own **private** set of categories — never shared.
4. `SavedPosts` page groups saved posts by category; `Favorites` page shows the
   default favourites collection.

## Current Behaviour

- `ToggleSavePost` creates a single `SavedPost` row with a `Tag` enum
  (`General` or `Favorite`).
- Clicking Save with `isFavorite=false` sets `Tag=General`.
- Clicking Add To Favorites with `isFavorite=true` sets `Tag=Favorite`.
- Clicking the SAME button again with a different flag removes the row entirely.
- Toggling Save on a favorited post removes the favourite as well (because the row is shared).
- No category / playlist system exists.

## Root Cause

The data model `SavedPost` only has an enum `Tag`. There is no
`SavedCollection`/`SavedCategory` entity:
- `Sohba.Domain/Entities/PostAggregate/SavedPost.cs`
- `Sohba.Application/Services/InteractionService.cs` (`SavePostAsync`, `GetSavedPostsByUserAsync`)
- `Sohba/Controllers/PostsController.cs` (`ToggleSavePost`, `SavedPosts`, `Favorites`)
- `Sohba/Views/Posts/SavedPosts.cshtml`, `Sohba/Views/Posts/Favorites.cshtml`

The system cannot express "categories" or "custom tags".

## Execution Flow

```
Click Save Post
    → sohba-posts.js savePost(postId)
        → POST /Posts/ToggleSavePost { postId, isFavorite:false }
            → PostsController.ToggleSavePost
                → existingSave? remove : save with Tag.General
    → UI shows "Saved" or removes it
```

## Related Files

- `Sohba.Domain/Entities/PostAggregate/SavedPost.cs`
- `Sohba.Application/Services/InteractionService.cs`
- `Sohba.Application/Interfaces/IInteractionService.cs`
- `Sohba/Controllers/PostsController.cs`
- `Sohba/wwwroot/js/sohba-posts.js`
- `Sohba/Views/Posts/SavedPosts.cshtml`
- `Sohba/Views/Posts/Favorites.cshtml`
- `Sohba/Views/Shared/Partials/_PostCard.cshtml`
- `Sohba.Infrastructure/Migrations/*` (new migration required)

## Affected Components

- Domain Entity — `SavedPost`
- New Domain Entity — `SavedCollection` (to be added)
- Application Service — `InteractionService`
- Controller — `PostsController`
- JavaScript — `sohba-posts.js`
- Views — `SavedPosts.cshtml`, `Favorites.cshtml`, `_PostCard.cshtml`
- EF Core Migration

## Files That Need Modification

1. `Sohba.Domain/Entities/PostAggregate/SavedCollection.cs` (NEW)
2. `Sohba.Domain/Entities/PostAggregate/SavedPost.cs`
3. `Sohba.Application/Interfaces/IInteractionService.cs`
4. `Sohba.Application/Services/InteractionService.cs`
5. `Sohba/Controllers/PostsController.cs`
6. `Sohba/wwwroot/js/sohba-posts.js`
7. `Sohba/Views/Posts/SavedPosts.cshtml`
8. `Sohba/Views/Posts/Favorites.cshtml`
9. `Sohba/Views/Shared/Partials/_PostCard.cshtml`
10. New EF Migration

## Implementation Plan

1. **Create a `SavedCollection` entity:**

   ```csharp
   public class SavedCollection
   {
       public Guid Id { get; set; }
       public Guid UserId { get; set; }
       public string Name { get; set; }
       public bool IsDefault { get; set; }          // true for "Saved" and "Favorites"
       public bool IsFavorites { get; set; }         // true for the special Favorites collection
       public DateTime CreatedAt { get; set; }
       public User User { get; set; }
       public ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
   }
   ```

2. **Modify `SavedPost`:**

   ```csharp
   public class SavedPost
   {
       public Guid Id { get; set; }
       public Guid UserId { get; set; }
       public Guid PostId { get; set; }
       public Guid? CollectionId { get; set; }        // null = legacy/default
       public DateTime SavedAt { get; set; }
       // keep Tag for backwards compatibility, or migrate; recommended: remove later
       public SavedTag? Tag { get; set; }
       public User User { get; set; }
       public Post Post { get; set; }
       public SavedCollection Collection { get; set; }
   }
   ```

3. **Add a migration** for the new FK + table. Seed two default collections per user on
   first save (or lazily create them):
   - "Saved" (`IsDefault = true`)
   - "Favorites" (`IsFavorites = true`)

4. **Extend `IInteractionService`:**

   ```csharp
   Task<Result<IEnumerable<SavedCollectionDto>>> GetUserCollectionsAsync(Guid userId);
   Task<Result<SavedCollectionDto>> CreateCollectionAsync(Guid userId, string name);
   Task<Result> SavePostToCollectionAsync(Guid userId, Guid postId, Guid collectionId);
   Task<Result> SavePostToFavoritesAsync(Guid userId, Guid postId);
   Task<Result> RemoveSavedPostAsync(Guid userId, Guid postId);
   ```

5. **Update `PostsController`:**
   - `ToggleSavePost` → split into:
     - `POST /Posts/SaveToCollection` `{ postId, collectionId }`
     - `POST /Posts/ToggleFavorite` `{ postId }`
     - `GET /Posts/GetUserCollections`
     - `POST /Posts/CreateCollection` `{ name }`
   - Keep backward-compatible wrappers if desired.

6. **Update the JS (`sohba-posts.js`):**
   - `savePost(postId)` → opens a **Save Modal** that fetches
     `GET /Posts/GetUserCollections`, lists the user's collections, and offers
     "Create new collection...".
   - `addToFavorites(postId)` → `POST /Posts/ToggleFavorite`.
   - After save, update the button text to "Saved ✓".

7. **Update `SavedPosts.cshtml`** to list collections as groups (tabs/sections) and
   `Favorites.cshtml` to query the Favorites collection only.

8. **Update `_PostCard.cshtml`** Save button: call `SohbaApp.openSavePostModal('@post.Id')`
   instead of `SohbaApp.savePost(...)`.

## Code Changes (Highlights)

<div style="color:green"><b>ADD — new Domain entity SavedCollection.cs:</b></div>

```csharp
public class SavedCollection
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public bool IsDefault { get; set; }
    public bool IsFavorites { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; }
    public ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
}
```

<div style="color:green"><b>ADD — new controller actions in PostsController (logic only, keep return shape):</b></div>

```csharp
        [HttpGet]
        public async Task<IActionResult> GetUserCollections()
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.GetUserCollectionsAsync(userId);
            return Json(BaseResponseDto<IEnumerable<SavedCollectionDto>>.SuccessResponse(result.Value));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCollection([FromBody] CreateSavedCollectionDto request)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(request.Name))
                return Json(BaseResponseDto.FailureResponse("Collection name is required."));

            var result = await _interactionService.CreateCollectionAsync(userId, request.Name.Trim());
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> SaveToCollection([FromBody] SaveToCollectionDto request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.PostId == Guid.Empty || request.CollectionId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request."));

            var result = await _interactionService.SavePostToCollectionAsync(userId, request.PostId, request.CollectionId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite([FromBody] SaveToCollectionDto request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.PostId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request."));

            var result = await _interactionService.SavePostToFavoritesAsync(userId, request.PostId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }
```

<div style="color:red"><b>REMOVE (keep as backward-compat or delete) — the old single-toggle block in sohba-posts.js:</b></div>

```javascript
window.SohbaApp.savePost = async function (postId) {
    try {
        const result = await window.SohbaApp.post('/Posts/ToggleSavePost', {
            postId: postId,
            isFavorite: false
        });
        ...
    }
};
```

<div style="color:green"><b>ADD — modal-based save flow in sohba-posts.js:</b></div>

```javascript
window.SohbaApp.openSavePostModal = async function (postId) {
    const modal = document.getElementById('savePostModal');
    if (!modal) return;

    modal.dataset.postId = postId;
    const listEl = document.getElementById('saveCollectionsList');
    const nameInput = document.getElementById('newCollectionName');
    listEl.innerHTML = '<div class="text-sm text-gray-400 text-center py-4">Loading...</div>';
    nameInput.value = '';

    const result = await window.SohbaApp.get('/Posts/GetUserCollections');
    const collections = result.data ?? [];

    if (collections.length === 0) {
        listEl.innerHTML = '<div class="text-sm text-gray-400 text-center py-4">No collections yet. Create one below.</div>';
    } else {
        listEl.innerHTML = collections.map(c => `
            <button onclick="SohbaApp.saveToCollection('${postId}', '${c.id}')"
                    class="w-full text-left px-4 py-2.5 rounded-xl hover:bg-slate-50 text-sm font-semibold text-gray-700">
                ${c.name}
            </button>
        `).join('');
    }

    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';
};

window.SohbaApp.saveToCollection = async function (postId, collectionId) {
    const result = await window.SohbaApp.post('/Posts/SaveToCollection', { postId, collectionId });
    if (result.success) {
        window.SohbaApp.toast('Post saved to collection!', 'success');
        window.SohbaApp.closeSavePostModal();
        updateSaveFavoriteButtons(postId, true, false);
    } else {
        window.SohbaApp.toast(result.error || 'Failed to save post', 'error');
    }
};

window.SohbaApp.createNewCollection = async function () {
    const name = document.getElementById('newCollectionName')?.value.trim();
    const postId = document.getElementById('savePostModal')?.dataset.postId;
    if (!name) { window.SohbaApp.toast('Please enter a collection name', 'error'); return; }

    const createResult = await window.SohbaApp.post('/Posts/CreateCollection', { name });
    if (!createResult.success) {
        window.SohbaApp.toast(createResult.error || 'Failed to create collection', 'error');
        return;
    }

    const collectionId = createResult.data?.id;
    if (postId && collectionId) {
        await window.SohbaApp.saveToCollection(postId, collectionId);
    } else {
        window.SohbaApp.closeSavePostModal();
        window.SohbaApp.toast('Collection created!', 'success');
    }
};

window.SohbaApp.closeSavePostModal = function () {
    const modal = document.getElementById('savePostModal');
    if (modal) modal.classList.add('hidden');
    document.body.style.overflow = '';
};
```

Note: `SohbaApp.get` also needs to be added to `sohba-core.js`:

```javascript
window.SohbaApp.get = async function (url) {
    try {
        const response = await fetch(url);
        const contentType = response.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            return { success: false, error: `Server error (HTTP ${response.status}).` };
        }
        return await response.json();
    } catch (error) {
        console.error('[SohbaApp.get] Network error:', error);
        return { success: false, error: 'Network error.' };
    }
};
```

<div style="color:green"><b>ADD — the Save Post modal markup into a shared partial (e.g. Sohba/Views/Shared/Partials/_SavePostModal.cshtml, included once in _AppLayout):</b></div>

```html
<div id="savePostModal" class="fixed inset-0 z-[100] hidden">
    <div class="absolute inset-0 bg-black/60" onclick="SohbaApp.closeSavePostModal()"></div>
    <div class="absolute inset-0 flex items-center justify-center p-4">
        <div class="bg-white w-full max-w-sm rounded-2xl shadow-2xl overflow-hidden">
            <div class="flex items-center justify-between p-4 border-b">
                <h3 class="text-lg font-bold text-gray-900">Save post to...</h3>
                <button onclick="SohbaApp.closeSavePostModal()" class="p-1 text-gray-400 hover:text-gray-600">
                    <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>
            <div class="p-4 space-y-2" id="saveCollectionsList"></div>
            <div class="p-4 border-t border-slate-100 flex gap-2">
                <input id="newCollectionName" type="text" placeholder="New collection name..."
                       class="flex-1 px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#345e69]/20" />
                <button onclick="SohbaApp.createNewCollection()"
                        class="px-4 py-2 bg-[#345e69] text-white text-sm font-semibold rounded-xl hover:bg-[#2a4b55]">
                    Create
                </button>
            </div>
        </div>
    </div>
</div>
```

## Regression Testing

- **Test Users:** `mohammed@sohba.com`, `ahmed@sohba.com` (to verify per-user isolation).
- **Navigation:** Home → post ⋮ menu → Save Post.
- **Expected Results:**
    - Save modal appears with existing collections + create-new field.
    - Saving to a named collection → toast + button state "Saved".
    - Add To Favorites → adds the post to the Favorites default collection.
    - SavedPosts page shows collections/groups; Favorites page shows favourites only.
    - Mohammed's collections are NEVER visible to Ahmed.
- **Failure Conditions:** saving to the same post twice into the same collection must
  not create duplicates (add an `AnyAsync` guard).
- **Edge Cases:** empty collection name, post already saved to that collection, deleting
  a collection cascades its SavedPost rows.

<br>
<br>

---

<br>

# Console Error — SyntaxError At Home:771

## Issue

```
Uncaught SyntaxError: Unexpected token '{' (at Home:771:49)
```

## Related Feature

- **Feature Name:** Home Feed — Create Post Modal.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 3.1.

## Expected Behaviour

No syntax errors in the browser console.

## Current Behaviour

Every page that renders `_CreatePost.cshtml` throws a SyntaxError at parse time.

## Root Cause

This is THE SAME bug as Issue 3.1. The typo `f (file.size > ...)` in
`Sohba/Views/Shared/Partials/_CreatePost.cshtml` line 165 breaks the whole `<script>` block.
The browser reports `Home:771` because Home's rendered HTML places the partial script there.

## Execution Flow

```
Browser parses <script> block inside _CreatePost.cshtml
    → encounters "f (" where an "if (" is expected
        → parser sees '{' after a bare identifier → SyntaxError
    → entire script block discarded → openCreatePostModal etc. never defined
```

## Related Files

- `Sohba/Views/Shared/Partials/_CreatePost.cshtml`

## Affected Components

- JavaScript (inline)

## Files That Need Modification

1. `Sohba/Views/Shared/Partials/_CreatePost.cshtml`

## Implementation Plan

Same fix as Issue 3.1 — change `f (` to `if (`.

## Code Changes

<div style="color:red"><b>REMOVE:</b></div>

```javascript
                f (file.size > 5 * 1024 * 1024) {
```

<div style="color:green"><b>ADD:</b></div>

```javascript
                if (file.size > 5 * 1024 * 1024) {
```

## Regression Testing

- **Test Users:** any authenticated user.
- **Navigation:** Home and Profile pages.
- **Expected Results:** DevTools console has zero `SyntaxError` entries.
- **Failure Conditions:** if the error persists, re-save the file and hard-refresh.

<br>
<br>

---

<br>

# Console Error — POST Friends/GetFriendSuggestions 405

## Issue

```
sohba-core.js:37  POST https://localhost:7154/Friends/GetFriendSuggestions 405 (Method Not Allowed)
window.SohbaApp.post @ sohba-core.js:37
loadFriendSuggestions @ sidebar.js:18
```

## Related Feature

- **Feature Name:** Right Sidebar — People You May Know.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 7.x (Friends suggestions).

## Expected Behaviour

The right sidebar loads 5 friend suggestions for the current user.

## Current Behaviour

The request returns **405 Method Not Allowed** and the sidebar shows
"Could not load suggestions".

## Root Cause

- `FriendsController.GetFriendSuggestions` is decorated `[HttpGet]`.
- `Sohba/wwwroot/js/features/sidebar.js` `loadFriendSuggestions()` calls
  `SohbaApp.post('/Friends/GetFriendSuggestions', ...)`.
- `SohbaApp.post` ALWAYS uses `method: 'POST'` (see `sohba-core.js` line 39).
- POST to a GET-only endpoint → 405.

## Execution Flow

```
DOMContentLoaded → sidebar.js loadFriendSuggestions()
    → SohbaApp.post('/Friends/GetFriendSuggestions', { count: 5 })   (POST verb)
        → FriendsController.GetFriendSuggestions [HttpGet]  → 405
    → sohba-core.js: content-type check fails → { success:false }
    → sidebar.js writes "Could not load suggestions"
```

## Related Files

- `Sohba/wwwroot/js/features/sidebar.js`
- `Sohba/wwwroot/js/sohba-core.js`
- `Sohba/Controllers/FriendsController.cs`
- `Sohba/Views/Shared/Partials/_RightSidebar.cshtml`

## Affected Components

- JavaScript — `sidebar.js`
- Controller — `FriendsController.cs`

## Files That Need Modification

1. `Sohba/wwwroot/js/features/sidebar.js`

## Implementation Plan

1. In `sidebar.js`, replace `SohbaApp.post` with a `fetch` GET (or use the new
   `SohbaApp.get` helper added in Issue 3.10).
2. Parse the `BaseResponseDto` payload with the `data` field (lowercase, handled by
   `sohba-core.js` normalisation only for `post`; for raw fetch read `payload.data ?? payload.Data`).

## Code Changes

<div style="color:red"><b>REMOVE — the SohbaApp.post call inside loadFriendSuggestions:</b></div>

```javascript
        const result = await SohbaApp.post('/Friends/GetFriendSuggestions', { count: 5 });

        if (!result.success) {
            container.innerHTML = '<div class="text-xs text-center text-slate-400 py-2">Could not load suggestions</div>';
            return;
        }

        const users = result.data ?? [];
```

<div style="color:green"><b>REPLACE WITH — fetch GET (or SohbaApp.get):</b></div>

```javascript
        const response = await fetch('/Friends/GetFriendSuggestions?count=5');
        const payload = await response.json();

        if (!payload.success && !payload.Success) {
            container.innerHTML = '<div class="text-xs text-center text-slate-400 py-2">Could not load suggestions</div>';
            return;
        }

        const users = payload.data ?? payload.Data ?? [];
```

## Regression Testing

- **Test Users:** any authenticated user with pending suggestions.
- **Navigation:** Home page.
- **Expected Results:**
    - Network tab shows `GET /Friends/GetFriendSuggestions?count=5 → 200`.
    - Sidebar renders up to 5 suggestion cards.
    - Clicking the + button sends a friend request (POST to `/Friends/SendRequest`).
- **Failure Conditions:** 405 must never appear again for this endpoint.
- **Edge Cases:** user with no suggestions → "No suggestions right now".

<br>
<br>

---

<br>

# Issue 4.2 — Story Viewer Never Opens

## Issue

Clicking a story card does nothing. No console error appears. The Network tab shows the
`/Stories/GetUserStories` request returning 200 with a valid payload.

## Related Feature

- **Feature Name:** Stories — Story Viewer.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 4.2 (Open story).

## Expected Behaviour

Clicking a story card opens the story viewer modal with the user's story media, progress bar,
navigation, and marks the story as viewed.

## Current Behaviour

The story viewer modal never opens. No error is thrown.

## Root Cause

`Sohba/wwwroot/js/sohba-stories.js` — `openStoryViewer`:

```javascript
window.openStoryViewer = async function (userId) {
    currentUserId = userId;
    currentStoryIndex = 0;

    const response = await fetch(`/Stories/GetUserStories?userId=${userId}`);
    const stories = await response.json();

    if (stories && stories.length > 0) {   // ← BUG: stories is an OBJECT, not an array
        ...
    }
};
```

`StoriesController.GetUserStories` returns `BaseResponseDto<IEnumerable<StoryResponseDto>>`:

```json
{
    "data": [ ... ],
    "success": true,
    "error": null
}
```

So `stories` is an object. `stories.length` is `undefined`, the condition is false,
the modal never opens. No exception is thrown, hence "no console error".

## Execution Flow

```
Click story card
    → features/stories.js opens via data-action → openStoryViewer(userId)
        → fetch GET /Stories/GetUserStories?userId=...
            → StoriesController.GetUserStories → Json({ data: [...], success:true })
        → const stories = await response.json()      // object
        → stories.length → undefined → falsy → modal NOT opened (silently)
```

## Related Files

- `Sohba/wwwroot/js/sohba-stories.js`
- `Sohba/Controllers/StoriesController.cs`
- `Sohba/Views/Shared/Partials/_StoryRail.cshtml`
- `Sohba/Views/Shared/Partials/_StoryViewer.cshtml`
- `Sohba/wwwroot/js/features/stories.js`

## Affected Components

- JavaScript — `sohba-stories.js`
- Controller — `StoriesController.cs` (response shape)

## Files That Need Modification

1. `Sohba/wwwroot/js/sohba-stories.js`

## Implementation Plan

1. Unwrap the `BaseResponseDto` shape in `openStoryViewer`:
   - `const payload = await response.json();`
   - `const stories = payload.data ?? payload.Data ?? (Array.isArray(payload) ? payload : []);`
2. Optionally handle the `success:false` case with a toast.

## Code Changes

<div style="color:red"><b>REMOVE — from openStoryViewer in sohba-stories.js:</b></div>

```javascript
    const response = await fetch(`/Stories/GetUserStories?userId=${userId}`);
    const stories = await response.json();

    if (stories && stories.length > 0) {
        currentUserStories = stories;
        showStory(0);
        document.getElementById('storyViewerModal').classList.remove('hidden');
        document.body.style.overflow = 'hidden';
        startProgress();
    }
```

<div style="color:green"><b>REPLACE WITH — DTO-aware unwrap:</b></div>

```javascript
    const response = await fetch(`/Stories/GetUserStories?userId=${userId}`);
    const payload = await response.json();

    const stories = payload.data ?? payload.Data ?? (Array.isArray(payload) ? payload : []);

    if (stories && stories.length > 0) {
        currentUserStories = stories;
        showStory(0);
        document.getElementById('storyViewerModal').classList.remove('hidden');
        document.body.style.overflow = 'hidden';
        startProgress();
    } else {
        window.SohbaApp.toast('No stories available', 'info');
    }
```

## Regression Testing

- **Test Users:** `44444444-4444-4444-4444-444444444444` (Sara — has a story from the seeder).
- **Navigation:** Login → Home → click Sara's story card.
- **Expected Results:**
    - Story viewer opens with Sara's image and username.
    - Progress bar advances; after 5s it navigates to next story or closes.
    - `POST /Stories/MarkAsViewed` fires for the viewed story.
    - The story ring becomes gray after being viewed.
- **Failure Conditions:**
    - Viewer still does not open → check DevTools for the payload shape again.
- **Edge Cases:**
    - User with ZERO stories → toast "No stories available".
    - Video story (if any) → `<video>` renders and autoplays.
    - Keyboard navigation (← → Escape) works.

<br>
<br>

---

<br>

# Issue 5.1 — Groups Appear Duplicated

## Issue

The Groups page (and sidebar "Groups To Join") shows the same groups repeated many times
(Sohba Developers, Sohba Designers, Sohba Travelers ... with different IDs).
The user asks whether this is from the DB seeder.

## Related Feature

- **Feature Name:** Groups — Discover / Groups Index / Sidebar Groups.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 5.1 (Groups list).

## Expected Behaviour

Each group appears exactly once.

## Current Behaviour

Duplicate groups accumulate on every application start.

## Root Cause

**YES — this IS caused by the DB seeder.** `Sohba.Infrastructure/DBInitializer/DBInitializer.cs`:

- `InitializeAsync()` runs on every app startup:

```csharp
public async Task InitializeAsync()
{
    await _context.Database.MigrateAsync();
    await SeedRolesAsync();
    await SeedAdminUserAsync();
    await SeedTestUsersAsync();
    await SeedSampleDataAsync();
    await SeedExtraTestDataAsync();
}
```

- `SeedTestUsersAsync()` → `CreateRelationshipsAsync(...)` →
  `CreateGroupAsync("Sohba Developers", ...)`:

```csharp
private async Task<Group> CreateGroupAsync(string name, string description, Guid adminId, string imageUrl)
{
    var group = new Group
    {
        Id = Guid.NewGuid(),    // ← NEW ID EVERY TIME - no existence check by Name
        ...
    };

    _context.Groups.Add(group);
    var rowsAffected = await _context.SaveChangesAsync();
    if (rowsAffected == 0) { throw ... }

    await AddGroupMemberAsync(group.Id, adminId, GroupRole.Admin);
    return group;
}
```

- `AddGroupMemberAsync` DOES check existence (`AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId)`),
  but because the group itself is always created fresh, the member check never blocks anything.
- `CreatePageAsync` has the same bug (`Guid.NewGuid()` with no name check).
- `AddFriendshipAsync` has a partial guard, but only the forward direction — reversed
  duplicates can occur (see Additional Issues).

So every app restart inserts 3 new Groups + 3 new Pages + new Posts + new followers.

This ALSO causes "duplicated Pages in the sidebar" — each duplicate Page has a different ID,
so the sidebar / Pages list shows N copies of the same name.

## Execution Flow

```
App startup
  → InitializeAsync
    → SeedTestUsersAsync → CreateRelationshipsAsync
        → CreateGroupAsync("Sohba Developers")   → Id = Guid.NewGuid()  → INSERT (group #1)
        → CreateGroupAsync("Sohba Designers")    → INSERT (group #1)
        → CreateGroupAsync("Sohba Travelers")    → INSERT (group #1)
        ...
  → (next startup)
    → same methods run again
        → CreateGroupAsync("Sohba Developers")   → Id = GUID.NEW → INSERT (group #2)
        → ... duplicates accumulate
```

## Related Files

- `Sohba.Infrastructure/DBInitializer/DBInitializer.cs`
- `Sohba/Views/Groups/Index.cshtml`
- `Sohba/Views/Shared/Partials/_Sidebar.cshtml`
- `Sohba.Application/Services/GroupService.cs` (`GetAllGroupsAsync`)
- `Sohba/Controllers/GroupsController.cs` (`Discover`, `Index`)

## Affected Components

- Infrastructure — `DBInitializer.cs`
- Database — duplicate rows

## Files That Need Modification

1. `Sohba.Infrastructure/DBInitializer/DBInitializer.cs`

## Implementation Plan

1. Make ALL seeding idempotent:
   - `CreateGroupAsync`: check `Name` first; if exists return it.
   - `CreatePageAsync`: check `Name` first; if exists return it.
   - `CreatePostAsync`: check by `Title` + author (`UserId`) first.
2. Add a name-exists check at the group/page level so the service also prevents duplicates.
3. Provide a SQL cleanup script for existing duplicates (keep the row with the oldest
   `CreatedAt`, reassign members to the kept row, delete the rest).
4. Optionally add a unique index on `Groups.Name` and `Pages.Name` (after cleaning duplicates).

## Code Changes

<div style="color:red"><b>REMOVE — from CreateGroupAsync in DBInitializer.cs (unedited):</b></div>

```csharp
        private async Task<Group> CreateGroupAsync(string name, string description, Guid adminId, string imageUrl)
        {
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = imageUrl,
                GroupMembers = new List<GroupMember>()
            };

            _context.Groups.Add(group);
```

<div style="color:green"><b>ADD — idempotency check at the top:</b></div>

```csharp
        private async Task<Group> CreateGroupAsync(string name, string description, Guid adminId, string imageUrl)
        {
            var existing = await _context.Groups
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.Name == name);
            if (existing != null)
            {
                if (existing.GroupMembers.All(m => m.UserId != adminId))
                {
                    await AddGroupMemberAsync(existing.Id, adminId, GroupRole.Admin);
                }
                return existing;
            }

            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = imageUrl,
                GroupMembers = new List<GroupMember>()
            };

            _context.Groups.Add(group);
```

<div style="color:red"><b>REMOVE — from CreatePageAsync in DBInitializer.cs (unedited):</b></div>

```csharp
        private async Task<Page> CreatePageAsync(string name, string description, Guid adminId, string imageUrl)
        {
            var page = new Page
            {
                Id = Guid.NewGuid(),
                Name = name,
                ...
            };
```

<div style="color:green"><b>ADD — idempotency check at the top:</b></div>

```csharp
        private async Task<Page> CreatePageAsync(string name, string description, Guid adminId, string imageUrl)
        {
            var existing = await _context.Pages.FirstOrDefaultAsync(p => p.Name == name);
            if (existing != null)
            {
                await AddPageFollowerAsync(existing.Id, adminId);
                return existing;
            }

            var page = new Page
            {
                Id = Guid.NewGuid(),
                Name = name,
                ...
            };
```

<div style="color:green"><b>ADD — SQL cleanup for existing duplicates (run manually / in a migration):</b></div>

```sql
-- Keep the earliest group per name, reassign members, then delete duplicates
;WITH cte AS (
    SELECT Id,
           Name,
           ROW_NUMBER() OVER (PARTITION BY Name ORDER BY CreatedAt) AS rn
    FROM Groups
)
DELETE FROM GroupMembers WHERE GroupId IN (SELECT Id FROM cte WHERE rn > 1);
DELETE FROM Groups WHERE Id IN (SELECT Id FROM cte WHERE rn > 1);

;WITH cte AS (
    SELECT Id,
           Name,
           ROW_NUMBER() OVER (PARTITION BY Name ORDER BY CreatedAt) AS rn
    FROM Pages
)
DELETE FROM PageFollowers WHERE PageId IN (SELECT Id FROM cte WHERE rn > 1);
DELETE FROM Pages WHERE Id IN (SELECT Id FROM cte WHERE rn > 1);
```

## Regression Testing

- **Test Users:** any user.
- **Navigation:** `/Groups`, Home sidebar "Groups To Join", `/Pages`, Home sidebar "Pages For You".
- **Expected Results:**
    - Each group/page appears exactly once.
    - Restarting the application does NOT add more groups/pages.
- **Failure Conditions:** duplicate rows still appear → run the SQL cleanup first.
- **Edge Cases:** A user-created group that happens to share a seeder name must not be
  overwritten — the idempotency check only returns the existing row; its admin member is
  only added if not already present.

<br>
<br>

---

<br>

# Issue 5.2 — Edit Group / Leave Visible To Non-Members

## Issue

On a group details page where the current user is NOT a member, the UI shows
**Edit Group** and **Leave** buttons. Clicking Edit opens the edit page (security issue);
Leave opens the leave confirmation (also wrong because the user isn't a member).

## Related Feature

- **Feature Name:** Groups — Group Details page / membership actions.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 5.2 (Join/Leave/Edit group).

## Expected Behaviour

- **Edit Group** — visible ONLY to the group admin.
- **Leave Group** — visible ONLY to current members.
- **Join Group** — visible ONLY to non-members.
- Server-side authorization must also enforce these rules.

## Current Behaviour

- The `@if (User.Identity?.Name == Model.Group.AdminName)` condition is **commented out**
  (lines 39-45), so Edit Group is always rendered.
- The Leave button (line 46) is always rendered.

## Root Cause

In `Sohba/Views/Groups/Details.cshtml`:

```html
@* @if (User.Identity?.Name == Model.Group.AdminName) *@
@* { *@
    <a asp-action="Edit" ...>Edit Group</a>
@* } *@
<button onclick="leaveGroup('@Model.Group.Id')" ...>Leave</button>

@*We Will Comment The Upper Button Leave And Uncomment That -- In Testing *@
@* @if (Model.Group.IsCurrentUserMember) { <button>Leave Group</button> }
else { <button>Join Group</button> } *@
```

The developer intentionally commented the access checks "for testing" — but they were left
commented in production.

Additionally, server-side:

- `GroupsController.Edit(Guid id)` has the ownership check **commented out**:

```csharp
//if (groupResult.Value.AdminName != GetCurrentUserName())
//    return Forbid();
```

So even if the UI were fixed, any user could call `/Groups/Edit/{id}` directly and edit a group.

## Execution Flow

```
Non-member navigates to /Groups/Details/{groupId}
    → Details.cshtml renders actions
        → Edit Group: always shown (condition commented)
        → Leave: always shown
        → Leave click → showConfirmModal → would fail (showConfirmModal bug) OR proceeds
```

## Related Files

- `Sohba/Views/Groups/Details.cshtml`
- `Sohba/Controllers/GroupsController.cs` (`Edit` GET/POST, `Leave`)
- `Sohba.Application/Services/GroupService.cs` (`UpdateGroupAsync`, `LeaveGroupAsync`)
- `Sohba.Application/DTOs/GroupAndPageAggregate/GroupResponseDto.cs` (check for IsCurrentUserMember)

## Affected Components

- View — `Groups/Details.cshtml`
- Controller — `GroupsController.cs`

## Files That Need Modification

1. `Sohba/Views/Groups/Details.cshtml`
2. `Sohba/Controllers/GroupsController.cs`

## Implementation Plan

1. In `Groups/Details.cshtml`:
   - Show **Edit Group** only when `Model.Group.AdminId == currentUserId`.
   - Show **Leave Group** only when `Model.Group.IsCurrentUserMember`.
   - Show **Join Group** only when NOT a member.
2. Pass `currentUserId` to the view (via `ViewBag.CurrentUserId` set in `Details` action).
3. Un-comment and improve the server-side check in `GroupsController.Edit`:

```csharp
if (groupResult.Value.AdminId != userId)
    return Forbid();
```

4. Sign the `Leave` endpoint must also verify membership before leaving.

## Code Changes

<div style="color:red"><b>REMOVE — the always-visible action block in Groups/Details.cshtml:</b></div>

```html
            <div class="flex items-center gap-3">
                @* @if (User.Identity?.Name == Model.Group.AdminName) *@
                @* { *@
                    <a asp-action="Edit" asp-route-id="@Model.Group.Id" class="px-5 py-2.5 bg-slate-100 text-gray-700 font-bold rounded-xl hover:bg-slate-200 transition-all flex items-center gap-2">
                        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                        Edit Group
                    </a>
                @* } *@
                <button onclick="leaveGroup('@Model.Group.Id')" class="px-5 py-2.5 bg-red-50 text-red-600 font-bold rounded-xl hover:bg-red-100 transition-all">
                    Leave
                </button>

                @*We Will Comment The Upper Button Leave And Uncomment That -- In Testing *@
                @* @if (Model.Group.IsCurrentUserMember)
                {
                    <button onclick="leaveGroup('@Model.Group.Id')"
                            class="px-5 py-2.5 bg-red-50 text-red-600 font-bold rounded-xl hover:bg-red-100 transition-all">
                        Leave Group
                    </button>
                }
                else
                {
                    <button onclick="joinGroup('@Model.Group.Id')"
                            class="px-5 py-2.5 bg-[#345e69] text-white font-bold rounded-xl hover:bg-[#2a4b55] transition-all">
                        Join Group
                    </button>
                } *@
            </div>
```

<div style="color:green"><b>ADD — correct conditional actions:</b></div>

```html
            <div class="flex items-center gap-3">
                @if (ViewBag.CurrentUserId == Model.Group.AdminId)
                {
                    <a asp-action="Edit" asp-route-id="@Model.Group.Id" class="px-5 py-2.5 bg-slate-100 text-gray-700 font-bold rounded-xl hover:bg-slate-200 transition-all flex items-center gap-2">
                        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                        Edit Group
                    </a>
                }

                @if (Model.Group.IsCurrentUserMember)
                {
                    <button onclick="leaveGroup('@Model.Group.Id')"
                            class="px-5 py-2.5 bg-red-50 text-red-600 font-bold rounded-xl hover:bg-red-100 transition-all">
                        Leave Group
                    </button>
                }
                else
                {
                    <button onclick="joinGroup('@Model.Group.Id')"
                            class="px-5 py-2.5 bg-[#345e69] text-white font-bold rounded-xl hover:bg-[#2a4b55] transition-all">
                        Join Group
                    </button>
                }
            </div>
```

<div style="color:red"><b>REMOVE — commented-out ownership check in GroupsController.Edit:</b></div>

```csharp
            //if (groupResult.Value.AdminName != GetCurrentUserName())
              //  return Forbid();
```

<div style="color:green"><b>ADD — proper ownership check:</b></div>

```csharp
            if (groupResult.Value.AdminId != userId)
                return Forbid();
```

<div style="color:green"><b>ADD — ViewBag.CurrentUserId in GroupsController.Details (so the view can compare):</b></div>

```csharp
            ViewBag.CurrentUserId = GetCurrentUserId();
```

## Regression Testing

- **Test Users:**
    - `mohammed@sohba.com` (admin of "Sohba Developers").
    - `khaled@sohba.com` (member of "Sohba Developers", non-admin).
    - `omar@sohba.com` (NOT a member).
- **Navigation:** `/Groups/Details/{Sohba Developers id}` logged in as each user.
- **Expected Results:**
    - Mohammed: sees Edit Group + Leave Group.
    - Khaled: sees Leave Group only (no Edit).
    - Omar: sees Join Group only (no Edit, no Leave).
    - Directly opening `/Groups/Edit/{id}` as non-admin → 403 Forbid.
- **Failure Conditions:** Edit must never render for non-admin. Leave must never render for non-member.
- **Edge Cases:** Group admin who is also a member must see both buttons; banned user must not
  see Join.

<br>
<br>

---

<br>

# Issue 5.4 — Group Action Button Not Working

## Issue

The group action button (Join / Leave) does not work on `Groups/Details.cshtml`.

## Related Feature

- **Feature Name:** Groups — Join / Leave actions.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 5.4.

## Expected Behaviour

Clicking Join adds the user to the group; clicking Leave removes them, with a confirmation.

## Current Behaviour

Clicking Leave throws `showConfirmModal is not a function` (the confirmation modal script is
missing). Clicking Join (if visible) also hits the same missing function via
`joinGroup` in the sidebar — `GroupsController.Join` is a valid POST endpoint, but the UI
needs `showConfirmModal` for leave confirmation, and the inline `leaveGroup` function in
`Details.cshtml` calls `showConfirmModal`.

## Root Cause

Same cross-cutting root cause as Issue 3.6:
- `Sohba/wwwroot/js/features/modal.js` (which defines `showConfirmModal`) is NOT loaded.
- `_ConfirmModal.cshtml` internal script is commented out.

## Execution Flow

```
Click Leave
    → inline leaveGroup(groupId)
        → showConfirmModal({...})   → undefined → TypeError
```

## Related Files

- `Sohba/Views/Groups/Details.cshtml`
- `Sohba/Views/Shared/Partials/_ConfirmModal.cshtml`
- `Sohba/wwwroot/js/features/modal.js`
- `Sohba/Views/Shared/_AppLayout.cshtml`
- `Sohba/Controllers/GroupsController.cs` (`Join`, `Leave`)

## Affected Components

- View / JS — confirm modal availability

## Files That Need Modification

1. `Sohba/Views/Shared/_AppLayout.cshtml` (load `features/modal.js`)

## Implementation Plan

1. Apply the Cross-Cutting Fix (load `features/modal.js`).
2. Verify `leaveGroup` and `joinGroup` call `SohbaApp.post('/Groups/Leave', { groupId })` and
   `SohbaApp.post('/Groups/Join', { id: groupId })` — note the payload key difference:
   - `Join` uses `IdRequestDto` → `{ id: groupId }`
   - `Leave` uses `LeaveGroupRequest` → `{ groupId }`

   The current `Details.cshtml` inline `leaveGroup` already posts `{ groupId }` — correct.
   The sidebar `joinGroup` posts `{ groupId }` — **WRONG** for `Join` which expects `{ id }`.
   Fix the sidebar too (it currently calls joinGroup with `{ groupId }` → `Id` will be
   `Guid.Empty` → "Invalid group ID").

## Code Changes

<div style="color:red"><b>REMOVE — the wrong payload in _Sidebar.cshtml joinGroup function:</b></div>

```javascript
    async function joinGroup(groupId) {
        const result = await SohbaApp.post('/Groups/Join', { groupId });
```

<div style="color:green"><b>ADD — the correct payload key (Join endpoint uses IdRequestDto.Id):</b></div>

```javascript
    async function joinGroup(groupId) {
        const result = await SohbaApp.post('/Groups/Join', { id: groupId });
```

Note: `Groups/Index.cshtml` inline `joinGroup(groupId, button)` already posts `{ id: groupId }`
correctly. Keep it.

## Regression Testing

- **Test Users:** `omar@sohba.com` (non-member of the target group).
- **Navigation:** `/Groups/Details/{groupId}` → click Join.
- **Expected Results:**
    - Join succeeds (network: `POST /Groups/Join` 200, JSON `{ success:true }`).
    - Button/UI updates to member state; page reload shows Leave Group.
    - Leave shows the Confirm modal; confirming leaves the group.
- **Failure Conditions:** "Invalid group ID" indicates the payload key is still wrong.
- **Edge Cases:** Joining twice (fast double click) — the service should fail gracefully with
  "Already a member" (domain rule).

<br>
<br>

---

<br>

# Issue 6.2 — Pages: No Images, No Preview, No Redirect

## Issue

- `/Pages` does not display page images (only colored placeholder squares).
- `/Pages/Create` has no image preview.
- After creating a new Page, the user is NOT redirected to the new page (unlike Groups).

## Related Feature

- **Feature Name:** Pages — List / Create.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 6.2 (Pages CRUD).

## Expected Behaviour

- `/Pages` shows each page's `ImageUrl` thumbnails.
- `/Pages/Create` shows a live image preview when a file is chosen.
- Creating a Page redirects to `Pages/Details/{newPageId}` (like Groups does).

## Current Behaviour

- `/Pages` always renders a gradient placeholder; `item.ImageUrl` is never used.
- `/Pages/Create` preview never appears; the JS references a non-existent element id.
- Creating succeeds but redirects to `/Pages` (Index) instead of the created page.

## Root Cause

### Bug A — no image in Pages list

`Sohba/Views/Pages/Index.cshtml` line 31:

```html
<div class="w-14 h-14 bg-gradient-to-br from-emerald-500 to-teal-600 rounded-2xl flex items-center justify-center text-white text-xl font-bold">
    @item.Name[0]
</div>
```

`item.ImageUrl` is available on the model but never rendered.

### Bug B — no preview in Pages/Create

`Sohba/Views/Pages/Create.cshtml` lines 73-90:

```javascript
document.getElementById('pageImageInput')?.addEventListener('change', function (e) {
```

The `<input asp-for="ImageFile" ...>` generates `id="ImageFile"`, NOT `id="pageImageInput"`.
So the listener is never attached.

### Bug C — no redirect to Details

`Sohba/Controllers/PagesController.cs` — `Create` POST action:

```csharp
if (result.IsSuccess)
    return RedirectToAction("Index");   // ← redirects to the list, not the new page
```

## Execution Flow

```
Browser GET /Pages
    → PagesController.Index → view
        → Placeholder div (ImageUrl ignored)   ← Bug A

Browser GET /Pages/Create → user picks file
    → pageImageInput listener never attached    ← Bug B (id mismatch)

User submits create form
    → PagesController.Create POST
        → _pageService.CreatePageAsync
        → success → RedirectToAction("Index")   ← Bug C (should be Details)
```

## Related Files

- `Sohba/Views/Pages/Index.cshtml`
- `Sohba/Views/Pages/Create.cshtml`
- `Sohba/Controllers/PagesController.cs`
- `Sohba/Views/Groups/Create.cshtml` (correct pattern to mimic)

## Affected Components

- View — `Pages/Index.cshtml`
- View — `Pages/Create.cshtml`
- Controller — `PagesController.cs`

## Files That Need Modification

1. `Sohba/Views/Pages/Index.cshtml`
2. `Sohba/Views/Pages/Create.cshtml`
3. `Sohba/Controllers/PagesController.cs`

## Implementation Plan

1. **Pages list**: render the `ImageUrl` if present, fall back to the placeholder.
2. **Pages/Create**: fix the JS selector to `ImageFile` (or add an explicit `id="pageImageInput"`
   via a tag-helper `id` attribute).
3. **PagesController.Create**: change `RedirectToAction("Index")` →
   `RedirectToAction("Details", new { id = result.Value.Id })`, mirroring Groups.

## Code Changes

<div style="color:red"><b>REMOVE — from Pages/Index.cshtml placeholder:</b></div>

```html
                        <div class="w-14 h-14 bg-gradient-to-br from-emerald-500 to-teal-600 rounded-2xl flex items-center justify-center text-white text-xl font-bold">
                            @item.Name[0]
                        </div>
```

<div style="color:green"><b>ADD — image-aware rendering:</b></div>

```html
                        @if (!string.IsNullOrEmpty(item.ImageUrl))
                        {
                            <img src="@item.ImageUrl" class="w-14 h-14 rounded-2xl object-cover shadow-sm border border-slate-100" alt="@item.Name" />
                        }
                        else
                        {
                            <div class="w-14 h-14 bg-gradient-to-br from-emerald-500 to-teal-600 rounded-2xl flex items-center justify-center text-white text-xl font-bold">
                                @item.Name[0]
                            </div>
                        }
```

<div style="color:red"><b>REMOVE — wrong selector in Pages/Create.cshtml script:</b></div>

```javascript
        document.getElementById('pageImageInput')?.addEventListener('change', function (e) {
```

<div style="color:green"><b>ADD — correct selector (matches asp-for generated id):</b></div>

```javascript
        document.getElementById('ImageFile')?.addEventListener('change', function (e) {
```

<div style="color:red"><b>REMOVE — wrong redirect in PagesController.Create:</b></div>

```csharp
            if (result.IsSuccess)
                return RedirectToAction("Index");
```

<div style="color:green"><b>ADD — redirect to the created page:</b></div>

```csharp
            if (result.IsSuccess)
                return RedirectToAction("Details", new { id = result.Value.Id });
```

## Regression Testing

- **Test Users:** `omar@sohba.com` (page creator).
- **Navigation:** `/Pages` then `/Pages/Create`.
- **Expected Results:**
    - `/Pages` shows page images.
    - `/Pages/Create`: picking an image shows the preview; removing clears it.
    - Submitting redirects to `/Pages/Details/{newId}`.
- **Failure Conditions:** creating a page without an image still works and redirects correctly.
- **Edge Cases:** > 5MB image rejected by `LocalFileStorageService`; existing page with null ImageUrl.

<br>
<br>

---

<br>

# Issue 7.2 — Search Not Working At All

## Issue

The header search box does nothing. No console message, no network request.
The user also wants:
- A dedicated Search button (for main search).
- A dedicated Friends-search button.
(Because mobile users may not have an Enter key.)

## Related Feature

- **Feature Name:** Search — Global Header Search + Quick Results.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 7.2 (Search flow).

## Expected Behaviour

- Typing in the header search shows a quick-results dropdown (people/posts/groups/pages).
- Pressing Enter OR clicking a Search button navigates to `/Search/Index?q=...`.
- A "Search Friends" button appears on the Friends/Find page.

## Current Behaviour

- The search input is inert. Typing produces nothing. No network call.

## Root Cause

The ENTIRE header search JavaScript is commented out inside
`Sohba/Views/Shared/Partials/_Header.cshtml` (lines 222-733 in the file — the giant commented
`<script>` block). The code that wires:

- `#searchInput` input → `/Search/QuickSearch`
- `#quickSearchResults` dropdown rendering
- Enter key → `#searchForm` submit
- `#mobileSearchBtn` / `#mobileSearchContainer` toggle

...is all commented out and was never placed into a loaded `.js` file. `Sohba/wwwroot/js/site.js`
is empty. `Sohba/wwwroot/js/features/search.js` only handles the tab switching on the
results page — it does NOT wire the header input.

## Execution Flow

```
User types in #searchInput
    → nothing is listening (no JS loaded that adds a listener)
    → no network request
User presses Enter
    → no keypress handler on #searchInput
    → no form submission
```

## Related Files

- `Sohba/Views/Shared/Partials/_Header.cshtml`
- `Sohba/wwwroot/js/features/search.js`
- `Sohba/wwwroot/js/site.js`
- `Sohba/Controllers/SearchController.cs` (`Index`, `QuickSearch`)
- `Sohba/Views/Search/Results.cshtml`
- `Sohba/Views/Friends/Find.cshtml`

## Affected Components

- JavaScript — header search (missing)
- View — `_Header.cshtml`
- Controller — `SearchController.cs` (endpoints exist and work)

## Files That Need Modification

1. `Sohba/Views/Shared/Partials/_Header.cshtml`
2. `Sohba/wwwroot/js/features/search.js` (add header-search wiring)
3. `Sohba/Views/Shared/_AppLayout.cshtml` (load `features/search.js` globally)

## Implementation Plan

1. **Move the commented search logic into `features/search.js`** so it loads globally
   (the app layout loads feature scripts on every page).
2. **Wire the input:**
   - `input` event → debounced `fetch('/Search/QuickSearch?q=...')` → render dropdown.
   - `keypress`/`keydown` Enter → submit `#searchForm`.
   - `click` outside → close dropdown.
3. **Add a search button** to the right of the input (and a mobile-friendly variant).
4. **Add a "Search Friends" button** on the Friends Find page (a secondary search input with
   its own button) that filters the `user-card` list (the existing input-event filter can stay)
   and a button that navigates to `/Friends/Find?q=...`.
5. Note the QuickSearch endpoint exists and works: `GET /Search/QuickSearch?q=...`.
6. Remove (or keep commented) the OLD giant script block from `_Header.cshtml` to avoid
   double-registration.

## Code Changes

<div style="color:green"><b>ADD — to Sohba/wwwroot/js/features/search.js (header + mobile search + friends search):</b></div>

```javascript
// ============================================================
// HEADER GLOBAL SEARCH (quick results + submit on Enter/Button)
// ============================================================
function initializeGlobalSearch() {
    const searchInput = document.getElementById('searchInput');
    const quickResults = document.getElementById('quickSearchResults');
    const searchForm = document.getElementById('searchForm');
    const searchQueryHidden = document.getElementById('searchQueryHidden');
    const searchBtn = document.getElementById('globalSearchBtn');

    if (!searchInput) return;

    let searchTimeout;

    searchInput.addEventListener('input', function (e) {
        const query = e.target.value.trim();
        clearTimeout(searchTimeout);

        if (query.length < 2) {
            if (quickResults) quickResults.classList.add('hidden');
            return;
        }

        searchTimeout = setTimeout(async () => {
            try {
                const response = await fetch(`/Search/QuickSearch?q=${encodeURIComponent(query)}`);
                const data = await response.json();

                if (data.success === false || data.data === null) {
                    if (quickResults) {
                        quickResults.innerHTML = '<div class="p-4 text-center text-gray-500">No results found</div>';
                        quickResults.classList.remove('hidden');
                    }
                    return;
                }

                const payload = data.data;
                if (!payload || payload.totalCount === 0) {
                    if (quickResults) {
                        quickResults.innerHTML = '<div class="p-4 text-center text-gray-500">No results found</div>';
                        quickResults.classList.remove('hidden');
                    }
                    return;
                }

                let html = '';

                const users = payload.users || [];
                if (users.length > 0) {
                    html += '<div class="px-4 py-2 bg-gray-50 text-xs font-bold text-gray-500">PEOPLE</div>';
                    html += users.map(user => `
                        <a href="${user.url}" class="flex items-center gap-3 px-4 py-2 hover:bg-gray-50 transition-colors">
                            <img src="${user.profilePictureUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(user.name)}&background=345e69&color=fff`}" class="w-8 h-8 rounded-full object-cover">
                            <div>
                                <div class="font-semibold text-gray-900">${user.name}</div>
                                <div class="text-xs text-gray-500">${user.bio || 'User'}</div>
                            </div>
                        </a>`).join('');
                }

                const posts = payload.posts || [];
                if (posts.length > 0) {
                    html += '<div class="px-4 py-2 bg-gray-50 text-xs font-bold text-gray-500">POSTS</div>';
                    html += posts.map(post => `
                        <a href="${post.url}" class="flex items-center gap-3 px-4 py-2 hover:bg-gray-50 transition-colors">
                            ${post.imageUrl
                                ? `<img src="${post.imageUrl}" class="w-8 h-8 rounded object-cover">`
                                : '<div class="w-8 h-8 bg-gray-200 rounded flex items-center justify-center text-gray-500">📝</div>'}
                            <div>
                                <div class="font-semibold text-gray-900">${post.title}</div>
                                <div class="text-xs text-gray-500">${post.authorName}</div>
                            </div>
                        </a>`).join('');
                }

                const groups = payload.groups || [];
                if (groups.length > 0) {
                    html += '<div class="px-4 py-2 bg-gray-50 text-xs font-bold text-gray-500">GROUPS</div>';
                    html += groups.map(group => `
                        <a href="${group.url}" class="flex items-center gap-3 px-4 py-2 hover:bg-gray-50 transition-colors">
                            <div class="w-8 h-8 bg-gray-200 rounded-lg flex items-center justify-center text-gray-500 font-bold">${group.name[0]}</div>
                            <div>
                                <div class="font-semibold text-gray-900">${group.name}</div>
                                <div class="text-xs text-gray-500">${group.membersCount} members</div>
                            </div>
                        </a>`).join('');
                }

                const pages = payload.pages || [];
                if (pages.length > 0) {
                    html += '<div class="px-4 py-2 bg-gray-50 text-xs font-bold text-gray-500">PAGES</div>';
                    html += pages.map(page => `
                        <a href="${page.url}" class="flex items-center gap-3 px-4 py-2 hover:bg-gray-50 transition-colors">
                            <div class="w-8 h-8 bg-gray-200 rounded-lg flex items-center justify-center text-gray-500 font-bold">${page.name[0]}</div>
                            <div class="font-semibold text-gray-900">${page.name}</div>
                        </a>`).join('');
                }

                if (payload.totalCount > 3) {
                    html += `
                        <div class="p-3 border-t border-gray-100 text-center">
                            <a href="/Search/Index?q=${encodeURIComponent(query)}"
                               class="text-sm text-[#345e69] font-semibold hover:underline">
                                See all ${payload.totalCount} results →
                            </a>
                        </div>`;
                }

                quickResults.innerHTML = html;
                quickResults.classList.remove('hidden');
            } catch (error) {
                console.error('Search error:', error);
            }
        }, 300);
    });

    function submitSearch() {
        const query = searchInput.value.trim();
        if (query.length >= 2 && searchForm) {
            if (searchQueryHidden) searchQueryHidden.value = query;
            searchForm.submit();
        } else if (query.length < 2) {
            if (window.SohbaApp && SohbaApp.toast) {
                SohbaApp.toast('Type at least 2 characters', 'info');
            }
        }
    }

    if (searchBtn) {
        searchBtn.addEventListener('click', function (e) {
            e.preventDefault();
            submitSearch();
        });
    }

    searchInput.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            submitSearch();
        }
    });

    document.addEventListener('click', function (e) {
        if (quickResults && !searchInput.contains(e.target) && !quickResults.contains(e.target)) {
            quickResults.classList.add('hidden');
        }
    });
}

// ============================================================
// MOBILE SEARCH TOGGLE
// ============================================================
function initializeMobileSearch() {
    const searchBtn = document.getElementById('mobileSearchBtn');
    const searchContainer = document.getElementById('mobileSearchContainer');
    if (searchBtn && searchContainer) {
        searchBtn.addEventListener('click', function () {
            const isClosed = searchContainer.classList.contains('max-h-0');
            const isOpen = isClosed;
            searchContainer.classList.toggle('max-h-0', !isOpen);
            searchContainer.classList.toggle('opacity-0', !isOpen);
            searchContainer.classList.toggle('border-transparent', !isOpen);
            searchContainer.classList.toggle('max-h-40', isOpen);
            searchContainer.classList.toggle('opacity-100', isOpen);
            searchContainer.classList.toggle('border-slate-100', isOpen);
            if (isOpen) {
                setTimeout(() => searchContainer.querySelector('input')?.focus(), 100);
            }
        });
    }
}

// ============================================================
// FRIENDS SEARCH BUTTON (Find Friends page)
// ============================================================
function initializeFriendsSearch() {
    const friendsSearchBtn = document.getElementById('friendsSearchBtn');
    const searchInput = document.getElementById('friendsSearchInput');
    if (friendsSearchBtn && searchInput) {
        friendsSearchBtn.addEventListener('click', function () {
            const term = searchInput.value.trim().toLowerCase();
            const userCards = document.querySelectorAll('.user-card');
            let visibleCount = 0;
            userCards.forEach(card => {
                const name = (card.dataset.name || '').toLowerCase();
                if (name.includes(term)) {
                    card.style.display = 'block';
                    visibleCount++;
                } else {
                    card.style.display = 'none';
                }
            });
            const noResults = document.getElementById('noResultsMessage');
            if (noResults) {
                noResults.classList.toggle('hidden', visibleCount > 0);
            }
        });
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
        initializeGlobalSearch();
        initializeMobileSearch();
        initializeFriendsSearch();
    });
} else {
    initializeGlobalSearch();
    initializeMobileSearch();
    initializeFriendsSearch();
}
```

<div style="color:green"><b>ADD — the Search button next to the input in _Header.cshtml:</b></div>

```html
                        <input type="text"
                               id="searchInput"
                               ...
                               placeholder="Search for posts, people, groups, pages..."
                               autocomplete="off">
                        <button id="globalSearchBtn"
                                type="button"
                                class="absolute right-1.5 top-1/2 -translate-y-1/2 px-3 py-1.5 bg-[#345e69] text-white text-sm font-semibold rounded-xl hover:bg-[#2a4b55] transition-colors">
                            Search
                        </button>
                        <div id="quickSearchResults" class="hidden absolute top-full left-0 right-0 mt-2 bg-white rounded-2xl shadow-2xl border border-gray-200 z-[100] max-h-96 overflow-y-auto"></div>
```

<div style="color:green"><b>ADD — in _AppLayout.cshtml, load search.js globally:</b></div>

```html
    <script src="~/js/features/search.js" asp-append-version="true"></script>
```

## Regression Testing

- **Test Users:** any authenticated user.
- **Navigation:** Home → type `sohba` in header search.
- **Expected Results:**
    - A dropdown appears with People/Posts/Groups/Pages after ~300ms.
    - Clicking Search button (or Enter) navigates to `/Search/Index?q=sohba`.
    - Clicking outside closes the dropdown.
    - Mobile view: the search icon toggles the mobile search box.
    - Friends page: type + button filters friend cards.
- **Failure Conditions:** type fewer than 2 chars → toast, no request.
- **Edge Cases:** search term with special characters (encodeURIComponent already applied);
  empty results → "No results found".

<br>
<br>

---

<br>

# Issue 7.4 & 7.5 — Accept / Reject Friend Request Fails With 429

## Issue

When accepting/declining a friend request:

```
POST https://localhost:7154/Friends/AcceptRequest 429 (Too Many Requests)
POST https://localhost:7154/Friends/RejectRequest  429 (Too Many Requests)
```

Also shows toast: `No pending friend request found.`

## Related Feature

- **Feature Name:** Friends — Accept / Reject Friend Requests.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 7.4 / 7.5.

## Expected Behaviour

- Clicking Accept once accepts the request and removes the card from the pending list.
- Clicking Decline once rejects and removes the card.
- No 429 should occur.

## Current Behaviour

- The first click may appear to do nothing or shows "No pending friend request found", then
  rapid retries trigger the `FriendRequest` rate limiter (limit = 10 / minute) → 429s.

## Root Cause

There are THREE real issues combining:

### Root Cause A — `FriendshipRepository.GetByUsersAsync` drops the reversed direction

```csharp
public async Task<Friend?> GetByUsersAsync(Guid userId, Guid friendId)
{
    var friendship = await _context.Friends
        .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendUserId == friendId);

    if (friendship == null)
    {
        var reversed = await _context.Friends
            .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendUserId == userId);
        // ← reversed is found but NEVER returned
    }

    return friendship;   // ← still null when only the reversed row exists
}
```

In `AcceptFriendRequestAsync`:

```csharp
var friendship = await _unitOfWork.Friendships.GetByUsersAsync(senderId, receiverId);
if (friendship == null)
    return Result.Failure("Friend request not found.");
```

If the stored row is `{ UserId = sender, FriendUserId = receiver }` and we call
`GetByUsersAsync(senderId, receiverId)` the forward lookup works. But if the row happens to be
stored in the other direction (which can happen with reverse duplicate rows caused by the
seeder, see Additional Issues), the lookup returns null only to then say
"No pending friend request found" — even though a valid pending request exists.

### Root Cause B — `HasPendingRequestAsync` ignores the reversed direction

```csharp
public async Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId)
{
    var exists = await _context.Friends
        .AnyAsync(f => f.UserId == senderId && f.FriendUserId == receiverId && f.Status == FriendshipStatus.Pending);
    if (!exists)
    {
        var reversedExists = await _context.Friends
            .AnyAsync(f => f.UserId == receiverId && f.FriendUserId == senderId && f.Status == FriendshipStatus.Pending);
        // ← ignored
    }
    return exists;   // ← reversedExists never influences the result
}
```

So `AcceptFriendRequestAsync` may pass the domain check but then fail to find the row, or
vice-versa.

### Root Cause C — client retries + strict rate limit

`friends.js` accept/reject functions do NOT disable the button while awaiting. A user
double-clicking (or the UI being slow to remove the card) causes multiple POSTs in the same
second. With `EnforceFixedWindowLimiter` `"FriendRequest"` at `PermitLimit = 10` per minute,
the burst is rejected with 429. The user then clicks again → more 429s.

Additionally `SohbaApp.post` treats the 429 body as "Non-JSON response" and logs it loudly,
making it look like an app crash.

## Execution Flow

```
Click Accept
    → friends.js acceptRequest(userId)
        → SohbaApp.post('/Friends/AcceptRequest', { senderId: userId })      (click 1)
            → FriendshipService.AcceptFriendRequestAsync(senderId, currentUserId)
                → HasPendingRequestAsync(sender, current) → true
                → GetByUsersAsync(sender, current) → forward match → row found → accepted
        → success → card removed
    → (double-click / second invocation)
        → click 2 → HasPendingRequestAsync → false → "No pending friend request found."
        → click 3 ... → 429 because limit exhausted by clicks 1-3 + other FriendRequest calls
```

## Related Files

- `Sohba.Infrastructure/Repositories/FriendshipRepository.cs`
- `Sohba.Application/Services/FriendshipService.cs`
- `Sohba/wwwroot/js/features/friends.js`
- `Sohba/Controllers/FriendsController.cs`
- `Sohba/Program.cs` (rate limiter config)

## Affected Components

- Repository — `FriendshipRepository.cs`
- Application Service — `FriendshipService.cs`
- JavaScript — `friends.js`
- Rate limiting — `Program.cs`

## Files That Need Modification

1. `Sohba.Infrastructure/Repositories/FriendshipRepository.cs`
2. `Sohba/wwwroot/js/features/friends.js`
3. `Sohba/Program.cs` (raise/divide the friend-request limit; optional)

## Implementation Plan

1. **Fix `GetByUsersAsync`** to return the reversed row when found:

   ```csharp
   if (friendship == null)
   {
       friendship = await _context.Friends
           .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendUserId == userId);
   }
   return friendship;
   ```

2. **Fix `HasPendingRequestAsync`** to return `exists || reversedExists`.

3. **Fix `friends.js`**: disable the Accept/Decline/Cancel buttons while in flight
   (prevent double-submit).

4. **Optionally adjust rate limits** in `Program.cs`:
   - Raise `FriendRequest` from 10/min to 30/min, or
   - Use the `Api` policy (60/min) for these endpoints, or
   - Add `QueueLimit = 2` so bursts queue instead of reject.

5. **Improve SohbaApp.post** so HTTP 429 returns a friendly message
   ("Too many requests. Please wait a moment and try again.") instead of a generic
   "Non-JSON response" log.

## Code Changes

<div style="color:red"><b>REMOVE — buggy GetByUsersAsync in FriendshipRepository.cs:</b></div>

```csharp
        public async Task<Friend?> GetByUsersAsync(Guid userId, Guid friendId)
        {

            var friendship = await _context.Friends
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendUserId == friendId);


            if (friendship == null)
            {
                var reversed = await _context.Friends
                    .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendUserId == userId);
            
            }

            return friendship;
        }
```

<div style="color:green"><b>REPLACE WITH — direction-agnostic lookup:</b></div>

```csharp
        public async Task<Friend?> GetByUsersAsync(Guid userId, Guid friendId)
        {
            var friendship = await _context.Friends
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendUserId == friendId);

            if (friendship == null)
            {
                friendship = await _context.Friends
                    .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendUserId == userId);
            }

            return friendship;
        }
```

<div style="color:red"><b>REMOVE — buggy HasPendingRequestAsync in FriendshipRepository.cs:</b></div>

```csharp
        public async Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId)
        {

            var exists = await _context.Friends
                .AnyAsync(f => f.UserId == senderId &&
                               f.FriendUserId == receiverId &&
                               f.Status == FriendshipStatus.Pending);
            if (!exists)
            {
                var reversedExists = await _context.Friends
                    .AnyAsync(f => f.UserId == receiverId &&
                                   f.FriendUserId == senderId &&
                                   f.Status == FriendshipStatus.Pending); 
            }

            return exists;
        }
```

<div style="color:green"><b>REPLACE WITH — return both directions:</b></div>

```csharp
        public async Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId)
        {
            var exists = await _context.Friends
                .AnyAsync(f => f.UserId == senderId &&
                               f.FriendUserId == receiverId &&
                               f.Status == FriendshipStatus.Pending);

            if (!exists)
            {
                exists = await _context.Friends
                    .AnyAsync(f => f.UserId == receiverId &&
                                   f.FriendUserId == senderId &&
                                   f.Status == FriendshipStatus.Pending);
            }

            return exists;
        }
```

<div style="color:red"><b>REMOVE — non-guarded accept/reject in friends.js:</b></div>

```javascript
async function acceptRequest(userId) {
    const result = await SohbaApp.post('/Friends/AcceptRequest', { senderId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request accepted!', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();
        ...
    } else {
        SohbaApp.toast(result.error || 'Failed to accept request', 'error');
    }
}
```

<div style="color:green"><b>ADD — in-flight guard + disable button:</b></div>

```javascript
async function acceptRequest(userId, btn) {
    if (btn) { btn.disabled = true; }

    const result = await SohbaApp.post('/Friends/AcceptRequest', { senderId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request accepted!', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();
    } else {
        if (btn) { btn.disabled = false; }
        SohbaApp.toast(result.error || 'Failed to accept request', 'error');
    }
}

async function rejectRequest(userId, btn) {
    if (btn) { btn.disabled = true; }

    const result = await SohbaApp.post('/Friends/RejectRequest', { requesterId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request declined', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();
    } else {
        if (btn) { btn.disabled = false; }
        SohbaApp.toast(result.error || 'Failed to decline request', 'error');
    }
}
```

`Requests.cshtml` buttons must pass `this`:

```html
<button onclick="acceptRequest('@request.UserId', this)" ...>Accept</button>
<button onclick="rejectRequest('@request.UserId', this)" ...>Decline</button>
```

<div style="color:green"><b>ADD — friendlier 429 handling in sohba-core.js post():</b></div>

```javascript
        const contentType = response.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            const statusLabel = response.status === 401 || response.status === 302
                ? 'Session expired. Please refresh and log in again.'
                : response.status === 429
                    ? 'Too many requests. Please wait a moment and try again.'
                    : `Server error (HTTP ${response.status}). Please try again.`;
            console.error(`[SohbaApp.post] Non-JSON response from ${url}:`, response.status, contentType);
            return { success: false, Success: false, error: statusLabel, Error: statusLabel };
        }
```

## Regression Testing

- **Test Users:**
    - `khaled@sohba.com` (sent request to Mohammed).
    - `mohammed@sohba.com` (receiver).
- **Navigation:** Login as Mohammed → `/Friends/Requests`.
- **Expected Results:**
    - Clicking Accept once → 200 JSON `{ success:true }`, card removed, the sender becomes a friend.
    - Clicking Decline once → 200, card removed.
    - No 429 appears.
- **Failure Conditions:** double-clicking should be blocked by the disabled button.
- **Edge Cases:** reverse-direction stored requests (seeder duplicates) no longer produce
  "No pending friend request found".

<br>
<br>

---

<br>

# Issue 7.6 — Profile Page: checkFriendshipStatus & blockUserFromProfile Undefined

## Issue

On another user's profile page:

```
Uncaught ReferenceError: checkFriendshipStatus is not defined
    at HTMLDocument.<anonymous> (profileId:666:17)
Uncaught ReferenceError: blockUserFromProfile is not defined
    at HTMLButtonElement.onclick (profileId:550:162)
```

Also `tailwind.css` fails to load with 404, and the `Add Friend` button shows even when the
two users are ALREADY friends.

## Related Feature

- **Feature Name:** Profile — view another user + friend actions.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 7.6.

## Expected Behaviour

- Profile page of another user:
    - Shows "Friends" or "Pending" instead of "Add Friend" if the relationship exists.
    - Block button works.
- No reference errors.

## Current Behaviour

- `checkFriendshipStatus` and `blockUserFromProfile` are not defined on the profile page.
- Add Friend button always shows for non-own profiles.
- Block button throws ReferenceError.
- `tailwind.css` 404.

## Root Cause

1. **`friends.js` is not loaded on Profile pages.**
   - `Sohba/Views/Profile/Index.cshtml` has no `@section Scripts` loading `friends.js`
     (its script section only has the `checkFriendshipStatus` call in a raw inline `<script>`).
   - `_AppLayout.cshtml` does NOT include `features/friends.js`.
   - The functions that Profile needs (`sendFriendRequestFromProfile`,
     `checkFriendshipStatus`, `blockUserFromProfile`, `unblockUserFromProfile`)
     are all defined in `friends.js`.

2. **`checkFriendshipStatus` uses the wrong HTTP verb.**
   - `FriendsController.CheckStatus` is `[HttpGet]`.
   - `friends.js` `checkFriendshipStatus` calls `SohbaApp.post('/Friends/CheckStatus', ...)`
     → would 405 even if loaded.

3. **Profile/Index.cshtml's inline script calls `checkFriendshipStatus('@Model.Profile.Id')`
   on DOMContentLoaded** — the function is undefined at that point.

4. **`tailwind.css` 404** — `_AppLayout.cshtml`:
   ```html
   <link rel="stylesheet" href="~/css/tailwind.css" />
   ```
   But `wwwroot/css/` contains `input.css`, `landing.css`, `legacy.css`, `site.css`,
   `v0-custom.css` — **no `tailwind.css`**.

5. **Add-Friend-already-friends** — even after loading friends.js, the button state is only
   set if `checkFriendshipStatus` runs; it currently never does because the file isn't loaded
   and the verb is wrong.

## Execution Flow

```
Open /Profile/Index/{anotherUserId}
    → shared _AppLayout loads scripts (no friends.js)
    → inline <script> in Profile/Index fires DOMContentLoaded
        → checkFriendshipStatus(...) → ReferenceError (not defined)
    → render Add Friend button (default state)
    → click "Block" → onclick="blockUserFromProfile('...')" → ReferenceError
```

## Related Files

- `Sohba/Views/Profile/Index.cshtml`
- `Sohba/wwwroot/js/features/friends.js`
- `Sohba/Views/Shared/_AppLayout.cshtml`
- `Sohba/Controllers/FriendsController.cs` (`CheckStatus`)
- `Sohba/wwwroot/css/*` (no tailwind.css)

## Affected Components

- View — `Profile/Index.cshtml`
- JavaScript — `friends.js` loading
- View — `_AppLayout.cshtml` (CSS link + script loading)
- CSS — missing `tailwind.css`

## Files That Need Modification

1. `Sohba/Views/Shared/_AppLayout.cshtml`
2. `Sohba/wwwroot/js/features/friends.js`
3. `Sohba/Views/Profile/Index.cshtml`
4. `Sohba/Views/Shared/Partials/_Header.cshtml` (CSS link — fix or remove `tailwind.css`)

## Implementation Plan

1. **Load `friends.js` globally** in `_AppLayout.cshtml`.
2. **Fix `checkFriendshipStatus`** to use `fetch` GET (`/Friends/CheckStatus?userId=...`).
3. **Remove the inline duplicated script** from `Profile/Index.cshtml` — let the globally
   loaded `friends.js` handle initialization. Add an init call guarded:
   ```javascript
   document.addEventListener('DOMContentLoaded', function () {
       const id = document.body.dataset.profileUserId;
       if (id) window.checkFriendshipStatus && checkFriendshipStatus(id);
   });
   ```
   (Or call it from the view's script section; simplest is to keep a small script that only
   calls the loaded function.)
4. **Fix `tailwind.css`**: either delete the link (Tailwind is loaded from the CDN script) or
   point to `v0-custom.css` if that's the intended Tailwind build output. The CDN script
   `<script src="https://cdn.tailwindcss.com"></script>` is already present, so removing the
   dead link is acceptable.
5. **Ensure `addFriendBtn`** is updated based on `CheckStatus`:
   - accepted → "Friends" (disabled, green)
   - pending → "Pending" (disabled, yellow)
   - none → "Add Friend"

6. **Make every user name/avatar clickable** to their profile (Feature request):
   - Wrap profile avatars/names in post cards, comments, sidebar suggestions, and friend cards
     with `<a href="/Profile/Index/{id}">`.

## Code Changes

<div style="color:green"><b>ADD — in _AppLayout.cshtml (script section):</b></div>

```html
    <script src="~/js/features/friends.js" asp-append-version="true"></script>
```

<div style="color:red"><b>REMOVE — dead CSS link in _AppLayout.cshtml:</b></div>

```html
    <link rel="stylesheet" href="~/css/tailwind.css" />
```

<div style="color:red"><b>REMOVE — incorrect post-based checkFriendshipStatus in friends.js:</b></div>

```javascript
window.checkFriendshipStatus = async function (targetUserId) {
    try {
        const result = await SohbaApp.post('/Friends/CheckStatus', { userId: targetUserId });
        ...
```

<div style="color:green"><b>REPLACE WITH — fetch GET based status check:</b></div>

```javascript
window.checkFriendshipStatus = async function (targetUserId) {
    try {
        const response = await fetch(`/Friends/CheckStatus?userId=${targetUserId}`);
        const result = await response.json();
        const data = result.data ?? result.Data;

        const btn = document.getElementById('addFriendBtn');
        if (!btn) return;

        if (data === 'pending') {
            btn.innerHTML = `
                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Pending</span>
            `;
            btn.disabled = true;
            btn.classList.remove('bg-[#345e69]', 'hover:bg-[#2a4b55]');
            btn.classList.add('bg-yellow-600', 'hover:bg-yellow-700', 'cursor-not-allowed');
        } else if (data === 'accepted') {
            btn.innerHTML = `
                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
                <span>Friends</span>
            `;
            btn.disabled = true;
            btn.classList.remove('bg-[#345e69]', 'hover:bg-[#2a4b55]');
            btn.classList.add('bg-green-600', 'hover:bg-green-700', 'cursor-not-allowed');
        }
    } catch (error) {
        console.error('Error checking friendship status:', error);
    }
};
```

<div style="color:red"><b>REMOVE — the commented-out duplicate script in Profile/Index.cshtml (lines 151-220):</b></div>

```html
@* @section Scripts {
    <script>
        async function sendFriendRequest(userId) { ... }
        async function checkFriendshipStatus(targetUserId) { ... }
        ...
    </script>
} *@
```

<div style="color:green"><b>ADD — replace the existing Scripts section in Profile/Index.cshtml with an init-only script:</b></div>

```html
@section Scripts {
    <script>
        document.addEventListener('DOMContentLoaded', function () {
            const profileUserId = '@Model.Profile.Id';
            if (!@Json.Serialize(Model.IsOwnProfile) && typeof window.checkFriendshipStatus === 'function') {
                window.checkFriendshipStatus(profileUserId);
            }
        });
    </script>
}
```

## Regression Testing

- **Test Users:**
    - `mohammed@sohba.com` and `ahmed@sohba.com` (already friends — seed data).
    - `omar@sohba.com` (not a friend).
- **Navigation:**
    - Login as Mohammed → open `/Profile/Index/{Ahmed's id}`.
        - Button should read "Friends" (disabled).
    - Login as Omar → open `/Profile/Index/{Mohammed's id}`.
        - Button should read "Add Friend". Click → "Request Sent" → becomes "Pending".
    - Refresh as a sender: button shows "Pending".
    - Click Block → Confirm modal → user blocked, page reloads with "Unblock".
- **Expected Results:**
    - No ReferenceError anywhere.
    - No `tailwind.css` 404 in the Network tab.
- **Failure Conditions:** if button shows "Add Friend" while already friends, check that
  `checkFriendshipStatus` ran (network GET `/Friends/CheckStatus` 200).

<br>
<br>

---

<br>

# Additional Profile Request — Add-Friend Shows Even When Already Friends

## Issue

Two users are already friends, but viewing one's profile shows "Add Friend".

## Related Feature

- **Feature Name:** Profile — Friend status button.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 7.6.

## Expected Behaviour

If already friends, the button reads "Friends" and is disabled.

## Current Behaviour

The "Add Friend" button always renders for non-own profiles (the server does not evaluate the
friendship state for the button), and the client-side status check never runs (covered in
Issue 7.6).

## Root Cause

`Profile/Index.cshtml` renders the button unconditionally for `!Model.IsOwnProfile` with no
server-side state, and `checkFriendshipStatus` is broken / unloaded (Issue 7.6).

## Execution Flow

```
ProfileController.Index → ProfileViewModel (no IsFriend flag on view model)
    → view renders Add Friend unconditionally
    → client-side checkFriendshipStatus never runs (broken)
```

## Related Files

- `Sohba/Views/Profile/Index.cshtml`
- `Sohba/ViewModels/Profile/ProfileViewModel.cs`
- `Sohba/Controllers/ProfileController.cs`
- `Sohba/wwwroot/js/features/friends.js`

## Affected Components

- View — `Profile/Index.cshtml`
- ViewModel — `ProfileViewModel`
- JavaScript — `friends.js`

## Files That Need Modification

1. `Sohba/ViewModels/Profile/ProfileViewModel.cs`
2. `Sohba/Controllers/ProfileController.cs`
3. `Sohba/Views/Profile/Index.cshtml`

## Implementation Plan

1. Add `IsFriend` / `FriendshipStatus` to `ProfileViewModel`.
2. In `ProfileController.Index`, populate it:
   ```csharp
   var friendshipStatus = "none";
   if (isFriend) friendshipStatus = "accepted";
   else if (await _friendshipService.HasPendingRequestAsync(profileUserId, currentUserId)
        || await _friendshipService.HasPendingRequestAsync(currentUserId, profileUserId))
       friendshipStatus = "pending";
   viewModel.FriendshipStatus = friendshipStatus;
   ```
3. In the view, set the initial button state from `Model.FriendshipStatus`, and let
   `checkFriendshipStatus` refine it client-side (for freshness).
4. Also render the initial state server-side so it works even without JS.

## Code Changes

<div style="color:green"><b>ADD — to ProfileViewModel:</b></div>

```csharp
        public string FriendshipStatus { get; set; } = "none"; // "none" | "pending" | "accepted"
```

<div style="color:green"><b>ADD — in ProfileController.Index (after computing isFriend):</b></div>

```csharp
            var friendshipStatus = "none";
            if (isFriend)
            {
                friendshipStatus = "accepted";
            }
            else
            {
                var senderPending = await _friendshipService.HasPendingRequestAsync(profileUserId, currentUserId);
                var receiverPending = await _friendshipService.HasPendingRequestAsync(currentUserId, profileUserId);
                if (senderPending || receiverPending)
                {
                    friendshipStatus = "pending";
                }
            }

            var isBlocked = currentUserId != profileUserId &&
                    await _friendshipService.IsBlockedAsync(currentUserId, profileUserId);

            var viewModel = new ProfileViewModel
            {
                Profile = profileResult.Value,
                Friends = friendsResult.Value ?? new List<FriendDto>(),
                Posts = postsResult.Value ?? new List<PostResponseDto>(),
                IsOwnProfile = profileUserId == currentUserId,
                CanViewFriends = canViewFriends,
                IsBlocked = isBlocked,
                FriendshipStatus = friendshipStatus
            };
```

<div style="color:green"><b>ADD — initial server-side button state in Profile/Index.cshtml (wrap the Add Friend button):</b></div>

```html
                    @if (Model.FriendshipStatus == "accepted")
                    {
                        <button class="px-6 py-2.5 bg-green-600 text-white font-bold rounded-xl shadow-lg flex items-center gap-2 cursor-not-allowed" disabled>
                            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                            </svg>
                            <span>Friends</span>
                        </button>
                    }
                    else if (Model.FriendshipStatus == "pending")
                    {
                        <button class="px-6 py-2.5 bg-yellow-600 text-white font-bold rounded-xl shadow-lg flex items-center gap-2 cursor-not-allowed" disabled>
                            <span>Pending</span>
                        </button>
                    }
                    else
                    {
                        <button onclick="sendFriendRequestFromProfile('@Model.Profile.Id')"
                                class="px-6 py-2.5 bg-[#345e69] text-white font-bold rounded-xl hover:bg-[#2a4b55] transition-colors shadow-lg flex items-center gap-2"
                                id="addFriendBtn">
                            <span>Add Friend</span>
                        </button>
                    }
```

## Regression Testing

- **Test Users:** Mohammed + Ahmed (friends), Mohammed + Khaled (pending), Mohammed + Omar (none).
- **Navigation:** View each relationship's profile.
- **Expected Results:**
    - Friends → button "Friends" disabled.
    - Pending (as receiver/sender) → "Pending" disabled.
    - None → "Add Friend" clickable.
- **Failure Conditions:** the client-side `checkFriendshipStatus` must agree with the
  server-side state after page load.
- **Edge Cases:** profile of self → no friend button (own profile shows Edit Profile).

<br>
<br>

---

<br>

# Issue 8.1 — Profile Edit Page Missing

## Issue

The Edit Profile link on the profile page navigates to `/Profile/Edit`, which throws
"page not found".

## Related Feature

- **Feature Name:** Profile — Edit Profile.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 8.1.

## Expected Behaviour

Clicking "Edit Profile" opens a form to edit Name, Bio, and Profile Picture; saving updates
the profile and redirects to the profile page.

## Current Behaviour

404 — the view does not exist.

## Root Cause

`ProfileController` HAS the actions:

- `GET Profile/Edit` → returns `View(viewModel)` (EditProfileViewModel)
- `POST Profile/Edit` → handles the update

But `Sohba/Views/Profile/Edit.cshtml` **does not exist** in the project
(the Profile views folder contains only `Index.cshtml`, `PrivateProfile.cshtml`, `Settings.cshtml`).

I searched the project and this implementation does not exist.

## Execution Flow

```
Click "Edit Profile" → GET /Profile/Edit
    → ProfileController.Edit builds EditProfileViewModel
    → return View(viewModel)
        → runtime looks for Views/Profile/Edit.cshtml → NOT FOUND → 404
```

## Related Files

- `Sohba/Controllers/ProfileController.cs`
- `Sohba/ViewModels/Profile/EditProfileViewModel.cs` (exists — used by controller)
- `Sohba/Views/Profile/Index.cshtml` (link to Edit)
- `Sohba/Views/Profile/Settings.cshtml` (sibling view for style reference)

## Affected Components

- View — missing `Edit.cshtml`
- Controller — actions exist

## Files That Need Modification

1. `Sohba/Views/Profile/Edit.cshtml` (NEW)

## Implementation Plan

1. Create `Sohba/Views/Profile/Edit.cshtml` bound to `EditProfileViewModel`.
2. Form fields: Name, Bio, Profile Picture URL (or file upload via `IFileStorageService` —
   see implementation note), displayed using the project's Tailwind styling pattern from
   `Pages/Create.cshtml` / `Settings.cshtml`.
3. Since the POST action currently accepts `EditProfileViewModel` with `ProfilePictureUrl`
   string, the minimal fix is a URL text input. For a proper upload, add an
   `IFormFile ProfileImageFile` to the view model and store it via `IFileStorageService`.
4. Show the current profile picture with a preview.
5. On success the controller already redirects to `Profile.Index`.

> Note: `ProfileController.Edit` GET maps `result.Value.Name`, `Bio`, `ProfilePictureUrl`.
> If you add an upload field, update the view model and POST action.

## Code Changes

<div style="color:green"><b>ADD — Sohba/Views/Profile/Edit.cshtml:</b></div>

```html
@model Sohba.ViewModels.Profile.EditProfileViewModel
@{
    ViewData["Title"] = "Edit Profile";
    Layout = "_AppLayout";
}

<div class="max-w-5xl mx-auto page-transition">
    <div class="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden hover-lift">
        <div class="bg-gradient-to-br from-[#345e69] via-[#407380] to-[#4a8291] p-8 text-white">
            <h1 class="text-3xl font-black tracking-tight">Edit Profile</h1>
            <p class="text-white/80 mt-1">Update your public profile information.</p>
        </div>

        <form asp-action="Edit" method="post" enctype="multipart/form-data" class="p-6 space-y-6">
            @Html.AntiForgeryToken()
            <div asp-validation-summary="ModelOnly" class="text-red-500 text-sm"></div>

            <div>
                <label class="block text-sm font-bold text-gray-700 mb-2">Name</label>
                <input asp-for="Name"
                       class="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69] transition-all"
                       placeholder="Your name..." />
            </div>

            <div>
                <label class="block text-sm font-bold text-gray-700 mb-2">Bio</label>
                <textarea asp-for="Bio" rows="4"
                          class="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69] transition-all resize-none"
                          placeholder="Tell people about yourself..."></textarea>
            </div>

            <div>
                <label class="block text-sm font-bold text-gray-700 mb-2">Profile Picture</label>
                @if (!string.IsNullOrEmpty(Model.ProfilePictureUrl))
                {
                    <img src="@Model.ProfilePictureUrl" class="w-24 h-24 rounded-full object-cover border border-slate-200 mb-3" id="profilePreview" />
                }
                else
                {
                    <img src="https://ui-avatars.com/api/?name=User&background=345e69&color=fff"
                         class="w-24 h-24 rounded-full object-cover border border-slate-200 mb-3" id="profilePreview" />
                }
                <input asp-for="ProfileImageFile" type="file" accept="image/*"
                       class="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#345e69]/20 focus:border-[#345e69] transition-all file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-[#345e69]/10 file:text-[#345e69] hover:file:bg-[#345e69]/20 cursor-pointer" />
                <p class="text-xs text-gray-500 mt-2">Optional. PNG, JPG up to 5MB.</p>
            </div>

            <div class="flex gap-3 pt-4 border-t border-slate-100">
                <a asp-action="Index" class="flex-1 py-2.5 border border-gray-200 text-gray-600 font-semibold rounded-xl hover:bg-gray-50 transition-colors text-center">
                    Cancel
                </a>
                <button type="submit" class="flex-1 py-2.5 bg-[#345e69] hover:bg-[#2a4b55] text-white font-semibold rounded-xl shadow-lg shadow-[#345e69]/30 transition-all">
                    Save Changes
                </button>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    <script>
        document.getElementById('ProfileImageFile')?.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (!file) return;
            const reader = new FileReader();
            reader.onload = function (ev) {
                const preview = document.getElementById('profilePreview');
                if (preview) preview.src = ev.target.result;
            };
            reader.readAsDataURL(file);
        });
    </script>
}
```

<div style="color:green"><b>ADD — to EditProfileViewModel (for the file upload):</b></div>

```csharp
        public IFormFile? ProfileImageFile { get; set; }
```

<div style="color:green"><b>UPDATE — ProfileController.Edit POST to persist the uploaded image:</b></div>

```csharp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            var dto = new UserRequestDto
            {
                Name = model.Name,
                Bio = model.Bio,
                ProfilePictureUrl = model.ProfilePictureUrl
            };

            // Persist any new uploaded image through IFileStorageService
            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveFileAsync(model.ProfileImageFile, "profiles");
                if (uploadResult.IsSuccess)
                    dto.ProfilePictureUrl = uploadResult.Value;
                else
                    ModelState.AddModelError("ProfileImageFile", uploadResult.Error);
            }

            var result = await _userService.UpdateProfileAsync(userId, dto);

            if (result.IsSuccess)
                return RedirectToAction("Index");

            ModelState.AddModelError("", result.Error);
            return View(model);
        }
```

(Inject `IFileStorageService _fileStorage` into `ProfileController`'s constructor.)

## Regression Testing

- **Test Users:** `mohammed@sohba.com`.
- **Navigation:** Own profile → Edit Profile.
- **Expected Results:**
    - Form loads with current name/bio/picture.
    - Uploading an image shows a preview; saving updates the header avatar.
    - Saving redirects to the profile with updated info.
- **Failure Conditions:** empty name → client/model validation error.
- **Edge Cases:** >5MB image rejected; no new image (keep old URL).

<br>
<br>

---

<br>

# Issue 8.2 — Settings Page: Save / Danger Zone Not Functional + UI

## Issue

On `/Profile/Settings`:

- Save Changes (with the notification checkboxes) does not actually bind the
  notification settings (they are hard-coded `checked`).
- Deactivate Account button does nothing.
- Delete Account button does nothing.
- The page needs UI/UX polish.

## Related Feature

- **Feature Name:** Profile — Account Settings.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 8.2.

## Expected Behaviour

- Toggle switches bind to the model (`EmailNotifications`, `PushNotifications`,
  `WeeklyDigest`, `IsPrivateAccount`, `ShowActivityStatus`).
- Save Changes persists them and shows a success message.
- Deactivate Account performs a soft deactivation (domain rule).
- Delete Account permanently deletes user data (with confirmation).
- Page is visually consistent.

## Current Behaviour

- The Notification section uses plain checkboxes with `checked` hard-coded — they are NOT
  bound to the view model, so saving never persists them.
- Deactivate / Delete buttons have no `onclick` handlers and no backend endpoints.
- The Settings controller action exists but only handles the small model; deactivate/delete
  are unimplemented.

## Root Cause

`Sohba/Views/Profile/Settings.cshtml`:

- Lines 98-109: checkboxes are hard-coded:

```html
<input type="checkbox" checked class="w-5 h-5 ..." />
```

instead of `asp-for="EmailNotifications"` etc.

- Lines 137-149: Deactivate / Delete buttons are plain `<button>` with no handler.
- `ProfileController` has no `Deactivate` / `Delete` actions.
- `IUserService` / `UserService` may not expose `DeactivateAccountAsync` / `DeleteAccountAsync`
  (I inspected: `DeleteUserAsync` exists on `IUserService` used by Dashboard).
- The `SettingsViewModel` DOES have `EmailNotifications`, `PushNotifications`, `WeeklyDigest`
  properties, so binding is easy.

## Execution Flow

```
User opens /Profile/Settings
    → checkboxes default checked regardless of actual settings  (hard-coded)
    → toggles only fire a local toast "Setting saved" (fake)
    → Save Changes posts the whole form
        → ProfileController.Settings POST
            → binds only those fields with asp-for; notification fields never post
    → click Deactivate → no handler → nothing
    → click Delete → no handler → nothing
```

## Related Files

- `Sohba/Views/Profile/Settings.cshtml`
- `Sohba/Controllers/ProfileController.cs`
- `Sohba/ViewModels/Profile/SettingsViewModel.cs`
- `Sohba.Application/Interfaces/IUserSettingsService.cs`
- `Sohba.Application/Services/UserSettingsService.cs`
- `Sohba.Application/Interfaces/IUserService.cs`
- `Sohba.Application/Services/UserService.cs`

## Affected Components

- View — `Settings.cshtml`
- Controller — `ProfileController`
- Application Service — `UserSettingsService`, `UserService`

## Files That Need Modification

1. `Sohba/Views/Profile/Settings.cshtml`
2. `Sohba/Controllers/ProfileController.cs`
3. `Sohba.Application/Interfaces/IUserService.cs`
4. `Sohba.Application/Services/UserService.cs`

## Implementation Plan

1. **Bind notification checkboxes to the model:**
   - `asp-for="EmailNotifications"`
   - `asp-for="PushNotifications"`
   - `asp-for="WeeklyDigest"`
   - The privacy toggles already use `asp-for="IsPrivateAccount"` and
     `asp-for="ShowActivityStatus"` — keep them.
2. **Remove the fake auto-save toast** in the Scripts section (or keep only informational).
3. **Implement Deactivate**:
   - Add `Task<Result> DeactivateAccountAsync(Guid userId)` to `IUserService` and implement it
     by soft-deactivating the user (e.g., set a flag on the `User` entity or use
     `IsActive = false`; the entity currently lacks such a field — add one via migration, or
     reuse an existing `IsBlocked`-like concept in a domain-safe way).
   - Wire the button with a confirm modal → POST `/Profile/Deactivate` → sign out → login page.
4. **Implement Delete**:
   - Add `Task<Result> DeleteMyAccountAsync(Guid userId)` that enforces ownership and deletes
     user + related rows per domain rules (posts, comments, friendships, collections...).
   - Wire the button with a strong-confirm modal → POST `/Profile/DeleteAccount` → sign out.
5. **Polish UI/UX**:
   - Group the toggles with descriptions; show success toast after save (redirect already sets
     `TempData["SuccessMessage"]` — display it).
   - Add danger-zone modals (reuse the global confirm modal).

## Code Changes

<div style="color:red"><b>REMOVE — the hard-coded notification checkboxes in Settings.cshtml:</b></div>

```html
                    <label class="flex items-center gap-3 cursor-pointer">
                        <input type="checkbox" checked class="w-5 h-5 rounded border-gray-300 text-[#345e69] focus:ring-[#345e69]" />
                        <span class="text-gray-700">Email notifications for new followers</span>
                    </label>
                    <label class="flex items-center gap-3 cursor-pointer">
                        <input type="checkbox" checked class="w-5 h-5 rounded border-gray-300 text-[#345e69] focus:ring-[#345e69]" />
                        <span class="text-gray-700">Push notifications for messages</span>
                    </label>
                    <label class="flex items-center gap-3 cursor-pointer">
                        <input type="checkbox" class="w-5 h-5 rounded border-gray-300 text-[#345e69] focus:ring-[#345e69]" />
                        <span class="text-gray-700">Weekly digest email</span>
                    </label>
```

<div style="color:green"><b>ADD — model-bound checkboxes:</b></div>

```html
                    <label class="flex items-center gap-3 cursor-pointer">
                        <input asp-for="EmailNotifications" class="w-5 h-5 rounded border-gray-300 text-[#345e69] focus:ring-[#345e69]" />
                        <span class="text-gray-700">Email notifications for new followers</span>
                    </label>
                    <label class="flex items-center gap-3 cursor-pointer">
                        <input asp-for="PushNotifications" class="w-5 h-5 rounded border-gray-300 text-[#345e69] focus:ring-[#345e69]" />
                        <span class="text-gray-700">Push notifications for messages</span>
                    </label>
                    <label class="flex items-center gap-3 cursor-pointer">
                        <input asp-for="WeeklyDigest" class="w-5 h-5 rounded border-gray-300 text-[#345e69] focus:ring-[#345e69]" />
                        <span class="text-gray-700">Weekly digest email</span>
                    </label>
```

<div style="color:red"><b>REMOVE — the fake auto-save toast script:</b></div>

```html
@section Scripts {
    <script>
        // Auto-save toggle switches
        document.querySelectorAll('input[type="checkbox"]').forEach(toggle => {
            toggle.addEventListener('change', function() {
                SohbaApp.toast('Setting saved', 'success');
            });
        });
    </script>
}
```

<div style="color:green"><b>ADD — show success message + wire danger zone:</b></div>

```html
@if (TempData["SuccessMessage"] != null)
{
    <div class="bg-green-50 border border-green-200 text-green-700 px-5 py-4 rounded-2xl mb-6">
        @TempData["SuccessMessage"]
    </div>
}

@section Scripts {
    <script>
        window.deactivateAccount = function () {
            showConfirmModal({
                title: 'Deactivate Account',
                message: 'Are you sure you want to temporarily disable your account? You can reactivate by logging in again.',
                type: 'warning',
                confirmText: 'Deactivate',
                onConfirm: async function () {
                    const result = await SohbaApp.post('/Profile/Deactivate', {});
                    if (result.success) {
                        SohbaApp.toast('Account deactivated.', 'success');
                        setTimeout(() => window.location.href = '/Auth/Logout', 800);
                    } else {
                        SohbaApp.toast(result.error || 'Failed to deactivate account', 'error');
                    }
                }
            });
        };

        window.deleteAccount = function () {
            showConfirmModal({
                title: 'Delete Account',
                message: 'This will permanently delete all your data. This cannot be undone. Are you absolutely sure?',
                type: 'delete',
                confirmText: 'Delete Forever',
                onConfirm: async function () {
                    const result = await SohbaApp.post('/Profile/DeleteAccount', {});
                    if (result.success) {
                        SohbaApp.toast('Account deleted.', 'success');
                        setTimeout(() => window.location.href = '/Auth/Logout', 800);
                    } else {
                        SohbaApp.toast(result.error || 'Failed to delete account', 'error');
                    }
                }
            });
        };
    </script>
}
```

Update the Danger Zone buttons:

```html
                    <button onclick="deactivateAccount()" class="px-4 py-2 border border-red-300 text-red-600 font-semibold rounded-xl hover:bg-red-100 transition-colors">
                        Deactivate
                    </button>
                    ...
                    <button onclick="deleteAccount()" class="px-4 py-2 bg-red-600 text-white font-semibold rounded-xl hover:bg-red-700 transition-colors">
                        Delete
                    </button>
```

<div style="color:green"><b>ADD — new ProfileController actions:</b></div>

```csharp
        [HttpPost]
        public async Task<IActionResult> Deactivate()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.DeactivateAccountAsync(userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.DeleteMyAccountAsync(userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }
```

<div style="color:green"><b>ADD — to IUserService / UserService:</b></div>

```csharp
        Task<Result> DeactivateAccountAsync(Guid userId);
        Task<Result> DeleteMyAccountAsync(Guid userId);
```

```csharp
        public async Task<Result> DeactivateAccountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.");

            // Domain rule: mark the account as deactivated (soft disable)
            user.IsActive = false;      // requires adding IsActive to User entity + migration
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<Result> DeleteMyAccountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.");

            // Delete related data (posts, comments, friendships, saved collections) per domain rules
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // NOTE: implement the cascade deletion carefully in the repository
                // (delete friendships, group memberships, page admin rows, posts, comments, saved)
                _unitOfWork.Users.Delete(user);
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();
                return Result.Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
```

## Regression Testing

- **Test Users:** `nour@sohba.com` (has notifications flags in DB).
- **Navigation:** `/Profile/Settings`.
- **Expected Results:**
    - Checkboxes reflect the DB values.
    - Toggling + Save → success message; refreshing keeps the toggled state.
    - Deactivate → confirm modal → user deactivated and logged out.
    - Delete → confirm modal → account deleted and logged out; re-login with that email
      fails (optional, user-specific decision).
- **Failure Conditions:** Save must NOT reset unrelated fields (`IsPrivateAccount`,
  `ShowActivityStatus` were already bound — keep them).
- **Edge Cases:** deleting an account that is admin of a page/group — must handle ownership
  transfer or reject deletion with a clear message.

<br>
<br>

---

<br>

# Sidebar Duplication + _RightSidebar Suggestions Broken

## Issue

- `_Sidebar.cshtml` shows duplicated "Pages For You" / "Groups To Join" entries.
- `_RightSidebar.cshtml` "People You May Know" shows "Could not load suggestions".

## Related Feature

- **Feature Name:** Sidebars — Left navigation + Right suggestions.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → generic layout sections.

## Expected Behaviour

- Each page / group appears once in the sidebar.
- Right sidebar shows 5 friend suggestions.

## Current Behaviour

- Duplicated pages/groups (different IDs but same name) appear.
- Right sidebar shows "Could not load suggestions".

## Root Cause

**Left sidebar duplication** — two separate causes:

1. **DB duplicates** (Issue 5.1 root cause): the seeder creates a NEW "Sohba Tech" page and
   NEW "Sohba Developers" group on every app restart. The sidebar's JS fetches
   `/Pages/Discover` and `/Groups/Discover` — each returns all the duplicate rows, so the
   sidebar shows N copies of the same name.

2. **`_Sidebar.cshtml` `refreshSidebarSections()`** — defined (lines 348-385) and ALSO the
   `joinGroupFromSidebar` callback re-fetches `loadGroupsToJoin()` after removing a card.
   This alone replaces the container's innerHTML (not append), so it should not duplicate —
   the primary cause is the DB duplicates.

**Right sidebar suggestions broken** — `sidebar.js` uses `SohbaApp.post` against a `[HttpGet]`
endpoint → 405 (Issue "Console Error — POST Friends/GetFriendSuggestions 405").

## Execution Flow

```
Left sidebar DOMContentLoaded
    → fetch /Pages/Discover      → returns duplicates (seeder bug)
    → fetch /Groups/Discover     → returns duplicates (seeder bug)

Right sidebar DOMContentLoaded
    → sidebar.js loadFriendSuggestions
        → SohbaApp.post('/Friends/GetFriendSuggestions') (POST) → 405
        → "Could not load suggestions"
```

## Related Files

- `Sohba/Views/Shared/Partials/_Sidebar.cshtml`
- `Sohba/Views/Shared/Partials/_RightSidebar.cshtml`
- `Sohba/wwwroot/js/features/sidebar.js`
- `Sohba/Controllers/GroupsController.cs` (`Discover`)
- `Sohba/Controllers/PagesController.cs` (`Discover`)
- `Sohba.Infrastructure/DBInitializer/DBInitializer.cs`

## Affected Components

- View — `_Sidebar.cshtml`
- View — `_RightSidebar.cshtml`
- JavaScript — `sidebar.js`
- Infrastructure — `DBInitializer.cs`

## Files That Need Modification

1. `Sohba.Infrastructure/DBInitializer/DBInitializer.cs` (idempotent seeding — fixes duplicates at the source)
2. `Sohba/wwwroot/js/features/sidebar.js` (GET fix)
3. `Sohba/Views/Shared/Partials/_Sidebar.cshtml` (optional: dedupe client-side as a safety net)

## Implementation Plan

1. Fix the seeder (Issue 5.1) — removes the source of duplicates.
2. Fix `sidebar.js` to use GET (fetch or `SohbaApp.get`).
3. Add a client-side safety net in `_Sidebar.cshtml`: after fetching pages/groups,
   de-duplicate by `name` before rendering:

   ```javascript
   const seen = new Set();
   const uniquePages = pages.filter(p => {
       if (seen.has(p.name)) return false;
       seen.add(p.name);
       return true;
   });
   ```

## Code Changes

<div style="color:green"><b>ADD — client-side dedupe in _Sidebar.cshtml (inside the fetch for pages):</b></div>

```javascript
            if (pages && pages.length > 0) {
                const seenNames = new Set();
                const uniquePages = pages.filter(page => {
                    if (!page.name || seenNames.has(page.name)) return false;
                    seenNames.add(page.name);
                    return true;
                });

                pagesContainer.innerHTML = uniquePages.map(page => `...`).join('');
            }
```

<div style="color:green"><b>ADD — client-side dedupe in loadGroupsToJoin:</b></div>

```javascript
            if (groups && groups.length > 0) {
                const seenNames = new Set();
                const uniqueGroups = groups.filter(group => {
                    if (!group.name || seenNames.has(group.name)) return false;
                    seenNames.add(group.name);
                    return true;
                });

                groupsContainer.innerHTML = uniqueGroups.map(group => `...`).join('');
            }
```

## Regression Testing

- **Test Users:** any user.
- **Navigation:** Home page (both sidebars visible on wide screens).
- **Expected Results:**
    - No duplicate page/group names in the sidebar.
    - Right sidebar shows friend suggestions (GET 200).
- **Failure Conditions:** if duplicates still appear after the seeder fix, run the SQL
  cleanup for groups/pages first.
- **Edge Cases:** `joinGroupFromSidebar` removes the card and re-fetches — the remaining list
  must not re-add members-only groups (server already filters `IsCurrentUserMember` in Discover).

<br>
<br>

---

<br>

# Dashboard — Make Everything Clickable

## Issue

On `/Dashboard`, users/posts/reports/groups/pages stats and recent lists are not clickable.

- User rows should open the user profile.
- Report rows should open the report / post.
- Post rows should open the post details.
- Same for groups and pages.

## Related Feature

- **Feature Name:** Dashboard — Admin overview.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Dashboard section (9.0).

## Expected Behaviour

Every stat card and recent-item row is clickable and navigates to the relevant resource.

## Current Behaviour

Only "View Reports" etc. links are clickable; recent users/posts rows are static.

## Root Cause

- The stat cards (`Total Users`, `Total Posts`, ...) are plain `<div>`s without links.
- Recent users/posts/reports rows are plain `<div>`s without `<a>` wrapping.

## Execution Flow

```
/Dashboard
    → Index.cshtml renders stat cards + recent lists
    → no <a> around rows
```

## Related Files

- `Sohba/Views/Dashboard/Index.cshtml`

## Affected Components

- View — `Dashboard/Index.cshtml`

## Files That Need Modification

1. `Sohba/Views/Dashboard/Index.cshtml`

## Implementation Plan

1. **Stat cards** → wrap with `<a>`:
   - Total Users → `/Dashboard/Users`
   - Total Posts → `/Dashboard/Posts`
   - Total Groups → `/Groups`
   - Total Pages → `/Pages`
   - Pending Reports → `/Dashboard/Reports`
2. **Recent Users** → link each row to `/Profile/Index/{user.Id}`.
3. **Recent Posts** → link each row to `/Posts/Details/{post.Id}`.
4. **Recent Reports** → link each row to `/Dashboard/Reports` (or `/Posts/Details/{postId}`).

## Code Changes (Pattern)

<div style="color:red"><b>REMOVE — static stat card structure (example: Total Users):</b></div>

```html
        <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-5 hover:shadow-lg transition-all duration-300 group">
            ...
            <h3 class="text-2xl font-black text-gray-900">@Model.TotalUsers.ToString("N0")</h3>
            <p class="text-sm text-gray-500 mt-1">Total Users</p>
        </div>
```

<div style="color:green"><b>ADD — clickable stat card:</b></div>

```html
        <a asp-action="Users" class="block bg-white rounded-2xl shadow-sm border border-slate-100 p-5 hover:shadow-lg transition-all duration-300 group">
            ...
            <h3 class="text-2xl font-black text-gray-900">@Model.TotalUsers.ToString("N0")</h3>
            <p class="text-sm text-gray-500 mt-1">Total Users</p>
        </a>
```

Apply the same pattern for Posts, Groups, Pages, and Reports.

<div style="color:green"><b>ADD — wrap recent user rows with a profile link:</b></div>

```html
                        <a href="/Profile/Index/@user.Id" class="flex items-center justify-between p-3 hover:bg-slate-50 rounded-xl transition-colors">
                            <div class="flex items-center gap-3">
                                <img src="..." class="w-10 h-10 rounded-full object-cover" />
                                <div>
                                    <h3 class="font-semibold text-gray-900">@user.Name</h3>
                                    <p class="text-xs text-gray-500">@user.Email</p>
                                </div>
                            </div>
                            <span class="text-xs text-gray-400">@user.CreatedAt.ToString("MMM dd")</span>
                        </a>
```

Similarly wrap recent posts with `/Posts/Details/@post.Id` and recent reports with
`/Dashboard/Reports`.

## Regression Testing

- **Test Users:** `admin@sohba.com`.
- **Navigation:** `/Dashboard`.
- **Expected Results:**
    - Clicking the users stat → `/Dashboard/Users`.
    - Clicking a recent user → profile page.
    - Clicking a recent post → post details.
- **Failure Conditions:** broken links when IDs are empty.
- **Edge Cases:** very large lists — the recent lists only show 5 each.

<br>
<br>

---

<br>

# Issue 9.1 — Dashboard Users: All Buttons Broken

## Issue

On `/Dashboard/Users`, all action buttons fail:

```
Users:1126 Uncaught (in promise) ReferenceError: showConfirmModal is not defined
    at window.deleteUser (Users:1126:17)
```

Also the search input does not filter (well — it reloads with query params, which works,
but the buttons are broken).

## Related Feature

- **Feature Name:** Dashboard — User Management.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 9.1.

## Expected Behaviour

- View user modal opens.
- Block / Unblock with confirmation.
- Delete with confirmation.
- Search + status filter reload the table.

## Current Behaviour

Block/Unblock/Delete all fail with `showConfirmModal is not a function`.

## Root Cause

Same cross-cutting root cause — `features/modal.js` is not loaded, and the
`_ConfirmModal.cshtml` internal script is commented out (Issue 3.6 / Cross-Cutting Fix).

## Execution Flow

```
Click Delete
    → window.deleteUser(userId)
        → showConfirmModal({...}) → ReferenceError
```

## Related Files

- `Sohba/Views/Dashboard/Users.cshtml`
- `Sohba/wwwroot/js/features/modal.js`
- `Sohba/Views/Shared/_AppLayout.cshtml`
- `Sohba/Views/Shared/Partials/_ConfirmModal.cshtml`
- `Sohba/Controllers/DashboardController.cs` (`BlockUser`, `UnblockUser`, `DeleteUser`, `GetUserDetails`)

## Affected Components

- JavaScript — confirm modal availability
- View — `Dashboard/Users.cshtml`

## Files That Need Modification

1. `Sohba/Views/Shared/_AppLayout.cshtml` (load `features/modal.js`)

## Implementation Plan

1. Load `features/modal.js` globally (Cross-Cutting Fix).
2. Verify the inline `showConfirmModal` calls in `Users.cshtml` work.
3. (Recommended) Move the inline dashboard scripts into `features/dashboard.js` and load it
   on the dashboard views, to follow RULES.md §2 (the file already exists).

## Code Changes

<div style="color:green"><b>ADD — in _AppLayout.cshtml:</b></div>

```html
    <script src="~/js/features/modal.js" asp-append-version="true"></script>
```

<div style="color:green"><b>ADD — in each Dashboard view script block, load dashboard.js instead of inline duplication (optional refactor):</b></div>

```html
@section Scripts {
    <script src="~/js/features/dashboard.js"></script>
    @* keep view-specific functions only *@
}
```

## Regression Testing

- **Test Users:** `admin@sohba.com`.
- **Navigation:** `/Dashboard/Users`.
- **Expected Results:**
    - Search "mohammed" → filtered URL with `?search=mohammed`.
    - Block → confirm modal → status "Blocked" + orange badge.
    - Unblock → confirm modal → status "Active".
    - Delete → confirm modal → row removed.
    - View → modal loads partial `_UserDetails`.
- **Failure Conditions:** `showConfirmModal is not a function` must not appear.
- **Edge Cases:** deleting the admin's own account is blocked by the service (verify).

<br>
<br>

---

<br>

# Issue 9.2 — Dashboard Posts: All Buttons Broken

## Issue

On `/Dashboard/Posts`:

```
Posts:1972 Uncaught (in promise) ReferenceError: showConfirmModal is not defined
    at window.deletePost (Posts:1972:17)
Posts:1923 Uncaught (in promise) ReferenceError: showConfirmModal is not defined
    at window.hidePost (Posts:1923:17)
```

## Related Feature

- **Feature Name:** Dashboard — Post Management.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 9.2.

## Expected Behaviour

- View post → `/Posts/Details/{id}`.
- Hide post with confirmation.
- Delete post with confirmation.

## Current Behaviour

Hide/Delete fail with `showConfirmModal is not a function`.

## Root Cause

Same Cross-Cutting Fix (Issue 3.6 / 9.1).

## Execution Flow

```
Click Delete → window.deletePost(postId) → showConfirmModal(...) → ReferenceError
Click Hide   → window.hidePost(postId)   → showConfirmModal(...) → ReferenceError
```

## Related Files

- `Sohba/Views/Dashboard/Posts.cshtml`
- `Sohba/Views/Shared/Partials/_ConfirmModal.cshtml`
- `Sohba/wwwroot/js/features/modal.js`
- `Sohba/Views/Shared/_AppLayout.cshtml`
- `Sohba/Controllers/DashboardController.cs` (`DeletePost`, `HidePost`)

## Affected Components

- JavaScript — confirm modal availability
- View — `Dashboard/Posts.cshtml`

## Files That Need Modification

1. `Sohba/Views/Shared/_AppLayout.cshtml`

## Implementation Plan

1. Load `features/modal.js` globally.
2. Verify inline `showConfirmModal` calls.

## Code Changes

Same as Issue 9.1 — add `features/modal.js` to `_AppLayout.cshtml`.

## Regression Testing

- **Test Users:** `admin@sohba.com`.
- **Navigation:** `/Dashboard/Posts`.
- **Expected Results:** hide / delete / view all work with the confirmation modal.
- **Failure Conditions:** modal doesn't appear if modal.js is still not loaded.
- **Edge Cases:** hiding an already hidden post is disabled server-side (Service rule).

<br>
<br>

---

<br>

# Issue 9.3 — Dashboard Reports: All Buttons Broken

## Issue

On `/Dashboard/Reports`:

```
Reports:730 Uncaught ReferenceError: showConfirmModal is not defined
    at resolveReport (Reports:730:13)
Reports:748 Uncaught ReferenceError: showConfirmModal is not defined
    at dismissReport (Reports:748:14)
Reports:767 Uncaught ReferenceError: showConfirmModal is not defined
    at deleteReportedPost (Reports:767:14)
```

## Related Feature

- **Feature Name:** Dashboard — Report Management.
- **Related Section:** `Sohba_Frontend_Test_Plan.md` → Section 9.3.

## Expected Behaviour

- Resolve report.
- Dismiss report.
- Delete reported post (resolves the report too).

## Current Behaviour

All buttons throw `showConfirmModal is not a function`.

## Root Cause

Same Cross-Cutting Fix.

## Execution Flow

```
Click Resolve → resolveReport(id) → showConfirmModal(...) → ReferenceError
```

## Related Files

- `Sohba/Views/Dashboard/Reports.cshtml`
- `Sohba/wwwroot/js/features/modal.js`
- `Sohba/Views/Shared/_AppLayout.cshtml`
- `Sohba/Controllers/DashboardController.cs` (`ResolveReport`, `DismissReport`, `DeleteReportedPost`)
- `Sohba/wwwroot/js/features/dashboard.js`

## Affected Components

- JavaScript — confirm modal availability
- View — `Dashboard/Reports.cshtml`

## Files That Need Modification

1. `Sohba/Views/Shared/_AppLayout.cshtml`

## Implementation Plan

1. Load `features/modal.js` globally.
2. Verify inline `showConfirmModal` calls.

## Code Changes

Same as Issue 9.1 — add `features/modal.js` to `_AppLayout.cshtml`.

## Regression Testing

- **Test Users:** `admin@sohba.com`.
- **Required Data:** A pending report exists (the seeder creates one for "My Travel Story: Paris").
- **Navigation:** `/Dashboard/Reports`.
- **Expected Results:**
    - Resolve → confirm → report becomes "Resolved".
    - Dismiss → confirm → report dismissed.
    - Delete Reported Post → confirm → post deleted + report resolved.
- **Failure Conditions:** buttons must not throw.
- **Edge Cases:** resolved reports should show "No actions".

<br>
<br>

---

<br>

# Additional Issues Found

The following were discovered during the investigation but were not explicitly reported.
Each is classified by impact and severity.

<br>

## Additional Issue 1 — Seeder Creates Duplicate Pages, Posts, Followers, Friendships

### Why it happens

`DBInitializer` runs on every startup and creates new rows with `Guid.NewGuid()` for Pages and
Posts without checking name/title existence (same bug as Groups, Issue 5.1).

### Impact

- Duplicated pages/posts visible in feeds and sidebars.
- Duplicated friendship rows (both directions possible).

### Severity

**HIGH** (data integrity + visible duplicates).

### Should it be fixed now or later?

**NOW** — before any production data. Same fix pattern as Issue 5.1:
add existence checks in `CreatePageAsync`, `CreatePostAsync`, `AddFriendshipAsync`.

<br>
<br>

---

<br>

## Additional Issue 2 — `FriendshipRepository.GetByUsersAsync` & `HasPendingRequestAsync` Ignore Reversed Direction

### Why it happens

The reversed-lookup query result is computed but never returned / used.

### Impact

- `AcceptFriendRequestAsync` may fail with "No pending friend request found."
- `RejectFriendRequestAsync` / `CancelFriendRequestAsync` same problem.
- Contributes to the 429 issue (issue 7.4/7.5).

### Severity

**HIGH**.

### Should it be fixed now or later?

**NOW** — exact fix included in Issue 7.4/7.5.

<br>
<br>

---

<br>

## Additional Issue 3 — `HomeController.LoadMore` Is Dead Code (duplicate of `GetPostCards`)

### Why it happens

`feed.js` uses only `GetPostCards`; `LoadMore` is never called.

### Impact

- Dead endpoint, wasted surface area, inconsistent pagination logic.

### Severity

**LOW**.

### Should it be fixed now or later?

**LATER** — remove or keep as backward-compat; keep state in sync if removed.

<br>
<br>

---

<br>

## Additional Issue 4 — `tailwind.css` 404 In `_AppLayout`

### Why it happens

`<link rel="stylesheet" href="~/css/tailwind.css" />` but no such file exists in `wwwroot/css`.

### Impact

- 404 console noise; Tailwind styles come from the CDN script anyway.

### Severity

**LOW**.

### Should it be fixed now or later?

**NOW** — remove the dead link (covered in Issue 7.6).

<br>
<br>

---

<br>

## Additional Issue 5 — Namespace Inconsistency in the JavaScript (window.* vs SohbaApp.*)

### Why it happens

Mixed conventions:

- `sohba-posts.js`: `window.showReplyForm`, `window.submitReply`, `window.toggleReplies`
- Views call: `SohbaApp.showReplyForm(...)`, `SohbaApp.submitReply(...)`

### Impact

- Reply buttons broken (Issue 3.5).
- Future similar bugs.

### Severity

**HIGH**.

### Should it be fixed now or later?

**NOW** — add namespace aliases (covered in Issue 3.5).

<br>
<br>

---

<br>

## Additional Issue 6 — `GetPostDetails` Returns Comments Without `Replies` / `IsAuthor`

### Why it happens

The controller projects an anonymous type, dropping `Replies`, `ReplyCount`, `ParentCommentId`,
and any author flag.

### Impact

- Replies never render in the post modal.
- Delete button cannot be rendered conditionally.

### Severity

**HIGH**.

### Should it be fixed now or later?

**NOW** — covered in Issue 3.5.

<br>
<br>

---

<br>

## Additional Issue 7 — Dashboard Inline Scripts Duplicate Logic That Already Exists In `features/dashboard.js`

### Why it happens

`Views/Dashboard/Users.cshtml`, `Posts.cshtml`, `Reports.cshtml` define `resolveReport`,
`dismissReport`, `deleteReportedPost` inline, AND `features/dashboard.js` defines them too.

### Impact

- If `dashboard.js` is ever loaded alongside, duplicate/conflicting functions.
- Currently only inline versions run (they reference missing `showConfirmModal`).

### Severity

**MEDIUM**.

### Should it be fixed now or later?

**LATER** — consolidate into `features/dashboard.js` per RULES.md §2.

<br>
<br>

---

<br>

## Additional Issue 8 — `_PostCard` Renders The Post Modal Per Instance (duplicate IDs)

### Why it happens

The modal markup lives inside the post-card partial.

### Impact

- Duplicate `id="postModal"` in the DOM on Home (AJAX) and Profile (loop).
- Broke JS event wiring and caused visible duplicates (Issue 3.2).

### Severity

**HIGH**.

### Should it be fixed now or later?

**NOW** — covered in Issue 3.2 (extract modal to shared partial).

<br>
<br>

---

<br>

## Additional Issue 9 — `GetTimeAgo` Computes From `DateTime.UtcNow - createdAt.ToLocalTime()`

### Why it happens

`createdAt` is stored as UTC; calling `.ToLocalTime()` then subtracting from UtcNow mixes
timezones, producing wrong "x hours ago" values.

### Impact

- Incorrect relative timestamps on post cards.

### Severity

**MEDIUM**.

### Should it be fixed now or later?

**NOW/LATER** — simple fix:

```csharp
var timeSpan = DateTime.UtcNow - createdAt;
```

(keep `createdAt` as UTC).

<br>
<br>

---

<br>

## Additional Issue 10 — Settings Checkboxes Hard-Coded (covered in Issue 8.2)

### Why it happens

The notification checkboxes don't use `asp-for`.

### Impact

- Settings never persist for notifications.

### Severity

**MEDIUM**.

### Should it be fixed now or later?

**NOW**.

<br>
<br>

---

<br>

## Additional Issue 11 — Groups/Details Renders `ViewBag.Posts` That Is Never Set

### Why it happens

`GroupsController.Details` doesn't populate `ViewBag.Posts`, but the view references
`model="ViewBag.Posts"` inside the `<partial>`.

### Impact

- The initial HTML has no posts; the page then loads them via AJAX
  (`loadGroupPosts`). Works, but the initial render is misleading / empty flash.

### Severity

**LOW**.

### Should it be fixed now or later?

**LATER** — either remove the initial partial or load posts in the controller.

<br>
<br>

---

<br>

## Additional Issue 12 — `PagesController.Edit` Has Correct Ownership Check But `Pages/Details.cshtml` Hides It Unconditionally

### Why it happens

Similar to Groups: `Pages/Details.cshtml` has the Edit Page button inside commented
`@if (User.Identity?.Name == Model.AdminName)`.

### Impact

- Non-admin page visitors see "Edit Page" and the controller returns `Forbid()` on click →
  confusing UX.

### Severity

**MEDIUM**.

### Should it be fixed now or later?

**NOW** — apply the same conditional rendering as Groups (compare `ViewBag.CurrentUserId`
with `Model.AdminId`), and set `ViewBag.CurrentUserId` in `PagesController.Details`.

<br>
<br>

---

<br>

## Additional Issue 13 — Profile Link On User Names Not Always Clickable

### Why it happens

User names/avatars in post cards, comments, members previews, and friend cards are plain
`<div>`/`<span>` with no `<a href="/Profile/Index/{id}">`.

### Impact

- Users cannot click a name to open a profile (feature request by the user).

### Severity

**MEDIUM**.

### Should it be fixed now or later?

**NOW/LATER** — wrap names/avatars in profile links in:
- `_PostCard.cshtml` (author name/avatar)
- `sohba-modal.js` comments
- `Groups/Details.cshtml` members
- `Friends/Index.cshtml` friend cards
- `Dashboard/Index.cshtml` recent users (covered above)

<br>
<br>

---

<br>

## Additional Issue 14 — Missing Banner / Cover Image For Groups, Pages, Profiles

### Why it happens

The cover areas are hard-coded gradients (`h-48 bg-gradient-to-r ...`); there are no
`CoverImageUrl` properties or upload inputs.

### Impact

- Feature request missing; profiles/groups/pages look generic.

### Severity

**LOW-MEDIUM** (feature).

### Should it be fixed now or later?

**LATER** — a new `CoverImageUrl` column + upload control + render in the header, per feature
request: "add banner/thumbnail for groups and pages and profiles".

<br>
<br>

---

<br>

## Additional Issue 15 — `SavedPost` Enum-Based Tag Prevents Collections (Issue 3.10)

### Why it happens

`SavedPost.Tag` is an enum; no collection entity exists.

### Impact

- No custom categories/playlists per user.

### Severity

**MEDIUM** (feature).

### Should it be fixed now or later?

**NOW** if the user wants the redesigned Save behavior (Issue 3.10).

<br>
<br>

---

<br>

## Additional Issue 16 — `Search` Results Page's `See all` Link Is `/Search?q=` (index) — Works But Could Omit `/Index`

### Why it happens

`_Header` / `search.js` builds `/Search?q=...` (which routes to `SearchController.Index` via
the default route) — functional but inconsistent with the explicit `/Search/Index`.

### Impact

- Minor inconsistency.

### Severity

**LOW**.

### Should it be fixed now or later?

**LATER** — use `/Search/Index?q=` consistently.

<br>
<br>

---

<br>

## Additional Issue 17 — Rate Limit `FriendRequest` Too Low For UI Retries

### Why it happens

`PermitLimit = 10` per minute with `QueueLimit = 0`; accept/reject bursts exceed it.

### Impact

- 429 responses (Issue 7.4/7.5).

### Severity

**MEDIUM**.

### Should it be fixed now or later?

**NOW** — together with the receiver buttons guard, raise to 30/min and/or set
`QueueLimit = 2`.

<br>
<br>

---

<br>

## Additional Issue 18 — `Groups/Index.cshtml` Join Button Payload Correct (`{ id }`) But Sidebar `joinGroup` Is Wrong (`{ groupId }`)

### Why it happens

- `GroupsController.Join` binds `IdRequestDto { Guid Id }`.
- Sidebar `joinGroup` posts `{ groupId }` → `Id` = `Guid.Empty` → "Invalid group ID."

### Impact

- Joining groups from the sidebar fails.

### Severity

**HIGH**.

### Should it be fixed now or later?

**NOW** — covered in Issue 5.4 (fix payload to `{ id: groupId }`).

<br>
<br>

---

<br>

## Additional Issue 19 — `CommentsController.Delete` Accepts No Anti-Forgery Token

### Why it happens

The action lacks `[ValidateAntiForgeryToken]`; though `SohbaApp.post` does include the token
header, the endpoint validates nothing server-side for this action.

### Impact

- CSRF risk on comment deletion.

### Severity

**MEDIUM** (security).

### Should it be fixed now or later?

**NOW** — add `[ValidateAntiForgeryToken]` to the `Delete` action and keep sending the
`RequestVerificationToken` header (already done by `SohbaApp.post`).

Note: check if the global antiforgery setup automatically validates header tokens. If MVC
does not auto-validate, add the attribute.

<br>
<br>

---

<br>

# Cross-Cutting Fix: The Real Cause Of `showConfirmModal is not a function`

## Summary

`window.showConfirmModal` is defined in `Sohba/wwwroot/js/features/modal.js`, but that file is
**never loaded** by the layout, and the copy inside `_ConfirmModal.cshtml` is **commented out**.

## The Single Fix That Resolves Issues 3.6, 5.4, 9.1, 9.2, 9.3 (and every confirmation modal)

<div style="color:green"><b>ADD — Sohba/Views/Shared/_AppLayout.cshtml (script section):</b></div>

```html
    <script src="~/js/sohba-core.js"></script>
    <script src="~/js/sohba-posts.js"></script>
    <script src="~/js/sohba-modal.js"></script>
    <script src="~/js/sohba-stories.js"></script>
    <script src="~/js/features/stories.js" asp-append-version="true"></script>
    <script src="~/js/features/groups.js" asp-append-version="true"></script>
    <script src="~/js/features/comments.js" asp-append-version="true"></script>
    <script src="~/js/features/modal.js" asp-append-version="true"></script>
    <script src="~/js/features/friends.js" asp-append-version="true"></script>
    <script src="~/js/features/search.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
```

Keep the `<script>` inside `_ConfirmModal.cshtml` commented (to avoid double registration).

## Verification After This Fix

Open the browser console on ANY page and run:

```javascript
typeof window.showConfirmModal   // must be "function"
typeof window.closeConfirmModal  // must be "function"
typeof window.SohbaApp           // must be "object"
```

All confirmation flows (Delete Post, Delete Comment, Cancel Request, Block User,
Leave Group, Dashboard actions) should work.

<br>
<br>

---

<br>

# Final Notes

1. **Apply the Cross-Cutting Fix first** — it unlocks Issues 3.6, 5.4, 9.1, 9.2, 9.3 and the
   Danger-Zone modals in Settings.
2. **Apply the DBInitializer idempotency fix before anything else on a fresh machine** —
   otherwise duplicates keep accumulating.
3. **Apply the script-loading changes together** so `friends.js`, `comments.js`, `modal.js`,
   `search.js` are globally available — this unlocks Profile actions, comment delete,
   confirmation modals, and the header search.
4. **No source file was modified while writing this document.** Every file change above is a
   recommendation for the implementing developer.

<br>
<br>

---

<br>

# Appendix — Full File Inventory Of Related Files Inspected

| Layer | Path |
|-------|------|
| Controllers | `Sohba/Controllers/HomeController.cs` |
| Controllers | `Sohba/Controllers/PostsController.cs` |
| Controllers | `Sohba/Controllers/CommentsController.cs` |
| Controllers | `Sohba/Controllers/FriendsController.cs` |
| Controllers | `Sohba/Controllers/GroupsController.cs` |
| Controllers | `Sohba/Controllers/PagesController.cs` |
| Controllers | `Sohba/Controllers/ProfileController.cs` |
| Controllers | `Sohba/Controllers/StoriesController.cs` |
| Controllers | `Sohba/Controllers/SearchController.cs` |
| Controllers | `Sohba/Controllers/DashboardController.cs` |
| Controllers | `Sohba/Controllers/BaseController.cs` |
| Application Services | `Sohba.Application/Services/PostService.cs` |
| Application Services | `Sohba.Application/Services/InteractionService.cs` |
| Application Services | `Sohba.Application/Services/FriendshipService.cs` |
| Application Services | `Sohba.Application/Services/GroupService.cs` |
| Application Services | `Sohba.Application/Services/UserSettingsService.cs` |
| Application Services | `Sohba.Application/Services/UserService.cs` |
| Domain | `Sohba.Domain/Domain Rules/Logic/FriendshipDomainService.cs` |
| Infrastructure | `Sohba.Infrastructure/Repositories/PostRepository.cs` |
| Infrastructure | `Sohba.Infrastructure/Repositories/FriendshipRepository.cs` |
| Infrastructure | `Sohba.Infrastructure/Repositories/InteractionRepository.cs` |
| Infrastructure | `Sohba.Infrastructure/DBInitializer/DBInitializer.cs` |
| Program | `Sohba/Program.cs` |
| Views | `Sohba/Views/Home/Index.cshtml` |
| Views | `Sohba/Views/Shared/_AppLayout.cshtml` |
| Views | `Sohba/Views/Shared/Partials/_Header.cshtml` |
| Views | `Sohba/Views/Shared/Partials/_Sidebar.cshtml` |
| Views | `Sohba/Views/Shared/Partials/_RightSidebar.cshtml` |
| Views | `Sohba/Views/Shared/Partials/_CreatePost.cshtml` |
| Views | `Sohba/Views/Shared/Partials/_PostCard.cshtml` |
| Views | `Sohba/Views/Shared/Partials/_ConfirmModal.cshtml` |
| Views | `Sohba/Views/Shared/Partials/_CreateStoryModal.cshtml` |
| Views | `Sohba/Views/Shared/Partials/_StoryRail.cshtml` |
| Views | `Sohba/Views/Shared/Partials/_StoryViewer.cshtml` |
| Views | `Sohba/Views/Groups/Details.cshtml` |
| Views | `Sohba/Views/Groups/Index.cshtml` |
| Views | `Sohba/Views/Groups/Edit.cshtml` |
| Views | `Sohba/Views/Pages/Index.cshtml` |
| Views | `Sohba/Views/Pages/Details.cshtml` |
| Views | `Sohba/Views/Pages/Create.cshtml` |
| Views | `Sohba/Views/Profile/Index.cshtml` |
| Views | `Sohba/Views/Profile/Settings.cshtml` |
| Views | `Sohba/Views/Friends/Index.cshtml` |
| Views | `Sohba/Views/Friends/Requests.cshtml` |
| Views | `Sohba/Views/Search/Results.cshtml` |
| Views | `Sohba/Views/Dashboard/Index.cshtml` |
| Views | `Sohba/Views/Dashboard/Users.cshtml` |
| Views | `Sohba/Views/Dashboard/Posts.cshtml` |
| Views | `Sohba/Views/Dashboard/Reports.cshtml` |
| JS | `Sohba/wwwroot/js/sohba-core.js` |
| JS | `Sohba/wwwroot/js/sohba-posts.js` |
| JS | `Sohba/wwwroot/js/sohba-modal.js` |
| JS | `Sohba/wwwroot/js/sohba-stories.js` |
| JS | `Sohba/wwwroot/js/features/feed.js` |
| JS | `Sohba/wwwroot/js/features/sidebar.js` |
| JS | `Sohba/wwwroot/js/features/friends.js` |
| JS | `Sohba/wwwroot/js/features/comments.js` |
| JS | `Sohba/wwwroot/js/features/dashboard.js` |
| JS | `Sohba/wwwroot/js/features/groups.js` |
| JS | `Sohba/wwwroot/js/features/header.js` |
| JS | `Sohba/wwwroot/js/features/search.js` |
| JS | `Sohba/wwwroot/js/features/stories.js` |
| JS | `Sohba/wwwroot/js/features/modal.js` |
| JS | `Sohba/wwwroot/js/site.js` |
| CSS | `Sohba/wwwroot/css/*` (no tailwind.css) |

<br>
<br>

---

<br>

# End Of Document

This document is a complete implementation guide. No project source files were modified while
producing it.