# Core Module

```
npx repomix ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba.Domain/Entities/UserAggregate/User.cs" ^
"Sohba.Domain/Entities/UserAggregate/Friend.cs" ^
"Sohba.Domain/Entities/UserAggregate/Notification.cs" ^
"Sohba.Domain/Entities/PostAggregate/Post.cs" ^
"Sohba.Domain/Entities/PostAggregate/Comment.cs" ^
"Sohba.Domain/Entities/PostAggregate/Reaction.cs" ^
"Sohba.Domain/Entities/PostAggregate/Hashtag.cs" ^
"Sohba.Domain/Entities/PostAggregate/PostHashtag.cs" ^
"Sohba.Domain/Entities/PostAggregate/PostReport.cs" ^
"Sohba.Domain/Entities/PostAggregate/SavedPost.cs" ^
"Sohba.Domain/Entities/GroupAndPage/Group.cs" ^
"Sohba.Domain/Entities/GroupAndPage/GroupMember.cs" ^
"Sohba.Domain/Entities/GroupAndPage/Page.cs" ^
"Sohba.Domain/Entities/GroupAndPage/PageFollower.cs" ^
"Sohba.Domain/Entities/StoryAggregate/Story.cs" ^
"Sohba.Domain/Entities/StoryAggregate/StoryViewer.cs" ^
"Sohba.Domain/Enums/FriendshipStatus.cs" ^
"Sohba.Domain/Enums/GroupRole.cs" ^
"Sohba.Domain/Enums/NotificationType.cs" ^
"Sohba.Domain/Enums/PostPrivacy.cs" ^
"Sohba.Domain/Enums/PostSourceType.cs" ^
"Sohba.Domain/Enums/ReactionType.cs" ^
"Sohba.Domain/Enums/ReportReason.cs" ^
"Sohba.Domain/Enums/SavedTag.cs" ^
"Sohba.Domain/Enums/StoryPrivacy.cs" ^
"Sohba.Domain/Interfaces/IGenericRepository.cs" ^
"Sohba.Domain/Interfaces/IUnitOfWork.cs" ^
"Sohba.Domain/Interfaces/IUserRepository.cs" ^
"Sohba.Domain/Interfaces/IPostRepository.cs" ^
"Sohba.Domain/Interfaces/IFriendshipRepository.cs" ^
"Sohba.Domain/Interfaces/IGroupRepository.cs" ^
"Sohba.Domain/Interfaces/IPageRepository.cs" ^
"Sohba.Domain/Interfaces/IStoryRepository.cs" ^
"Sohba.Domain/Interfaces/IInteractionRepository.cs" ^
"Sohba.Domain/Interfaces/INotificationRepository.cs" ^
"Sohba.Domain/Interfaces/IReportingRepository.cs" ^
"Sohba.Domain/Interfaces/IHashtagRepository.cs" ^
"Sohba.Domain/Domain Rules/Interface/IMediaDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/MediaDomainService.cs" ^
"Sohba.Application/DTOs/Common/BaseResponseDto.cs" ^
"Sohba.Application/DTOs/Common/IdRequestDto.cs" ^
"Sohba.Application/DTOs/Common/PagedResult.cs" ^
"Sohba.Application/Interfaces/IFileStorageService.cs" ^
"Sohba.Application/Interfaces/IEmailService.cs" ^
"Sohba.Application/Interfaces/INotificationEventHandler.cs" ^
"Sohba.Application/Interfaces/INotificationHubService.cs" ^
"Sohba.Application/Events/NotificationEvent.cs" ^
"Sohba.Application/Settings/JwtSettings.cs" ^
"Sohba.Application/Mappings/MappingProfile.cs" ^
"Sohba.Application/DependencyInjection/ApplicationServiceContainer.cs" ^
"Sohba.Infrastructure/Data/AppDbContext.cs" ^
"Sohba.Infrastructure/Data/Configurations/UserConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/PostConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/CommentConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/ReactionConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/HashtagConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/PostHashtagConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/PostReportConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/SavedPostConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/FriendConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/NotificationConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/GroupConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/GroupMemberConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/PageConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/PageFollowerConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/StoryConfiguration.cs" ^
"Sohba.Infrastructure/Data/Configurations/StoryViewerConfiguration.cs" ^
"Sohba.Infrastructure/Repositories/GenericRepository.cs" ^
"Sohba.Infrastructure/Repositories/UnitOfWork.cs" ^
"Sohba.Infrastructure/LocalFileStorageService.cs" ^
"Sohba.Infrastructure/Services/MailSettings.cs" ^
"Sohba.Infrastructure/Services/MailtrapEmailService.cs" ^
"Sohba.Infrastructure/DependencyInjection/InfrastructureServiceContainer.cs" ^
"Sohba.Infrastructure/DBInitializer/IDBInitializer.cs" ^
"Sohba.Infrastructure/DBInitializer/DBInitializer.cs" ^
"Sohba.Domain/Sohba.Domain.csproj" ^
"Sohba.Application/Sohba.Application.csproj" ^
"Sohba.Infrastructure/Sohba.Infrastructure.csproj" ^
"Sohba/Sohba.csproj" ^
"Sohba/Program.cs" ^
"Sohba/appsettings.json" ^
"Sohba/Controllers/BaseController.cs" ^
"Sohba/Models/ErrorViewModel.cs" ^
"Sohba/Filters/ValidationFilter.cs" ^
"Sohba/Extensions/ApplicationBuilderExtensions.cs" ^
"Sohba/Views/_ViewImports.cshtml" ^
"Sohba/Views/_ViewStart.cshtml" ^
"Sohba/Views/Shared/_Layout.cshtml" ^
"Sohba/Views/Shared/_Layout.cshtml.css" ^
"Sohba/Views/Shared/_AppLayout.cshtml" ^
"Sohba/Views/Shared/_ValidationScriptsPartial.cshtml" ^
"Sohba/Views/Shared/Error.cshtml" ^
"Sohba/Views/Shared/Partials/_Header.cshtml" ^
"Sohba/Views/Shared/Partials/_Sidebar.cshtml" ^
"Sohba/Views/Shared/Partials/_RightSidebar.cshtml" ^
"Sohba/Views/Shared/Partials/_ConfirmModal.cshtml" ^
"Sohba/wwwroot/css/site.css" ^
"Sohba/wwwroot/css/legacy.css" ^
"Sohba/wwwroot/css/v0-custom.css" ^
"Sohba/wwwroot/css/input.css" ^
"Sohba/wwwroot/js/site.js" ^
"Sohba/wwwroot/js/sohba-core.js" ^
"Sohba/wwwroot/js/sohba-modal.js" ^
"Sohba/wwwroot/js/script.js" ^
"Sohba/wwwroot/js/features/sidebar.js" ^
"Sohba/wwwroot/js/features/modal.js" ^
"Sohba.slnx" ^
--output "AI/Core.xml"
```

