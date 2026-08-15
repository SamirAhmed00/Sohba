# Sohba Full Project Context Report

> **Purpose:** This report provides a complete, accurate technical understanding of the **Sohba Social Media Web Application** codebase. It is intended to give another AI (or developer) everything needed to build a full manual testing plan. It is based **only** on what is actually present in the repository — no assumptions about unimplemented features.

---

## 1. Project Overview

**Sohba** is a social media web application built with **ASP.NET Core MVC on .NET 10** (net10.0), following a **Clean Architecture** style with four projects. It provides the core social features: user profiles, posts, comments/replies, reactions, saved posts/favorites/collections, stories, notifications (with real-time SignalR), friends/blocking, groups, pages, hashtags, global search, reporting/moderation, and an admin dashboard.

**Key technical stack:**
- **Backend:** ASP.NET Core MVC (Razor Views + AJAX/JSON endpoints), .NET 10
- **ORM:** Entity Framework Core 10 (SQL Server)
- **Identity:** ASP.NET Identity with `Guid` keys, dual auth (cookie for MVC + JWT for SignalR)
- **Real-time:** SignalR (`/notificationHub`)
- **Validation:** FluentValidation (auto-validation)
- **Mapping:** AutoMapper
- **Logging:** Serilog (console + rolling file)
- **Email:** Mailtrap SMTP (sandbox)
- **File storage:** Local `wwwroot/uploads` (via `IFileStorageService` abstraction)
- **Frontend:** Razor Views + Tailwind-style CSS + vanilla JS modules (fetch-based AJAX), jQuery for validation, Bootstrap/UIkit libs
- **Background jobs:** `NotificationCleanupService` (hosted service, deletes old read notifications every 24h)

**Solution file:** `Sohba.slnx`

**Projects:**
| Project | Role |
|---------|------|
| `Sohba.Domain` | Entities, enums, domain services (business rules), repository interfaces, `Result` pattern |
| `Sohba.Application` | Application services (use cases), DTOs, validators, AutoMapper profile, events, settings |
| `Sohba.Infrastructure` | EF Core DbContext + configurations, repositories, UnitOfWork, DB initializer/seeder, file storage, email, background service |
| `Sohba` (Web) | Controllers, ViewModels, Views, Hubs, Handlers, Filters, Extensions, `Program.cs` |

---

## 2. Architecture

### 2.1 Layer Boundaries

**Domain (`Sohba.Domain`)**
- Entities (aggregates: `UserAggregate`, `PostAggregate`, `StoryAggregate`, `GroupAndPage`)
- Enums
- `Result` / `Result<T>` (success/failure pattern)
- Domain services (`Domain Rules/Logic`) — pure business rules, no EF/I/O
- Repository interfaces + `IUnitOfWork`
- **Notable coupling:** `User` entity inherits `IdentityUser<Guid>` from `Microsoft.AspNetCore.Identity` — the only external package in the Domain layer.

**Application (`Sohba.Application`)**
- Application services (use cases) that orchestrate domain rules + repositories
- DTOs (request/response)
- FluentValidation validators
- AutoMapper `MappingProfile`
- `NotificationEvent` (decouples notification creation from SignalR delivery)
- `JwtSettings`
- **Notable coupling:** References `Microsoft.AspNetCore.Identity.EntityFrameworkCore` and `System.IdentityModel.Tokens.Jwt`; `IFormFile`/`IWebHostEnvironment` are referenced via interfaces (`IFileStorageService`, `IEmailService`) to keep file/email I/O out of the Application layer.

**Infrastructure (`Sohba.Infrastructure`)**
- `AppDbContext` + all EF entity configurations
- Repositories + `UnitOfWork`
- `DBInitializer` (migrations + seed data)
- `LocalFileStorageService` (concrete `IFileStorageService`)
- `MailtrapEmailService` (concrete `IEmailService`)
- `NotificationCleanupService` (background hosted service)
- DI container (`InfrastructureServiceContainer`)

**Presentation (`Sohba` Web)**
- Controllers (MVC + JSON/AJAX endpoints)
- ViewModels
- Razor Views + Partials
- `NotificationHub` (SignalR), `NotificationEventHandler`
- `ValidationFilter` (defined but **not registered** — commented out in `Program.cs`)
- `ApplicationBuilderExtensions` (DB init)
- `Program.cs` (pipeline, JWT, rate limiting, Serilog, cookie auth)

### 2.2 Dependency Flow
```
Sohba (Web) → Sohba.Application → Sohba.Domain
Sohba (Web) → Sohba.Infrastructure → Sohba.Application → Sohba.Domain
```
- Web references Application + Infrastructure.
- Infrastructure references Application + Domain.
- Application references Domain.
- Domain references nothing (except ASP.NET Identity for the `User` entity).

### 2.3 Cross-Layer Couplings / Concerns
- **Domain → Identity:** `User : IdentityUser<Guid>` couples the domain to ASP.NET Identity.
- **Application → Identity:** `AuthService`/`UserSettingsService` use `UserManager`/`SignInManager` directly.
- **BaseController → RequestServices:** `BaseController` resolves `IGroupService`, `JwtService`, `UserManager`, and `ILogger` via `HttpContext.RequestServices` (service locator) rather than constructor injection. It also generates a JWT per request to put in `ViewBag.JwtToken` for SignalR.
- **Application → Infrastructure boundary:** `StoryService` explicitly avoids file I/O; the controller resolves media URLs via `IFileStorageService` before calling the service (documented in code comments).
- **Repositories use `AsNoTracking`** in several places (Groups, Users) to avoid EF tracking conflicts — a deliberate pattern with documented rationale.

---

## 3. Complete Project Structure

```
SohbaANTII/
├── Sohba.slnx
├── Sohba.Domain/
│   ├── Common/Result.cs
│   ├── Domain Rules/
│   │   ├── Interface/  (IFriendshipDomainService, IGroupDomainService, IInteractionDomainService,
│   │   │                IMediaDomainService, INotificationDomainService, IPageDomainService,
│   │   │                IPostDomainService, IProfileDomainService, IReportingDomainService, IStoryDomainService)
│   │   └── Logic/      (concrete implementations of the above)
│   ├── Entities/
│   │   ├── GroupAndPage/ (Group, GroupMember, Page, PageFollower)
│   │   ├── PostAggregate/ (Comment, Hashtag, Post, PostHashtag, PostReport, Reaction, SavedCollection, SavedPost)
│   │   ├── StoryAggregate/ (Story, StoryViewer)
│   │   └── UserAggregate/ (Friend, Notification, User)
│   ├── Enums/ (FriendshipStatus, GroupRole, NotificationType, PostPrivacy, PostSourceType,
│   │          ReactionType, ReportReason, SavedTag, StoryPrivacy)
│   └── Interfaces/ (IFriendshipRepository, IGenericRepository, IGroupRepository, IHashtagRepository,
│                    IInteractionRepository, INotificationRepository, IPageRepository, IPostRepository,
│                    IReportingRepository, IStoryRepository, IUnitOfWork, IUserRepository)
│
├── Sohba.Application/
│   ├── DependencyInjection/ApplicationServiceContainer.cs
│   ├── DTOs/
│   │   ├── Common/ (BaseResponseDto, IdRequestDto, PagedResult)
│   │   ├── GroupAndPageAggregate/ (GroupCreateDto, GroupMemberDto, GroupResponseDto, GroupUpdateDto,
│   │   │                           PageCreateDto, PageFollowerDto, PageResponseDto, PageUpdateDto)
│   │   ├── PostAggregate/ (CommentRequestDto, CommentResponseDto, CreateSavedCollectionDto, HashtagDto,
│   │   │                   PostCreateDto, PostReportRequestDto, PostReportResponseDto, PostResponseDto,
│   │   │                   PostUpdateDto, ReactionRequestDto, ReactionResponseDto, SavedCollectionDto,
│   │   │                   SavedPostDto, SavedPostsGroupedDto, SaveToCollectionDto, Requests/)
│   │   ├── SearchAggregate/ (GroupSearchResultDto, PageSearchResultDto, PostSearchResultDto,
│   │   │                     SearchResultDto, UserSearchResultDto)
│   │   ├── StoryAggregate/ (StoryCreateDto, StoryResponseDto)
│   │   └── UserAggregate/ (AuthResponseDto, ForgotPasswordDto, FriendDto, LoginDto,
│   │                       NotificationResponseDto, RegisterDto, ResetPasswordDto,
│   │                       UserRequestDto, UserResponseDto, UserSettingsDto)
│   ├── Events/NotificationEvent.cs
│   ├── Interfaces/ (IAuthService, IEmailService, IFileStorageService, IFriendshipService, IGroupService,
│   │                IHashtagService, IInteractionService, IJwtService, INotificationEventHandler,
│   │                INotificationHubService, INotificationService, IPageService, IPostService,
│   │                IReportingService, ISearchService, IStoryService, IUserService, IUserSettingsService)
│   ├── Mappings/MappingProfile.cs
│   ├── Services/ (AuthService, FriendshipService, GroupService, HashtagService, InteractionService,
│   │              JwtService, NotificationService, PageService, PostService, ReportingService,
│   │              SearchService, StoryService, UserService, UserSettingsService)
│   ├── Settings/JwtSettings.cs
│   └── Validators/CommentRequestDtoValidator.cs
│
├── Sohba.Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Configurations/ (Comment, Friend, Group, GroupMember, Hashtag, Notification, Page,
│   │                        PageFollower, Post, PostHashtag, PostReport, Reaction, SavedPost,
│   │                        Story, StoryViewer, User)
│   ├── DBInitializer/DBInitializer.cs
│   ├── DependencyInjection/InfrastructureServiceContainer.cs
│   ├── Migrations/ (5 migrations + snapshot)
│   ├── Repositories/ (Friendship, Generic, Group, Hashtag, Interaction, Notification, Page,
│   │                  Post, Reporting, Story, UnitOfWork, User)
│   ├── Services/ (MailSettings, MailtrapEmailService, NotificationCleanupService)
│   └── LocalFileStorageService.cs
│
└── Sohba/ (Web)
    ├── Program.cs
    ├── Controllers/ (Auth, Base, Comments, Dashboard, Friends, Groups, Home, Landing,
    │                 Notifications, Pages, Posts, Profile, Search, Stories)
    ├── Extensions/ApplicationBuilderExtensions.cs
    ├── Filters/ValidationFilter.cs
    ├── Handlers/NotificationEventHandler.cs
    ├── Hubs/NotificationHub.cs
    ├── Models/ (ErrorViewModel)
    ├── Validators/PostCreateViewModelValidator.cs
    ├── ViewModels/ (Home, Dashboard/, Friend/, Group/, Page/, Post/, Profile/, Search/)
    ├── Views/ (Auth/, Dashboard/, Friends/, Groups/, Home/, Landing/, Notifications/, Pages/,
    │           Posts/, Profile/, Search/, Shared/, Stories/)
    └── wwwroot/
        ├── css/ (input, landing, legacy, site, v0-custom)
        ├── js/ (script, simplebar, site, sohba-core, sohba-modal, sohba-posts, sohba-stories,
        │        uikit.min, features/{comments,dashboard,feed,friends,groups,header,modal,posts,search,sidebar,stories}.js)
        ├── lib/ (bootstrap, jquery, jquery-validation, jquery-validation-unobtrusive)
        ├── syntax-highlighter/
        └── uploads/ (groups/, pages/, posts/, stories/)
```

