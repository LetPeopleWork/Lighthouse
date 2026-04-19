import { Box } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import {
	createForecastsColumn,
	createParentColumn,
	createStateColumn,
} from "../../../components/Common/FeatureListDataGrid/columns";
import FeatureListDataGrid from "../../../components/Common/FeatureListDataGrid/FeatureListDataGrid";
import FeatureProgressIndicator from "../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator";
import FeatureName from "../../../components/Common/FeatureName/FeatureName";
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import WorkItemsDialog from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { useParentWorkItems } from "../../../hooks/useParentWorkItems";
import type { IFeature } from "../../../models/Feature";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
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
	const [selectedFeature, setSelectedFeature] = useState<IFeature | null>(null);
	const [featureWorkItems, setFeatureWorkItems] = useState<IWorkItem[]>([]);
	const [isWorkItemsDialogOpen, setIsWorkItemsDialogOpen] = useState(false);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);

	const parentMap = useParentWorkItems(features);

	// Stable single-element array so useMemo deps don't change on every render
	const teams = useMemo(() => [team], [team]);

	const handleShowFeatureDetails = useCallback(
		async (feature: IFeature) => {
			setSelectedFeature(feature);
			setFeatureWorkItems([]);
			setIsWorkItemsDialogOpen(true);

			const items = await featureService.getFeatureWorkItems(feature.id);
			setFeatureWorkItems(items);
		},
		[featureService],
	);

	const handleCloseWorkItemsDialog = () => {
		setIsWorkItemsDialogOpen(false);
		setSelectedFeature(null);
	};

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
			const inProgress = await teamMetricsService.getFeaturesInProgress(
				team.id,
				new Date(),
			);
			setFeaturesInProgress(inProgress.map((feature) => feature.referenceId));
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
						name={getWorkItemName(row.name, row.referenceId)}
						url={row.url ?? ""}
					/>
				),
			},
			{
				field: "progress",
				headerName: "Progress",
				width: 400,
				sortable: false,
				renderCell: ({ row }) => (
					<FeatureProgressIndicator
						feature={row}
						teams={teams}
						overallTitle="Total"
						isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
						onShowDetails={async () => handleShowFeatureDetails(row)}
					/>
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
		[featureTerm, teams, parentMap, portfoliosTerm, handleShowFeatureDetails],
	);

	const getActiveWorkTeams = useCallback(
		(row: IFeature) =>
			featuresInProgress.includes(row.referenceId) ? [team] : [],
		[featuresInProgress, team],
	);

	return (
		<>
			<FeatureListDataGrid
				features={features}
				columns={columns}
				storageKey={`team-features-${team.id}`}
				hideCompletedStorageKey={`lighthouse_hide_completed_features_team_${team.id}`}
				loading={features.length === 0}
				getActiveWorkTeams={getActiveWorkTeams}
			/>
			{selectedFeature && (
				<WorkItemsDialog
					title={`${getWorkItemName(selectedFeature.name, selectedFeature.referenceId)} ${workItemsTerm}`}
					items={featureWorkItems}
					open={isWorkItemsDialogOpen}
					onClose={handleCloseWorkItemsDialog}
				/>
			)}
		</>
	);
};

export default TeamFeatureList;
