import { render, screen } from '@testing-library/react';
import { BrowserRouter as Router } from 'react-router-dom';
import LighthouseVersion from './LighthouseVersion';

describe('LighthouseVersion component', () => {
  it('renders version button with fetched version number', async () => {
    render(
        <Router>
        <LighthouseVersion />
      </Router>
    );
    
    await screen.findByText('v1.33.7');

    const button = screen.getByRole('link', { name: 'v1.33.7' });
    expect(button).toBeInTheDocument();
    expect(button).toHaveAttribute('href', 'https://github.com/LetPeopleWork/Lighthouse/releases/tag/v1.33.7');
    expect(button).toHaveAttribute('target', '_blank');
    expect(button).toHaveAttribute('rel', 'noopener noreferrer');
  });
});
