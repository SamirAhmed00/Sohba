# Sohba Social Media App — Implementation-Ready Fix Plan

**Source:** `QA_Audit_Report.md`
**Purpose:** Complete, implementation-ready solution for EVERY issue in the audit report.
**Constraint:** No application source code is modified by this document. It is the blueprint for the fixes.
**Method:** Every fix traces the full workflow through the actual codebase (Interface → Service → Repository → Domain → DTO → Mapping → Controller → View → JS → DI → DB). Only layers that actually need changes are included.

---

# Implementation Order

Ordered by priority and dependency. Fixes that must be implemented together are grouped.

## P0 — Blockers (must fix before any release)
| # | Issue | Depends on |
|---|-------|-----------|
| P0-1 | B1: Post edit AutoMapper crash | — |
| P0-2 | B2: Post edit authorization bypass | P0-1 (same workflow) |
| P0-3 | B3: Hardcoded secrets in source | — |
| P0-4 | B4: Non-idempotent DB seeder | — |
| P0-5 | B5: Admin dashboard delete post fails | — |

## P1 — High Severity
| # | Issue | Depends on |
|---|-------|-----------|
| P1-1 | H1: Private posts leak in search | — |
| P1-2 | H2: Friends list leaks to non-friends | — |
| P1-3 | H3: HidePostAsync no auth check | — |
| P1-4 | H4: SignalR hub methods spoofable | — |
| P1-5 | H5: GetUsersByStatusAsync("blocked") broken | — |
| P1-6 | H6: DeleteMyAccountAsync FK violation | — |
| P1-7 | H7: GetRecentAsync returns deleted posts | — |

## P2 — Medium Severity
| # | Issue | Depends on |
|---|-------|-----------|
| P2-1 | M1: RemoveReactionAsync not idempotent | — |
| P2-2 | M2: CanAddReaction never called | — |
| P2-3 | M3: AddCommentAsync hardcodes isBlockedByOwner | — |
| P2-4 | M4: GetCommentsByPostIdAsync no privacy check | — |
| P2-5 | M5: GetCommentDepthAsync N+1 | — |
| P2-6 | M6: PageService.CreatePageAsync drops image URL | — |
| P2-7 | M7: ToggleSavePost loads all saved posts | — |
| P2-8 | M8: SavedPosts(tag) parameter ignored | — |
| P2-9 | M9: GetUserStories fetches all then filters | — |
| P2-10 | M10: GetAboutTab passes Guid.Empty | — |
| P2-11 | M11: GetPageStats passes Guid.Empty | — |
| P2-12 | M12: CommentsController.Delete leaks exception | — |
| P2-13 | M13: GetPostDetails 4-level nested mapping | — |
| P2-14 | M14+M16: BaseController runs on every action + polling | Implement together |
| P2-15 | M15: GetCurrentUserId Guid.Parse | P2-14 (same file) |

## P3 — Low Severity / Cosmetic
| # | Issue | Depends on |
|---|-------|-----------|
| P3-1 | L1: Missing CSS files (404s) | — |
| P3-2 | L2: [cite: 1, 2] artifacts in Landing | — |
| P3-3 | L3: Broken aria-labels in _PostCard | — |
| P3-4 | L4: likeText always empty | — |
| P3-5 | L5: 0 shares hardcoded | — |
| P3-6 | L6: #1 rendered as hashtag | — |
| P3-7 | L7+L8: friends.js event ReferenceErrors | Implement together |
| P3-8 | L9: blockUserFromProfile native confirm | — |
| P3-9 | L10: collectRenderedPostIds never called | — |
| P3-10 | L11: GetPostCards/LoadMore duplicates | — |
| P3-11 | L12: Find/Suggestions duplicates | — |
| P3-12 | L13: GetTabContent/GetAboutTab/GetGroupMembers duplicates | — |
| P3-13 | L14: ReportPostAsync/ReportPostWithDetailsAsync duplicates | — |
| P3-14 | L15: SeedSampleDataAsync empty | P0-4 (same file) |
| P3-15 | L16+L17: Dead files | — |
| P3-16 | L18: Large commented-out code blocks | — |
| P3-17 | L19: GetPrivacyIcon only Public/Private | — |
| P3-18 | L20: _Layout loads dashboard.js everywhere | — |

## P4 — Architecture / Performance
| # | Issue | Depends on |
|---|-------|-----------|
| P4-1 | Arch-1: BaseController RequestServices | P2-14 |
| P4-2 | Arch-2: JwtService concrete class | — |
| P4-3 | Arch-3: UnitOfWork/repo scoping | — |
| P4-4 | Arch-4: GetProfileAsync overloads | — |
| P4-5 | Arch-5: GetAllPostsAsync Guid.Empty | — |
| P4-6 | Arch-6: protected readonly ILogger inconsistency | — |
| P4-7 | Arch-7: SearchResultDto.TotalCount computed | — |
| P4-8 | Arch-8: StoryService MediaType hardcoded | — |
| P4-9 | Arch-9: GetFeedAsync vs GetRecentPostsAsync | P1-7 |
| P4-10 | Arch-10: MapPostsWithInteractions vs MapPostsToResponse | — |
| P4-11 | Perf-1..10: Performance concerns | Various |

---

# P0 — BLOCKERS

---

## B1. Post Editing is Completely Broken (AutoMapper crash)

### Issue
- **Severity:** Critical
- **Blocker:** ✅ YES
- **Verified root cause:** `Sohba.Application/Mappings/MappingProfile.cs` defines `CreateMap<PostCreateDto, Post>()` but **no** `CreateMap<PostUpdateDto, Post>()`. `PostService.UpdatePostAsync` (line 225) calls `_mapper.Map(postDto, post)` which throws `AutoMapperMappingException` at runtime.
- **Affected workflow:** `PostsController.Edit (POST)` → `PostService.UpdatePostAsync` → `_mapper.Map(postDto, post)` → **crash**.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Mappings/MappingProfile.cs` | `MappingProfile` | constructor | ADD `CreateMap<PostUpdateDto, Post>()` |

**Checked but NOT changed:** `PostUpdateDto` (already has `Id`, `Title`, `Content`, `ImageUrl`, `Privacy` — all map to `Post`), `PostService.UpdatePostAsync` (call-site is correct once the map exists), `PostsController.Edit` (POST), `PostEditViewModel`, `Posts/Edit.cshtml`.

### Exact Changes
- **Change type:** ADD
- **File:** `Sohba.Application/Mappings/MappingProfile.cs`
- **Class:** `MappingProfile`
- **Location:** After the `CreateMap<PostCreateDto, Post>();` line (line 30)

### Old Code
```csharp
            // --- Post Mapping ---
            CreateMap<PostCreateDto, Post>();
            CreateMap<Post, PostResponseDto>()
```

### New Code
```csharp
            // --- Post Mapping ---
            CreateMap<PostCreateDto, Post>();
            CreateMap<PostUpdateDto, Post>();
            CreateMap<Post, PostResponseDto>()
```

### Workflow Verification
`PostsController.Edit (POST)` → `PostService.UpdatePostAsync(postId, postDto, userId)` → `_mapper.Map(postDto, post)` → ✅ map now exists → `_unitOfWork.Posts.Update(post)` → `CompleteAsync()` → returns `Result.Success()` → controller returns `BaseResponseDto<PostResponseDto>.SuccessResponse(updatedPost.Value)`.

### Acceptance Criteria
1. Log in → open any post → click Edit → change content → submit → **no exception**.
2. The post card updates in the DOM (via `editPost` in `sohba-posts.js`).
3. Server logs show no `AutoMapperMappingException`.

---

## B2. Any Authenticated User Can Edit Any Post (Authorization Bypass)

### Issue
- **Severity:** Critical
- **Blocker:** ✅ YES
- **Verified root cause:** `IPostDomainService.CanUpdatePost(Guid userId, Guid postId, bool isPostDeleted)` has **no `postOwnerId` parameter**. The implementation `PostDomainService.CanUpdatePost` only checks `isPostDeleted` and never verifies `userId == postOwnerId`. `PostService.UpdatePostAsync` (line 220) calls `_postDomainService.CanUpdatePost(userId, post.UserId, post.IsDeleted)` — passing `post.UserId` into the `postId` slot (wrong signature usage) — so ownership is never checked.
- **Affected workflow:** `PostsController.Edit (GET/POST)` → `PostService.UpdatePostAsync` → `PostDomainService.CanUpdatePost` → **no ownership check**.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Domain/Domain Rules/Interface/IPostDomainService.cs` | `IPostDomainService` | `CanUpdatePost` | ADD `Guid postOwnerId` parameter (mirror `CanDeletePost`) |
| `Sohba.Domain/Domain Rules/Logic/PostDomainService.cs` | `PostDomainService` | `CanUpdatePost` | ADD ownership check |
| `Sohba.Application/Services/PostService.cs` | `PostService` | `UpdatePostAsync` | Fix call-site to pass `post.UserId` as `postOwnerId` |
| `Sohba/Controllers/PostsController.cs` | `PostsController` | `Edit (GET)` | ADD ownership check so non-owners get `Forbid()` instead of the edit form |

**Checked but NOT changed:** `IPostService` (signature `UpdatePostAsync(Guid postId, PostUpdateDto postDto, Guid userId)` stays), `PostUpdateDto`, `MappingProfile` (B1 handles that), `Posts/Edit.cshtml`.

### Exact Changes

#### 1. Interface — `IPostDomainService.cs`
- **Change type:** MODIFY
- **Member:** `CanUpdatePost`

**Old Code**
```csharp
        Result CanUpdatePost(Guid userId, Guid postId, bool isPostDeleted);
```

**New Code**
```csharp
        Result CanUpdatePost(Guid userId, Guid postId, Guid postOwnerId, bool isPostDeleted);
```

#### 2. Implementation — `PostDomainService.cs`
- **Change type:** MODIFY
- **Member:** `CanUpdatePost`

**Old Code**
```csharp
        public Result CanUpdatePost(Guid userId, Guid postId, bool isPostDeleted)
        {
            if (isPostDeleted)
                return Result.Failure("Cannot update a deleted post.");

            // Note: Ownership check usually happens before calling this service or via another parameter, 
            // but here we focus on the state of the post itself as per the interface signature.
            return Result.Success();
        }
```

**New Code**
```csharp
        public Result CanUpdatePost(Guid userId, Guid postId, Guid postOwnerId, bool isPostDeleted)
        {
            if (isPostDeleted)
                return Result.Failure("Cannot update a deleted post.");

            // Owner can update their own post
            if (userId != postOwnerId)
                return Result.Failure("You are not authorized to edit this post.");

            return Result.Success();
        }
```

#### 3. Service call-site — `PostService.cs`
- **Change type:** MODIFY
- **Member:** `UpdatePostAsync`

**Old Code**
```csharp
            // 1. Delegate permission check to Domain Service
            var canUpdate = _postDomainService.CanUpdatePost(userId, post.UserId, post.IsDeleted);
            if (!canUpdate.IsSuccess)
                return canUpdate;
```

**New Code**
```csharp
            // 1. Delegate permission check to Domain Service
            var canUpdate = _postDomainService.CanUpdatePost(userId, post.Id, post.UserId, post.IsDeleted);
            if (!canUpdate.IsSuccess)
                return canUpdate;
```

#### 4. Controller GET — `PostsController.cs`
- **Change type:** MODIFY
- **Member:** `Edit (GET)`

**Old Code**
```csharp
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.GetPostByIdAsync(id, userId);

            if (result.IsFailure || result.Value == null)
            {
                return NotFound();
            }

            var post = result.Value;
```

**New Code**
```csharp
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.GetPostByIdAsync(id, userId);

            if (result.IsFailure || result.Value == null)
            {
                return NotFound();
            }

            var post = result.Value;

            // Authorization: only the author (or an admin) may open the edit form
            if (!post.IsAuthor && !User.IsInRole("Admin"))
                return Forbid();
```

### Workflow Verification
`PostsController.Edit (GET)` → `GetPostByIdAsync` → `IsAuthor` check → `Forbid()` for non-owners. `PostsController.Edit (POST)` → `UpdatePostAsync` → `CanUpdatePost(userId, post.Id, post.UserId, post.IsDeleted)` → ownership enforced → non-owner gets `Result.Failure("You are not authorized to edit this post.")` → controller returns `BaseResponseDto.FailureResponse`.

### Acceptance Criteria
1. User A editing User B's post via `/Posts/Edit/{B-post-id}` → GET returns `403 Forbid`.
2. User A POSTing an edit to User B's post → JSON `{ success: false, error: "You are not authorized to edit this post." }`.
3. Owner editing own post → succeeds.
4. Admin editing any post → succeeds.

---

## B3. Hardcoded Secrets Committed to Source Control

### Issue
- **Severity:** Critical
- **Blocker:** ✅ YES
- **Verified root cause:** `Sohba/appsettings.json` contains the JWT signing key `"YourSuperSecretKeyHereAtLeast32CharactersLong!"` and Mailtrap SMTP credentials. `Sohba.Infrastructure/DBInitializer/DBInitializer.cs` contains the admin password `Admin@123456` and 8 test-user passwords. All are committed to the git repo.
- **Affected workflow:** Any deployment using these defaults is compromised. The JWT key allows forging auth tokens; the SMTP creds allow sending mail as the app.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/appsettings.json` | — | `Jwt:Key`, `MailSettings` | REPLACE with placeholder values; real values move to env vars/user-secrets |
| `Sohba.Infrastructure/DBInitializer/DBInitializer.cs` | `DBInitializer` | `SeedAdminUserAsync`, `SeedTestUsersAsync` | REPLACE hardcoded passwords with env-var reads |
| `.gitignore` | — | — | ADD `appsettings.Production.json` / `.env` if used |

**Checked but NOT changed:** `Program.cs` (reads `Jwt:Key` from config — works with env vars), `JwtService` (reads from `JwtSettings`), `InfrastructureServiceContainer` (reads `MailSettings` from config).

### Exact Changes

#### 1. `appsettings.json`
- **Change type:** REPLACE

**Old Code**
```json
  "Jwt": {
    "Key": "YourSuperSecretKeyHereAtLeast32CharactersLong!",
    "Issuer": "https://localhost:7154",
    "Audience": "https://localhost:7154",
    "ExpireDays": 7
  },
  "MailSettings": {
    "Host": "sandbox.smtp.mailtrap.io",
    "Port": 2525,
    "UserName": "edb4e4eba1d2f0",
    "Password": "0b57dae9a862a3"
  }
