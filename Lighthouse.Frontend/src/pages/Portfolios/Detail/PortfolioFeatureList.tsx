import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useMemo } from "react";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import {
	createForecastedStartColumn,
	createForecastsColumn,
	createNameColumn,
	createParentColumn,
	createStateColumn,
} from "../../../components/Common/FeatureListDataGrid/columns";
import FeatureListDataGrid from "../../../components/Common/FeatureListDataGrid/FeatureListDataGrid";
import FeatureProgressIndicator from "../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator";
import { useFeatureChildItems } from "../../../components/Common/FeatureListDataGrid/useFeatureChildItems";
import WorkItemsDialog from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { useFeatureList } from "../../../hooks/useFeatureList";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import type { IFeature } from "../../../models/Feature";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";

const NO_FEATURES: IFeature[] = [];

interface PortfolioFeatureListProps {
	portfolio: IPortfolio;
}

const PortfolioFeatureList: React.FC<PortfolioFeatureListProps> = ({
	portfolio,
}) => {
	const featureList = useFeatureList({ kind: "portfolio", id: portfolio.id });
	const features = featureList.data?.features ?? NO_FEATURES;

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);

	const parentMap = useMemo(
		() =>
			new Map<string, ParentWorkItem>(
				(featureList.data?.parents ?? []).map((parent) => [
					parent.referenceId,
					parent,
				]),
			),
		[featureList.data],
	);

	const {
		selectedFeature,
		items: featureWorkItems,
		status: featureWorkItemsStatus,
		show: handleShowFeatureDetails,
		close: handleCloseWorkItemsDialog,
	} = useFeatureChildItems();

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
						onShowDetails={() => Promise.resolve(handleShowFeatureDetails(row))}
					/>
				),
			},
			createParentColumn(parentMap),
			createForecastedStartColumn(),
			createForecastsColumn(),
			createStateColumn(),
		],
		[featureTerm, portfolio.involvedTeams, parentMap, handleShowFeatureDetails],
	);

	const getActiveWorkTeams = useCallback(
		(row: IFeature) => {
			const teamIdsAtWork = new Set(
				(featureList.data?.activeWork ?? [])
					.filter((work) => work.featureReferenceIds.includes(row.referenceId))
					.map((work) => work.teamId),
			);
			return portfolio.involvedTeams.filter((team) =>
				teamIdsAtWork.has(team.id),
			);
		},
		[portfolio.involvedTeams, featureList.data],
	);

	return (
		<>
			<FeatureListDataGrid
				features={features}
				columns={columns}
				storageKey={`portfolio-features-${portfolio.id}`}
				hideCompletedStorageKey={`lighthouse_hide_completed_features_portfolio_${portfolio.id}`}
				status={featureList.look}
				// Until an answer has arrived the list is not empty, only unanswered, so it must not claim to be.
				emptyStateMessage={
					featureList.look === "ready"
						? `This ${portfolioTerm} has no ${featuresTerm} yet.`
						: ""
				}
				allCompletedMessage={(count) =>
					`All ${count} ${featuresTerm} are completed. Turn off Hide Completed ${featuresTerm} to see them.`
				}
				failureMessage={`The ${featuresTerm} couldn't be loaded.`}
				onRetry={() => void featureList.refetch()}
				getActiveWorkTeams={getActiveWorkTeams}
				showPosition
				// A move renumbers the whole instance, so the places every row shows are re-read, not patched.
				onOrderChanged={async () => {
					await featureList.refetch();
				}}
			/>
			{selectedFeature && (
				<WorkItemsDialog
					title={`${getWorkItemName(selectedFeature.name, selectedFeature.referenceId)} ${workItemsTerm}`}
					items={featureWorkItems}
					open
					context="featureChildren"
					ownerKind="portfolio"
					status={featureWorkItemsStatus}
					onClose={handleCloseWorkItemsDialog}
				/>
			)}
		</>
	);
};

export default PortfolioFeatureList;
