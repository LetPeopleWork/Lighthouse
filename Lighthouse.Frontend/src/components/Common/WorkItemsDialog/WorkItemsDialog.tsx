import CloseIcon from "@mui/icons-material/Close";
import CloseFullscreenIcon from "@mui/icons-material/CloseFullscreen";
import OpenInFullIcon from "@mui/icons-material/OpenInFull";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import {
	Box,
	Chip,
	Dialog,
	DialogContent,
	DialogTitle,
	IconButton,
	Link,
	Tooltip,
	Typography,
} from "@mui/material";
import {
	GridLoadingOverlay,
	type GridLoadingOverlayProps,
	type GridValidRowModel,
} from "@mui/x-data-grid";
import { useMemo } from "react";
import { useEnlargedWorkItemsDialog } from "../../../hooks/useEnlargedWorkItemsDialog";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import type { AgeBandColumnDescriptor } from "../../../utils/charts/paceBands";
import type { SleRiskColumnDescriptor } from "../../../utils/charts/sleRisk";
import { getStateColor } from "../../../utils/theme/colors";
import DataGridBase from "../DataGrid/DataGridBase";
import type { DataGridColumn } from "../DataGrid/types";
import { daysInState } from "../TimeInStateBadge/TimeInStateBadge";
import { contextGridLayout, isFeatureRow } from "./workItemColumns";
import type {
	WorkItemsDialogContextId,
	WorkItemsDialogOwnerKind,
} from "./workItemsDialogContexts";

export interface WorkItemsDialogProps {
	title: string;
	items: IWorkItem[];
	open: boolean;
	onClose: () => void;
	timeInStateColumn?: TimeInStateColumnDefinition;
	sle?: number;
	/**
	 * Given one, the dialog draws a column naming which pace band each row's age falls into. The
	 * descriptor already knows how to answer that for an item, so the dialog never learns what a
	 * percentile is; without one, nothing about the dialog changes.
	 */
	ageBandColumn?: AgeBandColumnDescriptor;
	/**
	 * Given one, the dialog draws a column naming each row's chance of missing the team's target. As
	 * with the band above, the descriptor already knows how to answer that for an item, so the dialog
	 * never learns what a cycle time is; without one, nothing about the dialog changes.
	 */
	sleRiskColumn?: SleRiskColumnDescriptor;
	/**
	 * Given one, the dialog draws a column listing what is worth checking about each row. The
	 * descriptor arrives with the sentences already written, so the dialog never learns what a
	 * dependency is; without one, nothing about the dialog changes.
	 */
	warningsColumn?: WarningsColumnDescriptor;
	/**
	 * What was clicked to open the dialog: it opens with the columns that explain that point and sorts
	 * by the one it plots.
	 */
	context: WorkItemsDialogContextId;
	/** Whose items these are; a Team's and a Portfolio's dialog for the same chart open alike. */
	ownerKind?: WorkItemsDialogOwnerKind;
	/**
	 * Whether the items are still on their way or could not be loaded, for a dialog that opens
	 * before its items arrive. Left out, the items given are the answer.
	 */
	status?: WorkItemsDialogStatus;
	/** The day Age is measured on, when the rows were picked on a day in the past. */
	ageOn?: Date;
	/** The named cycle time the Cycle Time column reads, by definition id. */
	cycleTimeScope?: number;
	/** The named cycle times offered as columns of their own; without them none is offered. */
	namedCycleTimeDefinitions?: INamedCycleTimeDefinition[];
	/** How many days each row added to the state that was clicked. */
	daysContributedColumn?: DaysContributedColumnDescriptor;
}

export type WorkItemsDialogStatus = "loading" | "error" | "ready";

export interface DaysContributedColumnDescriptor {
	daysFor: (workItem: IWorkItem) => number;
}

export interface WarningsColumnDescriptor {
	headerName: string;
	description: string;
	warningsFor: (workItem: IWorkItem) => string[];
}

export interface TimeInStateColumnDefinition {
	now?: Date;
	stalenessThresholdDays?: number;
	blockedStalenessThresholdDays?: number;
}

// With no rows yet, the grid's own loading look is a skeleton of blank rows, which reads like a
// table with nothing in it. A spinner says the items are still on their way.
const ItemsOnTheirWayOverlay = (props: GridLoadingOverlayProps) => (
	<GridLoadingOverlay
		{...props}
		variant="circular-progress"
		noRowsVariant="circular-progress"
	/>
);

const gridSlots = { loadingOverlay: ItemsOnTheirWayOverlay };

function layoutStorageKey(
	context: WorkItemsDialogContextId,
	ownerKind: WorkItemsDialogOwnerKind | undefined,
): string {
	return ownerKind === undefined
		? `work-items-dialog:${context}`
		: `work-items-dialog:${context}:${ownerKind}`;
}