---

## 4. Domain Analysis

### 4.1 Entities

#### UserAggregate
**`User`** (`: IdentityUser<Guid>`)
- `Name` (required, max 100), `Bio` (max 500), `IsDeleted` (soft delete), `DateOfBirth`, `ProfilePictureUrl?`, `CreatedAt`, `IsActive`
- Privacy settings: `IsPrivateAccount` (default false), `ShowActivityStatus` (default true), `EmailNotifications` (default true), `PushNotifications` (default true), `WeeklyDigest` (default false)
- Navigation: `Posts`, `Stories`, `Comments`, `Reactions`, `Friends`, `GroupMemberships`, `FollowedPages`, `SentReports`, `SavedPosts`, `AdministeredGroups`, `AdministeredPages`

**`Friend`** (composite key `UserId` + `FriendUserId`)
- `Status` (FriendshipStatus), `CreatedAt`
- Navigation: `User` (the requester), `FriendUser` (the target)
- Represents a **directed** relationship row; `GetByUsersAsync` searches both directions.

**`Notification`**
- `Message`, `IsRead` (default false), `CreatedAt` (default UtcNow), `Type` (NotificationType)
- `ReceiverId` (required), `SenderId?` (optional — system notifications have no sender), `TargetId?` (optional link to PostId/GroupId/etc.)
- Navigation: `Receiver`, `Sender`

#### PostAggregate
**`Post`**
- `Title`, `Content` (required), `CreatedAt`, `IsDeleted` (soft delete), `Privacy` (default Public), `IsHidden`, `IsPrivate`, `SourceType` (default User), `SourceId?`, `UpdatedAt?`, `ImageUrl?`
- `PageId?`/`Page`, `GroupId?`/`Group`, `UserId`/`User`
- Navigation: `Comments`, `Reactions`, `PostHashtags`, `Reports`, `SavedByUsers`
- **Note:** `IsPrivate` and `Privacy` are both present; `IsPrivate` is a legacy bool, `Privacy` is the enum. Domain logic uses `IsPrivate` in `CanViewPost` while the repository timeline uses `Privacy`.

**`Comment`**
- `Content` (max 1000), `CreatedAt`, `DateUpdated`, `Depth` (int), `ParentCommentId?`
- `UserId`/`User`, `PostId`/`Post`
- Self-referencing: `ParentComment`, `Replies`
- Supports nested replies up to depth 4 (enforced in `InteractionDomainService`).

**`Reaction`**
- `Type` (ReactionType), `CreatedAt`, `UserId`/`User`, `PostId`/`Post`
- One reaction per (user, post) — enforced in service logic (upsert).

**`Hashtag`**
- `Tag` (unique), `Location`, `Count`, `CreatedAt`, `UpdatedAt`
- Navigation: `PostHashtags`

**`PostHashtag`** (join table, composite key `PostId` + `HashtagId`)
- `PostId`/`Post`, `HashtagId`/`Hashtag`

**`PostReport`**
- `Reason` (ReportReason), `ReportedAt` (default UtcNow), `IsResolved` (default false)
- `PostId`/`Post`, `UserId`/`User`

**`SavedCollection`**
- `UserId`, `Name`, `IsDefault`, `IsFavorites`, `CreatedAt`
- Navigation: `User`, `SavedPosts`

**`SavedPost`**
- `UserId`/`User`, `PostId`/`Post`, `CollectionId?`/`Collection` (null = legacy/default), `SavedAt`, `Tag` (SavedTag, kept for backwards compat), `UserTag?` (kept for backwards compat)
- A post can be saved to multiple collections AND be a Favorite simultaneously.

#### StoryAggregate
**`Story`**
- `Content`, `MediaUrl?`, `MediaType?`, `CreatedAt`, `ExpiresAt`, `Privacy` (default Public), `IsDeleted` (soft delete)
- `UserId`/`User`
- Navigation: `Viewers`

**`StoryViewer`**
- `StoryId`/`Story`, `UserId`/`User`, `ViewedAt`

#### GroupAndPage
**`Group`**
- `Name`, `Description`, `ImageUrl?`, `CreatedAt`, `AdminId`/`Admin`
- Navigation: `GroupMembers`

**`GroupMember`** (composite key `GroupId` + `UserId`)
- `JoinedAt`, `Role` (GroupRole, default Member), `IsBanned` (default false)
- Navigation: `User`, `Group`

**`Page`**
- `Name`, `Description`, `ImageUrl?`, `CreatedAt`, `AdminId`/`Admin`

**`PageFollower`** (composite key `PageId` + `UserId`)
- `FollowedAt`
- Navigation: `User`, `Page`

### 4.2 Enums
| Enum | Values |
|------|--------|
| `FriendshipStatus` | Pending=1, Accepted=2, Rejected=3, Blocked=4 |
| `GroupRole` | Member=1, Moderator=2, Admin=3 |
| `NotificationType` | PostLike=1, PostComment=2, FriendRequest=3, GroupInvitation=4, SystemAlert=5, PageFollow=6 |
| `PostPrivacy` | Public=0, Friends=1, Private=2 |
| `PostSourceType` | User=1, Group=2, Page=3 |
| `ReactionType` | Like=1, Love=2, Haha=3, Wow=4, Sad=5, Angry=6 |
| `ReportReason` | Spam=1, Harassment=2, InappropriateContent=3, Violence=4, Other=5 |
| `SavedTag` | General=1, Favorite=2, WatchLater=3, Work=4, Education=5 |
| `StoryPrivacy` | Public=1, FriendsOnly=2 |

### 4.3 Result Pattern (`Common/Result.cs`)
- `Result` with `IsSuccess`, `Error`, `IsFailure`
- `Result<T>` adds `Value` (throws if accessed on failure)
- `Result.Success()`, `Result.Failure(error)`, `Result<T>.Success(value)`, `Result<T>.Failure(error)`
- Invariants: success cannot have an error; failure must have an error.

### 4.4 Domain Services (Business Rules)

**`PostDomainService`**
- `CanCreatePost`: post must have content OR attachments
- `CanUpdatePost`: cannot update deleted post; only owner can edit
- `CanDeletePost`: admin can delete anything; owner can delete own
- `CanViewPost`: owner always sees; public → anyone; private → friends only
- `CanCommentOnPost`: cannot comment on deleted post; blocked users cannot comment
- `CanReactToPost`: cannot react to deleted post
- `CanSharePost`: private posts cannot be shared publicly
- `CanPostInGroup`: must be member; banned users cannot post
- `CanReportPost`: cannot report twice

**`FriendshipDomainService`**
- `CanSendFriendRequest`: no self-request, not blocked, not already friends, no pending request
- `CanAcceptFriendRequest`: request must exist, not already friends
- `CanDeclineFriendRequest` / `CanCancelFriendRequest`: request must exist
- `CanRemoveFriend`: must be friends
- `CanBlockUser`: cannot block self, not already blocked
- `CanUnblockUser`: must be blocked

**`InteractionDomainService`**
- `CanAddComment`: content not deleted, not blocked by owner, text non-empty
- `CanAddReaction`: content not deleted, user not blocked
- `CanDeleteComment`: comment owner, post owner, or admin
- `CanEditComment`: only owner, within edit time limit (minutes param)
- `CanReplyToComment`: comment not deleted, thread not locked, depth < `MaxReplyDepth` (4)
- `CanUpdateReaction`: reaction must exist

**`GroupDomainService`**
- `CanDeleteGroup`: only owner
- `CanInviteToGroup`: must be member; group must allow member invites
- `CanJoinGroup`: banned users cannot join; private groups require invitation
- `CanKickMember`: cannot kick self; only Admin/Owner can kick; cannot kick Admin/Owner
- `CanPostInGroup`: must be member; group not locked
- `CanPromoteMember`: only Owner/Admin; cannot promote existing Admin/Owner
- `CanUpdateGroup`: only group admin
- `CanLeaveGroup`: sole admin cannot leave without promoting another

**`PageDomainService`**
- `CanCreatePage`: name non-empty, min length 3
- `CanFollowPage`: page exists, not the admin, not already following
- `CanUnfollowPage`: must be following

**`ProfileDomainService`**
- `CanUpdateProfile`: only owner
- `CanViewProfile`: not blocked; owner always; private requires friend
- `CanViewFriendsList`: owner always; "Private" → denied; "FriendsOnly" → friends only
- `CanViewContactInfo`: owner or friend
- `CanChangeUsername`: respects days-limit between changes

**`ReportingDomainService`**
- `CanReportEntity`: cannot report twice
- `CanReviewReport`: only admins
- `ShouldAutoHideContent`: auto-hide if report count >= threshold
- `CanAppealReport`: cannot appeal resolved report

**`StoryDomainService`**
- `CanCreateStory`: must have media; daily limit (10)
- `CanViewStory`: not expired; owner or friend
- `CanReplyToStory`: not expired; creator accepts replies
- `IsStoryExpired`: expires after 24 hours
- `CanHighlightStory`: only owner

**`NotificationDomainService`**
- `ShouldSendNotification`: don't notify self
- `ShouldBundleNotifications`: bundle if last similar sent within 15 min
- `CanMarkAsRead`: only notification owner

**`MediaDomainService`**
- `CanUploadMedia`: allowed extensions (.jpg/.jpeg/.png/.webp images; .mp4/.mov videos); 5MB image / 50MB video limits
- `CanAccessMedia`: owner or (not private or friend)
- `CanSetProfilePicture`: 2MB max; .jpg/.jpeg/.png only

---

## 5. Application Analysis

### 5.1 Services (Use Cases)

**`AuthService`** — Register, Login, Logout, GetCurrentUser, ForgotPassword, ResetPassword
- Register: checks email uniqueness, creates `User` with `UserName = Email`, adds to "User" role, generates JWT
- Login: `CheckPasswordSignInAsync` with lockout (5 attempts / 5 min), signs in cookie (persistent if RememberMe), generates JWT
- ForgotPassword: generates reset token, sends email via `IEmailService` with callback link
- ResetPassword: `ResetPasswordAsync` with token

**`PostService`** — GetFeed (paginated), CreatePost, GetPostById, UpdatePost, DeletePost (soft), GetGroupPosts, GetPagePosts, GetUserPosts, GetAllPosts, HidePost, GetPostsCount, GetRecentPosts
- CreatePost: validates via domain, enforces group-membership / page-admin access control, extracts hashtags via regex `#\w+`, uses transaction for post + hashtags, sends notifications to group/page admin
- GetFeed: delegates to `PostRepository.GetTimelineAsync` (privacy-aware), maps with interactions
- `MapPostsWithInteractions`: applies privacy filtering (owner always, public, friends), loads counts, user reactions, saved/favorite flags
- DeletePost: soft delete (`IsDeleted = true`)

