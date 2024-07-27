import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProjectDetail from './ProjectDetail';
import { Team } from '../../../models/Team/Team';
import { Milestone } from '../../../models/Project/Milestone';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { IApiService } from '../../../services/Api/IApiService';
import { Project } from '../../../models/Project/Project';

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
    default: ({ teams }: { teams: Team[] }) => (
        <div data-testid="involved-teams-list">{teams.length} teams</div>
    ),
}));

vi.mock('./MilestoneList', () => ({
    default: ({ milestones }: { milestones: Milestone[] }) => (
        <div data-testid="milestone-list">{milestones.length} milestones</div>
    ),
}));

vi.mock('../../../components/Common/ActionButton/ActionButton', () => ({
    default: ({ buttonText, onClickHandler }: { buttonText: string, onClickHandler: () => Promise<void> }) => (

        <button onClick={onClickHandler}>{buttonText}</button>
    ),
}));

describe('ProjectDetail component', () => {
    const apiService: IApiService = ApiServiceProvider.getApiService();

    beforeEach(() => {
        apiService.refreshFeaturesForProject = vi.fn()
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it('should render project details after loading', async () => {
        render(
            <MemoryRouter initialEntries={['/projects/3']}>
                <Routes>
                    <Route path="/projects/:id" element={<ProjectDetail />} />
                </Routes>
            </MemoryRouter>
        );

        expect(screen.getByText('Loading...')).toBeInTheDocument();

        await waitFor(() => {
            expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
        });

        expect(screen.getByTestId('project-feature-list')).toHaveTextContent('2 features');
        expect(screen.getByTestId('involved-teams-list')).toHaveTextContent('4 teams');
        expect(screen.getByTestId('milestone-list')).toHaveTextContent('2 milestones');
    });

    it('should refresh features on button click', async () => {
        render(
            <MemoryRouter initialEntries={['/projects/3']}>
                <Routes>
                    <Route path="/projects/:id" element={<ProjectDetail />} />
                </Routes>
            </MemoryRouter>
        );

        await waitFor(() => {
            expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
        });

        const refreshButton = screen.getByText('Refresh Features');
        fireEvent.click(refreshButton);

        await waitFor(() => {
            expect(refreshButton).not.toBeDisabled();
            expect(refreshButton).toHaveTextContent('Refresh Features');
        });

        expect(apiService.refreshFeaturesForProject).toHaveBeenCalledWith(3);
    });
});
