# Implementation Tasks: Location Management

## Task Overview

Tasks are ordered by dependency. Complete infrastructure and foundation tasks first before moving to feature tasks.

---

## 1. Project Scaffolding and Infrastructure

- [x] 1.1 Create solution structure: `src/AppHost`, `src/ServiceDefaults`, `src/Api`, `src/Api.Tests`, `src/client`, `tests/e2e`
- [x] 1.2 Configure .NET Aspire AppHost to orchestrate API, frontend dev server, and SQL Server container
- [x] 1.3 Apply `builder.AddServiceDefaults()` in API and configure OpenTelemetry (traces, metrics, logs via OTLP)
- [x] 1.4 Add health check endpoints `/health/live` and `/health/ready` to the API
- [x] 1.5 Configure `SecurityHeadersMiddleware` to set `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` on all responses
- [x] 1.6 Scaffold React + TypeScript + Vite frontend in `src/client` with React Router
- [x] 1.7 Configure Vitest and React Testing Library in `src/client`
- [x] 1.8 Configure Playwright in `tests/e2e` with `@axe-core/playwright`
- [x] 1.9 Configure xUnit and Moq in `src/Api.Tests`
- [x] 1.10 Configure FsCheck and FsCheck.Xunit in `src/Api.Tests` for property-based tests
- [x] 1.11 Configure Stryker.NET in `src/Api.Tests/stryker-config.json` with 80% threshold

---

## 2. Database and EF Core Setup

- [x] 2.1 Install EF Core with SQL Server provider and NetTopologySuite spatial support in `src/Api`
- [x] 2.2 Create `AppDbContext` with all entity DbSets and spatial configuration
- [x] 2.3 Create EF Core entity classes: `User`, `Location`, `PendingEdit`, `LocationCollection`, `CollectionMember`, `PendingMembershipRequest`, `NamedShape`, `Image`, `Notification`, `AuditEvent`, `PasswordResetToken`
- [x] 2.4 Configure entity relationships, indexes, and constraints in `AppDbContext.OnModelCreating`
  - [x] 2.4.1 Spatial index on `Locations.Coordinates` and `NamedShapes.Geometry`
  - [x] 2.4.2 Unique case-insensitive indexes on `Users.Username`, `Users.DisplayName`, `Users.Email`
  - [x] 2.4.3 Unique constraint on `PendingEdits(LocationId, SubmittedByUserId)`
  - [x] 2.4.4 Composite PK on `CollectionMembers(LocationId, CollectionId)`
- [x] 2.5 Configure `ContentSequence` JSON column serialisation via `HasConversion` on `Location` and `PendingEdit`
- [x] 2.6 Create and apply initial EF Core migration
- [x] 2.7 Write integration tests verifying migrations apply cleanly and spatial queries return correct results

---

## 3. Core Infrastructure Services

- [x] 3.1 Implement `ICoordinateReprojectionService` using ProjNET
  - [x] 3.1.1 `IsSridSupported(int srid)` — whitelist check against ProjNET SRID database
  - [x] 3.1.2 `ReprojectToWgs84(double lat, double lon, int sourceSrid)` — reproject and round to 6 decimal places
  - [x] 3.1.3 Unit tests: valid reprojection, unsupported SRID rejection, 6 decimal place rounding
  - [x] 3.1.4 Property-based test (Property 1): coordinate round-trip holds for all valid inputs
- [x] 3.2 Implement `IImageProcessingService` using SixLabors.ImageSharp
  - [x] 3.2.1 `ProcessAndStoreAsync` — validate MIME/size, generate ThumbnailVariant (200x200), generate ResponsiveVariantSet (400/700/1000px at 2:3 or 3:2), persist all variants atomically
  - [x] 3.2.2 `DeleteImageAndVariantsAsync` — remove all variant files and DB record
  - [x] 3.2.3 Unit tests: variant generation, MIME rejection, size rejection, failure atomicity (no partial persist)
- [x] 3.3 Implement `ICacheService` wrapping `IMemoryCache` (dev) / `IDistributedCache` (prod)
  - [x] 3.3.1 `GetAsync<T>`, `SetAsync<T>`, `InvalidateAsync`, `InvalidateByPrefixAsync`
  - [x] 3.3.2 Unit tests: get/set/invalidate behaviour
- [x] 3.4 Implement `IAuditService` — append-only `RecordAsync` writing to `AuditEvents` table
  - [x] 3.4.1 Unit tests: event recorded with correct fields; no update/delete methods exposed
  - [x] 3.4.2 Property-based test (Property 5): audit log append-only invariant