**`InteractionService`** — Comments (get tree, add, delete, reply), Reactions (add/update/remove, count), SavedPosts (save, remove, favorites, tags, collections)
- `GetCommentsByPostIdAsync`: builds a recursive reply tree (depth 1–4), sets `IsAuthor`, `ReplyCount`
- `AddCommentAsync`: validates post, parent comment belongs to post, depth limit, blocked check; sends notification to post owner
- `AddReactionAsync`: upsert (update existing or create new); sends notification
- Saved posts: `SavePostAsync` (upsert with tag), `SavePostToCollectionAsync`, `SavePostToFavoritesAsync` (toggle, auto-creates Favorites collection), `GetSavedPostsGroupedPagedAsync` (groups by "All Saved" + collections)
- **Important distinction:** A post is "saved" only when in a NON-Favorite collection; Favorites alone does not imply Saved.

**`StoryService`** — CreateStory, GetStoriesForFeed, GetStoryById, MarkStoryAsViewed, DeleteStory, GetUserStories
- CreateStory: enforces daily limit (10), 24h expiry, privacy mapping
- GetStoriesForFeed: filters by 24h cutoff, not deleted, owner/public/friends-only; groups by user
- MarkStoryAsViewed: skips owner, adds viewer if not already viewed

**`FriendshipService`** — Send/Accept/Reject/Cancel requests, Unfriend, GetFriendsList, GetPending/Sent requests, Block/Unblock, GetBlockedUsers, GetFriendSuggestions
- Send: domain pre-checks, creates `Friend` row (Pending), sends notification
- Accept: updates status to Accepted, sends notification
- Unfriend: deletes both direction rows
- Block: deletes existing friendships both directions, creates Blocked row
- GetFriendSuggestions: excludes self, friends, sent requests, blocked; random users

**`UserService`** — GetProfile (with privacy), UpdateProfile, GetAllUsers, DeleteUser (soft), GetUsersByStatus, GetUsersCount, GetRecentUsers, DeactivateAccount, DeleteMyAccount

**`UserSettingsService`** — GetSettings, UpdateSettings, UpdateEmail, UpdatePassword, DeactivateAccount, DeleteAccount
- Uses `UserManager` directly; settings stored on the `User` entity (no separate settings table — noted as TODO)

**`GroupService`** — CreateGroup, JoinGroup, GetAllGroups, GetGroupById, DeleteGroup, GetGroupMembers, KickMember, LeaveGroup, GetUserGroups, UpdateGroup, GetRecommendedGroups, GetGroupsCount
- CreateGroup: creates group + admin member
- JoinGroup: domain check (private groups rejected), adds member, notifies admin
- LeaveGroup: loads tracked member, checks sole-admin rule, removes member
- UpdateGroup: uses `AsNoTracking` + `Update()` to avoid EF tracking conflicts

**`PageService`** — CreatePage, FollowPage, UnfollowPage, GetPageById, GetUserFollowedPages, GetAllPages, DeletePage, ToggleFollowPage, IsFollowing, GetFollowersCount, GetFollowers, UpdatePage, GetPagesCount
- CreatePage: creates page + auto-follows admin
- Follow/Unfollow: domain checks, sends notifications to page admin

**`NotificationService`** — CreateNotification, GetUserNotifications (paged), GetUnreadNotifications, GetUnreadCount, MarkAsRead, MarkAllAsRead, DeleteNotification, DeleteOldNotifications
- CreateNotification: skips self-notifications, verifies receiver/sender exist, checks user preferences (`EmailNotifications`/`PushNotifications`), persists, then dispatches `NotificationEvent` via SignalR (errors logged, not fatal)

**`SearchService`** — GlobalSearch, SearchPosts, SearchUsers, SearchGroups, SearchPages
- GlobalSearch: min query length 2; searches posts (privacy-aware), users, groups, pages

**`HashtagService`** — GetTrendingHashtags, GetPostsByHashtag

**`ReportingService`** — ReportPostWithDetails, GetAllReports, ResolveReport, GetPendingReportsCount, GetRecentPendingReports
- ReportPostWithDetails: validates post exists, no duplicate report, parses reason enum, adds report, auto-hides post if report count >= 5 (sets `IsDeleted`), notifies post owner + admin (`admin@sohba.com`)
- ResolveReport: marks resolved, notifies reporter + post owner
- **Note:** `ReportPostAsync` (older method) is fully commented out.

**`JwtService`** — Generates JWT with claims (sub, email, name, jti, roles), HMAC-SHA256, from `JwtSettings`

### 5.2 DTOs (Key)
- **Common:** `BaseResponseDto`/`BaseResponseDto<T>` (Success/Error/Data), `IdRequestDto`, `PagedResult<T>` (Items, TotalCount, Page, PageSize, TotalPages, HasPreviousPage, HasNextPage)
- **Post:** `PostCreateDto`, `PostUpdateDto`, `PostResponseDto` (Id, Title, Content, ImageUrl, AuthorName, CommentsCount, ReactionsCount, CreatedAt, CurrentUserReaction, IsSaved, SavedTag, IsFavorite, IsReportedByCurrentUser, IsAuthor, IsPrivate, Privacy, SourceType, SourceName, SourceId), `CommentRequestDto`, `CommentResponseDto` (recursive Replies, Depth, ReplyCount, IsAuthor), `ReactionRequestDto`, `ReactionResponseDto`, `PostReportRequestDto`, `PostReportResponseDto`, `SavedPostDto`, `SavedCollectionDto`, `SavedPostsGroupedDto`, `CreateSavedCollectionDto`, `SaveToCollectionDto`, `HashtagDto`
- **User:** `RegisterDto`, `LoginDto`, `AuthResponseDto` (Token, Roles), `UserResponseDto`, `UserRequestDto`, `UserSettingsDto`, `FriendDto`, `NotificationResponseDto`, `ForgotPasswordDto`, `ResetPasswordDto`
- **Story:** `StoryCreateDto` (Content, MediaFile, MediaUrl, Privacy), `StoryResponseDto`
- **Group/Page:** `GroupCreateDto`, `GroupUpdateDto`, `GroupResponseDto` (AdminName, MembersCount, IsCurrentUserMember), `GroupMemberDto`, `PageCreateDto`, `PageUpdateDto`, `PageResponseDto` (AdminName, AdminId, IsFollowing), `PageFollowerDto`
- **Search:** `SearchResultDto` (Posts, Users, Groups, Pages), `PostSearchResultDto`, `UserSearchResultDto`, `GroupSearchResultDto`, `PageSearchResultDto`

### 5.3 Validators (FluentValidation)
- **`CommentRequestDtoValidator`** (Application): Content non-empty, max 500; PostId required
- **`PostCreateViewModelValidator`** (Web): Title max 150; Content non-empty, max 5000; Privacy in enum
- Registered via `AddFluentValidationAutoValidation()` + `AddValidatorsFromAssemblyContaining` for both assemblies in `Program.cs`

### 5.4 AutoMapper (`MappingProfile`)
- Maps entities ↔ DTOs; handles enum→string conversions (SourceType, Role, Type, Reason, Tag, NotificationType)
- `Post → PostResponseDto`: maps `AuthorName` from `User.Name`, `SourceName` from Group/Page name
- `Comment → CommentResponseDto`: maps `UserName`
- `Group → GroupResponseDto`: maps `AdminName`
- `Page → PageResponseDto`: maps `AdminName`
- `Notification → NotificationResponseDto`: maps `NotificationType`, `SenderName` (or "System"), `SenderProfilePicture`; `TimeAgo` ignored
- `Story → StoryResponseDto`: maps `UserName`, `UserId`, `UserProfilePicture`
- `SavedPost → SavedPostDto`: maps `PostTitle`, `Tag`
- `SavedCollection → SavedCollectionDto`: maps `PostCount`
- Search mappings for Post/User/Group/Page
- **Note:** `Friend → FriendDto` mapping is commented out; `FriendshipService` builds `FriendDto` manually.

### 5.5 Events
- **`NotificationEvent`** — carries ReceiverId, Message, Type, SenderId, TargetId, Notification entity. Raised by `NotificationService`, handled by `NotificationEventHandler` (Web layer) for SignalR delivery. Keeps Application decoupled from Web.

### 5.6 Settings
- **`JwtSettings`** — Key (min 32 chars), Issuer, Audience, ExpireDays (default 7). `Validate()` throws if invalid.

---

## 6. Infrastructure Analysis

### 6.1 DbContext (`AppDbContext`)
- `IdentityDbContext<User, IdentityRole<Guid>, Guid>`
- DbSets: Comments, Hashtags, Posts, PostHashtags, PostReports, Reactions, SavedPost, SavedCollections, Groups, GroupMembers, Pages, PageFollowers, Friends, Users, Stories
- `OnModelCreating` applies all configurations from assembly.

### 6.2 EF Configurations (Key rules)
- **User:** PK Id (NEWSEQUENTIALID), Name required max 100, Email required max 150 + unique index, Bio max 500, **global query filter `!IsDeleted`**, indexes on CreatedAt, IsDeleted
- **Post:** PK Id (NEWSEQUENTIALID), Content required, User FK Restrict, **global query filter `!IsDeleted`**, indexes (UserId, CreatedAt), (CreatedAt), (SourceType, SourceId)
- **Comment:** Content required max 1000, Post FK Restrict, User FK Restrict, self-referencing ParentComment
- **Reaction:** Post FK Restrict, User FK Restrict
- **Hashtag:** unique index on Tag
- **PostHashtag:** composite key (PostId, HashtagId); Hashtag FK Cascade, Post FK Restrict
- **PostReport:** Post FK Restrict, User FK Restrict
- **SavedPost:** PK Id; User FK Restrict, Post FK Restrict, Collection FK Cascade
- **SavedCollection:** User FK Cascade
- **Friend:** composite key (UserId, FriendUserId); both FKs Restrict
- **Group:** Admin FK Restrict
- **GroupMember:** composite key (GroupId, UserId); both FKs Restrict
- **Page:** Admin FK Restrict
- **PageFollower:** composite key (PageId, UserId); both FKs Restrict
- **Story:** User FK Restrict, **global query filter `!IsDeleted`**
- **StoryViewer:** Story FK Cascade, User FK Restrict
- **Notification:** Receiver FK Restrict, Sender FK Restrict; indexes (ReceiverId, CreatedAt), (ReceiverId, IsRead), (CreatedAt, IsRead)

