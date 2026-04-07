# 🐛 Social Media Project - UI Testing Bug Report (April 2026)

## 🚨 Priority 1: Security & Data Integrity (Blockers)
- **Group/Page Security:** Unauthorized users can post in Groups/Pages without joining/following.
- **EF Core Tracking Exception:** `InvalidOperationException` in `GroupService.Edit`. (Conflict in IdentityMap while attaching existing entities).
- **File System Errors:** HTTP 500 in Stories/Pages creation when uploading images. Needs migration to `IFileStorageService`.
- **Logic Flaw:** Users can send friend requests to themselves or duplicate requests (Need Domain Validation).

## 🛠️ Priority 2: Infrastructure & API Contracts
- **AJAX Response Format:** Some endpoints return anonymous objects or raw strings. Must strictly use `BaseResponseDto`.
- **Routing:** 404 Error when redirecting to `AccessDenied` from Pages.
- **Server Error (HTTP 500):** Occurs during "Block User" action. No Blocked section exists in UI yet.

## 🎨 Priority 3: UI/UX & JavaScript Issues
- **Modal Behavior:** Long comments (1000+ chars) cause horizontal scroll until the modal is reopened.
- **Hashtag Navigation:** Hashtags in the Right Sidebar (Trends) only refresh the page instead of searching.
- **Dropdown Search:** Pressing 'Enter' in Quick Search leads to a missing `Search/Index.cshtml` view.
- **Menu Logic:** Click-outside-to-close is missing for the "Three Dots" menu (Save/Favorite).
- **Sidebar Loading:** Friend suggestions stay in "Loading..." state indefinitely.

## ✨ Priority 4: Missing Features (To be Implemented)
- **Post/Comment Management:** Edit and Delete functionality is missing from the UI entirely.
- **Page Follow:** Toggle Follow button returns `POST Error: Request failed`.
- **Password Recovery:** Forget Password logic is not implemented.