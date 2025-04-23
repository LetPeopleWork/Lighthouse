import { TableCell, TableRow, Typography } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import FeatureName from "../../../components/Common/FeatureName/FeatureName";
import FeatureListBase from "../../../components/Common/FeaturesList/FeatureListBase";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import ForecastLikelihood from "../../../components/Common/Forecasts/ForecastLikelihood";
import LocalDateTimeDisplay from "../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import ProgressIndicator from "../../../components/Common/ProgressIndicator/ProgressIndicator";
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import type { IFeature } from "../../../models/Feature";
import type { IProject } from "../../../models/Project/Project";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface ProjectFeatureListProps {
	project: IProject;
}

const ProjectFeatureList: React.FC<ProjectFeatureListProps> = ({ project }) => {
	const { teamMetricsService } = useContext(ApiServiceContext);

	const [featuresInProgress, setFeaturesInProgress] = useState<
		Record<string, string[]>
	>({});

	useEffect(() => {
		const fetchFeaturesInProgress = async () => {
			const featuresByTeam: Record<string, string[]> = {};

			for (const team of project.involvedTeams) {
				try {
					const features = await teamMetricsService.getFeaturesInProgress(
						team.id,
					);
					featuresByTeam[team.id] = features.map(
						(feature) => feature.workItemReference,
					);
				} catch (error) {
					console.error(`Failed to fetch features for team ${team.id}:`, error);
					featuresByTeam[team.id] = [];
				}
			}

			setFeaturesInProgress(featuresByTeam);
		};

		fetchFeaturesInProgress();
	}, [project.involvedTeams, teamMetricsService]);

	const currentOrFutureMilestones = project.milestones.filter((milestone) => {
		const today = new Date();
		today.setHours(0, 0, 0, 0);

		const milestoneDate = new Date(milestone.date);
		milestoneDate.setHours(0, 0, 0, 0);

		return milestoneDate >= today;
	});

	const renderTableHeader = () => (
		<TableRow>
			<TableCell>
				<Typography variant="h6" component="div">
					Feature Name
				</Typography>
			</TableCell>
			<TableCell sx={{ width: "25%" }}>
				<Typography variant="h6" component="div">
					Progress
				</Typography>
			</TableCell>
			<TableCell>
				<Typography variant="h6" component="div">
					Forecasts
				</Typography>
			</TableCell>
			{currentOrFutureMilestones.map((milestone) => (
				<TableCell key={milestone.id}>
					<Typography variant="h6" component="div">
						{milestone.name} (
						<LocalDateTimeDisplay utcDate={milestone.date} />)
					</Typography>
				</TableCell>
			))}
			<TableCell>
				<Typography variant="h6" component="div">
					Updated On
				</Typography>
			</TableCell>
		</TableRow>
	);

	const renderTableRow = (feature: IFeature) => (
		<TableRow key={feature.id}>
			<TableCell>
				<FeatureName
					name={feature.name}
					url={feature.url ?? ""}
					stateCategory={feature.stateCategory}
					isUsingDefaultFeatureSize={feature.isUsingDefaultFeatureSize}
					teamsWorkIngOnFeature={project.involvedTeams.filter((team) =>
						featuresInProgress[team.id]?.includes(feature.workItemReference),
					)}
				/>
			</TableCell>
			<TableCell>
				<ProgressIndicator
					title="Overall Progress"
					progressableItem={{
						remainingWork: feature.getRemainingWorkForFeature(),
						totalWork: feature.getTotalWorkForFeature(),
					}}
				/>

				{project.involvedTeams
					.filter((team) => feature.getTotalWorkForTeam(team.id) > 0)
					.map((team) => (
						<div key={team.id}>
							<ProgressIndicator
								title={
									<StyledLink to={`/teams/${team.id}`}>{team.name}</StyledLink>
								}
								progressableItem={{
									remainingWork: feature.getRemainingWorkForTeam(team.id),
									totalWork: feature.getTotalWorkForTeam(team.id),
								}}
							/>
						</div>
					))}
			</TableCell>
			<TableCell>
				<ForecastInfoList title={""} forecasts={feature.forecasts} />
			</TableCell>
			{currentOrFutureMilestones.map((milestone) => (
				<TableCell key={milestone.id}>
					<ForecastLikelihood
						remainingItems={feature.getRemainingWorkForFeature()}
						targetDate={milestone.date}
						likelihood={feature.getMilestoneLikelihood(milestone.id)}
						showText={false}
					/>
				</TableCell>
			))}
			<TableCell>
				<LocalDateTimeDisplay utcDate={feature.lastUpdated} showTime={true} />
			</TableCell>
		</TableRow>
	);

	return (
		<FeatureListBase
			features={project.features}
			renderTableHeader={renderTableHeader}
			renderTableRow={renderTableRow}
			contextId={project.id}
			contextType="project"
		/>
	);
};

export default ProjectFeatureList;