### 6.3 Repositories
- **`GenericRepository<T>`** — GetByIdAsync (FindAsync), GetAllAsync, Add, Update, Delete, CountAsync
- **`PostRepository`** — GetTimelineAsync (privacy-aware, paginated), AddHashtagsToPostAsync, GetPostsCountsAsync, GetGroupPostsAsync, GetPagePostsAsync, GetUserPostsAsync, SearchPostsAsync (privacy-aware), GetPostsByHashtagAsync, GetRecentAsync
- **`FriendshipRepository`** — GetPendingRequestsAsync, GetSentRequestsAsync, GetPendingRequestsCountAsync, GetBlockedUsersAsync, GetByUsersAsync (both directions), GetListByUserAsync, GetAllBlockedAsync, AreFriendsAsync, IsUserBlockedAsync, HasPendingRequestAsync, GetFriendIdsAsync, GetFriendIdsSetAsync
- **`InteractionRepository`** — Reactions (GetReactionAsync, GetUserReactionsForPostsAsync, HasUserReacted, GetReactionCountAsync, Add/Remove/Update), Comments (GetCommentByIdAsync, GetCommentsByPostIdAsync, GetRepliesByCommentIdAsync, Add/Remove), SavedPosts (GetSavedPostAsync, GetSavedPostsByUserAsync, GetSavedPostsByUserAndTagAsync, Add/Remove/Update), Collections (GetCollectionsByUserAsync, GetCollectionByIdAsync, GetCollectionByNameAsync, AddCollection, GetSavedPostByCollectionAsync)
- **`GroupRepository`** — GetAllAsync/GetByIdAsync (AsNoTrackingWithIdentityResolution + includes), IsMemberAsync, GetUserRoleInGroup, GetGroupsByUserIdAsync, IsUserBannedFromGroup, AddMember, GetMemberByUserAndGroupAsync (tracked), RemoveMember (detaches nav props), SearchGroupsAsync, GetGroupMembersAsync, GetRecommendedGroupsAsync
- **`StoryRepository`** — GetActiveStoriesAsync, GetStoriesForFeedAsync, AddViewerAsync, HasUserViewedStoryAsync, GetViewersCountAsync, DeleteExpiredStoriesAsync, GetUserStoriesAsync, GetFriendIdsAsync
- **`UserRepository`** — GetByIdAsync (AsNoTracking), GetByUsernameAsync, GetRandomUsersAsync, SearchUsersAsync, GetRecentAsync
- **`NotificationRepository`** — GetUnreadNotificationsAsync, GetByReceiverPagedAsync, GetOldReadNotificationsAsync
- **`PageRepository`** — AddFollower, RemoveFollower, GetPagesByFollowerIdAsync, GetAllAsync/GetByIdAsync (with Admin), SearchPagesAsync, IsFollowingAsync, GetFollowersCountAsync, GetFollowersAsync (paged)
- **`HashtagRepository`** — GetTrendingHashtagsAsync, GetHashtagByTagAsync, IncrementHashtagCountAsync
- **`ReportingRepository`** — HasUserReportedEntityAsync, GetReportCountForEntityAsync, CountPendingAsync, GetRecentPendingAsync

### 6.4 UnitOfWork
- Holds all repositories (injected via DI)
- `CompleteAsync()` → SaveChangesAsync
- `BeginTransactionAsync` / `CommitTransactionAsync` / `RollbackTransactionAsync` (IDbContextTransaction)

### 6.5 DBInitializer (Seeder)
- Applies migrations (`MigrateAsync`)
- Seeds roles: **"Admin"**, **"User"**
- Seeds admin user: `admin@sohba.com` / `Admin@123456` (fixed GUID `11111111-...`)
- Seeds 8 test users with fixed GUIDs and passwords (`Mohammed123!`, `Ahmed123!`, `Sara123!`, `Khaled123!`, `Layla123!`, `Omar123!`, `Nour123!`, `Youssef123!`)
- Creates friendships (accepted + pending), 3 groups, 3 pages, posts with hashtags, stories, private/friends-only/group/page posts, comments + replies, reactions, a report, and a saved/favorite post
- Idempotent (checks existence before creating)

### 6.6 File Storage (`LocalFileStorageService`)
- Implements `IFileStorageService`
- Allowed extensions: .jpg, .jpeg, .png, .gif, .webp
- Max file size: 5 MB
- Saves to `wwwroot/uploads/{subFolder}/{guid}{ext}`, returns relative URL `/uploads/{subFolder}/{file}`
- `DeleteFileAsync` deletes by relative URL

### 6.7 Email (`MailtrapEmailService`)
- Implements `IEmailService` via SMTP (Mailtrap sandbox)
- From: `noreply@sohba.com` / "Sohba System"
- Re-throws on failure

### 6.8 Background Service (`NotificationCleanupService`)
- Hosted service, runs every 24h
- Calls `NotificationService.DeleteOldNotificationsAsync(30)` (deletes read notifications older than 30 days)

### 6.9 DI (`InfrastructureServiceContainer`)
- DbContext (SQL Server, connection string "DefaultConnection")
- Identity with password/lockout/user options
- All repositories + UnitOfWork (scoped)
- `IFileStorageService` → `LocalFileStorageService`
- `IEmailService` → `MailtrapEmailService`
- `NotificationCleanupService` (hosted)
- `IDBInitializer`

---

## 7. Presentation / MVC / API Analysis

### 7.1 Program.cs Pipeline
1. Serilog bootstrap (console + rolling file `logs/sohba-.log`, 30 days retained)
2. JWT settings loaded + validated
3. JWT Bearer auth (HMAC-SHA256, issuer/audience/lifetime validation, ClockSkew=0); SignalR reads `access_token` query param for `/notificationHub`
4. **Rate limiting** (fixed window): Auth (5/min), Api (60/min), Feed (30/min), FriendRequest (30/min, queue 2), Dashboard (20/min), Default (100/min); 429 rejection
5. Infrastructure + Application services, health checks
6. Cookie auth config (`.SohbaAuth`, 10 min expiry, sliding, HttpOnly, SecurePolicy.Always, LoginPath `/Auth/Login`)
7. SignalR (detailed errors in dev, max message 1MB), `INotificationEventHandler` scoped
8. MVC + FluentValidation auto-validation
9. DB initialization on startup
10. Middleware: HSTS (non-dev), HTTPS redirect, static files, routing, auth, authorization, rate limiter, global exception handler (JSON 500 with correlationId), SignalR hub, health checks, MVC route `{controller=Landing}/{action=Index}/{id?}`

### 7.2 Controllers

**`LandingController`** (no auth) — `Index` shows landing page; sets `ViewBag.IsAuthenticated`/`UserName`.

**`AuthController`** (rate limit "Auth") — Login (GET/POST), Register (GET/POST), Logout (POST, Authorize), ForgotPassword (GET/POST JSON), ResetPassword (GET/POST JSON), AccessDenied. Uses `IAuthService` + `SignInManager`.

**`BaseController`** (base for authenticated controllers)
- `OnActionExecutionAsync`: loads recommended groups into `ViewBag.RecommendedGroups` and sets `ViewBag.JwtToken` (generates JWT per request) for non-JSON/AJAX requests
- `GetCurrentUserId()`: reads `ClaimTypes.NameIdentifier`
- `GetCurrentUserName()`

**`HomeController`** (Authorize, rate limit "Feed") — `Index` (feed + stories + trending hashtags), `GetPostCards` (AJAX paginated partial render for infinite scroll), `Error`.

**`PostsController`** (Authorize, rate limit "Api") — Create (GET/POST with file upload), GetPostDetails (JSON), Details, Edit (GET/POST JSON), Delete (JSON), React (JSON toggle), Comment (JSON), Favorites, ToggleSavePost (JSON), GetUserCollections, CreateCollection, SaveToCollection, ToggleFavorite, ReportPost, ChangeSavedPostTag, RemoveSavedPost, RemoveFromSaved, SavedPosts (grouped paged view), Hashtag, SearchByHashtag.

**`CommentsController`** (Authorize, rate limit "Api") — Delete (JSON).

**`FriendsController`** (Authorize, rate limit "FriendRequest") — Index, Requests, Blocked, SendRequest, Unfriend, BlockUser, UnblockUser, Suggestions, GetFriendSuggestions, GetPendingRequestsCount, CheckStatus, AcceptRequest, RejectRequest, CancelRequest.

**`ProfileController`** (Authorize, rate limit "Api") — Index (with privacy enforcement, friends list, posts, friendship status, blocked check), Edit (GET/POST with profile pic upload), Settings (GET/POST), Deactivate, DeleteAccount.

**`StoriesController`** (Authorize, rate limit "Api") — Index, Create (form + file upload), GetStory, MarkAsViewed, Delete, GetUserStories.

**`GroupsController`** (Authorize, rate limit "Api") — Discover (JSON), Index, Create (GET/POST with image upload), Join, Details, Edit (GET/POST), GetGroupPosts (partial), GetTabContent (members/about partials), Leave.

**`PagesController`** (Authorize, rate limit "Api") — Discover (JSON), Details, Delete, Index, Create (GET/POST with image upload), ToggleFollow, GetPagesList, GetPagePosts (partial), GetFollowersPreview, GetAllFollowers, CheckFollowStatus, Edit (GET/POST), GetPageStats.

**`NotificationsController`** (Authorize, rate limit "Api") — Index, GetUnreadCount, GetUnreadNotifications, MarkAsRead, MarkAllAsRead, Delete.

**`SearchController`** (Authorize, rate limit "Api") — Index (Results view), QuickSearch (JSON).

**`DashboardController`** (Authorize Roles="Admin", rate limit "Dashboard") — Index (stats + recent), Users (search/status/paged), BlockUser, UnblockUser, DeleteUser, Posts (search/source/paged), DeletePost, HidePost, Reports (status/paged), ResolveReport, DismissReport, DeleteReportedPost, GetUserDetails (partial), GetPostDetails (partial), GetReportDetails (partial).

### 7.3 ViewModels
- `HomeViewModel` (Posts, Stories, PagedResult, RecommendedGroups)
- `Post/PostCreateViewModel` (Title, Content, IsPrivate, ImageFile, ImageUrl, Privacy), `PostEditViewModel`, `SavedPostsViewModel`
- `Profile/ProfileViewModel` (Profile, Friends, Posts, IsOwnProfile, CanViewFriends, IsBlocked, FriendshipStatus), `EditProfileViewModel`, `SettingsViewModel`
- `Friend/FriendRequestsViewModel` (Pending, Sent, PendingCount, SentCount), `FriendSuggestionViewModel`
- `Group/GroupCreateViewModel`, `GroupEditViewModel`, `GroupDetailsViewModel` (Group, Members)
- `Page/PageCreateViewModel`, `PageEditViewModel`
- `Dashboard/DashboardViewModel` (counts + recent + chart data), `DashboardUsersViewModel`, `DashboardPostsViewModel`, `DashboardReportsViewModel`
- `Search/SearchViewModel` (Query, Results, ActiveTab)

### 7.4 Filters
- **`ValidationFilter`** — defined but **NOT registered** (commented out in `Program.cs`). Would return JSON errors for AJAX/JSON requests on invalid ModelState.

### 7.5 Hubs & Handlers
- **`NotificationHub`** (Authorize) — tracks user connections in a static `ConcurrentDictionary`, `JoinGroup`/`LeaveGroup`, `GetConnectionCount`.
- **`NotificationEventHandler`** — implements `INotificationEventHandler`; on `NotificationEvent`, sends `ReceiveNotification` to `Clients.User(receiverId)` with a DTO (id, message, notificationType, senderId, targetId, createdAt, isRead).

### 7.6 Extensions
- **`ApplicationBuilderExtensions.InitializeDatabaseAsync`** — creates scope, resolves `IDBInitializer`, calls `InitializeAsync()`.

---

## 8. Database Analysis

**Database:** SQL Server, database name `SocialMediaAppDB` (from `appsettings.json` connection string).

