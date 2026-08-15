# Sohba Social Media App — Full QA & Production Readiness Audit

**Auditor:** Senior QA Engineer / Full-Stack Auditor
**Date:** 2026-08-10
**Scope:** End-to-end code review + live browser testing against `http://localhost:5206`
**Method:** Static analysis of all layers (Domain, Application, Infrastructure, Web, Views, JS) + live UI testing with Playwright + server log inspection.

---

## 1. Executive Summary

Sohba is a **well-structured but NOT production-ready** ASP.NET Core MVC social media application. The architecture is clean (Domain/Application/Infrastructure/Web separation, UnitOfWork, Repository pattern, Domain Services, AutoMapper, FluentValidation, SignalR, Serilog, Rate Limiting). However, it contains **multiple critical authorization bypasses, a completely broken post-edit feature, a non-idempotent database seeder that duplicates data on every startup, hardcoded secrets committed to source control, and several features that are stubs or dead code.**

The application **builds and runs**, login works, the feed renders, and basic navigation functions. But it is **not safe to deploy to production** in its current state.

---

## 2. Release Readiness Verdict

**NOT READY FOR PRODUCTION — BLOCKED.**

| Area | Status |
|------|--------|
| Build | ✅ Compiles (net10.0) |
| Runs | ✅ Starts, DB migrates, seeds |
| Login/Register | ⚠️ Works but no email confirmation, no IsActive check |
| Feed | ⚠️ Works but duplicates posts on every restart |
| Post Edit | ❌ **Broken — AutoMapper crash** |
| Post Delete | ⚠️ Works for owner/admin, but admin dashboard delete is broken |
| Comments | ⚠️ Works but no privacy check on comments |
| Groups/Pages | ⚠️ Basic CRUD works, but CSRF missing on several POSTs |
| Friends | ⚠️ Works but JS has ReferenceErrors |
| Notifications | ⚠️ Works but SignalR hub methods are spoofable |
| Search | ⚠️ Leaks private posts |
| Stories | ⚠️ Works but CSRF missing |
| Security | ❌ **Multiple critical auth bypasses + hardcoded secrets** |
| Performance | ⚠️ N+1 queries, no caching, in-memory SignalR |

---

## 3. Missing Features

1. **Email confirmation** — `RequireConfirmedEmail = false` (comment says "We Will Change This TO TRUE After Adding Email Service" — never done). Users can register with any email.
2. **Account deactivation enforcement** — `IsActive` is set to `false` on deactivate, but `LoginAsync` never checks `IsActive` or `IsDeleted`. A deactivated/deleted user can still log in.
3. **Share functionality** — Share button exists in UI but `0 shares` is hardcoded; no share model/table exists.
4. **Live Video / Feeling buttons** — Present in the create-post UI but do nothing.
5. **Real-time feed updates** — SignalR only handles notifications, not feed updates.
6. **Comment editing** — `CanEditComment` domain rule exists but no controller/service action implements it.
7. **Group/Page post privacy enforcement** — `GetGroupPostsAsync`/`GetPagePostsAsync` don't check membership/follow status.
8. **Blocked-user enforcement in interactions** — `AddCommentAsync` hardcodes `isBlockedByOwner: false`; `AddReactionAsync` never calls `CanAddReaction`.
9. **Admin dashboard "Users last 7 days" chart** — hardcoded fake data `{ 5, 8, 12, 7, 15, 10, 20 }` with a TODO.
10. **Story media type** — `MediaType` is always `"image"`; no video support despite UI.
11. **Pagination on comments** — `GetCommentsByPostIdAsync` loads ALL comments for a post with no paging.
12. **Hashtag Arabic support** — regex `#\w+` doesn't match Arabic hashtags (critical for an Arabic-market app).
13. **File cleanup** — `DeleteFileAsync` is never called; orphaned uploads accumulate.
14. **User search pagination** — `SearchUsersAsync` has no pagination.
15. **Notification preferences granularity** — `ShouldSendBasedOnPreferences` is coarse (social vs system only).

---

## 4. Blockers

### B1. Post Editing is Completely Broken (AutoMapper crash)
- **Severity:** Critical
- **Reproduction:** Log in → open any post → click Edit → change content → submit.
- **Root cause:** `MappingProfile.cs` defines `CreateMap<PostCreateDto, Post>` but **no `CreateMap<PostUpdateDto, Post>`**. `PostService.UpdatePostAsync` calls `_mapper.Map(postDto, post)` which throws `AutoMapperMappingException`.
- **Affected files:** `Sohba.Application/Mappings/MappingProfile.cs`, `Sohba.Application/Services/PostService.cs`, `Sohba/Controllers/PostsController.cs`
- **Layer:** Application (mapping)
- **Why:** Missing AutoMapper registration.
- **Fix:** Add `CreateMap<PostUpdateDto, Post>()` to `MappingProfile`.
- **Blocker:** ✅ YES

