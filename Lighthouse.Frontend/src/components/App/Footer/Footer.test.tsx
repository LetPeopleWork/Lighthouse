import { render, screen } from '@testing-library/react';
import { BrowserRouter as Router } from 'react-router-dom';
import Footer from './Footer';

describe('Footer component', () => {
  it('renders LetPeopleWorkLogo and LighthouseVersion components', async () => {
    render(
      <Router>
        <Footer />
      </Router>
    );

    await screen.findByText('DEMO VERSION');
    
    // Check if LetPeopleWorkLogo component is rendered
    const letPeopleWorkLogo = screen.getByRole('img', { name: "Let People Work Logo" });
    expect(letPeopleWorkLogo).toBeInTheDocument();

    // Check if LighthouseVersion component is rendered
    const lighthouseVersion = screen.getByRole('link', { name: 'DEMO VERSION'});
    expect(lighthouseVersion).toBeInTheDocument();
  });
});
