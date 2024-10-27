import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import ImageComponent from './ImageComponent';

describe('ImageComponent', () => {
  it('renders an image when src is provided', async () => {
    const testImageSrc = 'https://via.placeholder.com/150';
    render(<ImageComponent src={testImageSrc} alt="Test Image" />);

    const img = await screen.findByAltText(/test image/i);

    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute('src', testImageSrc);
  });

  it('renders the correct alt text', async () => {
    const testImageSrc = 'https://via.placeholder.com/150';
    const altText = 'Sample Image';
    render(<ImageComponent src={testImageSrc} alt={altText} />);

    const img = await screen.findByAltText(/sample image/i);
    expect(img).toHaveAttribute('alt', altText);
  });

  it('renders correctly when no alt text is provided', async () => {
    const testImageSrc = 'https://via.placeholder.com/150';
    render(<ImageComponent src={testImageSrc} />);

    const img = await screen.findByAltText('Image');
    expect(img).toBeInTheDocument();
  });
});