========================================================

# Module 01 - Authentication

```
npx repomix ^
"Sohba/Controllers/AuthController.cs" ^
"Sohba.Application/Interfaces/IAuthService.cs" ^
"Sohba.Application/Services/AuthService.cs" ^
"Sohba.Application/Services/JwtService.cs" ^
"Sohba.Application/Settings/JwtSettings.cs" ^
"Sohba.Application/DTOs/UserAggregate/LoginDto.cs" ^
"Sohba.Application/DTOs/UserAggregate/RegisterDto.cs" ^
"Sohba.Application/DTOs/UserAggregate/AuthResponseDto.cs" ^
"Sohba.Application/DTOs/UserAggregate/ForgotPasswordDto.cs" ^
"Sohba.Application/DTOs/UserAggregate/ResetPasswordDto.cs" ^
"Sohba.Application/DTOs/Common/BaseResponseDto.cs" ^
"Sohba.Application/Interfaces/IEmailService.cs" ^
"Sohba.Domain/Entities/UserAggregate/User.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba/Views/Auth/Login.cshtml" ^
"Sohba/Views/Auth/Register.cshtml" ^
"Sohba/Views/Auth/Logout.cshtml" ^
--output "AI/Authentication.xml"
```

========================================================

# Module 02 - Posts

