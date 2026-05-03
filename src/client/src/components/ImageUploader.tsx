import React, { useRef, useState } from 'react';
import axios from 'axios';

// Constants
const VALID_IMAGE_MIMES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
const MAX_FILE_SIZE_BYTES = 1024 * 1024; // 1 MB
const CROP_ASPECT_RATIO = '1:1';

interface ImageUploaderProps {
  onUploadSuccess?: (imageId: string, variantUrls: Record<string, string>) => void;
  onUploadError?: (error: string) => void;
  showCropTool?: boolean;
  aspectRatio?: 'free' | '1:1' | '2:3' | '3:2';
}

interface UploadResponse {
  id: string;
  altText: string;
  thumbnailVariantUrl: string;
  responsiveVariantSet: {
    width400Url: string;
    width700Url: string;
    width1000Url: string;
  };
}

interface VariantUrls {
  thumbnail: string;
  width400: string;
  width700: string;
  width1000: string;
}

/**
 * ImageUploader component with file picker, optional altText input, and upload to POST /api/images.
 * Supports optional 1:1 crop tool for avatar uploads.
 */
export const ImageUploader: React.FC<ImageUploaderProps> = ({
  onUploadSuccess,
  onUploadError,
  showCropTool = false,
  aspectRatio = 'free',
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [altText, setAltText] = useState('');
  const [isUploading, setIsUploading] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [cropMode, setCropMode] = useState(false);
  const [cropData, setCropData] = useState({ x: 0, y: 0, size: 0 });

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!VALID_IMAGE_MIMES.includes(file.type)) {
      onUploadError?.('Invalid image format. Please use JPEG, PNG, WebP, or GIF.');
      return;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      onUploadError?.('File size exceeds 1 MB limit.');
      return;
    }

    setSelectedFile(file);

    const reader = new FileReader();
    reader.onload = (e) => {
      setPreviewUrl(e.target?.result as string);
      if (showCropTool && aspectRatio === CROP_ASPECT_RATIO) {
        setCropMode(true);
      }
    };
    reader.readAsDataURL(file);
  };

  const handleCropChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setCropData((prev) => ({
      ...prev,
      [name]: parseInt(value, 10),
    }));
  };

  const applyCrop = async (): Promise<void> => {
    if (!previewUrl || !canvasRef.current) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const img = new Image();
    img.onload = () => {
      const size = Math.min(cropData.size, img.width - cropData.x, img.height - cropData.y);
      canvas.width = size;
      canvas.height = size;

      ctx.drawImage(
        img,
        cropData.x,
        cropData.y,
        size,
        size,
        0,
        0,
        size,
        size
      );

      canvas.toBlob((blob: Blob | null) => {
        if (blob) {
          const croppedFile = new File([blob], selectedFile?.name || 'cropped.jpg', {
            type: 'image/jpeg',
          });
          setSelectedFile(croppedFile);
          setCropMode(false);
        }
      }, 'image/jpeg');
    };
    img.src = previewUrl;
  };

  const buildFormData = (): FormData => {
    const formData = new FormData();
    formData.append('file', selectedFile!);
    if (altText) {
      formData.append('altText', altText);
    }
    return formData;
  };

  const extractVariantUrls = (response: UploadResponse): VariantUrls => ({
    thumbnail: response.data.thumbnailVariantUrl,
    width400: response.data.responsiveVariantSet.width400Url,
    width700: response.data.responsiveVariantSet.width700Url,
    width1000: response.data.responsiveVariantSet.width1000Url,
  });

  const resetUploadState = (): void => {
    setSelectedFile(null);
    setAltText('');
    setPreviewUrl(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleUpload = async (): Promise<void> => {
    if (!selectedFile) {
      onUploadError?.('Please select an image.');
      return;
    }

    setIsUploading(true);
    try {
      const formData = buildFormData();
      const response = await axios.post<UploadResponse>('/api/images', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      const variantUrls = extractVariantUrls(response);
      onUploadSuccess?.(response.data.id, variantUrls);
      resetUploadState();
    } catch (error) {
      const message = axios.isAxiosError(error)
        ? error.response?.data?.error || error.message
        : 'Upload failed';
      onUploadError?.(message);
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <div className="image-uploader">
      <div className="upload-section">
        <input
          ref={fileInputRef}
          type="file"
          accept="image/*"
          onChange={handleFileSelect}
          disabled={isUploading}
          aria-label="Select image file"
        />

        {previewUrl && !cropMode && (
          <div className="preview">
            <img src={previewUrl} alt="Preview" />
          </div>
        )}

        {cropMode && previewUrl && (
          <div className="crop-section">
            <img src={previewUrl} alt="Crop preview" id="crop-image" />
            <div className="crop-controls">
              <label>
                X Position:
                <input
                  type="number"
                  name="x"
                  value={cropData.x}
                  onChange={handleCropChange}
                  min="0"
                />
              </label>
              <label>
                Y Position:
                <input
                  type="number"
                  name="y"
                  value={cropData.y}
                  onChange={handleCropChange}
                  min="0"
                />
              </label>
              <label>
                Size (1:1):
                <input
                  type="number"
                  name="size"
                  value={cropData.size}
                  onChange={handleCropChange}
                  min="1"
                />
              </label>
              <button onClick={applyCrop} disabled={isUploading}>
                Apply Crop
              </button>
            </div>
            <canvas ref={canvasRef} style={{ display: 'none' }} />
          </div>
        )}

        <div className="alt-text-section">
          <label htmlFor="alt-text">
            Image Description (optional):
            <textarea
              id="alt-text"
              value={altText}
              onChange={(e) => setAltText(e.target.value)}
              placeholder="Describe the image for accessibility"
              disabled={isUploading}
            />
          </label>
        </div>

        <button
          onClick={handleUpload}
          disabled={!selectedFile || isUploading}
          aria-busy={isUploading}
        >
          {isUploading ? 'Uploading...' : 'Upload Image'}
        </button>
      </div>
    </div>
  );
};

export default ImageUploader;
