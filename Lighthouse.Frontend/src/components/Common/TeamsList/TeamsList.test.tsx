import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import TeamsList from './TeamsList';
import { ITeam } from '../../../models/Team/Team';

describe('TeamsList', () => {
    const mockOnSelectionChange = vi.fn();
    const allTeams: ITeam[] = [
        { id: 1, name: 'Team Alpha', projects: [], features: [], featureWip: 1, featuresInProgress: [], remainingFeatures: 1, lastUpdated: new Date(), remainingWork: 1, totalWork: 12, throughput: [1] },
        { id: 2, name: 'Team Beta', projects: [], features: [], featureWip: 1, featuresInProgress: [], remainingFeatures: 1, lastUpdated: new Date(), remainingWork: 1, totalWork: 12, throughput: [1] },
        { id: 3, name: 'Team Gamma', projects: [], features: [], featureWip: 1, featuresInProgress: [], remainingFeatures: 1, lastUpdated: new Date(), remainingWork: 1, totalWork: 12, throughput: [1] }
    ];
    const selectedTeams = [1, 3];

    beforeEach(() => {
        mockOnSelectionChange.mockClear();
    });

    it('renders correctly with teams and search input', () => {
        render(
            <TeamsList
                allTeams={allTeams}
                selectedTeams={selectedTeams}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        expect(screen.getByText('Involved Teams')).toBeInTheDocument();
        expect(screen.getByLabelText('Search Teams')).toBeInTheDocument();

        allTeams.forEach(team => {
            expect(screen.getByText(team.name)).toBeInTheDocument();
        });

        selectedTeams.forEach(teamId => {
            const team = allTeams.find(t => t.id === teamId);
            expect(screen.getByLabelText(team!.name)).toBeChecked();
        });
    });

    it('filters teams based on search input', () => {
        render(
            <TeamsList
                allTeams={allTeams}
                selectedTeams={selectedTeams}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        const searchInput = screen.getByLabelText('Search Teams');
        fireEvent.change(searchInput, { target: { value: 'Beta' } });

        expect(screen.queryByText('Team Alpha')).not.toBeInTheDocument();
        expect(screen.getByText('Team Beta')).toBeInTheDocument();
        expect(screen.queryByText('Team Gamma')).not.toBeInTheDocument();
    });

    it('calls onSelectionChange when a team is selected', () => {
        render(
            <TeamsList
                allTeams={allTeams}
                selectedTeams={selectedTeams}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        const checkbox = screen.getByLabelText('Team Beta');
        fireEvent.click(checkbox);

        expect(mockOnSelectionChange).toHaveBeenCalledWith([...selectedTeams, 2]);
        expect(mockOnSelectionChange).toHaveBeenCalledTimes(1);
    });

    it('calls onSelectionChange when a team is deselected', () => {
        render(
            <TeamsList
                allTeams={allTeams}
                selectedTeams={selectedTeams}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        const checkbox = screen.getByLabelText('Team Gamma');
        fireEvent.click(checkbox);

        expect(mockOnSelectionChange).toHaveBeenCalledWith([1]);
        expect(mockOnSelectionChange).toHaveBeenCalledTimes(1);
    });

    it('handles no matching teams in search', () => {
        render(
            <TeamsList
                allTeams={allTeams}
                selectedTeams={selectedTeams}
                onSelectionChange={mockOnSelectionChange}
            />
        );

        const searchInput = screen.getByLabelText('Search Teams');
        fireEvent.change(searchInput, { target: { value: 'Nonexistent' } });

        allTeams.forEach(team => {
            expect(screen.queryByText(team.name)).not.toBeInTheDocument();
        });
    });
});