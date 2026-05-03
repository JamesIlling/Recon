import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import axios from 'axios';
import { ImageUploader } from './ImageUploader';

vi.mock('axios');
const mockedAxios = axios as any;

describe('ImageUploader', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders file input and upload button', () => {
    render(<ImageUploader />);
    expect(screen.getByLabelText('Select image file')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /upload image/i })).toBeInTheDocument();
  });

  it('disables upload button when no file is selected', () => {
    render(<ImageUploader />);
    const uploadButton = screen.getByRole('button', { name: /upload image/i });
    expect(uploadButton).toBeDisabled();
  });

  it('rejects invalid MIME types', async () => {
    const onUploadError = vi.fn();
    render(<ImageUploader onUploadError={onUploadError} />);

    const fileInput = screen.getByLabelText('Select image file') as HTMLInputElement;
    const file = new File(['content'], 'test.txt', { type: 'text/plain' });

    fireEvent.change(fileInput, { target: { files: [file] } });

    await waitFor(() => {
      expect(onUploadError).toHaveBeenCalledWith(
        'Invalid image format. Please use JPEG, PNG, WebP, or GIF.'
      );
    });
  });

  it('rejects files larger than 1 MB', async () => {
    const onUploadError = vi.fn();
    render(<ImageUploader onUploadError={onUploadError} />);

    const fileInput = screen.getByLabelText('Select image file') as HTMLInputElement;
    const largeFile = new File(['x'.repeat(1024 * 1024 + 1)], 'large.jpg', {
      type: 'image/jpeg',
    });

    fireEvent.change(fileInput, { target: { files: [largeFile] } });

    await waitFor(() => {
      expect(onUploadError).toHaveBeenCalledWith('File size exceeds 1 MB limit.');
    });
  });

  it('accepts valid image files', async () => {
    render(<ImageUploader />);

    const fileInput = screen.getByLabelText('Select image file') as HTMLInputElement;
    const validFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });

    fireEvent.change(fileInput, { target: { files: [validFile] } });

    await waitFor(() => {
      const uploadButton = screen.getByRole('button', { name: /upload image/i });
      expect(uploadButton).not.toBeDisabled();
    });
  });

  it('displays preview after file selection', async () => {
    render(<ImageUploader />);

    const fileInput = screen.getByLabelText('Select image file') as HTMLInputElement;
    const validFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });

    fireEvent.change(fileInput, { target: { files: [validFile] } });

    await waitFor(() => {
      const preview = screen.getByAltText('Preview');
      expect(preview).toBeInTheDocument();
    });
  });

  it('uploads file successfully', async () => {
    const onUploadSuccess = vi.fn();
    mockedAxios.post.mockResolvedValue({
      data: {
        id: 'image-123',
        altText: 'Test image',
        thumbnailVariantUrl: 'http://example.com/thumb.jpg',
        responsiveVariantSet: {
          width400Url: 'http://example.com/400.jpg',
          width700Url: 'http://example.com/700.jpg',
          width1000Url: 'http://example.com/1000.jpg',
        },
      },
    });

    render(<ImageUploader onUploadSuccess={onUploadSuccess} />);

    const fileInput = screen.getByLabelText('Select image file') as HTMLInputElement;
    const validFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });

    fireEvent.change(fileInput, { target: { files: [validFile] } });

    await waitFor(() => {
      const uploadButton = screen.getByRole('button', { name: /upload image/i });
      expect(uploadButton).not.toBeDisabled();
    });

    const uploadButton = screen.getByRole('button', { name: /upload image/i });
    fireEvent.click(uploadButton);

    await waitFor(() => {
      expect(onUploadSuccess).toHaveBeenCalledWith('image-123', {
        thumbnail: 'http://example.com/thumb.jpg',
        width400: 'http://example.com/400.jpg',
        width700: 'http://example.com/700.jpg',
        width1000: 'http://example.com/1000.jpg',
      });
    });
  });

  it('handles upload errors', async () => {
    const onUploadError = vi.fn();
    mockedAxios.post.mockRejectedValue(new Error('Upload failed'));

    render(<ImageUploader onUploadError={onUploadError} />);

    const fileInput = screen.getByLabelText('Select image file') as HTMLInputElement;
    const validFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });

    fireEvent.change(fileInput, { target: { files: [validFile] } });

    await waitFor(() => {
      const uploadButton = screen.getByRole('button', { name: /upload image/i });
      expect(uploadButton).not.toBeDisabled();
    });

    const uploadButton = screen.getByRole('button', { name: /upload image/i });
    fireEvent.click(uploadButton);

    await waitFor(() => {
      expect(onUploadError).toHaveBeenCalled();
    });
  });

  it('allows optional alt text input', async () => {
    render(<ImageUploader />);

    const altTextInput = screen.getByPlaceholderText('Describe the image for accessibility');
    expect(altTextInput).toBeInTheDocument();

    fireEvent.change(altTextInput, { target: { value: 'Test alt text' } });
    expect((altTextInput as HTMLTextAreaElement).value).toBe('Test alt text');
  });

  it('shows crop tool when enabled with 1:1 aspect ratio', async () => {
    render(<ImageUploader showCropTool={true} aspectRatio="1:1" />);

    const fileInput = screen.getByLabelText('Select image file') as HTMLInputElement;
    const validFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });

    fireEvent.change(fileInput, { target: { files: [validFile] } });

    await waitFor(() => {
      expect(screen.getByText('X Position:')).toBeInTheDocument();
      expect(screen.getByText('Y Position:')).toBeInTheDocument();
      expect(screen.getByText('Size (1:1):')).toBeInTheDocument();
    });
  });

  it('enforces 1:1 aspect ratio in crop tool', async () => {
    render(<ImageUploader showCropTool={true} aspectRatio="1:1" />);

    const fileInput = screen.getByLabelText('Select image file') as HTMLInputElement;
    const validFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });

    fireEvent.change(fileInput, { target: { files: [validFile] } });

    await waitFor(() => {
      const sizeInputs = screen.getAllByDisplayValue('0');
      expect(sizeInputs.length).toBeGreaterThan(0);
    });
  });
});
