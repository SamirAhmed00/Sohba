## COMPLETE INCOMPLETE FEATURES & DEAD IMPLEMENTATION AUDIT — Sohba

---

### 1. CRITICAL INCOMPLETE FEATURES

#### INCOMPLETE 1.1: NOTIFICATION SYSTEM — ENGINE EXISTS AND NOW FIRES
**Status**: 60% Complete (Updated from 10%)  
**Severity**: HIGH (Downgraded from CRITICAL)  
**Feature Type**: Core Feature

**What Exists**:
- ✅ `Notification` entity (`Sohba.Domain/Entities/UserAggregate/Notification.cs`)
- ✅ `INotificationRepository` interface (`Sohba.Domain/Interfaces/INotificationRepository.cs`)
- ✅ `NotificationRepository` implementation (`Sohba.Infrastructure/Repositories/NotificationRepository.cs`)
- ✅ `INotificationDomainService` with business rules
- ✅ `NotificationDomainService` with logic
- ✅ `NotificationsController` with `Index`, `GetUnreadCount`, `MarkAsRead`, `MarkAllAsRead` actions
- ✅ `Notifications/Index.cshtml` view
- ✅ Notification type enum
- ✅ Notification mapping in `MappingProfile.cs`
- ✅ **NEW**: `NotificationService` fully implemented (`CreateNotificationAsync`, `MarkAsReadAsync`, `MarkAllAsReadAsync`, `GetUserNotificationsAsync`, `GetUnreadCountAsync`, `DeleteOldNotificationsAsync`)
- ✅ **NEW**: `INotificationService` interface created and registered in DI
- ✅ **NEW**: Notification creation wired into `FriendshipService.SendFriendRequestAsync` (line 75-80)
- ✅ **NEW**: Notification creation wired into `FriendshipService.AcceptFriendRequestAsync` (line 148-153)
- ✅ **NEW**: Notification creation wired into `InteractionService.AddCommentAsync` (line 99-112)
- ✅ **NEW**: Notification creation wired into `InteractionService.AddReactionAsync` (line 183-197)

**What is Missing**:
- ❌ Notification creation for group joins (`GroupService`)
- ❌ Notification creation for page follows (`PageService`)
- ❌ Notification creation for post reports
- ❌ Real-time delivery via SignalR hub
- ❌ Notification preferences respected before sending

**Files Involved**:
- `Sohba.Application/Services/NotificationService.cs` — Fully implemented
- `Sohba.Application/Services/FriendshipService.cs` — Now calls notifications
- `Sohba.Application/Services/InteractionService.cs` — Now calls notifications
- `Sohba.Application/Services/GroupService.cs` — Missing notification calls
- `Sohba.Application/Services/PageService.cs` — Missing notification calls

**Estimated Completion**: 60% (was 10% — the storage, display, AND creation layers now exist for key social interactions)

---

#### INCOMPLETE 1.2: PRIVACY ENFORCEMENT — RULES DEFINED AND PARTIALLY ENFORCED
**Status**: 50% Complete (Updated from 15%)  
**Severity**: HIGH (Downgraded from CRITICAL)  
**Feature Type**: Core Feature

**What Exists**:
- ✅ `Post.IsPrivate` and `Post.Privacy` enum
- ✅ `PostDomainService.CanViewPost()` — validates privacy
- ✅ `ProfileDomainService.CanViewProfile()` — validates privacy
- ✅ `ProfileDomainService.CanViewFriendsList()` — validates privacy
- ✅ `IProfileDomainService` with privacy rules
- ✅ `StoryPrivacy` enum
- ✅ `StoryDomainService.CanViewStory()` — checks privacy
- ✅ UserSettingsDto has `IsPrivateAccount`, `ShowActivityStatus`
- ✅ `SettingsViewModel` has privacy toggles
- ✅ **NEW**: `PostRepository.GetTimelineAsync` filters by `PostPrivacy.Public`, `PostPrivacy.Friends` (lines 36-39)
- ✅ **NEW**: `PostService.MapPostsWithInteractions` calls `_postDomainService.CanViewPost`
- ✅ **NEW**: `PostService.GetPostByIdAsync` calls `_postDomainService.CanViewPost`

