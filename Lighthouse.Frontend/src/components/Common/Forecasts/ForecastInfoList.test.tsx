import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import ForecastInfoList from './ForecastInfoList';
import { IForecast } from '../../../models/Forecasts/IForecast';

vi.mock('./ForecastInfo', () => ({
    default: ({ forecast }: { forecast: IForecast }) => (
        <div data-testid="forecast-info">Forecast {forecast.probability}</div>
    ),
}));

describe('ForecastInfoList component', () => {
    const mockForecasts: IForecast[] = [
        { probability: 80 } as IForecast,
        { probability: 60 } as IForecast,
    ];

    it('should render the title correctly', () => {
        render(<ForecastInfoList title="Forecast Title" forecasts={mockForecasts} />);

        expect(screen.getByText('Forecast Title')).toBeInTheDocument();
    });

    it('should render ForecastInfo components for each forecast', () => {
        render(<ForecastInfoList title="Forecast Title" forecasts={mockForecasts} />);

        const forecastInfoComponents = screen.getAllByTestId('forecast-info');
        expect(forecastInfoComponents).toHaveLength(mockForecasts.length);
    });

    it('should render forecasts in reverse order', () => {
        render(<ForecastInfoList title="Forecast Title" forecasts={mockForecasts} />);

        const forecastInfoComponents = screen.getAllByTestId('forecast-info');
        expect(forecastInfoComponents[0]).toHaveTextContent(`Forecast ${60}`);
        expect(forecastInfoComponents[1]).toHaveTextContent(`Forecast ${80}`);
    });
});
