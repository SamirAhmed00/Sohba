# Project Structure Technical Audit: Sohba Social Media App

This document provides a deep technical audit of the current state of the **Sohba** ASP.NET Core MVC application built using Clean Architecture.

---

## 1. Infrastructure & Data

### **AppDbContext & Configurations**
- **Implementation**: Inherits from `IdentityDbContext<User, IdentityRole<Guid>, Guid>`, overriding the primary key to `Guid`.
- **Custom Configurations**: Uses `modelBuilder.ApplyConfigurationsFromAssembly(...)` elegantly inside `OnModelCreating`, which correctly offloads table-specific constraints and relationship configurations from the context file into isolated `IEntityTypeConfiguration` classes.
- **Architectural Observations**: Clean and well-isolated.

### **Unit of Work**
- **Implementation**: The `UnitOfWork` implements `IUnitOfWork` by acting as a registry of specific repositories (e.g., `Users`, `Posts`, `Friendships`).
- **Architectural Smell**: The specific repositories are tightly coupled inside the `UnitOfWork` constructor (e.g., `Users = new UserRepository(_context);`). While typical in simple UoW patterns, this limits testability and bypasses dependency injection for the individual repositories themselves. 

### **Generic Repository Pattern**
- **Implementation**: `GenericRepository<T>` implements standard CRUD operations (`GetByIdAsync`, `GetAllAsync`, `Add`, `Update`, `Delete`). 
- **Inheritance**: Specific repositories (like `PostRepository`) inherit from `GenericRepository<Post>` and implement their own domain-specific interfaces (`IPostRepository`). This is an excellent approach, combining the DRYness of generic repositories with the flexibility of specific queries.

### **DbInitializer**
- **Implementation**: Found in `Sohba.Infrastructure.DBInitializer` and utilized in `Program.cs` by `app.InitializeDatabaseAsync()`. It is handling the initial migration and likely the initial seeding of Roles/Admin user.

---

## 2. Domain Layer

### **Entities & Enums**
- **Implementation**: Entities are properly grouped by aggregates (e.g., `UserAggregate`, `PostAggregate`, `GroupAndPageAggregate`). Enums are widely utilized (`ReactionType`, `PostSourceType`, `SavedTag`) to enforce type-safe constants.

### **Domain Rules**
- **Implementation**: Located within `Interfaces` and `Logic` subfolders under `Domain Rules`, representing a solid attempt to encapsulate core business invariants so they aren't scattered across application services.

### **Common Result Pattern**
- **Implementation**: Implemented via `Result` and `Result<T>` in `Sohba.Domain.Common`. It strictly manages success/failure states preventing invalid combinations via constructor validations (e.g., a success result cannot contain an error message). 

---

## 3. Application Layer

### **Services**
- **Implementation**: Services (`IPostService`, `IInteractionService`, etc.) execute the business logic, pulling from `IUnitOfWork`, orchestrating domains, and returning the standardized `Result<T>` pattern.

### **DTOs & AutoMapper**
- **Implementation**: Widespread use of Data Transfer Objects inside dedicated `DTOs` folders to decouple presentation logic from entities.
- **AutoMapper Profiles**: Configurations in `MappingProfile.cs` are dense and explicit. There is strong use of projective mapping (`.ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.Name))`), which protects sensitive nested object structures.

---

## 4. Presentation Layer (MVC)

### **Controllers & ViewModels**
- **Implementation**: Standard controllers exist mapping to UI specific `ViewModels` (like `PostCreateViewModel`). 
- **Authentication**: Usage of `[Authorize]` attributes is appropriately utilized alongside `BaseController` abstraction.

### **Architectural Smells & Dependency Leakage (Critical Issue Area)**
1. **Business Logic Leaking into Controllers**: In `PostsController.cs`, the `Create` method handles **file upload streams**, hardcoded size validation (`1024 * 1024`), extension validation, and raw `Directory.GetCurrentDirectory()` operations. This violates Clean Architecture principles because the MVC layer should not dictate how files are persistently stored or mapped locally. This logic belongs in a dedicated `IFileStorageService` in the Application/Infrastructure layer.
2. **Anonymous Object Responses**: Methods like `GetPostDetails` return rigid, arbitrarily structured anonymous types `return Json(new { success = true, post = new { ... } })` rather than fully utilizing mapped Application Layer DTOs or strongly-typed UI models.
3. **Program.cs Cookie Config**: Standard Identity configuring happens, but cookie configurations `ConfigureApplicationCookie` manually set path properties (`/Auth/Login`) inline in `Program.cs` which mixes authentication policy configuration with MVC route constants. Note: The cookie setting code block contains redundant declarations for `SlidingExpiration`.