### B2. Any Authenticated User Can Edit Any Post (Authorization Bypass)
- **Severity:** Critical
- **Reproduction:** Log in as user A → navigate to `/Posts/Edit/{postId-of-user-B}` → submit changes.
- **Root cause:** `PostDomainService.CanUpdatePost(userId, postId, isPostDeleted)` **never checks `userId == postOwnerId`**. It only checks `isPostDeleted`. `PostService.UpdatePostAsync` passes `post.UserId` as the `postId` param (wrong signature usage) and never verifies ownership.
- **Affected files:** `Sohba.Domain/Domain Rules/Logic/PostDomainService.cs`, `Sohba.Application/Services/PostService.cs`
- **Layer:** Domain + Application
- **Why:** Missing ownership check in domain rule.
- **Fix:** Add `if (userId != postOwnerId) return Result.Failure(...)` to `CanUpdatePost` and pass `post.UserId` correctly.
- **Blocker:** ✅ YES

### B3. Hardcoded Secrets Committed to Source Control
- **Severity:** Critical
- **Reproduction:** Read `Sohba/appsettings.json` — JWT key `"YourSuperSecretKeyHereAtLeast32CharactersLong!"` and Mailtrap credentials are committed.
- **Root cause:** Secrets in `appsettings.json` (not in user-secrets/env vars).
- **Affected files:** `Sohba/appsettings.json`, `Sohba.Infrastructure/DBInitializer/DBInitializer.cs` (admin password `Admin@123456`, test user passwords)
- **Layer:** Infrastructure/Config
- **Why:** No secret management.
- **Fix:** Move to environment variables / Azure Key Vault / user-secrets. Rotate all exposed credentials.
- **Blocker:** ✅ YES

### B4. Database Seeder is Non-Idempotent — Duplicates Data on Every Startup
- **Severity:** High
- **Reproduction:** Start the app twice. The feed shows the same posts (e.g., "My Photography Journey", "Social Media Marketing Tips", "My Travel Story", "Design Principles") twice with different timestamps.
- **Root cause:** `DBInitializer.CreatePostAsync`, `CreateGroupAsync`, `CreatePageAsync` always insert new rows with random GUIDs. Only `SeedExtraTestDataAsync` guards stories with `if (await _context.Stories.AnyAsync()) return;`. The `CreateRelationshipsAsync` posts are re-created every startup.
- **Affected files:** `Sohba.Infrastructure/DBInitializer/DBInitializer.cs`
- **Layer:** Infrastructure
- **Why:** Seeder lacks idempotency checks for posts/groups/pages.
- **Fix:** Guard each seed with existence checks (e.g., by title/name) before inserting.
- **Blocker:** ✅ YES (data integrity)

