# Requirements Document

## Introduction

The Location Management feature enables authenticated users to create, view, and edit geographic Locations. Each Location stores spatial coordinates, a Spatial Reference ID, rich formatted content (headings, paragraphs, and images), and ownership metadata. The feature also introduces a basic authentication system (username, display name, password) and an editorial approval workflow: edits made by the original creator are applied immediately, while edits from other users are held in a pending state until the creator approves or rejects them.

---

## Glossary

- **System**: The ASP.NET Core Web API and React frontend application collectively.
- **API**: The ASP.NET Core Web API backend (`src/Api`).
- **Client**: The React 18+ TypeScript frontend (`src/client`).
- **User**: A registered, authenticated human interacting with the System.
- **Creator**: The User who originally created a given Location.
- **Location**: A geographic entity storing coordinates, a Spatial Reference ID, rich formatted content, and ownership metadata.
- **Coordinate**: A latitude/longitude pair expressed as decimal degrees.
- **SRID**: Spatial Reference ID — an integer identifying a coordinate reference system (e.g. 4326 for WGS84).
- **WGS84**: The canonical coordinate reference system (EPSG:4326) used for all stored and returned coordinates in the System. All submitted coordinates are reprojected to WGS84 before persistence, regardless of the submitted SRID.
- **CoordinateReprojection**: The process of converting a coordinate pair from a source coordinate reference system (identified by the submitted SRID) to WGS84 (EPSG:4326) before the coordinate is persisted or returned by the API.
- **ContentBlock**: A single unit of formatted content — one of: `Heading`, `Paragraph`, or `Image`.
- **Heading**: A ContentBlock containing a plain-text heading string.
- **Paragraph**: A ContentBlock containing plain-text body content.
- **Image**: A ContentBlock referencing an image file stored internally within the System.
- **ContentSequence**: An ordered list of ContentBlocks that forms the formatted body of a Location.
- **PendingEdit**: A proposed set of changes to a Location submitted by a User who is not the Creator, awaiting approval or rejection by the Creator.
- **Canonical Version**: The currently approved, publicly visible state of a Location.
- **Approval Workflow**: The process by which the Creator reviews, approves, or rejects a PendingEdit.
- **JWT**: JSON Web Token — the bearer token issued by the API upon successful authentication.
- **Password Hash**: A bcrypt-derived hash of a User's password stored in the database; the plaintext password is never persisted.
- **LocationCollection**: A named, optionally public grouping of Locations, owned by a User, with optional metadata including a bounding shape, collection image, and description.
- **CollectionMember**: An association between a LocationCollection and a Location.
- **BoundingShape**: An optional polygon or other geometry drawn on the map to visually enclose the Locations in a LocationCollection.
- **CollectionVisibility**: Whether a LocationCollection is private (visible only to its creator) or public (visible to all users).
- **UserRole**: The access level assigned to a User account — either `Standard` (default) or `Admin`.
- **Admin**: A User with the `Admin` UserRole, permitted to manage NamedShapes and perform privileged system operations.
- **NamedShape**: A named, system-wide GeoJSON polygon or multi-polygon uploaded by an Admin, stored as a GEOGRAPHY type, and available for selection as the BoundingShape of a LocationCollection.
- **UserProfile**: The persisted preferences and profile data associated with a User account, including avatar image and display preferences.
- **Avatar**: A small square thumbnail image uploaded by a User to represent themselves in the UI.
- **ShowPublicCollections**: A boolean UserProfile preference that controls whether public LocationCollections from other users are shown on the authenticated User's homepage.
- **UserMenu**: A persistent UI component rendered in the top-right corner of every authenticated page, providing access to user profile settings and sign-out.
- **UserConfigurationPage**: A settings page accessible from the UserMenu where the authenticated User can manage their profile preferences.
- **ThumbnailVariant**: A pre-generated, resized version of an uploaded image stored alongside the original, used to serve small preview images efficiently without on-demand resizing.
- **ResponseCache**: A server-side in-memory or distributed cache storing the results of expensive read operations (list queries, collection detail, location detail) to reduce database load and meet latency targets.
- **DisplayVariant**: A pre-generated, resized version of an uploaded image cropped or fitted to a 2:3 or 3:2 aspect ratio with a longest edge of 1000px, stored alongside the original and ThumbnailVariant, used for full-content display in the UI.
- **ResponsiveVariantSet**: The complete set of pre-generated image variants for a single uploaded image: a 400px-wide variant, a 700px-wide variant, and the 1000px DisplayVariant, all sharing the same aspect ratio, used together in an HTML `<picture>` element with `srcset` to serve the most appropriate size for the user's viewport.
- **AuditEvent**: A tamper-evident, append-only record of a significant system action, stored in the audit log. Each AuditEvent captures who performed the action, what was done, which resource was affected, when it occurred, and whether it succeeded or failed.
- **AuditLog**: The persistent, append-only store of all AuditEvents in the System, retained for a minimum of 1 year and accessible only to Admin users.
- **BackupArchive**: An encrypted ZIP file produced by the export endpoint, containing all exportable system data (Locations, LocationCollections, NamedShapes, images, and AuditEvents) in a structured JSON format, encrypted using AES-256.
- **ImportUser**: A system-managed User account created automatically during an import operation, to which all imported Locations, LocationCollections, and other user-owned resources are assigned when the original owner cannot be matched to an existing User in the System.
- **OwnershipReassignment**: An Admin operation that transfers ownership or creator attribution of a Location, LocationCollection, or other user-owned resource from one User to another.

---

## Requirements

### Requirement 1: User Registration

**User Story:** As a visitor, I want to register an account with a username, display name, and password, so that I can authenticate and interact with Locations.

#### Acceptance Criteria

1. WHEN a registration request is received with a unique username, a unique non-empty display name, and a password that meets the complexity rules, THE API SHALL create a new User record and return HTTP 201.
2. WHEN a registration request is received with a username that already exists, THE API SHALL return HTTP 409 with a descriptive error message.
3. WHEN a registration request is received with a password shorter than 8 characters, THE API SHALL return HTTP 400 with a descriptive error message.
4. WHEN a registration request is received with a password that does not contain at least one uppercase letter, one lowercase letter, and one digit, THE API SHALL return HTTP 400 with a descriptive error message.
5. WHEN a registration request is received with an empty or whitespace-only username, THE API SHALL return HTTP 400 with a descriptive error message.
6. WHEN a registration request is received with an empty or whitespace-only display name, THE API SHALL return HTTP 400 with a descriptive error message.
7. THE API SHALL store only the Password Hash of the User's password — the plaintext password MUST NOT be persisted or logged.
8. THE API SHALL enforce a maximum username length of 50 characters and a maximum display name length of 100 characters; requests exceeding these limits SHALL return HTTP 400.
9. WHEN a registration request is received with a display name that already exists in the system (case-insensitive), THE API SHALL return HTTP 409 with a descriptive error message.
10. THE API SHALL assign the `Standard` UserRole to all newly registered Users by default; the `Admin` role MUST NOT be assignable via the public registration endpoint.
11. WHEN a registration request is received with a valid, unique email address, THE API SHALL store the email address associated with the User record.
12. WHEN a registration request is received without an email address or with a malformed email address, THE API SHALL return HTTP 400 with a descriptive error message.
13. WHEN a registration request is received with an email address that already exists in the system (case-insensitive), THE API SHALL return HTTP 409 with a descriptive error message.
14. THE API SHALL NOT expose the User's email address in any public-facing API response (list, detail, or profile endpoints accessible to other users).

---

### Requirement 2: User Authentication

**User Story:** As a registered User, I want to log in with my username and password, so that I can receive a token that authorises my subsequent requests.

#### Acceptance Criteria

1. WHEN a login request is received with a valid username and correct password, THE API SHALL return HTTP 200 with a JWT and the User's display name.
2. WHEN a login request is received with a valid username and an incorrect password, THE API SHALL return HTTP 401 with a generic error message that does not distinguish between unknown username and wrong password.
3. WHEN a login request is received with a username that does not exist, THE API SHALL return HTTP 401 with the same generic error message used for incorrect passwords.
4. THE API SHALL issue JWTs with an expiry of no more than 24 hours from the time of issuance.
5. WHEN a request is received on a protected endpoint without a JWT, THE API SHALL return HTTP 401.
6. WHEN a request is received on a protected endpoint with an expired or invalid JWT, THE API SHALL return HTTP 401.
7. THE API SHALL NOT log the submitted password at any log level.

---

### Requirement 3: Create a Location

**User Story:** As an authenticated User, I want to create a new Location with coordinates, a Spatial Reference ID, and formatted content, so that geographic places can be recorded in the System.

#### Acceptance Criteria

