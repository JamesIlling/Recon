# Design Document: Location Management

## Overview

The Location Management feature is a full-stack web application built on ASP.NET Core (.NET 10+) and React 18+. It enables authenticated users to create, view, edit, and organise geographic Locations. Each Location stores spatial coordinates (reprojected to WGS84), a source SRID, rich formatted content (headings, paragraphs, images), and ownership metadata.

The system introduces:
- A custom JWT-based authentication system (registration, login, password reset)
- An editorial approval workflow (creator edits apply immediately; non-creator edits are held as PendingEdits)
- LocationCollections for grouping Locations, with optional bounding shapes and collection images
- Admin capabilities: role management, NamedShape management, audit log, data export/import, ownership reassignment
- In-app notifications for workflow events
- Server-side image processing (thumbnails and responsive variants)
- Comprehensive observability via OpenTelemetry and .NET Aspire

### Key Design Goals

1. **Correctness** — coordinate reprojection and ContentSequence serialisation must be lossless and consistent across all write paths.
2. **Security** — all endpoints enforce authentication and authorisation; inputs are validated at the API boundary; audit events are append-only.
3. **Performance** — read endpoints respond within 500 ms p95; server-side caching with targeted invalidation; pre-generated image variants.
4. **Accessibility** — WCAG 2.1 AA compliance on all pages; keyboard-accessible map alternatives.
5. **Observability** — structured logs, distributed traces, and custom metrics on all operations.

---

## Architecture

### High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        React Client (Vite)                       │
│  ┌──────────┐ ┌──────────────┐ ┌──────────────┐ ┌───────────┐  │
│  │  Router  │ │  AuthContext │ │  LeafletMap  │ │  Notifs   │  │
│  └──────────┘ └──────────────┘ └──────────────┘ └───────────┘  │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTPS / JWT Bearer
┌────────────────────────▼────────────────────────────────────────┐
│                   ASP.NET Core Web API (src/Api)                 │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │ Controllers │→ │   Services   │→ │    Repositories      │   │
│  └─────────────┘  └──────────────┘  └──────────┬───────────┘   │
│  ┌─────────────────────────────────────────┐    │               │
│  │  Middleware: JWT Auth, Rate Limiting,   │    │               │
│  │  Security Headers, OTel Instrumentation │    │               │
│  └─────────────────────────────────────────┘    │               │
└────────────────────────────────────────────────┬┘               
                                                  │ EF Core
┌─────────────────────────────────────────────────▼───────────────┐
│              SQL Server (GEOGRAPHY / GEOMETRY spatial types)     │
│  Users │ Locations │ PendingEdits │ LocationCollections │ ...    │
└─────────────────────────────────────────────────────────────────┘
```

### Request Flow

```
Client → [Rate Limiting MW] → [JWT Auth MW] → [Security Headers MW]
       → Controller → Service → Repository → EF Core → SQL Server
       ← DTO mapping ← Business logic ← Query result ←
```

### .NET Aspire Orchestration

```
src/AppHost  orchestrates:
  ├── src/Api          (ASP.NET Core Web API)
  ├── src/client       (Vite dev server, proxied)
  └── SQL Server       (container resource)
  
All services call builder.AddServiceDefaults() which registers:
  - OpenTelemetry (traces, metrics, logs via OTLP)
  - Health checks (/health/live, /health/ready)
  - Resilience policies (retry, circuit breaker)
```

---

## Components and Interfaces

### Backend Layer Structure

```
src/Api/
  Controllers/
    AuthController.cs
    LocationsController.cs
    ImagesController.cs
    CollectionsController.cs
    NamedShapesController.cs
    UsersController.cs
    AdminController.cs
    NotificationsController.cs
  Services/
    IAuthService.cs / AuthService.cs
    ILocationService.cs / LocationService.cs
    IImageProcessingService.cs / ImageProcessingService.cs
    ICollectionService.cs / CollectionService.cs
    INamedShapeService.cs / NamedShapeService.cs
    IUserService.cs / UserService.cs
    INotificationService.cs / NotificationService.cs
    IAuditService.cs / AuditService.cs
    IBackupService.cs / BackupService.cs
    IEmailService.cs / EmailService.cs
    ICacheService.cs / CacheService.cs
    ICoordinateReprojectionService.cs / CoordinateReprojectionService.cs
  Repositories/
    ILocationRepository.cs / LocationRepository.cs
    IUserRepository.cs / UserRepository.cs
    IImageRepository.cs / ImageRepository.cs
    ICollectionRepository.cs / CollectionRepository.cs
    INamedShapeRepository.cs / NamedShapeRepository.cs
    INotificationRepository.cs / NotificationRepository.cs
    IAuditRepository.cs / AuditRepository.cs
    IPendingEditRepository.cs / PendingEditRepository.cs
  Data/
    AppDbContext.cs
    Migrations/
  Models/
    Entities/       (EF Core entity classes)
    Dtos/           (request/response DTOs)
    Enums/
  Middleware/
    SecurityHeadersMiddleware.cs
  Infrastructure/
    JwtTokenService.cs
    SmtpEmailService.cs
    LocalFileStorageService.cs
```

### Key Service Interfaces

```csharp
/// <summary>Reprojects coordinates from a source CRS to WGS84 (EPSG:4326).</summary>
public interface ICoordinateReprojectionService
{
    bool IsSridSupported(int srid);
    (double Latitude, double Longitude) ReprojectToWgs84(double latitude, double longitude, int sourceSrid);
}