### B5. Admin Dashboard "Delete Post" Fails
- **Severity:** High
- **Reproduction:** Log in as admin → Dashboard → Posts → Delete a post.
- **Root cause:** `DashboardController.DeletePost` calls `_postService.DeletePostAsync(model.postId, GetCurrentUserId())` **without `isAdmin: true`**. `PostDomainService.CanDeletePost` requires `isAdmin` or ownership, so the admin (who isn't the post owner) gets "You are not authorized to delete this post."
- **Affected files:** `Sohba/Controllers/DashboardController.cs`
- **Layer:** Web
- **Why:** Missing `isAdmin: true` argument.
- **Fix:** Pass `isAdmin: true` (or `User.IsInRole("Admin")`).
- **Blocker:** ✅ YES (admin moderation broken)

---

## 5. High Severity Bugs

### H1. Private Posts Leak in Search Results
- **Reproduction:** Search for a private post's content via `/Search?q=...`.
- **Root cause:** `PostRepository.SearchPostsAsync` has no privacy filter; `SearchService.GlobalSearchAsync` maps all results.
- **Affected:** `Sohba.Infrastructure/Repositories/PostRepository.cs`, `Sohba.Application/Services/SearchService.cs`
- **Layer:** Infrastructure/Application
- **Fix:** Filter by `!IsPrivate` or friendship in search query.
- **Blocker:** No (but serious privacy leak)

### H2. Friends List Leaks to Non-Friends on Profile
- **Reproduction:** View a private user's profile as a non-friend. The friends list is still fetched and rendered.
- **Root cause:** `ProfileController.Index` calls `GetFriendsListAsync(profileUserId)` unconditionally, even when `canViewFriends` is false.
- **Affected:** `Sohba/Controllers/ProfileController.cs`
- **Layer:** Web
- **Fix:** Only fetch friends when `canViewFriends` is true.
- **Blocker:** No

### H3. `HidePostAsync` Has No Authorization Check
- **Reproduction:** Any authenticated user can POST `/Dashboard/HidePost` with any postId.
- **Root cause:** `PostService.HidePostAsync` never checks ownership or admin role.
- **Affected:** `Sohba.Application/Services/PostService.cs`
- **Layer:** Application
- **Fix:** Add ownership/admin check.
- **Blocker:** No (but authorization hole)

### H4. SignalR Hub Methods Are Spoofable
- **Reproduction:** Any authenticated client can call `SendNotificationToUser`, `SendNotificationToUsers`, or `BroadcastNotification` to send fake notifications to any user or broadcast to all.
- **Root cause:** Public hub methods with no authorization beyond `[Authorize]` on the hub.
- **Affected:** `Sohba/Hubs/NotificationHub.cs`
- **Layer:** Web (SignalR)
- **Fix:** Remove public send methods or restrict to server-side via `IHubContext` only.
- **Blocker:** No (but spam/abuse vector)

### H5. `GetUsersByStatusAsync("blocked")` is Broken
- **Reproduction:** Admin Dashboard → Users → filter "Blocked".
- **Root cause:** `UserService.GetUsersByStatusAsync` calls `GetBlockedUsersAsync(Guid.Empty)` — passing empty GUID returns nothing (or throws).
- **Affected:** `Sohba.Application/Services/UserService.cs`
- **Layer:** Application
- **Fix:** Use a real user ID or query blocked users directly.
- **Blocker:** No (dashboard feature broken)

### H6. `DeleteMyAccountAsync` Will Throw FK Constraint Violation
- **Reproduction:** User with posts/comments/friendships tries to delete their account.
- **Root cause:** Comment says "NOTE: implement the cascade deletion carefully in the repository" — cascade deletion is NOT implemented. Hard-deleting a user with related rows violates FK constraints.
- **Affected:** `Sohba.Application/Services/UserService.cs`
- **Layer:** Application
- **Fix:** Implement cascade delete or soft-delete the user.
- **Blocker:** No (but data-loss/500 risk)

### H7. `GetRecentAsync` Returns Deleted Posts
- **Reproduction:** Admin dashboard "Recent Posts" shows soft-deleted posts.
- **Root cause:** `PostRepository.GetRecentAsync` has no `!p.IsDeleted` filter.
- **Affected:** `Sohba.Infrastructure/Repositories/PostRepository.cs`
- **Layer:** Infrastructure
- **Fix:** Add `!p.IsDeleted` filter.
- **Blocker:** No

---

## 6. Medium Severity Bugs

### M1. `RemoveReactionAsync` is Not Idempotent
- **Reproduction:** Double-click a reaction toggle. Second call returns "No reaction found" error.
- **Root cause:** Returns `Failure` when no reaction exists instead of `Success`.
- **Affected:** `Sohba.Application/Services/InteractionService.cs`
- **Layer:** Application
- **Fix:** Return `Success` when no reaction exists (idempotent).

### M2. `CanAddReaction` Domain Rule is Never Called
- **Reproduction:** A blocked user can still react to a post.
- **Root cause:** `InteractionService.AddReactionAsync` never calls `_interactionDomainService.CanAddReaction`.
- **Affected:** `Sohba.Application/Services/InteractionService.cs`
- **Layer:** Application
- **Fix:** Call the domain rule before adding a reaction.

### M3. `AddCommentAsync` Hardcodes `isBlockedByOwner: false`
- **Reproduction:** A user blocked by the post owner can still comment.
- **Root cause:** `CanAddComment(userId, content, post.IsDeleted, isBlockedByOwner: false)` — blocked check is hardcoded off.
- **Affected:** `Sohba.Application/Services/InteractionService.cs`
- **Layer:** Application
- **Fix:** Pass actual blocked status.

### M4. `GetCommentsByPostIdAsync` Has No Privacy Check
- **Reproduction:** View comments on a private post you can't see.
- **Root cause:** No privacy verification before returning comments.
- **Affected:** `Sohba.Application/Services/InteractionService.cs`
- **Layer:** Application
- **Fix:** Verify post visibility before returning comments.

### M5. `GetCommentDepthAsync` is N+1
- **Reproduction:** Reply to a deeply nested comment — multiple DB round-trips.
- **Root cause:** Walks up `ParentCommentId` one query at a time.
- **Affected:** `Sohba.Application/Services/InteractionService.cs`
- **Layer:** Application
- **Fix:** Use a single recursive query or store depth.

### M6. `PageService.CreatePageAsync` Drops the Image URL
- **Reproduction:** Create a page with an image — the image is uploaded but never saved to the page.
- **Root cause:** `CreatePageAsync` doesn't set `ImageUrl` from `dto.ImageUrl`.
- **Affected:** `Sohba.Application/Services/PageService.cs`
- **Layer:** Application
- **Fix:** Set `ImageUrl = dto.ImageUrl`.

### M7. `ToggleSavePost` Loads ALL Saved Posts to Check Existence
- **Reproduction:** Toggle save on a post — loads the user's entire saved-posts list.
- **Root cause:** `PostsController.ToggleSavePost` calls `GetSavedPostsAsync(userId)` then `.FirstOrDefault`.
- **Affected:** `Sohba/Controllers/PostsController.cs`
- **Layer:** Web
- **Fix:** Use `GetSavedPostAsync(userId, postId)` directly.

### M8. `SavedPosts(string tag)` Parameter is Ignored
- **Reproduction:** Visit `/Posts/SavedPosts?tag=favorites` — the tag filter does nothing.
- **Root cause:** `SavedPosts` sets `ViewBag.CurrentTag = tag` but never filters by it.
- **Affected:** `Sohba/Controllers/PostsController.cs`
- **Layer:** Web
- **Fix:** Apply the tag filter.

### M9. `GetUserStories` Fetches All Friend Stories Then Filters
- **Reproduction:** View a specific user's stories — loads all friends' stories first.
- **Root cause:** `StoriesController.GetUserStories` calls `GetStoriesForFeedAsync` then filters in memory.
- **Affected:** `Sohba/Controllers/StoriesController.cs`
- **Layer:** Web
- **Fix:** Query stories for the specific user directly.

### M10. `GetAboutTab` Passes `Guid.Empty` to `GetGroupPostsAsync`
- **Reproduction:** View a group's About tab — posts count is wrong/empty.
- **Root cause:** `GroupsController.GetAboutTab` calls `GetGroupPostsAsync(groupId, Guid.Empty)`.
- **Affected:** `Sohba/Controllers/GroupsController.cs`
- **Layer:** Web
- **Fix:** Pass the current user ID.

### M11. `GetPageStats` Passes `Guid.Empty`
- **Reproduction:** View page stats — posts count may be wrong.
- **Root cause:** `PagesController.GetPageStats` calls `GetPagePostsAsync(pageId, Guid.Empty)`.
- **Affected:** `Sohba/Controllers/PagesController.cs`
- **Layer:** Web
- **Fix:** Pass current user ID.

### M12. `CommentsController.Delete` Leaks Exception Details
- **Reproduction:** Trigger an exception in comment delete — the raw `ex.Message` is returned to the client.
- **Root cause:** `catch (Exception ex) { return Json(...ex.Message...) }`.
- **Affected:** `Sohba/Controllers/CommentsController.cs`
- **Layer:** Web
- **Fix:** Log the exception, return a generic message.

### M13. `GetPostDetails` Has 4-Level Deep Nested Anonymous Mapping
- **Reproduction:** Open a post modal — the JSON is deeply nested and duplicated.
- **Root cause:** Manual anonymous-type mapping in `PostsController.GetPostDetails`.
- **Affected:** `Sohba/Controllers/PostsController.cs`
- **Layer:** Web
- **Fix:** Use AutoMapper or a proper DTO.

### M14. `BaseController.OnActionExecutionAsync` Runs on EVERY Action
- **Reproduction:** Every request (including Landing, Auth) generates a JWT token and queries recommended groups.
- **Root cause:** The override runs for all controllers, even unauthenticated ones.
- **Affected:** `Sohba/Controllers/BaseController.cs`
- **Layer:** Web
- **Fix:** Only run for authenticated, app-layout controllers.

### M15. `GetCurrentUserId` Uses `Guid.Parse` Without TryParse
- **Reproduction:** A malformed `NameIdentifier` claim throws.
- **Root cause:** `Guid.Parse(userId)` can throw.
- **Affected:** `Sohba/Controllers/BaseController.cs`
- **Layer:** Web
- **Fix:** Use `Guid.TryParse`.

### M16. Notification Polling Every 30s Triggers Heavy Recommended-Groups Query
- **Reproduction:** Stay on any page. Every 30 seconds `header.js` calls `/Notifications/GetUnreadCount`, and each call runs the recommended-groups query (2 DB round-trips + a JWT generation) because `BaseController.OnActionExecutionAsync` runs for ALL actions including this lightweight JSON endpoint.
- **Root cause:** `BaseController.OnActionExecutionAsync` runs on every action unconditionally; `header.js` polls `GetUnreadCount` every 30s.
- **Affected:** `Sohba/Controllers/BaseController.cs`, `Sohba/wwwroot/js/features/header.js`
- **Layer:** Web/Backend
- **Fix:** Skip the recommended-groups/JWT work for JSON/AJAX endpoints, or use SignalR-only updates instead of polling.

---

## 7. Low Severity Bugs / Cosmetic Issues

### L1. Missing CSS Files (404s)
- **Reproduction:** Load any page — console shows 404 for `/css/tailwind.css`, `/css/tailwindcss`, `/css/tw-animate-css`.
- **Root cause:** `_Layout.cshtml` references `~/css/tailwind.css` which doesn't exist; `Landing/Index.cshtml` has `tailwind.config` JS that references non-existent paths.
- **Affected:** `Sohba/Views/Shared/_Layout.cshtml`, `Sohba/Views/Landing/Index.cshtml`
- **Layer:** Web/View
- **Fix:** Remove the missing CSS references or add the files.

### L2. `[cite: 1, 2]` Artifacts in Landing/Index.cshtml
- **Reproduction:** View the landing page source — the JS/C# contains literal `[cite: 1, 2]` tokens (corrupted copy-paste).
- **Root cause:** RAG/copy-paste corruption.
- **Affected:** `Sohba/Views/Landing/Index.cshtml` (lines 11-33, 622-660)
- **Layer:** View
- **Fix:** Remove the `[cite: 1, 2]` artifacts.

### L3. Broken `aria-label` Attributes in _PostCard.cshtml
- **Reproduction:** Inspect the share modal — `aria-label="Share on Facebook>` is missing a closing quote.
- **Affected:** `Sohba/Views/Shared/Partials/_PostCard.cshtml` (lines 484, 489, 494)
- **Layer:** View
- **Fix:** Add closing quotes.

### L4. `likeText` is Always Empty String
- **Reproduction:** The like button text is always empty.
- **Root cause:** `var likeText = currentReaction == "React" ? "" : "";` — both branches are empty.
- **Affected:** `Sohba/Views/Shared/Partials/_PostCard.cshtml` (line 320)
- **Layer:** View
- **Fix:** Set the reaction name as text.

### L5. `0 shares` Hardcoded
- **Reproduction:** Every post shows "0 shares".
- **Root cause:** `@* Will be dynamic later *@` comment.
- **Affected:** `Sohba/Views/Shared/Partials/_PostCard.cshtml` (line 301)
- **Layer:** View
- **Fix:** Implement share count or remove.

### L6. `#1` in "What's your #1 coding tip?" Rendered as Hashtag Link
- **Reproduction:** The seed post "5 Tips for Better Code" renders `#1` as a hashtag link.
- **Root cause:** Regex `#(\w+)` matches `#1`.
- **Affected:** `Sohba/Views/Shared/Partials/_PostCard.cshtml`
- **Layer:** View
- **Fix:** Require hashtags to start with a letter.

### L7. `filterUsers` and `switchTab` Reference Undefined `event`
- **Reproduction:** Click a filter button in Friends — `ReferenceError: event is not defined`.
- **Root cause:** `event` is used without being a parameter.
- **Affected:** `Sohba/wwwroot/js/features/friends.js`
- **Layer:** Frontend
- **Fix:** Pass `event` as a parameter.

### L8. `cancelRequest` Uses `event?.target` Inside `onConfirm` Callback
- **Reproduction:** Cancel a friend request — `event` is out of scope in the callback.
- **Affected:** `Sohba/wwwroot/js/features/friends.js`
- **Layer:** Frontend
- **Fix:** Capture the button reference before the callback.

### L9. `blockUserFromProfile` Uses Native `confirm()` While `blockUser` Uses Custom Modal
- **Reproduction:** Block from profile vs block from list — inconsistent UX.
- **Affected:** `Sohba/wwwroot/js/features/friends.js`
- **Layer:** Frontend
- **Fix:** Unify to use the custom modal.

### L10. `collectRenderedPostIds()` is Never Called
- **Reproduction:** Infinite scroll may re-render duplicate posts.
- **Root cause:** The dedup Set is never populated on first load.
- **Affected:** `Sohba/wwwroot/js/features/feed.js`
- **Layer:** Frontend
- **Fix:** Call `collectRenderedPostIds()` on DOMContentLoaded.

### L11. `GetPostCards` and `LoadMore` are Duplicate Endpoints
- **Reproduction:** Both `/Home/GetPostCards` and `/Home/LoadMore` do the same thing.
- **Affected:** `Sohba/Controllers/HomeController.cs`
- **Layer:** Web
- **Fix:** Consolidate into one.

### L12. `Find` and `Suggestions` are Duplicate Actions
- **Reproduction:** Both `/Friends/Find` and `/Friends/Suggestions` return the same view.
- **Affected:** `Sohba/Controllers/FriendsController.cs`
- **Layer:** Web
- **Fix:** Consolidate.

### L13. `GetTabContent` / `GetAboutTab` / `GetGroupMembers` are Duplicates
- **Affected:** `Sohba/Controllers/GroupsController.cs`
- **Layer:** Web
- **Fix:** Consolidate.

### L14. `ReportPostAsync` and `ReportPostWithDetailsAsync` are Duplicates
- **Affected:** `Sohba.Application/Services/ReportingService.cs`
- **Layer:** Application
- **Fix:** Consolidate.

### L15. `SeedSampleDataAsync` is an Empty Method
- **Affected:** `Sohba.Infrastructure/DBInitializer/DBInitializer.cs`
- **Layer:** Infrastructure
- **Fix:** Remove or implement.

### L16. `AppUserDto--Removed.cs` is a Dead File
- **Affected:** `Sohba.Application/DTOs/UserAggregate/AppUserDto--Removed.cs`
- **Layer:** Application
- **Fix:** Delete.

### L17. `SocialService_Removed.cs` is a Dead File
- **Affected:** `Sohba.Application/Services/SocialService_Removed.cs`
- **Layer:** Application
- **Fix:** Delete.

### L18. Large Commented-Out Code Blocks
- **Reproduction:** `Program.cs` has a full duplicate `Main` method commented out (lines 295-458); `_PostCard.cshtml` has ~140 lines of commented-out modals; `_Header.cshtml` has ~500 lines of commented-out JS.
- **Affected:** `Sohba/Program.cs`, `Sohba/Views/Shared/Partials/_PostCard.cshtml`, `Sohba/Views/Shared/Partials/_Header.cshtml`
- **Layer:** Multiple
- **Fix:** Remove dead code.

### L19. `GetPrivacyIcon` Only Shows Public/Private, Not "Friends"
- **Reproduction:** A Friends-only post shows "Public" icon.
- **Root cause:** `GetPrivacyIcon(bool isPrivate)` only takes a bool.
- **Affected:** `Sohba/Views/Shared/Partials/_PostCard.cshtml`
- **Layer:** View
- **Fix:** Pass the full `PostPrivacy` enum.

### L20. `_Layout.cshtml` Loads `features/dashboard.js` on All Pages
- **Reproduction:** The dashboard JS is loaded on every page using `_Layout`.
- **Affected:** `Sohba/Views/Shared/_Layout.cshtml` (line 40)
- **Layer:** View
- **Fix:** Only load on dashboard pages.

---

## 8. Architecture Issues

1. **`BaseController` uses `HttpContext.RequestServices`** instead of constructor injection (acknowledged in a TODO comment). This is an anti-pattern that makes testing hard and hides dependencies.
2. **`JwtService` is registered as a concrete class** (not an interface) and used directly in `BaseController` — tight coupling.
3. **`UnitOfWork` is registered as `Scoped`** but repositories are also `Scoped` and injected separately — the UoW doesn't truly own the DbContext lifecycle.
4. **`GetProfileAsync` overloads** — one with `(Guid userId)` and one with `(Guid userId, Guid currentUserId)` — the single-arg version defaults to owner, which is a footgun (used in `ProfileController.Edit` and `DashboardController.GetUserDetails` where it's fine, but risky).
5. **`PostService.GetAllPostsAsync` passes `Guid.Empty`** as current user — private posts get filtered out for admin dashboard.
6. **`NotificationService` and `FriendshipService` use `protected readonly ILogger`** — inconsistent with other services using `private readonly`.
7. **`SearchResultDto.TotalCount` is a computed property** — the frontend expects `data.data.totalCount` but the DTO serializes it as `totalCount` (works, but fragile).
8. **`StoryService.CreateStoryAsync` hardcodes `MediaType = "image"`** — no video support despite the UI.
9. **`PostService` has both `GetFeedAsync` (paged) and `GetRecentPostsAsync` (unpaged)** — the latter is used by the dashboard and returns deleted posts.
10. **`InteractionService` has two nearly identical mapping helpers** (`MapPostsWithInteractions` in PostService and `MapPostsToResponse` in InteractionService) — duplicated logic.

---

## 9. Duplicate Logic or Stale Flows

| Duplicate | Files |
|-----------|-------|
| `GetPostCards` vs `LoadMore` | `HomeController.cs` |
| `Find` vs `Suggestions` | `FriendsController.cs` |
| `GetTabContent` vs `GetAboutTab`/`GetGroupMembers` | `GroupsController.cs` |
| `ReportPostAsync` vs `ReportPostWithDetailsAsync` | `ReportingService.cs` |
| `MapPostsWithInteractions` vs `MapPostsToResponse` | `PostService.cs` / `InteractionService.cs` |
| `deletePost` in `sohba-posts.js` vs `features/posts.js` | Frontend |
| `SohbaApp.get` in `sohba-core.js` vs `sohba-posts.js` | Frontend |
| `getNotificationIcon`/`loadNotifications` in `_Header.cshtml` (commented) vs `header.js` | Frontend |
| `SeedSampleDataAsync` (empty) vs `SeedTestUsersAsync` | `DBInitializer.cs` |
| `DismissReport` vs `ResolveReport` (same impl) | `DashboardController.cs` |
| `GetPrivacyIcon` logic duplicated in `_PostCard.cshtml` and `sohba-posts.js` | Frontend |

---

## 10. Performance Concerns

1. **N+1 in `GetCommentDepthAsync`** — one query per depth level.
2. **N+1 in `StoryService.GetStoriesForFeedAsync`** — `GetViewersCountAsync` + `HasUserViewedStoryAsync` per story.
3. **`ToggleSavePost` loads all saved posts** to check one.
4. **`IsFollowingAsync`/`FollowPageAsync` load all followed pages** to check one.
5. **`BaseController` generates a JWT on every request** — expensive crypto on every page load.
6. **`GetUserStories` loads all friend stories** then filters.
7. **No response caching** on feed, search, or profile endpoints.
8. **`GetAllAsync` on Users/Posts/Groups/Pages** loads entire tables into memory for the dashboard, then filters/paginates in memory (not in SQL).
9. **`GetUsersByStatusAsync` loads all users** then filters in memory.
10. **SignalR `_userConnections` is a static ConcurrentDictionary** — not scaled across multiple server instances; single-user-per-connection (multi-tab loses notifications).

---

## 11. Security and Authorization Concerns

1. **CRITICAL: Any user can edit any post** (B2).
2. **CRITICAL: Hardcoded JWT secret + Mailtrap creds + admin/test passwords in source** (B3).
3. **HIGH: Private posts leak in search** (H1).
4. **HIGH: Friends list leaks to non-friends** (H2).
5. **HIGH: `HidePostAsync` has no auth check** (H3).
6. **HIGH: SignalR hub methods spoofable** (H4).
7. **MEDIUM: CSRF missing on several POST endpoints** — `StoriesController.Create/MarkAsViewed/Delete`, `PagesController.Create/Delete/ToggleFollow`, `ProfileController.Deactivate/DeleteAccount`, `DashboardController.*` all lack `[ValidateAntiForgeryToken]`.
8. **MEDIUM: `LoginAsync` doesn't check `IsActive`/`IsDeleted`** — deactivated/deleted users can log in.
9. **MEDIUM: `RegisterAsync` doesn't require email confirmation** — anyone can register with any email.
10. **MEDIUM: `CommentsController.Delete` leaks exception details**.
11. **LOW: `GetCurrentUserId` uses `Guid.Parse`** — can throw on malformed claim.
12. **LOW: `CookieSecurePolicy.Always`** — in dev over HTTP, the cookie may not be set (though it worked in our test because the app used the cookie auth scheme).

---

## 12. Feature-by-Feature Findings

### Authentication
- Login/Register work. No email confirmation. No `IsActive` check. `ForgotPassword`/`ResetPassword` use `[FromBody]` on MVC form posts — likely broken in a real form submission (the view may not send JSON).

### Posts
- Create works. **Edit is broken (AutoMapper crash)**. Delete works for owner/admin (but admin dashboard delete is broken). Feed works but duplicates on restart. Hashtag regex doesn't support Arabic.

### Comments & Replies
- Add/delete work. No privacy check. Blocked-user check hardcoded off. Depth limit enforced but N+1.

### Save & Favorite
- Save/favorite/collections work. `ToggleSavePost` is inefficient. `SavedPosts(tag)` filter is dead.

### Groups
- Create/join/leave/kick work. `GetAboutTab` passes `Guid.Empty`. No membership check on group posts.

### Pages
- Create/follow/unfollow work. **`CreatePageAsync` drops the image URL**. No CSRF on create/delete/toggle-follow.

### Friends & Friend Requests
- Send/accept/reject/cancel/block/unblock work. JS has `event` ReferenceErrors. `CheckStatus` doesn't check blocked status.

### Notifications
- Create/read/delete work. SignalR works but hub methods are spoofable. Multi-tab loses notifications.

### Search
- Works but **leaks private posts**. No pagination.

### Stories
- Create/view/mark-viewed/delete work. No CSRF. `MediaType` always "image". `GetUserStories` inefficient.

### Pagination
- Feed pagination works (Load More). Comments have no pagination. Dashboard pagination is in-memory.

### Modals & Dropdowns
- Confirm/report/share/save modals exist. Share modal has broken `aria-label`s. `toggleMenu` works.

### Repeated Actions & Race Conditions
- `RemoveReactionAsync` not idempotent (double-click error). No optimistic concurrency on posts/comments.

### Delete & Edit Flows
- Post delete works. **Post edit broken**. Comment delete works. Account delete will throw FK violation.

### Empty States
- Feed shows "No posts yet" empty state. Notifications show "No new notifications". Good.

### Error States
- Global exception handler returns generic JSON. But `CommentsController` leaks `ex.Message`.

### Edge Cases
- `#1` rendered as hashtag. Arabic hashtags unsupported. `Guid.Empty` passed in several places.

---

## 13. Realistic User Capacity Estimate

Based on the architecture and code inspection (not load testing):

**Realistic concurrent user estimate: 50–200 concurrent users, 1,000–5,000 total registered users.**

Reasoning:
1. **Single SQL Server instance** with no read replicas or caching layer. The dashboard queries load entire tables into memory (`GetAllAsync`), which will degrade quickly beyond a few thousand rows.
2. **In-memory SignalR** (`ConcurrentDictionary`) — does not scale across multiple app instances. A single instance handles all WebSocket connections.
3. **N+1 query patterns** (comment depth, story viewers) will cause DB saturation under load.
4. **No distributed cache** (Redis) for feed, search, or session.
5. **JWT generated on every request** in `BaseController` adds CPU overhead.
6. **Rate limiting** is configured (60 req/min API, 30 req/min feed) which helps but doesn't solve DB bottlenecks.
7. **No background job infrastructure** for notifications/cleanup beyond a single `NotificationCleanupService` hosted service.

For a production social app, you'd want: Redis cache, read replicas, paginated dashboard queries, distributed SignalR backplane (Redis), and connection pooling tuning. Without these, the app will start failing (slow responses, DB timeouts) well before 1,000 concurrent users.

---

## 14. Final Recommendation

**Do NOT release to production in the current state.**

### Must-fix before any release (Blockers):
1. Fix the AutoMapper crash on post edit (B1).
2. Fix the post-edit authorization bypass (B2).
3. Move all secrets out of source control (B3).
4. Make the DB seeder idempotent (B4).
5. Fix admin dashboard post delete (B5).

### Should-fix before a public beta:
- Fix private-post search leak (H1).
- Fix friends-list leak (H2).
- Add `[ValidateAntiForgeryToken]` to all POST endpoints.
- Enforce `IsActive`/`IsDeleted` on login.
- Remove spoofable SignalR hub methods.
- Fix `GetUsersByStatusAsync("blocked")`.
- Implement cascade delete for account deletion.

### Recommended for a "complete" social app:
- Email confirmation.
- Real-time feed updates via SignalR.
- Comment pagination.
- Arabic hashtag support.
- Share functionality.
- File cleanup on delete.
- Proper caching (Redis) and paginated dashboard queries.

---

*This report is based on static code analysis and live browser testing. All findings are reproducible from the code and observed behavior. No application code was modified during this audit.*