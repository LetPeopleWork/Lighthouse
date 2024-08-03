import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import InvolvedTeamsList from './InvolvedTeamsList';
import { ITeamSettings, TeamSettings } from '../../../models/Team/TeamSettings';

describe('InvolvedTeamsList component', () => {
    const teams: ITeamSettings[] = [
        new TeamSettings(1, "Team 1", 30, 2, "", [], 5, ""),
        new TeamSettings(2, "Team 2", 30, 1, "", [], 5, ""),
    ];

    it('should render without errors when there are teams', () => {
        render(
            <MemoryRouter>
                <InvolvedTeamsList teams={teams} initiallyExpanded={true} onTeamUpdated={() => new Promise(resolve => setTimeout(resolve, 0))} />
            </MemoryRouter>
        );

        expect(screen.getByText('Involved Teams (Feature WIP)')).toBeInTheDocument();

        teams.forEach((team) => {
            const teamLink = screen.getByRole('link', { name: team.name });
            expect(teamLink).toBeInTheDocument();
            expect(teamLink).toHaveAttribute('href', `/teams/${team.id}`);

            const wipField = screen.getByLabelText(team.name);
            expect(wipField).toBeInTheDocument();
            expect(wipField).toHaveAttribute('type', 'number');
            expect(wipField).toBeEnabled();
            expect(wipField).toHaveValue(team.featureWIP);
        });
    });

    it('should render nothing when there are no teams', () => {
        render(
            <MemoryRouter>
                <InvolvedTeamsList teams={[]} initiallyExpanded={true} onTeamUpdated={() => new Promise(resolve => setTimeout(resolve, 0))} />
            </MemoryRouter>
        );

        expect(screen.queryByText('Involved Teams (Feature WIP)')).not.toBeInTheDocument();
    });

    it('should update team with new settings on change', () => {
        const updateTeam = vi.fn();

        render(
            <MemoryRouter>
                <InvolvedTeamsList teams={teams} initiallyExpanded={true} onTeamUpdated={updateTeam} />
            </MemoryRouter>
        );

        fireEvent.change(screen.getByLabelText(/Team 1/i), { target: { value: '5' } });

        expect(updateTeam).toHaveBeenCalledWith(
            expect.objectContaining({
                id: 1,
                name: "Team 1",
                featureWIP: 5
            })
        );
    })
});