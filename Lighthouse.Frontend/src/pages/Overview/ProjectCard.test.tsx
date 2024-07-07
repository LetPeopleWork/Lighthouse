import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import ProjectCard from './ProjectCard';
import { Project } from '../../models/Project';
import { BrowserRouter as Router } from 'react-router-dom';
import { Team } from '../../models/Team';
import { Forecast } from '../../models/Forecast';

vi.mock('./TeamLink', () => ({
    default: ({ team }: { team: Team }) => (
        <span data-testid="team-link">{ team.id }</span>
    ),
}));

vi.mock('./ProjectLink', () => ({
    default: ({ project }: { project: Project }) => (
        <span data-testid="project-link">{ project.id }</span>
    ),
}));

vi.mock("../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay", () => ({
    default: ({ utcDate }: { utcDate: Date, showTime?: boolean }) => (
        <span data-testid="local-date-time-display"> {utcDate.toString()}</span>
    ),
}));

describe('ProjectCard component', () => {
    const project: Project = new Project(
        'Project Alpha',
        1,
        10,
        [new Team('Team A', 1, 1, 2, 3), new Team('Team B', 2, 1, 2, 3)],
        [new Forecast(50, new Date("2025-08-04")), new Forecast(70, new Date("2025-06-25")), new Forecast(85, new Date("2025-07-25")), new Forecast(95, new Date("2025-08-19"))],
        new Date('2024-06-01'))

  const renderComponent = () => render(
    <Router>
      <ProjectCard project={project} />
    </Router>
  );

  it('should render project details correctly', () => {
    renderComponent();

    // Check if project link is rendered
    const projectLink = screen.getByTestId('project-link');
    expect(projectLink).toBeInTheDocument();

    // Check if remaining work is rendered
    expect(screen.getByText('10 Work Items')).toBeInTheDocument();

    // Check if team links are rendered
    const teamLinks = screen.getAllByTestId('team-link');
    expect(teamLinks).toHaveLength(2);

    // Check if last updated is rendered
    expect(screen.getByText('Last Updated:')).toBeInTheDocument();
  });

  it('should render the date time display components', () => {
    renderComponent();

    // Check if LocalDateTimeDisplay components are rendered for forecasts
    const dateTimeDisplays = screen.getAllByTestId('local-date-time-display');
    expect(dateTimeDisplays).toHaveLength(5); // 4 forecasts + 1 last updated
  });
});
