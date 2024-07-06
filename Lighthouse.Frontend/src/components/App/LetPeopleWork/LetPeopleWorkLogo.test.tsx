import { render, screen } from '@testing-library/react';
import { BrowserRouter as Router } from 'react-router-dom';
import LetPeopleWorkLogo from './LetPeopleWorkLogo';

describe('LetPeopleWorkLogo component', () => {
  it('renders logo image', () => {
    render(
      <Router>
        <LetPeopleWorkLogo />
      </Router>
    );
    const logoImage = screen.getByAltText('Let People Work Logo');
    expect(logoImage).toBeInTheDocument();
    expect(logoImage).toHaveAttribute('src', '/src/assets/LetPeopleWorkLogo.png');
    expect(logoImage).toHaveAttribute('width', '70');
  });

  it('renders button with correct link attributes', () => {
    render(
      <Router>
        <LetPeopleWorkLogo />
      </Router>
    );
    const logoButton = screen.getByRole('link', { name: 'Let People Work Logo' });
    expect(logoButton).toBeInTheDocument();
    expect(logoButton).toHaveAttribute('href', 'https://letpeople.work');
    expect(logoButton).toHaveAttribute('target', '_blank');
    expect(logoButton).toHaveAttribute('rel', 'noopener noreferrer');
  });

  it('has "nav-link" class', () => {
    render(
      <Router>
        <LetPeopleWorkLogo />
      </Router>
    );
    const logoButton = screen.getByRole('link', { name: 'Let People Work Logo' });
    expect(logoButton).toHaveClass('nav-link');
  });
});
