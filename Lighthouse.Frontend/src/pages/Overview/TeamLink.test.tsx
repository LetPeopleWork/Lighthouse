import { render, screen } from '@testing-library/react';
import { describe, test, expect } from 'vitest';
import TeamLink from './TeamLink';
import { Team } from '../../models/Team/Team';
import { BrowserRouter as Router } from 'react-router-dom';

const team: Team = new Team("My Team", 12, [], [], 2, ["FTR-1"], new Date())

describe('TeamLink', () => {
  test('renders without crashing', () => {
    render(
      <Router>
        <TeamLink team={team} />
      </Router>
    );
    const teamLinkElement = screen.getByRole('link');
    expect(teamLinkElement).toBeInTheDocument();
  });

  test('creates correct link to Team', () => {
    render(
      <Router>
        <TeamLink team={team} />
      </Router>
    );
    const teamLinkElement = screen.getByRole('link');
    expect(teamLinkElement).toHaveAttribute('href', `/teams/${team.id}`);
  });

});