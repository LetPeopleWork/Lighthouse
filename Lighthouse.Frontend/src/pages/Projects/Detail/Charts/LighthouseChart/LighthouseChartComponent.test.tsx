// LighthouseChartComponent.test.tsx

import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import LighthouseChartComponent from './LighthouseChartComponent';
import { ApiServiceContext } from '../../../../../services/Api/ApiServiceContext';
import { createMockApiServiceContext, createMockChartService } from '../../../../../tests/MockApiServiceProvider';
import { IChartService } from '../../../../../services/Api/ChartService';
import dayjs, { Dayjs } from 'dayjs';

// Mocking the dependent components
vi.mock('../../../../../components/Common/DatePicker/DatePickerComponent', () => ({
    default: ({ label, value, onChange }: { label: string, value: Dayjs, onChange:(newValue: Dayjs | null) => void }) => (
        <input
            aria-label={label}
            value={value.format('YYYY-MM-DD')}
            onChange={(e) => onChange(dayjs(e.target.value))}
        />
    ),
}));

vi.mock('../../SampleFrequencySelector', () => ({
    default: ({ sampleEveryNthDay, onSampleEveryNthDayChange }: { sampleEveryNthDay: number, onSampleEveryNthDayChange: (value: number) => void; }) => (
        <select
            value={sampleEveryNthDay}
            onChange={(e) => onSampleEveryNthDayChange(Number(e.target.value))}
        >
            <option value={1}>Daily</option>
            <option value={7}>Weekly</option>
            <option value={30}>Monthly</option>
        </select>
    ),
}));

vi.mock('../../../../../components/Common/LoadingAnimation/LoadingAnimation', () => ({
    default: ({ hasError, isLoading }: { hasError: boolean, isLoading: boolean, children: React.ReactNode }) => (
        <>
            {isLoading && <div>Loading...</div>}
            {hasError && <div>Error loading data</div>}
            {!isLoading && !hasError}
        </>
    ),
}));


// Creating mock chart service
const mockChartService: IChartService = createMockChartService();

const mockGetLighthouseChartData = vi.fn();
mockChartService.getLighthouseChartData = mockGetLighthouseChartData;

const MockApiServiceProvider = ({ children }: { children: React.ReactNode }) => {
    const mockContext = createMockApiServiceContext({ chartService: mockChartService });

    return (
        <ApiServiceContext.Provider value={mockContext}>
            {children}
        </ApiServiceContext.Provider>
    );
};

const renderWithMockApiProvider = () => {
    render(
        <MockApiServiceProvider>
            <LighthouseChartComponent projectId={1} />
        </MockApiServiceProvider>
    );
};

describe('LighthouseChartComponent', () => {
    afterEach(() => {
        vi.clearAllMocks();
    });

    it('renders without crashing', () => {
        renderWithMockApiProvider();
        expect(screen.getByLabelText('Burndown Start Date')).toBeInTheDocument();
        expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    it('initially fetches lighthouse data', async () => {
        mockGetLighthouseChartData.mockResolvedValueOnce({});
        renderWithMockApiProvider();

        expect(mockGetLighthouseChartData).toHaveBeenCalledWith(
            1,
            expect.any(Date),
            1
        );

        await waitFor(() => expect(screen.queryByText('Loading...')).not.toBeInTheDocument());
    });

    it('handles data fetching error', async () => {
        mockGetLighthouseChartData.mockRejectedValueOnce(new Error('Fetch error'));
        renderWithMockApiProvider();

        await waitFor(() => expect(screen.getByText('Error loading data')).toBeInTheDocument());
    });

    it('updates start date and fetches new data', async () => {
        mockGetLighthouseChartData.mockResolvedValueOnce({});
        renderWithMockApiProvider();

        const startDateInput = screen.getByLabelText('Burndown Start Date');
        fireEvent.change(startDateInput, { target: { value: dayjs().format('YYYY-MM-DD') } });

        expect(mockGetLighthouseChartData).toHaveBeenCalledWith(
            1,
            expect.any(Date),
            1
        );
    });

    it('updates sample rate and fetches new data', async () => {
        mockGetLighthouseChartData.mockResolvedValueOnce({});
        renderWithMockApiProvider();

        const sampleRateSelector = screen.getByRole('combobox');
        fireEvent.change(sampleRateSelector, { target: { value: '7' } });

        expect(mockGetLighthouseChartData).toHaveBeenCalledWith(
            1,
            expect.any(Date),
            7
        );
    });
});
