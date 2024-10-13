import { LighthouseChartData, LighthouseChartFeatureData, BurndownEntry, featureColors } from './LighthouseChartData';
import { IMilestone } from '../Project/Milestone';

describe('LighthouseChartData Class', () => {
    let features: LighthouseChartFeatureData[];
    let milestones: IMilestone[];

    beforeEach(() => {
        const burndownEntries1: BurndownEntry[] = [
            new BurndownEntry(new Date('2023-07-01'), 10),
            new BurndownEntry(new Date('2023-07-10'), 5)
        ];

        const burndownEntries2: BurndownEntry[] = [
            new BurndownEntry(new Date('2023-08-01'), 20),
            new BurndownEntry(new Date('2023-08-15'), 15)
        ];

        features = [
            new LighthouseChartFeatureData('Feature 1', [new Date('2023-07-11')], burndownEntries1),
            new LighthouseChartFeatureData('Feature 2', [new Date('2023-08-12')], burndownEntries2)
        ];

        milestones = [
            { name: 'Milestone 1', date: new Date('2023-09-01') } as IMilestone,
            { name: 'Milestone 2', date: new Date('2023-10-01') } as IMilestone
        ];
    });

    it('should create an instance of LighthouseChartData with correct dates', () => {
        const lighthouseChartData = new LighthouseChartData(features, milestones);
        
        expect(lighthouseChartData.startDate).toEqual(new Date('2023-06-29'));  // 2 days before the earliest date
        expect(lighthouseChartData.endDate).toEqual(new Date('2023-10-03'));    // 2 days after the latest date
        expect(lighthouseChartData.features.length).toBe(2);
        expect(lighthouseChartData.milestones.length).toBe(2);
    });

    it('should calculate the correct max remaining items', () => {
        const lighthouseChartData = new LighthouseChartData(features, milestones);
        
        expect(lighthouseChartData.maxRemainingItems).toBe(20); // From the second feature's trend
    });
});

describe('LighthouseChartFeatureData Class', () => {
    let burndownEntries: BurndownEntry[];
    let forecasts: Date[];
    let feature: LighthouseChartFeatureData;

    beforeEach(() => {
        burndownEntries = [
            new BurndownEntry(new Date('2023-07-01'), 10),
            new BurndownEntry(new Date('2023-07-10'), 5)
        ];

        forecasts = [new Date('2023-07-15'), new Date('2023-07-20')];

        feature = new LighthouseChartFeatureData('Feature 1', forecasts, burndownEntries);
    });

    it('should create a feature with the correct name, forecasts, and burndown data', () => {
        expect(feature.name).toBe('Feature 1');
        expect(feature.forecasts).toEqual(forecasts);
        expect(feature.remainingItemsTrend).toEqual(burndownEntries);
    });

    it('should assign a random color from the available color set', () => {
        const color = feature.color;
        expect(featureColors).toContain(color);
    });

    it('should not assign a color already used', () => {
        const usedColor = feature.color;
        const anotherFeature = new LighthouseChartFeatureData('Feature 2', forecasts, burndownEntries);
        expect(usedColor).not.toBe(anotherFeature.color);
    });
});

describe('BurndownEntry Class', () => {
    it('should create a burndown entry with correct date and remaining items', () => {
        const entry = new BurndownEntry(new Date('2023-07-01'), 10);
        expect(entry.date).toEqual(new Date('2023-07-01'));
        expect(entry.remainingItems).toBe(10);
    });
});
