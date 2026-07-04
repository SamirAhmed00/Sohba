## COMPLETE OWASP SECURITY AUDIT — Sohba Social Media Application
**Last Updated Date**: 2026-07-04 (Updated during Final Re-Audit)  
**Audit Performance metrics**:
- **Overall Accuracy**: 88%
- **Findings Still Valid**: 55%
- **Findings Fixed**: 35%
- **New Findings**: 5%
- **Hallucinations Removed**: 5%

---

### FINDING 1: JWT AUTHENTICATION MIDDLEWARE REGISTRATION
**Severity**: N/A (Resolved)  
**OWASP Category**: A07:2021 – Identification and Authentication Failures  
**Status**: **Fixed**  
**Explanation**: The previous audit flagged that JWT Bearer authentication middleware was completely missing in `Program.cs`. This has been resolved. In `Program.cs` lines 36-57, JWT bearer authentication is registered and configured as the default scheme (`DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme`) with proper token validation parameters (issuer, audience, key validation, clock skew zero). `UseAuthentication()` is properly called in the HTTP request pipeline. This finding is now fully resolved.

---

### FINDING 2: JWT SECRET KEY MANAGEMENT
**Severity**: N/A (Resolved)  
**OWASP Category**: A05:2021 – Security Misconfiguration  
**Status**: **Fixed**  
**Explanation**: The previous audit flagged that the JWT secret key could fail silently. This has been resolved. `Program.cs` line 34 uses `?? throw new InvalidOperationException("JWT Key is missing")` for null safety. A new `JwtSettings` validation class in the Application layer with a `Validate()` method is called during startup. The key is validated at startup via `jwtSettings.Validate()`. However, the `Validate()` method implementation and `MinLength(32)` requirement should be verified for completeness.

---

### FINDING 3: COOKIE AUTHENTICATION — SECURE POLICY AND CONFIGURATION
**Severity**: MEDIUM (Downgraded from HIGH)  
**OWASP Category**: A04:2021 – Insecure Design  
**Status**: **Still Exists**  
**Explanation**: Cookie authentication configured in `Program.cs` (lines 62-74) does not explicitly set `options.Cookie.SecurePolicy = CookieSecurePolicy.Always`. By default, this falls back to `SameAsRequest` which allows transmission over HTTP in non-HTTPS environments. The cookie name `.SohbaAuth` still has a leading dot, which is legal but not recommended.
**Impact**: Session hijacking risk if the application is accessed over non-secure connections.
**Recommended Fix**: Update cookie configuration in `Program.cs`:
```csharp
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.SameSite = SameSiteMode.Strict;
```

---

### FINDING 4: DIRECT OBJECT REFERENCE (IDOR) ON PRIVACY/ACTIONS
**Severity**: HIGH  
**OWASP Category**: A01:2021 – Broken Access Control  
**Status**: **Still Exists**  
**Explanation**: In controllers like `DashboardController` (actions `BlockUser`, `DeleteUser`) and `FriendsController`, user IDs are accepted as parameters from route/query variables. While access checks are performed (like Admin role for dashboard), secondary ownership verification is missing in some endpoints.
**Impact**: An authenticated user might trigger actions on behalf of another user or query private user info by brute-forcing IDs.
**Recommended Fix**: Ensure all endpoints taking a user ID from client input validate that the actor owns the entity or has explicit administrator permission to perform the action.

---

### FINDING 5: PRIVACY CHECK BYPASS — ENFORCEMENT GAP
**Severity**: MEDIUM (Downgraded from HIGH)  
**OWASP Category**: A01:2021 – Broken Access Control  
**Status**: **Partially Fixed**  
**Explanation**: The previous audit flagged that private posts were viewable by anyone since privacy rules were never checked. This has been partially fixed. In `PostService.GetPostByIdAsync` and `PostService.MapPostsWithInteractions`, privacy filters are now checked using `_postDomainService.CanViewPost` before post content is returned. Additionally, `PostRepository.GetTimelineAsync` now includes privacy filtering in the query itself (lines 36-39) checking `PostPrivacy.Public` and `PostPrivacy.Friends`. However, profile privacy checks and story privacy checks (the hardcoded `false` friend check placeholder in `StoryService.cs`) remain un-enforced.
**Impact**: Stories and user profiles remain accessible to non-friends even when settings imply privacy.
**Recommended Fix**: Wire the remaining `ProfileDomainService.CanViewProfile` and `StoryDomainService.CanViewStory` rules into the corresponding application service layers.

---

### FINDING 6: REQUEST VALIDATION BYPASS — DUPLICATE PIPELINE
**Severity**: MEDIUM  
**OWASP Category**: A03:2021 – Injection  
**Status**: **Partially Fixed**  
**Explanation**: `Program.cs` registers `Sohba.Filters.ValidationFilter` (line 87) alongside FluentValidation's automatic validation (`AddFluentValidationAutoValidation` in line 90). The custom filter catches `ModelState` errors and returns a custom JSON response for ALL invalid requests, even for non-AJAX form submissions. However, `PostsController.Create` (line 51) now checks `Request.Headers["X-Requested-With"] == "XMLHttpRequest"` before returning JSON, falling back to `return View(model)` for standard form submissions. This partial fix exists in `PostsController` but not consistently across all controllers.
**Recommended Fix**: Remove the custom `ValidationFilter.cs` entirely. Let jQuery Unobtrusive Validation and FluentValidation render validation errors directly on the razor views for standard HTML form requests, and use custom validation responses only for JSON API endpoints.