/// <summary>Generates and stores image variants on upload.</summary>
public interface IImageProcessingService
{
    Task<ImageVariants> ProcessAndStoreAsync(Stream imageStream, string mimeType, string? altText, CancellationToken ct);
    Task DeleteImageAndVariantsAsync(Guid imageId, CancellationToken ct);
}

/// <summary>Records append-only audit events.</summary>
public interface IAuditService
{
    Task RecordAsync(AuditEventType eventType, Guid? actingUserId, string? targetResourceType,
        Guid? targetResourceId, AuditOutcome outcome, string sourceIp, CancellationToken ct);
}

/// <summary>Creates in-app notifications for workflow events.</summary>
public interface INotificationService
{
    Task NotifyPendingEditSubmittedAsync(Guid creatorUserId, Guid locationId, string locationName, CancellationToken ct);
    Task NotifyEditApprovedAsync(Guid submitterUserId, Guid locationId, string locationName, CancellationToken ct);
    Task NotifyEditRejectedAsync(Guid submitterUserId, Guid locationId, string locationName, CancellationToken ct);
    Task NotifyMembershipApprovedAsync(Guid requesterUserId, Guid collectionId, string collectionName, CancellationToken ct);
    Task NotifyMembershipRejectedAsync(Guid requesterUserId, Guid collectionId, string collectionName, CancellationToken ct);
}

/// <summary>Produces and consumes AES-256 encrypted backup archives.</summary>
public interface IBackupService
{
    Task<Stream> ExportAsync(string encryptionKey, CancellationToken ct);
    Task<ImportResult> ImportAsync(Stream archive, string decryptionKey, CancellationToken ct);
}

/// <summary>Wraps IMemoryCache / IDistributedCache with typed invalidation.</summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class;
    Task InvalidateAsync(string key, CancellationToken ct);
    Task InvalidateByPrefixAsync(string prefix, CancellationToken ct);
}
```

### Frontend Component Tree

```
App
├── AuthProvider (JWT context, user profile state)
├── Router
│   ├── /login                → LoginPage
│   │   ├── LoginForm
│   │   └── RegisterForm
│   ├── /forgot-password      → ForgotPasswordPage
│   ├── /reset-password       → ResetPasswordPage
│   ├── / (protected)         → HomePage
│   │   └── CollectionCard[]
│   ├── /locations (protected)→ LocationListPage
│   │   └── LocationCard[]
│   ├── /locations/:id        → LocationDetailPage
│   │   ├── LeafletMap
│   │   ├── ContentSequenceViewer
│   │   └── PendingEditPanel (creator only)
│   ├── /collections/:id      → CollectionDetailPage
│   │   ├── LeafletMap
│   │   └── LocationCard[]
│   ├── /settings (protected) → UserConfigurationPage
│   │   ├── DisplayNameEditor
│   │   ├── AvatarUploader (with crop tool)
│   │   ├── PasswordChangeForm
│   │   └── PreferencesToggle
│   ├── /admin/users          → AdminUsersPage
│   │   └── UserRoleTable
│   ├── /admin/audit-log      → AuditLogPage
│   │   └── AuditLogTable
│   └── /admin/import-export  → ImportExportPage
└── AppLayout (persistent shell for authenticated pages)
    ├── UserMenu (top-right)
    │   ├── AvatarDisplay
    │   ├── NotificationPanel
    │   └── SignOutButton
    └── <Outlet />