```

**New Code**
```json
  "Jwt": {
    "Key": "",
    "Issuer": "https://localhost:7154",
    "Audience": "https://localhost:7154",
    "ExpireDays": 7
  },
  "MailSettings": {
    "Host": "",
    "Port": 2525,
    "UserName": "",
    "Password": ""
  }
```

> **Note:** The empty `Jwt:Key` will cause `Program.cs` line 75 to throw `InvalidOperationException("JWT Key is missing")` at startup — which is the **intended fail-fast** behavior. In development, set the key via `dotnet user-secrets set "Jwt:Key" "<strong-random-key>"` or an environment variable. In production, use Azure Key Vault / environment variables.

#### 2. `DBInitializer.cs` — admin password
- **Change type:** MODIFY
- **Member:** `SeedAdminUserAsync`

**Old Code**
```csharp
                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
```

**New Code**
```csharp
                var adminPassword = Environment.GetEnvironmentVariable("SOHBA_ADMIN_PASSWORD")
                    ?? throw new InvalidOperationException("SOHBA_ADMIN_PASSWORD environment variable is not set.");
                var result = await userManager.CreateAsync(adminUser, adminPassword);
```

#### 3. `DBInitializer.cs` — test user passwords
- **Change type:** MODIFY
- **Member:** `SeedTestUsersAsync` / `CreateUserIfNotExists`

**Old Code**
```csharp
            var mohammed = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "mohammed@sohba.com",
                "Mohammed",
                "Mohammed123!",
                ...
```

**New Code**
```csharp
            var testPassword = Environment.GetEnvironmentVariable("SOHBA_TEST_PASSWORD")
                ?? throw new InvalidOperationException("SOHBA_TEST_PASSWORD environment variable is not set.");

            var mohammed = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "mohammed@sohba.com",
                "Mohammed",
                testPassword,
                ...
```

> **Note:** All 8 test users use the same `testPassword` variable. This keeps the seeder functional in dev while removing hardcoded credentials.

### Workflow Verification
`Program.cs` reads `Jwt:Key` from configuration (env var / user-secret overrides `appsettings.json`). `DBInitializer` reads passwords from environment. No secrets in source.

### Acceptance Criteria
1. `git grep "Admin@123456"` returns nothing.
2. `git grep "YourSuperSecretKeyHere"` returns nothing.
3. App starts with `SOHBA_ADMIN_PASSWORD` and `SOHBA_TEST_PASSWORD` env vars set.
4. App fails fast (clear error) if `Jwt:Key` is empty.

---

## B4. Database Seeder is Non-Idempotent — Duplicates Data on Every Startup

### Issue
- **Severity:** High
- **Blocker:** ✅ YES (data integrity)
- **Verified root cause:** `DBInitializer.CreatePostAsync`, `CreateGroupAsync`, `CreatePageAsync` always insert new rows with random GUIDs. Only `SeedExtraTestDataAsync` guards stories with `if (await _context.Stories.AnyAsync()) return;`. The `CreateRelationshipsAsync` posts are re-created every startup, producing duplicate feed content (verified live: 20 cards, 10 unique posts duplicated).
- **Affected workflow:** App startup → `InitializeAsync` → `SeedTestUsersAsync` → `CreateRelationshipsAsync` → duplicate posts/groups/pages.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Infrastructure/DBInitializer/DBInitializer.cs` | `DBInitializer` | `CreatePostAsync`, `CreateGroupAsync`, `CreatePageAsync`, `AddFriendshipAsync`, `AddGroupMemberAsync`, `AddPageFollowerAsync` | ADD existence checks before insert |

**Checked but NOT changed:** `SeedRolesAsync`, `SeedAdminUserAsync`, `CreateUserIfNotExists` (already idempotent via `FindByEmailAsync`), `SeedExtraTestDataAsync` (already guarded by `Stories.AnyAsync()`).

### Exact Changes

#### 1. `CreatePostAsync` — add existence guard
- **Change type:** MODIFY

**Old Code**
```csharp
        private async Task CreatePostAsync(string title, string content, Guid userId, string? imageUrl, string[] hashtags)
        {
            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                IsHidden = false,
                IsPrivate = false,
                Privacy = PostPrivacy.Public,
                ImageUrl = imageUrl,
                SourceType = PostSourceType.User
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
```

**New Code**
```csharp
        private async Task CreatePostAsync(string title, string content, Guid userId, string? imageUrl, string[] hashtags)
        {
            // Idempotency guard: skip if this exact post already exists for this user
            var exists = await _context.Posts
                .AnyAsync(p => p.Title == title && p.UserId == userId && !p.IsDeleted);
            if (exists)
                return;

            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                IsHidden = false,
                IsPrivate = false,
                Privacy = PostPrivacy.Public,
                ImageUrl = imageUrl,
                SourceType = PostSourceType.User
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
```

#### 2. `CreateGroupAsync` — already has a name-based guard, but verify it covers the admin-member re-add
- **Change type:** MODIFY (strengthen)

**Old Code**
```csharp
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
```

**New Code**
```csharp
            var existing = await _context.Groups
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.Name == name);
            if (existing != null)
            {
                // Idempotency: ensure the admin is a member, then return the existing group
                if (existing.GroupMembers.All(m => m.UserId != adminId))
                {
                    await AddGroupMemberAsync(existing.Id, adminId, GroupRole.Admin);
                }
                return existing;
            }
```

> **Note:** `CreateGroupAsync` already guards by name. The real duplication comes from `CreatePostAsync` (no guard) and `CreatePageAsync` (guards by name but re-adds the admin follower every time — `AddPageFollowerAsync` already checks existence, so this is safe). The critical fix is `CreatePostAsync`.

#### 3. `CreatePageAsync` — verify the name guard is sufficient
- **Change type:** No change needed — `CreatePageAsync` already checks `FirstOrDefaultAsync(p => p.Name == name)` and `AddPageFollowerAsync` already checks existence. Verified safe.

### Workflow Verification
App startup → `InitializeAsync` → `SeedTestUsersAsync` → `CreateRelationshipsAsync` → `CreatePostAsync` skips existing posts → no duplicates. `CreateGroupAsync`/`CreatePageAsync` return existing entities.

### Acceptance Criteria
1. Start the app twice. The feed shows **each seed post exactly once**.
2. `SELECT COUNT(*) FROM Posts WHERE Title = 'Welcome to Sohba! 🚀'` returns **1** after two startups.
3. Groups and pages are not duplicated.

---

## B5. Admin Dashboard "Delete Post" Fails

