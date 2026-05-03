import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ResponsiveImage } from './ResponsiveImage';

describe('ResponsiveImage', () => {
  const mockUrls = {
    width400: 'http://example.com/400.jpg',
    width700: 'http://example.com/700.jpg',
    width1000: 'http://example.com/1000.jpg',
  };

  it('renders picture element with all sources', () => {
    const { container } = render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
      />
    );

    const picture = container.querySelector('picture');
    expect(picture).toBeInTheDocument();

    const sources = container.querySelectorAll('source');
    expect(sources.length).toBe(3);
  });

  it('renders fallback img with correct src', () => {
    render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
      />
    );

    const img = screen.getByAltText('Test image');
    expect(img).toHaveAttribute('src', mockUrls.width1000);
  });

  it('sets correct media queries for responsive sources', () => {
    const { container } = render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
      />
    );

    const sources = container.querySelectorAll('source');
    expect(sources[0]).toHaveAttribute('media', '(max-width: 640px)');
    expect(sources[1]).toHaveAttribute('media', '(max-width: 1024px)');
    expect(sources[2]).toHaveAttribute('media', '(min-width: 1025px)');
  });

  it('sets correct srcSet for each source', () => {
    const { container } = render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
      />
    );

    const sources = container.querySelectorAll('source');
    expect(sources[0]).toHaveAttribute('srcSet', mockUrls.width400);
    expect(sources[1]).toHaveAttribute('srcSet', mockUrls.width700);
    expect(sources[2]).toHaveAttribute('srcSet', mockUrls.width1000);
  });

  it('sets correct sizes attributes', () => {
    const { container } = render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
      />
    );

    const sources = container.querySelectorAll('source');
    expect(sources[0]).toHaveAttribute('sizes', '100vw');
    expect(sources[1]).toHaveAttribute('sizes', '(max-width: 1024px) 100vw, 700px');
    expect(sources[2]).toHaveAttribute('sizes', '(min-width: 1025px) 1000px, 100vw');
  });

  it('applies correct aspect ratio padding for 3:2', () => {
    const { container } = render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
        aspectRatio="3:2"
      />
    );

    const containerDiv = container.querySelector('.responsive-image-container') as HTMLDivElement;
    expect(containerDiv.style.paddingBottom).toBe('66.67%');
  });

  it('applies correct aspect ratio padding for 2:3', () => {
    const { container } = render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
        aspectRatio="2:3"
      />
    );

    const containerDiv = container.querySelector('.responsive-image-container') as HTMLDivElement;
    expect(containerDiv.style.paddingBottom).toBe('150%');
  });

  it('applies correct aspect ratio padding for 1:1', () => {
    const { container } = render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
        aspectRatio="1:1"
      />
    );

    const containerDiv = container.querySelector('.responsive-image-container') as HTMLDivElement;
    expect(containerDiv.style.paddingBottom).toBe('100%');
  });

  it('applies custom className', () => {
    const { container } = render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
        className="custom-class"
      />
    );

    const containerDiv = container.querySelector('.responsive-image-container.custom-class');
    expect(containerDiv).toBeInTheDocument();
  });

  it('uses lazy loading on fallback image', () => {
    render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
      />
    );

    const img = screen.getByAltText('Test image');
    expect(img).toHaveAttribute('loading', 'lazy');
  });

  it('applies object-fit cover to fallback image', () => {
    render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
      />
    );

    const img = screen.getByAltText('Test image') as HTMLImageElement;
    expect(img.style.objectFit).toBe('cover');
  });

  it('renders container with correct positioning styles', () => {
    const { container } = render(
      <ResponsiveImage
        width400Url={mockUrls.width400}
        width700Url={mockUrls.width700}
        width1000Url={mockUrls.width1000}
        altText="Test image"
      />
    );

    const containerDiv = container.querySelector('.responsive-image-container') as HTMLDivElement;
    expect(containerDiv.style.position).toBe('relative');
    expect(containerDiv.style.width).toBe('100%');
    expect(containerDiv.style.overflow).toBe('hidden');
  });
});
