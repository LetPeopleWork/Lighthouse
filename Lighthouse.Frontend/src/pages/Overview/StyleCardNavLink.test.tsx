import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import SvgIcon from '@mui/material/SvgIcon';
import { SvgIconComponent } from '@mui/icons-material';
import StyleCardNavLink from './StyleCardNavLink';

describe('StyleCardNavLink component', () => {
  const MockIcon: SvgIconComponent = SvgIcon; // Use SvgIcon from @mui/material

  const mockLink = '/mock-link';
  const mockText = 'Styled Card NavLink';

  it('renders NavLink with text and icon correctly', () => {
    render(
      <MemoryRouter>
        <StyleCardNavLink link={mockLink} text={mockText} icon={MockIcon} />
      </MemoryRouter>
    );

    const navLinkElement = screen.getByRole('link', { name: mockText });
    expect(navLinkElement).toBeInTheDocument();

    const iconElement = screen.getByTestId('styled-card-icon');
    expect(iconElement).toBeInTheDocument();

    expect(navLinkElement).toHaveStyle('text-decoration: none');
    expect(navLinkElement).toHaveStyle('color: inherit');
  });

  it('applies title styles when isTitle is true', () => {
    render(
      <MemoryRouter>
        <StyleCardNavLink link={mockLink} text={mockText} icon={MockIcon} isTitle />
      </MemoryRouter>
    );

    const navLinkElement = screen.getByRole('link', { name: mockText });
    expect(navLinkElement).toHaveStyle('font-size: 1.5rem');
    expect(navLinkElement).toHaveStyle('font-weight: bold');
  });
});