**What is Missing**:
- ❌ Profile privacy (`ProfileDomainService.CanViewProfile`) never called in `UserService`
- ❌ Story privacy (`StoryDomainService.CanViewStory`) hardcoded `false` friend check
- ❌ `IsPrivateAccount` flag never checked anywhere
- ❌ Story feed doesn't filter friends-only stories

**Files Involved**:
- `Sohba.Infrastructure/Repositories/PostRepository.cs` — Now has privacy filtering
- `Sohba.Application/Services/PostService.cs` — Now calls CanViewPost
- `Sohba.Application/Services/UserService.cs` — Still missing privacy check
- `Sohba.Application/Services/StoryService.cs` — Hardcoded `false` placeholder
- `Sohba/Controllers/ProfileController.cs` — Still missing privacy check

**Estimated Completion**: 50% (was 15% — post privacy is now enforced, profile and story privacy still open)

---

#### INCOMPLETE 1.3: REPLY-TO-COMMENT — PARTIALLY FIXED
**Status**: 70% Complete (Updated from 5%)  
**Severity**: MEDIUM (Downgraded from HIGH)  
**Feature Type**: Core Feature

**What Exists**:
- ✅ **NEW**: `ParentCommentId` column on `Comment` entity
- ✅ **NEW**: Self-referencing relationship configured in Fluent API
- ✅ **NEW**: `InteractionService.AddCommentAsync` accepts optional `parentCommentId` parameter
- ✅ **NEW**: Validates parent comment exists and belongs to same post
- ✅ **NEW**: `GetCommentsByPostIdAsync` builds comment tree (top-level + replies with `ReplyCount`)
- ✅ `IInteractionService.AddReplyAsync` interface method exists
- ✅ `InteractionService.AddReplyAsync` implementation exists (but is a stub — just validates and returns success)

**What is Missing**:
- ❌ `AddReplyAsync` stub doesn't create the reply — should be removed in favor of `AddCommentAsync` flow
- ❌ No dedicated reply UI in comment threads
- ❌ No inline reply input field in frontend

**Files Involved**:
- `Sohba.Application/Services/InteractionService.cs` (lines 133-142 — `AddReplyAsync` stub still exists)
- `Sohba.Domain/Entities/PostAggregate/Comment.cs` — Now has `ParentCommentId`

**Estimated Completion**: 70% (was 5% — the database schema, tree-building, and unified comment creation flow all work)

---

#### INCOMPLETE 1.4: JWT AUTHENTICATION — NOW FULLY CONFIGURED
**Status**: 90% Complete (Updated from 40%)  
**Severity**: LOW (Downgraded from CRITICAL)  
**Feature Type**: Security/Auth

**What Exists**:
- ✅ `JwtService.GenerateToken()` — fully implemented
- ✅ `JwtService` registered in DI
- ✅ Called in `AuthService.RegisterAsync` and `LoginAsync`
- ✅ Token returned in `AuthResponseDto`
- ✅ **NEW**: JWT bearer authentication registered in `Program.cs` (lines 36-57) with `AddJwtBearer()`
- ✅ **NEW**: `TokenValidationParameters` configured (issuer, audience, key validation, lifetime, clock skew)
- ✅ **NEW**: `JwtSettings` typed options class with `Validate()` method
- ✅ **NEW**: Null check on `Jwt:Key` via `?? throw new InvalidOperationException`
- ✅ **NEW**: `UseAuthentication()` called in pipeline (line 122)
- ✅ **NEW**: JWT is the default scheme (`DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme`)

**What is Missing / Remaining Concern**:
- ❌ `Jwt:ExpireDays` in `JwtService` lacks null check — could crash if config missing
- ❌ `JwtSettings.Validate()` method should be verified for minimum key length enforcement
- ❌ Cookie auth secondary scheme not explicitly configured alongside JWT

**Files Involved**:
- `Sohba/Program.cs` (lines 36-57 — JWT now fully configured)
- `Sohba.Application/Services/JwtService.cs` (lines 35-37 — minor null check gap on ExpireDays)
- `Sohba.Application/Settings/JwtSettings.cs` — Validation class added

