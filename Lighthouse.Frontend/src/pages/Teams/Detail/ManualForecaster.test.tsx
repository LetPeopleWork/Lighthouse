import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import dayjs from 'dayjs';
import ManualForecaster from './ManualForecaster';
import { ManualForecast } from '../../../models/Forecasts/ManualForecast';
import { WhenForecast } from '../../../models/Forecasts/WhenForecast';
import { HowManyForecast } from '../../../models/Forecasts/HowManyForecast';

vi.mock('@mui/x-date-pickers/DatePicker', () => ({
    DatePicker: ({ value, onChange }: { value: dayjs.Dayjs | null; onChange: (date: dayjs.Dayjs | null) => void }) => (
        <input
            type="text"
            value={value ? value.format('YYYY-MM-DD') : ''}
            onChange={(e) => onChange(e.target.value ? dayjs(e.target.value) : null)}
        />
    ),
}));

vi.mock('@mui/x-date-pickers/LocalizationProvider', () => ({
    LocalizationProvider: ({ children }: { children: React.ReactNode }) => (
        <div data-testid="mocked-localization-provider">
            {children}
        </div>
    ),
    AdapterDayjs: () => null,
}));

vi.mock('../../../components/Common/Forecasts/ForecastInfoList', () => ({
    default: ({ title }: { title: string }) => <div data-testid="forecast-info-list">{title}</div>,
}));

vi.mock('../../../components/Common/Forecasts/ForecastLikelihood', () => ({
    default: ({ likelihood }: { howMany: number; when: Date; likelihood: number }) => (
        <div data-testid="forecast-likelihood">{`Likelihood: ${likelihood}%`}</div>
    ),
}));

describe('ManualForecaster component', () => {
    const mockManualForecastResult: ManualForecast = new ManualForecast(
        12,
        new Date(),
        [
            new WhenForecast(50, dayjs().add(1, 'week').toDate()),
            new WhenForecast(70, dayjs().add(2, 'weeks').toDate())
        ],
        [
            new HowManyForecast(60, 15),
            new HowManyForecast(80, 20)
        ],
        70
    );

    const mockOnRemainingItemsChange = vi.fn();
    const mockOnTargetDateChange = vi.fn();
    const mockOnRunManualForecast = vi.fn();

    beforeEach(() => {
        render(
            <ManualForecaster
                remainingItems={10}
                targetDate={dayjs().add(2, 'weeks')}
                manualForecastResult={null}
                onRemainingItemsChange={mockOnRemainingItemsChange}
                onTargetDateChange={mockOnTargetDateChange}
                onRunManualForecast={mockOnRunManualForecast} />
        );
    });

    it('should call onRemainingItemsChange when items input changes', () => {
        const itemsTextField = screen.getByLabelText('Number of Items to Forecast') as HTMLInputElement;
        fireEvent.change(itemsTextField, { target: { value: '15' } });
        expect(mockOnRemainingItemsChange).toHaveBeenCalled();
        expect(mockOnRemainingItemsChange.mock.calls[0][0]).toBe(15);
    });

    it('should call onRunManualForecast when Forecast button is clicked', () => {
        const forecastButton = screen.getByText('Forecast');
        fireEvent.click(forecastButton);
        expect(mockOnRunManualForecast).toHaveBeenCalled();
    });

    it('should render ForecastInfoList and ForecastLikelihood when manualForecastResult is not null', () => {
        render(
            <ManualForecaster
                remainingItems={10}
                targetDate={dayjs().add(2, 'weeks')}
                manualForecastResult={mockManualForecastResult}
                onRemainingItemsChange={mockOnRemainingItemsChange}
                onTargetDateChange={mockOnTargetDateChange}
                onRunManualForecast={mockOnRunManualForecast}/>
        );

        const whenForecastList = screen.getAllByTestId((id) => id.startsWith('forecast-info-list'));
        whenForecastList.map((element) => {
            expect(element).toBeInTheDocument();
        })

        const howManyForecastList = screen.getByText(
            `How Many Items will you get done till ${new Date().toLocaleDateString()}?`
        );
        const likelihoodComponent = screen.getByTestId('forecast-likelihood');

        expect(howManyForecastList).toBeInTheDocument();
        expect(likelihoodComponent).toBeInTheDocument();
    });
});