```
npx repomix ^
"Sohba/Controllers/PostsController.cs" ^
"Sohba.Application/Interfaces/IPostService.cs" ^
"Sohba.Application/Services/PostService.cs" ^
"Sohba.Application/Interfaces/IInteractionService.cs" ^
"Sohba.Application/Services/InteractionService.cs" ^
"Sohba.Application/Interfaces/IReportingService.cs" ^
"Sohba.Application/Services/ReportingService.cs" ^
"Sohba.Application/Interfaces/IHashtagService.cs" ^
"Sohba.Application/Services/HashtagService.cs" ^
"Sohba.Application/Interfaces/IFileStorageService.cs" ^
"Sohba.Application/DTOs/PostAggregate/PostCreateDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/PostUpdateDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/PostResponseDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/CommentRequestDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/CommentResponseDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/ReactionRequestDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/ReactionResponseDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/HashtagDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/PostReportRequestDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/PostReportResponseDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/SavedPostDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/Requests/ToggleSaveRequestDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/Requests/ChangeTagRequestDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/Requests/RemoveSavedRequestDto.cs" ^
"Sohba.Application/DTOs/Common/BaseResponseDto.cs" ^
"Sohba.Application/DTOs/Common/PagedResult.cs" ^
"Sohba.Application/Validators/CommentRequestDtoValidator.cs" ^
"Sohba/Validators/PostCreateViewModelValidator.cs" ^
"Sohba/ViewModels/Post/PostCreateViewModel.cs" ^
"Sohba/ViewModels/Post/PostEditViewModel.cs" ^
"Sohba.Domain/Entities/PostAggregate/Post.cs" ^
"Sohba.Domain/Entities/PostAggregate/Comment.cs" ^
"Sohba.Domain/Entities/PostAggregate/Reaction.cs" ^
"Sohba.Domain/Entities/PostAggregate/Hashtag.cs" ^
"Sohba.Domain/Entities/PostAggregate/PostHashtag.cs" ^
"Sohba.Domain/Entities/PostAggregate/PostReport.cs" ^
"Sohba.Domain/Entities/PostAggregate/SavedPost.cs" ^
"Sohba.Domain/Enums/PostPrivacy.cs" ^
"Sohba.Domain/Enums/PostSourceType.cs" ^
"Sohba.Domain/Enums/ReactionType.cs" ^
"Sohba.Domain/Enums/ReportReason.cs" ^
"Sohba.Domain/Enums/SavedTag.cs" ^
"Sohba.Domain/Interfaces/IPostRepository.cs" ^
"Sohba.Domain/Interfaces/IInteractionRepository.cs" ^
"Sohba.Domain/Interfaces/IReportingRepository.cs" ^
"Sohba.Domain/Interfaces/IHashtagRepository.cs" ^
"Sohba.Domain/Domain Rules/Interface/IPostDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/PostDomainService.cs" ^
"Sohba.Domain/Domain Rules/Interface/IInteractionDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/InteractionDomainService.cs" ^
"Sohba.Domain/Domain Rules/Interface/IReportingDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/ReportingDomainService.cs" ^
"Sohba.Domain/Domain Rules/Interface/IMediaDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/MediaDomainService.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba.Infrastructure/Repositories/PostRepository.cs" ^
"Sohba.Infrastructure/Repositories/InteractionRepository.cs" ^
"Sohba.Infrastructure/Repositories/ReportingRepository.cs" ^
"Sohba.Infrastructure/Repositories/HashtagRepository.cs" ^
"Sohba/Controllers/CommentsController.cs" ^
"Sohba/Views/Posts/Create.cshtml" ^
"Sohba/Views/Posts/Details.cshtml" ^
"Sohba/Views/Posts/Favorites.cshtml" ^
"Sohba/Views/Posts/Hashtag.cshtml" ^
"Sohba/Views/Posts/SavedPosts.cshtml" ^
"Sohba/Views/Shared/Partials/_PostCard.cshtml" ^
"Sohba/Views/Shared/Partials/_CreatePost.cshtml" ^
"Sohba/wwwroot/js/sohba-posts.js" ^
"Sohba/wwwroot/js/features/posts.js" ^
"Sohba/wwwroot/js/features/comments.js" ^
--output "AI/Posts.xml"
```

