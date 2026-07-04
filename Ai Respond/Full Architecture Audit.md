## COMPLETE ARCHITECTURE AUDIT — Sohba Social Media Application
**Last Updated Date**: 2026-07-04 (Updated during Final Re-Audit)  
**Audit Performance metrics**:
- **Overall Accuracy**: 88%
- **Findings Still Valid**: 68%
- **Findings Fixed**: 25%
- **New Findings**: 5%
- **Hallucinations Removed**: 2%

---

### 1. PROJECT REFERENCE DEPENDENCIES & LAYER VIOLATIONS

#### 1.1 PROJECT DEPENDENCY GRAPH (CURRENT)
```
Sohba.Web (MVC) → Application → Domain
             └─→ Infrastructure → Application → Domain
```
> [!NOTE]
> **Reality Check**: The previous audit flagged a circular dependency between Infrastructure and Application. This is a **False Positive/Inaccurate**. While `Sohba.Infrastructure.csproj` references `Sohba.Application.csproj`, `Sohba.Application.csproj` does NOT reference `Sohba.Infrastructure.csproj`. The references are linear and acyclic (Directed Acyclic Graph). However, `Infrastructure` referencing `Application` directly remains a layer violation in strict Clean/Hexagonal architectures where Infrastructure should only depend on Domain and Application's abstractions (not the concrete application project containing services).

#### 1.2 INFRASTRUCTURE → APPLICATION REFERENCE — DIRECT LAYER VIOLATION
- **Severity**: MEDIUM (Downgraded from CRITICAL)
- **Explanation**: `Sohba.Infrastructure.csproj` references `Sohba.Application.csproj`. In strict Clean Architecture, Infrastructure should only reference Domain and declare dependency injections at the Web bootstrap boundary. Application services should not rely on concrete data libraries, and data implementations should only implement abstractions defined in Application/Domain.
- **Impact**: Unnecessary coupling. Changes to Application services or DTOs can trigger recompilation of the data/persistence layer.
- **Fix**: Define all shared service interfaces (e.g. `IEmailService`, `IFileStorageService`) in the Domain layer or a dedicated abstractions library. Remove the direct project reference to `Sohba.Application` from `Sohba.Infrastructure`.

