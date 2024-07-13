import { Project } from './Project';
import { Feature } from './Feature';
import { Team } from './Team';
import { WhenForecast } from './Forecasts/WhenForecast';

describe('Project Class', () => {
    let project: Project;
    let name: string;
    let id: number;
    let involvedTeams: Team[];
    let features: Feature[];
    let lastUpdated: Date;

    beforeEach(() => {
        name = 'New Project';
        id = 1;
        involvedTeams = [new Team('Team A', 1, [], []), new Team('Team B', 2, [], [])];
        lastUpdated = new Date('2023-07-11');

        const feature1 = new Feature('Feature 1', 1, new Date('2023-07-10'), { 1: 10, 2: 20 }, [
            new WhenForecast(0.8, new Date('2023-08-01')),
        ]);
        const feature2 = new Feature('Feature 2', 2, new Date('2023-07-09'), { 1: 5, 2: 15 }, [
            new WhenForecast(0.6, new Date('2023-09-01')),
        ]);

        features = [feature1, feature2];
        project = new Project(name, id, involvedTeams, features, lastUpdated);
    });

    it('should create an instance of Project correctly', () => {
        expect(project.name).toBe(name);
        expect(project.id).toBe(id);
        expect(project.involvedTeams).toEqual(involvedTeams);
        expect(project.lastUpdated).toBe(lastUpdated);
        expect(project.features).toEqual(features);
    });

    it('should return correct total remaining work', () => {
        const expectedRemainingWork = 10 + 20 + 5 + 15;
        expect(project.remainingWork).toBe(expectedRemainingWork);
    });

    it('should return correct number of remaining features', () => {
        expect(project.remainingFeatures).toBe(2);
    });
});