### Issue
- **Severity:** High
- **Blocker:** ✅ YES (admin moderation broken)
- **Verified root cause:** `DashboardController.DeletePost` calls `_postService.DeletePostAsync(model.postId, GetCurrentUserId())` **without `isAdmin: true`**. `PostDomainService.CanDeletePost` requires `isAdmin` or ownership, so the admin (who isn't the post owner) gets "You are not authorized to delete this post."
- **Affected workflow:** `DashboardController.DeletePost` → `PostService.DeletePostAsync(postId, userId, isAdmin: false)` → `CanDeletePost` → rejected.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/DashboardController.cs` | `DashboardController` | `DeletePost` | Pass `isAdmin: true` |

**Checked but NOT changed:** `PostService.DeletePostAsync` (signature already supports `isAdmin`), `PostDomainService.CanDeletePost` (already correct), `Dashboard/Posts.cshtml`, `dashboard.js`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `DeletePost`

**Old Code**
```csharp
            var result = await _postService.DeletePostAsync(model.postId, GetCurrentUserId());
```

**New Code**
```csharp
            var result = await _postService.DeletePostAsync(model.postId, GetCurrentUserId(), isAdmin: true);
```

### Workflow Verification
`DashboardController.DeletePost` → `DeletePostAsync(postId, adminId, isAdmin: true)` → `CanDeletePost(adminId, postId, postOwnerId, isAdmin: true)` → `if (isAdmin) return Result.Success();` → soft-delete applied.

### Acceptance Criteria
1. Log in as admin → Dashboard → Posts → Delete any post → **succeeds**.
2. The post is soft-deleted (`IsDeleted = true`).
3. Non-admin users still cannot delete others' posts.

---

# P1 — HIGH SEVERITY

---

## H1. Private Posts Leak in Search Results

### Issue
- **Severity:** High
- **Blocker:** No (but serious privacy leak)
- **Verified root cause:** `PostRepository.SearchPostsAsync` has no privacy filter. `SearchService.GlobalSearchAsync` maps all results regardless of the current user's relationship to the post author.
- **Affected workflow:** `SearchController.Index/QuickSearch` → `SearchService.GlobalSearchAsync` → `PostRepository.SearchPostsAsync` → private posts returned.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Domain/Interfaces/IPostRepository.cs` | `IPostRepository` | `SearchPostsAsync` | ADD `Guid currentUserId` parameter |
| `Sohba.Infrastructure/Repositories/PostRepository.cs` | `PostRepository` | `SearchPostsAsync` | ADD privacy filter (public posts OR own posts OR friends' posts) |
| `Sohba.Application/Services/SearchService.cs` | `SearchService` | `GlobalSearchAsync`, `SearchPostsAsync` | Pass `currentUserId` through |

**Checked but NOT changed:** `SearchResultDto`, `PostSearchResultDto`, `SearchController`, `Search/Results.cshtml`, `search.js`.

### Exact Changes

#### 1. Interface — `IPostRepository.cs`
- **Change type:** MODIFY

**Old Code**
```csharp
        Task<IEnumerable<Post>> SearchPostsAsync(string query, int limit = 10);
```

**New Code**
```csharp
        Task<IEnumerable<Post>> SearchPostsAsync(string query, Guid currentUserId, int limit = 10);
```

#### 2. Implementation — `PostRepository.cs`
- **Change type:** MODIFY

**Old Code**
```csharp
        public async Task<IEnumerable<Post>> SearchPostsAsync(string query, int limit = 10)
        {
            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => !p.IsDeleted &&
                           (p.Title.Contains(query) ||
                            p.Content.Contains(query)))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
```

**New Code**
```csharp
        public async Task<IEnumerable<Post>> SearchPostsAsync(string query, Guid currentUserId, int limit = 10)
        {
            // Get the current user's accepted friend IDs for privacy filtering
            var friendIds = await _context.Friends
                .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId)
                            && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.UserId == currentUserId ? f.FriendUserId : f.UserId)
                .ToListAsync();

            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => !p.IsDeleted &&
                           (p.Title.Contains(query) ||
                            p.Content.Contains(query)) &&
                           // Privacy: own posts, public posts, or friends' posts
                           (p.UserId == currentUserId ||
                            p.Privacy == PostPrivacy.Public ||
                            (p.Privacy == PostPrivacy.Friends && friendIds.Contains(p.UserId))))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
```

#### 3. Service — `SearchService.cs`
- **Change type:** MODIFY
- **Members:** `GlobalSearchAsync`, `SearchPostsAsync`

**Old Code**
```csharp
            var posts = await _unitOfWork.Posts.SearchPostsAsync(query, 5);
```
```csharp
        public async Task<Result<List<PostSearchResultDto>>> SearchPostsAsync(string query)
        {
            var posts = await _unitOfWork.Posts.SearchPostsAsync(query);
```

**New Code**
```csharp
            var posts = await _unitOfWork.Posts.SearchPostsAsync(query, currentUserId, 5);
```
```csharp
        public async Task<Result<List<PostSearchResultDto>>> SearchPostsAsync(string query, Guid currentUserId)
        {
            var posts = await _unitOfWork.Posts.SearchPostsAsync(query, currentUserId);
```

> **Note:** `SearchPostsAsync(string query)` in `SearchService` is only called from `GlobalSearchAsync` (which has `currentUserId`). Update the interface `ISearchService.SearchPostsAsync` to accept `currentUserId` too.

### Workflow Verification
`SearchController.Index(q)` → `GlobalSearchAsync(q, userId)` → `SearchPostsAsync(query, userId, 5)` → privacy filter applied → private posts from non-friends excluded.

### Acceptance Criteria
1. User A (not friends with User B) searches for User B's private post content → **not returned**.
2. User A searches for User B's public post → **returned**.
3. User A searches for their own private post → **returned**.

---

## H2. Friends List Leaks to Non-Friends on Profile

### Issue
- **Severity:** High
- **Blocker:** No
- **Verified root cause:** `ProfileController.Index` calls `GetFriendsListAsync(profileUserId)` unconditionally, even when `canViewFriends` is false (non-friend viewing a private account).
- **Affected workflow:** `ProfileController.Index` → `GetFriendsListAsync(profileUserId)` → friends list rendered even for non-friends.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/ProfileController.cs` | `ProfileController` | `Index` | Only fetch friends when `canViewFriends` is true |

**Checked but NOT changed:** `ProfileViewModel` (already has `CanViewFriends`), `Profile/Index.cshtml` (already conditionally renders), `FriendshipService.GetFriendsListAsync`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `Index`

**Old Code**
```csharp
            // Get friends list (may be empty if not allowed to view)
            var friendsResult = await _friendshipService.GetFriendsListAsync(profileUserId);
            var postsResult = await _postService.GetUserPostsAsync(profileUserId, currentUserId);

            // Check if user can view friends list
            var isFriend = await _friendshipService.AreFriendsAsync(currentUserId, profileUserId);
```

**New Code**
```csharp
            // Check if user can view friends list
            var isFriend = await _friendshipService.AreFriendsAsync(currentUserId, profileUserId);

            // Get friends list ONLY if the viewer is allowed to see it
            var friendsResult = isFriend || currentUserId == profileUserId
                ? await _friendshipService.GetFriendsListAsync(profileUserId)
                : Result<IEnumerable<FriendDto>>.Success(new List<FriendDto>());

            var postsResult = await _postService.GetUserPostsAsync(profileUserId, currentUserId);
```

### Workflow Verification
`ProfileController.Index` → compute `isFriend` first → only fetch friends when `isFriend || own profile` → non-friends get an empty list → view renders no friends section.

### Acceptance Criteria
1. Non-friend viewing a private profile → friends list is empty (no data leak).
2. Friend viewing the profile → friends list shown.
3. Owner viewing own profile → friends list shown.

---

## H3. `HidePostAsync` Has No Authorization Check

### Issue
- **Severity:** High
- **Blocker:** No (but authorization hole)
- **Verified root cause:** `PostService.HidePostAsync` never checks ownership or admin role. Any authenticated user can POST `/Dashboard/HidePost` with any postId.
- **Affected workflow:** `DashboardController.HidePost` → `PostService.HidePostAsync` → no auth check.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/PostService.cs` | `PostService` | `HidePostAsync` | ADD ownership/admin check |

**Checked but NOT changed:** `IPostService.HidePostAsync` (signature stays), `DashboardController.HidePost` (controller is `[Authorize(Roles = "Admin")]`, so the caller is already admin — the service should still enforce it defensively).

### Exact Changes
- **Change type:** MODIFY
- **Member:** `HidePostAsync`

**Old Code**
```csharp
        public async Task<Result> HidePostAsync(Guid postId, Guid userId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null)
                return Result.Failure("Post not found");

            post.IsHidden = true; 
            _unitOfWork.Posts.Update(post);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }
```

**New Code**
```csharp
        public async Task<Result> HidePostAsync(Guid postId, Guid userId, bool isAdmin = false)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null)
                return Result.Failure("Post not found");

            // Authorization: only the post owner or an admin can hide a post
            if (!isAdmin && post.UserId != userId)
                return Result.Failure("You are not authorized to hide this post.");

            post.IsHidden = true; 
            _unitOfWork.Posts.Update(post);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }
```

> **Note:** Update `IPostService.HidePostAsync` signature to `Task<Result> HidePostAsync(Guid postId, Guid userId, bool isAdmin = false);` and update `DashboardController.HidePost` to pass `isAdmin: true`.

### Workflow Verification
`DashboardController.HidePost` → `HidePostAsync(postId, adminId, isAdmin: true)` → admin allowed. Any other caller without ownership → rejected.

### Acceptance Criteria
1. Admin hides any post → succeeds.
2. Non-owner, non-admin calling the service directly → `Result.Failure("You are not authorized to hide this post.")`.

---

## H4. SignalR Hub Methods Are Spoofable

### Issue
- **Severity:** High
- **Blocker:** No (but spam/abuse vector)
- **Verified root cause:** `NotificationHub` exposes public methods `SendNotificationToUser`, `SendNotificationToUsers`, `BroadcastNotification` that any authenticated client can invoke to send fake notifications to any user or broadcast to all.
- **Affected workflow:** Any authenticated client → `notificationHub.invoke("SendNotificationToUser", ...)` → spoofed notification.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Hubs/NotificationHub.cs` | `NotificationHub` | `SendNotificationToUser`, `SendNotificationToUsers`, `BroadcastNotification` | DELETE (server-side only via `IHubContext`) |

**Checked but NOT changed:** `NotificationEventHandler` (uses `IHubContext.Clients.User(...)` — the correct server-side path), `NotificationService` (calls `_eventHandler.HandleAsync`), `header.js` (client only listens to `ReceiveNotification`).

### Exact Changes
- **Change type:** DELETE
- **Members:** `SendNotificationToUser`, `SendNotificationToUsers`, `BroadcastNotification`

**Old Code**
```csharp
        // Method to send notification to a specific user
        public async Task SendNotificationToUser(string userId, object notification)
        {
            if (_userConnections.TryGetValue(userId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveNotification", notification);
            }
        }

        // Method to send notification to multiple users
        public async Task SendNotificationToUsers(string[] userIds, object notification)
        {
            var connectionIds = new List<string>();
            foreach (var userId in userIds)
            {
                if (_userConnections.TryGetValue(userId, out var connectionId))
                {
                    connectionIds.Add(connectionId);
                }
            }

            if (connectionIds.Any())
            {
                await Clients.Clients(connectionIds).SendAsync("ReceiveNotification", notification);
            }
        }

        // Method to broadcast to all connected users (admin use)
        public async Task BroadcastNotification(object notification)
        {
            await Clients.All.SendAsync("ReceiveNotification", notification);
        }
```

**New Code**
```csharp
        // (Removed: SendNotificationToUser, SendNotificationToUsers, BroadcastNotification)
        // All notification delivery is server-side only via IHubContext in NotificationEventHandler.
```

### Workflow Verification
`NotificationService.CreateNotificationAsync` → `NotificationEventHandler.HandleAsync` → `_hubContext.Clients.User(receiverId).SendAsync("ReceiveNotification", dto)` → client receives. No client-invokable send methods remain.

### Acceptance Criteria
1. A client calling `notificationHub.invoke("SendNotificationToUser", ...)` → **throws** (method not found).
2. Real notifications still arrive via `ReceiveNotification`.

---

## H5. `GetUsersByStatusAsync("blocked")` is Broken

### Issue
- **Severity:** High
- **Blocker:** No (dashboard feature broken)
- **Verified root cause:** `UserService.GetUsersByStatusAsync` calls `GetBlockedUsersAsync(Guid.Empty)` — passing empty GUID returns nothing (or throws).
- **Affected workflow:** `DashboardController.Users(status="blocked")` → `UserService.GetUsersByStatusAsync("blocked")` → `GetBlockedUsersAsync(Guid.Empty)` → broken.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/UserService.cs` | `UserService` | `GetUsersByStatusAsync` | Fix the `Guid.Empty` call |

**Checked but NOT changed:** `IFriendshipRepository.GetBlockedUsersAsync` (signature takes a userId — correct), `DashboardController.Users` (caller is fine).

### Exact Changes
- **Change type:** MODIFY
- **Member:** `GetUsersByStatusAsync`

**Old Code**
```csharp
                case "active":
                    var blockedUsers = await _friendshipRepository.GetBlockedUsersAsync(Guid.Empty);
                    var blockedIds = blockedUsers.Select(b => b.FriendUserId).ToList();
                    filteredUsers = allUsers.Where(u => !blockedIds.Contains(u.Id));
                    break;

                case "blocked":
                    blockedUsers = await _friendshipRepository.GetBlockedUsersAsync(Guid.Empty);
                    filteredUsers = allUsers.Where(u => blockedUsers.Any(b => b.FriendUserId == u.Id));
                    break;
```

**New Code**
```csharp
                case "active":
                    // "Active" = not soft-deleted (IsDeleted is already filtered by the global query filter)
                    filteredUsers = allUsers.Where(u => !u.IsDeleted);
                    break;

                case "blocked":
                    // Blocked users are those with a Blocked friendship row where they are the target.
                    // Since blocking is per-user, we query all Blocked rows and collect the targets.
                    var allBlocked = await _unitOfWork.Friendships.GetAllBlockedAsync();
                    var blockedIds = allBlocked.Select(b => b.FriendUserId).Distinct().ToList();
                    filteredUsers = allUsers.Where(u => blockedIds.Contains(u.Id));
                    break;
```

> **Note:** This requires a new repository method `GetAllBlockedAsync()` on `IFriendshipRepository`/`FriendshipRepository` that returns all `Friend` rows with `Status == FriendshipStatus.Blocked` (no userId filter). This is the correct semantic for the admin dashboard "blocked users" list.

#### New repository method — `IFriendshipRepository.cs`
- **Change type:** ADD

```csharp
        Task<IEnumerable<Friend>> GetAllBlockedAsync();
```

#### New repository method — `FriendshipRepository.cs`
- **Change type:** ADD

```csharp
        public async Task<IEnumerable<Friend>> GetAllBlockedAsync()
        {
            return await _context.Friends
                .Include(f => f.FriendUser)
                .Where(f => f.Status == FriendshipStatus.Blocked)
                .ToListAsync();
        }
```

### Workflow Verification
`DashboardController.Users(status="blocked")` → `GetUsersByStatusAsync("blocked")` → `GetAllBlockedAsync()` → all blocked targets → filtered users returned.

### Acceptance Criteria
1. Admin Dashboard → Users → filter "Blocked" → shows users who have been blocked.
2. Filter "Active" → shows non-deleted users.
3. No `Guid.Empty` passed to `GetBlockedUsersAsync`.

---

## H6. `DeleteMyAccountAsync` Will Throw FK Constraint Violation

### Issue
- **Severity:** High
- **Blocker:** No (but data-loss/500 risk)
- **Verified root cause:** `UserService.DeleteMyAccountAsync` hard-deletes the user with a comment "NOTE: implement the cascade deletion carefully in the repository" — cascade deletion is NOT implemented. Deleting a user with posts/comments/friendships violates FK constraints.
- **Affected workflow:** `ProfileController.DeleteAccount` → `UserService.DeleteMyAccountAsync` → `_unitOfWork.Users.Delete(user)` → FK violation.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/UserService.cs` | `UserService` | `DeleteMyAccountAsync` | REPLACE hard-delete with soft-delete (`IsDeleted = true`) |

**Checked but NOT changed:** `User.IsDeleted` (already exists), `UserConfiguration` (global query filter on `IsDeleted` already applied — confirmed by the EF warnings at startup), `ProfileController.DeleteAccount`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `DeleteMyAccountAsync`

**Old Code**
```csharp
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

**New Code**
```csharp
        public async Task<Result> DeleteMyAccountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.");

            // Soft-delete the account. The global query filter on IsDeleted
            // automatically excludes this user from all queries.
            user.IsDeleted = true;
            user.IsActive = false;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }
```

### Workflow Verification
`ProfileController.DeleteAccount` → `DeleteMyAccountAsync` → soft-delete → user excluded from all queries via the global `IsDeleted` filter → no FK violations. Related rows (posts, comments, friendships) remain but are orphaned safely (their queries filter by the user's `IsDeleted` via the global filter on `User`).

### Acceptance Criteria
1. User with posts/comments/friendships deletes account → **no exception**.
2. The user can no longer log in (login query filters `IsDeleted`).
3. The user's posts no longer appear in feeds (post queries join `User` which is filtered).

---

## H7. `GetRecentAsync` Returns Deleted Posts

### Issue
- **Severity:** High
- **Blocker:** No
- **Verified root cause:** `PostRepository.GetRecentAsync` has no `!p.IsDeleted` filter.
- **Affected workflow:** `DashboardController.Index` → `PostService.GetRecentPostsAsync(5)` → `PostRepository.GetRecentAsync(5)` → deleted posts shown.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Infrastructure/Repositories/PostRepository.cs` | `PostRepository` | `GetRecentAsync` | ADD `!p.IsDeleted` filter |

**Checked but NOT changed:** `PostService.GetRecentPostsAsync`, `DashboardController.Index`, `Dashboard/Index.cshtml`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `GetRecentAsync`

**Old Code**
```csharp
        public async Task<IEnumerable<Post>> GetRecentAsync(int count)
        {
            return await _context.Set<Post>()
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
```

**New Code**
```csharp
        public async Task<IEnumerable<Post>> GetRecentAsync(int count)
        {
            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
```

### Workflow Verification
`DashboardController.Index` → `GetRecentPostsAsync(5)` → `GetRecentAsync(5)` → only non-deleted posts.

### Acceptance Criteria
1. Soft-delete a post → Dashboard "Recent Posts" no longer shows it.
2. Non-deleted posts still appear.

---

# P2 — MEDIUM SEVERITY

---

## M1. `RemoveReactionAsync` is Not Idempotent

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `InteractionService.RemoveReactionAsync` returns `Result.Failure("No reaction found")` when no reaction exists. Double-clicking the reaction toggle triggers the second call to fail.
- **Affected workflow:** `PostsController.React` → `RemoveReactionAsync` → double-click → error.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/InteractionService.cs` | `InteractionService` | `RemoveReactionAsync` | Return `Success` when no reaction exists |

**Checked but NOT changed:** `IInteractionService.RemoveReactionAsync`, `PostsController.React`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `RemoveReactionAsync`

**Old Code**
```csharp
        public async Task<Result> RemoveReactionAsync(Guid userId, Guid postId)
        {
            var reaction = await _unitOfWork.Interactions.GetReactionAsync(userId, postId);
            if (reaction == null)
                return Result.Failure("No reaction found");

            _unitOfWork.Interactions.RemoveReaction(reaction);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
```

**New Code**
```csharp
        public async Task<Result> RemoveReactionAsync(Guid userId, Guid postId)
        {
            var reaction = await _unitOfWork.Interactions.GetReactionAsync(userId, postId);
            if (reaction == null)
                return Result.Success(); // Idempotent: nothing to remove

            _unitOfWork.Interactions.RemoveReaction(reaction);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
```

### Workflow Verification
`PostsController.React` → `RemoveReactionAsync` → no reaction → `Success` → controller returns `{ success = true, action = "removed", newCount }`.

### Acceptance Criteria
1. Double-click the reaction toggle → no error toast.
2. Second click returns `success: true`.

---

## M2. `CanAddReaction` Domain Rule is Never Called

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `InteractionService.AddReactionAsync` never calls `_interactionDomainService.CanAddReaction`. A blocked user can still react.
- **Affected workflow:** `PostsController.React` → `AddReactionAsync` → no blocked-user check.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/InteractionService.cs` | `InteractionService` | `AddReactionAsync` | ADD domain-rule call |

**Checked but NOT changed:** `IInteractionDomainService.CanAddReaction` (already exists), `PostsController.React`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `AddReactionAsync`

**Old Code**
```csharp
        public async Task<Result> AddReactionAsync(Guid userId, Guid postId, ReactionType type)
        {
            var existingReaction = await _unitOfWork.Interactions.GetReactionAsync(userId, postId);
```

**New Code**
```csharp
        public async Task<Result> AddReactionAsync(Guid userId, Guid postId, ReactionType type)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null)
                return Result.Failure("Post not found.");

            // Domain rule: cannot react to deleted content or if blocked by the post owner
            var isBlocked = await _unitOfWork.Friendships.IsUserBlockedAsync(post.UserId, userId);
            var canReact = _interactionDomainService.CanAddReaction(userId, post.IsDeleted, isBlocked);
            if (!canReact.IsSuccess)
                return canReact;

            var existingReaction = await _unitOfWork.Interactions.GetReactionAsync(userId, postId);
```

> **Note:** `IsUserBlockedAsync(post.UserId, userId)` checks if the **post owner** blocked the **current user**. This matches the domain rule's intent.

### Workflow Verification
`PostsController.React` → `AddReactionAsync` → post loaded → `CanAddReaction` → blocked/deleted → rejected → else reaction added.

### Acceptance Criteria
1. A user blocked by the post owner tries to react → `Result.Failure("You are blocked from interacting with this user.")`.
2. Reacting to a deleted post → `Result.Failure("Cannot react to deleted content.")`.
3. Normal reaction → succeeds.

---

## M3. `AddCommentAsync` Hardcodes `isBlockedByOwner: false`

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `InteractionService.AddCommentAsync` calls `CanAddComment(userId, content, post.IsDeleted, isBlockedByOwner: false)` — the blocked check is hardcoded off.
- **Affected workflow:** `PostsController.Comment` → `AddCommentAsync` → blocked user can comment.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/InteractionService.cs` | `InteractionService` | `AddCommentAsync` | Pass actual blocked status |

**Checked but NOT changed:** `IInteractionDomainService.CanAddComment`, `PostsController.Comment`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `AddCommentAsync`

**Old Code**
```csharp
            var canComment = _interactionDomainService.CanAddComment(userId, content, post.IsDeleted, isBlockedByOwner: false);
```

**New Code**
```csharp
            var isBlockedByOwner = await _unitOfWork.Friendships.IsUserBlockedAsync(post.UserId, userId);
            var canComment = _interactionDomainService.CanAddComment(userId, content, post.IsDeleted, isBlockedByOwner);
```

### Workflow Verification
`PostsController.Comment` → `AddCommentAsync` → blocked status computed → `CanAddComment` → blocked → rejected.

### Acceptance Criteria
1. A user blocked by the post owner tries to comment → `Result.Failure("You cannot comment on this post.")`.
2. Normal comment → succeeds.

---

## M4. `GetCommentsByPostIdAsync` Has No Privacy Check

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `InteractionService.GetCommentsByPostIdAsync` returns comments for any postId without verifying the current user can view the post.
- **Affected workflow:** `PostsController.GetPostDetails` → `GetCommentsByPostIdAsync` → comments on private posts leak.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/InteractionService.cs` | `InteractionService` | constructor | ADD `IPostDomainService` injection |
| `Sohba.Application/Services/InteractionService.cs` | `InteractionService` | `GetCommentsByPostIdAsync` | ADD post-visibility check |

**Checked but NOT changed:** `IInteractionService.GetCommentsByPostIdAsync`, `PostsController.GetPostDetails` (already calls `GetPostByIdAsync` first, but the service should be defensive), `IPostDomainService` (already registered in DI).

### Exact Changes

#### 1. Constructor — inject `IPostDomainService`
- **Change type:** MODIFY

**Old Code**
```csharp
        public InteractionService(
            IUnitOfWork unitOfWork,
            IInteractionDomainService interactionDomainService,
            IMapper mapper,
            INotificationService notificationService,
            IUserService userService,
            ILogger<InteractionService> logger)
        {
            _unitOfWork = unitOfWork;
            _interactionDomainService = interactionDomainService;
            _mapper = mapper;
            _notificationService = notificationService;
            _userService = userService;
            _logger = logger;
        }
```

**New Code**
```csharp
        public InteractionService(
            IUnitOfWork unitOfWork,
            IInteractionDomainService interactionDomainService,
            IPostDomainService postDomainService,
            IMapper mapper,
            INotificationService notificationService,
            IUserService userService,
            ILogger<InteractionService> logger)
        {
            _unitOfWork = unitOfWork;
            _interactionDomainService = interactionDomainService;
            _postDomainService = postDomainService;
            _mapper = mapper;
            _notificationService = notificationService;
            _userService = userService;
            _logger = logger;
        }
```

> **Note:** Add the private field `private readonly IPostDomainService _postDomainService;` alongside the existing `_interactionDomainService` field. DI resolves `IPostDomainService` automatically (it is registered in the Domain DI container).

#### 2. Method — `GetCommentsByPostIdAsync`
- **Change type:** MODIFY

**Old Code**
```csharp
        public async Task<IEnumerable<CommentResponseDto>> GetCommentsByPostIdAsync(Guid postId, Guid currentUserId)
        {
            var comments = await _unitOfWork.Interactions.GetCommentsByPostIdAsync(postId);
```

**New Code**
```csharp
        public async Task<IEnumerable<CommentResponseDto>> GetCommentsByPostIdAsync(Guid postId, Guid currentUserId)
        {
            // Privacy check: verify the current user can view this post
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeleted)
                return new List<CommentResponseDto>();

            var isFriend = await _unitOfWork.Friendships.AreFriendsAsync(currentUserId, post.UserId);
            var canView = _postDomainService.CanViewPost(currentUserId, post.UserId, post.IsPrivate, isFriend);
            if (!canView.IsSuccess)
                return new List<CommentResponseDto>();

            var comments = await _unitOfWork.Interactions.GetCommentsByPostIdAsync(postId);
```

> **Note:** `IPostDomainService` is already registered in DI (used by `PostService`), so no DI container change is needed. Verify no circular dependency: `InteractionService` → `IPostDomainService` (Domain) is a one-way dependency; `PostService` → `IPostDomainService` is also one-way. No cycle.

### Workflow Verification
`PostsController.GetPostDetails` → `GetCommentsByPostIdAsync` → post visibility verified → private post + non-friend → empty comments.

### Acceptance Criteria
1. Non-friend viewing a private post's comments → empty list.
2. Friend/owner viewing → comments returned.

---

## M5. `GetCommentDepthAsync` is N+1

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `InteractionService.GetCommentDepthAsync` walks up `ParentCommentId` one query at a time.
- **Affected workflow:** `AddCommentAsync` (reply) → `GetCommentDepthAsync` → multiple DB round-trips.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/InteractionService.cs` | `InteractionService` | `GetCommentDepthAsync` | Use a single query |

**Checked but NOT changed:** `IInteractionRepository.GetCommentByIdAsync`, `Comment.Depth` (already stored on the entity).

### Exact Changes
- **Change type:** MODIFY
- **Member:** `GetCommentDepthAsync`

**Old Code**
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

**New Code**
```csharp
        // Reads the stored Depth from the parent comment (single query).
        // Depth is persisted on the Comment entity at creation time.
        private async Task<int> GetCommentDepthAsync(Guid commentId)
        {
            var parent = await _unitOfWork.Interactions.GetCommentByIdAsync(commentId);
            return parent?.Depth ?? 0;
        }
```

> **Note:** `Comment.Depth` is already persisted when a comment/reply is created (`Depth = parentDepth + 1` in `AddCommentAsync`). The parent's `Depth` is the correct source — no need to walk the chain.

### Workflow Verification
`AddCommentAsync` (reply) → `GetCommentDepthAsync(parentCommentId)` → single query → `parent.Depth` → `Depth = parentDepth + 1`.

### Acceptance Criteria
1. Replying to a deeply nested comment → exactly **1** DB query for depth (verified in server logs).
2. Depth limit (4) still enforced.

---

## M6. `PageService.CreatePageAsync` Drops the Image URL

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `PageService.CreatePageAsync` creates the `Page` entity but never sets `ImageUrl` from `dto.ImageUrl`.
- **Affected workflow:** `PagesController.Create` → uploads image → `CreatePageAsync` → image URL lost.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/PageService.cs` | `PageService` | `CreatePageAsync` | Set `ImageUrl = dto.ImageUrl` |

**Checked but NOT changed:** `PageCreateDto` (has `ImageUrl`), `PagesController.Create`, `Page` entity (has `ImageUrl`).

### Exact Changes
- **Change type:** MODIFY
- **Member:** `CreatePageAsync`

**Old Code**
```csharp
            var page = new Page
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };
```

**New Code**
```csharp
            var page = new Page
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };
```

### Workflow Verification
`PagesController.Create` → uploads image → `CreatePageAsync(adminId, dto)` → `ImageUrl` persisted → page details show the image.

### Acceptance Criteria
1. Create a page with an image → the image URL is saved.
2. Page details page shows the uploaded image.

---

## M7. `ToggleSavePost` Loads ALL Saved Posts to Check Existence

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `PostsController.ToggleSavePost` calls `GetSavedPostsAsync(userId)` (loads ALL saved posts) then `.FirstOrDefault(sp => sp.Id == request.PostId)`.
- **Affected workflow:** `PostsController.ToggleSavePost` → `GetSavedPostsAsync` → full table scan per toggle.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Interfaces/IInteractionService.cs` | `IInteractionService` | — | ADD `GetSavedPostAsync(Guid userId, Guid postId)` |
| `Sohba.Application/Services/InteractionService.cs` | `InteractionService` | — | ADD `GetSavedPostAsync` |
| `Sohba/Controllers/PostsController.cs` | `PostsController` | `ToggleSavePost` | Use the new method |

**Checked but NOT changed:** `IInteractionRepository.GetSavedPostAsync` (already exists), `SavedPost` entity.

### Exact Changes

#### 1. Interface — `IInteractionService.cs`
- **Change type:** ADD

```csharp
        Task<Result<SavedPostDto?>> GetSavedPostAsync(Guid userId, Guid postId);
```

#### 2. Implementation — `InteractionService.cs`
- **Change type:** ADD

```csharp
        public async Task<Result<SavedPostDto?>> GetSavedPostAsync(Guid userId, Guid postId)
        {
            var saved = await _unitOfWork.Interactions.GetSavedPostAsync(userId, postId);
            if (saved == null)
                return Result<SavedPostDto?>.Success(null);

            var dto = _mapper.Map<SavedPostDto>(saved);
            return Result<SavedPostDto?>.Success(dto);
        }
```

#### 3. Controller — `PostsController.cs`
- **Change type:** MODIFY
- **Member:** `ToggleSavePost`

**Old Code**
```csharp
            var existingSave = (await _interactionService.GetSavedPostsAsync(userId)).Value?
                .FirstOrDefault(sp => sp.Id == request.PostId);

            if (existingSave != null)
```

**New Code**
```csharp
            var existingSave = await _interactionService.GetSavedPostAsync(userId, request.PostId);

            if (existingSave.Value != null)
```

### Workflow Verification
`PostsController.ToggleSavePost` → `GetSavedPostAsync(userId, postId)` → single indexed query → toggle.

### Acceptance Criteria
1. Toggling save on a post → exactly **1** query for the existing save (verified in server logs).
2. Save/unsave still works correctly.

---

## M8. `SavedPosts(string tag)` Parameter is Ignored

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `PostsController.SavedPosts` sets `ViewBag.CurrentTag = tag` but never filters by it.
- **Affected workflow:** `/Posts/SavedPosts?tag=favorites` → tag filter does nothing.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/PostsController.cs` | `PostsController` | `SavedPosts` | Apply the tag filter |

**Checked but NOT changed:** `SavedPostsGroupedDto`, `SavedPosts.cshtml` (renders grouped collections), `IInteractionService.GetSavedPostsGroupedAsync`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `SavedPosts`

**Old Code**
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

**New Code**
```csharp
        [HttpGet]
        public async Task<IActionResult> SavedPosts(string tag = "all")
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.GetSavedPostsGroupedAsync(userId);
            ViewBag.CurrentTag = tag;

            // Apply tag filter: "favorites" shows only the Favorites collection
            if (tag.Equals("favorites", StringComparison.OrdinalIgnoreCase) && result.Value != null)
            {
                result.Value = result.Value.Where(g => g.IsFavorites).ToList();
            }

            return View(result.Value ?? new List<SavedPostsGroupedDto>());
        }
```

### Workflow Verification
`/Posts/SavedPosts?tag=favorites` → `GetSavedPostsGroupedAsync` → filter to favorites → view shows only favorites.

### Acceptance Criteria
1. `/Posts/SavedPosts?tag=favorites` → only the Favorites collection shown.
2. `/Posts/SavedPosts` (default) → all collections shown.

---

## M9. `GetUserStories` Fetches All Friend Stories Then Filters

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `StoriesController.GetUserStories` calls `GetStoriesForFeedAsync(currentUserId)` (loads ALL friend stories) then filters in memory for the target user.
- **Affected workflow:** `StoriesController.GetUserStories` → `GetStoriesForFeedAsync` → wasteful.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Domain/Interfaces/IStoryRepository.cs` | `IStoryRepository` | — | ADD `GetUserStoriesAsync(Guid userId, Guid currentUserId)` |
| `Sohba.Infrastructure/Repositories/StoryRepository.cs` | `StoryRepository` | — | ADD `GetUserStoriesAsync` |
| `Sohba.Application/Interfaces/IStoryService.cs` | `IStoryService` | — | ADD `GetUserStoriesAsync(Guid userId, Guid currentUserId)` |
| `Sohba.Application/Services/StoryService.cs` | `StoryService` | — | ADD `GetUserStoriesAsync` |
| `Sohba/Controllers/StoriesController.cs` | `StoriesController` | `GetUserStories` | Use the new method |

**Checked but NOT changed:** `Story` entity, `StoryResponseDto`, `StoryPrivacy`.

### Exact Changes

#### 1. Repository interface — `IStoryRepository.cs`
- **Change type:** ADD

```csharp
        Task<IEnumerable<Story>> GetUserStoriesAsync(Guid userId, Guid currentUserId);
```

#### 2. Repository — `StoryRepository.cs`
- **Change type:** ADD

```csharp
        public async Task<IEnumerable<Story>> GetUserStoriesAsync(Guid userId, Guid currentUserId)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);

            // Owner always sees their own stories; otherwise only public stories
            // (or friends-only stories if the viewer is a friend) are returned.
            var isFriend = await _context.Friends
                .AnyAsync(f => (f.UserId == currentUserId && f.FriendUserId == userId)
                            || (f.UserId == userId && f.FriendUserId == currentUserId)
                            && f.Status == FriendshipStatus.Accepted);

            return await _context.Stories
                .Include(s => s.User)
                .Where(s => s.UserId == userId &&
                           s.CreatedAt >= cutoffTime &&
                           !s.IsDeleted &&
                           (s.UserId == currentUserId ||
                            s.Privacy == StoryPrivacy.Public ||
                            (s.Privacy == StoryPrivacy.FriendsOnly && isFriend)))
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }
```

#### 3. Service interface — `IStoryService.cs`
- **Change type:** ADD

```csharp
        Task<Result<IEnumerable<StoryResponseDto>>> GetUserStoriesAsync(Guid userId, Guid currentUserId);
```

#### 4. Service — `StoryService.cs`
- **Change type:** ADD

```csharp
        public async Task<Result<IEnumerable<StoryResponseDto>>> GetUserStoriesAsync(Guid userId, Guid currentUserId)
        {
            var stories = await _unitOfWork.Stories.GetUserStoriesAsync(userId, currentUserId);

            var result = new List<StoryResponseDto>();
            foreach (var story in stories)
            {
                var viewersCount = await _unitOfWork.Stories.GetViewersCountAsync(story.Id);
                var hasViewed = await _unitOfWork.Stories.HasUserViewedStoryAsync(story.Id, currentUserId);

                result.Add(new StoryResponseDto
                {
                    Id = story.Id,
                    UserId = story.UserId,
                    Content = story.Content,
                    MediaUrl = story.MediaUrl,
                    MediaType = story.MediaType,
                    UserName = story.User?.Name,
                    UserProfilePicture = story.User?.ProfilePictureUrl,
                    CreatedAt = story.CreatedAt,
                    ExpiresAt = story.ExpiresAt,
                    ViewersCount = viewersCount,
                    HasUserViewed = hasViewed,
                    Privacy = story.Privacy.ToString()
                });
            }

            return Result<IEnumerable<StoryResponseDto>>.Success(result);
        }
```

#### 5. Controller — `StoriesController.cs`
- **Change type:** MODIFY
- **Member:** `GetUserStories`

**Old Code**
```csharp
        [HttpGet]
        public async Task<IActionResult> GetUserStories(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _storyService.GetStoriesForFeedAsync(currentUserId);

            if (result.IsSuccess)
            {
                var userStories = result.Value.Where(s => s.UserId == userId).ToList();
                return Json(BaseResponseDto<IEnumerable<StoryResponseDto>>.SuccessResponse(userStories));
            }

            return Json(BaseResponseDto<IEnumerable<StoryResponseDto>>.SuccessResponse(new List<StoryResponseDto>()));
        }
```

**New Code**
```csharp
        [HttpGet]
        public async Task<IActionResult> GetUserStories(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _storyService.GetUserStoriesAsync(userId, currentUserId);

            if (result.IsSuccess)
            {
                return Json(BaseResponseDto<IEnumerable<StoryResponseDto>>.SuccessResponse(result.Value));
            }

            return Json(BaseResponseDto<IEnumerable<StoryResponseDto>>.SuccessResponse(new List<StoryResponseDto>()));
        }
```

### Workflow Verification
`StoriesController.GetUserStories(userId)` → `StoryService.GetUserStoriesAsync(userId, currentUserId)` → `StoryRepository.GetUserStoriesAsync` → single targeted query.

### Acceptance Criteria
1. Viewing a user's stories → only that user's stories are queried (verified in server logs).
2. Privacy respected: non-friend sees only public stories.

---

## M10. `GetAboutTab` Passes `Guid.Empty` to `GetGroupPostsAsync`

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `GroupsController.GetAboutTab` calls `GetGroupPostsAsync(groupId, Guid.Empty)`.
- **Affected workflow:** `GroupsController.GetAboutTab` → `GetGroupPostsAsync(groupId, Guid.Empty)` → posts count wrong/empty.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/GroupsController.cs` | `GroupsController` | `GetAboutTab` | Pass the current user ID |

**Checked but NOT changed:** `PostService.GetGroupPostsAsync`, `_AboutTab.cshtml`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `GetAboutTab`

**Old Code**
```csharp
            var postsResult = await _postService.GetGroupPostsAsync(groupId, Guid.Empty);
```

**New Code**
```csharp
            var postsResult = await _postService.GetGroupPostsAsync(groupId, GetCurrentUserId());
```

### Workflow Verification
`GroupsController.GetAboutTab` → `GetGroupPostsAsync(groupId, currentUserId)` → correct posts count.

### Acceptance Criteria
1. Group About tab shows the correct posts count.
2. No `Guid.Empty` passed.

---

## M11. `GetPageStats` Passes `Guid.Empty`

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `PagesController.GetPageStats` calls `GetPagePostsAsync(pageId, Guid.Empty)`.
- **Affected workflow:** `PagesController.GetPageStats` → `GetPagePostsAsync(pageId, Guid.Empty)` → posts count wrong.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/PagesController.cs` | `PagesController` | `GetPageStats` | Pass the current user ID |

**Checked but NOT changed:** `PostService.GetPagePostsAsync`, `Pages/Details.cshtml`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `GetPageStats`

**Old Code**
```csharp
            var postsResult = await _postService.GetPagePostsAsync(pageId, Guid.Empty);
```

**New Code**
```csharp
            var postsResult = await _postService.GetPagePostsAsync(pageId, GetCurrentUserId());
```

### Workflow Verification
`PagesController.GetPageStats` → `GetPagePostsAsync(pageId, currentUserId)` → correct posts count.

### Acceptance Criteria
1. Page stats show the correct posts count.
2. No `Guid.Empty` passed.

---

## M12. `CommentsController.Delete` Leaks Exception Details

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `CommentsController.Delete` catches exceptions and returns `ex.Message` to the client.
- **Affected workflow:** `CommentsController.Delete` → exception → raw message leaked.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/CommentsController.cs` | `CommentsController` | `Delete` | Log the exception, return a generic message |

**Checked but NOT changed:** `IInteractionService.DeleteCommentAsync`, `comments.js`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `Delete`

**Old Code**
```csharp
            catch (Exception ex)
            {
                // Global exception handling standard per RULES.md §6
                return Json(BaseResponseDto<object>.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
```

**New Code**
```csharp
            catch (Exception ex)
            {
                // Log the full exception server-side; return a generic message to the client
                Logger.LogError(ex, "Unexpected error deleting comment {CommentId}", request?.Id);
                return Json(BaseResponseDto<object>.FailureResponse("An unexpected error occurred. Please try again."));
            }
```

> **Note:** `CommentsController` inherits `BaseController` which exposes `Logger` (via `HttpContext.RequestServices`). This is available.

### Workflow Verification
`CommentsController.Delete` → exception → logged server-side → generic message returned.

### Acceptance Criteria
1. Trigger an exception → client sees "An unexpected error occurred. Please try again." (no `ex.Message`).
2. Server logs contain the full exception.

---

## M13. `GetPostDetails` Has 4-Level Deep Nested Anonymous Mapping

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `PostsController.GetPostDetails` manually maps posts and comments into 4-level nested anonymous types.
- **Affected workflow:** `PostsController.GetPostDetails` → deeply nested JSON.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/PostsController.cs` | `PostsController` | `GetPostDetails` | Return the DTOs directly (they already serialize correctly) |

**Checked but NOT changed:** `CommentResponseDto` (has `Replies` collection), `PostResponseDto`, `sohba-posts.js` (consumes `result.comment`, `result.post`, `result.comments`).

### Exact Changes
- **Change type:** MODIFY
- **Member:** `GetPostDetails`

**Old Code**
```csharp
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
            });
```

**New Code**
```csharp
            return Json(new
            {
                success = true,
                post = postResult.Value,
                comments = comments
            });
```

> **Note:** `PostResponseDto` and `CommentResponseDto` serialize with camelCase by default in ASP.NET Core JSON options, so the frontend (`sohba-posts.js`) which reads `result.post.id`, `result.comment.id`, `result.comment.userName`, etc. continues to work.

### Workflow Verification
`PostsController.GetPostDetails` → returns DTOs → JSON serialized → frontend reads the same property names.

### Acceptance Criteria
1. Opening a post modal → post and comments render correctly.
2. The JSON payload is flat (no 4-level nesting).

---

## M14 + M16. `BaseController.OnActionExecutionAsync` Runs on EVERY Action + Notification Polling Triggers Heavy Query

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `BaseController.OnActionExecutionAsync` runs for ALL actions (including unauthenticated and JSON/AJAX endpoints), generating a JWT and querying recommended groups. `header.js` polls `/Notifications/GetUnreadCount` every 30s, and each poll triggers the heavy recommended-groups query + JWT generation (confirmed in server logs).
- **Affected workflow:** Every request → `BaseController.OnActionExecutionAsync` → heavy work. Every 30s → notification poll → heavy work again.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/BaseController.cs` | `BaseController` | `OnActionExecutionAsync` | Skip heavy work for unauthenticated users and JSON/AJAX requests |

**Checked but NOT changed:** `header.js` (polling interval is acceptable once the backend is cheap), `NotificationsController.GetUnreadCount`.

### Exact Changes
- **Change type:** MODIFY
- **Member:** `OnActionExecutionAsync`

**Old Code**
```csharp
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = GetCurrentUserId();
            if (userId != Guid.Empty)
            {
                var recommendedGroups = await GroupService.GetRecommendedGroupsAsync(userId, 5);
                ViewBag.RecommendedGroups = recommendedGroups.Value ?? new List<GroupResponseDto>();
                await SetJwtTokenInViewBag();
            }

            await next();
        }
```

**New Code**
```csharp
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = GetCurrentUserId();

            // Skip heavy work for unauthenticated requests and JSON/AJAX endpoints
            var isJsonRequest = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || context.HttpContext.Request.Path.Value?.Contains("/Get", StringComparison.OrdinalIgnoreCase) == true
                || context.HttpContext.Request.Path.Value?.Contains("/Quick", StringComparison.OrdinalIgnoreCase) == true;

            if (userId != Guid.Empty && !isJsonRequest)
            {
                var recommendedGroups = await GroupService.GetRecommendedGroupsAsync(userId, 5);
                ViewBag.RecommendedGroups = recommendedGroups.Value ?? new List<GroupResponseDto>();
                await SetJwtTokenInViewBag();
            }

            await next();
        }
```

> **Note:** This is a pragmatic fix that keeps the existing pattern (no new architecture) while eliminating the heavy work on JSON endpoints. The `GetCurrentUserId` fix (M15) is in the same file and should be applied together.

### Workflow Verification
`/Notifications/GetUnreadCount` (polled every 30s) → `OnActionExecutionAsync` → `isJsonRequest` true → skips recommended-groups query + JWT generation → lightweight response.

### Acceptance Criteria
1. Server logs show NO recommended-groups query when `/Notifications/GetUnreadCount` is polled.
2. Full page loads (Home, Profile, etc.) still populate `ViewBag.RecommendedGroups` and `ViewBag.JwtToken`.

---

## M15. `GetCurrentUserId` Uses `Guid.Parse` Without TryParse

### Issue
- **Severity:** Medium
- **Blocker:** No
- **Verified root cause:** `BaseController.GetCurrentUserId` uses `Guid.Parse(userId)` which throws on a malformed claim.
- **Affected workflow:** Any action with a malformed `NameIdentifier` claim → exception.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/BaseController.cs` | `BaseController` | `GetCurrentUserId` | Use `Guid.TryParse` |

**Checked but NOT changed:** All controllers that call `GetCurrentUserId()` (they already handle `Guid.Empty`).

### Exact Changes
- **Change type:** MODIFY
- **Member:** `GetCurrentUserId`

**Old Code**
```csharp
        protected Guid GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId != null ? Guid.Parse(userId) : Guid.Empty;
        }
```

**New Code**
```csharp
        protected Guid GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var parsed) ? parsed : Guid.Empty;
        }
```

### Workflow Verification
Any action → `GetCurrentUserId()` → malformed claim → `Guid.Empty` instead of exception.

### Acceptance Criteria
1. A malformed `NameIdentifier` claim → no exception, `Guid.Empty` returned.
2. Normal claims → correct user ID.

---

# P3 — LOW SEVERITY / COSMETIC

---

## L1. Missing CSS Files (404s)

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `_Layout.cshtml` references `~/css/tailwind.css` which doesn't exist in `wwwroot/css/`. The `Landing/Index.cshtml` `tailwind.config` JS references non-existent paths.
- **Affected workflow:** Every page load → 404 for `/css/tailwind.css`, `/css/tailwindcss`, `/css/tw-animate-css`.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Views/Shared/_Layout.cshtml` | — | `<link>` tags | DELETE the missing `~/css/tailwind.css` reference |

**Checked but NOT changed:** `wwwroot/css/` (no `tailwind.css` file exists — confirmed), `_AppLayout.cshtml` (uses CDN `https://cdn.tailwindcss.com` — correct).

### Exact Changes
- **Change type:** DELETE
- **File:** `Sohba/Views/Shared/_Layout.cshtml`

**Old Code**
```html
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    <link rel="stylesheet" href="~/css/v0-custom.css" />
    <link rel="stylesheet" href="~/css/tailwind.css" />
    <link rel="stylesheet" href="~/css/legacy.css" />
```

**New Code**
```html
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    <link rel="stylesheet" href="~/css/v0-custom.css" />
    <link rel="stylesheet" href="~/css/legacy.css" />
```

### Workflow Verification
Page load → no 404 for `/css/tailwind.css`.

### Acceptance Criteria
1. Console shows no 404 for CSS files.
2. Page styling is unchanged (Tailwind comes from the CDN).

---

## L2. `[cite: 1, 2]` Artifacts in Landing/Index.cshtml

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `Landing/Index.cshtml` contains literal `[cite: 1, 2]` tokens in the JS/C# (lines 11-33, 622-660) — corrupted copy-paste.
- **Affected workflow:** Landing page JS → syntax errors.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Views/Landing/Index.cshtml` | — | `tailwind.config` JS block, stats observer JS block | DELETE all `[cite: 1, 2]` tokens |

### Exact Changes
- **Change type:** DELETE (multiple occurrences)
- **File:** `Sohba/Views/Landing/Index.cshtml`

**Old Code (example, lines 11-33)**
```javascript
    tailwind.config = {
        theme: {
            extend: {
                colors: {
                    primary: {
                        DEFAULT: '#3d8b8b', [cite: 1, 2]
                        50: '#f0f9f9', [cite: 1, 2]
                        100: '#d9f0f0', [cite: 1, 2]
                        ...
```

**New Code**
```javascript
    tailwind.config = {
        theme: {
            extend: {
                colors: {
                    primary: {
                        DEFAULT: '#3d8b8b',
                        50: '#f0f9f9',
                        100: '#d9f0f0',
                        ...
```

> **Note:** Remove every `[cite: 1, 2]` token throughout the file (lines 11-33 and 622-660). This is a mechanical find-and-replace.

### Workflow Verification
Landing page JS parses without syntax errors.

### Acceptance Criteria
1. `grep -n "\[cite" Sohba/Views/Landing/Index.cshtml` returns nothing.
2. Landing page console shows no "Unexpected token" errors.

---

## L3. Broken `aria-label` Attributes in _PostCard.cshtml

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `_PostCard.cshtml` lines 484, 489, 494 have `aria-label="Share on Facebook>` — missing closing quote.
- **Affected workflow:** Share modal buttons → broken HTML attributes.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Views/Shared/Partials/_PostCard.cshtml` | — | Share modal buttons | ADD closing quotes |

### Exact Changes
- **Change type:** MODIFY (3 lines)

**Old Code**
```html
<button class="p-3 bg-blue-600 text-white rounded-full hover:scale-110 transition-transform" title="Share on Facebook" aria-label="Share on Facebook>
<button class="p-3 bg-sky-500 text-white rounded-full hover:scale-110 transition-transform" title="Share on Twitter" aria-label="Share on Twitter>
<button class="p-3 bg-green-600 text-white rounded-full hover:scale-110 transition-transform" title="Share on WhatsApp" aria-label="Share on WhatsApp>
```

**New Code**
```html
<button class="p-3 bg-blue-600 text-white rounded-full hover:scale-110 transition-transform" title="Share on Facebook" aria-label="Share on Facebook">
<button class="p-3 bg-sky-500 text-white rounded-full hover:scale-110 transition-transform" title="Share on Twitter" aria-label="Share on Twitter">
<button class="p-3 bg-green-600 text-white rounded-full hover:scale-110 transition-transform" title="Share on WhatsApp" aria-label="Share on WhatsApp">
```

### Workflow Verification
Share modal HTML is well-formed.

### Acceptance Criteria
1. Inspect the share modal → all `aria-label` attributes have closing quotes.

---

## L4. `likeText` is Always Empty String

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `_PostCard.cshtml` line 320: `var likeText = currentReaction == "React" ? "" : "";` — both branches are empty.
- **Affected workflow:** Like button text never shows the reaction name.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Views/Shared/Partials/_PostCard.cshtml` | — | Like button | Set the reaction name as text |

### Exact Changes
- **Change type:** MODIFY

**Old Code**
```csharp
                                var likeText = currentReaction == "React" ? "" : "";
```

**New Code**
```csharp
                                var likeText = currentReaction == "React" ? "" : currentReaction;
```

### Workflow Verification
Like button shows the reaction name (e.g., "Like", "Love") when a reaction exists.

### Acceptance Criteria
1. React to a post → the button text shows the reaction name.

---

## L5. `0 shares` Hardcoded

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `_PostCard.cshtml` line 301: `<span>0 shares</span> @* Will be dynamic later *@`.
- **Affected workflow:** Every post shows "0 shares".

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Views/Shared/Partials/_PostCard.cshtml` | — | Engagement stats | Remove the hardcoded share count (no share feature exists) |

### Exact Changes
- **Change type:** DELETE

**Old Code**
```html
                            <span class="w-1 h-1 bg-slate-300 rounded-full"></span>
                            <span>0 shares</span> @* Will be dynamic later *@
```

**New Code**
```html
                            <span class="w-1 h-1 bg-slate-300 rounded-full"></span>
```

### Workflow Verification
Post cards no longer show a misleading "0 shares".

### Acceptance Criteria
1. Post cards show only comments count (no fake share count).

---

## L6. `#1` in "What's your #1 coding tip?" Rendered as Hashtag Link

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `_PostCard.cshtml` regex `#(\w+)` matches `#1`.
- **Affected workflow:** Hashtag rendering turns `#1` into a link.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Views/Shared/Partials/_PostCard.cshtml` | — | Hashtag regex | Require hashtags to start with a letter |

### Exact Changes
- **Change type:** MODIFY

**Old Code**
```csharp
                            var contentWithLinks = Regex.Replace(
                            post.Content ?? "",
                                                    @"#(\w+)",
                                                "<a href='/Posts/Hashtag?tag=$1' class='text-[#345e69] hover:underline font-bold'>#$1</a>"
                            );
```

**New Code**
```csharp
                            var contentWithLinks = Regex.Replace(
                            post.Content ?? "",
                                                    @"#([A-Za-z_][A-Za-z0-9_]*)",
                                                "<a href='/Posts/Hashtag?tag=$1' class='text-[#345e69] hover:underline font-bold'>#$1</a>"
                            );
```

> **Note:** This also fixes the Arabic-hashtag limitation partially (English hashtags must start with a letter). Full Arabic support requires a Unicode-aware regex — see the "Missing Features" note in the audit. For this fix, the priority is preventing `#1` from becoming a link.

### Workflow Verification
`#1` in post content → not linked. `#Coding` → linked.

### Acceptance Criteria
1. "What's your #1 coding tip?" renders `#1` as plain text.
2. `#Coding` still renders as a link.

---

## L7 + L8. friends.js `event` ReferenceErrors

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `filterUsers` and `switchTab` use `event` without it being a parameter (ReferenceError). `cancelRequest` uses `event?.target` inside the `onConfirm` callback where `event` is out of scope.
- **Affected workflow:** Friends filter buttons, tab switching, cancel request.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/wwwroot/js/features/friends.js` | — | `filterUsers`, `switchTab`, `cancelRequest` | Pass `event` explicitly / capture button reference |

### Exact Changes

#### 1. `filterUsers`
- **Change type:** MODIFY

**Old Code**
```javascript
function filterUsers(filter) {
    // Update active button
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.classList.remove('active', 'bg-[#345e69]', 'text-white');
        btn.classList.add('bg-slate-100', 'text-gray-700');
    });
    const target = event.target;
```

**New Code**
```javascript
function filterUsers(filter, event) {
    // Update active button
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.classList.remove('active', 'bg-[#345e69]', 'text-white');
        btn.classList.add('bg-slate-100', 'text-gray-700');
    });
    const target = event.target;
```

> **Note:** The HTML callers must pass `event`: `onclick="filterUsers('all', event)"`.

#### 2. `switchTab`
- **Change type:** MODIFY

**Old Code**
```javascript
function switchTab(tab) {
    console.log('Switching to tab:', tab);
    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active', 'border-[#345e69]', 'text-[#345e69]');
        btn.classList.add('border-transparent', 'text-gray-400');
    });

    const target = event.target || event.currentTarget;
```

**New Code**
```javascript
function switchTab(tab, event) {
    console.log('Switching to tab:', tab);
    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active', 'border-[#345e69]', 'text-[#345e69]');
        btn.classList.add('border-transparent', 'text-gray-400');
    });

    const target = event.target || event.currentTarget;
```

> **Note:** The HTML callers must pass `event`: `onclick="switchTab('pending', event)"`.

#### 3. `cancelRequest`
- **Change type:** MODIFY

**Old Code**
```javascript
async function cancelRequest(userId) {
    window.showConfirmModal({
        title: 'Cancel Friend Request',
        message: 'Are you sure you want to cancel this friend request?',
        type: 'warning',
        confirmText: 'Cancel Request',
        onConfirm: async () => {
            const btn = event?.target;
```

**New Code**
```javascript
async function cancelRequest(userId, btn) {
    window.showConfirmModal({
        title: 'Cancel Friend Request',
        message: 'Are you sure you want to cancel this friend request?',
        type: 'warning',
        confirmText: 'Cancel Request',
        onConfirm: async () => {
            if (btn) { btn.disabled = true; btn.innerHTML = 'Cancelling...'; }
```

> **Note:** The HTML callers must pass the button: `onclick="cancelRequest('@request.FriendUserId', this)"`.

### Workflow Verification
Friends filter/tab/cancel → no ReferenceError.

### Acceptance Criteria
1. Clicking a filter button → no console error.
2. Switching tabs → no console error.
3. Cancelling a request → button disables correctly.

---

## L9. `blockUserFromProfile` Uses Native `confirm()` While `blockUser` Uses Custom Modal

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `friends.js` has two block implementations with inconsistent UX.
- **Affected workflow:** Block from profile vs block from list.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/wwwroot/js/features/friends.js` | — | `blockUserFromProfile` | Use the custom modal |

### Exact Changes
- **Change type:** MODIFY

**Old Code**
```javascript
async function blockUserFromProfile(userId) {
    if (!confirm('Are you sure you want to block this user?')) return;

    try {
        const result = await SohbaApp.post('/Friends/BlockUser', { userId });
        if (result.success) {
            SohbaApp.toast('User blocked', 'success');
            setTimeout(() => window.location.reload(), 800);
        } else {
            SohbaApp.toast(result.error || 'Failed to block user', 'error');
        }
    } catch (error) {
        console.error('Block error:', error);
        SohbaApp.toast('Network error', 'error');
    }
}
```

**New Code**
```javascript
async function blockUserFromProfile(userId) {
    window.showConfirmModal({
        title: 'Block User',
        message: 'Are you sure you want to block this user? They will no longer be able to interact with you.',
        type: 'warning',
        confirmText: 'Block',
        onConfirm: async () => {
            try {
                const result = await SohbaApp.post('/Friends/BlockUser', { userId });
                if (result.success) {
                    SohbaApp.toast('User blocked', 'success');
                    setTimeout(() => window.location.reload(), 800);
                } else {
                    SohbaApp.toast(result.error || 'Failed to block user', 'error');
                }
            } catch (error) {
                console.error('Block error:', error);
                SohbaApp.toast('Network error', 'error');
            }
        }
    });
}
```

### Workflow Verification
Block from profile → custom modal → consistent UX.

### Acceptance Criteria
1. Blocking from profile uses the same modal as blocking from the list.

---

## L10. `collectRenderedPostIds()` is Never Called

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `feed.js` defines `collectRenderedPostIds()` but never calls it, so the dedup Set is empty on first load.
- **Affected workflow:** Infinite scroll may re-render duplicate posts.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/wwwroot/js/features/feed.js` | — | `DOMContentLoaded` handler | Call `collectRenderedPostIds()` |

### Exact Changes
- **Change type:** MODIFY

**Old Code**
```javascript
document.addEventListener('DOMContentLoaded', function () {
    // Get initial page from URL or default to 1
    const urlParams = new URLSearchParams(window.location.search);
    currentPage = parseInt(urlParams.get('page')) || 1;
```

**New Code**
```javascript
document.addEventListener('DOMContentLoaded', function () {
    // Get initial page from URL or default to 1
    const urlParams = new URLSearchParams(window.location.search);
    currentPage = parseInt(urlParams.get('page')) || 1;

    // Populate the dedup Set with already-rendered post IDs
    collectRenderedPostIds();
```

### Workflow Verification
Infinite scroll → dedup Set populated → no duplicate cards.

### Acceptance Criteria
1. Load more posts → no duplicate post cards rendered.

---

## L11. `GetPostCards` and `LoadMore` are Duplicate Endpoints

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `HomeController` has both `GetPostCards` (returns HTML partial) and `LoadMore` (returns JSON posts) doing the same pagination.
- **Affected workflow:** Infinite scroll uses `GetPostCards`; `LoadMore` is dead code.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/HomeController.cs` | `HomeController` | `LoadMore` | DELETE (dead code — `feed.js` uses `GetPostCards`) |

**Checked but NOT changed:** `feed.js` (uses `/Home/GetPostCards`), `GetPostCards` (keep).

### Exact Changes
- **Change type:** DELETE
- **Member:** `LoadMore`

**Old Code**
```csharp
        //  NEW: Load more posts via AJAX (for infinite scroll)
        [HttpGet]
        public async Task<IActionResult> LoadMore(int page = 2, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.GetFeedAsync(userId, page, pageSize);

            if (result.IsFailure)
                return Json(new { success = false, error = result.Error });

            return Json(new
            {
                success = true,
                posts = result.Value.Items,
                hasMore = result.Value.HasNextPage,
                currentPage = result.Value.Page,
                totalPages = result.Value.TotalPages
            });
        }
```

**New Code**
```csharp
        // (Removed: LoadMore — GetPostCards is the single infinite-scroll endpoint)
```

### Workflow Verification
Infinite scroll → `GetPostCards` → works. `LoadMore` no longer exists.

### Acceptance Criteria
1. Infinite scroll still works.
2. No references to `/Home/LoadMore` remain.

---

## L12. `Find` and `Suggestions` are Duplicate Actions

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `FriendsController.Find` and `Suggestions` both return the same suggestions view.
- **Affected workflow:** Two routes to the same page.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/FriendsController.cs` | `FriendsController` | `Find` | DELETE (keep `Suggestions`) |

**Checked but NOT changed:** `Friends/Suggestions.cshtml`, `Friends/Find.cshtml` (both exist — keep `Suggestions.cshtml`), sidebar links.

### Exact Changes
- **Change type:** DELETE
- **Member:** `Find`

**Old Code**
```csharp
        [HttpGet]
        public async Task<IActionResult> Find()
        {
            var userId = GetCurrentUserId();
            var suggestions = await _friendshipService.GetFriendSuggestionsAsync(userId, 20);
            return View(suggestions.Value);
        }
```

**New Code**
```csharp
        // (Removed: Find — use Suggestions instead)
```

> **Note:** Update any links pointing to `/Friends/Find` to `/Friends/Suggestions`.

### Workflow Verification
`/Friends/Suggestions` → works. `/Friends/Find` → 404 (or redirect).

### Acceptance Criteria
1. Suggestions page works.
2. No links point to `/Friends/Find`.

---

## L13. `GetTabContent` / `GetAboutTab` / `GetGroupMembers` are Duplicates

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `GroupsController` has three overlapping tab-content actions.
- **Affected workflow:** Group tabs.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/GroupsController.cs` | `GroupsController` | `GetTabContent`, `GetAboutTab`, `GetGroupMembers` | Consolidate into `GetTabContent` |

**Checked but NOT changed:** `_MembersTab.cshtml`, `_AboutTab.cshtml`, `groups.js` (check which endpoints it calls).

### Exact Changes
- **Change type:** DELETE `GetAboutTab` and `GetGroupMembers`; keep `GetTabContent` (which already handles `members` and `about` tabs).

**Old Code (GetAboutTab)**
```csharp
        [HttpGet]
        public async Task<IActionResult> GetAboutTab(Guid groupId)
        {
            var groupResult = await _groupService.GetGroupByIdAsync(groupId);
            if (!groupResult.IsSuccess)
                return Content($"<div class='text-center py-10 text-red-500'>Group not found</div>");

            var membersResult = await _groupService.GetGroupMembersAsync(groupId);
            var postsResult = await _postService.GetGroupPostsAsync(groupId, Guid.Empty);
            var postsCount = postsResult.IsSuccess ? postsResult.Value?.Count() ?? 0 : 0;

            var viewModel = new
            {
                Group = groupResult.Value,
                Members = membersResult.Value ?? new List<GroupMemberDto>(),
                PostsCount = postsCount
            };

            ViewBag.GroupId = groupId;
            return PartialView("_AboutTab", viewModel);
        }
```

**New Code**
```csharp
        // (Removed: GetAboutTab — GetTabContent handles the "about" tab)
```

**Old Code (GetGroupMembers)**
```csharp
        [HttpGet]
        public async Task<IActionResult> GetGroupMembers(Guid groupId)
        {
            var membersResult = await _groupService.GetGroupMembersAsync(groupId);

            if (!membersResult.IsSuccess)
                return Content($"<div class='text-center py-10 text-red-500'>{membersResult.Error}</div>");

            ViewBag.GroupId = groupId;
            return PartialView("_MembersTab", membersResult.Value ?? new List<GroupMemberDto>());
        }
```

**New Code**
```csharp
        // (Removed: GetGroupMembers — GetTabContent handles the "members" tab)
```

> **Note:** Update `GetTabContent`'s "about" branch to pass the current user ID (M10 fix) and set `ViewBag.PostsCount` correctly.

### Workflow Verification
Group tabs → `GetTabContent?tab=members|about` → works.

### Acceptance Criteria
1. Group members tab works.
2. Group about tab works.
3. No references to `/Groups/GetAboutTab` or `/Groups/GetGroupMembers` remain.

---

## L14. `ReportPostAsync` and `ReportPostWithDetailsAsync` are Duplicates

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `ReportingService` has two nearly identical report methods.
- **Affected workflow:** `PostsController.ReportPost` uses `ReportPostWithDetailsAsync`; `ReportPostAsync` is dead code.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/ReportingService.cs` | `ReportingService` | `ReportPostAsync` | DELETE (dead code — controller uses `ReportPostWithDetailsAsync`) |

**Checked but NOT changed:** `IReportingService` (remove `ReportPostAsync` from interface too), `PostsController.ReportPost` (uses `ReportPostWithDetailsAsync`).

### Exact Changes
- **Change type:** DELETE
- **Member:** `ReportPostAsync`

**Old Code**
```csharp
        public async Task<Result> ReportPostAsync(PostReportRequestDto reportDto, Guid reporterId)
        {
            // ... (full duplicate implementation)
        }
```

**New Code**
```csharp
        // (Removed: ReportPostAsync — ReportPostWithDetailsAsync is the single implementation)
```

> **Note:** Also remove `Task<Result> ReportPostAsync(...)` from `IReportingService`.

### Workflow Verification
`PostsController.ReportPost` → `ReportPostWithDetailsAsync` → works.

### Acceptance Criteria
1. Reporting a post works.
2. No references to `ReportPostAsync` remain.

---

## L15. `SeedSampleDataAsync` is an Empty Method

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `DBInitializer.SeedSampleDataAsync` is empty (`await Task.CompletedTask;`).
- **Affected workflow:** Startup seeding.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Infrastructure/DBInitializer/DBInitializer.cs` | `DBInitializer` | `SeedSampleDataAsync` | DELETE (dead code) |

### Exact Changes
- **Change type:** DELETE
- **Member:** `SeedSampleDataAsync`

**Old Code**
```csharp
        private async Task SeedSampleDataAsync()
        {
            // This is now handled by SeedTestUsersAsync
            await Task.CompletedTask;
        }
```

**New Code**
```csharp
        // (Removed: SeedSampleDataAsync — seeding is handled by SeedTestUsersAsync + SeedExtraTestDataAsync)
```

> **Note:** Also remove the `await SeedSampleDataAsync();` call in `InitializeAsync`.

### Workflow Verification
Startup → `InitializeAsync` → no empty method call.

### Acceptance Criteria
1. App starts without calling the empty method.

---

## L16 + L17. Dead Files

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `AppUserDto--Removed.cs` and `SocialService_Removed.cs` are dead files.
- **Affected workflow:** None (dead code).

### Files To Change
| Full path | Why |
|-----------|-----|
| `Sohba.Application/DTOs/UserAggregate/AppUserDto--Removed.cs` | DELETE |
| `Sohba.Application/Services/SocialService_Removed.cs` | DELETE |
| `Sohba.Application/Interfaces/ISocialService_Removed.cs` | DELETE |

### Exact Changes
- **Change type:** DELETE (3 files)

### Workflow Verification
Build succeeds without the dead files.

### Acceptance Criteria
1. `dotnet build` succeeds.
2. No references to `AppUserDto` or `ISocialService` remain.

---

## L18. Large Commented-Out Code Blocks

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `Program.cs` has a full duplicate `Main` method commented out (lines 295-458); `_PostCard.cshtml` has ~140 lines of commented-out modals; `_Header.cshtml` has ~500 lines of commented-out JS.
- **Affected workflow:** None (dead code).

### Files To Change
| Full path | Why |
|-----------|-----|
| `Sohba/Program.cs` | DELETE commented-out `Main` method (lines 295-458) |
| `Sohba/Views/Shared/Partials/_PostCard.cshtml` | DELETE commented-out Report/Share modals (lines 363-503) |
| `Sohba/Views/Shared/Partials/_Header.cshtml` | DELETE commented-out notification JS (lines 227-737) |

### Exact Changes
- **Change type:** DELETE (large commented blocks)

### Workflow Verification
Build succeeds; views render correctly.

### Acceptance Criteria
1. `dotnet build` succeeds.
2. No large commented-out blocks remain.

---

## L19. `GetPrivacyIcon` Only Shows Public/Private, Not "Friends"

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `_PostCard.cshtml` `GetPrivacyIcon(bool isPrivate)` only takes a bool, so a Friends-only post shows the "Public" icon.
- **Affected workflow:** Post privacy badge.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Views/Shared/Partials/_PostCard.cshtml` | — | `GetPrivacyIcon` | Accept the full `PostPrivacy` enum |

### Exact Changes
- **Change type:** MODIFY

**Old Code**
```csharp
    private string GetPrivacyIcon(bool isPrivate)
    {
        return isPrivate ?
            "<svg ...> Private" :
            "<svg ...> Public";
    }
```

**New Code**
```csharp
    private string GetPrivacyIcon(PostPrivacy privacy)
    {
        return privacy switch
        {
            PostPrivacy.Private => "<svg ...> Private",
            PostPrivacy.Friends => "<svg ...> Friends",
            _ => "<svg ...> Public"
        };
    }
```

> **Note:** Update the call-site `@Html.Raw(GetPrivacyIcon(post.IsPrivate))` to `@Html.Raw(GetPrivacyIcon(post.Privacy))`.

### Workflow Verification
Post privacy badge shows the correct label for Public/Friends/Private.

### Acceptance Criteria
1. A Friends-only post shows "Friends" badge.
2. Public/Private posts show correct badges.

---

## L20. `_Layout.cshtml` Loads `features/dashboard.js` on All Pages

### Issue
- **Severity:** Low
- **Blocker:** No
- **Verified root cause:** `_Layout.cshtml` line 40 loads `~/js/features/dashboard.js` on every page using `_Layout`.
- **Affected workflow:** Unnecessary JS on non-dashboard pages.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Views/Shared/_Layout.cshtml` | — | script tag | DELETE the dashboard.js reference |

**Checked but NOT changed:** `Dashboard/Index.cshtml` (should load dashboard.js in its own `@section Scripts`).

### Exact Changes
- **Change type:** DELETE

**Old Code**
```html
    <script src="~/js/features/dashboard.js" asp-append-version="true"></script> // I Puted It By MMesylf -- Check 1.2 
```

**New Code**
```html
    <!-- dashboard.js is loaded only on Dashboard pages via @section Scripts -->
```

> **Note:** Add `<script src="~/js/features/dashboard.js" asp-append-version="true"></script>` inside `@section Scripts` in `Dashboard/Index.cshtml`, `Dashboard/Users.cshtml`, `Dashboard/Posts.cshtml`, `Dashboard/Reports.cshtml`.

### Workflow Verification
Dashboard pages load dashboard.js; other pages don't.

### Acceptance Criteria
1. Home page does NOT load dashboard.js.
2. Dashboard pages DO load dashboard.js.

---

# P4 — ARCHITECTURE / PERFORMANCE

---

## Arch-1. `BaseController` Uses `HttpContext.RequestServices`

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `BaseController` resolves `IGroupService`, `ILogger`, `JwtService`, `UserManager` via `HttpContext.RequestServices` instead of constructor injection.
- **Affected workflow:** All controllers inheriting `BaseController`.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba/Controllers/BaseController.cs` | `BaseController` | constructor | ADD constructor injection for `IGroupService`, `ILogger<BaseController>`, `JwtService`, `UserManager<User>` |

### Exact Changes
- **Change type:** MODIFY

**Old Code**
```csharp
    public class BaseController : Controller
    {
        private IGroupService _groupService;

        protected IGroupService GroupService =>
            _groupService ??= HttpContext.RequestServices.GetRequiredService<IGroupService>();

        // ----- TODO: i Will Make It Injected In Constructor And Make All Controlles That Inherit From BaseController To Use Constructor Injection Instead Of Using RequestServices -----
        protected ILogger<BaseController> Logger =>
     HttpContext.RequestServices.GetRequiredService<ILogger<BaseController>>();
```

**New Code**
```csharp
    public class BaseController : Controller
    {
        protected readonly IGroupService GroupService;
        protected readonly ILogger<BaseController> Logger;
        protected readonly JwtService JwtService;
        protected readonly UserManager<User> UserManager;

        public BaseController(
            IGroupService groupService,
            ILogger<BaseController> logger,
            JwtService jwtService,
            UserManager<User> userManager)
        {
            GroupService = groupService;
            Logger = logger;
            JwtService = jwtService;
            UserManager = userManager;
        }
```

> **Note:** All controllers inheriting `BaseController` must add the base constructor call: `: base(groupService, logger, jwtService, userManager)`. This is a mechanical change across all 10 controllers.

### Workflow Verification
All controllers → constructor injection → no `RequestServices`.

### Acceptance Criteria
1. `dotnet build` succeeds.
2. No `HttpContext.RequestServices` in `BaseController`.

---

## Arch-2. `JwtService` is a Concrete Class

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `JwtService` is registered as a concrete class and used directly.
- **Affected workflow:** DI.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/JwtService.cs` | `JwtService` | — | Extract an `IJwtService` interface |

### Exact Changes
- **Change type:** ADD interface + MODIFY registration

**New interface `Sohba.Application/Interfaces/IJwtService.cs`**
```csharp
namespace Sohba.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user, IList<string> roles);
    }
}
```

**Registration in `Sohba.Application/DependencyInjection/ApplicationServiceContainer.cs`**
```csharp
services.AddScoped<IJwtService, JwtService>();
```

### Workflow Verification
`BaseController` and `AuthService` depend on `IJwtService` instead of the concrete class.

### Acceptance Criteria
1. `dotnet build` succeeds.
2. No direct `JwtService` concrete dependency outside DI registration.

---

## Arch-3. `UnitOfWork` / Repository Scoping

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `UnitOfWork` and repositories are both `Scoped` and injected separately; the UoW doesn't truly own the DbContext lifecycle.
- **Affected workflow:** DI.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Infrastructure/DependencyInjection/InfrastructureServiceContainer.cs` | — | registrations | Keep as-is (both Scoped share the same DbContext per request — this is actually correct for EF Core). Document the intent. |

> **Note:** This is **not a bug**. Both `UnitOfWork` and repositories are `Scoped`, so they share the same `AppDbContext` instance per HTTP request. The current design is valid. No change required — the audit flagged it as a concern, but verification shows it's correct.

### Acceptance Criteria
1. No change needed. Documented as verified-correct.

---

## Arch-4. `GetProfileAsync` Overloads

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `IUserService` has `GetProfileAsync(Guid userId)` and `GetProfileAsync(Guid userId, Guid currentUserId)`. The single-arg version defaults to owner, which is a footgun.
- **Affected workflow:** `ProfileController.Edit`, `DashboardController.GetUserDetails`, `FriendshipService`, `NotificationService`, `ReportingService`.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/UserService.cs` | `UserService` | `GetProfileAsync(Guid userId)` | Keep as a thin wrapper but document the intent |

### Exact Changes
- **Change type:** MODIFY (add XML doc)

**Old Code**
```csharp
        // Original method (kept for backward compatibility)
        public async Task<Result<UserResponseDto>> GetProfileAsync(Guid userId)
        {
            // Call the new overload with the same userId as current user (owner)
            return await GetProfileAsync(userId, userId);
        }
```

**New Code**
```csharp
        /// <summary>
        /// Gets a profile as the owner (no privacy enforcement).
        /// Use the (userId, currentUserId) overload when the viewer may differ from the owner.
        /// </summary>
        public async Task<Result<UserResponseDto>> GetProfileAsync(Guid userId)
        {
            return await GetProfileAsync(userId, userId);
        }
```

### Workflow Verification
All callers use the correct overload.

### Acceptance Criteria
1. No behavior change. Documented.

---

## Arch-5. `GetAllPostsAsync` Passes `Guid.Empty`

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `PostService.GetAllPostsAsync` calls `MapPostsWithInteractions(posts, Guid.Empty)`, which filters out private posts for the admin dashboard.
- **Affected workflow:** `DashboardController.Posts` → `GetAllPostsAsync` → private posts missing.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/PostService.cs` | `PostService` | `GetAllPostsAsync` | Skip privacy filtering for admin (return all non-deleted posts) |

### Exact Changes
- **Change type:** MODIFY

**Old Code**
```csharp
        public async Task<Result<IEnumerable<PostResponseDto>>> GetAllPostsAsync()
        {
            var posts = await _unitOfWork.Posts.GetAllAsync();
            return await MapPostsWithInteractions(posts, Guid.Empty);
        }
```

**New Code**
```csharp
        public async Task<Result<IEnumerable<PostResponseDto>>> GetAllPostsAsync()
        {
            var posts = await _unitOfWork.Posts.GetAllAsync();
            // Admin view: map without privacy filtering (Guid.Empty bypasses the friend check,
            // but MapPostsWithInteractions still filters private posts). Use a direct map instead.
            var dtos = posts.Select(p => _mapper.Map<PostResponseDto>(p)).ToList();
            return Result<IEnumerable<PostResponseDto>>.Success(dtos);
        }
```

### Workflow Verification
`DashboardController.Posts` → `GetAllPostsAsync` → all non-deleted posts (including private) returned.

### Acceptance Criteria
1. Admin dashboard shows private posts too.

---

## Arch-6. `protected readonly ILogger` Inconsistency

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `NotificationService` and `FriendshipService` use `protected readonly ILogger`; other services use `private readonly`.
- **Affected workflow:** None (style).

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/NotificationService.cs` | `NotificationService` | `_logger` | Change `protected` → `private` |
| `Sohba.Application/Services/FriendshipService.cs` | `FriendshipService` | `_logger` | Change `protected` → `private` |

### Exact Changes
- **Change type:** MODIFY (2 files)

**Old Code**
```csharp
        protected readonly ILogger<NotificationService> _logger;
```
```csharp
        protected readonly ILogger<FriendshipService> _logger;
```

**New Code**
```csharp
        private readonly ILogger<NotificationService> _logger;
```
```csharp
        private readonly ILogger<FriendshipService> _logger;
```

### Workflow Verification
Build succeeds.

### Acceptance Criteria
1. `dotnet build` succeeds.

---

## Arch-7. `SearchResultDto.TotalCount` Computed Property

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `SearchResultDto.TotalCount` is a computed property; the frontend reads `data.data.totalCount`.
- **Affected workflow:** Search results.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/DTOs/SearchAggregate/SearchResultDto.cs` | `SearchResultDto` | `TotalCount` | Keep as-is (works correctly with camelCase serialization) |

> **Note:** Verified working — the computed property serializes as `totalCount` and the frontend reads it correctly. No change required.

### Acceptance Criteria
1. No change needed. Verified-correct.

---

## Arch-8. `StoryService` Hardcodes `MediaType = "image"`

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `StoryService.CreateStoryAsync` sets `MediaType = storyDto.MediaUrl != null ? "image" : null`.
- **Affected workflow:** Story creation.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/DTOs/StoryAggregate/StoryCreateDto.cs` | `StoryCreateDto` | — | ADD `MediaType` property |
| `Sohba.Application/Services/StoryService.cs` | `StoryService` | `CreateStoryAsync` | Use `storyDto.MediaType` |

### Exact Changes

#### 1. DTO — `StoryCreateDto.cs`
- **Change type:** ADD

```csharp
        public string? MediaType { get; set; }
```

#### 2. Service — `StoryService.cs`
- **Change type:** MODIFY

**Old Code**
```csharp
                MediaType = storyDto.MediaUrl != null ? "image" : null,
```

**New Code**
```csharp
                MediaType = storyDto.MediaType ?? (storyDto.MediaUrl != null ? "image" : null),
```

### Workflow Verification
Story creation → `MediaType` from DTO (defaults to "image" for backward compatibility).

### Acceptance Criteria
1. Creating a story with `MediaType = "video"` persists it.

---

## Arch-9. `GetFeedAsync` vs `GetRecentPostsAsync`

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `PostService` has both paged `GetFeedAsync` and unpaged `GetRecentPostsAsync` (the latter returns deleted posts — fixed in H7).
- **Affected workflow:** Dashboard recent posts.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/PostService.cs` | `PostService` | `GetRecentPostsAsync` | Keep (H7 fixes the deleted-post issue); document the distinction |

### Exact Changes
- **Change type:** MODIFY (add XML doc)

**Old Code**
```csharp
        public async Task<Result<IEnumerable<PostResponseDto>>> GetRecentPostsAsync(int count)
```

**New Code**
```csharp
        /// <summary>
        /// Returns the most recent non-deleted posts (admin dashboard widget).
        /// For the user feed, use GetFeedAsync (paged + privacy-filtered).
        /// </summary>
        public async Task<Result<IEnumerable<PostResponseDto>>> GetRecentPostsAsync(int count)
```

### Workflow Verification
Dashboard uses `GetRecentPostsAsync`; feed uses `GetFeedAsync`.

### Acceptance Criteria
1. No behavior change. Documented.

---

## Arch-10. `MapPostsWithInteractions` vs `MapPostsToResponse`

### Issue
- **Severity:** Architecture
- **Blocker:** No
- **Verified root cause:** `PostService.MapPostsWithInteractions` and `InteractionService.MapPostsToResponse` are nearly identical.
- **Affected workflow:** Post mapping.

### Files To Change
| Full path | Class/Interface | Member | Why |
|-----------|----------------|--------|-----|
| `Sohba.Application/Services/InteractionService.cs` | `InteractionService` | `MapPostsToResponse` | DELETE (use `PostService.MapPostsWithInteractions` instead) |

### Exact Changes
- **Change type:** DELETE `MapPostsToResponse`; update callers in `InteractionService` to use `IPostService.MapPostsWithInteractions`.

**Old Code (callers)**
```csharp
            var dtos = await MapPostsToResponse(posts, userId);
```

**New Code**
```csharp
            var mapped = await _postService.MapPostsWithInteractions(posts, userId);
            var dtos = mapped.Value ?? new List<PostResponseDto>();
```

> **Note:** Inject `IPostService` into `InteractionService` (it already injects `IUserService` and `INotificationService`).

### Workflow Verification
Saved/favorite posts → mapped via `PostService.MapPostsWithInteractions` → same output.

### Acceptance Criteria
1. Saved/favorite posts render correctly.
2. No duplicate mapping logic.

---

## Perf-1..10. Performance Concerns

### Perf-1: N+1 in `GetCommentDepthAsync`
- **Fix:** Covered by **M5** (single query using stored `Depth`).

### Perf-2: N+1 in `StoryService.GetStoriesForFeedAsync`
- **Fix:** Batch the `GetViewersCountAsync` + `HasUserViewedStoryAsync` calls. Add repository methods:
  - `GetViewersCountsAsync(IEnumerable<Guid> storyIds)` → `Dictionary<Guid, int>`
  - `GetViewedStoryIdsAsync(IEnumerable<Guid> storyIds, Guid userId)` → `HashSet<Guid>`
- **Files:** `IStoryRepository.cs`, `StoryRepository.cs`, `StoryService.cs`.

### Perf-3: `ToggleSavePost` loads all saved posts
- **Fix:** Covered by **M7**.

### Perf-4: `IsFollowingAsync`/`FollowPageAsync` load all followed pages
- **Fix:** Add `IPageRepository.IsFollowingAsync(Guid userId, Guid pageId)` that does a single `AnyAsync` query.
- **Files:** `IPageRepository.cs`, `PageRepository.cs`, `PageService.cs`.

### Perf-5: `BaseController` generates JWT on every request
- **Fix:** Covered by **M14/M16** (skip for JSON endpoints).

### Perf-6: `GetUserStories` loads all friend stories
- **Fix:** Covered by **M9**.

### Perf-7: No response caching
- **Fix:** Add `[ResponseCache]` to static-ish endpoints (trending hashtags, group/page lists). Low priority.

### Perf-8: `GetAllAsync` in-memory pagination (dashboard)
- **Fix:** Add paginated repository methods (`GetUsersPagedAsync`, `GetPostsPagedAsync`, `GetReportsPagedAsync`) that push `Skip/Take` to SQL.
- **Files:** `IUserRepository.cs`, `IPostRepository.cs`, `IReportingRepository.cs` + implementations + `UserService`/`PostService`/`ReportingService` + `DashboardController`.

### Perf-9: `GetUsersByStatusAsync` loads all users
- **Fix:** Covered by **H5** (uses `GetAllBlockedAsync` + in-memory filter — acceptable for now; a SQL-level filter is the long-term fix).

### Perf-10: SignalR static `ConcurrentDictionary`
- **Fix:** For multi-instance scaling, use a Redis backplane (`AddStackExchangeRedis`). For single-instance, the current design works but loses multi-tab connections. Change `_userConnections` to `ConcurrentDictionary<string, HashSet<string>>` (userId → connectionIds) to support multi-tab.
- **Files:** `NotificationHub.cs`.

---

# Final Fix Roadmap

## Total Issues
- **5 Blockers** (B1–B5)
- **7 High** (H1–H7)
- **16 Medium** (M1–M16)
- **20 Low** (L1–L20)
- **10 Architecture** (Arch-1..10)
- **10 Performance** (Perf-1..10)
- **Total: 68 issues**

## Recommended Implementation Order
1. **P0 (Blockers):** B1 → B2 → B3 → B4 → B5. These must all land before any release.
2. **P1 (High):** H1 → H2 → H3 → H4 → H5 → H6 → H7.
3. **P2 (Medium):** M1 → M2 → M3 → M4 → M5 → M6 → M7 → M8 → M9 → M10 → M11 → M12 → M13 → M14+M15+M16 (same file).
4. **P3 (Low):** L1 → L2 → L3 → L4 → L5 → L6 → L7+L8 → L9 → L10 → L11 → L12 → L13 → L14 → L15 → L16+L17 → L18 → L19 → L20.
5. **P4 (Arch/Perf):** Arch-1 → Arch-2 → Arch-5 → Arch-6 → Arch-8 → Arch-10 → Perf-2 → Perf-4 → Perf-8 → Perf-10.

## Fixes That Should Be Implemented Together
- **B1 + B2:** Same workflow (post edit). Implement together to avoid touching `PostService.UpdatePostAsync` twice.
- **M14 + M15 + M16:** Same file (`BaseController.cs`). Implement together.
- **L7 + L8:** Same file (`friends.js`). Implement together.
- **L16 + L17:** Dead files. Delete together.
- **H5 + Perf-9:** Same method (`GetUsersByStatusAsync`). Implement together.
- **H7 + Arch-9:** Same method (`GetRecentPostsAsync`). Implement together.
- **M5 + Perf-1:** Same method (`GetCommentDepthAsync`). Implement together.
- **M7 + Perf-3:** Same method (`ToggleSavePost`). Implement together.
- **M9 + Perf-6:** Same method (`GetUserStories`). Implement together.
- **M10 + L13:** Same controller (`GroupsController`). Implement together.
- **B4 + L15:** Same file (`DBInitializer.cs`). Implement together.

## Main Regression Risks
1. **B2 signature change** (`CanUpdatePost`) — must update the interface, implementation, and the single call-site in `PostService.UpdatePostAsync`. Missing any one breaks the build.
2. **M4 (comments privacy)** — injecting `IPostDomainService` into `InteractionService` changes the constructor; DI resolves automatically but verify no circular dependency (PostService → InteractionService → PostDomainService is fine; InteractionService does NOT depend on PostService).
3. **M14 (BaseController skip)** — the `isJsonRequest` heuristic must not skip full-page GETs that need `ViewBag.RecommendedGroups`. Verify Home/Profile/Groups/Pages still render the sidebar.
4. **H6 (soft-delete account)** — verify the global `IsDeleted` query filter on `User` actually excludes the user from all queries (confirmed by EF warnings at startup).
5. **Arch-1 (BaseController constructor)** — all 10 controllers must add the base constructor call; missing one breaks the build.
6. **L13 (Groups consolidation)** — verify `groups.js` calls the correct endpoint after removing `GetAboutTab`/`GetGroupMembers`.

## What Must Be Verified Before Declaring the Application Production-Ready
1. **Build:** `dotnet build` succeeds with zero warnings introduced.
2. **Blockers:** B1–B5 all pass their acceptance criteria.
3. **Security:** No hardcoded secrets in `git grep`; post-edit authorization enforced; SignalR hub methods removed; CSRF tokens on all POST endpoints.
4. **Data integrity:** Seeder idempotent (start app twice → no duplicates).
5. **Privacy:** Search and profile no longer leak private data.
6. **Browser E2E:** Login → create post → edit post → delete post → comment → react → save → friend request → group → page → story → search → notifications all work.
7. **Console/Network:** No 404s, no JS ReferenceErrors, no leaked exception messages.
8. **Performance:** Notification polling no longer triggers heavy queries; N+1 patterns eliminated.
9. **Admin:** Dashboard delete/hide/block/resolve all work.

---

*This document is the implementation blueprint for all 68 issues in `QA_Audit_Report.md`. No application source code was modified in the creation of this document.*