```

---

## Data Models

### Database Schema

#### Users

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK, default NEWSEQUENTIALID() |
| Username | NVARCHAR(50) | NOT NULL, UNIQUE (case-insensitive collation) |
| DisplayName | NVARCHAR(100) | NOT NULL, UNIQUE (case-insensitive collation) |
| Email | NVARCHAR(254) | NOT NULL, UNIQUE (case-insensitive collation) |
| PasswordHash | NVARCHAR(72) | NOT NULL (bcrypt hash) |
| Role | NVARCHAR(20) | NOT NULL, DEFAULT 'Standard' |
| AvatarImageId | UNIQUEIDENTIFIER | FK → Images.Id, NULL |
| ShowPublicCollections | BIT | NOT NULL, DEFAULT 1 |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSUTCDATETIME() |

#### Locations

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK, default NEWSEQUENTIALID() |
| Name | NVARCHAR(200) | NOT NULL |
| CreatorId | UNIQUEIDENTIFIER | NOT NULL, FK → Users.Id |
| CreatedAt | DATETIMEOFFSET | NOT NULL |
| SourceSrid | INT | NOT NULL |
| Coordinates | GEOGRAPHY | NOT NULL, SRID 4326 |
| ContentSequence | NVARCHAR(MAX) | NOT NULL (JSON) |

Indexes: spatial index on `Coordinates`; non-clustered index on `CreatorId`; non-clustered index on `CreatedAt DESC`.

#### PendingEdits

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| LocationId | UNIQUEIDENTIFIER | NOT NULL, FK → Locations.Id |
| SubmittedByUserId | UNIQUEIDENTIFIER | NOT NULL, FK → Users.Id |
| SubmittedAt | DATETIMEOFFSET | NOT NULL |
| SourceSrid | INT | NOT NULL |
| Coordinates | GEOGRAPHY | NOT NULL, SRID 4326 |
| ContentSequence | NVARCHAR(MAX) | NOT NULL (JSON) |

Unique constraint: (LocationId, SubmittedByUserId) — one pending edit per user per location.

#### LocationCollections

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| Name | NVARCHAR(200) | NOT NULL |
| Description | NVARCHAR(2000) | NULL |
| OwnerId | UNIQUEIDENTIFIER | NOT NULL, FK → Users.Id |
| Visibility | NVARCHAR(20) | NOT NULL, DEFAULT 'Private' |
| NamedShapeId | UNIQUEIDENTIFIER | FK → NamedShapes.Id, NULL |
| CollectionImageId | UNIQUEIDENTIFIER | FK → Images.Id, NULL |
| CreatedAt | DATETIMEOFFSET | NOT NULL |

Indexes: non-clustered on `OwnerId`; non-clustered on `Visibility, CreatedAt DESC`.

#### CollectionMembers

| Column | Type | Constraints |
|---|---|---|
| LocationId | UNIQUEIDENTIFIER | NOT NULL, FK → Locations.Id |
| CollectionId | UNIQUEIDENTIFIER | NOT NULL, FK → LocationCollections.Id |

PK: (LocationId, CollectionId).

#### PendingMembershipRequests

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| LocationId | UNIQUEIDENTIFIER | NOT NULL, FK → Locations.Id |
| CollectionId | UNIQUEIDENTIFIER | NOT NULL, FK → LocationCollections.Id |
| RequestedByUserId | UNIQUEIDENTIFIER | NOT NULL, FK → Users.Id |
| RequestedAt | DATETIMEOFFSET | NOT NULL |

#### NamedShapes

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| Name | NVARCHAR(200) | NOT NULL, UNIQUE (case-insensitive) |
| Geometry | GEOGRAPHY | NOT NULL |

Indexes: spatial index on `Geometry`.

#### Images

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| MimeType | NVARCHAR(50) | NOT NULL |
| AltText | NVARCHAR(500) | NULL |
| StoredPath | NVARCHAR(500) | NOT NULL |
| ThumbnailPath | NVARCHAR(500) | NOT NULL |
| Variant400Path | NVARCHAR(500) | NULL |
| Variant700Path | NVARCHAR(500) | NULL |
| Variant1000Path | NVARCHAR(500) | NULL |
| UploadedByUserId | UNIQUEIDENTIFIER | NOT NULL, FK → Users.Id |
| UploadedAt | DATETIMEOFFSET | NOT NULL |

#### Notifications

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| RecipientUserId | UNIQUEIDENTIFIER | NOT NULL, FK → Users.Id |
| EventType | NVARCHAR(50) | NOT NULL |
| ResourceType | NVARCHAR(50) | NOT NULL |
| ResourceId | UNIQUEIDENTIFIER | NOT NULL |
| ResourceName | NVARCHAR(200) | NOT NULL |
| IsRead | BIT | NOT NULL, DEFAULT 0 |
| CreatedAt | DATETIMEOFFSET | NOT NULL |

Indexes: non-clustered on `(RecipientUserId, IsRead, CreatedAt DESC)`.

#### AuditEvents

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| EventType | NVARCHAR(100) | NOT NULL |
| ActingUserId | UNIQUEIDENTIFIER | NULL (anonymous for unauthenticated) |
| ActingUserLabel | NVARCHAR(20) | NOT NULL (stores 'anonymous' or user id string) |
| TargetResourceType | NVARCHAR(50) | NULL |
| TargetResourceId | UNIQUEIDENTIFIER | NULL |
| Outcome | NVARCHAR(20) | NOT NULL ('success' or 'failure') |
| SourceIp | NVARCHAR(45) | NOT NULL |
| OccurredAt | DATETIMEOFFSET | NOT NULL |

Indexes: non-clustered on `OccurredAt DESC`; non-clustered on `(EventType, OccurredAt DESC)`; non-clustered on `ActingUserId`.

Note: AuditEvents has no UPDATE or DELETE permissions granted to the application database account — enforced at the database level.

#### PasswordResetTokens

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| UserId | UNIQUEIDENTIFIER | NOT NULL, FK → Users.Id |
| TokenHash | NVARCHAR(64) | NOT NULL (SHA-256 hash of the token) |
| ExpiresAt | DATETIMEOFFSET | NOT NULL |
| UsedAt | DATETIMEOFFSET | NULL |

### EF Core Entity Models (C# summary)

```csharp
// Key entity shapes — full implementations in src/Api/Data/Entities/

public sealed class Location
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public required Guid CreatorId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int SourceSrid { get; set; }
    public required Point Coordinates { get; set; }   // NetTopologySuite Point, SRID 4326
    public required string ContentSequenceJson { get; set; }
    public User Creator { get; init; } = null!;
}

