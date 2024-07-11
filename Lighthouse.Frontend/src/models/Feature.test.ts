import { Feature } from './Feature';
import { WhenForecast } from './WhenForecast';

describe('Feature Class', () => {
    let feature: Feature;
    let name: string;
    let id: number;
    let lastUpdated: Date;
    let remainingWork: { [key: number]: number };
    let forecasts: WhenForecast[];

    beforeEach(() => {
        name = 'New Feature';
        id = 1;
        lastUpdated = new Date('2023-07-11');
        remainingWork = { 1: 10, 2: 20, 3: 30 };
        forecasts = [
            new WhenForecast(0.8, new Date('2023-08-01')),
            new WhenForecast(0.6, new Date('2023-09-01')),
        ];
        feature = new Feature(name, id, lastUpdated, remainingWork, forecasts);
    });

    it('should create an instance of Feature correctly', () => {
        expect(feature.name).toBe(name);
        expect(feature.id).toBe(id);
        expect(feature.lastUpdated).toBe(lastUpdated);
        expect(feature.remainingWork).toEqual(remainingWork);
        expect(feature.forecasts).toEqual(forecasts);
    });

    it('should return correct remaining work for a team', () => {
        expect(feature.getRemainingWorkForTeam(1)).toBe(10);
        expect(feature.getRemainingWorkForTeam(2)).toBe(20);
        expect(feature.getRemainingWorkForTeam(3)).toBe(30);
        expect(feature.getRemainingWorkForTeam(4)).toBe(0);
    });

    it('should return correct total remaining work', () => {
        expect(feature.getAllRemainingWork()).toBe(60);
    });
});
