import { describe, it, expect } from 'vitest';
import { WhenForecast } from './WhenForecast';

describe('WhenForecast', () => {
    it('should create a WhenForecast instance with the correct properties', () => {
        const probability = 75;
        const expectedDate = new Date('2024-12-25');
        const whenForecast = new WhenForecast(probability, expectedDate);

        expect(whenForecast.probability).toBe(probability);
        expect(whenForecast.expectedDate).toBe(expectedDate);
    });

    it('should handle invalid probability values', () => {
        const expectedDate = new Date('2024-12-25');
        
        expect(() => new WhenForecast(-10, expectedDate)).toThrow(RangeError);
        expect(() => new WhenForecast(110, expectedDate)).toThrow(RangeError);
    });

    it('should handle invalid date values', () => {
        const probability = 75;

        expect(() => new WhenForecast(probability, new Date('invalid-date'))).toThrow(Error);
    });
});