const WorkItemsDialog: React.FC<WorkItemsDialogProps> = ({
	title,
	items,
	open,
	onClose,
	timeInStateColumn,
	sle,
	ageBandColumn,
	sleRiskColumn,
	warningsColumn,
	context,
	ownerKind,
	status = "ready",
	ageOn,
	cycleTimeScope,
	namedCycleTimeDefinitions,
	daysContributedColumn,
}) => {
	const { getTerm } = useTerminology();
	const { enlarged, toggleEnlarged } = useEnlargedWorkItemsDialog();
	const hasOwningTeams = items.some(
		(item) =>
			isFeatureRow(item) && item.owningTeam && item.owningTeam.trim() !== "",
	);

	const daysInStateOf = (workItem: IWorkItem): number =>
		daysInState(workItem.currentStateEnteredAt ?? null, timeInStateColumn?.now);

	const sortedItems = timeInStateColumn
		? [...items].sort((a, b) => daysInStateOf(b) - daysInStateOf(a))
		: items;

	const parentReferences = useMemo(
		() =>
			[
				...new Set(
					items
						.map((item) => item.parentWorkItemReference)
						.filter((reference) => reference),
				),
			].sort((a, b) => a.localeCompare(b)),
		[items],
	);

	const contextLayout = useMemo(
		() =>
			contextGridLayout(context, items, {
				getTerm,
				parentReferences,
				timeInState: timeInStateColumn,
				sle,
				ageOn,
				cycleTimeScope,
				namedCycleTimeDefinitions,
				daysContributed: daysContributedColumn,
				ageBand: ageBandColumn,
				sleRisk: sleRiskColumn,
				warnings: warningsColumn,
			}),
		[
			context,
			items,
			getTerm,
			parentReferences,
			timeInStateColumn,
			sle,
			ageOn,
			cycleTimeScope,
			namedCycleTimeDefinitions,
			daysContributedColumn,
			ageBandColumn,
			sleRiskColumn,
			warningsColumn,
		],
	);

	const storageKey = layoutStorageKey(context, ownerKind);

	const columns = useMemo(() => {
		const baseColumns: DataGridColumn<IWorkItem & GridValidRowModel>[] = [
			{
				field: "referenceId",
				headerName: "ID",
				width: 120,
				renderCell: ({ row }) => {
					return row.name?.toLowerCase().includes("unparented")
						? ""
						: row.referenceId;
				},
			},
			{
				field: "name",
				headerName: "Name",
				width: 300,
				// The only flexing column, so it absorbs whatever the fixed ones leave. With enough of
				// them the remainder falls below a readable width and every name wraps into a tall
				// stack; a floor turns that into a horizontal scrollbar instead.
				minWidth: 200,
				hideable: false,
				flex: 1,
				renderCell: ({ row }) => {
					if (row.url) {
						return (
							<Link href={row.url} target="_blank" rel="noopener noreferrer">
								{row.name}
							</Link>
						);
					}
					return row.name;
				},
			},
			{
				field: "type",
				headerName: "Type",
				width: 120,
			},
			{
				field: "state",
				headerName: "State",
				width: 150,
				renderCell: ({ row }) => (
					<Chip
						size="small"
						label={row.state}
						color={getStateColor(row.stateCategory)}
						variant="outlined"
					/>
				),
			},
		];

		if (hasOwningTeams) {
			baseColumns.push({
				field: "owningTeam",
				headerName: "Owned by",
				width: 150,
				renderCell: ({ row }) => {
					return isFeatureRow(row) ? row.owningTeam : "";
				},
			});
		}

		baseColumns.push(...contextLayout.columns);

		return baseColumns;
	}, [hasOwningTeams, contextLayout]);

	return (
		<Dialog
			open={open}
			onClose={onClose}
			fullWidth
			maxWidth="xl"
			fullScreen={enlarged}
		>
			<DialogTitle sx={{ backgroundColor: "background.paper" }}>
				{title}
				<IconButton
					onClick={onClose}
					aria-label="Close"
					sx={{ position: "absolute", right: 8, top: 8 }}
				>
					<CloseIcon />
				</IconButton>
				<Tooltip title={enlarged ? "Restore size" : "Enlarge"}>
					<IconButton
						onClick={toggleEnlarged}
						aria-label={enlarged ? "Restore size" : "Enlarge"}
						sx={{ position: "absolute", right: 48, top: 8 }}
					>
						{enlarged ? <CloseFullscreenIcon /> : <OpenInFullIcon />}
					</IconButton>
				</Tooltip>
			</DialogTitle>
			<DialogContent sx={{ backgroundColor: "background.paper" }}>
				{status === "error" && (
					<Box sx={{ display: "flex", alignItems: "center", gap: 1, mt: 2 }}>
						<WarningAmberIcon color="warning" fontSize="small" />
						<Typography variant="body2" color="text.secondary">
							{`These ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} couldn't be loaded. Close and reopen to try again.`}
						</Typography>
					</Box>
				)}
				{status !== "error" &&
					(items.length > 0 || status === "loading" ? (
						<Box sx={{ mt: 2 }}>
							<DataGridBase
								// The grid reads its saved layout only when it mounts, so a dialog switched to
								// another context while open needs a fresh grid to pick up that context's layout.
								key={storageKey}
								rows={sortedItems as (IWorkItem & GridValidRowModel)[]}
								columns={columns}
								storageKey={storageKey}
								defaultColumnVisibilityModel={contextLayout.visibilityModel}
								initialSortModel={[
									{ field: contextLayout.sortBy, sort: "desc" as const },
								]}
								loading={status === "loading"}
								slots={gridSlots}
								enableExport={true}
								exportFileName={title.replaceAll(/\s+/g, "_")}
							/>
						</Box>
					) : (
						<Typography variant="body2" color="text.secondary">
							No items to display
						</Typography>
					))}
			</DialogContent>
		</Dialog>
	);
};

export default WorkItemsDialog;
