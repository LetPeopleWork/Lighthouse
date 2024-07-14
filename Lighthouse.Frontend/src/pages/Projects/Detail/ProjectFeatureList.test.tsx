import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import ProjectFeatureList from './ProjectFeatureList';
import { Project } from '../../../models/Project';
import { Feature } from '../../../models/Feature';
import { Team } from '../../../models/Team';
import { Milestone } from '../../../models/Milestone';
import { WhenForecast } from '../../../models/Forecasts/WhenForecast';
import { IForecast } from '../../../models/Forecasts/IForecast';

vi.mock('../../../components/Common/Forecasts/ForecastInfoList', () => ({
    default: ({ title, forecasts }: { title: string; forecasts: IForecast[] }) => (
        <div data-testid={`forecast-info-list-${title}`}>
            {forecasts.map((forecast: IForecast, index: number) => (
                <div key={index}>{forecast.probability}%</div>
            ))}
        </div>
    ),
}));

vi.mock('../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay', () => ({
    default: ({ utcDate }: { utcDate: Date }) => <span data-testid="local-date-time-display">{utcDate.toString()}</span>,
}));

vi.mock('../../../components/Common/Forecasts/ForecastLikelihood', () => ({
    default: ({ likelihood }: { likelihood: number }) => <div data-testid="forecast-likelihood">{likelihood}%</div>,
}));

describe('ProjectFeatureList component', () => {
    const team1: Team = new Team('Team A', 1, [], [], 0);
    const team2: Team = new Team('Team B', 2, [], [], 0);

    const milestone1: Milestone = new Milestone(1, 'Milestone 1', new Date('2023-07-01'));
    const milestone2: Milestone = new Milestone(2, 'Milestone 2', new Date('2023-08-01'));

    const feature1: Feature = new Feature('Feature 1', 1, new Date(), 10, '', { 1: 5, 2: 5 }, {}, [new WhenForecast(80, new Date())]);
    const feature2: Feature = new Feature('Feature 2', 2, new Date(), 15, '', { 1: 10, 2: 5 }, {}, [new WhenForecast(60, new Date())]);

    const project: Project = new Project('Project 1', 1, [team1, team2], [feature1, feature2], [milestone1, milestone2], new Date());

    it('should render all features with correct data', () => {
        render(
            <MemoryRouter>
                <ProjectFeatureList project={project} />
            </MemoryRouter>
        );

        project.features.forEach((feature) => {
            const featureRow = screen.getByText(feature.name).closest('tr');
            expect(featureRow).toBeInTheDocument();

            const withinRow = within(featureRow!);

            const totalRemainingWorkElement = withinRow.getByText(`Total: ${feature.getAllRemainingWork()}`);
            expect(totalRemainingWorkElement).toBeInTheDocument();

            project.involvedTeams
                .filter(team => feature.getRemainingWorkForTeam(team.id) > 0)
                .forEach(team => {
                    const teamWorkElement = withinRow.getByText(`${team.name}: ${feature.getRemainingWorkForTeam(team.id)}`);
                    expect(teamWorkElement).toBeInTheDocument();
                });

            const forecastInfoListElement = withinRow.getByTestId(`forecast-info-list-`);
            expect(forecastInfoListElement).toBeInTheDocument();

            const forecastLikelihoodElements = withinRow.getAllByTestId('forecast-likelihood');
            project.milestones.forEach((milestone, index) => {
                expect(forecastLikelihoodElements[index]).toBeInTheDocument();
                expect(forecastLikelihoodElements[index]).toHaveTextContent(`${feature.getMilestoneLikelihood(milestone.id)}%`);
            });

            const localDateTimeDisplayElement = withinRow.getByTestId('local-date-time-display');
            expect(localDateTimeDisplayElement).toBeInTheDocument();
        });
    });

    it('should render the correct number of features', () => {
        render(
            <MemoryRouter>
                <ProjectFeatureList project={project} />
            </MemoryRouter>
        );

        const featureRows = screen.getAllByRole('row');
        expect(featureRows).toHaveLength(project.features.length + 1); // Including header row
    });
});