1. WHEN an authenticated User submits a valid create-location request with a name, THE API SHALL persist the Location and return HTTP 201 with the created Location's identifier, name, and canonical data.
2. THE API SHALL record the authenticated User as the Creator of the Location.
3. THE API SHALL record the UTC timestamp at which the Location was created.
4. WHEN a create-location request is received with a latitude outside the range -90 to 90 (inclusive), THE API SHALL return HTTP 400 with a descriptive error message.
5. WHEN a create-location request is received with a longitude outside the range -180 to 180 (inclusive), THE API SHALL return HTTP 400 with a descriptive error message.
6. WHEN a create-location request is received without an SRID, THE API SHALL default the SRID to 4326 (WGS84).
7. WHEN a create-location request is received with an SRID that is not a positive integer, THE API SHALL return HTTP 400 with a descriptive error message.
8. WHEN a create-location request is received with a ContentSequence containing zero ContentBlocks, THE API SHALL return HTTP 400 with a descriptive error message.
9. WHEN a create-location request is received with a ContentSequence containing more than 200 ContentBlocks, THE API SHALL return HTTP 400 with a descriptive error message.
10. WHEN a create-location request is received without authentication, THE API SHALL return HTTP 401.
11. THE API SHALL reproject the submitted coordinates from the source coordinate reference system identified by the submitted SRID to WGS84 (EPSG:4326) before persisting them; the stored GEOGRAPHY value SHALL always use SRID 4326.
12. WHEN a create-location request is received without a name, THE API SHALL return HTTP 400 with a descriptive error message.
13. THE API SHALL enforce a maximum Location name length of 200 characters; requests exceeding this limit SHALL return HTTP 400.
14. Location names are not required to be unique system-wide; multiple Locations MAY share the same name.
15. WHEN a create-location request is received with an Image ContentBlock that references an image with no `altText` stored, THE API SHALL accept the request but THE Client SHALL prompt the User to provide alt text for the image before the ContentSequence is submitted, consistent with Requirement 38.
16. THE API SHALL round the reprojected WGS84 latitude and longitude values to exactly 6 decimal places before persisting them, providing approximately 11.1 cm ground accuracy; this rounding applies to all submitted coordinates regardless of the source SRID.
17. Rounding reprojected coordinates to 6 decimal places is a normalisation step, not a validation error — the API SHALL silently round without returning an error regardless of the precision of the submitted input.
18. THE API SHALL return HTTP 400 with a descriptive error message WHEN a create-location request is received with an SRID that identifies a coordinate reference system from which reprojection to WGS84 is not supported by the System.
19. THE API SHALL return the stored WGS84 coordinates (post-reprojection, rounded to 6 decimal places) in the HTTP 201 response, alongside the original submitted SRID retained as source metadata.

---

### Requirement 4: View a Location

**User Story:** As any User (authenticated or not), I want to view the canonical version of a Location, so that I can see its coordinates and formatted content.

#### Acceptance Criteria

1. WHEN a request is received for a Location that exists, THE API SHALL return HTTP 200 with the Location's identifier, name, coordinates, SRID, Creator's display name, creation timestamp, and ContentSequence.
2. WHEN a request is received for a Location that does not exist, THE API SHALL return HTTP 404.
3. THE API SHALL return only the Canonical Version of a Location in response to a public view request.
4. THE Client SHALL render the Location's ContentSequence in order, displaying Headings, Paragraphs, and Images as distinct visual elements.
5. THE Client SHALL render the Location's coordinates on an interactive Leaflet map.
6. WHEN a Location contains an Image ContentBlock, THE Client SHALL display the image using an HTML `<picture>` element with a `srcset` attribute referencing the ResponsiveVariantSet URLs (400px, 700px, 1000px), consistent with Requirement 27.

---

### Requirement 5: List Locations

**User Story:** As any User, I want to browse a list of all Locations, so that I can discover and navigate to individual Location pages.

#### Acceptance Criteria

1. THE API SHALL provide an endpoint that returns a paginated list of Locations in descending order of creation timestamp.
2. WHEN a list request is received without pagination parameters, THE API SHALL default to returning the first page of 20 Locations.
3. WHEN a list request is received with a page size greater than 100, THE API SHALL return HTTP 400 with a descriptive error message.
4. THE API SHALL return, for each Location in the list: identifier, name, coordinates, SRID, Creator's display name, and creation timestamp — but NOT the full ContentSequence.
5. THE Client SHALL display the list of Locations and provide a navigation link to each Location's detail view.

---

### Requirement 6: Upload an Image

**User Story:** As an authenticated User, I want to upload an image to the System, so that I can include it as an Image ContentBlock when creating or editing a Location.

#### Acceptance Criteria

1. WHEN an authenticated User submits a valid image upload request, THE API SHALL store the image internally and return HTTP 201 with a unique image identifier.
2. WHEN an image upload request is received with a file whose MIME type is not `image/jpeg`, `image/png`, or `image/webp`, THE API SHALL return HTTP 415 with a descriptive error message.
3. WHEN an image upload request is received with a file larger than 10 MB, THE API SHALL return HTTP 413 with a descriptive error message.
4. WHEN an image upload request is received without authentication, THE API SHALL return HTTP 401.
5. THE API SHALL serve uploaded images via a dedicated endpoint using the image identifier returned at upload time.
6. WHEN a request is received for an image identifier that does not exist, THE API SHALL return HTTP 404.
7. THE API SHALL NOT expose the internal storage path or file system structure in any API response.
8. WHEN an authenticated User submits a valid image upload request, THE API SHALL return HTTP 201 with the image identifier, the ThumbnailVariant URL, and the full ResponsiveVariantSet URL set (400px, 700px, 1000px variant URLs), so that the client does not need a second request to obtain variant URLs.
9. WHEN an image upload request is received for a content image (used in a Location ContentSequence), THE API SHALL accept an optional `altText` field (plain text, maximum 500 characters) and store it alongside the image.
10. WHEN an image upload request is received with an `altText` value exceeding 500 characters, THE API SHALL return HTTP 400 with a descriptive error message.
11. THE API SHALL include the `altText` field in all API responses that reference a content image identifier, returning `null` if no alt text was provided.

---

### Requirement 7: Edit a Location — Creator Path

**User Story:** As the Creator of a Location, I want to edit its content and coordinates, so that I can keep the Location accurate and up to date.

#### Acceptance Criteria

1. WHEN the Creator of a Location submits a valid edit request (which MAY include changes to the name, coordinates, SRID, and/or ContentSequence), THE API SHALL immediately replace the Canonical Version with the submitted changes and return HTTP 200.
2. WHEN the Creator submits an edit request with invalid coordinate or ContentSequence data, THE API SHALL return HTTP 400 applying the same validation rules as Requirement 3.
3. WHEN an edit request is received for a Location that does not exist, THE API SHALL return HTTP 404.
4. WHEN an edit request is received without authentication, THE API SHALL return HTTP 401.
5. WHEN the Creator replaces the Canonical Version with an edit, THE API SHALL delete from internal storage any images (and their ThumbnailVariant and ResponsiveVariantSet variants) that were referenced in the previous ContentSequence but are not referenced in the new ContentSequence or any other ContentBlock in the System.
6. THE API SHALL reproject the submitted coordinates from the source CRS identified by the submitted SRID to WGS84 (EPSG:4326) and round to 6 decimal places before persisting the updated Location, consistent with Requirement 3 criteria 11 and 16–17.

---

### Requirement 8: Edit a Location — Non-Creator Path

**User Story:** As an authenticated User who is not the Creator of a Location, I want to propose edits to that Location, so that I can contribute improvements that the Creator can review.

#### Acceptance Criteria

1. WHEN a non-Creator authenticated User submits a valid edit request for a Location, THE API SHALL create a PendingEdit containing the proposed changes and return HTTP 202.
2. THE API SHALL associate the PendingEdit with the submitting User and the target Location.
3. WHEN a non-Creator User submits an edit request for a Location that already has a PendingEdit from that same User, THE API SHALL replace the existing PendingEdit with the new submission and return HTTP 202.
4. THE API SHALL NOT apply the PendingEdit to the Canonical Version until the Creator approves it.
5. WHEN a non-Creator User requests the view of a Location for which they have a PendingEdit, THE API SHALL return the PendingEdit's content rather than the Canonical Version.
6. WHEN a non-Creator User submits an edit request with invalid coordinate or ContentSequence data, THE API SHALL return HTTP 400 applying the same validation rules as Requirement 3.
7. THE API SHALL reproject the submitted coordinates from the source CRS identified by the submitted SRID to WGS84 (EPSG:4326) and round to 6 decimal places before storing the PendingEdit, consistent with Requirement 3 criteria 11 and 16–17.

---

### Requirement 9: Approval Workflow — Creator Review

**User Story:** As the Creator of a Location, I want to see all pending edits and compare them against the current canonical version, so that I can make an informed decision to approve or reject each one.

#### Acceptance Criteria

