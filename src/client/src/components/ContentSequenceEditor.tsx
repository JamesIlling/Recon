import React, { useState } from 'react';
import { ContentBlock } from './ContentSequenceViewer';
import { ImageUploader } from './ImageUploader';

/**
 * Props for ContentSequenceEditor.
 */
export interface ContentSequenceEditorProps {
  blocks: ContentBlock[];
  onBlocksChange: (blocks: ContentBlock[]) => void;
  className?: string;
}

/**
 * ContentSequenceEditor allows users to add, remove, and reorder content blocks.
 * Supports heading level selection and inline image upload.
 * Implements task 8.7.
 */
export function ContentSequenceEditor({
  blocks,
  onBlocksChange,
  className = '',
}: ContentSequenceEditorProps): JSX.Element {
  const [showImageUploader, setShowImageUploader] = useState(false);
  const [imageUploaderIndex, setImageUploaderIndex] = useState<number | null>(null);

  const addBlock = (type: 'Heading' | 'Paragraph' | 'Image') => {
    const newBlock: ContentBlock = {
      type,
      text: type === 'Heading' ? 'New Heading' : type === 'Paragraph' ? 'New Paragraph' : undefined,
      level: type === 'Heading' ? 2 : undefined,
    };
    onBlocksChange([...blocks, newBlock]);
  };

  const removeBlock = (index: number) => {
    onBlocksChange(blocks.filter((_, i) => i !== index));
  };

  const updateBlock = (index: number, updates: Partial<ContentBlock>) => {
    const updated = [...blocks];
    updated[index] = { ...updated[index], ...updates };
    onBlocksChange(updated);
  };

  const moveBlock = (index: number, direction: 'up' | 'down') => {
    if ((direction === 'up' && index === 0) || (direction === 'down' && index === blocks.length - 1)) {
      return;
    }

    const newIndex = direction === 'up' ? index - 1 : index + 1;
    const updated = [...blocks];
    [updated[index], updated[newIndex]] = [updated[newIndex], updated[index]];
    onBlocksChange(updated);
  };

  const handleImageUpload = (imageId: string) => {
    if (imageUploaderIndex !== null) {
      updateBlock(imageUploaderIndex, { imageId });
      setShowImageUploader(false);
      setImageUploaderIndex(null);
    }
  };

  return (
    <div className={`content-sequence-editor ${className}`}>
      <div className="editor-toolbar">
        <button onClick={() => addBlock('Heading')} className="btn-secondary">
          Add Heading
        </button>
        <button onClick={() => addBlock('Paragraph')} className="btn-secondary">
          Add Paragraph
        </button>
        <button onClick={() => addBlock('Image')} className="btn-secondary">
          Add Image
        </button>
      </div>

      <div className="blocks-list">
        {blocks.length === 0 ? (
          <p className="empty-blocks">No content blocks. Add one to get started.</p>
        ) : (
          blocks.map((block, index) => (
            <div key={index} className="block-editor">
              <div className="block-header">
                <span className="block-type">{block.type}</span>
                <div className="block-actions">
                  <button
                    onClick={() => moveBlock(index, 'up')}
                    disabled={index === 0}
                    className="btn-icon"
                    aria-label="Move up"
                  >
                    ↑
                  </button>
                  <button
                    onClick={() => moveBlock(index, 'down')}
                    disabled={index === blocks.length - 1}
                    className="btn-icon"
                    aria-label="Move down"
                  >
                    ↓
                  </button>
                  <button
                    onClick={() => removeBlock(index)}
                    className="btn-icon btn-danger"
                    aria-label="Delete block"
                  >
                    ✕
                  </button>
                </div>
              </div>

              <div className="block-content">
                {block.type === 'Heading' && (
                  <>
                    <div className="form-group">
                      <label htmlFor={`heading-level-${index}`}>Heading Level</label>
                      <select
                        id={`heading-level-${index}`}
                        value={block.level || 2}
                        onChange={(e) => updateBlock(index, { level: parseInt(e.target.value) })}
                      >
                        <option value={1}>H1</option>
                        <option value={2}>H2</option>
                        <option value={3}>H3</option>
                      </select>
                    </div>
                    <div className="form-group">
                      <label htmlFor={`heading-text-${index}`}>Heading Text</label>
                      <input
                        id={`heading-text-${index}`}
                        type="text"
                        value={block.text || ''}
                        onChange={(e) => updateBlock(index, { text: e.target.value })}
                        maxLength={10000}
                        placeholder="Enter heading text"
                      />
                    </div>
                  </>
                )}

                {block.type === 'Paragraph' && (
                  <div className="form-group">
                    <label htmlFor={`paragraph-text-${index}`}>Paragraph Text</label>
                    <textarea
                      id={`paragraph-text-${index}`}
                      value={block.text || ''}
                      onChange={(e) => updateBlock(index, { text: e.target.value })}
                      maxLength={10000}
                      placeholder="Enter paragraph text"
                      rows={4}
                    />
                  </div>
                )}

                {block.type === 'Image' && (
                  <div className="form-group">
                    {block.imageId ? (
                      <div className="image-block-info">
                        <p>Image ID: {block.imageId}</p>
                        <button
                          onClick={() => {
                            setImageUploaderIndex(index);
                            setShowImageUploader(true);
                          }}
                          className="btn-secondary"
                        >
                          Replace Image
                        </button>
                      </div>
                    ) : (
                      <button
                        onClick={() => {
                          setImageUploaderIndex(index);
                          setShowImageUploader(true);
                        }}
                        className="btn-primary"
                      >
                        Upload Image
                      </button>
                    )}
                  </div>
                )}
              </div>
            </div>
          ))
        )}
      </div>

      {showImageUploader && imageUploaderIndex !== null && (
        <div className="image-uploader-modal">
          <div className="modal-content">
            <h3>Upload Image</h3>
            <ImageUploader onUploadComplete={handleImageUpload} />
            <button
              onClick={() => {
                setShowImageUploader(false);
                setImageUploaderIndex(null);
              }}
              className="btn-secondary"
            >
              Cancel
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
