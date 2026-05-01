# 🐛 Social Media Project - UI Testing Bug Report (April 2026)

## 🚨 Priority 1: Security & Data Integrity (Blockers)
- ✅ **RESOLVED** **Group/Page Security:** Unauthorized users can post in Groups/Pages without joining/following.
- ✅ **RESOLVED** **EF Core Tracking Exception:** `InvalidOperationException` in `GroupService.Edit`. (Conflict in IdentityMap while attaching existing entities).
- ✅ **RESOLVED** **File System Errors:** HTTP 500 in Stories/Pages creation when uploading images. Needs migration to `IFileStorageService`.
- ✅ **RESOLVED** **Logic Flaw:** Users can send friend requests to themselves or duplicate requests (Need Domain Validation).
- **File Validation:** No strict file extension or size protection. Arbitrary files can be pushed via `IFileStorageService`.
- **Post Privacy UI/Logic Flaw:** Feed displays incorrect privacy relationships (Public/Private/Friends) and UI doesn't accurately reflect Post status.

## 🛠️ Priority 2: Infrastructure & API Contracts
- ✅ **RESOLVED** **AJAX Response Format:** Some endpoints return anonymous objects or raw strings. Must strictly use `BaseResponseDto`.
- ✅ **RESOLVED** **Routing:** 404 Error when redirecting to `AccessDenied` from Pages.
- **Server Error (HTTP 500):** Occurs during "Block User" action. No Blocked section exists in UI yet.

## 🎨 Priority 3: UI/UX & JavaScript Issues
- **Modal Behavior:** Long comments (1000+ chars) cause horizontal scroll until the modal is reopened.
- ✅ **RESOLVED** **Hashtag Navigation:** Hashtags in the Right Sidebar (Trends) only refresh the page instead of searching.
- ✅ **RESOLVED** **Dropdown Search:** Pressing 'Enter' in Quick Search leads to a missing `Search/Index.cshtml` view.
- **Menu Logic:** Click-outside-to-close is missing for the "Three Dots" menu (Save/Favorite).
- ✅ **RESOLVED** **Sidebar Loading:** Friend suggestions stay in "Loading..." state indefinitely.

## ✨ Priority 4: Missing Features (To be Implemented)
- ✅ **RESOLVED** **Post/Comment Management:** Edit and Delete functionality is missing from the API and UI JS entirely.
- **Page Follow:** Toggle Follow button returns `POST Error: Request failed`.
- ✅ **RESOLVED** **Password Recovery:** Forget Password logic is now implemented utilizing Mailtrap SMTP endpoints.