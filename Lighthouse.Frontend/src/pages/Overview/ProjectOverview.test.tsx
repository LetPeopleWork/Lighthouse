import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import ProjectOverview from './ProjectOverview';
import { Project } from '../../models/Project';
import { Forecast } from '../../models/Forecast';
import { BrowserRouter as Router } from 'react-router-dom';

describe('ProjectOverview component', () => {
  const projects: Project[] = [
    {
      id: 1,
      name: 'Project Alpha',
      remainingWork: 10,
      involvedTeams: [{ id: 1, name: 'Team A', remainingWork: 12, projects: 1, features: 3 }, { id: 2, name: 'Team B', remainingWork: 12, projects: 1, features: 3 }],
      features: 3,
      forecasts: [new Forecast(50, new Date("2024-01-01")), new Forecast(70, new Date("2024-02-01")), new Forecast(85, new Date("2024-03-01")), new Forecast(95, new Date("2024-04-01"))],
      lastUpdated: new Date('2024-06-01'),
    },
    {
      id: 2,
      name: 'Project Beta',
      remainingWork: 20,
      involvedTeams: [{ id: 3, name: 'Team C', remainingWork: 12, projects: 1, features: 3 }],
      features: 3,
      forecasts: [new Forecast(50, new Date("2024-01-01")), new Forecast(70, new Date("2024-02-01")), new Forecast(85, new Date("2024-03-01")), new Forecast(95, new Date("2024-04-01"))],
      lastUpdated: new Date('2024-06-02'),
    },
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
