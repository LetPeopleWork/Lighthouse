import { Paper, Stack, TableContainer, Typography } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useContext, useEffect, useMemo, useState } from "react";
import DataGridBase from "../../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../../components/Common/DataGrid/types";
import { createNameColumn } from "../../../../components/Common/FeatureListDataGrid/columns";
import ParentWorkItemCell from "../../../../components/Common/ParentWorkItemCell/ParentWorkItemCell";
import { ErrorSnackbarContext } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import {
	type ParentWorkItem,
	useParentWorkItems,
} from "../../../../hooks/useParentWorkItems";
import type {
	IRefinementRow,
	IRefinementView,
} from "../../../../models/Refinement/Refinement";
import type { Team } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../services/TerminologyContext";

type RefinementGridRow = IRefinementRow & GridValidRowModel;

const NO_ROWS: IRefinementRow[] = [];

const createRefinementColumns = (
	workItemTerm: string,
	parentMap: Map<string, ParentWorkItem>,
): DataGridColumn<RefinementGridRow>[] => [
	createNameColumn<RefinementGridRow>(workItemTerm),
	{
		field: "parentReferenceId",
		headerName: "Parent",
		width: 300,
		sortable: false,
		renderCell: ({ row }) => (
			<ParentWorkItemCell
				parentReference={row.parentReferenceId}
				parentMap={parentMap}
			/>
		),
	},
	{
		field: "state",
		headerName: "State",
		width: 160,
	},
];

interface RefinementViewProps {
	team: Team;
}

const RefinementView: React.FC<Readonly<RefinementViewProps>> = ({ team }) => {
	const { refinementService } = useContext(ApiServiceContext);
	// Read without the throwing hook: the view still renders where no snackbar host is mounted.
	const errorSnackbar = useContext(ErrorSnackbarContext);
	const { getTerm } = useTerminology();
	const [refinement, setRefinement] = useState<IRefinementView | null>(null);

	useEffect(() => {
		let isCurrent = true;

		refinementService
			.getRefinement(team.id)
			.then((answer) => {
				if (isCurrent) {
					setRefinement(answer);
				}
			})
			.catch((error: unknown) => {
				if (isCurrent) {
					errorSnackbar?.showError(
						error instanceof Error ? error.message : String(error),
					);
				}
			});

		return () => {
			isCurrent = false;
		};
	}, [team.id, refinementService, errorSnackbar]);

	const workItems = refinement?.workItems ?? NO_ROWS;
	const parentReferences = useMemo(
		() =>
			workItems.map((workItem) => ({
				parentWorkItemReference: workItem.parentReferenceId,
			})),
		[workItems],
	);
	const parentMap = useParentWorkItems(parentReferences);
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const columns = useMemo(
		() => createRefinementColumns(workItemTerm, parentMap),
		[workItemTerm, parentMap],
	);

	if (refinement === null) {
		return null;
	}

	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);
	const count = workItems.length;

	if (count === 0) {
		return (
			<Typography>
				{`No ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} in ${refinementTerm} states right now`}
			</Typography>
		);
	}

	const workItemsTerm =
		count === 1 ? workItemTerm : getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	return (
		<Stack spacing={2}>
			<Typography variant="h6" component="h2">
				{`${count} ${workItemsTerm} in ${refinementTerm}`}
			</Typography>
			<TableContainer component={Paper}>
				<DataGridBase<RefinementGridRow>
					rows={workItems as RefinementGridRow[]}
					columns={columns}
					idField="referenceId"
					storageKey={`team-refinement-${team.id}`}
				/>
			</TableContainer>
		</Stack>
	);
};

export default RefinementView;