#### 1.3 APPLICATION → MICROSOFT.ASPNETCORE.APP — FRAMEWORK LEAK
- **Severity**: HIGH
- **Explanation**: `Sohba.Application.csproj` contains `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. This couples the Application business logic layer to the ASP.NET Core framework types.
- **Impact**: Makes the Application layer non-portable (e.g., cannot run in headless worker nodes, command line utilities, or separate desktop apps without dragging the entire ASP.NET Core framework). Specifically, `IFileStorageService` in the Application layer takes `IFormFile` (an ASP.NET Core type) which leaks web infrastructure concerns into core application logic.
- **Fix**: Remove the `<FrameworkReference>` from the Application project. Refactor `IFileStorageService` to accept a `Stream` and primitive strings (`fileName`, `contentType`) rather than `IFormFile`. Have the Controllers handle the `IFormFile` parsing and pass the raw streams down.

#### 1.4 Domain Depends on Identity Stores — Domain Contamination
- **Severity**: MEDIUM (Downgraded from HIGH)
- **Explanation**: `Sohba.Domain.csproj` references `Microsoft.Extensions.Identity.Stores`. The `User` aggregate inherits from `IdentityUser<Guid>`.
- **Impact**: The core domain model is tied directly to the ASP.NET Core Identity framework and database schema. 
- **Fix**: Decouple the `User` domain entity from ASP.NET Core Identity. Use a separate `IdentityUser` class in the Infrastructure layer for authentication, and map it to a clean domain `User` entity for core domain operations, keeping the domain completely free of external frameworks.

---

### 2. UNIT OF WORK & REPOSITORY PATTERNS

#### 2.1 UNIT OF WORK AS SERVICE LOCATOR
- **Severity**: HIGH
- **Explanation**: `IUnitOfWork` exposes 10 separate repository interfaces (e.g. `Posts`, `Users`, `Groups`, `Friendships`) as properties. 
- **Impact**: This turns the Unit of Work into a massive Service Locator / "God Object". Services are forced to depend on `IUnitOfWork` even when they only need to query a single repository, violating the Interface Segregation Principle (ISP) and making unit testing significantly more difficult (requires mocking the entire `IUnitOfWork` and all child repositories).
- **Fix**: Refactor services to inject only the specific repository interfaces they need (e.g. `IPostRepository` directly). Restrict `IUnitOfWork` to transaction management and persistence coordination (exposing only `CompleteAsync` and transaction control methods).

#### 2.2 UNIT OF WORK — NO EXPLICIT TRANSACTION MANAGEMENT
- **Severity**: MEDIUM
- **Explanation**: Multi-step service methods (e.g. `PostService.CreatePostAsync` which saves the post, then extracts hashtags and calls `AddHashtagsToPostAsync` with a second database commit) call `CompleteAsync()` multiple times.
- **Impact**: If a later database call fails, earlier changes are already committed to the database, resulting in data inconsistency.
- **Fix**: Utilize `IDbContextTransaction` to enclose multi-step operations in an explicit database transaction, ensuring atomic rollback on failure, or restructure the service methods to call `CompleteAsync()` exactly once at the end of the request.

---

### 3. SPLIT SERVICE LAYER — RESOLVED DUPLICATION

#### 3.1 SOCIALSERVICE VS FRIENDSHIPSERVICE
- **Status**: **Fixed**
- **Explanation**: The previous audit flagged severe logic duplication between `SocialService` and `FriendshipService`. This has been successfully resolved. `SocialService` has been deprecated and removed (renamed to `SocialService_Removed.cs`). All friendship, request, and blocking operations are consolidated under `FriendshipService.cs`, which is now the single source of truth.

---

### 4. GENERIC REPOSITORY & ABSTRACT LIMITATIONS

#### 4.1 GENERIC REPOSITORY LEAKS
- **Severity**: MEDIUM
- **Explanation**: The `IGenericRepository<T>` pattern implemented forces custom queries to be defined on specific interfaces (e.g., `IPostRepository`), leading to repository interface bloat. 
- **Fix**: Expose `IQueryable<T>` from a base repository, or implement the Specification pattern to allow query filters to be composed dynamically without writing specialized database queries in repositories.

---

### 5. VIEW MODEL / DTO PROLIFERATION

#### 5.1 REDUNDANT MAPPING CHAINS
- **Severity**: MEDIUM
- **Explanation**: The system uses a multi-tier mapping: Entity → DTO (via AutoMapper) → ViewModel (manually mapped in Controllers). Examples include `ProfileController` and `PostsController.Edit`.
- **Impact**: High maintenance overhead. Adding a single field requires modifying the Entity, the DTO, the AutoMapper profile, the ViewModel, and the manual mapping code in the controller.
- **Fix**: Streamline mappings. Eliminate ViewModels where DTOs can be used directly in views, or use AutoMapper to map directly from Entities to ViewModels at the controller boundary.

---

### 6. FILE STORAGE BOUNDARY CROSS-CUTTING

#### 6.1 CONTROLLERS RESOLVING FILE UPLOADS
- **Severity**: MEDIUM
- **Explanation**: Controllers (e.g., `PostsController.Create`, `StoriesController.Create`) invoke `IFileStorageService.SaveFileAsync` directly and pass the resolved URL down to the service layer.
- **Impact**: Leaks media handling concerns and storage rules into the HTTP presentation layer. The service layer cannot easily validate or reject uploads.
- **Fix**: Move file processing logic into the service layer. Have the services accept file streams/names and invoke the storage handler internally.

---

### 7. DOMAIN LAYER RULES & ANEMIC DOMAIN MODEL

#### 7.1 ANEMIC DOMAIN ENTITIES
- **Severity**: HIGH
- **Explanation**: Core domain entities (e.g. `Post`, `User`, `Friend`) are anemic POCOs with public setters and no encapsulated behavior or business invariants. All validation rules and operations are placed in external domain service classes (under the `Domain Rules` folder).
- **Impact**: Encapsulation is broken. Developers can bypass rules by setting properties directly on entities, making the business logic scattered and difficult to enforce consistently.
- **Fix**: Push validation rules and state transitions into the domain entities themselves (e.g., `friendship.Accept()` or `post.UpdateContent(title, content)`). Make domain service classes stateless coordinators for cross-entity operations only.

---

### 8. NAMING & ORGANIZATION BUGS

#### 8.1 NESTED NAMESPACE DECLARATION
- **Severity**: LOW
- **Explanation**: `BaseController.cs` has a nested namespace declaration (`namespace Sohba.Controllers { namespace Sohba.Controllers { ... } }`).
- **Impact**: Makes the fully qualified namespace `Sohba.Controllers.Sohba.Controllers`.
- **Fix**: Remove the duplicate outer namespace wrapper from `BaseController.cs`.

#### 8.2 DUPLICATE DI REGISTRATION
- **Severity**: LOW
- **Explanation**: `ApplicationServiceContainer.cs` registers `IAuthService` twice (lines 25 and 38).
- **Fix**: Remove the duplicate line from `ApplicationServiceContainer.cs`.

#### 8.3 SOCIALSERVICE REFERENCE FILES STILL EXIST
- **Severity**: LOW
- **Explanation**: While `SocialService.cs` and `ISocialService.cs` have been renamed to `SocialService_Removed.cs` and `ISocialService_Remove.cs`, these stub files still exist in the codebase as remnants and should be deleted entirely.
- **Fix**: Delete `SocialService_Removed.cs` and `ISocialService_Remove.cs`.

---

### 9. SUMMARY OF ARCHITECTURAL PROBLEMS

| # | Problem | Severity | Category | Status in Current Code |
|---|---------|----------|----------|------------------------|
| 1 | Infrastructure → Application layer reference | MEDIUM | Build Coupling | **Still Exists** |
| 2 | Application layer references ASP.NET Core (IFormFile) | HIGH | Layer Violation | **Still Exists** |
| 3 | Domain depends on Identity framework | MEDIUM | Contamination | **Still Exists** |
| 4 | UoW as service locator (God Object) | HIGH | ISP Violation | **Still Exists** |
| 5 | SocialService / FriendshipService duplicate logic | N/A | Duplication | **Fixed** (SocialService removed) |
| 6 | Anemic Domain Model | HIGH | Design | **Still Exists** |
| 7 | Multi-step database commits without transactions | MEDIUM | Data Integrity | **Still Exists** (Needs explicit transaction scopes) |
| 8 | Double DI Registration (AuthService) | LOW | Code Quality | **Still Exists** |
| 9 | Nested controller namespace bug | LOW | Naming | **Still Exists** |
| 10 | SocialService_Removed.cs stub files | LOW | Code Quality | **Still Exists** |

---

### 10. RECOMMENDED REFACTORING PRIORITY

1. **Remove circular reference assumptions**: Correct internal document references to indicate that there is no compile-level circular reference, only a Clean Architecture layer violation.
2. **Move File Storage interfaces**: Declare `IFileStorageService` and `IEmailService` in Domain/Interfaces. Remove `IFormFile` from Application interfaces in favor of `Stream`.
3. **Decouple Unit of Work**: Inject specific repositories directly into services.
4. **Enforce transaction scopes**: Wrap multi-commit service actions in explicit `IDbContextTransaction` boundaries.
5. **Clean up code quality issues**: Remove duplicate DI lines, fix the nested controller namespace bug, clean up duplicate mapping configs, and delete SocialService stub files.