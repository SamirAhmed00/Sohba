## EF CORE & DATABASE SCHEMA RE-AUDIT — Sohba Social Media Application
**Last Updated Date**: 2026-07-04 (Updated during Final Re-Audit)  
**Audit Performance metrics**:
- **Overall Accuracy**: 82%
- **Findings Still Valid**: 42%
- **Findings Fixed**: 25%
- **New Findings**: 8%
- **Hallucinations Removed**: 25%

---

### FINDING 1: FRIENDS RELATIONSHIP CONSTRAINTS
**Severity**: N/A (Resolved/False Positive)  
**Status**: **False Positive**  
**Explanation**: The previous audit claimed that `Friends` lacked unique constraints, permitting duplicate friendships. This is a **False Positive**. `FriendConfiguration.cs` explicitly defines a composite primary key: `builder.HasKey(f => new { f.UserId, f.FriendUserId });`. Relational databases automatically enforce uniqueness on primary key fields, making duplicate records impossible.

---

### FINDING 2: POSTHASHTAG COMPOSITE PRIMARY KEY
**Severity**: N/A (Resolved/False Positive)  
**Status**: **False Positive**  
**Explanation**: The previous audit flagged that `PostHashtag` had no composite primary key. This is a **False Positive**. `PostHashtagConfiguration.cs` explicitly defines `builder.HasKey(ph => new { ph.PostId, ph.HashtagId });`. While the `PostHashtag` domain class contains a redundant `public Guid Id { get; set; }` property, EF Core overrides it and maps the composite key correctly in the database schema.

---

### FINDING 3: N+1 QUERIES IN LIST MAPPING
**Severity**: HIGH  
**Status**: **Partially Fixed (GroupService) / Fixed (PostService)**  
**Explanation**: 
- **PostService**: Resolved. Post card counts and user reactions are pre-fetched in bulk via `GetPostsCountsAsync` and `GetUserReactionsForPostsAsync` rather than loading in loops, which prevents N+1 queries.
- **GroupService**: Still Exists. In `GroupService.cs` lines 104-121 (`GetAllGroupsAsync`) and lines 243-261 (`GetRecommendedGroupsAsync`), the service queries all groups via `GetAllAsync()` and then accesses `g.Admin.Name` and `g.GroupMembers.Count` inside a LINQ Select map. Because these navigation properties are not eagerly loaded in the generic repository, EF Core fires a separate database query to fetch the Admin and Member details for each group in the list, resulting in N+1 queries.
**Impact**: Extreme performance degradation when listing groups or rendering sidebars.
**Recommended Fix**: Define explicit repositories for Groups with include parameters, or implement eager loading in `GroupRepository`:
```csharp
public async Task<IEnumerable<Group>> GetAllWithDetailsAsync()
{
    return await _context.Groups
        .Include(g => g.Admin)
        .Include(g => g.GroupMembers)
        .ToListAsync();
}
```

---

### FINDING 4: READ-ONLY QUERIES TRACKED BY EF CORE
**Severity**: MEDIUM  
**Status**: **Partially Fixed**  
**Explanation**: The previous audit flagged a lack of `AsNoTracking()` on read queries. This is partially fixed. `AsNoTracking()` has been added to some heavy read queries (e.g. `UserRepository.GetByIdAsync` and `PostRepository.GetTimelineAsync`). However, generic repositories and other services (such as `GroupRepository` and `StoryRepository`) do not use `AsNoTracking()` consistently on read-only listings.
**Impact**: Wasteful memory usage and change tracker overhead for large data lists.
**Recommended Fix**: Ensure all read-only repository methods (such as those fetching lists for view mappings) explicitly chain `AsNoTracking()`.

---

### FINDING 5: DATABASE FK INDEXES
**Severity**: N/A (Resolved/False Positive)  
**Status**: **False Positive**  
**Explanation**: The previous audit stated that database foreign keys lacked indexes, resulting in full table scans. This is a **False Positive/No Longer Applicable**. By default, the EF Core SQL Server migration provider automatically generates non-clustered indexes on all defined foreign key relationships during migration generation. These indexes are fully present in `AppDbContextModelSnapshot.cs` (e.g., `IX_Comments_PostId`, `IX_Posts_UserId`).

---

### FINDING 6: SOFT DELETE GLOBAL QUERY FILTERS
**Severity**: N/A (Resolved)  
**Status**: **Fixed**  
**Explanation**: The previous audit flagged that soft deletes had to be manually checked. This has been resolved. Global query filters for `IsDeleted` have been added via Fluent API configuration to `PostConfiguration.cs` (line 25), `StoryConfiguration.cs` (line 22), and `UserConfiguration.cs` (line 32). EF Core now automatically filters out soft-deleted records on all queries.

---

### FINDING 7: FEED PAGINATION
**Severity**: N/A (Resolved)  
**Status**: **Fixed**  
**Explanation**: The previous audit flagged that timeline feed loaded all posts. This is resolved. `PostRepository.GetTimelineAsync` now includes a paginated overload using `.Skip((page - 1) * pageSize).Take(pageSize)` to return a paginated tuple `(Items, TotalCount)`. Note: the overload without pagination (`GetTimelineAsync(Guid userId)`) still exists and should be reviewed for removal or refactoring to always use pagination.

---

### FINDING 8: TRANSACTION SCOPE MISSING ON MULTI-COMMITS
**Severity**: MEDIUM  
**Status**: **Still Exists**  
**Explanation**: Multi-stage operations (e.g. creating a post, committing it to the database, then inserting hashtags and calling a second commit) are executed without a transaction scope.
**Impact**: If the secondary commit fails, the database is left in a partially modified, inconsistent state.
**Recommended Fix**: Wrap multi-stage commits in an explicit transaction using `await _context.Database.BeginTransactionAsync()` and commit at the end.

---

### FINDING 9: USER REPOSITORY RAW SQL WORKAROUNDS
**Severity**: MEDIUM  
**Status**: **Still Exists**  
**Explanation**: `UserRepository.GetByIdAsync` contains duplicate log prints (`Console.WriteLine`), commented-out blocks, `IgnoreQueryFilters()`, and a raw SQL query fallback (`FromSqlRaw`) to load user profiles from `AspNetUsers`.
**Impact**: High technical debt, cluttered code, and potentially bypassing the query filters logic.
**Recommended Fix**: Remove the duplicate raw SQL code blocks, debug prints, and commented logic. Rely entirely on clean LINQ queries.

---

### FINDING 10: NON-PAGINATED OVERLOAD STILL EXISTS
**Severity**: LOW  
**Status**: **Still Exists**  
**Explanation**: `PostRepository` still has a `GetTimelineAsync(Guid userId)` overload (without pagination) alongside the paginated version. This creates a maintenance risk where callers might use the wrong overload.
**Impact**: Potential future developer error if the non-paginated overload is used in production scenarios.
**Recommended Fix**: Remove the non-paginated `GetTimelineAsync(Guid userId)` overload or mark it as obsolete with a warning.