import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ThumbnailImage } from './ThumbnailImage';

describe('ThumbnailImage', () => {
  it('renders image when thumbnailUrl is provided', () => {
    render(
      <ThumbnailImage
        thumbnailUrl="http://example.com/thumb.jpg"
        altText="Test thumbnail"
      />
    );

    const img = screen.getByAltText('Test thumbnail');
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute('src', 'http://example.com/thumb.jpg');
  });

  it('renders placeholder when thumbnailUrl is missing', () => {
    render(<ThumbnailImage altText="Test" />);

    const placeholder = screen.getByRole('img', { name: 'No image' });
    expect(placeholder).toBeInTheDocument();
  });

  it('uses custom fallback text', () => {
    render(
      <ThumbnailImage
        altText="Test"
        fallbackText="Image not available"
      />
    );

    const placeholder = screen.getByRole('img', { name: 'Image not available' });
    expect(placeholder).toBeInTheDocument();
  });

  it('applies custom className', () => {
    const { container } = render(
      <ThumbnailImage
        thumbnailUrl="http://example.com/thumb.jpg"
        className="custom-class"
      />
    );

    const img = container.querySelector('.thumbnail-image.custom-class');
    expect(img).toBeInTheDocument();
  });

  it('sets correct width and height', () => {
    render(
      <ThumbnailImage
        thumbnailUrl="http://example.com/thumb.jpg"
        width={150}
        height={150}
      />
    );

    const img = screen.getByAltText('Image thumbnail') as HTMLImageElement;
    expect(img.style.width).toBe('150px');
    expect(img.style.height).toBe('150px');
  });

  it('uses lazy loading', () => {
    render(
      <ThumbnailImage
        thumbnailUrl="http://example.com/thumb.jpg"
      />
    );

    const img = screen.getByAltText('Image thumbnail');
    expect(img).toHaveAttribute('loading', 'lazy');
  });

  it('applies object-fit cover style', () => {
    render(
      <ThumbnailImage
        thumbnailUrl="http://example.com/thumb.jpg"
      />
    );

    const img = screen.getByAltText('Image thumbnail') as HTMLImageElement;
    expect(img.style.objectFit).toBe('cover');
  });

  it('renders placeholder with correct dimensions', () => {
    const { container } = render(
      <ThumbnailImage
        width={200}
        height={200}
      />
    );

    const placeholder = container.querySelector('.thumbnail-placeholder') as HTMLDivElement;
    expect(placeholder.style.width).toBe('200px');
    expect(placeholder.style.height).toBe('200px');
  });
});
