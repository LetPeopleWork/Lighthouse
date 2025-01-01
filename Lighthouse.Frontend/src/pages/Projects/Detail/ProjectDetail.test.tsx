import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProjectDetail from './ProjectDetail';
import { Milestone } from '../../../models/Project/Milestone';
import { Project } from '../../../models/Project/Project';
import { createMockApiServiceContext, createMockPreviewFeatureService, createMockProjectService, createMockTeamService, createMockUpdateSubscriptionService } from '../../../tests/MockApiServiceProvider';
import { IProjectService } from '../../../services/Api/ProjectService';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';
import { ITeamService } from '../../../services/Api/TeamService';
import { Feature } from '../../../models/Feature';
import { IPreviewFeatureService } from '../../../services/Api/PreviewFeatureService';
import { PreviewFeature } from '../../../models/Preview/PreviewFeature';
import { ITeamSettings } from '../../../models/Team/TeamSettings';
import { IUpdateSubscriptionService } from '../../../services/UpdateSubscriptionService';

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
    default: ({ teams }: { teams: ITeamSettings[] }) => (
        <div data-testid="involved-teams-list">{teams.length} teams</div>
    ),
}));

vi.mock('../../../components/Common/Milestones/MilestonesComponent', () => ({
    default: ({ milestones }: { milestones: Milestone[] }) => (
        <div data-testid="milestone-component">{milestones.length} milestones</div>
    ),
}));

vi.mock('../../../components/Common/ActionButton/ActionButton', () => ({
    default: ({ buttonText, onClickHandler, externalIsWaiting }: { buttonText: string, onClickHandler: () => Promise<void>, externalIsWaiting: boolean }) => (

        <button onClick={onClickHandler} disabled={externalIsWaiting}>{buttonText}</button>
    ),
}));

const mockProjectService: IProjectService = createMockProjectService();
const mockTeamService: ITeamService = createMockTeamService();
const mockPreviewFeatureService: IPreviewFeatureService = createMockPreviewFeatureService();
const mockUpdateSubscriptionService: IUpdateSubscriptionService = createMockUpdateSubscriptionService();

const mockGetProject = vi.fn();
const mockGetProjectSettings = vi.fn();
const mockGetPreviewFeatures = vi.fn();

const mockSubscribeToFeatureUpdates = vi.fn();
const mockSubscribeToForecastUpdates = vi.fn();
const mockGetUpdateStatus = vi.fn();

mockProjectService.getProject = mockGetProject;
mockProjectService.getProjectSettings = mockGetProjectSettings;
mockPreviewFeatureService.getFeatureByKey = mockGetPreviewFeatures;

mockUpdateSubscriptionService.subscribeToFeatureUpdates = mockSubscribeToFeatureUpdates;
mockUpdateSubscriptionService.subscribeToForecastUpdates = mockSubscribeToForecastUpdates;
mockUpdateSubscriptionService.getUpdateStatus = mockGetUpdateStatus;

const MockApiServiceProvider = ({ children }: { children: React.ReactNode }) => {
    const mockContext = createMockApiServiceContext({ projectService: mockProjectService, teamService: mockTeamService, previewFeatureService: mockPreviewFeatureService, updateSubscriptionService: mockUpdateSubscriptionService });

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
        mockGetProject.mockResolvedValue(new Project("Release Codename Daniel", 2, [], [new Feature("Feature 1", 0, "FTR-1", "url", new Date(), false, {}, {}, {}, {}, []), new Feature("Feature 2", 1, "FTR-2", "url", new Date(), true, {}, {}, {}, {}, [])], [new Milestone(1, "Milestone", new Date())], new Date()));
        mockGetProjectSettings.mockResolvedValue({
            id: 2,
            name: "Release Codename Daniel",
            workItemTypes: [],
            milestones: [],
            workItemQuery: "Query",
            unparentedItemsQuery: "Unparented Query",
            usePercentileToCalculateDefaultAmountOfWorkItems: false,
            defaultAmountOfWorkItemsPerFeature: 10,
            defaultWorkItemPercentile: 85,
            historicalFeaturesWorkItemQuery: "",
            workTrackingSystemConnectionId: 0,
            sizeEstimateField: "SizeEstimate",
            toDoStates: ["New"],
            doingStates: ["Active"],
            doneStates: ["Done"],
        });

        mockGetPreviewFeatures.mockResolvedValue(new PreviewFeature(0, "Feature", "Feature", "", false));
    })

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
            expect(refreshButton).toBeDisabled();
            expect(refreshButton).toHaveTextContent('Refresh Features');
        });
    });

    it('should render involved teams', async () => {
        renderWithMockApiProvider();

        await waitFor(() => {
            expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
        });

        expect(screen.getByTestId('involved-teams-list')).toHaveTextContent('0 teams');
    });

    it('should subscribe to feature and forecast updates on mount', async () => {
        renderWithMockApiProvider();

        await waitFor(() => {
            expect(mockSubscribeToFeatureUpdates).toHaveBeenCalled();
            expect(mockSubscribeToForecastUpdates).toHaveBeenCalled();
        });
    });

    it('should set Refresh Button to Enabled if Feature Update Completed', async () => {
        mockGetUpdateStatus.mockResolvedValueOnce({ status: 'Completed', updateType: 'Features', id: 2 });
        renderWithMockApiProvider();

        expect(await screen.findByText('Refresh Features')).toBeEnabled();
    });

    it('should set Refresh Button to Enabled if no Update In Progress', async () => {
        mockGetUpdateStatus.mockResolvedValueOnce(null);
        renderWithMockApiProvider();

        expect(await screen.findByText('Refresh Features')).toBeEnabled();
    });

    it('should set Refresh Button to Disabled if Feature Update Queued', async () => {
        mockGetUpdateStatus.mockResolvedValueOnce({ status: 'Queued', updateType: 'Features', id: 2 });
        renderWithMockApiProvider();

        expect(await screen.findByText('Refresh Features')).toBeDisabled();
    });

    it('should set Refresh Button to Disabled if Feature Update In Progress', async () => {
        mockGetUpdateStatus.mockResolvedValueOnce({ status: 'InProgress', updateType: 'Features', id: 2 });
        renderWithMockApiProvider();

        expect(await screen.findByText('Refresh Features')).toBeDisabled();
    });

    it('should not set Refresh Button to Disabled if Forecast Update Queued', async () => {
        mockGetUpdateStatus.mockResolvedValueOnce({ status: 'Queued', updateType: 'Forecasts', id: 2 });
        renderWithMockApiProvider();

        expect(await screen.findByText('Refresh Features')).toBeEnabled();
    });

    it('should not set Refresh Button to Disabled if Forecast Update In Progress', async () => {
        mockGetUpdateStatus.mockResolvedValueOnce({ status: 'InProgress', updateType: 'Forecasts', id: 2 });
        renderWithMockApiProvider();

        expect(await screen.findByText('Refresh Features')).toBeEnabled();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });
});