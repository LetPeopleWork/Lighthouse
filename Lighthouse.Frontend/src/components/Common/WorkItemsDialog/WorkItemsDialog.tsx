import BlockIcon from "@mui/icons-material/Block";
import CloseIcon from "@mui/icons-material/Close";
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
import type { IFeature } from "../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import type { AgeBandColumnDescriptor } from "../../../utils/charts/paceBands";
import { formatBlockedSince } from "../../../utils/date/blockedDuration";
import {
	certainColor,
	confidentColor,
	getStateColor,
	hexToRgba,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";
import DataGridBase from "../DataGrid/DataGridBase";
import type { DataGridColumn } from "../DataGrid/types";
import TimeInStateBadge, {
	daysInState,
} from "../TimeInStateBadge/TimeInStateBadge";

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

/**
 * The look shared by every cell whose value carries a judgement: the text takes the colour of that
 * judgement and the cell a tenth-strength wash of it, so a reader spots the bad ones by scanning
 * rather than reading. `plainColor` is what the text falls back to when there is no judgement to
 * show — leave it out to inherit the surrounding text colour.
 */
const judgementCell = (color: string | undefined, plainColor?: string) => ({
	sx: {
		color: color ?? plainColor,
		padding: "4px 8px",
		borderRadius: 1,
		display: "inline-flex",
		alignItems: "center",
	},
	// The wash is inline because it differs per row: a style rule per distinct colour would grow the
	// stylesheet with every value the grid shows.
	style: {
		backgroundColor: color ? hexToRgba(color, 0.1) : "transparent",
	},
});

/**
 * The band column. It lives outside the dialog because it needs nothing from it beyond the
 * descriptor, and because the tie between the sort and the option list below is easier to see when
 * the two sit next to each other rather than buried in a list of six other columns.
 */
const ageBandGridColumn = (
	descriptor: AgeBandColumnDescriptor,
): DataGridColumn<IWorkItem & GridValidRowModel> => ({
	field: "ageBand",
	headerName: descriptor.headerName,
	description: descriptor.description,
	width: 200,
	type: "singleSelect",
	valueOptions: descriptor.optionLabels,
	// The band names are listed from least to most worrying, so a name's position in that list is
	// the order a reader wants. Sorting the words themselves orders them by spelling instead.
	sortComparator: (first, second) =>
		descriptor.optionLabels.indexOf(first as string) -
		descriptor.optionLabels.indexOf(second as string),
	valueGetter: (_, row) => descriptor.bandFor(row),
	renderCell: ({ row }) => {
		const label = descriptor.bandFor(row);
		return (
			<Typography
				variant="body2"
				data-testid="ageBandColumnContent"
				{...judgementCell(descriptor.colorForBand(label), "text.secondary")}
			>
				{label}
			</Typography>
		);
	},
});

const WorkItemsDialog: React.FC<WorkItemsDialogProps> = ({
	title,
	items,
	open,
	onClose,
	highlightColumn = emptyHighlightColumnDefinition,
	timeInStateColumn,
	sle,
	ageBandColumn,
}) => {
	const { getTerm } = useTerminology();
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

		if (timeInStateColumn) {
			baseColumns.push({
				field: "timeInState",
				headerName: "Time in State",
				width: 200,
				sortable: true,
				valueGetter: (_, row) =>
					daysInState(row.currentStateEnteredAt ?? null, timeInStateColumn.now),
				renderCell: ({ row }) => (
					<TimeInStateBadge
						currentStateEnteredAt={row.currentStateEnteredAt ?? null}
						currentStateName={row.state}
						stalenessThresholdDays={timeInStateColumn.stalenessThresholdDays}
						blockedStalenessThresholdDays={
							timeInStateColumn.blockedStalenessThresholdDays
						}
						isBlocked={row.isBlocked}
						blockedSince={row.blockedSince ?? null}
						now={timeInStateColumn.now}
					/>
				),
			});
		}

		return baseColumns;
	}, [
		hasOwningTeams,
		sle,
		workItemTerm,
		blockedTerm,
		isFeature,
		getColumnColor,
		highlightColumn,
		timeInStateColumn,
		ageBandColumn,
	]);

	return (
		<Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
			<DialogTitle sx={{ backgroundColor: "background.paper" }}>
				{title}
				<IconButton
					onClick={onClose}
					sx={{ position: "absolute", right: 8, top: 8 }}
				>
					<CloseIcon />
				</IconButton>
			</DialogTitle>
			<DialogContent sx={{ backgroundColor: "background.paper" }}>
				{items.length > 0 ? (
					<Box sx={{ mt: 2 }}>
						<DataGridBase
							rows={sortedItems as (IWorkItem & GridValidRowModel)[]}
							columns={columns}
							storageKey="work-items-dialog"
							initialSortModel={[
								{
									field: timeInStateColumn ? "timeInState" : "additionalColumn",
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
