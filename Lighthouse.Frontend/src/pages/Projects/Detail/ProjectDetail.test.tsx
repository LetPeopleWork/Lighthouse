import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProjectDetail from './ProjectDetail';
import { Milestone } from '../../../models/Project/Milestone';
import { Project } from '../../../models/Project/Project';
import { TeamSettings } from '../../../models/Team/TeamSettings';
import { createMockApiServiceContext, createMockProjectService, createMockTeamService } from '../../../tests/MockApiServiceProvider';
import { IProjectService } from '../../../services/Api/ProjectService';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';
import { ITeamService } from '../../../services/Api/TeamService';
import { ProjectSettings } from '../../../models/Project/ProjectSettings';
import { Feature } from '../../../models/Feature';

vi.mock('../../../components/Common/LoadingAnimation/LoadingAnimation', () => ({
    default: ({ children, hasError, isLoading }: { children: React.ReactNode, hasError: boolean, isLoading: boolean }) => (
        <>
            {isLoading && <div>Loading...</div>}
            {hasError && <div>Error loading data</div>}
            {!isLoading && !hasError && children}
        </>
    ),
}));

vi.mock('../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay', () => ({
    default: ({ utcDate }: { utcDate: Date }) => <span>{utcDate.toString()}</span>,
}));

vi.mock('./ProjectFeatureList', () => ({
    default: ({ project }: { project: Project }) => (
        <div data-testid="project-feature-list">{project.features.length} features</div>
    ),
}));

vi.mock('./InvolvedTeamsList', () => ({
    default: ({ teams }: { teams: TeamSettings[] }) => (
        <div data-testid="involved-teams-list">{teams.length} teams</div>
    ),
}));

vi.mock('../../../components/Common/Milestones/MilestonesComponent', () => ({
    default: ({ milestones }: { milestones: Milestone[] }) => (
        <div data-testid="milestone-component">{milestones.length} milestones</div>
    ),
}));

vi.mock('../../../components/Common/ActionButton/ActionButton', () => ({
    default: ({ buttonText, onClickHandler }: { buttonText: string, onClickHandler: () => Promise<void> }) => (

        <button onClick={onClickHandler}>{buttonText}</button>
    ),
}));

const mockProjectService: IProjectService = createMockProjectService();
const mockTeamService: ITeamService = createMockTeamService();

const mockGetProject = vi.fn();
const mockGetProjectSettings = vi.fn();

mockProjectService.getProject = mockGetProject;
mockProjectService.getProjectSettings = mockGetProjectSettings;

const MockApiServiceProvider = ({ children }: { children: React.ReactNode }) => {
    const mockContext = createMockApiServiceContext({ projectService: mockProjectService, teamService: mockTeamService });

    return (
        <ApiServiceContext.Provider value={mockContext} >
            {children}
        </ApiServiceContext.Provider>
    );
};

const renderWithMockApiProvider = () => {
    render(
        <MockApiServiceProvider>
            <MemoryRouter initialEntries={['/projects/2']}>
                <Routes>
                    <Route path="/projects/:id" element={<ProjectDetail />} />
                </Routes>
            </MemoryRouter>
        </MockApiServiceProvider>
    )
}

describe('ProjectDetail component', () => {

    beforeEach(() => {
        mockGetProject.mockResolvedValue(new Project("Release Codename Daniel", 2, [], [new Feature("Feature 1", 0, "url", new Date(), {}, {}, {}, {}, []), new Feature("Feature 2", 1, "url", new Date(), {}, {}, {}, {}, [])], [new Milestone(1, "Milestone", new Date())], new Date()));
        mockGetProjectSettings.mockResolvedValue(new ProjectSettings(2, "Release Codename Daniel", [], [], "Query", "Unparented Query", 10, 0, "SizeEstimate"));
    })

    afterEach(() => {
        vi.clearAllMocks();
    });

    it('should render project details after loading', async () => {
        renderWithMockApiProvider();

        expect(screen.getByText('Loading...')).toBeInTheDocument();

        await waitFor(() => {
            expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
        });

        expect(screen.getByTestId('project-feature-list')).toHaveTextContent('2 features');
    });

    it('should refresh features on button click', async () => {
        renderWithMockApiProvider();

        await waitFor(() => {
            expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
        });

        const refreshButton = screen.getByText('Refresh Features');
        fireEvent.click(refreshButton);

        await waitFor(() => {
            expect(refreshButton).not.toBeDisabled();
            expect(refreshButton).toHaveTextContent('Refresh Features');
        });
    });
});
