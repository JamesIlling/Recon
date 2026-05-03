import React from 'react';
import { ResponsiveImage } from './ResponsiveImage';

/**
 * Content block types.
 */
export type ContentBlockType = 'Heading' | 'Paragraph' | 'Image';

/**
 * A single content block in a sequence.
 */
export interface ContentBlock {
  type: ContentBlockType;
  text?: string;
  level?: number;
  imageId?: string;
}

/**
 * Props for ContentSequenceViewer.
 */
export interface ContentSequenceViewerProps {
  blocks: ContentBlock[];
  className?: string;
}

/**
 * ContentSequenceViewer renders a sequence of content blocks (headings, paragraphs, images) in order.
 * Implements task 8.6.
 */
export function ContentSequenceViewer({
  blocks,
  className = '',
}: ContentSequenceViewerProps): JSX.Element {
  const renderBlock = (block: ContentBlock, index: number) => {
    switch (block.type) {
      case 'Heading': {
        const level = block.level || 2;
        const HeadingTag = `h${Math.min(Math.max(level, 1), 6)}` as keyof JSX.IntrinsicElements;
        return (
          <HeadingTag key={index} className="content-heading">
            {block.text}
          </HeadingTag>
        );
      }

      case 'Paragraph':
        return (
          <p key={index} className="content-paragraph">
            {block.text}
          </p>
        );

      case 'Image':
        return (
          <div key={index} className="content-image-container">
            {block.imageId ? (
              <ResponsiveImage
                imageId={block.imageId}
                alt={`Content image ${index + 1}`}
                className="content-image"
              />
            ) : (
              <div className="content-image-placeholder">Image not available</div>
            )}
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className={`content-sequence-viewer ${className}`}>
      {blocks.length > 0 ? (
        blocks.map((block, index) => renderBlock(block, index))
      ) : (
        <p className="empty-content">No content available.</p>
      )}
    </div>
  );
}
