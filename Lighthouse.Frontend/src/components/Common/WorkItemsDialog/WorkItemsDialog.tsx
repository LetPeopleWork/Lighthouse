import BlockIcon from "@mui/icons-material/Block";
import CloseIcon from "@mui/icons-material/Close";
import CloseFullscreenIcon from "@mui/icons-material/CloseFullscreen";
import OpenInFullIcon from "@mui/icons-material/OpenInFull";
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
import type { GridValidRowModel } from "@mui/x-data-grid";
import { useCallback, useMemo } from "react";
import { useEnlargedWorkItemsDialog } from "../../../hooks/useEnlargedWorkItemsDialog";
import type { IFeature } from "../../../models/Feature";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import type { AgeBandColumnDescriptor } from "../../../utils/charts/paceBands";
import type { SleRiskColumnDescriptor } from "../../../utils/charts/sleRisk";
import { formatBlockedSince } from "../../../utils/date/blockedDuration";
import {
	certainColor,
	confidentColor,
	getStateColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";
import DataGridBase from "../DataGrid/DataGridBase";
import type { DataGridColumn } from "../DataGrid/types";
import { daysInState } from "../TimeInStateBadge/TimeInStateBadge";
import {
	ageBandGridColumn,
	contextGridLayout,
	judgementCell,
	sleRiskGridColumn,
	timeInStateGridColumn,
	warningsGridColumn,
} from "./workItemColumns";
import type {
	WorkItemsDialogContextId,
	WorkItemsDialogOwnerKind,
} from "./workItemsDialogContexts";

export interface WorkItemsDialogProps {
	title: string;
	items: IWorkItem[];
	open: boolean;
	onClose: () => void;
	highlightColumn?: HighlightColumnDefinition;
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
	 * What was clicked to open the dialog. Given one, the dialog opens with the columns that explain
	 * that chart's point and sorts by the one it plots; without one, it shows today's columns.
	 */
	context?: WorkItemsDialogContextId;
	/** Whose items these are; a Team's and a Portfolio's dialog for the same chart open alike. */
	ownerKind?: WorkItemsDialogOwnerKind;
	// The props below are declared for the per-context columns and are not read yet.
	status?: WorkItemsDialogStatus;
	/** The day Age is measured on, when the rows were picked on a day in the past. */
	ageOn?: Date;
	/** The named cycle time the Cycle Time column reads, by definition id. */
	cycleTimeScope?: number;
	namedCycleTimeDefinitions?: INamedCycleTimeDefinition[];
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

export interface HighlightColumnDefinition {
	title: string;
	description: string;
	valueGetter: (workItem: IWorkItem) => number;
}

export interface TimeInStateColumnDefinition {
	now?: Date;
	stalenessThresholdDays?: number;
	blockedStalenessThresholdDays?: number;
}

const emptyHighlightColumnDefinition: HighlightColumnDefinition = {
	title: "",
	description: "",
	valueGetter: () => 0,
};

const WorkItemsDialog: React.FC<WorkItemsDialogProps> = ({
	title,
	items,
	open,
	onClose,
	highlightColumn = emptyHighlightColumnDefinition,
	timeInStateColumn,
	sle,
	ageBandColumn,
	sleRiskColumn,
	warningsColumn,
	context,
}) => {
	const { getTerm } = useTerminology();
	const { enlarged, toggleEnlarged } = useEnlargedWorkItemsDialog();
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	const isFeature = useCallback((item: IWorkItem): item is IFeature => {
		return "owningTeam" in item;
	}, []);

	const hasOwningTeams = items.some(
		(item) =>
			isFeature(item) && item.owningTeam && item.owningTeam.trim() !== "",
	);

	const sortValueOf = (workItem: IWorkItem): number => {
		if (timeInStateColumn) {
			return daysInState(
				workItem.currentStateEnteredAt ?? null,
				timeInStateColumn.now,
			);
		}
		return highlightColumn.valueGetter(workItem);
	};

	const sortedItems = [...items].sort((a, b) => {
		return sortValueOf(b) - sortValueOf(a);
	});

	const getColumnColor = useCallback(
		(value: number) => {
			if (!sle) return undefined;

			const seventyPercentSLE = sle * 0.7;
			const fiftyPercentSLE = sle * 0.5;

			if (value > sle) {
				return riskyColor;
			}
			if (value >= seventyPercentSLE) {
				return realisticColor;
			}
			if (value >= fiftyPercentSLE) {
				return confidentColor;
			}
			return certainColor;
		},
		[sle],
	);

	const contextLayout = useMemo(
		() => contextGridLayout(context, items, getTerm, timeInStateColumn),
		[context, items, getTerm, timeInStateColumn],
	);

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
					return isFeature(row) ? row.owningTeam : "";
				},
			});
		}

		baseColumns.push(...contextLayout.columns);

		if (highlightColumn.title) {
			baseColumns.push({
				field: "additionalColumn",
				headerName: `${highlightColumn.title} (${highlightColumn.description})`,
				width: 200,
				sortable: true,
				valueGetter: (_, row) => highlightColumn.valueGetter(row),
				renderCell: ({ row }) => {
					const value = highlightColumn.valueGetter(row);
					const treatment = judgementCell(getColumnColor(value));
					return (
						<Typography
							variant="body2"
							data-testid="additionalColumnContent"
							style={treatment.style}
							sx={{
								...treatment.sx,
								fontWeight: sle ? "bold" : "normal",
							}}
						>
							{value}
							{row.isBlocked && (
								<Tooltip
									title={(() => {
										const duration = row.blockedSince
											? formatBlockedSince(row.blockedSince)
											: null;
										return duration
											? `Blocked for ${duration}`
											: `This ${workItemTerm} is ${blockedTerm}`;
									})()}
								>
									<BlockIcon
										sx={{
											color: "error.main",
											fontSize: "1rem",
											ml: 1,
										}}
									/>
								</Tooltip>
							)}
						</Typography>
					);
				},
			});
		}

		if (ageBandColumn) {
			baseColumns.push(ageBandGridColumn(ageBandColumn));
		}

		if (sleRiskColumn) {
			baseColumns.push(sleRiskGridColumn(sleRiskColumn));
		}

		if (warningsColumn) {
			baseColumns.push(warningsGridColumn(warningsColumn));
		}

		// A dialog opened from a context already has Time in State among its catalogue columns.
		if (timeInStateColumn && context === undefined) {
			baseColumns.push(timeInStateGridColumn(timeInStateColumn));
		}

		return baseColumns;
	}, [
		hasOwningTeams,
		context,
		contextLayout,
		sle,
		workItemTerm,
		blockedTerm,
		isFeature,
		getColumnColor,
		highlightColumn,
		timeInStateColumn,
		ageBandColumn,
		sleRiskColumn,
		warningsColumn,
	]);

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
				{items.length > 0 ? (
					<Box sx={{ mt: 2 }}>
						<DataGridBase
							rows={sortedItems as (IWorkItem & GridValidRowModel)[]}
							columns={columns}
							storageKey="work-items-dialog"
							defaultColumnVisibilityModel={contextLayout.visibilityModel}
							initialSortModel={[
								{
									field:
										contextLayout.sortBy ??
										(timeInStateColumn ? "timeInState" : "additionalColumn"),
									sort: "desc" as const,
								},
							]}
							enableExport={true}
							exportFileName={title.replaceAll(/\s+/g, "_")}
						/>
					</Box>
				) : (
					<Typography variant="body2" color="text.secondary">
						No items to display
					</Typography>
				)}
			</DialogContent>
		</Dialog>
	);
};

export default WorkItemsDialog;