- [x] 3.5 Implement `IEmailService` using SMTP (config from environment variables only)
  - [x] 3.5.1 Fail-fast on missing SMTP config at startup (emit `Critical` log)
  - [x] 3.5.2 Unit tests: email sent with correct content; SMTP credentials never logged
- [x] 3.6 Implement `LocalFileStorageService` — store/retrieve/delete files at configurable `IMAGES_STORAGE_PATH`
- [x] 3.7 Implement `JwtTokenService` — issue and validate JWT bearer tokens (key from `JWT_SIGNING_KEY` env var)
- [x] 3.8 Implement `AuditRetentionService` (background `IHostedService`) — daily purge of AuditEvents older than 1 year
- [x] 3.9 Implement `NotificationCleanupService` (background `IHostedService`) — delete read notifications older than 30 days

---

## 4. Authentication

- [x] 4.1 Implement `IAuthService` / `AuthService`
  - [x] 4.1.1 `RegisterAsync` — validate uniqueness (username, displayName, email), hash password with BCrypt.Net-Next (cost 12), assign Standard role, assign Admin to first user
  - [x] 4.1.2 `LoginAsync` — verify credentials, issue JWT (24h expiry), record failed auth AuditEvent
  - [x] 4.1.3 `ForgotPasswordAsync` — generate single-use token (1h expiry), store SHA-256 hash, send reset email; return same response for unknown usernames (prevent enumeration)
  - [x] 4.1.4 `ResetPasswordAsync` — validate token, update password hash, invalidate token
  - [x] 4.1.5 `ChangePasswordAsync` — verify current password, update hash
  - [x] 4.1.6 Unit tests for all paths including edge cases
  - [x] 4.1.7 Property-based test (Property 4): password complexity rejection
