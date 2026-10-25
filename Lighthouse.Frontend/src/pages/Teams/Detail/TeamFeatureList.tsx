import { Box } from "@mui/material";
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
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import WorkItemsDialog from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { useFeatureList } from "../../../hooks/useFeatureList";
import type { IFeature } from "../../../models/Feature";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";

const NO_FEATURES: IFeature[] = [];

interface FeatureListProps {
	team: Team;
}

const TeamFeatureList: React.FC<FeatureListProps> = ({ team }) => {
	const featureList = useFeatureList({ kind: "team", id: team.id });
	const features = featureList.data?.features ?? NO_FEATURES;
	const { parentsByReferenceId } = featureList;

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	// Stable single-element array so useMemo deps don't change on every render
	const teams = useMemo(() => [team], [team]);

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
						teams={teams}
						overallTitle="Total"
						isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
						onShowDetails={() => Promise.resolve(handleShowFeatureDetails(row))}
					/>
				),
			},
			createParentColumn(parentsByReferenceId),
			createForecastedStartColumn(),
			createForecastsColumn(),
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
		[
			featureTerm,
			teams,
			parentsByReferenceId,
			portfoliosTerm,
			handleShowFeatureDetails,
		],
	);

	const getActiveWorkTeams = useCallback(
		(row: IFeature) =>
			(featureList.data?.activeWork ?? []).some(
				(work) =>
					work.teamId === team.id &&
					work.featureReferenceIds.includes(row.referenceId),
			)
				? [team]
				: [],
		[featureList.data, team],
	);

	return (
		<>
			<FeatureListDataGrid
				features={features}
				columns={columns}
				storageKey={`team-features-${team.id}`}
				hideCompletedStorageKey={`lighthouse_hide_completed_features_team_${team.id}`}
				status={featureList.look}
				// Until an answer has arrived the list is not empty, only unanswered, so it must not claim to be.
				emptyStateMessage={
					featureList.look === "ready"
						? `This ${teamTerm} has no ${featuresTerm} yet.`
						: ""
				}
				allCompletedMessage={(count) =>
					`All ${count} ${featuresTerm} are completed. Turn off Hide Completed ${featuresTerm} to see them.`
				}
				failureMessage={`The ${featuresTerm} couldn't be loaded.`}
				onRetry={() => void featureList.refetch()}
				getActiveWorkTeams={getActiveWorkTeams}
			/>
			{selectedFeature && (
				<WorkItemsDialog
					title={`${getWorkItemName(selectedFeature.name, selectedFeature.referenceId)} ${workItemsTerm}`}
					items={featureWorkItems}
					open
					context="featureChildren"
					ownerKind="team"
					status={featureWorkItemsStatus}
					onClose={handleCloseWorkItemsDialog}
				/>
			)}
		</>
	);
};

export default TeamFeatureList;
