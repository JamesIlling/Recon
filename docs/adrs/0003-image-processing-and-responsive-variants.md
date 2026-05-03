# ADR-0003: Image Processing and Responsive Variants Strategy

- **Status**: Accepted
- **Date**: 2026-05-03
- **Deciders**: Project Team

## Context

The application requires image upload, processing, and serving capabilities with support for multiple responsive variants. Users need to upload images for locations and avatars, and the system must serve optimized variants for different screen sizes and use cases.

## Decision

Implement a multi-variant image processing strategy:

1. **Backend Processing** (via `IImageProcessingService`):
   - Validate MIME type (JPEG, PNG, WebP, GIF) and file size (1 MB max)
   - Generate three responsive variants: 400px, 700px, 1000px width
   - Generate one thumbnail variant: 200x200px
   - Support both 2:3 and 3:2 aspect ratios for responsive variants
   - Store all variants atomically (all-or-nothing persistence)

2. **Frontend Rendering** (via React components):
   - Use HTML5 `<picture>` element with `<source>` tags for responsive images
   - Implement `srcset` with correct `sizes` attributes for mobile/tablet/desktop
   - Provide `ThumbnailImage` component for list/card views
   - Support lazy loading on all images

3. **Storage**:
   - Store images on local filesystem at configurable `IMAGES_STORAGE_PATH`
   - Organize by image ID: `{IMAGES_STORAGE_PATH}/{imageId}/original.jpg`, `{IMAGES_STORAGE_PATH}/{imageId}/thumb.jpg`, etc.

## Rationale

- **Multi-variant approach**: Reduces bandwidth and improves performance on mobile devices by serving appropriately-sized images.
- **Atomic persistence**: Prevents partial uploads (e.g., thumbnail generated but responsive variants fail).
- **HTML5 `<picture>` element**: Native browser support for responsive images without JavaScript; better accessibility and SEO.
- **Lazy loading**: Defers image loading until they enter the viewport, improving initial page load performance.
- **Local filesystem storage**: Simplifies deployment and avoids external service dependencies; can be replaced with cloud storage (S3, Azure Blob) later without code changes.

## Consequences

- Image processing is CPU-intensive; consider rate limiting (20 uploads/user/hour).
- Filesystem storage requires backup and disaster recovery planning.
- Variant generation adds latency to upload responses; consider async processing for future optimization.
- All image URLs must be validated and sanitized before serving to prevent directory traversal attacks.
- Orphaned images (not referenced by any Location or User) must be cleaned up periodically.

## Alternatives Considered

- **Single image, client-side resizing**: Reduces server load but increases client-side complexity and bandwidth for high-DPI devices.
- **Cloud storage (S3/Azure Blob)**: Better scalability and reliability, but adds external dependency and cost.
- **CDN-based image optimization (Cloudinary, Imgix)**: Offloads processing, but adds external dependency and cost.

