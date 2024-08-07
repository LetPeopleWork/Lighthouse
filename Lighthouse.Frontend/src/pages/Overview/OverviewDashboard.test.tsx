import { fireEvent, render, screen } from '@testing-library/react';
import { describe, test, expect } from 'vitest';
import OverviewDashboard from './OverviewDashboard';
import { BrowserRouter as Router } from 'react-router-dom';

describe('OverviewDashboard', () => {
  test('renders overview dashboard with mock data', async () => {
    render(
      <Router>
        <OverviewDashboard />
      </Router>);

    await screen.findByText('Release 1.33.7');

    expect(screen.getByText('Release 1.33.7')).toBeInTheDocument();
    expect(screen.getByText('Release 42')).toBeInTheDocument();
    expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
  });

  test('filters projects based on filter text', async () => {
    render(
      <Router>
        <OverviewDashboard />
      </Router>);

    await screen.findByText('Release 1.33.7');

    const filterInput = screen.getByPlaceholderText('Search');
    fireEvent.change(filterInput, { target: { value: 'Daniel' } });

    expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
    expect(screen.queryByText('Release 1.33.7')).not.toBeInTheDocument();
  });

});