public sealed class ContentBlock
{
    public required string Type { get; init; }   // "Heading" | "Paragraph" | "Image"
    public string? Text { get; init; }
    public int? Level { get; init; }             // 1, 2, or 3 for Heading blocks
    public Guid? ImageId { get; init; }
}
// ContentSequence is serialised as JSON array of ContentBlock into Location.ContentSequenceJson
```

### ContentSequence JSON Schema

```json
[
  { "type": "Heading", "text": "Introduction", "level": 1 },
  { "type": "Paragraph", "text": "Body text here." },
  { "type": "Image", "imageId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
]
```

Validation rules:
- Array length: 1–200 items
- `type` must be one of `Heading`, `Paragraph`, `Image`
- `Heading`: `text` required (max 10,000 chars), `level` required (1, 2, or 3; defaults to 2 if omitted)
- `Paragraph`: `text` required (max 10,000 chars)
- `Image`: `imageId` required (must reference an existing Image record)

---

## API Endpoint Reference

### Authentication

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | /api/auth/register | None | Register new user |
| POST | /api/auth/login | None | Login, returns JWT |
| POST | /api/auth/forgot-password | None | Request password reset email |
| POST | /api/auth/reset-password | None | Complete password reset with token |

### Locations

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/locations | None | Paginated list (no ContentSequence) |
| POST | /api/locations | Required | Create location |
| GET | /api/locations/{id} | None | Location detail (canonical or pending for submitter) |
| PUT | /api/locations/{id} | Required | Edit location (creator: immediate; other: pending) |
| DELETE | /api/locations/{id} | Required | Delete (creator or admin) |
| GET | /api/locations/{id}/pending-edits | Required (creator) | List pending edits |
| POST | /api/locations/{id}/pending-edits/{editId}/approve | Required (creator) | Approve pending edit |
| POST | /api/locations/{id}/pending-edits/{editId}/reject | Required (creator) | Reject pending edit |

### Images

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | /api/images | Required | Upload image, returns all variant URLs |
| GET | /api/images/{id} | None | Serve full-resolution image |
| GET | /api/images/{id}/thumbnail | None | Serve 200×200 thumbnail |
| GET | /api/images/{id}/variants/{width} | None | Serve responsive variant (400, 700, 1000) |

### LocationCollections

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/collections | None | Paginated public collections list |
| GET | /api/collections/combined | Required | Public + owned collections with isOwner flag |
| POST | /api/collections | Required | Create collection |
| GET | /api/collections/{id} | Conditional | Detail (public: none; private: owner only) |
| PUT | /api/collections/{id} | Required (owner) | Edit collection metadata |
| DELETE | /api/collections/{id} | Required (owner or admin) | Delete collection |
| POST | /api/collections/{id}/members | Required | Add member (owner: direct; other: pending) |
| DELETE | /api/collections/{id}/members/{locationId} | Required (owner) | Remove member |
| GET | /api/collections/{id}/pending-members | Required (owner) | List pending membership requests |
| POST | /api/collections/{id}/pending-members/{requestId}/approve | Required (owner) | Approve membership |
| POST | /api/collections/{id}/pending-members/{requestId}/reject | Required (owner) | Reject membership |

### Named Shapes

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/named-shapes | Required | Paginated list (id + name only) |
| POST | /api/named-shapes | Required (admin) | Upload GeoJSON shape |
| PUT | /api/named-shapes/{id} | Required (admin) | Rename shape |
| DELETE | /api/named-shapes/{id} | Required (admin) | Delete shape |

### Users

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/users/me | Required | Get own profile |
| PUT | /api/users/me/display-name | Required | Change display name |
| PUT | /api/users/me/password | Required | Change password |
| PUT | /api/users/me/avatar | Required | Upload avatar image |
| PUT | /api/users/me/preferences | Required | Update ShowPublicCollections |

### Admin

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/admin/users | Required (admin) | List all users with roles |
| PUT | /api/admin/users/{id}/role | Required (admin) | Promote or demote user |
| GET | /api/admin/audit-log | Required (admin) | Paginated, filterable audit log |
| POST | /api/admin/export | Required (admin) | Export encrypted backup archive |
| POST | /api/admin/import | Required (admin) | Import backup archive |
| POST | /api/admin/resources/{type}/{id}/reassign | Required (admin) | Reassign resource ownership |

### Notifications

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/notifications | Required | List unread notifications |
| PUT | /api/notifications/{id}/read | Required | Mark one notification as read |
| PUT | /api/notifications/read-all | Required | Mark all notifications as read |
| DELETE | /api/notifications/{id} | Required | Delete a notification |

---

## Coordinate Reprojection

### Design

The `CoordinateReprojectionService` wraps the **ProjNET** library (NuGet: `ProjNET`) to convert submitted coordinates from any supported CRS to WGS84 (EPSG:4326).

```
Submitted (lat, lon, srid)
        │
        ▼
IsSridSupported(srid)?  ──No──→ HTTP 400
        │ Yes
        ▼
ReprojectToWgs84(lat, lon, srid)
        │
        ▼
Round to 6 decimal places
        │
        ▼
Store as GEOGRAPHY SRID 4326
Store original srid as SourceSrid metadata
```

### Supported SRIDs

The service maintains a whitelist of supported SRIDs loaded from the embedded SRID database shipped with ProjNET. The `IsSridSupported` check runs before any reprojection attempt. Unsupported SRIDs return HTTP 400 (Requirements 3.18, 13.3, 13.8).

### Rounding

After reprojection, both latitude and longitude are rounded to exactly 6 decimal places using `Math.Round(value, 6, MidpointRounding.AwayFromZero)`. This is a silent normalisation step — no error is returned (Requirements 3.16, 3.17).

### Consistency

The same `ICoordinateReprojectionService` is injected into `LocationService` (create and creator-edit paths) and `PendingEditService` (non-creator path), ensuring identical behaviour across all write paths (Requirement 13.6).

---

## Image Processing Pipeline

### Upload Flow

```
POST /api/images
        │
        ▼
Validate MIME type (jpeg/png/webp) ──fail──→ HTTP 415
        │
        ▼
Validate file size ≤ 10 MB ──fail──→ HTTP 413
        │
        ▼
Validate altText ≤ 500 chars (if provided) ──fail──→ HTTP 400
        │
        ▼
IImageProcessingService.ProcessAndStoreAsync()
  ├── Generate ThumbnailVariant: 200×200 square crop (centre)
  ├── Detect dominant orientation (portrait → 2:3, landscape → 3:2)
  ├── Generate Variant400: 400px wide, 2:3 or 3:2 aspect
  ├── Generate Variant700: 700px wide, 2:3 or 3:2 aspect
  └── Generate Variant1000: 1000px wide, 2:3 or 3:2 aspect
        │
        ▼
All variants generated successfully? ──No──→ HTTP 422 (no partial persist)
        │ Yes
        ▼
Persist Image record + all variant files
        │
        ▼
HTTP 201 with imageId, thumbnailUrl, variant400Url, variant700Url, variant1000Url
```

### Library

**SixLabors.ImageSharp** (NuGet: `SixLabors.ImageSharp`) is used for all server-side image processing. It is cross-platform, does not require GDI+, and supports JPEG, PNG, and WebP natively.

### Storage

Images are stored on the local filesystem. The root storage path is configured via the `IMAGES_STORAGE_PATH` environment variable. The `LocalFileStorageService` resolves paths as `{root}/{imageId}/{variant}.{ext}`. The internal path is never exposed in API responses — only the URL endpoint path is returned.

### Avatar Upload

Avatar uploads follow the same pipeline but with a 1 MB size limit (Requirement 26.7). The crop is always 1:1 (square). No responsive variant set is generated for avatars — only the ThumbnailVariant is stored and served.

### Image Deletion

When a Location is deleted, edited (creator path), or a PendingEdit is rejected, the service checks whether each referenced image is referenced by any other ContentBlock in the system. If not, `IImageProcessingService.DeleteImageAndVariantsAsync` removes all variant files and the database record.

---

## Caching Strategy

### Cache Layers

| Environment | Implementation |
|---|---|
| Development (single instance) | `IMemoryCache` |
| Production (multi-instance) | `IDistributedCache` (Redis or SQL Server) |

The `ICacheService` abstraction wraps both, so the application code is identical in both environments. The implementation is selected via DI registration in `Program.cs` based on configuration.

### Cache Keys and TTLs

| Resource | Cache Key Pattern | TTL | Invalidation Trigger |
|---|---|---|---|
| Location list | `locations:list:{page}:{pageSize}` | 60 s | Location created or updated |
| Location detail | `locations:detail:{id}` | 60 s | Location canonical version changes |
| Public collection list | `collections:public:{page}:{pageSize}` | 60 s | Collection created/updated/deleted/visibility changed |
| Combined collection list | `collections:combined:{userId}:{page}:{pageSize}` | NOT CACHED | Always fresh (user-specific) |
| Collection detail (public) | `collections:detail:{id}` | 60 s | Collection or membership changes |
| Image variants | HTTP Cache-Control headers | 1 year (immutable) | New upload = new ID |

### Rules

- User-specific responses (private collections, PendingEdits, UserProfile, notifications) are **never** cached server-side (Requirement 27.12).
- Cache keys include all query parameters to prevent stale data (Requirement 27.13).
- Image variant responses carry `Cache-Control: public, max-age=31536000, immutable` since a new upload always produces a new identifier (Requirement 27.23).

---

## Security Design

### Authentication

- JWT bearer tokens issued on login with a 24-hour expiry (Requirement 2.4).
- Signing key stored in environment variable `JWT_SIGNING_KEY` — never hardcoded.
- Tokens contain: `sub` (userId), `role`, `iat`, `exp`.
- `[Authorize]` attribute applied at controller level; specific role requirements use `[Authorize(Roles = "Admin")]`.

### Password Handling

- Passwords hashed with **BCrypt** (cost factor 12) via `BCrypt.Net-Next` (NuGet: `BCrypt.Net-Next`).
- Plaintext password never persisted or logged (Requirements 1.7, 2.7, 33.5).
- Password reset tokens are single-use, 1-hour expiry, stored as SHA-256 hash (Requirement 34.3).

### Rate Limiting

ASP.NET Core built-in rate limiting (`Microsoft.AspNetCore.RateLimiting`) is used:

| Endpoint | Policy | Limit |
|---|---|---|
| POST /api/auth/login | Fixed window per username | 10 failures / 15 min |
| POST /api/auth/login | Fixed window per IP | 20 attempts / 1 min |
| POST /api/auth/forgot-password | Fixed window per username | 5 requests / 1 hour |
| POST /api/images | Sliding window per user | 20 uploads / 1 hour |

All 429 responses include a `Retry-After` header (Requirement 36.5). Rate limit counters are stored in the distributed cache when configured, surviving restarts (Requirement 36.6).

### Security Headers

A `SecurityHeadersMiddleware` sets on every response:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`

(Requirement 15.7)

### Input Validation

- All request DTOs use `[Required]`, `[MaxLength]`, `[Range]` data annotations validated by the ASP.NET Core model binding pipeline.
- Coordinate ranges validated: latitude [-90, 90], longitude [-180, 180].
- ContentSequence validated at the service layer (type, length, block count).
- GeoJSON geometry validated for type (Polygon/MultiPolygon) and vertex count ≤ 1000 (Requirement 21.4).
- All database access via EF Core LINQ — no raw SQL string concatenation (Requirement 15.4).

### Authorisation Enforcement

Authorisation checks are enforced at the **service layer**, not only at the controller layer, for:
- Private collection visibility (Requirement 21.2)
- Audit log access (Requirement 39.19)
- Admin-only operations

---

## Observability Design

### ActivitySource and Meter Registration

```csharp
// Registered in Program.cs via AddServiceDefaults() extension
public static class LocationManagementTelemetry
{
    public static readonly ActivitySource ActivitySource =
        new("LocationManagement", "1.0.0");

    public static readonly Meter Meter =
        new("LocationManagement", "1.0.0");

    // Histograms
    public static readonly Histogram<double> LocationCreateDuration =
        Meter.CreateHistogram<double>("location.create.duration", "ms");

    public static readonly Histogram<double> LocationEditDuration =
        Meter.CreateHistogram<double>("location.edit.duration", "ms");

    public static readonly Counter<long> ImageUploadCount =
        Meter.CreateCounter<long>("image.upload.count");
}
```

### Structured Log Events

Key log events (using `LoggerMessage.Define` source-generated logging):

| Level | Event | Fields |
|---|---|---|
| Information | LocationCreated | locationId, actingUserId |
| Information | LocationEdited | locationId, actingUserId, editType (creator/pending) |
| Information | PendingEditApproved | locationId, editId, actingUserId |
| Information | PendingEditRejected | locationId, editId, actingUserId |
| Information | LocationDeleted | locationId, actingUserId |
| Warning | AuthenticationFailed | username (not password), sourceIp |
| Warning | AuditLogAccessDenied | requestingUserId |
| Error | UnhandledException | traceId, spanId, exceptionType |
| Information | PasswordChanged | userId |
| Information | PasswordResetCompleted | userId |
| Information | AvatarUploaded | userId |
| Information | PreferenceChanged | userId |

---

## Audit Log Design

### Append-Only Enforcement

The `AuditEvents` table is protected at two levels:
1. The application database account has INSERT-only permission on `AuditEvents` — no UPDATE or DELETE.
2. The `IAuditService` interface exposes only `RecordAsync` — no update or delete methods exist.

### Retention

A background `IHostedService` (`AuditRetentionService`) runs daily and deletes AuditEvents older than 1 year. It logs the count of purged records at `Information` level (Requirement 39.7–39.8).

### Notification Cleanup

A separate background service (`NotificationCleanupService`) deletes read notifications older than 30 days (Requirement 31.14).

---

## Backup and Import Design

### Export

The `BackupService.ExportAsync` method:
1. Queries all Locations, LocationCollections, NamedShapes, Images, and AuditEvents.
2. Serialises to a structured JSON manifest.
3. Packages the JSON manifest and all image files into a ZIP archive in memory.
4. Encrypts the ZIP using AES-256 with the caller-supplied key (minimum 32 characters).
5. Returns the encrypted stream as a binary download.

The encryption key is never logged or stored (Requirements 40.4–40.5).

### Import

The `BackupService.ImportAsync` method:
1. Decrypts the archive using the caller-supplied key.
2. Validates the archive schema (HTTP 422 on failure — no partial import).
3. Creates or reuses an `ImportUser` account.
4. Imports records additively, assigning new IDs to any that conflict with existing records.
5. Validates coordinates and images against the same rules as create operations; skips invalid records with a warning log.
6. Returns an `ImportResult` summary.

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

Property-based testing (PBT) is applicable to this feature because it contains pure transformation functions (coordinate reprojection, ContentSequence serialisation, password validation) with large input spaces where 100+ iterations meaningfully increase confidence. PBT is implemented using **FsCheck** with the **FsCheck.Xunit** integration (NuGet: `FsCheck.Xunit`), configured to run a minimum of 100 iterations per property.

### Property 1: Coordinate Round-Trip

*For any* valid coordinate pair (latitude in [-90, 90], longitude in [-180, 180]) and any supported SRID, storing the coordinate via any write path (create Location, creator edit, non-creator PendingEdit) and then retrieving it SHALL produce WGS84 values equal to the reprojected input rounded to 6 decimal places.

Formally: `round(retrieve(store(reproject(lat, lon, srid))), 6) == round(reproject(lat, lon, srid), 6)`

**Validates: Requirements 3.11, 3.16, 13.5, 13.6**

### Property 2: ContentSequence Serialisation Round-Trip

*For any* valid ContentSequence (a list of 1–200 ContentBlocks of types Heading, Paragraph, and Image, with all required fields populated), serialising the sequence to JSON and deserialising it SHALL produce a ContentSequence that is structurally and semantically equivalent to the original — preserving block order, block types, text content, heading levels, and image identifiers.

**Validates: Requirements 12.1, 12.2, 12.3, 37.6**

### Property 3: Image Alt Text Round-Trip

*For any* valid alt text string (non-null, length 1–500 characters, arbitrary Unicode content), uploading an image with that alt text and then retrieving the image record SHALL return the same alt text string without modification.

**Validates: Requirements 6.9, 6.11**

### Property 4: Password Complexity Rejection

*For any* string that violates at least one password complexity rule (length < 8, no uppercase letter, no lowercase letter, or no digit), submitting that string as a password in a registration or change-password request SHALL result in HTTP 400 being returned, and no User record SHALL be created or modified.

**Validates: Requirements 1.3, 1.4, 33.3**

### Property 5: Audit Log Append-Only Invariant

*For any* sequence of mutating operations performed on the system, the set of AuditEvents recorded before those operations SHALL remain unchanged after those operations complete — no previously recorded AuditEvent SHALL be modified or deleted as a side effect of any application code path.

**Validates: Requirements 39.6**

---

## Error Handling

### API Error Response Format

All error responses use a consistent JSON envelope:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Latitude must be between -90 and 90.",
  "traceId": "00-abc123-def456-00"
}
```

This is the standard ASP.NET Core `ProblemDetails` format. The `traceId` field links the error to the distributed trace for debugging.

### Error Scenarios by Domain

#### Authentication Errors
- `401` — missing or invalid JWT on protected endpoints
- `401` — incorrect credentials on login (generic message, no username/password distinction)
- `429` — rate limit exceeded (with `Retry-After` header)

#### Location Errors
- `400` — invalid coordinates, SRID, ContentSequence, or name
- `400` — unsupported SRID for reprojection
- `401` — unauthenticated mutation
- `403` — non-creator attempting creator-only operation
- `404` — location not found

#### Image Errors
- `400` — alt text exceeds 500 characters
- `413` — file exceeds size limit
- `415` — unsupported MIME type
- `422` — image processing failed (corrupt data, variant generation failure)

#### Collection Errors
- `400` — invalid NamedShape reference
- `403` — non-owner attempting owner-only operation
- `403` — accessing private collection as non-owner
- `409` — location already a member of the collection

#### Admin Errors
- `403` — non-admin accessing admin endpoint
- `409` — attempting to demote the last admin
- `409` — attempting to delete a NamedShape that is in use

#### Backup/Import Errors
- `400` — missing or too-short encryption/decryption key
- `400` — incorrect decryption key or corrupt archive
- `422` — archive does not conform to expected schema

### Unhandled Exceptions

A global exception handler middleware catches all unhandled exceptions, logs them at `Error` level with `TraceId` and `SpanId`, and returns a generic `500` ProblemDetails response. The exception detail is never exposed to the client in production.

---

## Testing Strategy

### Overview

The testing strategy follows the pyramid: ~70% unit tests, ~20% integration tests, ~10% E2E tests.

### Backend Unit Tests (xUnit + Moq)

Every public method on every service and repository has unit tests. Key areas:

- `CoordinateReprojectionService` — reprojection correctness, unsupported SRID rejection, rounding
- `LocationService` — create/edit/delete logic, creator vs non-creator path routing, image cleanup
- `ImageProcessingService` — variant generation, MIME validation, size validation, failure handling
- `AuthService` — registration validation, login, JWT issuance, password complexity
- `BackupService` — export structure, import additive behaviour, ImportUser creation
- `AuditService` — event recording, append-only enforcement
- `CacheService` — get/set/invalidate behaviour

Test naming convention: `MethodName_WhenCondition_ExpectedOutcome` (e.g. `CreateLocation_WhenSridUnsupported_Returns400`).

### Property-Based Tests (FsCheck + FsCheck.Xunit)

Each correctness property from the Correctness Properties section is implemented as a single property-based test, configured to run a minimum of 100 iterations. Tests are tagged with a comment referencing the design property:

```csharp
// Feature: location-management, Property 1: Coordinate Round-Trip
[Property(MaxTest = 1000)]
public Property CoordinateRoundTrip_HoldsForAllValidInputs()
{
    // Arrange generators for valid (lat, lon, srid) triples
    // Act: reproject, store, retrieve
    // Assert: round(retrieved, 6) == round(reprojected, 6)
}
```

Tag format: `Feature: location-management, Property {N}: {property_text}`

### Backend Integration Tests (xUnit + real DB)

Integration tests use a real SQL Server instance (via Testcontainers or a local instance) and test:
- EF Core migrations apply cleanly
- Spatial queries return correct results
- Cache invalidation triggers correctly on mutations
- Audit events are recorded for all mutating operations (1–3 examples per operation type)
- Rate limiting counters persist across requests

### Frontend Unit Tests (Vitest + React Testing Library)

- `ContentSequenceViewer` — renders all block types correctly
- `ContentSequenceEditor` — add/remove/reorder blocks
- `LeafletMap` — renders pins at correct coordinates
- `ImageUploader` — crop tool constrains to correct aspect ratio
- `NotificationPanel` — displays unread count, marks as read
- `LoginForm` / `RegisterForm` — validation feedback, error display
- `AuditLogTable` — filter controls, pagination

### E2E Tests (Playwright + @axe-core/playwright)

Every page has at least one E2E test covering the happy path. Critical journeys:

1. Register → Login → Create Location → View Location
2. Non-creator submits edit → Creator approves → Canonical version updated
3. Create Collection → Add Location → View Collection map
4. Admin promotes user → Promoted user accesses admin features
5. Admin exports backup → Admin imports backup → Data present

Every Playwright test includes an axe-core accessibility assertion:

```typescript
// Feature: location-management — accessibility gate on every page
const results = await new AxeBuilder({ page }).analyze();
const violations = results.violations.filter(
  v => v.impact === 'critical' || v.impact === 'serious'
);
expect(violations).toHaveLength(0);
```

### Accessibility Testing

- Automated: `@axe-core/playwright` on all pages — zero critical/serious violations required.
- Manual: keyboard-only navigation verified for map, modals, and rich content editor.
- Map alternative: a text list of all Locations with names and coordinates is always rendered alongside the Leaflet map (Requirement 38.6).
- All form fields have associated `<label>` elements; validation errors use `aria-describedby`.
- Notification count uses `aria-live="polite"`.

### Mutation Testing (Stryker.NET)

Stryker.NET is configured in `src/Api.Tests/stryker-config.json` with a minimum threshold of 80%. Property-based tests count toward mutation coverage.

---

## Frontend Architecture Detail

### Auth Context

```typescript
// src/client/src/contexts/AuthContext.tsx
interface AuthContextValue {
  user: UserProfile | null;
  token: string | null;
  login: (token: string, profile: UserProfile) => void;
  logout: () => void;
  isAuthenticated: boolean;
}
```

The JWT is stored in `localStorage`. On app load, the token is read and the user profile is fetched from `GET /api/users/me`. If the token is expired or invalid, the user is redirected to `/login`.

### Protected Routes

```typescript
// Wraps any route that requires authentication
function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}

