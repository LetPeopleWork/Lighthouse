import { render, screen } from '@testing-library/react';
import { describe, test, expect } from 'vitest';
import TeamLink from './TeamLink';
import { Team } from '../../models/Team';

const team : Team = new Team("My Team", 12)

describe('TeamLink', () => {
    test('renders without crashing', () => {
        render(<TeamLink team={team} />);
        const teamLinkElement = screen.getByRole('link');
        expect(teamLinkElement).toBeInTheDocument();
      });

      test('creates correct link to Team', () => {
        render(<TeamLink team={team} />);
        const teamLinkElement = screen.getByRole('link');
        expect(teamLinkElement).toHaveAttribute('href', `/teams/${team.id}`);
      });
    
});