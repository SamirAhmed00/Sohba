# Sohba Project Coding Standards (2026)

## 1. Architectural Integrity
- **No Infrastructure in MVC:** Strictly forbid file system operations (IO) or DB logic inside Controllers. Use `IFileStorageService`.
- **No I/O in Application Layer:** Services must NOT perform `Directory.CreateDirectory`, `FileStream`, or any OS-level I/O. Delegate to `IFileStorageService` (Infrastructure).
- **Dependency Injection:** Use the existing Extension Methods for DI. Do not pollute `Program.cs`.
- **Result Pattern:** Every Service method MUST return `Result<T>` or `Result`. Controllers must handle failure cases based on this result.

## 2. UI & JavaScript (The "Anti-Spaghetti" Rule)
- **Zero Inline JS:** No `<script>` tags inside `.cshtml` files.
- **Isolation:** Every View/Feature must have its own `.js` file in `wwwroot/js/features/`.
- **AJAX Responses:** Do NOT return anonymous objects `new { success = true }`. Use strongly-typed DTOs (`BaseResponseDto`, `BaseResponseDto<T>`) to ensure the JS knows exactly what it's receiving.
- **Section Scripts:** Use `@section Scripts { <script src="..."></script> }` to link files.
- **JS casing:** `SohbaApp.post()` normalises `Success→success` and `Error→error`. All JS callers must read `result.success` and `result.error` (lowercase only).

## 3. Validation Strategy
- **FluentValidation:** Use FluentValidation for all ViewModels/DTOs. 
- **Location:** Place Validators in `Sohba.Application` layer next to the DTOs.
- **Integration:** Ensure `AddFluentValidationAutoValidation()` is configured to show errors in MVC `ValidationSummary`.

## 4. Mapping & Data
- **AutoMapper:** Use `MappingProfile.cs`. Avoid manual mapping in Controllers.
- **DTOs vs ViewModels:** Controllers receive `ViewModels`, Services receive/return `DTOs`. Use AutoMapper to bridge them.
- **Include completeness:** Any repository query returning an entity used by AutoMapper must `.Include()` every navigation property referenced in `MappingProfile`. Missing includes cause `NullReferenceException` at mapping time — not at DB query time.

## 5. Performance
- **EF Core:** Always use `.AsNoTracking()` for read-only queries in Repositories.
- **Projections:** Prefer `.Select()` to fetch only needed columns for large lists.

## 6. Global Exception Safety (AJAX Endpoints)
- **Mandatory try-catch:** Every `[HttpPost]` action that returns JSON **must** wrap its body in `try-catch (Exception ex)` and return `Json(BaseResponseDto.FailureResponse(ex.Message))` on failure.
- **Null model guard:** Always check `if (model == null || model.Id == Guid.Empty)` before calling any service — `[FromBody]` binding silently produces `null` on malformed JSON.
- **No HTML on failure:** A 500 HTML Developer Exception Page reaching a JS `fetch()` caller will cause a `SyntaxError`. The try-catch is the contract that prevents this.

## 7. DateTime & Timezone Safety
- **Always use `DateTimeOffset.UtcNow`** (not `DateTime.UtcNow`) for any value stored in the DB or compared across service boundaries. `DateTimeOffset` carries explicit UTC offset metadata.
- **Never compare bare `DateTime` values** without verifying `Kind == DateTimeKind.Utc`. Use `.SpecifyKind(dt, DateTimeKind.Utc)` when receiving from external sources.
- **Story expiry:** Story `ExpiresAt` and `CreatedAt` must both be `DateTimeOffset.UtcNow` to guarantee 24h window accuracy regardless of server locale.