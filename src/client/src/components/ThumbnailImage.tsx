import React from 'react';

interface ThumbnailImageProps {
  thumbnailUrl?: string;
  altText?: string;
  fallbackText?: string;
  className?: string;
  width?: number;
  height?: number;
}

/**
 * ThumbnailImage component using ThumbnailVariant URL for list/card views.
 * Displays a placeholder if the image URL is missing.
 */
export const ThumbnailImage: React.FC<ThumbnailImageProps> = ({
  thumbnailUrl,
  altText = 'Image thumbnail',
  fallbackText = 'No image',
  className = '',
  width = 200,
  height = 200,
}) => {
  if (!thumbnailUrl) {
    return (
      <div
        className={`thumbnail-placeholder ${className}`}
        style={{
          width: `${width}px`,
          height: `${height}px`,
          backgroundColor: '#f0f0f0',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          borderRadius: '4px',
          border: '1px solid #ddd',
        }}
        role="img"
        aria-label={fallbackText}
      >
        <span style={{ color: '#999', fontSize: '14px' }}>{fallbackText}</span>
      </div>
    );
  }

  return (
    <img
      src={thumbnailUrl}
      alt={altText}
      className={`thumbnail-image ${className}`}
      style={{
        width: `${width}px`,
        height: `${height}px`,
        objectFit: 'cover',
        borderRadius: '4px',
      }}
      loading="lazy"
    />
  );
};

export default ThumbnailImage;
