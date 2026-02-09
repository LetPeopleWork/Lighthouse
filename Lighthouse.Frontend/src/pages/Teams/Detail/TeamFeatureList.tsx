import { Box } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useContext, useEffect, useMemo, useState } from "react";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import {
	createForecastsColumn,
	createParentColumn,
	createStateColumn,
} from "../../../components/Common/FeatureListDataGrid/columns";
import FeatureListDataGrid from "../../../components/Common/FeatureListDataGrid/FeatureListDataGrid";
import FeatureName from "../../../components/Common/FeatureName/FeatureName";
import ProgressIndicator from "../../../components/Common/ProgressIndicator/ProgressIndicator";
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import { useParentWorkItems } from "../../../hooks/useParentWorkItems";
import type { IFeature } from "../../../models/Feature";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";

interface FeatureListProps {
	team: Team;
}

const TeamFeatureList: React.FC<FeatureListProps> = ({ team }) => {
	const { teamMetricsService, featureService } = useContext(ApiServiceContext);

	const [featuresInProgress, setFeaturesInProgress] = useState<string[]>([]);
	const [features, setFeatures] = useState<IFeature[]>([]);

	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);

	const parentMap = useParentWorkItems(features);

	// Load features
	useEffect(() => {
		const fetchFeatures = async () => {
			const featureIds = team.features.map((fr) => fr.id);
			const featureData = await featureService.getFeaturesByIds(featureIds);
			setFeatures(featureData);
		};

		fetchFeatures();
	}, [team.features, featureService]);

	// Fetch features in progress
	useEffect(() => {
		const fetchFeaturesInProgress = async () => {
			const features = await teamMetricsService.getFeaturesInProgress(team.id);
			setFeaturesInProgress(features.map((feature) => feature.referenceId));
		};

		fetchFeaturesInProgress();
	}, [team, teamMetricsService]);

	// Define columns
	const columns: DataGridColumn<IFeature & GridValidRowModel>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: `${featureTerm} Name`,
				width: 300,
				flex: 1,
				hideable: false,
				renderCell: ({ row }) => (
					<FeatureName
						name={getWorkItemName(row)}
						url={row.url ?? ""}
						isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
						teamsWorkIngOnFeature={
							featuresInProgress.includes(row.referenceId) ? [team] : []
						}
					/>
				),
			},
			{
				field: "progress",
				headerName: "Progress",
				width: 400,
				sortable: false,
				renderCell: ({ row }) => (
					<Box sx={{ width: "100%" }}>
						<ProgressIndicator
							title="Total"
							progressableItem={{
								remainingWork: row.getRemainingWorkForFeature(),
								totalWork: row.getTotalWorkForFeature(),
							}}
						/>
						<ProgressIndicator
							title={team.name}
							progressableItem={{
								remainingWork: row.getRemainingWorkForTeam(team.id),
								totalWork: row.getTotalWorkForTeam(team.id),
							}}
						/>
					</Box>
				),
			},
			createForecastsColumn(),
			createParentColumn(parentMap),
			{
				field: "projects",
				headerName: portfoliosTerm,
				width: 200,
				sortable: false,
				renderCell: ({ row }) => (
					<Box>
						{row.projects.map((project) => (
							<Box key={project.id}>
								<StyledLink to={`/portfolios/${project.id}`}>
									{project.name}
								</StyledLink>
							</Box>
						))}
					</Box>
				),
			},
			createStateColumn(),
		],
		[featureTerm, team, featuresInProgress, parentMap, portfoliosTerm],
	);

	return (
		<FeatureListDataGrid
			features={features}
			columns={columns}
			storageKey={`team-features-${team.id}`}
			hideCompletedStorageKey={`lighthouse_hide_completed_features_team_${team.id}`}
			loading={features.length === 0}
		/>
	);
};

export default TeamFeatureList;