**Estimated Completion**: 90% (was 40% — the major gap of missing middleware is closed)

---

#### INCOMPLETE 1.5: FEED PAGINATION — NOW IMPLEMENTED
**Status**: 95% Complete (Updated from 0%)  
**Severity**: LOW (Downgraded from CRITICAL)  
**Feature Type**: Performance/Core

**What Exists**:
- ✅ **NEW**: `PostRepository.GetTimelineAsync(Guid userId, int page, int pageSize)` with `.Skip().Take()`
- ✅ **NEW**: Returns `(IEnumerable<Post> Items, int TotalCount)` tuple
- ✅ **NEW**: `PostService.GetFeedAsync` uses paginated repository method
- ✅ **NEW**: `PagedResult<PostResponseDto>` with TotalCount, Page, PageSize, TotalPages
- ✅ **NEW**: Frontend infinite scroll / AJAX loading support

**What is Missing**:
- ❌ Non-paginated overload `GetTimelineAsync(Guid userId)` still exists — maintenance risk

**Files Involved**:
- `Sohba.Infrastructure/Repositories/PostRepository.cs` (lines 17-53 — paginated; lines 56-80 — non-paginated overload)
- `Sohba.Application/Services/PostService.cs` (lines 32-60 — uses pagination)

**Estimated Completion**: 95% (was 0% — pagination is fully implemented)

---

### 2. ORPHANED DTOs

#### ORPHAN 2.1: AppUserDto — DEAD DTO
**Status**: 0% (never used)  
**Severity**: LOW
**Explanation**: Unchanged from previous audit. `AppUserDto` is NEVER returned from any endpoint or accepted as a parameter. The mapping exists only as DTO-to-DTO middleman that is entirely bypassed.
**Action**: Delete `AppUserDto.cs` and remove its mappings from `MappingProfile.cs`.

#### ORPHAN 2.2: ForgotPasswordDto — DUPLICATED IN CONTROLLER
**Status**: Redundant — Unchanged  
**Severity**: LOW
**Explanation**: The Application layer DTO is dead. The controller's inline class is used but incomplete.
**Action**: Consolidate — keep one version.

#### ORPHAN 2.3: ResetPasswordDto — DUPLICATED IN CONTROLLER
**Status**: Redundant — Unchanged  
**Severity**: LOW
**Same pattern as ForgotPasswordDto.**

---

### 3. ORPHANED SERVICES & INTERFACES — UPDATED

#### ORPHAN 3.1: ISocialService — **RESOLVED**
**Status**: **Fixed**  
**Severity**: N/A  
**Explanation**: `SocialService.cs` and `ISocialService.cs` have been renamed to `SocialService_Removed.cs` and `ISocialService_Remove.cs`. These stub files still exist and should be deleted.

---

#### ORPHAN 3.2: InteractionRepository — IInteractionRepository — BOTH HAVE UNUSED METHODS
**Status**: 70% Used — Unchanged  
**Severity**: LOW
**Explanation**: `IInteractionRepository.UpdateSavedPost()` still never called.

---

#### ORPHAN 3.3: IGroupRepository — UNUSED METHODS
**Status**: 80% Used — Unchanged  
**Severity**: LOW

---

### 4. ORPHANED REPOSITORY METHODS

#### ORPHAN 4.1: PostRepository.SearchPostsAsync — BACKED BY NO CONTROLLER ENDPOINT
**Status**: Still orphaned — Unchanged  
**Severity**: MEDIUM

#### ORPHAN 4.2: PostRepository.IsPostDeleted — NEVER CALLED
**Status**: Still orphaned — Unchanged  
**Severity**: LOW

#### ORPHAN 4.3: UserRepository.EmailExists — NEVER CALLED
**Status**: Still orphaned — Unchanged  
**Severity**: LOW

---

### 5. INCOMPLETE FEATURES BY PERCENTAGE — UPDATED

#### A. FEATURES 0-25% COMPLETE

