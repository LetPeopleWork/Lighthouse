import { describe, it, expect } from 'vitest';
import { HowManyForecast, IHowManyForecast } from './HowManyForecast';

describe('HowManyForecast', () => {
    it('should create a HowManyForecast with the correct properties', () => {
        const probability = 80;
        const expectedItems = 100;
        const forecast = new HowManyForecast(probability, expectedItems);

        expect(forecast.probability).toBe(probability);
        expect(forecast.expectedItems).toBe(expectedItems);
    });

    it('should implement the IHowManyForecast interface', () => {
        const forecast: IHowManyForecast = new HowManyForecast(80, 100);

        expect(forecast).toBeInstanceOf(HowManyForecast);
        expect(forecast).toHaveProperty('probability');
        expect(forecast).toHaveProperty('expectedItems');
    });
});