### 8.1 Tables & Keys
| Table | PK | Notes |
|-------|----|-------|
| `AspNetUsers` | Id (Guid, NEWSEQUENTIALID) | Identity user; unique Email index; global filter `!IsDeleted` |
| `AspNetRoles` | Id | Identity role |
| `AspNetUserRoles` | (UserId, RoleId) | |
| `AspNetUserClaims` / `AspNetUserLogins` / `AspNetUserTokens` / `AspNetRoleClaims` | standard | |
| `Posts` | Id | global filter `!IsDeleted`; indexes (UserId, CreatedAt), (CreatedAt), (SourceType, SourceId) |
| `Comments` | Id | self-referencing ParentCommentId; Content max 1000 |
| `Reactions` | Id | |
| `Hashtags` | Id | unique index on Tag |
| `PostHashtags` | (PostId, HashtagId) | join table |
| `PostReports` | Id | |
| `SavedPost` | Id | **Note:** snapshot shows a stray `PostId1` FK (legacy `SavedByUsers` nav) |
| `SavedCollections` | Id | |
| `Stories` | Id | global filter `!IsDeleted` |
| `StoryViewer` | Id | |
| `Friends` | (UserId, FriendUserId) | directed relationship |
| `Notification` | Id | indexes (ReceiverId, CreatedAt), (ReceiverId, IsRead), (CreatedAt, IsRead) |
| `Groups` | Id | |
| `GroupMembers` | (GroupId, UserId) | |
| `Pages` | Id | |
| `PageFollowers` | (PageId, UserId) | |

### 8.2 Relationships & Delete Behaviors
- **User → Posts:** 1:N, Restrict
- **User → Comments:** 1:N, Restrict
- **User → Reactions:** 1:N, Restrict
- **User → Stories:** 1:N, Restrict
- **User → Friends:** 1:N (both sides), Restrict
- **User → Notifications:** 1:N (Receiver, Sender), Restrict
- **User → GroupMemberships:** 1:N, Restrict
- **User → FollowedPages:** 1:N, Restrict
- **User → SentReports:** 1:N, Restrict
- **User → SavedPosts:** 1:N, Restrict
- **User → SavedCollections:** 1:N, **Cascade**
- **User → AdministeredGroups/Pages:** 1:N, Restrict
- **Post → Comments:** 1:N, Restrict
- **Post → Reactions:** 1:N, Restrict
- **Post → PostHashtags:** 1:N, Restrict
- **Post → PostReports:** 1:N, Restrict
- **Post → SavedPosts:** 1:N, Restrict
- **Comment → Comment (self):** ParentCommentId, no cascade (default)
- **Hashtag → PostHashtags:** 1:N, **Cascade**
- **SavedCollection → SavedPosts:** 1:N, **Cascade**
- **Story → StoryViewers:** 1:N, **Cascade**
- **Group → GroupMembers:** 1:N, Restrict
- **Page → PageFollowers:** 1:N, Restrict

### 8.3 Global Query Filters (Soft Delete)
- `User`: `!IsDeleted`
- `Post`: `!IsDeleted`
- `Story`: `!IsDeleted`

### 8.4 Migrations (5)
1. `20260706214629_InitialCreate`
2. `20260802150410_SyncModelChanges`
3. `20260806085753_AddSavedCollections`
4. `20260808150543_FixSavedPostPrimaryKey`
5. `20260809154029_AddCommentDepth`

### 8.5 Seed Data (via DBInitializer)
- Roles: Admin, User
- Admin user + 8 test users (fixed GUIDs, known passwords)
- Friendships (accepted + pending), 3 groups, 3 pages, posts (public/private/friends/group/page), hashtags, stories, comments + replies, reactions, 1 report, 1 saved/favorite post

### 8.6 Notable DB Observations
- **`SavedPost` has a stray `PostId1` FK** in the snapshot (from the `SavedByUsers` navigation on `Post`), creating a second FK to Posts. This is a legacy artifact.
- **`GroupMember` and `PageFollower` have an `Id` column** in the snapshot even though their PKs are composite — the `Id` is a non-key column.
- **`Notification` table** is named `Notification` (singular) in the snapshot.
- **`StoryViewer` table** is named `StoryViewer` (singular).
- **`SavedPost` table** is named `SavedPost` (singular) while the DbSet is `SavedPost`.

---

## 9. Authentication & Authorization

### 9.1 Dual Authentication
- **Cookie auth** for MVC pages (`.SohbaAuth` cookie, 10-min expiry, sliding, HttpOnly, SecurePolicy.Always)
- **JWT Bearer** for SignalR (`/notificationHub` reads `access_token` query param)
- Both are configured; `AddAuthentication` defaults to JWT Bearer, but cookie auth is also configured via `ConfigureApplicationCookie`.

### 9.2 ASP.NET Identity
- `User : IdentityUser<Guid>`, `IdentityRole<Guid>`
- Roles: **"Admin"**, **"User"**
- Password policy: digit, length >= 6, uppercase, lowercase (no non-alphanumeric required)
- Lockout: 5 failed attempts → 5 min lockout
- `RequireUniqueEmail = true`
- `RequireConfirmedEmail = false` (noted as TODO to enable after email service)

### 9.3 JWT
- Claims: sub (userId), email, name, jti, roles
- HMAC-SHA256, key from `Jwt:Key` (min 32 chars), issuer/audience from config, 7-day expiry
- `JwtService` validates settings on construction

### 9.4 Authorization
- `[Authorize]` on all feature controllers (Home, Posts, Comments, Friends, Profile, Stories, Groups, Pages, Notifications, Search)
- `[Authorize(Roles = "Admin")]` on `DashboardController`
- `[Authorize]` on `NotificationHub`
- `[Authorize]` on Logout
- `LandingController` is public

### 9.5 Rate Limiting
- Auth: 5/min
- Api: 60/min
- Feed: 30/min
- FriendRequest: 30/min (queue 2)
- Dashboard: 20/min
- Default: 100/min
- 429 Too Many Requests on rejection

### 9.6 Anti-Forgery
- `[ValidateAntiForgeryToken]` on POST actions
- JS sends `RequestVerificationToken` header from `__RequestVerificationToken` input


---

## 10. Feature-by-Feature Analysis

### 10.1 Posts
- **Where:** `PostService`, `PostRepository`, `PostsController`, `HomeController`, `_PostCard` partial
- **Flow:** View/JS → `PostsController.Create` → `PostService.CreatePostAsync` → domain `CanCreatePost` → access control (group member / page admin) → transaction (post + hashtags) → notifications → response
- **Entities:** `Post`, `Hashtag`, `PostHashtag`
- **Rules:** content or attachment required; privacy (Public/Friends/Private); soft delete; hide; group/page source access control; hashtag extraction (`#\w+`)
- **Auth:** `[Authorize]`
- **Edge cases:** privacy filtering in feed and search; `IsPrivate` vs `Privacy` dual fields; `MapPostsWithInteractions` filters by friendship

### 10.2 Comments / Replies
- **Where:** `InteractionService`, `InteractionRepository`, `PostsController.Comment`, `CommentsController.Delete`
- **Flow:** JS → `PostsController.Comment` → `InteractionService.AddCommentAsync` → domain `CanAddComment`/`CanReplyToComment` → depth check → persist → notification → recursive tree response
- **Entities:** `Comment`
- **Rules:** max depth 4; parent must belong to same post; blocked users cannot comment; delete by comment owner/post owner/admin
- **Auth:** `[Authorize]`
- **Edge cases:** recursive tree building; `ReplyCount` only for depth < 4; comment edit time limit (domain rule exists but no controller endpoint)

### 10.3 Reactions / Likes
- **Where:** `InteractionService`, `InteractionRepository`, `PostsController.React`
- **Flow:** JS → `PostsController.React` → toggle (remove if exists, else add/update) → `AddReactionAsync` → notification → new count
- **Entities:** `Reaction`
- **Rules:** one reaction per (user, post) — upsert; cannot react to deleted post; blocked users cannot react
- **Auth:** `[Authorize]`
- **Edge cases:** toggle behavior in controller (removes if exists, adds if not); `ReactionType` enum (Like, Love, Haha, Wow, Sad, Angry)

### 10.4 Saved Posts / Favorites / Collections
- **Where:** `InteractionService`, `InteractionRepository`, `PostsController` (Favorites, ToggleSavePost, GetUserCollections, CreateCollection, SaveToCollection, ToggleFavorite, ChangeSavedPostTag, RemoveSavedPost, RemoveFromSaved, SavedPosts)
- **Entities:** `SavedPost`, `SavedCollection`
- **Rules:** post is "saved" only in NON-Favorite collection; Favorites is separate; toggle favorite auto-creates Favorites collection; collections are per-user, unique by name; cannot save same post to same collection twice
- **Auth:** `[Authorize]`
- **Edge cases:** `GetSavedPostsGroupedPagedAsync` groups "All Saved" + collections; `RemoveSavedPostsFromCollectionsAsync` keeps Favorites; legacy `Tag`/`UserTag` fields for backwards compat

### 10.5 Stories
- **Where:** `StoryService`, `StoryRepository`, `StoriesController`, `sohba-stories.js`, `_StoryRail`/`_StoryViewer` partials
- **Flow:** JS → `StoriesController.Create` (file upload) → `StoryService.CreateStoryAsync` → daily limit → persist → feed; viewer → `GetUserStories` → `MarkAsViewed`
- **Entities:** `Story`, `StoryViewer`
- **Rules:** must have media; daily limit 10; 24h expiry; privacy (Public/FriendsOnly); owner or friend can view; owner cannot be a viewer
- **Auth:** `[Authorize]`
- **Edge cases:** `GetStoriesForFeedAsync` filters by owner/public/friends-only at service level; `GetUserStoriesAsync` has a friendship query precedence bug (see §16); `DeleteExpiredStoriesAsync` exists but is not called by any scheduled job

### 10.6 Notifications
- **Where:** `NotificationService`, `NotificationRepository`, `NotificationsController`, `NotificationHub`, `NotificationEventHandler`, `header.js`
- **Flow:** Any service calls `CreateNotificationAsync` → domain `ShouldSendNotification` → preference check → persist → `NotificationEvent` → SignalR `ReceiveNotification` → client toast + badge
- **Entities:** `Notification`
- **Rules:** no self-notifications; preference-based suppression; bundling rule exists (15 min) but not implemented in service; mark-as-read only by owner
- **Auth:** `[Authorize]` (hub + controller)
- **Edge cases:** SignalR errors logged but non-fatal; client polls `GetUnreadCount` every 30s + real-time via SignalR; `DeleteOldNotificationsAsync` (30 days) run by background service

### 10.7 Users / Profiles
- **Where:** `UserService`, `UserSettingsService`, `ProfileController`, `UserRepository`
- **Flow:** `ProfileController.Index` → `UserService.GetProfileAsync(userId, currentUserId)` → privacy check → friends list (if allowed) → posts → friendship status
- **Entities:** `User`
- **Rules:** private account requires friendship; blocked users cannot view; only owner can edit; soft delete; deactivate
- **Auth:** `[Authorize]`
- **Edge cases:** `GetProfileAsync` overload (owner vs viewer); `IsBlockedAsync` only checks one direction; settings stored on User entity (no separate table)

### 10.8 Friends / Social Relationships
- **Where:** `FriendshipService`, `FriendshipRepository`, `FriendsController`
- **Flow:** JS → `FriendsController.SendRequest` → `FriendshipService.SendFriendRequestAsync` → domain checks → create Pending row → notification
- **Entities:** `Friend`
- **Rules:** no self-request; no duplicate pending; not blocked; not already friends; accept/decline/cancel; unfriend deletes both directions; block deletes friendships + creates Blocked row
- **Auth:** `[Authorize]`
- **Edge cases:** `GetByUsersAsync` searches both directions; `IsUserBlockedAsync` only checks `UserId → targetId` (one direction); friend suggestions exclude self/friends/sent/blocked

