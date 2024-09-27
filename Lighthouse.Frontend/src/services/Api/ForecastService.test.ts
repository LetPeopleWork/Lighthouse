import axios from 'axios';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ForecastService } from './ForecastService';
import { IManualForecast, ManualForecast } from '../../models/Forecasts/ManualForecast';
import { WhenForecast } from '../../models/Forecasts/WhenForecast';
import { HowManyForecast } from '../../models/Forecasts/HowManyForecast';

vi.mock('axios');
const mockedAxios = vi.mocked(axios, true);

describe('ForecastService', () => {
    let forecastService: ForecastService;

    beforeEach(() => {
        mockedAxios.create.mockReturnThis();
        forecastService = new ForecastService();
    });

    afterEach(() => {
        vi.resetAllMocks();
    });

    it('should run a manual forecast for a team', async () => {
        const teamId = 1;
        const remainingItems = 10;
        const targetDate = new Date('2023-10-01');

        const mockResponse: IManualForecast = new ManualForecast(remainingItems, targetDate, [new WhenForecast(0.75, new Date("2023-10-15T00:00:00Z"))], [new HowManyForecast(0.75, 8)], 0.9 );

        mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

        const result = await forecastService.runManualForecast(teamId, remainingItems, targetDate);

        expect(result).toEqual(new ManualForecast(
            10,
            new Date('2023-10-01T00:00:00Z'),
            [new WhenForecast(0.75, new Date('2023-10-15T00:00:00Z'))],
            [new HowManyForecast(0.75, 8)],
            0.9
        ));
        expect(mockedAxios.post).toHaveBeenCalledWith(`/forecast/manual/${teamId}`, {
            remainingItems,
            targetDate
        });
    });

    it('should handle empty whenForecasts and howManyForecasts', async () => {
        const teamId = 2;
        const remainingItems = 5;
        const targetDate = new Date('2023-12-01');

        const mockResponse: IManualForecast =  new ManualForecast(remainingItems, targetDate, [], [], 0.85);

        mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

        const result = await forecastService.runManualForecast(teamId, remainingItems, targetDate);

        expect(result).toEqual(new ManualForecast(
            5,
            new Date('2023-12-01T00:00:00Z'),
            [],
            [],
            0.85
        ));
        expect(mockedAxios.post).toHaveBeenCalledWith(`/forecast/manual/${teamId}`, {
            remainingItems,
            targetDate
        });
    });

    it('should throw an error if API call fails', async () => {
        const teamId = 3;
        const remainingItems = 12;
        const targetDate = new Date('2023-11-01');

        mockedAxios.post.mockRejectedValueOnce(new Error('API error'));

        await expect(forecastService.runManualForecast(teamId, remainingItems, targetDate))
            .rejects
            .toThrow('API error');

        expect(mockedAxios.post).toHaveBeenCalledWith(`/forecast/manual/${teamId}`, {
            remainingItems,
            targetDate
        });
    });
});