========================================================

# Module 03 - Friends

```
npx repomix ^
"Sohba/Controllers/FriendsController.cs" ^
"Sohba.Application/Interfaces/IFriendshipService.cs" ^
"Sohba.Application/Services/FriendshipService.cs" ^
"Sohba.Application/DTOs/UserAggregate/FriendDto.cs" ^
"Sohba.Application/DTOs/UserAggregate/UserResponseDto.cs" ^
"Sohba.Application/DTOs/Common/BaseResponseDto.cs" ^
"Sohba/ViewModels/Friend/FriendRequestsViewModel.cs" ^
"Sohba/ViewModels/Friend/FriendSuggestionViewModel.cs" ^
"Sohba.Domain/Entities/UserAggregate/Friend.cs" ^
"Sohba.Domain/Entities/UserAggregate/User.cs" ^
"Sohba.Domain/Enums/FriendshipStatus.cs" ^
"Sohba.Domain/Interfaces/IFriendshipRepository.cs" ^
"Sohba.Domain/Interfaces/IUserRepository.cs" ^
"Sohba.Domain/Domain Rules/Interface/IFriendshipDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/FriendshipDomainService.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba.Infrastructure/Repositories/FriendshipRepository.cs" ^
"Sohba.Infrastructure/Repositories/UserRepository.cs" ^
"Sohba/Views/Friends/Index.cshtml" ^
"Sohba/Views/Friends/Requests.cshtml" ^
"Sohba/Views/Friends/Suggestions.cshtml" ^
"Sohba/Views/Friends/Find.cshtml" ^
"Sohba/Views/Friends/Blocked.cshtml" ^
"Sohba/wwwroot/js/features/friends.js" ^
--output "AI/Friends.xml"
```

========================================================

# Module 04 - Groups

```
npx repomix ^
"Sohba/Controllers/GroupsController.cs" ^
"Sohba.Application/Interfaces/IGroupService.cs" ^
"Sohba.Application/Services/GroupService.cs" ^
"Sohba.Application/Interfaces/IPostService.cs" ^
"Sohba.Application/Interfaces/IFileStorageService.cs" ^
"Sohba.Application/DTOs/GroupAndPageAggregate/GroupCreateDto.cs" ^
"Sohba.Application/DTOs/GroupAndPageAggregate/GroupUpdateDto.cs" ^
"Sohba.Application/DTOs/GroupAndPageAggregate/GroupResponseDto.cs" ^
"Sohba.Application/DTOs/GroupAndPageAggregate/GroupMemberDto.cs" ^
"Sohba.Application/DTOs/Common/BaseResponseDto.cs" ^
"Sohba.Application/DTOs/Common/IdRequestDto.cs" ^
"Sohba/ViewModels/Group/GroupCreateViewModel.cs" ^
"Sohba/ViewModels/Group/GroupDetailsViewModel.cs" ^
"Sohba/ViewModels/Group/GroupEditViewModel.cs" ^
"Sohba.Domain/Entities/GroupAndPage/Group.cs" ^
"Sohba.Domain/Entities/GroupAndPage/GroupMember.cs" ^
"Sohba.Domain/Enums/GroupRole.cs" ^
"Sohba.Domain/Interfaces/IGroupRepository.cs" ^
"Sohba.Domain/Domain Rules/Interface/IGroupDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/GroupDomainService.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba.Infrastructure/Repositories/GroupRepository.cs" ^
"Sohba/Views/Groups/Index.cshtml" ^
"Sohba/Views/Groups/Create.cshtml" ^
"Sohba/Views/Groups/Details.cshtml" ^
"Sohba/Views/Groups/Edit.cshtml" ^
"Sohba/Views/Groups/_AboutTab.cshtml" ^
"Sohba/Views/Groups/_MembersTab.cshtml" ^
"Sohba/wwwroot/js/features/groups.js" ^
--output "AI/Groups.xml"
```