### 10.9 Groups
- **Where:** `GroupService`, `GroupRepository`, `GroupsController`
- **Flow:** `GroupsController.Create` → `GroupService.CreateGroupAsync` → group + admin member; `Join` → `JoinGroupAsync` → domain check → member + notification
- **Entities:** `Group`, `GroupMember`
- **Rules:** only owner can delete/update; join requires not banned, not private; kick hierarchy (Admin/Owner can kick Member, not Admin/Owner); sole admin cannot leave; member can post (checked in PostService)
- **Auth:** `[Authorize]`
- **Edge cases:** `AsNoTrackingWithIdentityResolution` to avoid EF tracking conflicts; `RemoveMember` detaches nav props; `GetRecommendedGroupsAsync` excludes user's groups, orders by member count

### 10.10 Pages
- **Where:** `PageService`, `PageRepository`, `PagesController`
- **Flow:** `PagesController.Create` → `PageService.CreatePageAsync` → page + auto-follow admin; `ToggleFollow` → follow/unfollow
- **Entities:** `Page`, `PageFollower`
- **Rules:** page name min 3 chars; admin cannot follow own page; only admin can post/delete/update; follow/unfollow notifications
- **Auth:** `[Authorize]`
- **Edge cases:** `ToggleFollowPageAsync` returns bool (isFollowing); `GetPageStats` uses `Guid.Empty` as current user

### 10.11 Search
- **Where:** `SearchService`, `SearchController`, `header.js` (quick search)
- **Flow:** `SearchController.Index` → `GlobalSearchAsync` → posts (privacy-aware), users, groups, pages
- **Entities:** Post, User, Group, Page
- **Rules:** min query length 2; posts filtered by privacy (own/public/friends)
- **Auth:** `[Authorize]`
- **Edge cases:** `QuickSearch` returns top 3 of each; `SearchController.Index` explicitly returns "Results" view

### 10.12 Hashtags
- **Where:** `HashtagService`, `HashtagRepository`, `PostRepository.AddHashtagsToPostAsync`, `PostsController.Hashtag`/`SearchByHashtag`
- **Entities:** `Hashtag`, `PostHashtag`
- **Rules:** unique tag; count incremented on use; trending by count desc
- **Auth:** `[Authorize]`
- **Edge cases:** hashtags extracted at post creation; `GetPostsByHashtagAsync` joins through PostHashtag

### 10.13 Reporting / Moderation
- **Where:** `ReportingService`, `ReportingRepository`, `PostsController.ReportPost`, `DashboardController` (Reports)
- **Entities:** `PostReport`
- **Rules:** no duplicate report; auto-hide post at 5 reports (sets IsDeleted); only admin can review; resolve notifies reporter + owner
- **Auth:** `[Authorize]` (report), `[Authorize(Roles="Admin")]` (dashboard)
- **Edge cases:** `ReportPostAsync` (old) commented out; admin notification targets `admin@sohba.com`; `DeleteReportedPost` deletes post + resolves report

### 10.14 Dashboard / Admin
- **Where:** `DashboardController`, `DashboardViewModel` etc.
- **Flow:** `DashboardController.Index` → counts (users, posts, groups, pages, pending reports) + recent lists
- **Rules:** Admin role only; rate limited
- **Edge cases:** `UsersLast7Days` chart data is **hardcoded** (not real); `GetUserDetails`/`GetPostDetails`/`GetReportDetails` return partials; `HidePost` called without `isAdmin` flag in controller (relies on `post.UserId == userId` check)

### 10.15 Media / File Handling
- **Where:** `LocalFileStorageService`, `IFileStorageService`, controllers (Posts, Profile, Stories, Groups, Pages)
- **Flow:** Controller receives `IFormFile` → `SaveFileAsync` → validates extension/size → saves to `wwwroot/uploads/{subFolder}` → returns URL → stored in entity
- **Rules:** allowed extensions (.jpg/.jpeg/.png/.gif/.webp); 5MB max; unique filename (GUID)
- **Auth:** `[Authorize]`
- **Edge cases:** `MediaDomainService` has stricter rules (2MB profile pic, video support) but `LocalFileStorageService` only allows images; `DeleteFileAsync` exists but is rarely called

### 10.16 Authentication Flows
- **Register:** email uniqueness → create user → "User" role → JWT
- **Login:** password check with lockout → cookie sign-in → JWT
- **Forgot/Reset Password:** token via email → reset
- **Logout:** `SignInManager.SignOutAsync`

---

## 11. Important End-to-End Flows

### 11.1 Home Feed (Infinite Scroll)
1. Browser → `GET /Home/Index?page=1`
2. `HomeController.Index` → `PostService.GetFeedAsync(userId, 1, 10)` → `PostRepository.GetTimelineAsync` (privacy-aware SQL) → `MapPostsWithInteractions` (counts, reactions, saved flags)
3. Also loads `StoryService.GetStoriesForFeedAsync` + `HashtagService.GetTrendingHashtagsAsync`
4. Renders `Home/Index.cshtml` with `_PostCard` partials
5. On scroll → JS `feed.js` → `GET /Home/GetPostCards?page=N` → returns rendered HTML partial → appended to DOM (dedup by post-id)

### 11.2 Create Post with Image
1. `POST /Posts/Create` (multipart form + antiforgery)
2. `PostsController.Create` → `IFileStorageService.SaveFileAsync` (validates + saves image) → `PostService.CreatePostAsync`
3. Domain `CanCreatePost` → group/page access control → transaction (post + hashtags) → notifications to group/page admin
4. Redirect to Home/Group/Page

### 11.3 React to a Post (Toggle)
1. JS → `POST /Posts/React` (JSON `{postId, reactionType}`)
2. `PostsController.React` → checks existing reaction → `RemoveReactionAsync` (if exists) or `AddReactionAsync` (if not) → returns `{action, newCount}`
3. `AddReactionAsync` → domain `CanAddReaction` → upsert → notification to post owner

### 11.4 Comment / Reply
1. JS → `POST /Posts/Comment` (JSON `{postId, content, parentCommentId?}`)
2. `PostsController.Comment` → `InteractionService.AddCommentAsync` → domain checks (deleted, blocked, depth) → persist → notification → rebuild comment tree → find new node → return JSON
3. Delete: `POST /Comments/Delete` → `DeleteCommentAsync` → domain `CanDeleteComment` (owner/post-owner/admin)

### 11.5 Friend Request Lifecycle
1. `POST /Friends/SendRequest` → `SendFriendRequestAsync` → domain checks → Pending row → notification
2. `POST /Friends/AcceptRequest` → `AcceptFriendRequestAsync` → status Accepted → notification
3. `POST /Friends/RejectRequest` / `CancelRequest` → delete row
4. `POST /Friends/BlockUser` → delete friendships both directions → Blocked row

### 11.6 Story Viewing
1. JS `openStoryViewer(userId)` → `GET /Stories/GetUserStories?userId=X` → `StoryService.GetUserStoriesAsync` (privacy + expiry)
2. `showStory(index)` → `POST /Stories/MarkAsViewed` (fire-and-forget) → `MarkStoryAsViewedAsync` (adds viewer if not already)
3. Auto-advance every 5s; keyboard nav (←/→/Esc)

### 11.7 Real-time Notification
1. Any service calls `NotificationService.CreateNotificationAsync` → persists → raises `NotificationEvent`
2. `NotificationEventHandler.HandleAsync` → `IHubContext<NotificationHub>.Clients.User(receiverId).SendAsync("ReceiveNotification", dto)`
3. Client `header.js` → `handleNotificationReceived` → toast + badge increment + prepend to dropdown
4. Client also polls `GET /Notifications/GetUnreadCount` every 30s

### 11.8 Global Search
1. Header input → debounced `GET /Search/QuickSearch?q=...` → `SearchService.GlobalSearchAsync` → top 3 each → dropdown
2. Full page: `GET /Search?q=...&tab=...` → `SearchController.Index` → "Results" view

---

## 12. Frontend / JavaScript Behavior

### 12.1 Core (`sohba-core.js`)
- `SohbaApp.toast(message, type)` — toast notifications
- `SohbaApp.post(url, data)` — JSON POST with antiforgery token; normalizes `Success`/`Error` casing; handles non-JSON responses (session expired, 429, server error); never throws
- `SohbaApp.postForm(url, formData)` — multipart POST
- `SohbaApp.get(url)` — GET JSON
- `SohbaApp.toggleMenu`, `setButtonLoading`, `resetButton`
- Auto-disables submit buttons on form submit (with jQuery validation check)

### 12.2 Feature Modules (`wwwroot/js/features/`)
- **`feed.js`** — infinite scroll + "Load More" button; dedup by post-id; calls `/Home/GetPostCards`
- **`posts.js`** — `deletePost` (confirm modal → `/Posts/Delete` → DOM removal), `editPost` (form → `/Posts/Edit` → DOM update)
- **`comments.js`** — `deleteComment` (confirm modal → `/Comments/Delete` → DOM removal + count decrement)
- **`friends.js`** — search/filter UI, `sendFriendRequest`, `acceptRequest`, `rejectRequest`, `cancelRequest`, `blockUser`, `unblockUser`, `checkFriendshipStatus`, profile-specific variants
- **`stories.js`** — story rail scroll, open create-story modal, open story viewer
- **`header.js`** — notification count polling, load notifications dropdown, mark read/all, delete, SignalR connection (`/notificationHub` with JWT from `<meta name="jwt-token">`), mobile search, profile dropdown, quick search (debounced 300ms)
- **`search.js`** — (search-specific behaviors)
- **`groups.js`** — group join/leave/tab behaviors
- **`dashboard.js`** — admin dashboard actions (block/unblock/delete user, delete/hide post, resolve/dismiss report, delete reported post)
- **`sidebar.js`** — sidebar toggle
- **`modal.js`** — modal open/close

### 12.3 Story Viewer (`sohba-stories.js`)
- `openStoryViewer(userId)` → fetch user stories → show modal
- `showStory(index)` → render media, mark viewed
- `startProgress()` → 5s auto-advance
- `navigateStory(direction)` → next/prev
- Keyboard navigation

### 12.4 AJAX Endpoints Used by JS
| JS | Endpoint |
|----|----------|
| feed.js | `GET /Home/GetPostCards` |
| posts.js | `POST /Posts/Delete`, `POST /Posts/Edit` |
| comments.js | `POST /Comments/Delete` |
| friends.js | `POST /Friends/SendRequest`, `/AcceptRequest`, `/RejectRequest`, `/CancelRequest`, `/BlockUser`, `/UnblockUser`; `GET /Friends/CheckStatus` |
| stories.js | `GET /Stories/GetUserStories`, `POST /Stories/MarkAsViewed` |
| header.js | `GET /Notifications/GetUnreadCount`, `/GetUnreadNotifications`; `POST /Notifications/MarkAsRead`, `/MarkAllAsRead`, `/Delete`; `GET /Search/QuickSearch`; SignalR `/notificationHub` |
| dashboard.js | `POST /Dashboard/BlockUser`, `/UnblockUser`, `/DeleteUser`, `/DeletePost`, `/HidePost`, `/ResolveReport`, `/DismissReport`, `/DeleteReportedPost` |

