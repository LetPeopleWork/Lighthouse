import { render, screen } from '@testing-library/react';
import { describe, test, expect } from 'vitest';
import ProjectLink from './ProjectLink';
import { BrowserRouter as Router } from 'react-router-dom';
import { Project } from '../../models/Project';

const project: Project = new Project("My Team", 12, [], [], new Date())

describe('ProjectLink', () => {
  test('renders without crashing', () => {
    render(
      <Router>
        <ProjectLink project={project} />
      </Router>
    );
    const projectLinkElement = screen.getByRole('link');
    expect(projectLinkElement).toBeInTheDocument();
  });

  test('creates correct link to Project', () => {
    render(
      <Router>
        <ProjectLink project={project} />
      </Router>
    );
    const projectLinkElement = screen.getByRole('link');
    expect(projectLinkElement).toHaveAttribute('href', `/projects/${project.id}`);
  });

});