========================================================

# Module 05 - Pages

```
npx repomix ^
"Sohba/Controllers/PagesController.cs" ^
"Sohba.Application/Interfaces/IPageService.cs" ^
"Sohba.Application/Services/PageService.cs" ^
"Sohba.Application/Interfaces/IPostService.cs" ^
"Sohba.Application/Interfaces/IFriendshipService.cs" ^
"Sohba.Application/Interfaces/IFileStorageService.cs" ^
"Sohba.Application/DTOs/GroupAndPageAggregate/PageCreateDto.cs" ^
"Sohba.Application/DTOs/GroupAndPageAggregate/PageUpdateDto.cs" ^
"Sohba.Application/DTOs/GroupAndPageAggregate/PageResponseDto.cs" ^
"Sohba.Application/DTOs/GroupAndPageAggregate/PageFollowerDto.cs" ^
"Sohba/ViewModels/Page/PageCreateViewModel.cs" ^
"Sohba/ViewModels/Page/PageEditViewModel.cs" ^
"Sohba.Domain/Entities/GroupAndPage/Page.cs" ^
"Sohba.Domain/Entities/GroupAndPage/PageFollower.cs" ^
"Sohba.Domain/Interfaces/IPageRepository.cs" ^
"Sohba.Domain/Domain Rules/Interface/IPageDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/PageDomainService.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba.Infrastructure/Repositories/PageRepository.cs" ^
"Sohba/Views/Pages/Index.cshtml" ^
"Sohba/Views/Pages/Create.cshtml" ^
"Sohba/Views/Pages/Details.cshtml" ^
"Sohba/Views/Pages/Edit.cshtml" ^
--output "AI/Pages.xml"
```

========================================================

# Module 06 - Stories

```
npx repomix ^
"Sohba/Controllers/StoriesController.cs" ^
"Sohba.Application/Interfaces/IStoryService.cs" ^
"Sohba.Application/Services/StoryService.cs" ^
"Sohba.Application/Interfaces/IFileStorageService.cs" ^
"Sohba.Application/DTOs/StoryAggregate/StoryCreateDto.cs" ^
"Sohba.Application/DTOs/StoryAggregate/StoryResponseDto.cs" ^
"Sohba.Application/DTOs/Common/BaseResponseDto.cs" ^
"Sohba.Domain/Entities/StoryAggregate/Story.cs" ^
"Sohba.Domain/Entities/StoryAggregate/StoryViewer.cs" ^
"Sohba.Domain/Enums/StoryPrivacy.cs" ^
"Sohba.Domain/Interfaces/IStoryRepository.cs" ^
"Sohba.Domain/Domain Rules/Interface/IStoryDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/StoryDomainService.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba.Infrastructure/Repositories/StoryRepository.cs" ^
"Sohba/Views/Stories/Index.cshtml" ^
"Sohba/Views/Shared/Partials/_Stories.cshtml" ^
"Sohba/Views/Shared/Partials/_StoryRail.cshtml" ^
"Sohba/Views/Shared/Partials/_StoryViewer.cshtml" ^
"Sohba/Views/Shared/Partials/_CreateStoryModal.cshtml" ^
"Sohba/wwwroot/js/sohba-stories.js" ^
"Sohba/wwwroot/js/features/stories.js" ^
--output "AI/Stories.xml"
```

