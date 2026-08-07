import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import {
	createForecastsColumn,
	createNameColumn,
	createParentColumn,
	createStateColumn,
} from "../../../components/Common/FeatureListDataGrid/columns";
import FeatureListDataGrid from "../../../components/Common/FeatureListDataGrid/FeatureListDataGrid";
import FeatureProgressIndicator from "../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator";
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
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
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
	const fetchFeatures = useCallback(async () => {
		const featureIds = portfolio.features.map((fr) => fr.id);
		const featureData = await featureService.getFeaturesByIds(featureIds);
		setFeatures(featureData);
	}, [portfolio.features, featureService]);

	useEffect(() => {
		fetchFeatures();
	}, [fetchFeatures]);

	// Fetch features in progress
	useEffect(() => {
		const fetchFeaturesInProgress = async () => {
			const featuresByTeam: Record<string, string[]> = {};

			for (const team of portfolio.involvedTeams) {
				try {
					const inProgress = await teamMetricsService.getFeaturesInProgress(
						team.id,
						new Date(),
					);
					featuresByTeam[team.id] = inProgress.map(
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

	const columns: DataGridColumn<IFeature & GridValidRowModel>[] = useMemo(
		() => [
			createNameColumn(featureTerm),
			{
				field: "progress",
				headerName: "Progress",
				width: 400,
				sortable: false,
				renderCell: ({ row }) => (
					<FeatureProgressIndicator
						feature={row}
						teams={portfolio.involvedTeams}
						isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
						onShowDetails={async () => handleShowFeatureDetails(row)}
					/>
				),
			},
			createParentColumn(parentMap),
			createForecastsColumn(),
			createStateColumn(),
		],
		[featureTerm, portfolio.involvedTeams, parentMap, handleShowFeatureDetails],
	);

	const getActiveWorkTeams = useCallback(
		(row: IFeature) =>
			portfolio.involvedTeams.filter((team) =>
				featuresInProgress[team.id]?.includes(row.referenceId),
			),
		[portfolio.involvedTeams, featuresInProgress],
	);

	return (
		<>
			<FeatureListDataGrid
				features={features}
				columns={columns}
				storageKey={`portfolio-features-${portfolio.id}`}
				hideCompletedStorageKey={`lighthouse_hide_completed_features_portfolio_${portfolio.id}`}
				loading={features.length === 0}
				getActiveWorkTeams={getActiveWorkTeams}
				showPosition
				// A move renumbers the whole instance, so the places every row shows are re-read, not patched.
				onOrderChanged={fetchFeatures}
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

export default PortfolioFeatureList;
