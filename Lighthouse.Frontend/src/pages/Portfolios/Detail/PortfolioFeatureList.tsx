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
import FeatureName from "../../../components/Common/FeatureName/FeatureName";
import ProgressIndicator from "../../../components/Common/ProgressIndicator/ProgressIndicator";
import ProgressTitle from "../../../components/Common/ProgressIndicator/ProgressTitle";
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import WorkItemsDialog from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { useParentWorkItems } from "../../../hooks/useParentWorkItems";
import type { IFeature } from "../../../models/Feature";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";

interface PortfolioFeatureListProps {
	portfolio: IPortfolio;
}

const PortfolioFeatureList: React.FC<PortfolioFeatureListProps> = ({
	portfolio,
}) => {
	const { teamMetricsService, featureService } = useContext(ApiServiceContext);

	const [featuresInProgress, setFeaturesInProgress] = useState<
		Record<string, string[]>
	>({});
	const [features, setFeatures] = useState<IFeature[]>([]);
	const [selectedFeature, setSelectedFeature] = useState<IFeature | null>(null);
	const [featureWorkItems, setFeatureWorkItems] = useState<IWorkItem[]>([]);
	const [isWorkItemsDialogOpen, setIsWorkItemsDialogOpen] = useState(false);

	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);

	const parentMap = useParentWorkItems(features);

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
			const featureIds = portfolio.features.map((fr) => fr.id);
			const featureData = await featureService.getFeaturesByIds(featureIds);
			setFeatures(featureData);
		};

		fetchFeatures();
	}, [portfolio.features, featureService]);

	// Fetch features in progress
	useEffect(() => {
		const fetchFeaturesInProgress = async () => {
			const featuresByTeam: Record<string, string[]> = {};

			for (const team of portfolio.involvedTeams) {
				try {
					const features = await teamMetricsService.getFeaturesInProgress(
						team.id,
					);
					featuresByTeam[team.id] = features.map(
						(feature) => feature.referenceId,
					);
				} catch (error) {
					console.error(`Failed to fetch features for team ${team.id}:`, error);
					featuresByTeam[team.id] = [];
				}
			}

			setFeaturesInProgress(featuresByTeam);
		};

		fetchFeaturesInProgress();
	}, [portfolio.involvedTeams, teamMetricsService]);

	// Define columns
	const columns: DataGridColumn<IFeature & GridValidRowModel>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: `${featureTerm} Name`,
				hideable: false,
				width: 300,
				flex: 1,
				renderCell: ({ row }) => (
					<FeatureName
						name={getWorkItemName(row)}
						url={row.url ?? ""}
						isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
						teamsWorkIngOnFeature={portfolio.involvedTeams.filter((team) =>
							featuresInProgress[team.id]?.includes(row.referenceId),
						)}
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
							title={
								<ProgressTitle
									title="Overall Progress"
									isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
									onShowDetails={async () =>
										await handleShowFeatureDetails(row)
									}
								/>
							}
							progressableItem={{
								remainingWork: row.getRemainingWorkForFeature(),
								totalWork: row.getTotalWorkForFeature(),
							}}
						/>
						{portfolio.involvedTeams
							.filter((team) => row.getTotalWorkForTeam(team.id) > 0)
							.map((team) => (
								<Box key={team.id}>
									<ProgressIndicator
										title={
											<StyledLink to={`/teams/${team.id}`}>
												{team.name}
											</StyledLink>
										}
										progressableItem={{
											remainingWork: row.getRemainingWorkForTeam(team.id),
											totalWork: row.getTotalWorkForTeam(team.id),
										}}
									/>
								</Box>
							))}
					</Box>
				),
			},
			createParentColumn(parentMap),
			createForecastsColumn(),
			createStateColumn(),
		],
		[
			featureTerm,
			portfolio.involvedTeams,
			featuresInProgress,
			parentMap,
			handleShowFeatureDetails,
		],
	);

	return (
		<>
			<FeatureListDataGrid
				features={features}
				columns={columns}
				storageKey={`portfolio-features-${portfolio.id}`}
				hideCompletedStorageKey={`lighthouse_hide_completed_features_portfolio_${portfolio.id}`}
				loading={features.length === 0}
			/>
			{selectedFeature && (
				<WorkItemsDialog
					title={`${getWorkItemName(selectedFeature)} Stories`}
					items={featureWorkItems}
					open={isWorkItemsDialogOpen}
					onClose={handleCloseWorkItemsDialog}
				/>
			)}
		</>
	);
};

export default PortfolioFeatureList;
