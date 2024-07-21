import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import InvolvedTeamsList from './InvolvedTeamsList';
import { ITeam, Team } from '../../../models/Team/Team';

describe('InvolvedTeamsList component', () => {
    const teams: ITeam[] = [
        new Team("Team A", 1, [], [], 5 ),
        new Team("Team B", 2, [], [], 3 ),
    ];

    it('should render without errors when there are teams', () => {
        render(
            <MemoryRouter>
                <InvolvedTeamsList teams={teams} />
            </MemoryRouter>
        );

        expect(screen.getByText('Feature WIP')).toBeInTheDocument();

        teams.forEach((team) => {
            const teamLink = screen.getByRole('link', { name: team.name });
            expect(teamLink).toBeInTheDocument();
            expect(teamLink).toHaveAttribute('href', `/teams/${team.id}`);

            const wipField = screen.getByLabelText(team.name);
            expect(wipField).toBeInTheDocument();
            expect(wipField).toHaveAttribute('type', 'number');
            expect(wipField).toBeDisabled();
            expect(wipField).toHaveValue(team.featureWip);
        });
    });

    it('should render nothing when there are no teams', () => {
        render(
            <MemoryRouter>
                <InvolvedTeamsList teams={[]} />
            </MemoryRouter>
        );

        expect(screen.queryByText('Feature WIP')).not.toBeInTheDocument();
    });
});