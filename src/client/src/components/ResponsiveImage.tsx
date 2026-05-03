import React from 'react';

interface ResponsiveImageProps {
  width400Url: string;
  width700Url: string;
  width1000Url: string;
  altText: string;
  className?: string;
  aspectRatio?: '2:3' | '3:2' | '1:1';
}

/**
 * ResponsiveImage component using <picture> / srcset rendering for content images.
 * Uses ResponsiveVariantSet URLs with correct sizes attributes.
 * Supports multiple aspect ratios: 2:3, 3:2, or 1:1.
 */
export const ResponsiveImage: React.FC<ResponsiveImageProps> = ({
  width400Url,
  width700Url,
  width1000Url,
  altText,
  className = '',
  aspectRatio = '3:2',
}) => {
  // Calculate aspect ratio padding for responsive container
  const aspectRatioValue = aspectRatio === '2:3' ? 150 : aspectRatio === '1:1' ? 100 : 66.67;

  return (
    <div
      className={`responsive-image-container ${className}`}
      style={{
        position: 'relative',
        width: '100%',
        paddingBottom: `${aspectRatioValue}%`,
        overflow: 'hidden',
        borderRadius: '4px',
      }}
    >
      <picture
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
        }}
      >
        {/* Mobile: up to 400px */}
        <source
          media="(max-width: 640px)"
          srcSet={width400Url}
          sizes="100vw"
        />
        {/* Tablet: 400px to 700px */}
        <source
          media="(max-width: 1024px)"
          srcSet={width700Url}
          sizes="(max-width: 1024px) 100vw, 700px"
        />
        {/* Desktop: 700px and above */}
        <source
          media="(min-width: 1025px)"
          srcSet={width1000Url}
          sizes="(min-width: 1025px) 1000px, 100vw"
        />
        {/* Fallback for browsers that don't support picture */}
        <img
          src={width1000Url}
          alt={altText}
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            objectFit: 'cover',
          }}
          loading="lazy"
        />
      </picture>
    </div>
  );
};

export default ResponsiveImage;
