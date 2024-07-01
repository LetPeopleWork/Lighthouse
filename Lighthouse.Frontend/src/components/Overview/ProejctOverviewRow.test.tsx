import { render, screen } from '@testing-library/react';
import { describe, test, expect, vi } from 'vitest';
import ProjectOverviewRow from './ProjectOverviewRow';
import { Project } from '../../models/Project';
import { Team } from '../../models/Team';
import { Forecast } from '../../models/Forecast';

vi.mock('./TeamLink', () => ({
    default: ({ team }: { team: Team }) => (
        <div data-testid="team-row">
            <a>{team.name}</a>
        </div>
    ),
}));

vi.mock('../Common/LocalDateTimeDisplay/LocalDateTimeDisplay', () => ({
    default: ({ utcDate }: { utcDate: Date, showTime?: boolean }) => (
        <div data-testid="time-row">
            {utcDate.toString()}
        </div>
    ),
}));

const project: Project = new Project(
    'Project Alpha',
    1,
    10,
    [new Team('Team A', 1), new Team('Team B', 2)],
    [new Forecast(50, new Date("2025-08-04")), new Forecast(70, new Date("2025-06-25")), new Forecast(85, new Date("2025-07-25")), new Forecast(95, new Date("2025-08-19"))],
    new Date('2024-06-01'))

describe('ProjectOverviewRow', () => {
    test('renders without crashing', () => {
        render(<ProjectOverviewRow project={project} />);
        const tableRow = screen.getByRole('row');
        expect(tableRow).toBeInTheDocument();
    });

    test('renders the correct number of TeamLink components', () => {
        render(<ProjectOverviewRow project={project} />);
        const projectRows = screen.getAllByTestId('team-row');
        expect(projectRows).toHaveLength(2);
    });

    test('renders the correct number of LocalDateTimeDisplay components', () => {
        render(<ProjectOverviewRow project={project} />);
        const projectRows = screen.getAllByTestId('time-row');
        expect(projectRows).toHaveLength(5);
    });
});