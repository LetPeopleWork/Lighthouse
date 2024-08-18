import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ProjectsOverview from './ProjectsOverview';
import { BrowserRouter as Router } from 'react-router-dom';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { DemoApiService } from '../../../services/Api/DemoApiService';

describe('ProjectOverview component', () => {
    beforeEach(() => {
        ApiServiceProvider['instance'] = null;
    });

    test('renders project data when fetched successfully', async () => {
        render(
            <Router>
                <ProjectsOverview />
            </Router>
        );

        await waitFor(() => {
            expect(screen.getByText('Release 1.33.7')).toBeInTheDocument();
            expect(screen.getByText('Release 42')).toBeInTheDocument();
            expect(screen.getByText('Release Codename Daniel')).toBeInTheDocument();
        });

        expect(screen.queryByText('Error loading data. Please try again later.')).toBeNull();
    });

    test('displays error message when fetching projects fails', async () => {
        ApiServiceProvider['instance'] = new DemoApiService(false, true);

        render(
            <Router>
                <ProjectsOverview />
            </Router>
        );

        expect(screen.getByTestId('loading-animation-progress-indicator')).toBeInTheDocument();
        await waitFor(() => {
            expect(screen.getByTestId('loading-animation-error-message')).toBeInTheDocument();
        });
    });

    test('opens delete confirmation dialog when delete button is clicked', async () => {
        render(
            <Router>
                <ProjectsOverview />
            </Router>
        );

        await waitFor(() => {
            expect(screen.getByText('Release 1.33.7')).toBeInTheDocument();
        });

        fireEvent.click(screen.getAllByTestId('delete-item-button')[0]);
        expect(screen.getByText('Confirm Delete')).toBeInTheDocument();
        expect(screen.getByText('Do you really want to delete Release 1.33.7?')).toBeInTheDocument();
    });
});