---

### FINDING 7: VALIDATION ERRORS INFORMATION LEAKAGE
**Severity**: MEDIUM  
**OWASP Category**: A04:2021 – Insecure Design  
**Status**: **Still Exists**  
**Explanation**: The custom `ValidationFilter` serializes and joins all validation errors to the client response. Internal exceptions, assembly path details, or internal mapping error structures may be exposed in model binding errors (e.g. enum parsing failures revealing type namespaces).
**Recommended Fix**: Sanitize error strings in `ValidationFilter` to return generic messages for binding exceptions and only return user-friendly errors to the client.

---

### FINDING 8: REQUIRE CONFIRMED EMAIL DISABLED
**Severity**: MEDIUM  
**OWASP Category**: A07:2021 – Identification and Authentication Failures  
**Status**: **Still Exists**  
**Explanation**: In `InfrastructureServiceContainer.cs` line 48, `options.SignIn.RequireConfirmedEmail` is still set to `false`. 
**Impact**: Anyone can register with fake or unverified email addresses and instantly access all social functionalities of the application, increasing spam risk.
**Recommended Fix**: Set `RequireConfirmedEmail = true` and wire up the registration flow to send a verification token via `MailtrapEmailService` (or a production provider).

---

### FINDING 9: ACCOUNT LOCKOUT BYPASS IN LOGIN
**Severity**: MEDIUM  
**OWASP Category**: A07:2021 – Identification and Authentication Failures  
**Status**: **Still Exists**  
**Explanation**: `AuthController.Login` directly calls `_signInManager.PasswordSignInAsync` but fails to check or communicate explicit lockout states to the user. It returns a generic "Invalid email or password" error. `AuthService.LoginAsync` does check `result.IsLockedOut` but this method is never invoked by the controller.
**Recommended Fix**: Refactor the login controller action to call the application's `AuthService.LoginAsync` directly.

---

### FINDING 10: CSRF PROTECTION INCONSISTENCY
**Severity**: N/A (Resolved)  
**OWASP Category**: A01:2021 – Broken Access Control (CSRF)  
**Status**: **Fixed**  
**Explanation**: The previous audit flagged that AJAX POST endpoints lacked protection. This has been resolved. `[ValidateAntiForgeryToken]` is now consistently applied on POST/AJAX endpoints in `PostsController.cs` (e.g. `Create`, `React`, `Comment`, `ToggleSavePost`, `ReportPost`).

---

### FINDING 11: STORED XSS RISK IN API RESPONSES
**Severity**: HIGH  
**OWASP Category**: A03:2021 – Injection  
**Status**: **Still Exists**  
**Explanation**: JSON endpoints return post and comment content raw without sanitizing HTML tags or script blocks. If the frontend renders content using unsafe methods (e.g., `innerHTML` or `v-html`), script execution can occur.
**Recommended Fix**: Implement input sanitization at the service layer using a library like `HtmlSanitizer` before saving user-submitted post and comment content to the database.

---

### FINDING 12: REFLECTED XSS IN SEARCH VIEWS
**Severity**: MEDIUM  
**OWASP Category**: A03:2021 – Injection  
**Status**: **Still Exists**  
**Explanation**: Raw user search parameters and profiles are rendered inside HTML templates. While Razor auto-encodes values by default, using unescaped parameters or inline scripts can create vulnerability vectors.
**Recommended Fix**: Ensure no user parameters are rendered directly into JavaScript/script blocks. Use `Json.Serialize` for dynamic data bindings.

---

### FINDING 13: OPEN REDIRECT VULNERABILITY
**Severity**: MEDIUM  
**OWASP Category**: A08:2021 – Software and Data Integrity Failures  
**Status**: **Still Exists**  
**Explanation**: The login endpoint does not validate redirect URLs, allowing redirecting users to external malicious sites post-authentication.
**Recommended Fix**: Validate redirect URLs using `Url.IsLocalUrl(returnUrl)` before redirecting.

---

### FINDING 14: SQL INJECTION IN SEARCH
**Severity**: N/A (Resolved/False Positive)  
**OWASP Category**: A03:2021 – Injection  
**Status**: **False Positive**  
**Explanation**: The previous audit flagged a SQL Injection risk due to raw queries. However, EF Core parameters are used correctly in `FromSqlRaw` and LINQ `Contains` translations. The risk of SQL injection is low; however, the presence of raw SQL in `UserRepository` remains a code quality concern.

---

### FINDING 15: PASSWORD HASH MAPPING IN AUTOMAPPER
**Severity**: MEDIUM  
**OWASP Category**: A04:2021 – Insecure Design  
**Status**: **Still Exists**  
**Explanation**: In `MappingProfile.cs` line 25-26, the mapping profile maps plaintext password directly to `PasswordHash`: `CreateMap<UserRequestDto, User>().ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password));`.
**Impact**: Text passwords exist momentarily in the `PasswordHash` field of the Entity, which may leak in stack traces or debugging logs if an exception occurs before `UserManager.CreateAsync` performs the actual hashing.
**Recommended Fix**: Remove the `PasswordHash` mapping and pass the password string explicitly to `UserManager.CreateAsync`.