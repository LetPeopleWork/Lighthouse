import { render, screen } from '@testing-library/react';
import { describe, test, expect, vi } from 'vitest';
import ProjectOverviewTable from './ProjectOverviewTable';
import { Project } from '../../models/Project';

// Mock ProjectOverviewRow component to simplify tests
vi.mock('./ProjectOverviewRow', () => ({
    default: ({ project }: { project: Project }) => (
    <tr data-testid="project-row">
      <td>{project.name}</td>
    </tr>
  ),
}));

const projects: Project[] = [
  {
    id: 1,
    name: 'Project Alpha',
    remainingWork: 10,
    involvedTeams: [{ id: 1, name: 'Team A' }, { id: 2, name: 'Team B' }],
    forecasts: [],
    lastUpdated: new Date('2024-06-01'),
  },
  {
    id: 2,
    name: 'Project Beta',
    remainingWork: 20,
    involvedTeams: [{ id: 3, name: 'Team C' }],
    forecasts: [],
    lastUpdated: new Date('2024-06-02'),
  },
];

describe('ProjectOverviewTable', () => {
  test('renders without crashing', () => {
    render(<ProjectOverviewTable projects={projects} filterText="" />);
    const tableElement = screen.getByRole('table');
    expect(tableElement).toBeInTheDocument();
  });

  test('renders the correct number of ProjectOverviewRow components', () => {
    render(<ProjectOverviewTable projects={projects} filterText="" />);
    const projectRows = screen.getAllByTestId('project-row');
    expect(projectRows).toHaveLength(2);
  });

  test('filters projects based on the filter text in project name', () => {
    render(<ProjectOverviewTable projects={projects} filterText="Alpha" />);
    const projectRows = screen.getAllByTestId('project-row');
    expect(projectRows).toHaveLength(2);
    expect(screen.getByText('Project Alpha')).toBeInTheDocument();
  });

  test('filters projects based on the filter text in involved teams', () => {
    render(<ProjectOverviewTable projects={projects} filterText="Team C" />);
    const projectRows = screen.getAllByTestId('project-row');
    expect(projectRows).toHaveLength(1);
    expect(screen.getByText('Project Beta')).toBeInTheDocument();
  });

  test('displays correct table headers', () => {
    render(<ProjectOverviewTable projects={projects} filterText="" />);
    const headers = ['Name', 'Remaining Work', 'Involved Teams', '50%', '70%', '85%', '95%', 'Last Updated on:'];
    headers.forEach(header => {
      expect(screen.getByText(header)).toBeInTheDocument();
    });
  });
});