1. THE API SHALL provide an endpoint that returns all PendingEdits for a given Location, accessible only to the Creator of that Location.
2. WHEN a non-Creator User requests the PendingEdits list for a Location, THE API SHALL return HTTP 403.
3. WHEN an unauthenticated request is received for the PendingEdits list, THE API SHALL return HTTP 401.
4. THE API SHALL return, for each PendingEdit: the submitting User's display name, the submission timestamp, and the full proposed ContentSequence and coordinates.
5. THE Client SHALL present the Creator with a side-by-side or toggled comparison view showing the Canonical Version alongside each PendingEdit.
6. THE Client SHALL provide the Creator with an Approve action and a Reject action for each PendingEdit.

---

### Requirement 10: Approval Workflow — Approve an Edit

**User Story:** As the Creator of a Location, I want to approve a pending edit, so that the contributor's changes become the canonical version visible to all users.

#### Acceptance Criteria

1. WHEN the Creator submits an approve request for a valid PendingEdit, THE API SHALL replace the Canonical Version with the PendingEdit's content and return HTTP 200.
2. WHEN the Creator approves a PendingEdit, THE API SHALL delete the PendingEdit record from the System.
3. WHEN a non-Creator User submits an approve request for a PendingEdit, THE API SHALL return HTTP 403.
4. WHEN an approve request is received for a PendingEdit identifier that does not exist, THE API SHALL return HTTP 404.
5. WHEN an unauthenticated approve request is received, THE API SHALL return HTTP 401.

---

### Requirement 11: Approval Workflow — Reject an Edit

**User Story:** As the Creator of a Location, I want to reject a pending edit, so that unwanted proposed changes are permanently removed from the System.

#### Acceptance Criteria

1. WHEN the Creator submits a reject request for a valid PendingEdit, THE API SHALL permanently delete the PendingEdit and all its associated data and return HTTP 200.
2. WHEN a non-Creator User submits a reject request for a PendingEdit, THE API SHALL return HTTP 403.
3. WHEN a reject request is received for a PendingEdit identifier that does not exist, THE API SHALL return HTTP 404.
4. WHEN an unauthenticated reject request is received, THE API SHALL return HTTP 401.
5. IF a rejected PendingEdit contained Image ContentBlocks whose images are not referenced by any other ContentBlock in the System, THEN THE API SHALL delete those images from internal storage.

---

### Requirement 12: Content Serialisation Round-Trip

**User Story:** As a developer, I want the ContentSequence serialisation to be lossless, so that formatted content is never corrupted when stored and retrieved.

#### Acceptance Criteria

1. THE API SHALL serialise ContentSequence data to and from the database without loss of ordering, type information, or content.
2. FOR ALL valid ContentSequence values, serialising then deserialising SHALL produce a ContentSequence that is structurally and semantically equivalent to the original (round-trip property).
3. WHEN a ContentSequence containing all three ContentBlock types (Heading, Paragraph, Image) is stored and retrieved, THE API SHALL return the blocks in the original order with all fields intact.
4. THE API SHALL reject a ContentSequence that contains a ContentBlock with an unrecognised type and return HTTP 400.

---

### Requirement 13: Spatial Data Integrity

**User Story:** As a developer, I want Location coordinates to be stored and retrieved accurately, so that map rendering and spatial queries produce correct results.

#### Acceptance Criteria

1. THE API SHALL store all Location coordinates using the SQL Server GEOGRAPHY type with SRID 4326 (WGS84), after reprojecting from the source coordinate reference system identified by the submitted SRID.
2. WHEN a Location is retrieved, THE API SHALL return the stored WGS84 latitude and longitude values rounded to exactly 6 decimal places (approximately 11.1 cm ground accuracy), alongside the original submitted SRID retained as source metadata.
3. THE API SHALL validate that the SRID corresponds to a recognised coordinate reference system before persisting a Location; WHEN an unrecognised SRID is submitted, THE API SHALL return HTTP 400.
4. THE Client SHALL project Location coordinates onto the Leaflet map using the SRID associated with the Location, defaulting to WGS84 (EPSG:4326) when no projection transform is required.
5. FOR ALL coordinate pairs submitted to the System, the round-trip property SHALL hold: storing a coordinate and retrieving it SHALL produce WGS84 values equal to the reprojected input rounded to 6 decimal places (property: `round(retrieve(store(reproject(lat, lon, srid))), 6) == round(reproject(lat, lon, srid), 6)` for all valid inputs).
6. THE API SHALL apply CoordinateReprojection to WGS84 followed by 6 decimal place rounding consistently across all write paths: create Location (Requirement 3), edit Location by Creator (Requirement 7), and edit Location by non-Creator PendingEdit (Requirement 8).
7. THE API SHALL retain the original submitted SRID as source metadata on the Location record, so that the client can display or reference the source coordinate reference system; the stored and returned coordinate values SHALL always be in WGS84 regardless of the submitted SRID.
8. THE API SHALL validate that the submitted SRID identifies a coordinate reference system supported for reprojection to WGS84; WHEN an unsupported SRID is submitted, THE API SHALL return HTTP 400 with a descriptive error message.

---

### Requirement 14: Observability

**User Story:** As an operator, I want all Location Management operations to emit structured logs, traces, and metrics, so that I can monitor system health and diagnose issues in production.

#### Acceptance Criteria

1. THE API SHALL emit a structured log entry at `Information` level for each successful create, edit, approve, and reject operation, including the Location identifier and the acting User's identifier (not display name or credentials).
2. THE API SHALL emit a structured log entry at `Warning` level for each rejected authentication attempt, including the username attempted (not the password).
3. THE API SHALL emit a structured log entry at `Error` level for each unhandled exception, including the TraceId and SpanId of the active trace.
4. THE API SHALL produce a distributed trace span for each inbound HTTP request, including HTTP method, route, and response status code as span attributes.
5. THE API SHALL record a histogram metric for Location create and edit operation durations, labelled by operation type and outcome (success or failure).
6. THE API SHALL NOT include passwords, JWT secrets, image binary data, or raw spatial coordinate values in any log entry.

---

### Requirement 15: Security Baseline

**User Story:** As a security engineer, I want the Location Management API to enforce authentication, authorisation, and input validation consistently, so that the system is protected against common web vulnerabilities.

#### Acceptance Criteria