========================================================

# Module 07 - Notifications

```
npx repomix ^
"Sohba/Controllers/NotificationsController.cs" ^
"Sohba/Hubs/NotificationHub.cs" ^
"Sohba/Handlers/NotificationEventHandler.cs" ^
"Sohba.Application/Interfaces/INotificationService.cs" ^
"Sohba.Application/Services/NotificationService.cs" ^
"Sohba.Application/Interfaces/INotificationEventHandler.cs" ^
"Sohba.Application/Interfaces/INotificationHubService.cs" ^
"Sohba.Application/Events/NotificationEvent.cs" ^
"Sohba.Application/DTOs/UserAggregate/NotificationResponseDto.cs" ^
"Sohba.Application/DTOs/Common/BaseResponseDto.cs" ^
"Sohba.Domain/Entities/UserAggregate/Notification.cs" ^
"Sohba.Domain/Enums/NotificationType.cs" ^
"Sohba.Domain/Interfaces/INotificationRepository.cs" ^
"Sohba.Domain/Domain Rules/Interface/INotificationDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/NotificationDomainService.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba.Infrastructure/Repositories/NotificationRepository.cs" ^
"Sohba.Infrastructure/Services/NotificationCleanupService.cs" ^
"Sohba/Views/Notifications/Index.cshtml" ^
--output "AI/Notifications.xml"
```

========================================================

# Module 08 - Profile

```
npx repomix ^
"Sohba/Controllers/ProfileController.cs" ^
"Sohba.Application/Interfaces/IUserService.cs" ^
"Sohba.Application/Services/UserService.cs" ^
"Sohba.Application/Interfaces/IUserSettingsService.cs" ^
"Sohba.Application/Services/UserSettingsService.cs" ^
"Sohba.Application/Interfaces/IPostService.cs" ^
"Sohba.Application/Interfaces/IFriendshipService.cs" ^
"Sohba.Application/DTOs/UserAggregate/UserResponseDto.cs" ^
"Sohba.Application/DTOs/UserAggregate/UserRequestDto.cs" ^
"Sohba.Application/DTOs/UserAggregate/UserSettingsDto.cs" ^
"Sohba.Application/DTOs/UserAggregate/FriendDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/PostResponseDto.cs" ^
"Sohba/ViewModels/Profile/ProfileViewModel.cs" ^
"Sohba/ViewModels/Profile/EditProfileViewModel.cs" ^
"Sohba/ViewModels/Profile/SettingsViewModel.cs" ^
"Sohba.Domain/Entities/UserAggregate/User.cs" ^
"Sohba.Domain/Interfaces/IUserRepository.cs" ^
"Sohba.Domain/Domain Rules/Interface/IProfileDomainService.cs" ^
"Sohba.Domain/Domain Rules/Logic/ProfileDomainService.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba.Infrastructure/Repositories/UserRepository.cs" ^
"Sohba/Views/Profile/Index.cshtml" ^
"Sohba/Views/Profile/PrivateProfile.cshtml" ^
"Sohba/Views/Profile/Settings.cshtml" ^
--output "AI/Profile.xml"
```

========================================================

# Module 09 - Search

```
npx repomix ^
"Sohba/Controllers/SearchController.cs" ^
"Sohba.Application/Interfaces/ISearchService.cs" ^
"Sohba.Application/Services/SearchService.cs" ^
"Sohba.Application/DTOs/SearchAggregate/SearchResultDto.cs" ^
"Sohba.Application/DTOs/SearchAggregate/UserSearchResultDto.cs" ^
"Sohba.Application/DTOs/SearchAggregate/PostSearchResultDto.cs" ^
"Sohba.Application/DTOs/SearchAggregate/GroupSearchResultDto.cs" ^
"Sohba.Application/DTOs/SearchAggregate/PageSearchResultDto.cs" ^
"Sohba.Application/DTOs/Common/BaseResponseDto.cs" ^
"Sohba/ViewModels/Search/SearchViewModel.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba/Views/Search/Results.cshtml" ^
"Sohba/Views/Search/Results.cshtml.cs" ^
"Sohba/wwwroot/js/features/search.js" ^
--output "AI/Search.xml"
```

