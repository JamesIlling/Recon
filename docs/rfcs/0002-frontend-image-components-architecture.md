# RFC-0002: Frontend Image Components Architecture

- **Status**: Accepted
- **Date**: 2026-05-03
- **Author(s)**: Project Team

## Summary

Define the architecture and component structure for handling image uploads, processing, and responsive rendering on the frontend. This RFC establishes patterns for image handling that will be reused across the application (avatars, location content, collection thumbnails).

## Motivation

Image handling is a cross-cutting concern that appears in multiple features:
- User avatars (1:1 crop, thumbnail only)
- Location content images (responsive variants, multiple aspect ratios)
- Collection thumbnails (thumbnail variant)

A well-designed component architecture ensures consistency, reusability, and maintainability across these use cases.

## Proposal

### Component Structure

#### 1. `ImageUploader` Component
**Purpose**: Handle file selection, validation, optional cropping, and upload.

**Props**:
- `onUploadSuccess(imageId, variantUrls)`: Callback on successful upload
- `onUploadError(error)`: Callback on error
- `showCropTool?: boolean`: Enable/disable crop tool
- `aspectRatio?: '1:1' | '2:3' | '3:2' | 'free'`: Crop aspect ratio constraint

**Features**:
- File input with MIME type validation (JPEG, PNG, WebP, GIF)
- File size validation (1 MB max)
- Optional alt text input for accessibility
- Optional 1:1 crop tool for avatars
- Image preview before upload
- Upload to `POST /api/images` with FormData
- Error handling and loading states

**Usage**:
```tsx
<ImageUploader
  showCropTool={true}
  aspectRatio="1:1"
  onUploadSuccess={(id, urls) => setAvatarUrl(urls.thumbnail)}
  onUploadError={(err) => setError(err)}
/>
```

#### 2. `ResponsiveImage` Component
**Purpose**: Render images with responsive variants using HTML5 `<picture>` element.

**Props**:
- `width400Url: string`: 400px variant URL
- `width700Url: string`: 700px variant URL
- `width1000Url: string`: 1000px variant URL
- `altText: string`: Accessibility alt text
- `aspectRatio?: '2:3' | '3:2' | '1:1'`: Aspect ratio for padding
- `className?: string`: Custom CSS class

**Features**:
- HTML5 `<picture>` element with multiple `<source>` tags
- Responsive `srcset` with correct `sizes` attributes
- Mobile (max-width: 640px) → 400px variant
- Tablet (max-width: 1024px) → 700px variant
- Desktop (min-width: 1025px) → 1000px variant
- Lazy loading for performance
- Fallback `<img>` for older browsers
- Aspect ratio padding for responsive containers

**Usage**:
```tsx
<ResponsiveImage
  width400Url={imageUrls.width400}
  width700Url={imageUrls.width700}
  width1000Url={imageUrls.width1000}
  altText="Location photo"
  aspectRatio="3:2"
/>
```

#### 3. `ThumbnailImage` Component
**Purpose**: Display thumbnail variant for list/card views with placeholder fallback.

**Props**:
- `thumbnailUrl?: string`: Thumbnail variant URL
- `altText?: string`: Accessibility alt text
- `fallbackText?: string`: Placeholder text when image missing
- `width?: number`: Image width in pixels (default: 200)
- `height?: number`: Image height in pixels (default: 200)
- `className?: string`: Custom CSS class

**Features**:
- Displays thumbnail image if URL provided
- Shows placeholder div if URL missing
- Lazy loading
- Object-fit cover for consistent aspect ratio
- Customizable dimensions and styling

**Usage**:
```tsx
<ThumbnailImage
  thumbnailUrl={location.thumbnailUrl}
  altText={location.name}
  width={150}
  height={150}
/>
```

### Data Flow

```
User selects file
    ↓
ImageUploader validates (MIME, size)
    ↓
User optionally crops (1:1 constraint)
    ↓
User enters alt text
    ↓
Upload to POST /api/images
    ↓
Backend processes variants
    ↓
Response: { id, thumbnailUrl, responsiveVariantSet }
    ↓
Frontend stores URLs in state/context
    ↓
Render with ResponsiveImage or ThumbnailImage
```

### Accessibility Considerations

- All images have meaningful `alt` text
- `ImageUploader` prompts for alt text input
- Lazy loading uses `loading="lazy"` attribute
- Crop tool uses semantic HTML with proper labels
- Placeholder divs use `role="img"` and `aria-label`

### Testing Strategy

- **Unit tests**: Component rendering, prop validation, event handling
- **Integration tests**: Upload flow, error handling, variant URL handling
- **E2E tests**: Full user journey (select → crop → upload → display)

## Alternatives Considered

- **Single component for all image handling**: Would be too complex and inflexible.
- **Image processing on frontend**: Reduces server load but increases client complexity and bandwidth for high-DPI devices.
- **Third-party image library (react-image-crop, react-dropzone)**: Adds dependencies; custom implementation is simpler for our use case.

## Consequences

- Components are reusable across avatars, location images, and collection thumbnails.
- Frontend and backend are decoupled; backend can change variant generation without frontend changes.
- Lazy loading improves initial page load performance.
- Responsive images reduce bandwidth on mobile devices.
- Alt text input improves accessibility but adds UX complexity.

## Implementation Notes

- Components use TypeScript for type safety.
- Axios is used for HTTP requests (already in dependencies).
- Vitest + React Testing Library for unit tests.
- All components follow React best practices (hooks, memoization where appropriate).