| Feature | % | Evidence | What's Missing |
|---------|---|----------|----------------|
| Reply-to-Comment | 70% | ParentCommentId, tree building, unified AddCommentAsync all work | Stub AddReplyAsync remaining, UI |
| Feed Pagination | 95% | Skip/Take, PagedResult, frontend infinite scroll | Non-paginated overload cleanup |
| Notification Creation | 60% | Service + 4 integration points implemented | Group, Page, Report notifications |
| Privacy Enforcement | 50% | Post privacy enforced (repo + service) | Profile/Story privacy still open |
| Post Scheduling | 0% | No ScheduledAt field | Everything |
| Post Drafts | 0% | No IsDraft field | Everything |
| Group Invitations | 0% | CanInviteToGroup exists in domain service | Controller, service, UI, entity |
| Account Email Confirmation | 10% | Identity configured, service registered | Email sending, endpoints, enforcement |
| 2FA | 0% | Identity supports it, nothing configured | Everything |
| Social Login (OAuth) | 0% | No external auth providers | Everything |
| GDPR Data Export | 0% | Nothing | Everything |
| Direct Messaging | 0% | Nothing | Everything |
| Post Sharing | 0% | CanSharePost exists in domain service | Controller, service, UI, entity |
| Admin Audit Log | 0% | Nothing | Everything |
| Report Moderation Actions | 20% | Report entity + dashboard exist | Auto-hide, warn, suspend workflows |
| Comment/User Reporting | 0% | Only Post can be reported | ReportComment, ReportUser endpoints |
| NSFW/Sensitive Content | 0% | Nothing | Everything |
| Login History | 0% | Nothing | Everything |
| Offline/PWA Support | 0% | No Service Worker, no manifest | Everything |

#### B. FEATURES 25-50% COMPLETE — UPDATED

| Feature | % | Evidence | What's Missing |
|---------|---|----------|----------------|
| JWT Authentication | 90% | Full middleware + config in Program.cs | Minor null check gap on ExpireDays |
| Group System | 35% | Create/Join/Leave work, privacy/invite missing | Group settings, invites, roles |
| Page System | 40% | Create/Follow work, categories/discovery missing | Categories, insights, advanced settings |
| Story System | 40% | Create/view/viewed/delete work, video/expiry missing | Video support, expiry job, highlights |
| Email Notifications | 30% | IEmailService exists, only used for password reset | 10+ email scenarios not implemented |
| Hashtags | 30% | Extract and store work, trending/UI missing | Trending algorithm, hashtag page, following |
| Friend Suggestions | 30% | Returns random users, no algorithm | Mutual friends, relevance scoring |
| Admin Dashboard | 40% | Basic views work, analytics hardcoded | Real analytics, audit log, user warnings |

#### C. FEATURES 50-75% COMPLETE — UPDATED

| Feature | % | Evidence | What's Missing |
|---------|---|----------|----------------|
| Friend Requests | 80% | Send/accept/reject/block + notifications work | Notification on reject |
| Post CRUD | 80% | Create/edit/delete/view + pagination + privacy work | Sharing, scheduling, drafts |
| Comment System | 75% | Create/delete + replies work | Stub AddReplyAsync cleanup, nested UI |
| Reactions | 75% | Add/remove + notifications + emoji types work | Emoji picker UI, real-time update |
| Saved Posts | 70% | Save/unsave/tag/favorites work, UI polish missing | Better organization UI |
| Search | 60% | Global search works, filters/suggestions missing | Full-text search, filters, suggestions |
| User Profile | 70% | View/edit/profile page work, privacy, GDPR missing | Privacy enforcement, data export |
| Authentication (JWT + Cookie) | 90% | JWT middleware + Cookie auth both configured | Minor config polish |

#### D. FEATURES 75-99% COMPLETE — UPDATED

| Feature | % | Evidence | What's Missing |
|---------|---|----------|----------------|
| User Registration | 85% | Works with Identity, no email confirmation | Email confirmation, age validation |
| User Login | 85% | Works with Identity, no lockout feedback | Better error messages for lockout |
| Database Migrations | 95% | All entity configurations exist | Clean up non-paginated overload |
| AutoMapper Profiles | 90% | All entity→DTO mappings exist | Dead AppUserDto mapping removal |
| Service Layer DI | 90% | All services registered | Duplicate AuthService registration |
| Repository Layer | 85% | All repositories exist | AsNoTracking, caching missing |

