import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import ProjectOverview from './ProjectOverview';
import { Project } from '../../models/Project';
import { WhenForecast } from '../../models/Forecasts/WhenForecast';
import { BrowserRouter as Router } from 'react-router-dom';
import { Team } from '../../models/Team';
import { Feature } from '../../models/Feature';

describe('ProjectOverview component', () => {
  const projects: Project[] = [
    new Project('Project Alpha', 1, [new Team('Team A', 1, [], []), new Team('Team B', 2, [], [])], [new Feature('Feature 1', 1, new Date('2024-06-01'), { }, [new WhenForecast(50, new Date("2024-01-01")), new WhenForecast(70, new Date("2024-02-01")), new WhenForecast(85, new Date("2024-03-01")), new WhenForecast(95, new Date("2024-04-01"))])], new Date('2024-06-01')),
    new Project('Project Beta', 2, [new Team('Team C', 3, [], [])], [new Feature('Feature 3', 1, new Date('2024-06-01'), { }, [new WhenForecast(50, new Date("2024-01-01")), new WhenForecast(70, new Date("2024-02-01")), new WhenForecast(85, new Date("2024-03-01")), new WhenForecast(95, new Date("2024-04-01"))])], new Date('2024-06-01'))
  ];

  it('should render all projects when no filter is applied', () => {
    const { container } = render(
      <Router>
        <ProjectOverview projects={projects} filterText="" />
      </Router>
    );

    // Check if all projects are rendered
    projects.forEach(project => {
      const projectCard = container.querySelector(`[data-testid="project-card-${project.id}"]`);
      expect(projectCard).toBeInTheDocument();
    });

    // Check if no projects found message is not rendered
    const noProjectsMessage = screen.queryByTestId('no-projects-message');
    expect(noProjectsMessage).not.toBeInTheDocument();
  });

  it('should render filtered projects based on filterText', () => {
    const { container } = render(
      <Router>
        <ProjectOverview projects={projects} filterText="A" />
      </Router>
    );

    // Check if only projects matching filterText are rendered
    const filteredProjects = projects.filter(project => project.name.toLowerCase().includes('a'));
    filteredProjects.forEach(project => {
      const projectCard = container.querySelector(`[data-testid="project-card-${project.id}"]`);
      expect(projectCard).toBeInTheDocument();
    });

    // Check if no projects found message is not rendered
    const noProjectsMessage = screen.queryByTestId('no-projects-message');
    expect(noProjectsMessage).not.toBeInTheDocument();
  });

  it('should render no projects found message when no projects match the filter', () => {
    render(
      <Router>
        <ProjectOverview projects={projects} filterText="XYZ" />
      </Router>
    );

    // Check if no projects found message is rendered
    const noProjectsMessage = screen.getByText('No projects found matching the filter.');
    expect(noProjectsMessage).toBeInTheDocument();

    // Check if project cards are not rendered
    projects.forEach(project => {
      const projectCard = screen.queryByTestId(`project-card-${project.id}`);
      expect(projectCard).not.toBeInTheDocument();
    });
  });
});