// Wraps admin-only routes
function AdminRoute({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  return user?.role === 'Admin' ? children : <Navigate to="/" replace />;
}
```

### Leaflet Map Integration

The `LeafletMap` component renders Location pins using `react-leaflet`. Coordinate reprojection for display is handled client-side using **Proj4js** when the Location's `sourceSrid` is not 4326 — the stored WGS84 coordinates are used directly for rendering, but the source SRID is displayed as metadata.

A keyboard-accessible text alternative is always rendered below the map as a `<table>` or `<ul>` listing all Locations with their names and WGS84 coordinates (Requirement 38.6, accessibility.md).

### ContentSequence Editor

The `ContentSequenceEditor` component manages an ordered list of ContentBlocks. It supports:
- Add block (Heading, Paragraph, Image) via a toolbar
- Reorder blocks via drag-and-drop (with keyboard alternative using move-up/move-down buttons)
- Remove blocks
- Edit heading level (1/2/3 selector)
- Image upload inline (triggers `POST /api/images`, displays thumbnail preview)

### Image Upload with Crop

The `ImageUploader` component uses the browser's `<input type="file">` and a canvas-based crop tool. For avatars, the crop is constrained to 1:1. For content images, no crop constraint is applied (the server determines orientation). The cropped canvas is converted to a Blob and submitted as `multipart/form-data`.

### Notification Panel

The `NotificationPanel` is rendered inside the `UserMenu`. It polls `GET /api/notifications` on a 30-second interval when the panel is open. The unread count badge uses `aria-live="polite"` so screen readers announce new notifications.

---

## Design Decisions

### Decision 1: ProjNET for Coordinate Reprojection

**Chosen**: `ProjNET` (NuGet: `ProjNET`) — the actively maintained successor to `ProjNet4GeoAPI`.

**Rationale**: ProjNET is the standard .NET library for coordinate system transformations, maintained by the NetTopologySuite organisation. It ships with an embedded SRID database covering thousands of coordinate reference systems. The alternative (calling an external projection service) would introduce a network dependency and latency on every write operation.

**ADR reference**: See `docs/adrs/0001-use-nettopologysuite-for-gis.md` for the broader GIS stack decision.

### Decision 2: SixLabors.ImageSharp for Image Processing

**Chosen**: `SixLabors.ImageSharp` (NuGet: `SixLabors.ImageSharp`).

**Rationale**: ImageSharp is cross-platform, does not depend on GDI+ (which is unavailable on Linux containers), and supports JPEG, PNG, and WebP. It is the de facto standard for server-side image processing in .NET. The alternative (`System.Drawing`) is not supported on non-Windows platforms.

**Licensing note**: ImageSharp uses the Six Labors Split License. Commercial use requires a commercial license for organisations above the free tier threshold. This should be confirmed before production deployment.

### Decision 3: FsCheck for Property-Based Testing

**Chosen**: `FsCheck` with `FsCheck.Xunit` integration.

**Rationale**: FsCheck is the most mature PBT library for .NET, with excellent xUnit integration and support for custom generators. It is well-suited for testing the coordinate round-trip and ContentSequence serialisation properties, which have large input spaces.

### Decision 4: Local Filesystem for Image Storage

**Chosen**: Local filesystem with configurable root path.

**Rationale**: Keeps the initial implementation simple and avoids a cloud storage dependency. The `IFileStorageService` abstraction allows swapping to S3 or Azure Blob Storage in a future iteration without changing the service layer. The storage path is configured via `IMAGES_STORAGE_PATH` environment variable.

### Decision 5: ContentSequence as JSON Column

**Chosen**: Store ContentSequence as a JSON string in a single `NVARCHAR(MAX)` column.

**Rationale**: ContentSequence is always read and written as a whole unit — there is no requirement to query individual blocks. A JSON column avoids a separate `ContentBlocks` table with a join on every Location read, simplifying queries and reducing latency. EF Core's `HasConversion` is used to serialise/deserialise the `List<ContentBlock>` automatically.

### Decision 6: BCrypt for Password Hashing

**Chosen**: BCrypt via `BCrypt.Net-Next` (NuGet: `BCrypt.Net-Next`), cost factor 12.

**Rationale**: BCrypt is a well-established, adaptive password hashing algorithm. Cost factor 12 provides a good balance between security and performance on modern hardware (~250 ms per hash). The alternative (Argon2 via `Konscious.Security.Cryptography`) would be marginally stronger but adds a dependency with less community adoption in the .NET ecosystem.

---
