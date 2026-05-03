import React from 'react';
import { ThumbnailImage } from './ThumbnailImage';

/**
 * Collection card data for display.
 */
export interface CollectionCardData {
  id: string;
  name: string;
  description?: string;
  thumbnailUrl?: string;
  isOwner: boolean;
  visibility: 'Private' | 'Public';
}

/**
 * Props for the CollectionCard component.
 */
export interface CollectionCardProps {
  collection: CollectionCardData;
  onClick?: (collectionId: string) => void;
  className?: string;
}

/**
 * CollectionCard component displays a collection as a card with thumbnail, name, description, and ownership badge.
 * Implements task 11.1:
 * - Display thumbnail, name, description (truncated to 100 chars)
 * - Owner badge vs public badge
 * - Use ThumbnailImage component for thumbnail rendering
 * - Include proper TypeScript types and accessibility attributes
 */
export const CollectionCard: React.FC<CollectionCardProps> = ({
  collection,
  onClick,
  className = '',
}) => {
  const truncatedDescription = collection.description
    ? collection.description.length > 100
      ? `${collection.description.substring(0, 100)}...`
      : collection.description
    : '';

  const handleClick = () => {
    if (onClick) {
      onClick(collection.id);
    }
  };

  const badgeLabel = collection.isOwner ? 'Your Collection' : 'Public Collection';
  const badgeClass = collection.isOwner ? 'badge-owner' : 'badge-public';

  return (
    <div
      className={`collection-card ${className}`}
      role="article"
      aria-label={`${collection.name} collection`}
    >
      <div className="collection-card-thumbnail">
        <ThumbnailImage
          thumbnailUrl={collection.thumbnailUrl}
          altText={`${collection.name} collection thumbnail`}
          fallbackText="No image"
          width={200}
          height={200}
        />
      </div>

      <div className="collection-card-content">
        <div className="collection-card-header">
          <h3 className="collection-card-title">{collection.name}</h3>
          <span className={`collection-badge ${badgeClass}`} aria-label={badgeLabel}>
            {collection.isOwner ? '👤' : '🌐'}
          </span>
        </div>

        {truncatedDescription && (
          <p className="collection-card-description">{truncatedDescription}</p>
        )}

        {onClick && (
          <button
            onClick={handleClick}
            className="collection-card-link"
            aria-label={`View ${collection.name} collection`}
          >
            View Collection →
          </button>
        )}
      </div>
    </div>
  );
};

export default CollectionCard;