- [x] 4.2 Implement `AuthController` with routes: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`
- [x] 4.3 Configure JWT bearer authentication middleware in `Program.cs`
- [x] 4.4 Configure ASP.NET Core rate limiting: login (10 failures/15 min per username, 20/min per IP), forgot-password (5/hour per username)
- [x] 4.5 Write security tests: unauthenticated requests return 401; rate limits return 429 with `Retry-After` header

---

## 5. Image Upload and Processing

- [x] 5.1 Implement `IImageRepository` / `ImageRepository` — CRUD for Image records
- [x] 5.2 Implement `ImagesController`
  - [x] 5.2.1 `POST /api/images` — upload, process, return imageId + all variant URLs
  - [x] 5.2.2 `GET /api/images/{id}` — serve full-resolution image
  - [x] 5.2.3 `GET /api/images/{id}/thumbnail` — serve ThumbnailVariant
  - [x] 5.2.4 `GET /api/images/{id}/variants/{width}` — serve responsive variant (400/700/1000)
- [x] 5.3 Apply `Cache-Control: public, max-age=31536000, immutable` to all image variant responses
- [x] 5.4 Apply rate limiting: 20 image uploads per user per hour
- [x] 5.5 Unit tests: MIME validation, size validation, altText validation, variant URL response
- [x] 5.6 Property-based test (Property 3): image alt text round-trip

---

## 6. Locations — Backend

- [x] 6.1 Implement `ILocationRepository` / `LocationRepository`
- [x] 6.2 Implement `ILocationService` / `LocationService`
  - [x] 6.2.1 `CreateAsync` — validate, reproject coordinates, round to 6 decimal places, persist, record AuditEvent
  - [x] 6.2.2 `GetByIdAsync` — return canonical version (or PendingEdit for submitter)
  - [x] 6.2.3 `ListAsync` — paginated, descending by createdAt, no ContentSequence
  - [x] 6.2.4 `UpdateAsync` (creator path) — validate, reproject, replace canonical, clean up orphaned images, record AuditEvent
  - [x] 6.2.5 `SubmitPendingEditAsync` (non-creator path) — validate, reproject, upsert PendingEdit, create notification, record AuditEvent
  - [x] 6.2.6 `ApprovePendingEditAsync` — promote PendingEdit to canonical, delete PendingEdit, create notification, record AuditEvent
  - [x] 6.2.7 `RejectPendingEditAsync` — delete PendingEdit + orphaned images, create notification, record AuditEvent
  - [x] 6.2.8 `DeleteAsync` — creator or admin only, cascade delete PendingEdits + CollectionMembers + orphaned images, record AuditEvent
  - [x] 6.2.9 Unit tests for all paths
- [x] 6.3 Implement `LocationsController` with all routes (GET list, POST, GET detail, PUT, DELETE, GET pending-edits, POST approve, POST reject)
- [x] 6.4 Configure response caching: location list (60s TTL), location detail (60s TTL), invalidate on mutation
- [x] 6.5 Write security tests: 401 unauthenticated, 403 non-creator edit, 403 non-creator approve/reject
- [x] 6.6 Property-based test (Property 2): ContentSequence serialisation round-trip

---

## 7. Images — Frontend Integration

- [x] 7.1 Implement `ImageUploader` React component with file picker, optional altText input, and upload to `POST /api/images`
- [x] 7.2 Implement avatar crop tool (1:1 aspect ratio constraint) in `ImageUploader`
- [x] 7.3 Implement `<picture>` / `srcset` rendering for content images using ResponsiveVariantSet URLs with correct `sizes` attributes
- [x] 7.4 Implement `ThumbnailImage` component using ThumbnailVariant URL for list/card views
- [x] 7.5 Unit tests: crop constraint, srcset attribute values, placeholder on missing image

---

## 8. Locations — Frontend

- [x] 8.1 Implement `AuthContext` — JWT storage in localStorage, user profile state, login/logout
- [x] 8.2 Implement `ProtectedRoute` and `AdminRoute` components
- [x] 8.3 Implement `LoginPage` with `LoginForm` and `RegisterForm` (tabs or toggle)
  - [x] 8.3.1 Inline password complexity validation before submit
  - [x] 8.3.2 Duplicate username/displayName/email error display
  - [x] 8.3.3 "Forgot password?" link
  - [x] 8.3.4 Redirect authenticated users to homepage
- [x] 8.4 Implement `ForgotPasswordPage` and `ResetPasswordPage`
- [x] 8.5 Implement `LeafletMap` component
  - [x] 8.5.1 Render Location pins at WGS84 coordinates
  - [x] 8.5.2 Display Location name in pin tooltip
  - [x] 8.5.3 Render BoundingShape overlay when present
  - [x] 8.5.4 Auto-fit viewport to all pins + shape
  - [x] 8.5.5 Keyboard-accessible text alternative (table/list of Locations with names and coordinates)
- [x] 8.6 Implement `ContentSequenceViewer` — render Heading (h1/h2/h3), Paragraph, Image blocks in order
- [x] 8.7 Implement `ContentSequenceEditor` — add/remove/reorder blocks, heading level selector, inline image upload
- [x] 8.8 Implement `LocationListPage` with pagination and navigation links
- [x] 8.9 Implement `LocationDetailPage`
  - [x] 8.9.1 Display name, coordinates, SRID metadata, creator, timestamp
  - [x] 8.9.2 Render ContentSequence via `ContentSequenceViewer`
  - [x] 8.9.3 Render Leaflet map with single pin
  - [x] 8.9.4 Show `PendingEditPanel` for creator (side-by-side comparison, Approve/Reject actions)
  - [x] 8.9.5 Show loading skeleton while data is in flight
- [x] 8.10 Unit tests for all components; Playwright E2E for create/view/edit happy paths
- [x] 8.11 Axe-core accessibility assertions on all Location pages

---

## 9. LocationCollections — Backend

- [x] 9.1 Implement `ICollectionRepository` / `CollectionRepository`
- [x] 9.2 Implement `ICollectionService` / `CollectionService`
  - [x] 9.2.1 `CreateAsync` — validate, persist, record AuditEvent
  - [x] 9.2.2 `GetByIdAsync` — enforce private visibility at service layer (not only controller)
  - [x] 9.2.3 `ListPublicAsync` — paginated public collections with thumbnail URL
  - [x] 9.2.4 `ListCombinedAsync` — public + owned with `isOwner` flag (never cached)
  - [x] 9.2.5 `UpdateAsync` — owner only, validate NamedShape reference, record AuditEvent
  - [x] 9.2.6 `DeleteAsync` — owner or admin, cascade CollectionMembers + orphaned image, record AuditEvent
  - [x] 9.2.7 `AddMemberAsync` — owner: direct; non-owner: pending request; create notification on approval/rejection
  - [x] 9.2.8 `RemoveMemberAsync` — owner only
  - [x] 9.2.9 `ApproveMembershipAsync` / `RejectMembershipAsync` — owner only, create notifications, record AuditEvents
  - [x] 9.2.10 Unit tests for all paths; service-layer private visibility enforcement test
- [x] 9.3 Implement `CollectionsController` with all routes
- [x] 9.4 Configure response caching: public list (60s), public detail (60s), invalidate on mutation; never cache combined list
- [x] 9.5 Write security tests: 403 non-owner mutations, 403 private collection access

---

## 10. NamedShapes — Backend

- [x] 10.1 Implement `INamedShapeRepository` / `NamedShapeRepository`
- [x] 10.2 Implement `INamedShapeService` / `NamedShapeService`
  - [x] 10.2.1 `UploadAsync` — admin only, validate GeoJSON (type, vertex count max 1000), store as GEOGRAPHY, record AuditEvent
  - [x] 10.2.2 `RenameAsync` — admin only, unique name check, record AuditEvent
  - [x] 10.2.3 `DeleteAsync` — admin only, reject if referenced by any collection, record AuditEvent
  - [x] 10.2.4 `ListAsync` — any authenticated user, paginated (id + name only)
  - [x] 10.2.5 Unit tests including geometry bomb protection (more than 1000 vertices returns 400)
- [x] 10.3 Implement `NamedShapesController` with all routes
- [x] 10.4 Write security tests: 403 Standard user mutations, 401 unauthenticated

---

## 11. LocationCollections — Frontend

- [x] 11.1 Implement `CollectionCard` component — thumbnail, name, description (truncated), owner badge vs public badge
- [x] 11.2 Implement `HomePage`
  - [x] 11.2.1 Paginated card list using `GET /api/collections/combined`
  - [x] 11.2.2 Filter by `ShowPublicCollections` preference (read from API, not client state)
  - [x] 11.2.3 Empty-state with "Create collection" prompt
  - [x] 11.2.4 Loading skeleton while data is in flight
  - [x] 11.2.5 Redirect unauthenticated users to `/login`
- [x] 11.3 Implement `CollectionDetailPage`
  - [x] 11.3.1 Leaflet map with all member Location pins and optional BoundingShape overlay
  - [x] 11.3.2 Auto-fit viewport to all pins + shape
  - [x] 11.3.3 Linked list of member Locations below map
  - [x] 11.3.4 Empty-state when no members
  - [x] 11.3.5 Ownership reassignment UI (admin only, accessible from detail page)
- [x] 11.4 Unit tests for all components; Playwright E2E for collection create/view/map happy paths
- [x] 11.5 Axe-core accessibility assertions on all Collection pages

---

## 12. User Profile and Configuration

- [x] 12.1 Implement `IUserService` / `UserService`
  - [x] 12.1.1 `GetProfileAsync` — return own profile (never expose email publicly)
  - [x] 12.1.2 `ChangeDisplayNameAsync` — unique check (case-insensitive), record AuditEvent
  - [x] 12.1.3 `ChangePasswordAsync` — verify current password, update hash, record AuditEvent
  - [x] 12.1.4 `UploadAvatarAsync` — 1 MB limit, 1:1 crop, generate ThumbnailVariant only, replace previous avatar, record AuditEvent
  - [x] 12.1.5 `UpdatePreferencesAsync` — persist `ShowPublicCollections`, record AuditEvent
  - [x] 12.1.6 Unit tests for all paths
- [x] 12.2 Implement `UsersController` with routes: `GET /api/users/me`, `PUT /api/users/me/display-name`, `PUT /api/users/me/password`, `PUT /api/users/me/avatar`, `PUT /api/users/me/preferences`
- [x] 12.3 Implement `UserMenu` React component — avatar (ThumbnailVariant), display name, links to settings and sign-out
- [-] 12.4 Implement `UserConfigurationPage`
  - [x] 12.4.1 Display name inline editor with uniqueness error
  - [x] 12.4.2 Avatar uploader with 1:1 crop tool and optional altText input
  - [x] 12.4.3 Change password form (current + new + confirm)
  - [x] 12.4.4 `ShowPublicCollections` toggle (persisted immediately to API)
  - [x] 12.4.5 Redirect unauthenticated users to `/login`
- [x] 12.5 Unit tests for all components; Playwright E2E for settings happy path
- [x] 12.6 Axe-core accessibility assertions on settings page

---

## 13. In-App Notifications

- [x] 13.1 Implement `INotificationService` / `NotificationService` — create notifications for all workflow events (PendingEdit submitted/approved/rejected, membership approved/rejected)
- [x] 13.2 Implement `INotificationRepository` / `NotificationRepository`
- [x] 13.3 Implement `NotificationsController` with routes: `GET /api/notifications`, `PUT /api/notifications/{id}/read`, `PUT /api/notifications/read-all`, `DELETE /api/notifications/{id}`
- [x] 13.4 Implement `NotificationPanel` React component
  - [x] 13.4.1 Unread count badge in `UserMenu` with `aria-live="polite"`
  - [x] 13.4.2 Panel listing unread notifications with description and navigation link
  - [x] 13.4.3 Mark as read / delete actions
  - [x] 13.4.4 Poll `GET /api/notifications` every 30 seconds when panel is open
- [x] 13.5 Unit tests for notification creation on each workflow event
- [x] 13.6 Axe-core accessibility assertions on notification panel

---

## 14. Admin Features

- [x] 14.1 Implement `IUserAdminService` — `ListUsersAsync`, `PromoteAsync`, `DemoteAsync` (last-admin guard), record AuditEvents
- [x] 14.2 Implement `AdminController` — `GET /api/admin/users`, `PUT /api/admin/users/{id}/role`
- [x] 14.3 Implement `AdminUsersPage` React component — user list with role display, Promote/Demote actions (admin only)
- [x] 14.4 Implement audit log API: `GET /api/admin/audit-log` with filter parameters (eventType, actingUserId, resourceType, resourceId, outcome, date range), paginated (default 50, max 200)
- [x] 14.5 Implement `AuditLogPage` React component — paginated table with filter controls (dropdowns, text search, date pickers)
- [x] 14.6 Implement ownership reassignment: `POST /api/admin/resources/{type}/{id}/reassign`, record AuditEvent
- [x] 14.7 Unit tests: last-admin guard, 403 non-admin access, audit log filter combinations
- [x] 14.8 Axe-core accessibility assertions on all admin pages

---

## 15. Data Export and Import

- [x] 15.1 Implement `IBackupService` / `BackupService`
  - [x] 15.1.1 `ExportAsync` — query all exportable data, serialise to JSON manifest, package with image files into ZIP, encrypt with AES-256 (caller-supplied key, min 32 chars), never log key
  - [x] 15.1.2 `ImportAsync` — decrypt, validate schema (HTTP 422 on failure), create ImportUser, import additively with new IDs on conflict, validate coordinates and images (skip invalid with warning), return ImportResult summary
  - [x] 15.1.3 Unit tests: export structure, import additive behaviour, ImportUser creation, invalid record skipping
- [x] 15.2 Implement export/import endpoints in `AdminController`: `POST /api/admin/export`, `POST /api/admin/import`
- [x] 15.3 Implement `ImportExportPage` React component — export form (encryption key input, download button), import form (file picker, decryption key input, result summary)
- [x] 15.4 Write security tests: 403 non-admin, 400 missing/short key, 422 invalid archive schema

---

## 16. Observability and Telemetry

- [~] 16.1 Register `LocationManagementTelemetry` `ActivitySource` and `Meter` in `Program.cs`
- [~] 16.2 Add custom spans for business operations (create/edit/delete Location, approve/reject PendingEdit)
- [~] 16.3 Add histograms for Location create/edit durations and image upload count
- [~] 16.4 Implement source-generated logging (`LoggerMessage.Define`) for all key log events
- [~] 16.5 Verify all log events never include passwords, JWT secrets, image binary data, or coordinate values
- [~] 16.6 Integration test: verify AuditEvents are recorded for all mutating operations

---

## 17. End-to-End Tests and Accessibility

- [~] 17.1 Playwright E2E: Register then Login then Create Location then View Location
- [~] 17.2 Playwright E2E: Non-creator submits edit then Creator approves then Canonical version updated
- [~] 17.3 Playwright E2E: Create Collection then Add Location then View Collection map
- [~] 17.4 Playwright E2E: Admin promotes user then Promoted user accesses admin features
- [~] 17.5 Playwright E2E: Admin exports backup then Admin imports backup then Data present
- [~] 17.6 Axe-core accessibility assertions on every page (zero critical/serious violations)
- [~] 17.7 Manual accessibility checklist: keyboard-only navigation, screen reader smoke test, 200% zoom, high-contrast mode

---

## 18. Security Scanning and Quality Gates

- [~] 18.1 Run `semgrep --config=p/owasp-top-ten src/` — zero findings required
- [~] 18.2 Run `dotnet list package --vulnerable --include-transitive` — no high/critical vulnerabilities
- [~] 18.3 Run `npm audit` in `src/client` — no high/critical vulnerabilities
- [~] 18.4 Run `dotnet stryker` — mutation score 80% or above
- [~] 18.5 Run `pre-commit run --all-files` — zero detected secrets
- [~] 18.6 Verify all Husky pre-commit hooks pass: `dotnet husky run --group pre-commit`
