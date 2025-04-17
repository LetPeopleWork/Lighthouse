import { TableCell, TableRow, Typography } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import FeatureName from "../../../components/Common/FeatureName/FeatureName";
import FeatureListBase from "../../../components/Common/FeaturesList/FeatureListBase";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import LocalDateTimeDisplay from "../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import ProgressIndicator from "../../../components/Common/ProgressIndicator/ProgressIndicator";
import type { IFeature } from "../../../models/Feature";
import type { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface FeatureListProps {
	team: Team;
}

const TeamFeatureList: React.FC<FeatureListProps> = ({ team }) => {
	const { teamMetricsService } = useContext(ApiServiceContext);

	const [featuresInProgress, setFeaturesInProgress] = useState<string[]>([]);

	useEffect(() => {
		const fetchFeaturesInProgress = async () => {
			const features = await teamMetricsService.getFeaturesInProgress(team.id);
			setFeaturesInProgress(
				features.map((feature) => feature.workItemReference),
			);
		};

		fetchFeaturesInProgress();
	}, [team, teamMetricsService]);

	const renderTableHeader = () => (
		<TableRow>
			<TableCell sx={{ width: "15%" }}>
				<Typography variant="h6" component="div">
					Feature Name
				</Typography>
			</TableCell>
			<TableCell sx={{ width: "30%" }}>
				<Typography variant="h6" component="div">
					Progress
				</Typography>
			</TableCell>
			<TableCell sx={{ width: "20%" }}>
				<Typography variant="h6" component="div">
					Forecasts
				</Typography>
			</TableCell>
			<TableCell sx={{ width: "15%" }}>
				<Typography variant="h6" component="div">
					Projects
				</Typography>
			</TableCell>
			<TableCell sx={{ width: "15%" }}>
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
					teamsWorkIngOnFeature={
						featuresInProgress.includes(feature.workItemReference) ? [team] : []
					}
				/>
			</TableCell>
			<TableCell>
				<ProgressIndicator
					title="Total"
					progressableItem={{
						remainingWork: feature.getRemainingWorkForFeature(),
						totalWork: feature.getTotalWorkForFeature(),
					}}
				/>

				<ProgressIndicator
					title={team.name}
					progressableItem={{
						remainingWork: feature.getRemainingWorkForTeam(team.id),
						totalWork: feature.getTotalWorkForTeam(team.id),
					}}
				/>
			</TableCell>
			<TableCell>
				<ForecastInfoList title={""} forecasts={feature.forecasts} />
			</TableCell>
			<TableCell>
				{Object.entries(feature.projects).map(([projectId, projectName]) => (
					<div key={projectId}>
						<Link to={`/projects/${projectId}`}>{projectName}</Link>
					</div>
				))}
			</TableCell>
			<TableCell>
				<LocalDateTimeDisplay utcDate={feature.lastUpdated} showTime={true} />
			</TableCell>
		</TableRow>
	);

	return (
		<FeatureListBase
			features={team.features}
			renderTableHeader={renderTableHeader}
			renderTableRow={renderTableRow}
		/>
	);
};

export default TeamFeatureList;
