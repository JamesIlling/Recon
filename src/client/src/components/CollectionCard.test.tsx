import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CollectionCard, CollectionCardData } from './CollectionCard';

describe('CollectionCard', () => {
  const mockCollection: CollectionCardData = {
    id: '1',
    name: 'Test Collection',
    description: 'This is a test collection with a description',
    thumbnailUrl: 'https://example.com/thumb.jpg',
    isOwner: true,
    visibility: 'Private',
  };

  it('renders collection name', () => {
    render(<CollectionCard collection={mockCollection} />);
    expect(screen.getByText('Test Collection')).toBeInTheDocument();
  });

  it('renders truncated description when longer than 100 characters', () => {
    const longDescription = 'a'.repeat(150);
    const collection: CollectionCardData = {
      ...mockCollection,
      description: longDescription,
    };

    render(<CollectionCard collection={collection} />);
    const description = screen.getByText(/^a+\.\.\./);
    expect(description.textContent).toHaveLength(103); // 100 chars + '...'
  });

  it('renders full description when 100 characters or less', () => {
    const collection: CollectionCardData = {
      ...mockCollection,
      description: 'Short description',
    };

    render(<CollectionCard collection={collection} />);
    expect(screen.getByText('Short description')).toBeInTheDocument();
  });

  it('does not render description when not provided', () => {
    const collection: CollectionCardData = {
      ...mockCollection,
      description: undefined,
    };

    render(<CollectionCard collection={collection} />);
    expect(screen.queryByText(/description/i)).not.toBeInTheDocument();
  });

  it('displays owner badge when isOwner is true', () => {
    const collection: CollectionCardData = {
      ...mockCollection,
      isOwner: true,
    };

    render(<CollectionCard collection={collection} />);
    const badge = screen.getByLabelText('Your Collection');
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveClass('badge-owner');
  });

  it('displays public badge when isOwner is false', () => {
    const collection: CollectionCardData = {
      ...mockCollection,
      isOwner: false,
    };

    render(<CollectionCard collection={collection} />);
    const badge = screen.getByLabelText('Public Collection');
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveClass('badge-public');
  });

  it('calls onClick handler when view button is clicked', () => {
    const handleClick = vi.fn();

    render(<CollectionCard collection={mockCollection} onClick={handleClick} />);

    const viewButton = screen.getByRole('button', { name: /view collection/i });
    viewButton.click();

    expect(handleClick).toHaveBeenCalledWith(mockCollection.id);
  });

  it('does not render view button when onClick is not provided', () => {
    render(<CollectionCard collection={mockCollection} />);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('renders thumbnail image with correct alt text', () => {
    render(<CollectionCard collection={mockCollection} />);
    const image = screen.getByAltText('Test Collection collection thumbnail');
    expect(image).toBeInTheDocument();
  });

  it('applies custom className', () => {
    const { container } = render(
      <CollectionCard collection={mockCollection} className="custom-class" />
    );
    const card = container.querySelector('.collection-card');
    expect(card).toHaveClass('custom-class');
  });

  it('has proper accessibility attributes', () => {
    render(<CollectionCard collection={mockCollection} />);
    const card = screen.getByRole('article');
    expect(card).toHaveAttribute('aria-label', 'Test Collection collection');
  });

  it('renders with empty description string', () => {
    const collection: CollectionCardData = {
      ...mockCollection,
      description: '',
    };

    render(<CollectionCard collection={collection} />);
    expect(screen.queryByText(/description/i)).not.toBeInTheDocument();
  });

  it('handles description with exactly 100 characters', () => {
    const description = 'a'.repeat(100);
    const collection: CollectionCardData = {
      ...mockCollection,
      description,
    };

    render(<CollectionCard collection={collection} />);
    expect(screen.getByText(description)).toBeInTheDocument();
    expect(screen.queryByText(/\.\.\./)).not.toBeInTheDocument();
  });

  it('handles description with 101 characters', () => {
    const description = 'a'.repeat(101);
    const collection: CollectionCardData = {
      ...mockCollection,
      description,
    };

    render(<CollectionCard collection={collection} />);
    const truncated = screen.getByText(/^a+\.\.\./);
    expect(truncated.textContent).toHaveLength(103); // 100 chars + '...'
  });
});
