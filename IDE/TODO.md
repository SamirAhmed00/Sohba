# Sohba Project Restoration - Task List

## ✅ Completed (Sprint 1)
- [x] Standardize AJAX Responses — `BaseResponseDto` across `FriendsController`
- [x] Move inline JS to `wwwroot/js/features/` — `Find.cshtml`, `Requests.cshtml`
- [x] Fix Friend Request "User Not Found" — removed redundant user-existence fetch
- [x] Fix `GetSentRequestsAsync` NullRef — added `.Include(f => f.User)`
- [x] Fix `sohba-core.js` SyntaxError — Content-Type guard before `response.json()`
- [x] Add try-catch + null model guard to all AJAX POST actions in `FriendsController`
- [x] Implement Access Control for Groups/Pages — `IsMemberAsync` + `AdminId` check in `PostService`

## 🔴 High Priority (Security & Logic)
- [ ] Fix EF Core Tracking Error in `GroupService.Edit` — see S-09/S-10 in Audit
- [ ] Fix Story creation 500 Error — file I/O must move to `IFileStorageService` (Audit: S-09)
- [ ] Fix Story Timezone calculation — use `DateTimeOffset.UtcNow` (Audit: S-11, RULES §7)
- [ ] Create missing `Search/Index.cshtml` — 404 on `/Search` without query (Audit: S-14)

## 🟡 Medium Priority (Standards & Refactoring)
- [ ] Standardize `SearchController.QuickSearch` — replace anonymous object with `BaseResponseDto<T>` (Audit: S-13)
- [ ] Move inline JS from `Search/Results.cshtml` to `wwwroot/js/features/search.js` (Audit: S-12)
- [ ] Standardize `StoriesController` — replace `new { success, data }` with `BaseResponseDto` (Audit: S-15)
- [ ] Fix Hashtag links in Sidebar (Trends section)
- [ ] Fix Forget Password flow

## 🔵 Features (CRUD)
- [ ] Wire Post Edit/Delete controller actions (service methods exist: `UpdatePostAsync`, `DeletePostAsync`)
- [ ] Implement Comment Delete logic