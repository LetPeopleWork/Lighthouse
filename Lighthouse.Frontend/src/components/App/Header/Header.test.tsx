import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import Header from './Header';
import { MemoryRouter } from 'react-router-dom';

describe('Header component', () => {
  it('should render the LighthouseLogo', () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );
    const logo = screen.getByTestId('CellTowerIcon'); // Assuming the logo has this test id
    expect(logo).toBeInTheDocument();
  });

  it('should render navigation items', () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );

    expect(screen.getByText('Overview')).toBeInTheDocument();
    expect(screen.getByText('Teams')).toBeInTheDocument();
    expect(screen.getByText('Projects')).toBeInTheDocument();
    expect(screen.getByText('Settings')).toBeInTheDocument();
  });

  it('should render external link buttons with correct links', () => {
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>
    );

    const bugReportLink = screen.getByTestId('https://github.com/LetPeopleWork/Lighthouse/issues');
    const youtubeLink = screen.getByTestId('https://www.youtube.com/channel/UCipDDn2dpVE3rpoKNW2asZQ');
    const blogLink = screen.getByTestId('https://www.letpeople.work/blog/');
    const githubLink = screen.getByTestId('https://github.com/LetPeopleWork/');

    expect(bugReportLink).toHaveAttribute('href', 'https://github.com/LetPeopleWork/Lighthouse/issues');
    expect(youtubeLink).toHaveAttribute('href', 'https://www.youtube.com/channel/UCipDDn2dpVE3rpoKNW2asZQ');
    expect(blogLink).toHaveAttribute('href', 'https://www.letpeople.work/blog/');
    expect(githubLink).toHaveAttribute('href', 'https://github.com/LetPeopleWork/');
  });
});
