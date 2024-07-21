import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import ProjectCard from './ProjectCard';
import { Project } from '../../models/Project';
import { BrowserRouter as Router } from 'react-router-dom';
import { Team } from '../../models/Team/Team';
import { WhenForecast } from '../../models/Forecasts/WhenForecast';
import { Feature } from '../../models/Feature';
import { Milestone } from '../../models/Milestone';

vi.mock('./TeamLink', () => ({
  default: ({ team }: { team: Team }) => (
    <span data-testid="team-link">{team.id}</span>
  ),
}));

vi.mock('./ProjectLink', () => ({
  default: ({ project }: { project: Project }) => (
    <span data-testid="project-link">{project.id}</span>
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
    [new Team('Team A', 1, [], [], 1), new Team('Team B', 2, [], [], 1)],
    [new Feature('Feature', 0, new Date(), { 1: "Project Alpha" }, { 1: 7, 2: 3 }, { 0: 74.5 }, [new WhenForecast(50, new Date("2025-08-04")), new WhenForecast(70, new Date("2025-06-25")), new WhenForecast(85, new Date("2025-07-25")), new WhenForecast(95, new Date("2025-08-19"))])],
    [new Milestone(1, "Milestone 1", new Date(Date.now() + 14 * 24 * 60 * 60))],
    new Date('2024-06-01')
  );

  const renderComponent = () => render(
    <Router>
      <ProjectCard project={project} />
    </Router>
  );

  it('should render project details correctly', () => {
    renderComponent();

    const projectLink = screen.getByTestId('project-link');
    expect(projectLink).toBeInTheDocument();

    expect(screen.getByText('10 Work Items')).toBeInTheDocument();

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

  it('should render forecasts from the feature with the latest expectedDate', () => {
    renderComponent();

    const forecastDates = [
      new Date("2025-08-04").toString(),
      new Date("2025-06-25").toString(),
      new Date("2025-07-25").toString(),
      new Date("2025-08-19").toString()
    ];

    forecastDates.forEach(date => {
      expect(screen.getByText((content) => content.includes(date))).toBeInTheDocument();
    });
  });
});