### **Extension Methods**
- `ApplicationBuilderExtensions` and `DependencyInjection` correctly extract standard configurations (e.g., `AddApplicationServices()`, `AddInfrastructureService()`) to keep `Program.cs` readable. 

---

## 5. Logic & EF Core Utilizaton

### **LINQ & EF Usage**
- **Implementation**: Observed in `PostRepository.cs`, queries make aggressive and appropriate use of EF Core’s explicit loading strategy (`.Include(p => p.User)`).
- **Complexity**: Methods like `GetTimelineAsync` correctly resolve relational complexities (fetching friends, resolving Ids, preventing deleted/hidden objects) cleanly inside the data access layer.
- **Optimization Opportunities**: When counting relations (`GetPostsCountsAsync`), two separate `.GroupBy` queries are fired to fetch reactions and comments sequentially, which are parsed locally into dictionaries. This could potentially be optimized into projections when lists get larger, but functions appropriately under the current workload.

---

## Summary of Immediate Takeaways & "Where we are"

The project structure demonstrates a strong 80% completion of a standard enterprise-grade MVC application. The backbone (Data, Repository, Result pattern, Domain models) is sturdy and strictly adheres to its principles.

---

## Architectural Smells Register

### ✅ RESOLVED (Sprint 1)
| # | Smell | File | Fix Applied |
|---|---|---|---|
| S-01 | Anonymous JSON objects returned from AJAX endpoints | `FriendsController` | Replaced with `BaseResponseDto` |
| S-02 | Inline JS in `Find.cshtml` + `Requests.cshtml` | Views | Migrated to `friends.js` |
| S-03 | `sendFriendRequest` JS posting `{ userId }` instead of `{ receiverId }` | `friends.js` | Key corrected |
| S-04 | `FriendshipService.SendFriendRequestAsync` redundant user-existence fetch | `FriendshipService.cs` | Removed double-fetch |
| S-05 | `GetSentRequestsAsync` missing `.Include(f => f.User)` → NullRef in AutoMapper | `FriendshipRepository.cs` | Include added |
| S-06 | No try-catch on AJAX POST actions → HTML 500 on exceptions | `FriendsController` | try-catch + null guards added |
| S-07 | `sohba-core.js` calling `response.json()` on HTML pages → SyntaxError | `sohba-core.js` | Content-Type guard added |
| S-08 | `PostService` missing access-control for Group/Page posts | `PostService.cs` | `IsMemberAsync` + `AdminId` checks added |

### 🔴 OPEN — HIGH SEVERITY
| # | Smell | File | Root Cause | Impact |
|---|---|---|---|---|
| S-09 | **File I/O in Application Layer** | `StoryService.cs` lines 48-64 | `Directory.CreateDirectory` + `FileStream` inside Application service | Violates Clean Architecture; untestable; should be `IFileStorageService` in Infrastructure |
| S-10 | **File I/O in Application Layer** | `GroupService.cs` (Create/Edit controllers) | Same pattern as S-09 | Same impact |
| S-11 | **Story Timezone Ambiguity** | `StoryService.cs` lines 76, 106, 113 | `DateTime.UtcNow` used but `CanViewStory(createdAt)` receives an ambiguous value with no `DateTimeKind` guarantee | Stories may expire incorrectly based on server timezone config |
| S-12 | **Inline JS in `Search/Results.cshtml`** | Lines 343-374 | `switchTab()` + `refineSearch()` functions inside `<script>` block | Violates Zero Inline JS rule; should be `wwwroot/js/features/search.js` |
| S-13 | **Anonymous objects in `SearchController.QuickSearch`** | `SearchController.cs` lines 51-62 | `return Json(new { success, results })` | Inconsistent with `BaseResponseDto` standard |
| S-14 | **Missing `Search/Index.cshtml`** | `Views/Search/` | `SearchController.Index` calls `return View(new SearchViewModel)` for empty queries but only `Results.cshtml` exists | 404 when navigating to `/Search` without a query |
| S-15 | **`StoriesController.Create` returns raw anonymous DTO** | `StoriesController.cs` line 34 | `return Json(new { success = true, data = result.Value })` | Violates `BaseResponseDto` standard |

### 🟡 OPEN — MEDIUM SEVERITY
| # | Smell | File | Note |
|---|---|---|---|
| S-16 | `SearchController.QuickSearch` `IsMemberAsync` missing `.AsNoTracking()` | `SearchController.cs` | Not the primary issue but a performance smell |
| S-17 | Missing Post Edit/Delete in `PostService` | `PostService.cs` | Methods exist (`UpdatePostAsync`, `DeletePostAsync`) but no controller actions wired |
| S-18 | `UnitOfWork` tightly constructs repositories via `new` | `UnitOfWork.cs` | Reduces testability; prefer DI-registration |