### 12.5 Client-side Validation
- jQuery Validation + Unobtrusive (libs present)
- `SohbaApp.post` checks `jQuery(form).valid()` before submitting
- Server-side FluentValidation auto-validation also runs

---

## 13. External Integrations

- **Mailtrap SMTP** — `MailtrapEmailService` sends password-reset emails via `sandbox.smtp.mailtrap.io:2525` (credentials in `appsettings.json`). Used by `AuthService.ForgotPasswordAsync`.
- **SignalR** — real-time notifications via `/notificationHub` (self-hosted, not external).
- **Local file storage** — `wwwroot/uploads` (no cloud storage; `IFileStorageService` abstraction allows swapping to S3/Azure Blob).
- **ui-avatars.com** — used for default profile pictures in seed data and JS fallbacks.
- **No other external APIs** (no social login, no cloud storage, no payment, no maps).

---

## 14. Business Rules (Consolidated)

1. **Post content:** must have text or attachment.
2. **Post privacy:** Public (anyone), Friends (friends only), Private (owner + friends via `CanViewPost`).
3. **Post edit/delete:** only owner (admin can delete any).
4. **Post hide:** owner or admin.
5. **Group/Page posts:** only active non-banned group members; only page admin.
6. **Comments:** max depth 4; parent must belong to same post; blocked users cannot comment; delete by comment owner/post owner/admin; edit time limit (rule exists, no endpoint).
7. **Reactions:** one per (user, post) — upsert; cannot react to deleted post; blocked users cannot react.
8. **Saved/Favorites:** "saved" = non-Favorite collection; Favorites separate; collections unique per user by name.
9. **Stories:** must have media; daily limit 10; 24h expiry; owner or friend can view; owner not a viewer.
10. **Friends:** no self-request; no duplicate pending; not blocked; not already friends; unfriend removes both directions; block removes friendships + blocks.
11. **Groups:** only owner can delete/update; join requires not banned/not private; kick hierarchy; sole admin cannot leave.
12. **Pages:** name min 3 chars; admin cannot follow own page; only admin can post/delete/update.
13. **Search:** min query length 2; posts privacy-filtered.
14. **Hashtags:** unique; count incremented; trending by count.
15. **Reporting:** no duplicate report; auto-hide at 5 reports; only admin reviews; resolve notifies reporter + owner.
16. **Notifications:** no self-notifications; preference-based suppression; mark-read only by owner; cleanup after 30 days.
17. **Media:** images only (.jpg/.jpeg/.png/.gif/.webp), 5MB max (LocalFileStorageService); stricter domain rules (2MB profile pic, video support) exist but not enforced by the storage service.
18. **Auth:** password policy (digit, 6+ length, upper, lower); lockout 5/5min; unique email.
19. **Rate limits:** Auth 5/min, Api 60/min, Feed 30/min, FriendRequest 30/min, Dashboard 20/min, Default 100/min.

---

## 15. Security-Relevant Areas

- **JWT:** HMAC-SHA256, issuer/audience/lifetime validation, ClockSkew=0. **Concern:** `appsettings.json` contains a placeholder key (`YourSuperSecretKeyHereAtLeast32CharactersLong!`) — must be overridden in production.
- **Cookie auth:** HttpOnly, SecurePolicy.Always, SameSite=Lax, 10-min sliding expiry.
- **Anti-forgery:** `[ValidateAntiForgeryToken]` on POSTs; JS sends `RequestVerificationToken` header.
- **Rate limiting:** protects auth and API endpoints from brute force/abuse.
- **XSS:** JS builds HTML strings from server data (e.g., `header.js` notification rendering, `sohba-stories.js` media injection). Server data is user-generated (names, messages, content) — **potential XSS risk** if not escaped. Razor views use `@` escaping server-side, but client-side `innerHTML`/`insertAdjacentHTML` with raw data is a concern.
- **Authorization:** `[Authorize]` on feature controllers; `[Authorize(Roles="Admin")]` on Dashboard; `[Authorize]` on SignalR hub.
- **IDOR / authorization checks:** Most service methods take `userId` from the controller (`GetCurrentUserId()`) and enforce ownership in domain services (e.g., `CanUpdatePost`, `CanDeletePost`, `CanUpdateGroup`, `CanUpdatePage`). However, some checks are one-directional (e.g., `IsUserBlockedAsync` only checks `userId → targetId`).
- **Soft delete:** `IsDeleted` flags with global query filters; deleted users/posts/stories are hidden from queries.
- **File upload:** extension + size validation in `LocalFileStorageService`; unique GUID filenames.
- **Email:** Mailtrap sandbox credentials in `appsettings.json` (dev only).
- **Error handling:** global exception handler returns generic JSON with correlationId (no stack traces leaked); Serilog logs full exceptions server-side.
- **`ValidationFilter`** is defined but not registered — ModelState errors are handled per-controller instead.
- **`ForgotPassword`** returns success regardless of whether the user exists (anti-enumeration), but the service logs a warning when the user is not found.

---

## 16. Potentially Risky / Complex Areas

1. **EF Tracking Conflicts:** `GroupRepository` uses `AsNoTrackingWithIdentityResolution` and `RemoveMember` detaches nav props to avoid duplicate-tracking exceptions. `UserRepository.GetByIdAsync` uses `AsNoTracking`. `GroupService.UpdateGroupAsync` relies on this pattern. This is fragile and depends on consistent usage.
2. **`SavedPost` dual FK (`PostId` + `PostId1`):** The model snapshot shows a stray `PostId1` FK from the `SavedByUsers` navigation on `Post`. This is a legacy artifact that could cause confusion or subtle bugs.
3. **`IsPrivate` vs `Privacy`:** Two overlapping fields on `Post`. `CanViewPost` uses `IsPrivate`, while the timeline query uses `Privacy`. Inconsistent usage could cause privacy leaks or over-restriction.
4. **`GetUserStoriesAsync` friendship query precedence bug:** The `AnyAsync` condition `(f.UserId == currentUserId && f.FriendUserId == userId) || (f.UserId == userId && f.FriendUserId == currentUserId) && f.Status == Accepted` — the `&&` binds tighter than `||`, so the status filter only applies to the second branch. This could allow non-friends to see friends-only stories in some cases.
5. **`GetStoriesForFeedAsync` repository method** only returns public + own stories (not friends-only), but the service re-filters by friend IDs. The repository comment notes this is a TODO.
6. **N+1 query patterns:** `StoryService.GetStoriesForFeedAsync` and `GetUserStoriesAsync` call `GetViewersCountAsync`/`HasUserViewedStoryAsync` per story in a loop. `MapPostsWithInteractions` does batched queries (better). `GetSavedPostsGroupedPagedAsync` calls `MapPostsToResponse` per collection.
7. **`BaseController` service locator:** Resolves services via `HttpContext.RequestServices` and generates a JWT per request — performance and testability concerns.
8. **Hardcoded values:** `userLocation = "Egypt"` in `PostService`; report auto-hide threshold `5`; story daily limit `10`; `admin@sohba.com` in `ReportingService`; dashboard chart data hardcoded.
9. **Commented-out code:** Large blocks in `PostsController` (GetPostDetails), `AuthController` (ForgotPassword/ResetPassword), `ReportingService` (ReportPostAsync), `HomeController` (LoadMore), `GroupsController` (GetGroupMembers/GetAboutTab), `MappingProfile` (Friend mapping), `UserRepository` (EmailExists), `Program.cs` (ValidationFilter). These are dead code.
10. **`ValidationFilter` not registered:** The filter exists but is commented out in `Program.cs`, so it's inactive.
11. **`INotificationHubService` interface** exists but is not registered/implemented (commented out in DI).
12. **`DeleteExpiredStoriesAsync`** exists in `StoryRepository` but is not invoked by any scheduled job — expired stories are only filtered at query time, not physically cleaned.
13. **`NotificationCleanupService`** deletes old read notifications but there's no corresponding story cleanup.
14. **`GetPageStats`** uses `Guid.Empty` as current user — privacy filtering may behave unexpectedly.
15. **`DashboardController.HidePost`** calls `HidePostAsync` without `isAdmin: true`, so it relies on `post.UserId == userId` — an admin hiding another user's post would fail.
16. **`DashboardController.DeleteReportedPost`** calls `DeletePostAsync` without `isAdmin: true` — same concern.
17. **`UserSettingsService`** has TODO comments about a separate settings table; settings are stored on the User entity.
18. **`JwtService`** has commented-out `IConfiguration` usage; relies on `IOptions<JwtSettings>`.
19. **`origin)` file** in the `Sohba/` root — a stray file, likely a misnamed artifact.
20. **`Search/Results.cshtml.cs`** — a Razor Page code-behind file inside a Views folder (unusual for MVC).

---

## 17. Unknowns / Areas That Could Not Be Fully Traced

The following areas were identified but not fully inspected during this analysis. They are listed so a follow-up analysis (or the testing-plan builder) knows where gaps exist:

1. **Razor view markup** — The exact HTML/JS in most views was not read in detail. Key partials whose markup drives client behavior: `_PostCard.cshtml`, `_Header.cshtml`, `_Sidebar.cshtml`, `_RightSidebar.cshtml`, `_Stories.cshtml`, `_StoryRail.cshtml`, `_StoryViewer.cshtml`, `_PostModal.cshtml`, `_SavePostModal.cshtml`, `_ShareModal.cshtml`, `_ReportModal.cshtml`, `_ConfirmModal.cshtml`, `_CreatePost.cshtml`, `_CreateStoryModal.cshtml`. Element IDs and data attributes used by JS are inferred from the JS files.
2. **Some JS files** — `features/search.js`, `features/groups.js`, `features/dashboard.js`, `features/sidebar.js`, `features/modal.js`, `sohba-modal.js`, `sohba-posts.js`, `site.js`, `script.js`, `simplebar.js` were not fully read; behaviors are inferred from filenames and controller endpoints.
3. **`Sohba/Models/`** — only `ErrorViewModel` was referenced; full contents not enumerated.
4. **`Sohba/Views/Dashboard/Partials/`** and **`Sohba/Views/Groups/Partials/`** — contents not enumerated.
5. **`Sohba.Application/DTOs/PostAggregate/Requests/`** — subfolder contents not enumerated (referenced by `PostsController`: `ReactionRequestDto`, `ToggleSaveRequestDto`, `ChangeTagRequestDto`, `RemoveSavedRequestDto`, `SaveToCollectionDto`, `CreateSavedCollectionDto`).
6. **`Sohba/appsettings.Development.json`** — not read (only `appsettings.json`).
7. **`Sohba/package.json`** — npm contents not read (likely Tailwind build tooling).
8. **`Sohba.Domain/Sohba.Domain.csproj`** — not read (only Application/Infrastructure/Web csproj).
9. **`Sohba/Properties/`** (launchSettings) — not read.
10. **`Sohba/Views/Shared/_Layout.cshtml` / `_AppLayout.cshtml`** — which layout is used by which views, and exact script/style includes, not fully traced.
11. **`Sohba/Views/Search/Results.cshtml.cs`** — code-behind not read.
12. **`Sohba/wwwroot/css/*`** — styling details not analyzed (Tailwind-based).
13. **`Sohba/wwwroot/syntax-highlighter/`** — purpose not confirmed (likely for code blocks in posts).
14. **`Sohba/Views/Shared/_ValidationScriptsPartial.cshtml`** — exact validation script includes not read.
15. **`Sohba/Views/Shared/_Layout.cshtml.css`** — scoped CSS not read.
16. **`Sohba/Views/Home/Index.cshtml`** and other main views — exact markup, data attributes, and inline event handlers not fully read.
17. **`Sohba/Views/Dashboard/Index.cshtml`** and other dashboard views — chart rendering and table markup not read.
18. **`Sohba/Views/Profile/Index.cshtml`** — profile page markup (friends list, posts, friendship status buttons) not read.
19. **`Sohba/Views/Posts/SavedPosts.cshtml` / `Favorites.cshtml` / `Hashtag.cshtml`** — markup not read.
20. **`Sohba/Views/Stories/Index.cshtml` / `Notifications/Index.cshtml` / `Search/Results.cshtml`** — markup not read.
21. **`Sohba/Views/Auth/*.cshtml` / `Landing/Index.cshtml` / `Friends/*.cshtml` / `Groups/*.cshtml` / `Pages/*.cshtml` / `Posts/Create|Edit|Details.cshtml` / `Profile/Edit|Settings|PrivateProfile.cshtml` / `Shared/Error.cshtml`** — markup not read.
22. **`Sohba/Views/Shared/_ViewImports.cshtml` / `_ViewStart.cshtml`** — not read.
23. **`Sohba/Views/Dashboard/Partials/_UserDetails.cshtml` / `_PostDetails.cshtml` / `_ReportDetails.cshtml`** — not read.
24. **`Sohba/Views/Groups/_AboutTab.cshtml` / `_MembersTab.cshtml`** — not read.
25. **`Sohba/Views/Groups/Partials/`** — not enumerated/read.
26. **`Sohba/Views/Shared/Partials/*.cshtml`** — the individual partial views listed in item 1 were not read in detail.
27. **`Sohba/wwwroot/uploads/`** — contains subfolders (groups/, pages/, posts/, stories/) but no interest in inspection; runtime artifacts.
28. **`Sohba/Views/Profile/PrivateProfile.cshtml`** — private-profile view markup not read.
29. **`Sohba/Views/Posts/Details.cshtml`** — post details view markup not read.
30. **`Sohba/Views/Posts/SavedPosts.cshtml`** — grouped saved posts view markup not read.
31. **`Sohba/Views/Posts/Favorites.cshtml`** — favorites view markup not read.
32. **`Sohba/Views/Posts/Hashtag.cshtml`** — hashtag view markup not read.
33. **`Sohba/Views/Stories/Index.cshtml`** — stories page markup not read.
34. **`Sohba/Views/Notifications/Index.cshtml`** — notifications page markup not read.
35. **`Sohba/Views/Search/Results.cshtml`** — search results markup not read.
36. **`Sohba/Views/Auth/Login.cshtml` / `Register.cshtml` / `Logout.cshtml`** — auth views markup not read.
37. **`Sohba/Views/Landing/Index.cshtml`** — landing page markup not read.
38. **`Sohba/Views/Friends/Index.cshtml` / `Requests.cshtml` / `Blocked.cshtml` / `Suggestions.cshtml` / `Find.cshtml`** — friends views markup not read.
39. **`Sohba/Views/Groups/Index.cshtml` / `Details.cshtml` / `Create.cshtml` / `Edit.cshtml`** — group views markup not read.
40. **`Sohba/Views/Pages/Index.cshtml` / `Details.cshtml` / `Create.cshtml` / `Edit.cshtml`** — page views markup not read.
41. **`Sohba/Views/Profile/Edit.cshtml` / `Settings.cshtml`** — profile edit/settings markup not read.
42. **`Sohba/Views/Posts/Create.cshtml` / `Edit.cshtml`** — post create/edit form markup not read.
43. **`Sohba/Views/Shared/Error.cshtml`** — error page markup not read.
44. **`Sohba/Views/Shared/_ViewImports.cshtml` / `_ViewStart.cshtml`** — view imports/start not read.
45. **`Sohba/ViewModels/Dashboard/*.cshtml`** — dashboard viewmodels were partially read (view models exist) but dashboard partial views not read.
46. **`Sohba/ViewModels/Friend/FriendSuggestionViewModel.cs`** — not read.
47. **`Sohba/ViewModels/Post/SavedPostsViewModel.cs`** — not read.
48. **`Sohba/ViewModels/Search/SearchViewModel.cs`** — not read (structure inferred).
49. **`Sohba/ViewModels/Group/*.cshtml`** — group viewmodels not read in detail (inferred from usage).
50. **`Sohba/ViewModels/Page/*.cshtml`** — page viewmodels not read in detail (inferred from usage).
51. **`Sohba/Models/ErrorViewModel.cs`** — not read (referenced by HomeController).
52. **`Sohba/package.json`** — npm dependencies not read; presence of `package-lock.json` indicates an npm-managed frontend build (likely Tailwind).
53. **`Sohba/Properties/launchSettings.json`** — not read; startup URLs/profiles unknown.
54. **`Sohba/appsettings.Development.json`** — not read; may override connection string / JWT / mail settings for dev.
55. **`Social Media App ERD.png`** — an ERD image exists at repo root; not analyzed.
56. **The `AI/` and `Ai Respond/` directories** — contain generated analysis reports (XML/Markdown) about this project; not analyzed as source code.
57. **`IDE/` directory** — contains project guidance docs (RULES.md, ARCHITECTURE.md, etc.); not analyzed as source code.
58. **`AI/` and `Ai Respond/` outputs** — may describe planned (unimplemented) features; not treated as authoritative for what exists in code.
59. **`Sohba/Views/Shared/Partials/_ShareModal.cshtml`** — share modal UI exists as a partial; the actual share functionality (creating a share post/URL) was not found in controllers/services — likely UI-only.
60. **`Sohba/Views/Shared/Partials/_SavePostModal.cshtml`** — save-post modal exists; collection save flow is implemented in `PostsController` (SaveToCollection).
61. **`Sohba/Views/Shared/Partials/_ReportModal.cshtml`** — report modal exists; report flow is implemented in `PostsController.ReportPost`.
62. **`Sohba/Views/Shared/Partials/_PostModal.cshtml`** — post detail modal exists; `GetPostDetails` action exists.
63. **`_ShareModal` / share functionality** — no service method for sharing posts was found; share may be a UI-only feature.
64. **Comment editing** — domain rule `CanEditComment` exists but no controller endpoint invokes it; UI may not expose edit.
65. **Story replies / highlights** — domain rules exist (`CanReplyToStory`, `CanHighlightStory`) but no controller/service endpoint implements them.
66. **User contact info / username change / friends-list privacy** — domain rules exist (`CanViewContactInfo`, `CanChangeUsername`, `CanViewFriendsList`) but no UI/controller flow was found invoking them.
67. **Group invitations / promotions / bans** — domain rules exist (`CanInviteToGroup`, `CanPromoteMember`, `CanKickMember`) but `KickMember` is implemented; invite/promote/ban flows were not found in controllers.
68. **Media domain service enforcement** — `MediaDomainService` rules (2MB profile pic, video extensions, 50MB video) are not enforced by `LocalFileStorageService` (which only allows images ≤5MB); the domain rules appear unused by actual upload paths.
69. **Notification bundling** — `ShouldBundleNotifications` rule exists but is not invoked by `NotificationService`.
70. **`INotificationHubService`** — interface exists in `Sohba.Application/Interfaces` but is not implemented or registered.
71. **Razor Pages** — `Search/Results.cshtml.cs` is a Razor Page code-behind inside an MVC Views folder; usage unclear.
72. **`Sohba/origin)`** — a stray file at `Sohba/origin)`; contents unknown.
73. **`Sohba/Views/Shared/Partials/_CreatePost.cshtml`** — create-post partial exists; used by Posts/Create or a modal; interaction not traced.
74. **`Sohba/Views/Shared/Partials/_CreateStoryModal.cshtml`** — create-story modal exists; `openStoryModal` is referenced by `features/stories.js` but the modal implementation file (`sohba-modal.js` / `modal.js`) was not read.
75. **`Sohba/Views/Shared/Partials/_ConfirmModal.cshtml`** — confirm modal exists; `window.showConfirmModal` is used by `posts.js`/`comments.js`/`friends.js`; the implementation was not read.
76. **`Sohba/Views/Shared/Partials/_StoryViewer.cshtml`** — story viewer modal elements (`storyViewerModal`, `storyProgress`, etc.) are referenced by `sohba-stories.js`; markup not read.
77. **`Sohba/Views/Shared/Partials/_Stories.cshtml` / `_StoryRail.cshtml`** — story rail markup not read; `storiesContainer` element referenced by `features/stories.js`.
78. **`Sohba/wwwroot/js/features/modal.js`** — modal open/close behavior not read.
79. **`Sohba/wwwroot/js/features/groups.js`** — group join/leave/tab behaviors not read (endpoints exist in `GroupsController`).
80. **`Sohba/wwwroot/js/features/dashboard.js`** — dashboard admin actions not read (endpoints exist in `DashboardController`).
81. **`Sohba/wwwroot/js/features/search.js`** — Search page behaviors not read.
82. **`Sohba/wwwroot/js/features/sidebar.js`** — sidebar behaviors not read.
83. **`Sohba/wwwroot/js/sohba-modal.js`** — modal logic (openStoryModal, showConfirmModal, etc.) not read.
84. **`Sohba/wwwroot/js/sohba-posts.js`** — post-specific JS not read (may overlap with `features/posts.js`).
85. **`Sohba/wwwroot/js/site.js` / `script.js`** — general site JS not read.
86. **`Sohba/wwwroot/js/simplebar.js`** — scrollbar library; not read.
87. **`Sohba/wwwroot/css/*`** — styling details not analyzed.
88. **`Sohba/wwwroot/syntax-highlighter/`** — purpose not confirmed.
89. **`Sohba/wwwroot/Icon.png` / `images/*`** — static assets; not analyzed.
90. **`Sohba/Views/Shared/_Layout.cshtml.css`** — scoped CSS not read.
91. **`Sohba/Views/Shared/_ValidationScriptsPartial.cshtml`** — not read.
92. **`Sohba/Views/_ViewImports.cshtml` / `_ViewStart.cshtml`** — not read.
93. **`Sohba/Views/Dashboard/Partials/*`** — dashboard partial views not read.
94. **`Sohba/Views/Groups/Partials/*`** — group partial views not read.
95. **`Sohba/Views/Shared/Partials/_Header.cshtml`** — header markup not read (element IDs inferred from `header.js`).
96. **`Sohba/Views/Shared/Partials/_Sidebar.cshtml` / `_RightSidebar.cshtml`** — sidebar markup not read (recommended groups / trending hashtags inferred from `BaseController` and `HomeController`).
97. **`Sohba/Views/Shared/Partials/_PostCard.cshtml`** — post card markup not read.
98. **`Sohba.Application/DTOs/PostAggregate/Requests/*`** — request DTOs not individually read (used by `PostsController` JSON actions).
99. **`Sohba.Application/Interfaces/IJwtService.cs`** — interface exists; `JwtService` registered twice in DI (scoped + as `IJwtService`).
100. **`Sohba.Application/Services/JwtService.cs`** — was modified during the session (auto-save); final state reflects `JwtSettings`-based configuration.

---
*End of report.*
