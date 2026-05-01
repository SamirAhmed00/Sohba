# Sohba Clean Architecture Map

## Layer Responsibilities

| Layer | Contains | Must NOT Contain |
|---|---|---|
| **Domain** | Entities, Enums, `Result<T>`, Domain Rule Interfaces + Logic, `IUnitOfWork` | Business orchestration, I/O, HTTP |
| **Application** | Services (`IXService` → `XService`), DTOs, `MappingProfile`, FluentValidators (e.g. Comment validation) | EF Core, file I/O, HTTP context |
| **Infrastructure** | `AppDbContext`, EF Configurations, Repository implementations, `UnitOfWork`, `FileStorageService` (w/ strict validation) | Domain rule decisions, service orchestration |
| **Presentation (MVC)** | Controllers, ViewModels, Razor Views, `wwwroot/js`, Extension Methods | Business logic, file I/O, direct DB access |

## Canonical Request Flow

```
Browser (JS/Form)
  → Controller  [validate model, call service, return DTO/View — NO logic]
    → Service   [orchestrate domain rules + repositories, return Result<T>]
      → Domain Rules [pure business decisions — no I/O]
      → Repository   [EF Core queries — AsNoTracking for reads]
        → Database
```

## Access Control Enforcement Flow (Security Rule)

```
Controller.PostCreate()
  → PostService.CreatePostAsync()
    → IPostDomainService.CanCreatePost()           [content validation]
    → IGroupRepository.IsMemberAsync()             [group membership gate]
    → IPageRepository.GetByIdAsync() + AdminId check [page admin gate]
    → Insert Entity
```

## AJAX Contract (Mandatory since Sprint 1)

All AJAX POST endpoints **must**:
1. Return `BaseResponseDto` or `BaseResponseDto<T>` (never anonymous types)
2. Wrap the action body in `try-catch` returning `BaseResponseDto.FailureResponse(ex.Message)`
3. Guard against null/empty model before calling any service

`SohbaApp.post()` in `sohba-core.js` normalises `Success→success` and `Error→error` before returning, so all JS callers use lowercase keys only.

## Known Architectural Violations (Tracked in PROJECT_STRUCTURE_AUDIT.md)

1. **File I/O in Application Layer** — `StoryService.CreateStoryAsync` and `GroupService.CreateGroupAsync` perform `Directory.CreateDirectory` / `FileStream` inline. Must be moved to `IFileStorageService` in Infrastructure.
2. **Inline JS in Search/Results.cshtml** — `switchTab()` and `refineSearch()` functions live inside a `<script>` tag. Must migrate to `wwwroot/js/features/search.js`.
3. **Anonymous objects in SearchController** — `QuickSearch` returns `new { success, results }`. Must use `BaseResponseDto<SearchResultDto>`.
4. **Story Timezone Leak** — `StoryService` stores and compares `DateTime.UtcNow` but the domain rule `CanViewStory(createdAt)` receives a local/UTC ambiguous value. All DateTime must be explicitly `DateTimeOffset.UtcNow` or tagged with `Kind = Utc`.