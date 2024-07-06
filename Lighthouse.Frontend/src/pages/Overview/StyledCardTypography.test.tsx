import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import SvgIcon from '@mui/material/SvgIcon';
import { SvgIconComponent } from '@mui/icons-material';
import StyledCardTypography from './StyledCardTypography';

describe('StyledCardTypography component', () => {
  const MockIcon: SvgIconComponent = SvgIcon;

  const mockText = 'Styled Card Typography';
  const mockChildren = <span>Mock Children</span>;

  it('renders text and icon correctly', () => {
    render(<StyledCardTypography text={mockText} icon={MockIcon} />);

    const iconElement = screen.getByTestId('styled-card-icon');
    expect(iconElement).toBeInTheDocument();

    const textElement = screen.getByText(mockText);
    expect(textElement).toBeInTheDocument();
  });

  it('renders text, icon, and children correctly', () => {
    render(
      <StyledCardTypography text={mockText} icon={MockIcon}>
        {mockChildren}
      </StyledCardTypography>
    );

    const iconElement = screen.getByTestId('styled-card-icon');
    expect(iconElement).toBeInTheDocument();

    const textElement = screen.getByText(mockText);
    expect(textElement).toBeInTheDocument();

    const childrenElement = screen.getByText('Mock Children');
    expect(childrenElement).toBeInTheDocument();
  });

  it('uses default icon style', () => {
    render(<StyledCardTypography text={mockText} icon={MockIcon} />);

    const iconElement = screen.getByTestId('styled-card-icon');
    expect(iconElement).toHaveStyle('color: rgba(48, 87, 78, 1)');
    expect(iconElement).toHaveStyle('margin-right: 8px');
  });
});
