import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import TeamsOverview from './TeamsOverview';
import { BrowserRouter as Router } from 'react-router-dom';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { MockApiService } from '../../../services/Api/MockApiService';

describe('TeamsOverview component', () => {
    beforeEach(() => {
        ApiServiceProvider['instance'] = null;
    });


    test('renders teams data when fetched successfully', async () => {
        render(
            <Router>
                <TeamsOverview />
            </Router>
        );

        await waitFor(() => {
            expect(screen.getByText('Binary Blazers')).toBeInTheDocument();
            expect(screen.getByText('Mavericks')).toBeInTheDocument();
        });

        expect(screen.queryByText('Error loading data. Please try again later.')).toBeNull();
    });

    test('displays error message when fetching teams fails', async () => {
        ApiServiceProvider['instance'] = new MockApiService(false, true);

        render(
            <Router>
                <TeamsOverview />
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
                <TeamsOverview />
            </Router>
        );

        await waitFor(() => {
            expect(screen.getByText('Binary Blazers')).toBeInTheDocument();
        });

        fireEvent.click(screen.getAllByTestId('delete-team-button')[0]);
        expect(screen.getByText('Confirm Delete')).toBeInTheDocument();
        expect(screen.getByText('Do you really want to delete Binary Blazers?')).toBeInTheDocument();
    });
});