1. WHEN any mutating request (create, edit, approve, reject, upload) is received without a valid JWT, THE API SHALL return HTTP 401.
2. WHEN a User attempts to approve or reject a PendingEdit for a Location they did not create, THE API SHALL return HTTP 403.
3. THE API SHALL validate all coordinate, SRID, ContentBlock, and image inputs at the API boundary before passing them to business logic or the database.
4. THE API SHALL use parameterised queries or EF Core LINQ for all database operations — raw SQL string concatenation with user-supplied values is prohibited.
5. WHEN a ContentBlock of type Heading or Paragraph is submitted, THE API SHALL enforce a maximum text length of 10,000 characters per block; requests exceeding this limit SHALL return HTTP 400.
6. THE API SHALL enforce a maximum image upload rate of 20 images per User per hour; requests exceeding this limit SHALL return HTTP 429.
7. THE API SHALL set the following HTTP response headers on all responses: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`.
8. THE API SHALL store JWT signing keys in environment variables or a secrets manager and MUST NOT hardcode them in source files.

---

### Requirement 16: Create a LocationCollection

**User Story:** As an authenticated User, I want to create a LocationCollection with a name, optional description, optional bounding shape, and optional collection image, so that I can organise related Locations together.

#### Acceptance Criteria

1. WHEN an authenticated User submits a valid create-collection request with a unique name, THE API SHALL persist the LocationCollection and return HTTP 201 with the collection's identifier.
2. THE API SHALL record the authenticated User as the owner of the LocationCollection.
3. THE API SHALL default the CollectionVisibility to private (not visible to other users) unless explicitly set to public.
4. WHEN a create-collection request is received with an empty or whitespace-only name, THE API SHALL return HTTP 400.
5. THE API SHALL enforce a maximum name length of 200 characters and a maximum description length of 2000 characters; requests exceeding these limits SHALL return HTTP 400.
6. WHEN a create-collection request includes a bounding shape, THE API SHALL accept only a reference to an existing NamedShape identifier; if the identifier does not correspond to a known NamedShape, THE API SHALL return HTTP 400 with a descriptive error message.
7. WHEN a create-collection request includes a collection image, THE API SHALL apply the same upload validation rules as Requirement 6 (MIME type, size limit).
8. WHEN a create-collection request is received without authentication, THE API SHALL return HTTP 401.
9. WHEN a create-collection request includes a collection image, THE API SHALL accept an optional `altText` field (plain text, maximum 500 characters) for the collection image and store it alongside the image; THE Client SHALL prompt the User to provide alt text for the collection image before submission.

---

### Requirement 17: View and List LocationCollections

**User Story:** As a User, I want to browse public LocationCollections and view the details of a specific collection, so that I can discover grouped Locations.

#### Acceptance Criteria

1. THE API SHALL provide an endpoint that returns a paginated list of public LocationCollections in descending order of creation timestamp.
2. WHEN a list request is received without pagination parameters, THE API SHALL default to returning the first page of 20 LocationCollections.
3. THE API SHALL return, for each LocationCollection in the list: identifier, name, description, owner's display name, creation timestamp, CollectionVisibility, and a thumbnail image URL (the collection image if one exists, otherwise null) — but NOT the full member list.
4. WHEN a request is received for a specific LocationCollection that is public, THE API SHALL return HTTP 200 with the full collection metadata and the list of member Locations (identifier, name/coordinates of each).
5. WHEN a request is received for a private LocationCollection by its owner, THE API SHALL return HTTP 200 with the full collection data.
6. WHEN a request is received for a private LocationCollection by any other User (authenticated or not), THE API SHALL return HTTP 403.
7. WHEN a request is received for a LocationCollection identifier that does not exist, THE API SHALL return HTTP 404.
8. THE Client SHALL display the list of public LocationCollections and provide a navigation link to each collection's detail view.
9. THE API SHALL provide an authenticated endpoint that returns a combined, paginated list of: all public LocationCollections, plus all private LocationCollections owned by the requesting User — in descending order of creation timestamp.
10. THE API SHALL include a boolean field `isOwner` on each LocationCollection entry in the combined list response, set to `true` when the requesting User is the owner of that collection and `false` otherwise.

---

### Requirement 18: Manage LocationCollection Membership

**User Story:** As the owner of a LocationCollection, I want to add and remove Locations from my collection, so that I can curate which Locations belong to it.

#### Acceptance Criteria

1. WHEN the owner of a LocationCollection submits a request to add a Location to the collection, THE API SHALL create a CollectionMember association and return HTTP 201.
2. WHEN a non-owner authenticated User submits a request to add a Location to a public LocationCollection, THE API SHALL create a pending membership request and return HTTP 202; the Location SHALL NOT be added to the collection until the owner approves it.
3. WHEN the owner approves a pending membership request, THE API SHALL create the CollectionMember association and return HTTP 200.
4. WHEN the owner rejects a pending membership request, THE API SHALL permanently delete the pending request and return HTTP 200.
5. WHEN the owner submits a request to remove a Location from the collection, THE API SHALL delete the CollectionMember association and return HTTP 200.
6. WHEN a non-owner User submits a request to remove a Location from a collection, THE API SHALL return HTTP 403.
7. A Location MAY belong to multiple LocationCollections simultaneously.
8. WHEN a request is received to add a Location that is already a member of the specified collection, THE API SHALL return HTTP 409.
9. WHEN a membership request is received without authentication, THE API SHALL return HTTP 401.

---

### Requirement 19: Edit and Delete a LocationCollection

**User Story:** As the owner of a LocationCollection, I want to edit its metadata and delete it, so that I can keep my collections accurate and remove ones I no longer need.

#### Acceptance Criteria

1. WHEN the owner submits a valid edit request for a LocationCollection, THE API SHALL update the metadata (name, description, collection image, visibility) and return HTTP 200. WHEN the owner updates the bounding shape, THE API SHALL accept only a reference to an existing NamedShape identifier or a null value to remove the bounding shape; if the identifier does not correspond to a known NamedShape, THE API SHALL return HTTP 400.
2. WHEN a non-owner User submits an edit request for a LocationCollection, THE API SHALL return HTTP 403.
3. WHEN the owner submits a delete request for a LocationCollection, THE API SHALL permanently delete the collection and all its CollectionMember associations and return HTTP 200; the member Locations themselves SHALL NOT be deleted.
4. WHEN a non-owner User submits a delete request for a LocationCollection, THE API SHALL return HTTP 403.
5. WHEN an edit or delete request is received without authentication, THE API SHALL return HTTP 401.
6. IF a deleted LocationCollection had a collection image that is not referenced by any other entity in the System, THE API SHALL delete that image from internal storage.

---

### Requirement 20: LocationCollection Map View

**User Story:** As a User, I want to view all Locations in a LocationCollection on an interactive map, so that I can see their geographic distribution at a glance.

#### Acceptance Criteria

1. THE Client SHALL render a Leaflet map on the LocationCollection detail page showing each member Location as a pin at its coordinates.
2. THE Client SHALL display the name of each Location alongside or in the tooltip of its map pin.
3. IF the LocationCollection has a BoundingShape, THE Client SHALL render it as an overlay on the map.
4. THE Client SHALL display a list of all member Locations below the map, with each entry linking to the individual Location detail view.
5. THE Client SHALL fit the map viewport to encompass all member Location pins (and the BoundingShape if present) when the collection page loads.
6. WHEN a LocationCollection has no member Locations, THE Client SHALL display an empty-state message in place of the map and list.

---

### Requirement 21: LocationCollection Observability and Security

**User Story:** As an operator and security engineer, I want LocationCollection operations to be observable and access-controlled, consistent with the rest of the system.

#### Acceptance Criteria

1. THE API SHALL emit structured log entries at `Information` level for create, edit, delete, member-add, member-remove, approve-membership, and reject-membership operations, including the collection identifier and acting User's identifier.
2. THE API SHALL enforce that private LocationCollections are never returned in public list or detail endpoints — this MUST be validated at the service layer, not only at the controller layer.
3. THE API SHALL use parameterised queries or EF Core LINQ for all LocationCollection database operations.
4. WHEN a NamedShape GeoJSON file is uploaded, THE API SHALL validate geometry complexity (maximum 1000 vertices) to guard against geometry bomb attacks; requests exceeding this limit SHALL return HTTP 400.
5. Only Users with the `Admin` UserRole SHALL be permitted to create, rename, or delete NamedShapes; requests from `Standard` Users SHALL return HTTP 403.

---

### Requirement 22: Named Shape Management

**User Story:** As an Admin, I want to upload, name, and manage GeoJSON shapes, so that collection owners can select from a curated list of shapes when defining the boundary of a LocationCollection.

#### Acceptance Criteria

1. WHEN an Admin submits a valid named-shape upload request with a unique name and a `.geojson` file, THE API SHALL parse the file, validate the geometry, store it as a GEOGRAPHY type, and return HTTP 201 with the NamedShape identifier and name.
2. WHEN a named-shape upload request is received with a name that already exists in the system, THE API SHALL return HTTP 409 with a descriptive error message.
3. WHEN a named-shape upload request is received with a file whose content is not valid GeoJSON, THE API SHALL return HTTP 400 with a descriptive error message.
4. WHEN a named-shape upload request is received with a GeoJSON geometry type other than `Polygon` or `MultiPolygon`, THE API SHALL return HTTP 400 with a descriptive error message.
5. WHEN a named-shape upload request is received with a GeoJSON file larger than 5 MB, THE API SHALL return HTTP 413 with a descriptive error message.
6. WHEN a named-shape upload request is received with geometry exceeding 1000 vertices, THE API SHALL return HTTP 400 with a descriptive error message.
7. THE API SHALL provide an endpoint that returns a paginated list of all NamedShapes (identifier and name only), accessible to any authenticated User, so that collection owners can browse available shapes.
8. WHEN an Admin submits a rename request for an existing NamedShape with a new unique name, THE API SHALL update the name and return HTTP 200.
9. WHEN an Admin submits a rename request with a name that already exists for a different NamedShape, THE API SHALL return HTTP 409 with a descriptive error message.
10. WHEN an Admin submits a delete request for a NamedShape that is not currently referenced by any LocationCollection, THE API SHALL permanently delete the NamedShape and return HTTP 200.
11. WHEN an Admin submits a delete request for a NamedShape that is currently referenced by one or more LocationCollections, THE API SHALL return HTTP 409 with a descriptive error message indicating the shape is in use.
12. WHEN a named-shape upload, rename, or delete request is received from a `Standard` User, THE API SHALL return HTTP 403.
13. WHEN a named-shape upload, rename, or delete request is received without authentication, THE API SHALL return HTTP 401.
14. THE API SHALL emit structured log entries at `Information` level for all NamedShape create, rename, and delete operations, including the NamedShape identifier and the acting Admin's User identifier.

---

### Requirement 23: Change Display Name

**User Story:** As an authenticated User, I want to change my display name, so that I can update how I appear to others in the system.

#### Acceptance Criteria

1. WHEN an authenticated User submits a valid change-display-name request with a new display name that is unique across the system (case-insensitive), THE API SHALL update the User's display name and return HTTP 200.
2. WHEN a change-display-name request is received with a display name that already exists for a different User (case-insensitive), THE API SHALL return HTTP 409 with a descriptive error message.
3. WHEN a change-display-name request is received with an empty or whitespace-only display name, THE API SHALL return HTTP 400 with a descriptive error message.
4. WHEN a change-display-name request is received with a display name exceeding 100 characters, THE API SHALL return HTTP 400 with a descriptive error message.
5. WHEN a change-display-name request is received without authentication, THE API SHALL return HTTP 401.
6. THE API SHALL emit a structured log entry at `Information` level when a display name is changed, including the User's identifier (not the old or new display name).
7. THE Client SHALL provide an authenticated User with a UI control to update their display name, showing a clear error message when the chosen name is already taken.

---

### Requirement 24: Homepage

**User Story:** As an authenticated User, I want a homepage that shows all public LocationCollections alongside my own private collections, so that I can quickly find and navigate to collections that are relevant to me.

#### Acceptance Criteria

1. WHEN an unauthenticated User navigates to the homepage, THE Client SHALL redirect them to the login/sign-up page.
2. WHEN an authenticated User navigates to the homepage, THE Client SHALL display a paginated list of LocationCollections combining all public collections and all collections owned by the authenticated User.
3. THE Client SHALL render each LocationCollection in the list as a card containing: a thumbnail image (or a placeholder if no collection image exists), the collection name, and the collection description (truncated if necessary).
4. THE Client SHALL visually differentiate collections owned by the authenticated User from public collections not owned by them, using a distinct icon or badge on the card.
5. THE Client SHALL use the `isOwner` field returned by the API to determine which icon or badge to display — no client-side ownership inference is permitted.
6. THE Client SHALL provide a navigation link on each card that takes the User to the LocationCollection detail view.
7. THE Client SHALL support pagination on the homepage list, allowing the User to load additional collections beyond the first page.
8. WHEN the authenticated User has no owned collections and there are no public collections, THE Client SHALL display an empty-state message with a prompt to create a new LocationCollection.
9. WHEN the authenticated User has the `ShowPublicCollections` preference set to `false`, THE Client SHALL exclude public LocationCollections not owned by the User from the homepage list, showing only the User's own collections.
10. THE Client SHALL derive the homepage list contents from the `ShowPublicCollections` preference stored in the User's profile — this preference MUST be read from the API and MUST NOT be stored only in client-side state.

---

### Requirement 25: User Menu

**User Story:** As an authenticated User, I want a persistent menu in the top-right corner of the screen, so that I can quickly access my profile settings and sign out from any page.

#### Acceptance Criteria

1. THE Client SHALL render a UserMenu component in the top-right corner of the application layout on every page accessible to authenticated Users.
2. THE UserMenu SHALL display the authenticated User's Avatar (or a default placeholder if no Avatar has been uploaded) and their display name.
3. THE UserMenu SHALL provide a navigation link to the UserConfigurationPage.
4. THE UserMenu SHALL provide a sign-out action that clears the User's JWT from client storage and redirects them to the login/sign-up page.
5. WHEN an unauthenticated User accesses any page, THE Client SHALL NOT render the UserMenu.

---

### Requirement 26: User Configuration

**User Story:** As an authenticated User, I want a configuration page where I can update my display name, upload an avatar, and control my homepage preferences, so that I can personalise my experience.

#### Acceptance Criteria

1. THE Client SHALL provide a UserConfigurationPage accessible only to authenticated Users, reachable via the UserMenu.
2. WHEN an unauthenticated User navigates directly to the UserConfigurationPage URL, THE Client SHALL redirect them to the login/sign-up page.
3. THE UserConfigurationPage SHALL display the User's current display name with an inline edit control, applying the same uniqueness and length validation rules as Requirement 23.
4. THE UserConfigurationPage SHALL display the User's current Avatar (or a placeholder) with an option to upload a new Avatar image.
5. WHEN a User uploads an Avatar, THE Client SHALL present a crop tool that constrains the selection to a 1:1 (square) aspect ratio before submitting the cropped image to the API.
6. WHEN an Avatar upload request is received with a file whose MIME type is not `image/jpeg`, `image/png`, or `image/webp`, THE API SHALL return HTTP 415 with a descriptive error message.
7. WHEN an Avatar upload request is received with a file larger than 1 MB, THE API SHALL return HTTP 413 with a descriptive error message.
8. WHEN a valid Avatar upload is received, THE API SHALL store the image internally, associate it with the User's profile, and return HTTP 200.
9. WHEN a User uploads a new Avatar, THE API SHALL replace the previous Avatar; if the previous Avatar image is not referenced by any other entity in the System, THE API SHALL delete it from internal storage.
10. THE UserConfigurationPage SHALL display a toggle control labelled "Show public collections on homepage" reflecting the current value of the User's `ShowPublicCollections` preference.
11. WHEN the User changes the `ShowPublicCollections` toggle, THE Client SHALL immediately persist the new value to the API and update the homepage list behaviour accordingly.
12. WHEN a preference update request is received without authentication, THE API SHALL return HTTP 401.
13. THE API SHALL persist UserProfile preferences (Avatar, ShowPublicCollections) server-side and return them as part of the authenticated User's profile response, so that preferences are consistent across devices and sessions.
14. THE API SHALL emit a structured log entry at `Information` level for Avatar upload and preference change operations, including the User's identifier (not the Avatar binary data or preference values).
15. WHEN a User uploads an Avatar, THE API SHALL accept an optional `altText` field (plain text, maximum 200 characters) for the Avatar image; if provided, THE API SHALL store it and use it when rendering the Avatar in the UI; THE Client SHALL provide an optional alt text input alongside the Avatar upload control.

---

### Requirement 27: Performance

**User Story:** As a User, I want every page to load within 500 milliseconds, so that the application feels responsive and does not impede my workflow.

#### Acceptance Criteria

**API Response Time**

1. THE API SHALL respond to all read endpoints (list, detail, image serve) within 500 ms at the 95th percentile under normal load, measured at the API boundary excluding network transit time.
2. THE API SHALL respond to all mutating endpoints (create, edit, approve, reject, upload) within 500 ms at the 95th percentile under normal load.
3. THE API SHALL record a histogram metric for all endpoint response durations, labelled by route and HTTP method, so that p95 latency can be monitored via the Aspire Dashboard or any OTLP-compatible backend.
4. WHEN an API endpoint exceeds 500 ms at the p95 percentile over a 5-minute window, THE system SHALL emit an alert consistent with the alerting thresholds defined in Requirement 14.

**Client-Side Page Load**

5. THE Client SHALL achieve a Time to Interactive (TTI) of 500 ms or less for all pages under normal network conditions (measured against a simulated broadband connection in Playwright performance tests).
6. THE Client SHALL lazy-load images and non-critical assets so that initial page render is not blocked by off-screen content.
7. THE Client SHALL display a loading skeleton or placeholder for list and detail pages while API data is in flight, so that the page is visually responsive immediately on navigation.

**Server-Side Caching**

8. THE API SHALL cache the results of the public LocationCollection list endpoint and the combined authenticated list endpoint using a ResponseCache with a maximum TTL of 60 seconds; cached responses SHALL be invalidated when a LocationCollection is created, updated, deleted, or its visibility changes.
9. THE API SHALL cache the results of the Location list endpoint using a ResponseCache with a maximum TTL of 60 seconds; cached responses SHALL be invalidated when a Location is created or updated.
10. THE API SHALL cache individual LocationCollection detail responses (public collections only) with a maximum TTL of 60 seconds; cached responses SHALL be invalidated when the collection or its membership changes.
11. THE API SHALL cache individual Location detail responses with a maximum TTL of 60 seconds; cached responses SHALL be invalidated when the Location's canonical version changes.
12. THE API SHALL NOT cache responses that contain user-specific data (private collections, PendingEdits, UserProfile preferences) — these MUST always be served fresh from the database.
13. THE API SHALL use cache keys that include all relevant query parameters (page number, page size, filters) to prevent stale or incorrect data being served from cache.

**Thumbnail Pre-generation**

14. WHEN an image is uploaded (via Requirement 6, Requirement 16 collection image, or Requirement 26 Avatar), THE API SHALL synchronously generate and store a ThumbnailVariant at 200×200 pixels before returning the upload response.
15. THE API SHALL serve ThumbnailVariants via a dedicated thumbnail endpoint distinct from the full-image endpoint, so that list and card views never download full-resolution images.
16. THE API SHALL return the ThumbnailVariant URL alongside the full-image URL in all API responses that include image references (LocationCollection list, Location list, UserProfile).
17. WHEN a ThumbnailVariant cannot be generated (e.g. corrupt image data), THE API SHALL return HTTP 422 with a descriptive error message and SHALL NOT persist the original image.
18. THE Client SHALL use ThumbnailVariant URLs exclusively for all list views, card thumbnails, Avatar display in the UserMenu, and any other context where a small preview is sufficient — full-resolution images SHALL only be loaded on explicit user request (e.g. viewing a Location's ContentSequence).

**Responsive Image Variants**

19. WHEN an image is uploaded via Requirement 6 (content image), THE API SHALL synchronously generate and store a ResponsiveVariantSet — three variants at 400px, 700px, and 1000px widths — cropped or fitted to a 2:3 or 3:2 aspect ratio (matching the dominant orientation of the original image) before returning the upload response.
20. WHEN an image is uploaded via Requirement 16 (LocationCollection collection image), THE API SHALL synchronously generate and store a ResponsiveVariantSet at 400px, 700px, and 1000px widths in a 2:3 or 3:2 aspect ratio before returning the upload response.
21. THE API SHALL serve each variant in the ResponsiveVariantSet via dedicated endpoints keyed by image identifier and width, distinct from the full-image and ThumbnailVariant endpoints.
22. THE API SHALL return the full ResponsiveVariantSet URL set (400px, 700px, 1000px variant URLs) alongside the ThumbnailVariant URL and full-image URL in all API responses that include content image or collection image references.
23. THE API SHALL cache served image variant responses (ThumbnailVariant and all ResponsiveVariantSet variants) with HTTP `Cache-Control: public, max-age=31536000, immutable` headers, since image content is immutable once generated — a new upload always produces a new identifier.
24. WHEN a ResponsiveVariantSet cannot be generated for any variant (e.g. corrupt image data or unsupported dimensions), THE API SHALL return HTTP 422 with a descriptive error message and SHALL NOT persist the original image or any partial variants.
25. THE Client SHALL render all content images (Image ContentBlocks within a Location's ContentSequence) and LocationCollection collection images using an HTML `<picture>` element with a `srcset` attribute referencing the 400px, 700px, and 1000px ResponsiveVariantSet URLs, so that the browser selects the most appropriate variant for the current viewport width.
26. THE Client SHALL specify `sizes` attribute values on `<picture>` elements that accurately reflect the rendered image width at each breakpoint, enabling the browser to select the correct variant without downloading unnecessarily large images.
27. THE Client SHALL NOT use full-resolution original image URLs for any display purpose — full-resolution images are stored for archival purposes only and are not served to the UI.

---

### Requirement 28: Delete a Location

**User Story:** As the Creator of a Location or an Admin, I want to delete a Location, so that outdated or incorrect entries can be permanently removed from the System.

#### Acceptance Criteria

1. WHEN the Creator of a Location submits a delete request, THE API SHALL permanently delete the Location and return HTTP 200.
2. WHEN an Admin submits a delete request for any Location, THE API SHALL permanently delete the Location and return HTTP 200.
3. WHEN a non-Creator, non-Admin authenticated User submits a delete request for a Location, THE API SHALL return HTTP 403.
4. WHEN a delete request is received for a Location that does not exist, THE API SHALL return HTTP 404.
5. WHEN a delete request is received without authentication, THE API SHALL return HTTP 401.
6. WHEN a Location is deleted, THE API SHALL permanently delete all PendingEdits associated with that Location.
7. WHEN a Location is deleted, THE API SHALL remove all CollectionMember associations linking that Location to any LocationCollection.
8. WHEN a Location is deleted, THE API SHALL delete from internal storage all images (and their ThumbnailVariant and ResponsiveVariantSet variants) referenced in the Location's ContentSequence that are not referenced by any other ContentBlock in the System.
9. THE API SHALL emit a structured log entry at `Information` level for each Location deletion, including the Location identifier and the acting User's identifier.

---

### Requirement 29: Admin Role Management

**User Story:** As an Admin, I want to promote Standard users to Admin and demote Admins back to Standard, so that the set of privileged users can be managed over time.

#### Acceptance Criteria

1. THE API SHALL automatically assign the `Admin` UserRole to the first User account created in the System; all subsequent registrations receive the `Standard` UserRole as defined in Requirement 1.
2. WHEN an Admin submits a promote request for a Standard User, THE API SHALL update that User's role to `Admin` and return HTTP 200.
3. WHEN an Admin submits a demote request for an Admin User, and at least one other Admin User exists in the System, THE API SHALL update that User's role to `Standard` and return HTTP 200.
4. WHEN an Admin submits a demote request for an Admin User who is the only remaining Admin in the System, THE API SHALL return HTTP 409 with a descriptive error message indicating that at least one Admin must always exist.
5. WHEN a Standard User submits a promote or demote request, THE API SHALL return HTTP 403.
6. WHEN an unauthenticated request is received for a promote or demote operation, THE API SHALL return HTTP 401.
7. THE API SHALL emit structured log entries at `Information` level for all role promotion and demotion operations, including the target User's identifier and the acting Admin's identifier.
8. THE Client SHALL provide an Admin-only user management UI accessible from the UserMenu, listing all Users with their current role and providing Promote and Demote actions.

---

### Requirement 30: Admin Moderation

**User Story:** As an Admin, I want to be able to delete any Location, LocationCollection, or NamedShape in the System, so that I can moderate content and maintain data quality.

#### Acceptance Criteria

1. WHEN an Admin submits a delete request for any LocationCollection, THE API SHALL permanently delete the collection, all its CollectionMember associations, and return HTTP 200; the member Locations themselves SHALL NOT be deleted.
2. WHEN an Admin deletes a LocationCollection that has a collection image not referenced by any other entity, THE API SHALL delete that image and all its variants from internal storage.
3. WHEN an Admin submits a delete request for any Location, the same rules as Requirement 28 SHALL apply.
4. WHEN an Admin submits a delete request for any NamedShape that is not currently referenced by any LocationCollection, THE API SHALL permanently delete the NamedShape and return HTTP 200.
5. WHEN an Admin submits a delete request for a NamedShape that is currently referenced by one or more LocationCollections, THE API SHALL return HTTP 409 consistent with Requirement 22 criterion 11.
6. THE API SHALL emit structured log entries at `Information` level for all Admin moderation deletions, including the resource type, resource identifier, and the acting Admin's identifier.

---

### Requirement 31: In-App Notifications

**User Story:** As a User, I want to receive in-app notifications for events that require my attention, so that I am aware of pending edits, approvals, and rejections without having to poll each Location manually.

#### Acceptance Criteria

1. WHEN a non-Creator User submits a PendingEdit for a Location, THE API SHALL create an in-app notification for the Creator of that Location indicating that a new edit is awaiting review.
2. WHEN the Creator approves a PendingEdit, THE API SHALL create an in-app notification for the User who submitted the PendingEdit indicating that their edit was approved.
3. WHEN the Creator rejects a PendingEdit, THE API SHALL create an in-app notification for the User who submitted the PendingEdit indicating that their edit was rejected.
4. WHEN the owner of a LocationCollection approves a pending membership request, THE API SHALL create an in-app notification for the User who submitted the request indicating that their Location was added to the collection.
5. WHEN the owner of a LocationCollection rejects a pending membership request, THE API SHALL create an in-app notification for the User who submitted the request indicating that their request was rejected.
6. THE API SHALL provide an authenticated endpoint that returns the authenticated User's unread notifications in descending order of creation timestamp.
7. THE API SHALL provide an authenticated endpoint to mark one or all notifications as read.
8. WHEN a notification is marked as read, THE API SHALL update its status and return HTTP 200.
9. THE Client SHALL display a notification indicator (e.g. a badge or bell icon) in the UserMenu showing the count of unread notifications.
10. THE Client SHALL provide a notification panel accessible from the UserMenu listing all unread notifications with a brief description and a navigation link to the relevant Location or LocationCollection.
11. THE API SHALL NOT persist notification content that includes PII beyond the minimum needed to describe the event (e.g. Location identifier and name, not user credentials).
12. THE API SHALL emit structured log entries at `Information` level when notifications are created, including the notification type and recipient User's identifier.
13. THE API SHALL provide an authenticated endpoint to delete a single notification; WHEN a delete request is received for a notification that belongs to the requesting User, THE API SHALL permanently delete it and return HTTP 200.
14. THE API SHALL automatically delete read notifications that are older than 30 days; this cleanup MAY be performed by a background process and does not need to be synchronous.
15. WHEN a delete request is received for a notification that does not belong to the requesting User, THE API SHALL return HTTP 403.

---

### Requirement 32: Login and Sign-Up UI

**User Story:** As a visitor, I want a login and sign-up page, so that I can register a new account or authenticate with an existing one before accessing the application.

#### Acceptance Criteria

1. THE Client SHALL provide a login/sign-up page accessible to unauthenticated Users at a well-known route (e.g. `/login`).
2. THE login/sign-up page SHALL present both a login form (username and password) and a registration form (username, display name, email address, and password) — either as tabs, separate sections, or a toggle.
3. WHEN a User submits the login form with valid credentials, THE Client SHALL store the returned JWT securely in client storage and redirect the User to the homepage.
4. WHEN a User submits the login form with invalid credentials, THE Client SHALL display the generic error message returned by the API without revealing whether the username or password was incorrect.
5. WHEN a User submits the registration form with valid data, THE Client SHALL store the returned JWT and redirect the User to the homepage.
6. WHEN a User submits the registration form with a duplicate username or display name, THE Client SHALL display a clear error message identifying which field is already taken.
7. WHEN a User submits the registration form with a duplicate email address, THE Client SHALL display a clear error message indicating the email address is already registered.
8. WHEN a User submits the registration form with a password that does not meet complexity rules, THE Client SHALL display inline validation feedback before the form is submitted.
9. THE Client SHALL provide a "Forgot password?" link on the login form that navigates to the password reset flow defined in Requirement 34.
10. WHEN an authenticated User navigates to the login/sign-up page, THE Client SHALL redirect them to the homepage.

---

### Requirement 33: Change Password

**User Story:** As an authenticated User, I want to change my password, so that I can keep my account secure.

#### Acceptance Criteria

1. WHEN an authenticated User submits a valid change-password request with their correct current password and a new password that meets the complexity rules, THE API SHALL update the Password Hash and return HTTP 200.
2. WHEN a change-password request is received with an incorrect current password, THE API SHALL return HTTP 401 with a descriptive error message.
3. WHEN a change-password request is received with a new password that does not meet the complexity rules defined in Requirement 1 (minimum 8 characters, at least one uppercase, one lowercase, one digit), THE API SHALL return HTTP 400 with a descriptive error message.
4. WHEN a change-password request is received without authentication, THE API SHALL return HTTP 401.
5. THE API SHALL NOT log the current or new password at any log level.
6. THE API SHALL emit a structured log entry at `Information` level when a password is changed successfully, including the User's identifier only.
7. THE UserConfigurationPage SHALL provide a change-password form requiring the current password and the new password (with confirmation), accessible to authenticated Users.

---

### Requirement 34: Forgotten Password

**User Story:** As a User who has forgotten their password, I want to request a password reset by email, so that I can regain access to my account.

#### Acceptance Criteria

1. THE API SHALL provide an unauthenticated endpoint that accepts a username and, if the username exists, sends a password reset email to the email address associated with that account.
2. WHEN a forgotten-password request is received for a username that does not exist, THE API SHALL return HTTP 200 with a generic response — the same response as for a valid username — to prevent username enumeration.
3. THE password reset email SHALL contain a single-use reset token with an expiry of no more than 1 hour from the time of issuance.
4. WHEN a User submits a valid, unexpired reset token with a new password that meets the complexity rules, THE API SHALL update the Password Hash, invalidate the token, and return HTTP 200.
5. WHEN a User submits an expired or already-used reset token, THE API SHALL return HTTP 400 with a descriptive error message.
6. WHEN a User submits a new password via the reset token that does not meet the complexity rules, THE API SHALL return HTTP 400 with a descriptive error message.
7. THE API SHALL NOT log the reset token value at any log level.
8. THE API SHALL emit a structured log entry at `Information` level when a password reset is completed, including the User's identifier only.
9. THE Client SHALL provide a password reset page (linked from the login form's "Forgot password?" link) where the User can enter their username to request a reset email.
10. THE Client SHALL provide a password reset confirmation page where the User can enter their new password after following the link in the reset email.
11. THE User record MUST include an email address field; Requirement 1 SHALL be updated to require a valid email address at registration.

---

### Requirement 35: Email Service Configuration

**User Story:** As an operator, I want the email service used for password reset emails to be configured via environment variables, so that the same application binary can be deployed to any environment without code changes.

#### Acceptance Criteria

1. THE API SHALL send password reset emails (Requirement 34) via an SMTP server whose connection details (host, port, username, password, sender address) are provided exclusively through environment variables or a secrets manager — these values MUST NOT be hardcoded in source files or committed to version control.
2. WHEN the SMTP configuration environment variables are absent or malformed at application startup, THE API SHALL fail to start and emit a `Critical` log entry describing the missing configuration.
3. WHEN an attempt to send a password reset email fails due to an SMTP error, THE API SHALL log the failure at `Error` level (without logging the SMTP credentials or the reset token) and return HTTP 500 to the caller.
4. THE API SHALL NOT expose SMTP credentials, sender addresses, or any email service configuration in any API response or log entry.
5. THE API SHALL support configuring a maximum email send rate to prevent abuse; the default limit SHALL be 5 password reset emails per username per hour; requests exceeding this limit SHALL return HTTP 429.

---

### Requirement 36: Authentication Rate Limiting

**User Story:** As a security engineer, I want login and password reset endpoints to enforce rate limits, so that brute-force and credential-stuffing attacks are mitigated.

#### Acceptance Criteria

1. THE API SHALL enforce a maximum of 10 failed login attempts per username per 15-minute window; after this threshold is exceeded, THE API SHALL return HTTP 429 for subsequent login attempts for that username within the window.
2. THE API SHALL enforce a maximum of 5 password reset requests per username per hour (consistent with Requirement 35 criterion 5); requests exceeding this limit SHALL return HTTP 429.
3. THE API SHALL enforce a maximum of 20 login attempts per IP address per minute across all usernames; requests exceeding this limit SHALL return HTTP 429.
4. WHEN a rate limit is triggered on the login endpoint, THE API SHALL emit a structured log entry at `Warning` level including the username attempted and the rate limit type (per-username or per-IP), but NOT the submitted password.
5. THE API SHALL return a `Retry-After` header on all HTTP 429 responses indicating the number of seconds until the rate limit window resets.
6. Rate limit counters SHALL be stored server-side (not in the JWT or client storage) and SHALL survive application restarts when a distributed cache is configured.

---

### Requirement 37: Heading Levels

**User Story:** As a User creating or editing a Location, I want to use different heading levels in my formatted content, so that I can structure the document with a clear visual hierarchy.

#### Acceptance Criteria

1. THE `Heading` ContentBlock SHALL support a `level` field with valid values of `1`, `2`, and `3`, corresponding to H1, H2, and H3 heading elements respectively.
2. WHEN a create or edit request is received with a Heading ContentBlock that does not include a `level` field, THE API SHALL default the level to `2`.
3. WHEN a create or edit request is received with a Heading ContentBlock whose `level` value is not `1`, `2`, or `3`, THE API SHALL return HTTP 400 with a descriptive error message.
4. THE Client SHALL render Heading ContentBlocks using the corresponding HTML heading element (`<h1>`, `<h2>`, or `<h3>`) so that the visual hierarchy is preserved.
5. THE API SHALL include the `level` field in all ContentSequence responses that contain Heading ContentBlocks.
6. FOR ALL valid Heading ContentBlocks, serialising then deserialising SHALL preserve the `level` field without modification (round-trip property, consistent with Requirement 12).

---

### Requirement 38: Accessibility

**User Story:** As a User with accessibility needs, I want the application to meet WCAG 2.1 AA standards, so that I can use all features regardless of disability or assistive technology.

#### Acceptance Criteria

1. THE Client SHALL meet WCAG 2.1 Level AA conformance for all pages and interactive components.
2. All interactive controls (buttons, links, form inputs, toggles) SHALL have accessible names and roles that are correctly announced by screen readers.
3. All images displayed in the UI SHALL have meaningful `alt` text; decorative images SHALL use `alt=""` to be ignored by screen readers.
4. THE Client SHALL ensure sufficient colour contrast ratios for all text and interactive elements as defined by WCAG 2.1 criterion 1.4.3 (minimum 4.5:1 for normal text, 3:1 for large text).
5. THE Client SHALL ensure all functionality is operable via keyboard alone, with a visible focus indicator on all interactive elements.
6. THE Leaflet map component SHALL provide a keyboard-accessible alternative for Users who cannot use a pointer device — at minimum, a text list of all Locations with their names and coordinates SHALL be available as a non-map fallback.
7. Form validation error messages SHALL be programmatically associated with their respective input fields using `aria-describedby` or equivalent, so that screen readers announce errors when the field receives focus.
8. THE Client SHALL use semantic HTML elements (`<nav>`, `<main>`, `<header>`, `<section>`, `<article>`) to provide document structure that assistive technologies can navigate.
9. Playwright accessibility tests SHALL be included for all pages, using `@axe-core/playwright` or equivalent, and SHALL pass with zero critical or serious violations before a feature is considered complete.

---

### Requirement 39: Audit Log

**User Story:** As an Admin, I want a filterable audit log of all significant system actions, so that I can investigate security incidents, track content changes, and maintain accountability across the system.

#### Acceptance Criteria

**Audit Event Recording**

1. THE API SHALL record an AuditEvent for every mutating operation in the System, including: create Location, edit Location (Creator path), create PendingEdit (non-Creator path), approve PendingEdit, reject PendingEdit, delete Location, create LocationCollection, edit LocationCollection, delete LocationCollection, add CollectionMember, remove CollectionMember, approve membership request, reject membership request, upload image, upload NamedShape, rename NamedShape, delete NamedShape, user registration, change display name, change password, password reset, Avatar upload, and preference change.
2. THE API SHALL record an AuditEvent for every failed authentication attempt (login failure), including the username attempted (not the password) and the source IP address.
3. THE API SHALL record an AuditEvent for every Admin action, including: promote User to Admin, demote Admin to Standard, delete any Location (Admin moderation), delete any LocationCollection (Admin moderation), and delete any NamedShape (Admin moderation).
4. Each AuditEvent SHALL capture the following fields: event type, acting User identifier (or `anonymous` for unauthenticated attempts), target resource type and identifier (where applicable), UTC timestamp, outcome (`success` or `failure`), and source IP address.
5. THE API SHALL NOT include passwords, JWT secrets, email addresses, image binary data, or coordinate values in any AuditEvent record.
6. AuditEvents SHALL be written to the AuditLog as an append-only operation — existing AuditEvents MUST NOT be modified or deleted by any application code path.

**Retention**

7. THE API SHALL retain AuditEvents for a minimum of 1 year from the time of recording; AuditEvents older than 1 year MAY be automatically purged by a background process.
8. THE API SHALL emit a structured log entry at `Information` level when the automatic purge process runs, including the number of records purged and the cutoff timestamp.

**Audit Log API**

9. THE API SHALL provide an authenticated, Admin-only endpoint that returns a paginated list of AuditEvents in descending order of timestamp.
10. THE API SHALL support the following filter parameters on the audit log endpoint: event type, acting User identifier, target resource type, target resource identifier, outcome (`success` or `failure`), and a UTC date range (from/to).
11. WHEN a non-Admin authenticated User requests the audit log endpoint, THE API SHALL return HTTP 403.
12. WHEN an unauthenticated request is received for the audit log endpoint, THE API SHALL return HTTP 401.
13. WHEN a list request is received without pagination parameters, THE API SHALL default to returning the first page of 50 AuditEvents.
14. WHEN a list request is received with a page size greater than 200, THE API SHALL return HTTP 400 with a descriptive error message.

**Audit Log UI**

15. THE Client SHALL provide an Admin-only audit log page accessible from the UserMenu, displaying AuditEvents in a paginated table.
16. THE audit log page SHALL provide filter controls for: event type (dropdown), acting User (text search), target resource type (dropdown), outcome (dropdown), and date range (date pickers for from/to).
17. THE Client SHALL display for each AuditEvent: the UTC timestamp, event type, acting User identifier, target resource type and identifier, outcome, and source IP address.
18. WHEN an unauthenticated or non-Admin User navigates directly to the audit log page URL, THE Client SHALL redirect them to the login/sign-up page or display an access-denied message respectively.

**Security**

19. THE API SHALL enforce that the audit log endpoint is accessible only to Users with the `Admin` UserRole — this MUST be validated at the service layer, not only at the controller layer.
20. THE API SHALL use parameterised queries or EF Core LINQ for all audit log database operations — raw SQL string concatenation with user-supplied filter values is prohibited.
21. THE API SHALL emit a structured log entry at `Warning` level WHEN a non-Admin User attempts to access the audit log endpoint, including the requesting User's identifier.

---

### Requirement 40: Data Export

**User Story:** As an Admin, I want to export all system data as an encrypted backup archive, so that I can restore the system or migrate data if needed.

#### Acceptance Criteria

**Export Scope**

1. THE API SHALL provide an Admin-only export endpoint that produces a BackupArchive containing all of the following: all Locations (including their ContentSequences, coordinates, and source SRID metadata), all LocationCollections (including membership associations and NamedShape references), all NamedShapes (including their stored geometry), all uploaded images and their variants (ThumbnailVariant and ResponsiveVariantSet), and all AuditEvents.
2. THE API SHALL NOT include User account data (usernames, display names, email addresses, password hashes, or UserProfile preferences) in the BackupArchive — user data is excluded from export for privacy reasons.
3. THE API SHALL record the export timestamp and the acting Admin's identifier in the BackupArchive metadata, so that the archive can be identified and audited.

**Encryption**

4. THE BackupArchive SHALL be encrypted using AES-256 before being returned to the client; the encryption key SHALL be provided by the Admin as a request parameter and SHALL NOT be stored by the System.
5. THE API SHALL NOT log or persist the encryption key provided by the Admin at any point.
6. WHEN an export request is received without an encryption key, THE API SHALL return HTTP 400 with a descriptive error message.
7. THE API SHALL enforce a minimum encryption key length of 32 characters; requests with a shorter key SHALL return HTTP 400 with a descriptive error message.

**Access Control**

8. WHEN a non-Admin authenticated User requests the export endpoint, THE API SHALL return HTTP 403.
9. WHEN an unauthenticated request is received for the export endpoint, THE API SHALL return HTTP 401.
10. THE API SHALL record an AuditEvent for every export operation, including the acting Admin's identifier and the export timestamp (but NOT the encryption key).

**Response**

11. THE API SHALL return the BackupArchive as a binary file download with the `Content-Type: application/zip` header and a filename including the UTC export timestamp (e.g. `backup-2026-05-02T12-00-00Z.zip`).
12. WHEN the export operation fails for any reason, THE API SHALL return HTTP 500 with a descriptive error message and SHALL NOT return a partial archive.

---

### Requirement 41: Data Import

**User Story:** As an Admin, I want to import a previously exported backup archive, so that I can restore lost data or migrate content into the system.

#### Acceptance Criteria

**Import Behaviour**

1. THE API SHALL provide an Admin-only import endpoint that accepts a BackupArchive file and a decryption key, decrypts the archive, and imports the contained data into the System.
2. THE import operation SHALL be additive — it SHALL add imported records alongside existing data and SHALL NOT delete or overwrite any existing records.
3. WHEN an imported Location, LocationCollection, or NamedShape has an identifier that already exists in the System, THE API SHALL assign a new identifier to the imported record rather than overwriting the existing one.

**User Assignment**

4. WHEN importing data, THE API SHALL create a dedicated ImportUser account (if one does not already exist) with a system-generated display name indicating it is an import account (e.g. `Import User [timestamp]`).
5. All imported Locations, LocationCollections, and other user-owned resources SHALL be assigned to the ImportUser as their Creator or owner, since original User accounts are not included in the BackupArchive.
6. THE ImportUser account SHALL be a `Standard` UserRole account and SHALL NOT be assignable as an Admin via the import process.

**Decryption**

7. THE API SHALL decrypt the BackupArchive using the decryption key provided in the import request; WHEN the decryption key is incorrect or the archive is corrupt, THE API SHALL return HTTP 400 with a descriptive error message.
8. THE API SHALL NOT log or persist the decryption key provided by the Admin at any point.
9. WHEN an import request is received without a decryption key, THE API SHALL return HTTP 400 with a descriptive error message.

**Validation**

10. WHEN the decrypted archive does not conform to the expected BackupArchive schema, THE API SHALL return HTTP 422 with a descriptive error message and SHALL NOT import any data.
11. THE API SHALL validate all imported coordinate data against the same rules as Requirement 3 (coordinate ranges, SRID validation, reprojection to WGS84); WHEN any coordinate fails validation, THE API SHALL skip that record, log a warning, and continue importing remaining records.
12. THE API SHALL validate all imported image data against the same MIME type and size rules as Requirement 6; WHEN any image fails validation, THE API SHALL skip that image and its associated ContentBlock references, log a warning, and continue importing remaining records.

**Access Control and Observability**

13. WHEN a non-Admin authenticated User requests the import endpoint, THE API SHALL return HTTP 403.
14. WHEN an unauthenticated request is received for the import endpoint, THE API SHALL return HTTP 401.
15. THE API SHALL record an AuditEvent for every import operation, including the acting Admin's identifier, the import timestamp, the number of records imported, and the number of records skipped (but NOT the decryption key).
16. THE API SHALL return a summary response on successful import including: total records imported by type (Locations, LocationCollections, NamedShapes, images), total records skipped, and the identifier of the ImportUser created or used.

---

### Requirement 42: Ownership Reassignment

**User Story:** As an Admin, I want to reassign ownership of any Location, LocationCollection, or other user-owned resource from one User to another, so that I can correct import assignments or transfer content when a user account changes.

#### Acceptance Criteria

1. THE API SHALL provide an Admin-only ownership reassignment endpoint that accepts a resource type, resource identifier, and target User identifier, and transfers ownership of the specified resource to the target User.
2. THE API SHALL support reassignment for the following resource types: Location (Creator), LocationCollection (owner).
3. WHEN the Admin submits a valid reassignment request, THE API SHALL update the Creator or owner of the specified resource to the target User and return HTTP 200.
4. WHEN a reassignment request is received for a resource identifier that does not exist, THE API SHALL return HTTP 404.
5. WHEN a reassignment request is received with a target User identifier that does not exist, THE API SHALL return HTTP 404 with a descriptive error message identifying the target User as not found.
6. WHEN a non-Admin authenticated User submits a reassignment request, THE API SHALL return HTTP 403.
7. WHEN an unauthenticated reassignment request is received, THE API SHALL return HTTP 401.
8. THE API SHALL record an AuditEvent for every ownership reassignment, including the resource type, resource identifier, previous owner identifier, new owner identifier, and the acting Admin's identifier.
9. THE Client SHALL provide an Admin-accessible ownership reassignment UI, reachable from the resource detail page (Location or LocationCollection), allowing an Admin to select a target User from a searchable list and confirm the reassignment.
