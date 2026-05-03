import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ContentSequenceEditor, ContentBlock } from './ContentSequenceEditor';

// Mock ImageUploader
vi.mock('./ImageUploader', () => ({
  ImageUploader: ({ onUploadComplete }: { onUploadComplete: (id: string) => void }) => (
    <button onClick={() => onUploadComplete('test-image-id')}>Upload Image</button>
  ),
}));

describe('ContentSequenceEditor', () => {
  const mockOnBlocksChange = vi.fn();

  beforeEach(() => {
    mockOnBlocksChange.mockClear();
  });

  it('renders toolbar with add buttons', () => {
    render(<ContentSequenceEditor blocks={[]} onBlocksChange={mockOnBlocksChange} />);
    expect(screen.getByText('Add Heading')).toBeInTheDocument();
    expect(screen.getByText('Add Paragraph')).toBeInTheDocument();
    expect(screen.getByText('Add Image')).toBeInTheDocument();
  });

  it('shows empty state when no blocks', () => {
    render(<ContentSequenceEditor blocks={[]} onBlocksChange={mockOnBlocksChange} />);
    expect(screen.getByText('No content blocks. Add one to get started.')).toBeInTheDocument();
  });

  it('adds heading block when Add Heading clicked', () => {
    render(<ContentSequenceEditor blocks={[]} onBlocksChange={mockOnBlocksChange} />);
    fireEvent.click(screen.getByText('Add Heading'));
    expect(mockOnBlocksChange).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({
          type: 'Heading',
          text: 'New Heading',
          level: 2,
        }),
      ])
    );
  });

  it('adds paragraph block when Add Paragraph clicked', () => {
    render(<ContentSequenceEditor blocks={[]} onBlocksChange={mockOnBlocksChange} />);
    fireEvent.click(screen.getByText('Add Paragraph'));
    expect(mockOnBlocksChange).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({
          type: 'Paragraph',
          text: 'New Paragraph',
        }),
      ])
    );
  });

  it('adds image block when Add Image clicked', () => {
    render(<ContentSequenceEditor blocks={[]} onBlocksChange={mockOnBlocksChange} />);
    fireEvent.click(screen.getByText('Add Image'));
    expect(mockOnBlocksChange).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({
          type: 'Image',
        }),
      ])
    );
  });

  it('renders existing blocks', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'Title', level: 1 },
      { type: 'Paragraph', text: 'Body' },
    ];
    render(<ContentSequenceEditor blocks={blocks} onBlocksChange={mockOnBlocksChange} />);
    expect(screen.getByText('Heading')).toBeInTheDocument();
    expect(screen.getByText('Paragraph')).toBeInTheDocument();
  });

  it('removes block when delete button clicked', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'Title', level: 1 },
    ];
    render(<ContentSequenceEditor blocks={blocks} onBlocksChange={mockOnBlocksChange} />);
    const deleteButtons = screen.getAllByLabelText('Delete block');
    fireEvent.click(deleteButtons[0]);
    expect(mockOnBlocksChange).toHaveBeenCalledWith([]);
  });

  it('moves block up when up button clicked', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'First', level: 1 },
      { type: 'Paragraph', text: 'Second' },
    ];
    render(<ContentSequenceEditor blocks={blocks} onBlocksChange={mockOnBlocksChange} />);
    const upButtons = screen.getAllByLabelText('Move up');
    fireEvent.click(upButtons[1]); // Click up on second block
    expect(mockOnBlocksChange).toHaveBeenCalledWith([
      { type: 'Paragraph', text: 'Second' },
      { type: 'Heading', text: 'First', level: 1 },
    ]);
  });

  it('moves block down when down button clicked', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'First', level: 1 },
      { type: 'Paragraph', text: 'Second' },
    ];
    render(<ContentSequenceEditor blocks={blocks} onBlocksChange={mockOnBlocksChange} />);
    const downButtons = screen.getAllByLabelText('Move down');
    fireEvent.click(downButtons[0]); // Click down on first block
    expect(mockOnBlocksChange).toHaveBeenCalledWith([
      { type: 'Paragraph', text: 'Second' },
      { type: 'Heading', text: 'First', level: 1 },
    ]);
  });

  it('disables move up button on first block', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'First', level: 1 },
    ];
    render(<ContentSequenceEditor blocks={blocks} onBlocksChange={mockOnBlocksChange} />);
    const upButton = screen.getByLabelText('Move up');
    expect(upButton).toBeDisabled();
  });

  it('disables move down button on last block', () => {
    const blocks: ContentBlock[] = [
      { type: 'Heading', text: 'First', level: 1 },
    ];
    render(<ContentSequenceEditor blocks={blocks} onBlocksChange={mockOnBlocksChange} />);
    const downButton = screen.getByLabelText('Move down');
    expect(downButton).toBeDisabled();
  });

  it('applies custom className', () => {
    const { container } = render(
      <ContentSequenceEditor blocks={[]} onBlocksChange={mockOnBlocksChange} className="custom" />
    );
    const editor = container.querySelector('.content-sequence-editor');
    expect(editor).toHaveClass('custom');
  });
});
