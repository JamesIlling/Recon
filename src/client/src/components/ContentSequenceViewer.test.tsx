import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ContentSequenceViewer, ContentBlock } from './ContentSequenceViewer';

// Mock ResponsiveImage
vi.mock('./ResponsiveImage', () => ({
  ResponsiveImage: ({ alt }: { alt: string }) => <div data-testid="responsive-image">{alt}</div>,
}));

describe('ContentSequenceViewer', () => {
  it('renders empty state when no blocks', () => {
    render(<ContentSequenceViewer blocks={[]} />);
    expect(screen.getByText('No content available.')).toBeInTheDocument();
  });

  it('renders heading block with correct level', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'Test Heading', level: 1 },
    ];
    render(<ContentSequenceViewer blocks={blocks} />);
    const heading = screen.getByRole('heading', { level: 1 });
    expect(heading).toHaveTextContent('Test Heading');
  });

  it('defaults heading level to 2 when not specified', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'Default Heading' },
    ];
    render(<ContentSequenceViewer blocks={blocks} />);
    const heading = screen.getByRole('heading', { level: 2 });
    expect(heading).toHaveTextContent('Default Heading');
  });

  it('renders paragraph block', () => {
    const blocks: ContentBlock[] = [
      { type: 'Paragraph', text: 'Test paragraph content' },
    ];
    render(<ContentSequenceViewer blocks={blocks} />);
    expect(screen.getByText('Test paragraph content')).toBeInTheDocument();
  });

  it('renders image block with imageId', () => {
    const blocks: ContentBlock[] = [
      { type: 'Image', imageId: 'test-image-id' },
    ];
    render(<ContentSequenceViewer blocks={blocks} />);
    expect(screen.getByTestId('responsive-image')).toBeInTheDocument();
  });

  it('renders image placeholder when imageId is missing', () => {
    const blocks: ContentBlock[] = [
      { type: 'Image' },
    ];
    render(<ContentSequenceViewer blocks={blocks} />);
    expect(screen.getByText('Image not available')).toBeInTheDocument();
  });

  it('renders multiple blocks in order', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'Title', level: 1 },
      { type: 'Paragraph', text: 'Introduction' },
      { type: 'Paragraph', text: 'Body' },
    ];
    render(<ContentSequenceViewer blocks={blocks} />);

    const headings = screen.getAllByRole('heading');
    expect(headings[0]).toHaveTextContent('Title');
    expect(screen.getByText('Introduction')).toBeInTheDocument();
    expect(screen.getByText('Body')).toBeInTheDocument();
  });

  it('applies custom className', () => {
    const { container } = render(
      <ContentSequenceViewer blocks={[]} className="custom-class" />
    );
    const viewer = container.querySelector('.content-sequence-viewer');
    expect(viewer).toHaveClass('custom-class');
  });

  it('clamps heading level to valid range', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'High Level', level: 10 },
    ];
    render(<ContentSequenceViewer blocks={blocks} />);
    const heading = screen.getByRole('heading', { level: 6 });
    expect(heading).toHaveTextContent('High Level');
  });
});
