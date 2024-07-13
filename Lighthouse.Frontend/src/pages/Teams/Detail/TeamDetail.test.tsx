import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom'; // Import Routes and MemoryRouter
import TeamDetail from './TeamDetail';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';

describe('TeamDetail component', () => {

    beforeEach(() => {
        vi.restoreAllMocks()
    });

    it('should render TeamDetail component with team name', async () => {
        render(
            <MemoryRouter initialEntries={['/teams/1']}>
                <Routes>
                    <Route path="/teams/:id" element={<TeamDetail />} />
                </Routes>
            </MemoryRouter>
        );

        await waitFor(() => {
            const teamNameElement = screen.getByText('Binary Blazers');
            expect(teamNameElement).toBeInTheDocument();
        });
    });

    it('should update throughput on button click', async () => {
        const spy = vi.spyOn(ApiServiceProvider.getApiService(), "updateThroughput");
        expect(spy.getMockName()).toEqual('updateThroughput')

        render(
            <MemoryRouter initialEntries={['/teams/1']}>
                <Routes>
                    <Route path="/teams/:id" element={<TeamDetail />} />
                </Routes>
            </MemoryRouter>
        );

        await waitFor(() => {
            const updateThroughputButton = screen.getByText('Update Throughput');
            expect(updateThroughputButton).toBeInTheDocument();

            fireEvent.click(updateThroughputButton);
        });

        expect(spy).toHaveBeenCalledTimes(1);
    });

    it('should update forecast on button click', async () => {
        const spy = vi.spyOn(ApiServiceProvider.getApiService(), "updateForecast");
        expect(spy.getMockName()).toEqual('updateForecast')

        render(
            <MemoryRouter initialEntries={['/teams/1']}>
                <Routes>
                    <Route path="/teams/:id" element={<TeamDetail />} />
                </Routes>
            </MemoryRouter>
        );

        await waitFor(() => {
            const runManualForecastButton = screen.getByText('Update Forecast');
            expect(runManualForecastButton).toBeInTheDocument();

            fireEvent.click(runManualForecastButton);
        });

        expect(spy).toHaveBeenCalledTimes(1);
    });
});
