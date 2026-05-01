# Sohba Project Restoration - Task List

## ✅ Completed (Sprint 1 & Sprint 2)
- [x] Standardize AJAX Responses — `BaseResponseDto` across `FriendsController`
- [x] Move inline JS to `wwwroot/js/features/` — `Find.cshtml`, `Requests.cshtml`
- [x] Fix Friend Request "User Not Found" — removed redundant user-existence fetch
- [x] Fix `GetSentRequestsAsync` NullRef — added `.Include(f => f.User)`
- [x] Fix `sohba-core.js` SyntaxError — Content-Type guard before `response.json()`
- [x] Add try-catch + null model guard to all AJAX POST actions in `FriendsController`
- [x] Implement Access Control for Groups/Pages — `IsMemberAsync` + `AdminId` check in `PostService`
- [x] Fix EF Core Tracking Error in `GroupService.Edit` (added `.AsNoTracking` to `GetAllAsync`)
- [x] Fix Story creation 500 Error & File System Errors — moved file I/O to `IFileStorageService`
- [x] Fix Story Timezone calculation — used `DateTimeOffset.UtcNow` / UTC kind
- [x] Create missing `Search/Index.cshtml` — changed to explicit `View("Results")`
- [x] Move inline JS from `Search/Results.cshtml` and Sidebar to `wwwroot/js/features/`
- [x] Fix Hashtag Navigation in Sidebar
- [x] Implement Post Edit/Delete logic (Controller actions added)

## 🔴 High Priority (Security & Logic)
- [ ] Implement File Validation (5MB limit, image-only) in `IFileStorageService`
- [ ] Fix Post Privacy Logic in UI and Feed
- [ ] Integrate FluentValidation for comments (no empty, 500 chars limit)

## 🟡 Medium Priority (Standards & Refactoring)
- [x] Standardize `SearchController.QuickSearch` — replace anonymous object with `BaseResponseDto<T>` (Audit: S-13)
- [x] Standardize `StoriesController` — replace `new { success, data }` with `BaseResponseDto` (Audit: S-15)
- [ ] Fix Hashtag links in Sidebar (Trends section)
- [x] Fix Forget Password flow

## 🔵 Features (CRUD & Finalization)
- [x] Wire Post Edit/Delete controller actions (service methods exist: `UpdatePostAsync`, `DeletePostAsync`)
- [x] Implement Comment Delete logic
- [ ] Implement dynamic UI updates (decrement Comment Count automatically)
- [ ] Enable Moderator/Admin roles to delete reported/infringing content
