import { fireEvent, render, screen } from '@testing-library/react';
import { describe, test, expect } from 'vitest';
import OverviewDashboard from './OverviewDashboard';

describe('OverviewDashboard', () => {
  test('renders overview dashboard with mock data', async () => {
    render(<OverviewDashboard />);

    await screen.findByText('Release 1.33.7');

    expect(screen.getByText('Release 1.33.7')).toBeInTheDocument();
    expect(screen.getByText('Release 42')).toBeInTheDocument();
    expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
  });

  test('filters projects based on filter text', async () => {
    render(<OverviewDashboard />);
  
    // Wait for data to load
    await screen.findByText('Release 1.33.7');
  
    // Simulate typing into the filter input
    const filterInput = screen.getByPlaceholderText('Search');
    fireEvent.change(filterInput, { target: { value: 'Daniel' } });
  
    // Verify filtered results
    expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
    expect(screen.queryByText('Release 1.33.7')).not.toBeInTheDocument();
  });

  test('renders FilterBar and ProjectOverviewTable components', async () => {
    render(<OverviewDashboard />);
  
    // Wait for data to load
    await screen.findByText('Release 1.33.7');
  
    expect(screen.getByPlaceholderText('Search')).toBeInTheDocument();
    expect(screen.getByRole('table')).toBeInTheDocument();
  });
  
});