========================================================

# Module 10 - Dashboard

```
npx repomix ^
"Sohba/Controllers/DashboardController.cs" ^
"Sohba.Application/Interfaces/IUserService.cs" ^
"Sohba.Application/Interfaces/IPostService.cs" ^
"Sohba.Application/Interfaces/IGroupService.cs" ^
"Sohba.Application/Interfaces/IPageService.cs" ^
"Sohba.Application/Interfaces/IReportingService.cs" ^
"Sohba.Application/Interfaces/IFriendshipService.cs" ^
"Sohba.Application/DTOs/UserAggregate/UserResponseDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/PostResponseDto.cs" ^
"Sohba.Application/DTOs/PostAggregate/PostReportResponseDto.cs" ^
"Sohba/ViewModels/Dashboard/DashboardViewModel.cs" ^
"Sohba/ViewModels/Dashboard/DashboardUsersViewModel.cs" ^
"Sohba/ViewModels/Dashboard/DashboardPostsViewModel.cs" ^
"Sohba/ViewModels/Dashboard/DashboardReportsViewModel.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba/Views/Dashboard/Index.cshtml" ^
"Sohba/Views/Dashboard/Users.cshtml" ^
"Sohba/Views/Dashboard/Posts.cshtml" ^
"Sohba/Views/Dashboard/Reports.cshtml" ^
"Sohba/Views/Dashboard/Partials/_UserDetails.cshtml" ^
"Sohba/Views/Dashboard/Partials/_PostDetails.cshtml" ^
"Sohba/Views/Dashboard/Partials/_ReportDetails.cshtml" ^
"Sohba/wwwroot/js/features/dashboard.js" ^
--output "AI/Dashboard.xml"
```

========================================================

# Module 11 - HomeFeed

```
npx repomix ^
"Sohba/Controllers/HomeController.cs" ^
"Sohba.Application/Interfaces/IPostService.cs" ^
"Sohba.Application/Interfaces/IStoryService.cs" ^
"Sohba.Application/Interfaces/IHashtagService.cs" ^
"Sohba.Application/DTOs/PostAggregate/PostResponseDto.cs" ^
"Sohba.Application/DTOs/StoryAggregate/StoryResponseDto.cs" ^
"Sohba.Application/DTOs/GroupAndPageAggregate/GroupResponseDto.cs" ^
"Sohba.Application/DTOs/Common/BaseResponseDto.cs" ^
"Sohba.Application/DTOs/Common/PagedResult.cs" ^
"Sohba/ViewModels/HomeViewModel.cs" ^
"Sohba/Models/ErrorViewModel.cs" ^
"Sohba.Domain/Common/Result.cs" ^
"Sohba/Views/Home/Index.cshtml" ^
"Sohba/Views/Shared/Partials/_PostCard.cshtml" ^
"Sohba/Views/Shared/Partials/_CreatePost.cshtml" ^
"Sohba/Views/Shared/Partials/_Stories.cshtml" ^
"Sohba/Views/Shared/Partials/_StoryRail.cshtml" ^
"Sohba/Views/Shared/Partials/_StoryViewer.cshtml" ^
"Sohba/Views/Shared/Partials/_CreateStoryModal.cshtml" ^
"Sohba/wwwroot/js/features/feed.js" ^
"Sohba/wwwroot/js/sohba-posts.js" ^
"Sohba/wwwroot/js/sohba-stories.js" ^
--output "AI/HomeFeed.xml"
```

========================================================

# Module 12 - Landing

```
npx repomix ^
"Sohba/Controllers/LandingController.cs" ^
"Sohba/Views/Landing/Index.cshtml" ^
"Sohba/wwwroot/css/landing.css" ^
--output "AI/Landing.xml"
```
