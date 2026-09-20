# Sohba

Sohba is a social networking web application built with **ASP.NET Core MVC** on a layered
**Domain / Application / Infrastructure** architecture. It implements the feature set of a
small social platform: accounts and profiles, posts with media, comments and reactions,
stories, friendships with blocking and privacy controls, groups, pages, notifications with
real-time delivery, search, hashtags, reporting and an administrative dashboard.

The project targets **portfolio / small-scale deployment**. It is a complete, self-contained
application rather than an enterprise platform - see [Known limitations](#known-limitations)
for an honest account of what has and has not been verified.

![Entity relationship diagram](Social%20Media%20App%20ERD.png)

---

## Features

| Area | Capabilities |
|---|---|
| Accounts | Registration, login, logout, refresh tokens, password reset, password/email change, account deactivation |
| Profiles | Profile + cover image, bio, details, public/private account, friend-list visibility |
| Posts | Create (text with optional image or video), edit, delete, visibility (public / friends / only me), feed, saved posts and collections |
| Engagement | Comments, replies, reactions, hashtags |
| Stories | Image/video stories, 24-hour expiry, viewers, replies, reactions, protected (non-public) media storage |
| Social graph | Friend requests, accept/decline, unfriend, suggestions, blocking |
| Groups | Create, join requests, roles (owner/admin/moderator/member), group posts, leave/kick |
| Pages | Create, follow requests, page roles, page posts |
| Notifications | Persisted notifications, unread badge, real-time push over SignalR |
| Search | Users, posts, groups, pages, hashtags |
| Moderation | Report posts, review dashboard, resolve/dismiss reports, hide/delete content, block/delete users, admin role toggles, audit log |
| Settings | Account, privacy and notification preferences |
| Platform | Structured logging with correlation IDs, rate limiting, security headers, health check |

## Architecture

The solution enforces a one-way dependency graph. Business rules live in the Domain and are
never duplicated in the web layer.

```text
        Sohba (MVC: controllers, views, view models, SignalR hub, middleware)
                 |                      |
                 v                      v
        Sohba.Application  <----  Sohba.Infrastructure
                 |                      |
                 v                      v
             Sohba.Domain  <------------+
```

| Project | Responsibility | References |
|---|---|---|
| `Sohba.Domain` | Entities, enums, repository interfaces, `Result`, and ten pure domain-rule services (`Domain Rules/Logic`) that decide ownership, privacy and role hierarchies before anything is persisted | none |
| `Sohba.Application` | Use-case services, DTOs, AutoMapper profiles, FluentValidation validators, settings (JWT, mail, owner bootstrap), `IFileStorageService` contract | Domain |
| `Sohba.Infrastructure` | EF Core `AppDbContext` + entity configurations, 25 migrations, repositories, `UnitOfWork`, `DBInitializer`, local file storage, email service, background cleanup services | Application, Domain |
| `Sohba` | MVC presentation layer: 14 controllers, 74 Razor views, view models, `NotificationHub`, middleware, filters, rate-limiting policies | Application, Infrastructure |
| `Sohba.Domain.Tests` | xUnit tests for domain rules and entities | Domain |
| `Sohba.Application.Tests` | xUnit tests for application services (including refresh-token rotation and reuse detection) | Application |

Controllers stay thin: they bind and validate input, call an Application service, and translate
the returned `Result` into a view, redirect or JSON response. Persistence is reached through
repositories and `IUnitOfWork`; authorization is delegated to the Domain rule services.

## Technology stack

| Component | Version / choice |
|---|---|
| Runtime | .NET 10 (`net10.0`), ASP.NET Core MVC |
| Language | C# with nullable reference types enabled |
| Persistence | EF Core 10.0.5 + SQL Server |
| Identity | ASP.NET Core Identity (`IdentityDbContext<User, IdentityRole<Guid>, Guid>`) |
| Authentication | Cookie-first for the browser, JWT (HS256) + rotating refresh tokens |
| Real-time | SignalR (server) / SignalR 8.0.7 client from cdnjs |
| Mapping | AutoMapper |
| Validation | FluentValidation |
| Image processing | SixLabors.ImageSharp 3.1.12 (re-encodes uploads to WebP) |
| Logging | Serilog (console + rolling file, optional Elasticsearch sink) |
| Tests | xUnit |
| Front end | Razor views, vanilla JavaScript modules, custom CSS |

## Authentication

Sohba uses **ASP.NET Core Identity** with a **cookie-first** browser architecture plus a
**JWT + refresh-token** surface for the access-token/SignalR path.

- **Registration / login**: email + password (Identity password hashing: PBKDF2). Password policy:
  >= 6 chars, digit, upper + lower case. Lockout: 5 failed attempts -> 5 minutes.
- **Cookie authentication** (`.SohbaAuth`): authenticates every MVC request. HttpOnly, Secure,
  SameSite=Lax, 10-minute sliding expiration (persistent when "Remember me" is checked).
- **JWT** (HS256, configurable): issued at login/register and on page render for the SignalR
  connection (`<meta name="jwt-token">`). Claims: `sub`, `email`, `name`, `jti`, roles.
  Lifetime: `Jwt:AccessTokenLifetimeMinutes` (default **60 minutes**).
- **Refresh tokens**: on login the server sets `Sohba.RefreshToken` - an HttpOnly, Secure,
  SameSite=Lax cookie scoped to `/Auth`. The raw token (64 random bytes, Base64) is NEVER stored
  server-side; only its SHA-256 hash is persisted (`RefreshTokens` table). Endpoints:
  - `POST /Auth/Refresh` - validates + **rotates** the token, re-checks account state
    (blocked/deleted/inactive accounts are refused), returns a fresh access token JSON and a new
    cookie. Rate limited (`TokenRefresh`: 10/min/IP).
  - `POST /Auth/Revoke` - `[Authorize]` + antiforgery; revokes the current refresh token.
- **Rotation & reuse detection**: one refresh token = one use. Replaying an already-revoked token
  revokes ALL active tokens for that user (session-family revocation) and logs a warning.
- **Logout** revokes the refresh token, clears the cookie and signs out of Identity.
- **Password reset**: emailed token link (`/Auth/ResetPassword`); tokens via Identity's default
  token providers. **Email confirmation** is currently not required (`RequireConfirmedEmail = false`).
- **Role authorization**: `Owner`, `Admin`, `User` roles (`[Authorize(Roles = "Admin")]` on the
  dashboard) plus strongly-typed `UserRole` state on the user entity.

## Authorization

Server-side enforcement is layered and cannot be bypassed from the client:

1. **Framework level** - `[Authorize]` on every feature controller; role checks for the dashboard.
2. **Domain Rules** - pure services decide ownership/privacy/roles BEFORE persistence:
   post update/delete ownership, post visibility (Public / Friends / Only me), comment/react
   eligibility (deleted, blocked), story viewing (private account, friend, 24h expiry, daily
   limits), friendship direction (requests can only be accepted by the receiver), group/page role
   hierarchies (promote/demote/kick/review), notification ownership (only the owner can mark read),
   media limits. Negative results return `Result.Failure` and are surfaced as JSON errors.
3. **Privacy & blocking** - private accounts hide profile content and friends lists from
   non-friends; peer blocks prevent interactions; deleted/blocked/inactive accounts are signed out
   by `BaseController` on every action.
4. **Repositories** - visibility/privacy filtering is additionally applied at query level.

## Security

| Control | Implementation |
|---|---|
| XSS prevention | Razor HTML-encoding server-side; client-side rendering escapes all user data via a shared `escapeModalHtml()` helper before any `innerHTML`/template interpolation |
| CSRF | Antiforgery tokens on every state-changing POST (`[ValidateAntiForgeryToken]`), delivered to JS via the `csrf-token` meta and sent by `SohbaApp.post` as the `X-CSRF-TOKEN` header (registered as the antiforgery header so JSON AJAX works) |
| Secure cookies | Auth + refresh cookies: HttpOnly, Secure, SameSite=Lax; refresh cookie scoped to `/Auth` |
| JWT validation | Issuer/audience/lifetime/signing-key validation, zero clock skew |
| Refresh-token hashing | Only SHA-256 hashes stored; raw tokens never logged, never in URLs or HTML |
| Refresh-token rotation + reuse detection | One use per token; replay revokes the whole family |
| Rate limiting | Per-policy fixed windows: Auth 15/min, API 60/min, StoryCreate 10/min, Feed 60/min, FriendRequest 30/min, Search 60/min, Dashboard 180/min, Default 100/min, TokenRefresh 10/min - all partitioned by user/IP with JSON-aware 429 responses |
| File validation | Images: ImageSharp format detection + 5 MB + 4096px + WebP re-encode. Videos: extension + 50 MB + ISO-BMFF `ftyp` magic-byte check (MP4 brand allow-list / MOV `qt  ` brand) |
| Upload size limits | ASP.NET request limits + per-file checks (5 MB images / 50 MB videos) |
| Path traversal protection | Sub-folder whitelist + `Path.GetFullPath` root containment on save AND delete |
| Protected story storage | Story media under `ProtectedUploads/` outside wwwroot (no direct static access) |
| ImageSharp re-encoding | All images re-encoded to WebP (quality 82) - strips malicious metadata/payloads |
| Security headers | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, CSP whitelisting only the origins the app actually uses |
| Secret management | Secrets via user-secrets (Development) / environment variables (Production); `Jwt:Key`, connection string, mail credentials and `Sohba:Owner:InitialPassword` are never committed |
| Account lockout | 5 failures / 5 minutes, `AllowedForNewUsers` |
| Error handling | Global exception handler + status-code pages with JSON-aware responses; correlation IDs on every log entry |
| Logging | Serilog (console + rolling file + optional Elasticsearch); request context (user, endpoint, status, duration); token/password material is never logged |

## Database

- **SQL Server** via EF Core 10 (`AppDbContext` : `IdentityDbContext<User, IdentityRole<Guid>, Guid>`).
- **Migrations**: 25 committed migrations; `dotnet ef migrations add <Name> --project
  Sohba.Infrastructure --startup-project Sohba` is the convention.
- **DB initialization**: `DBInitializer.InitializeAsync` runs `Database.MigrateAsync()` on every
  startup - **auto-migration is enabled**. Consequence: deploying the app applies pending
  migrations automatically; take a database backup before deploying, or remove the auto-migrate
  call and run `dotnet ef database update` in your release pipeline if you prefer manual control.
- **Seeding**: roles (`Owner/Admin/User`) in all environments; the platform **Owner** is
  bootstrapped from `Sohba:Owner:Email` + `Sohba:Owner:InitialPassword` (required outside
  Development - startup fails without it). Demo content (admin/test users, sample posts) is
  Development-only.

## Background Processing

Two `BackgroundService`s:
- **NotificationCleanupService** (every 24h): deletes notifications older than 30 days
  (`NotificationService.DeleteOldNotificationsAsync`); failures are logged, never fatal.
- **StoryCleanupService** (every 1h): soft-deletes expired stories and deletes their media files
  from `ProtectedUploads`; files that cannot be deleted are logged for later cleanup.

## Real-Time Features

- **SignalR** hub at `/notificationHub` (`[Authorize]`), client loaded from cdnjs
  (SignalR 8.0.7). JavaScript connects with `withAutomaticReconnect([0, 2s, 5s, 10s, 20s])` and an
  `accessTokenFactory` supplying the JWT (the identity cookie also authenticates the handshake).
- **Notification flow**: an Application service raises a `NotificationEvent` ->
  `NotificationEventHandler` persists + pushes via `IHubContext<NotificationHub>` ->
  `ReceiveNotification` updates the badge, shows a toast and bridges a DOM event to page
  components. Bundle rules (15 min window) and self-action suppression are Domain Rules.

## Media

- **Images**: max 5 MB, 4096 px, formats jpg/jpeg/png/gif/webp; validated by decoding the actual
  content (not the extension); re-encoded to WebP (quality 82); stored as `/uploads/<area>/<guid>.webp`.
- **Videos**: max 50 MB, `.mp4`/`.mov`, extension + size + `ftyp` magic-byte verification; stored
  under `/uploads/<area>/<guid>.<ext>`.
- **Stories**: media under `ProtectedUploads/stories/` (outside wwwroot, served through
  authorization-checked endpoints).
- **Lifecycle**: replaced profile images/backgrounds are deleted after successful persistence;
  deleted posts/stories delete their media; story cleanup removes expired media hourly.

## Configuration

| Section | Keys | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string | secret |
| `Jwt:Key` | >= 32 chars signing key | secret |
| `Jwt:Issuer` / `Jwt:Audience` | token issuer/audience | e.g. your production URL |
| `Jwt:AccessTokenLifetimeMinutes` | default 60 | access-token lifetime |
| `Jwt:RefreshTokenLifetimeDays` | default 14 | refresh-token lifetime |
| `Jwt:ExpireDays` | legacy (unused by code now) | kept for compatibility |
| `MailSettings` | Host/Port/UserName/Password | Mailtrap in development; password is secret |
| `Sohba:Owner:Email` / `Sohba:Owner:InitialPassword` | Owner bootstrap | required in Production |
| `Serilog` (+ `SOHBA_ES_URL/USER/PASSWORD`) | logging; Elasticsearch optional | ES vars optional |

## Environment Variables

.NET standard double-underscore naming:

```text
ConnectionStrings__DefaultConnection=...
Jwt__Key=YOUR_SECRET_HERE
Jwt__Issuer=https://your-domain
Jwt__Audience=https://your-domain
MailSettings__UserName=...
MailSettings__Password=YOUR_SECRET_HERE
Sohba__Owner__Email=owner@your-domain
Sohba__Owner__InitialPassword=YOUR_SECRET_HERE
SOHBA_ES_URL=...            # optional
SOHBA_ES_USER=...           # optional
SOHBA_ES_PASSWORD=...       # optional
## Prerequisites

| Requirement | Notes |
|---|---|
| .NET SDK 10.0 | The solution targets `net10.0` |
| SQL Server | LocalDB, a SQL Server instance or the container from `docker-compose.yml` |
| SMTP mailbox | Optional locally; without `MailSettings` only password-reset emails are affected |

Docker (optional): Docker Desktop or a Docker Engine with Compose v2 to run the full stack.

## Getting started (local, without Docker)

```bash
git clone https://github.com/SamirAhmed00/Sohba.git
cd Sohba
dotnet restore Sohba.slnx
```

Configure the local secrets with user-secrets (nothing sensitive is read from `appsettings.json`):

```bash
cd Sohba
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=Sohba;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>"
dotnet user-secrets set "Sohba:Owner:InitialPassword" "<owner bootstrap password>"
# optional - password-reset e-mail
dotnet user-secrets set "MailSettings:UserName" "<smtp username>"
dotnet user-secrets set "MailSettings:Password" "<smtp password>"
```

Run the application:

```bash
dotnet run --project Sohba
```

`DBInitializer.InitializeAsync` runs `Database.MigrateAsync()` during startup, so the database and
schema are created on the first run without a manual `dotnet ef database update` step. No database
is required in advance; only a reachable SQL Server instance.

## Front-end assets

`wwwroot/css/site.css` is generated from `wwwroot/css/input.css` by Tailwind CSS 4 and is committed,
so the application runs without a Node toolchain. If you change styles, rebuild it:

```bash
cd Sohba
npm install
npm run tailwind:build     # one-off
npm run tailwind:watch     # during development
```

Other stylesheets (`sohba.css`, `landing.css`, `legacy.css`, `v0-custom.css`) and the JavaScript
modules under `wwwroot/js` are hand-written and loaded directly by the layouts.

## Running the tests

```bash
dotnet build Sohba.slnx
dotnet test Sohba.slnx

# one project at a time
dotnet test Sohba.Domain.Tests/Sohba.Domain.Tests.csproj
dotnet test Sohba.Application.Tests/Sohba.Application.Tests.csproj
```

The suites are self-contained (xUnit; domain-rule and application-service tests with fakes) and do
not require a database.

## Docker

`docker-compose.yml` starts the **complete application**: the MVC app image built from the root
`Dockerfile` plus a SQL Server 2022 container. The app applies migrations on startup, so the
database is provisioned automatically.

```bash
cp .env.example .env      # then fill in the required values
docker compose up --build
# http://localhost:8080
```

Required `.env` values: `MSSQL_SA_PASSWORD`, `JWT_KEY` (>= 32 characters),
`SOHBA_OWNER_EMAIL`, `SOHBA_OWNER_INITIAL_PASSWORD`. Compose fails fast with a clear message when
one is missing. Set `ASPNETCORE_ENVIRONMENT=Development` to also seed the demo accounts and sample
content; the default `Production` seeds only roles and the Owner account.

Named volumes persist the database, `wwwroot/uploads`, `ProtectedUploads`, the Serilog file sink
and the Data Protection key ring (so auth cookies survive container restarts).

The app publishes plain HTTP on `8080` and calls `UseHttpsRedirection()`. With no HTTPS
endpoint configured, ASP.NET Core logs `Failed to determine the https port for redirect` once and
continues serving HTTP, so the container works as-is. Browser sign-in on `http://localhost` works
because browsers treat localhost as a secure context (a requirement of the `Secure` auth and
refresh cookies); for anything else, terminate TLS in front of the container. The alternative for a
real deployment is to expose an HTTPS endpoint and set `HttpsPort` so the redirect applies.

`docker-compose.dev.yml` is separate and only starts **Elasticsearch/Kibana log tooling** - it is
not part of the application stack. See [ELASTICSEARCH.md](ELASTICSEARCH.md).

## CI/CD

**CI:** `.github/workflows/ci.yml` runs on pushes and pull requests to `master`. It checks out the
repository, installs the .NET 10 SDK, then runs `dotnet restore`, `dotnet build --configuration
Release` and `dotnet test --configuration Release` against `Sohba.slnx` (which includes both test
projects). No database is needed because the suites are self-contained.

**CD:** none. The repository has no deployment or release pipeline; deployment is currently a
manual process.

## Known limitations

- **ImageSharp is pinned to 3.1.x.** Version 4.x fails `dotnet build -c Release` unless a Six
  Labors license key or `sixlabors.lic` file is supplied (`ValidateLicenseTask`). The 3.1.x line
  needs no build-time key and the imaging API used by `LocalFileStorageService` is identical. If
  you obtain a license, bumping to 4.x requires no code changes.
- **No Web API / mobile backend.** The application is server-rendered MVC. The Application layer is
  API-consumable, but no API project or API controllers exist yet.
- **No load or stress testing has been performed.** The project targets portfolio / small-scale
  deployment; no concurrency or throughput figures have been measured, and none are claimed.
- **`AllowedHosts` is `*`.** Host-header filtering is not restricted, which is acceptable for local
  review but should be tightened to real hostnames before a public deployment.
- **Email confirmation is not enforced** (`RequireConfirmedEmail = false`), and password-reset
  e-mails require valid SMTP credentials to be configured.
- **Elasticsearch log shipping is optional and unverified here.** Without `SOHBA_ES_URL` the sink is
  skipped and Serilog writes to console + rolling file only.
- **Refresh-token storage is untested against a live SQL Server** in this environment; the rotation
  and reuse-detection logic is covered by unit tests with fakes.
- **Anti-malware scanning is not performed** on uploads. Content is validated by decoding
  (ImageSharp) or magic bytes (video `ftyp`), which rejects malformed files but is not an AV scan.
- **Single-instance assumptions:** media are stored on the local filesystem and background cleanup
  services are not coordinated across instances, so the app is intended to run as one instance.