---

### 6. TODO MARKERS IN CODE — UPDATED

| File | Line | TODO | Status |
|------|------|------|--------|
| `Post.cs` | 23 | `// i think it will be nullable ?` | **Still Exists** — indecision about nullability |
| `InteractionService.cs` | 133 | `AddReplyAsync` is a stub | **Partially Fixed** — `AddCommentAsync` handles replies now |
| `UserService.cs` | 89 | `// Need Edit ?` | **Still Exists** — passing empty GUID |
| `DashboardController.cs` | 74 | `// TODO: Get users count for last 7 days` | **Still Exists** — hardcoded analytics |
| `InfrastructureServiceContainer.cs` | 48 | `// We Will Change This TO True After Adding Email Service` | **Still Exists** |
| `MappingProfile.cs` | 26 | `// Basic mapping for now` | **Still Exists** |
| `InfrastructureServiceContainer.cs` | 74 | `// Swap this registration to use cloud storage` | **Still Exists** |

---

### 7. DEAD AND ORPHANED CODE SUMMARY — UPDATED

#### Dead Code (Can Be Deleted Immediately) — UPDATED

| Item | File | Status |
|------|------|--------|
| `AppUserDto.cs` | `DTOs/UserAggregate/` | **Still Dead** |
| `AppUserDto` mappings | `MappingProfile.cs` lines 115-129 | **Still Dead** |
| `ISocialService.cs` | Renamed to `ISocialService_Remove.cs` | **Partially Resolved** — delete stub |
| `SocialService.cs` | Renamed to `SocialService_Removed.cs` | **Partially Resolved** — delete stub |
| `PostRepository.IsPostDeleted()` | `PostRepository.cs` | **Still Dead** |
| `UserRepository.EmailExists()` | `UserRepository.cs` | **Still Dead** |
| `PostRepository.SearchPostsAsync()` | `PostRepository.cs` | **Still Dead** |
| `legacy.css` | `wwwroot/css/` | **Still Dead** |
| `site.js` | `wwwroot/js/` | **Still Dead** |
| Bootstrap JS | Layouts | **Still Dead** |
| Empty `<script type="importmap">` | Both layout files | **Still Dead** |
| Duplicate `IAuthService` registration | `ApplicationServiceContainer.cs` | **Still Exists** |
| Console.WriteLine in FriendshipService | Lines 110-141 | **Still Exists** |
| Console.WriteLine in UserRepository | Lines 21-51 | **Still Exists** |
| Console.WriteLine in AuthController | Multiple lines | **Still Exists** |

---

### 8. TOTAL CODE HEALTH METRICS — UPDATED

| Metric | Previous Count | New Count | Change |
|--------|---------------|-----------|--------|
| Total features identified | ~85 | ~85 | — |
| Features 75-99% complete | 5 | 7 | +2 (JWT, Pagination) |
| Features 50-75% complete | 7 | 9 | +2 (Notifications, Comments) |
| Features 25-50% complete | 5 | 4 | -1 (JWT moved up) |
| Features 0-25% complete | 18 | 16 | -2 (Pagination, Notifications moved up) |
| Dead code items | 18 | 16 | -2 (SocialService renamed) |
| Console.Writeline debug statements | 15+ | 15+ | Unchanged |

---

**Conclusion — UPDATED**: This codebase has made significant progress since the previous audit. The most impactful fixes implemented are:
1. **JWT Authentication** — from "40% / CRITICAL" to "90% / LOW" — middleware now fully configured
2. **Feed Pagination** — from "0% / CRITICAL" to "95% / LOW" — will no longer crash under load
3. **Notification System** — from "10% / CRITICAL" to "60% / HIGH" — now creates notifications for friend requests, comments, and reactions
4. **Reply-to-Comment** — from "5% / CRITICAL" to "70% / MEDIUM" — database schema and unified creation flow work
5. **Privacy Enforcement** — from "15% / CRITICAL" to "50% / HIGH" — post privacy now enforced in repository and service

The remaining critical gaps are: profile/story privacy enforcement, full notification coverage (groups, pages), background jobs, logging, Docker/CI-CD, and email configuration.