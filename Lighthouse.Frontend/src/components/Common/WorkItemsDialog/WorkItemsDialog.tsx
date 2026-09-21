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
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	type AgeBandColumnDescriptor,
	paceBandSortRank,
} from "../../../utils/charts/paceBands";
import {
	type SleRiskColumnDescriptor,
	sleRiskSortValue,
} from "../../../utils/charts/sleRisk";
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
	// Wide enough for "85th-95th", the longest name a band can have. Any wider and it eats into the
	// name column, which is the only one here that flexes.
	width: 130,
	type: "singleSelect",
	valueOptions: descriptor.optionLabels,
	// The band names are listed from least to most worrying, so a name's position in that list is
	// the order a reader wants. Sorting the words themselves orders them by spelling instead.
	//
	// Reversing for a descending click is done here rather than left to the grid, because the grid
	// would reverse the whole order and carry a name the list never carried to the top with it. A
	// band nobody can place belongs at the bottom whichever way round the column is read.
	getSortComparator: (direction) => {
		const unplaced = descriptor.optionLabels.length;
		const worstFirst = direction === "desc" ? -1 : 1;

		return (first, second) => {
			const firstRank = paceBandSortRank(
				first as string,
				descriptor.optionLabels,
			);
			const secondRank = paceBandSortRank(
				second as string,
				descriptor.optionLabels,
			);

			if (firstRank === unplaced || secondRank === unplaced) {
				return firstRank - secondRank;
			}

			return worstFirst * (firstRank - secondRank);
		};
	},
	valueGetter: (_, row) => descriptor.bandFor(row),
	renderCell: ({ value }) => {
		const label = value as string;
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

/**
 * The risk column. It sits beside the band column above for the same reason that one sits outside
 * the dialog: neither needs anything from it but its descriptor, and a reader who meets both in the
 * grid is better served by their definitions being neighbours here too.
 */
const sleRiskGridColumn = (
	descriptor: SleRiskColumnDescriptor,
): DataGridColumn<IWorkItem & GridValidRowModel> => ({
	field: "sleRisk",
	headerName: descriptor.headerName,
	description: descriptor.description,
	// Wide enough that the header, rather than any cell, sets the column's width - every value here
	// is a short percentage.
	width: 130,
	sortable: true,
	// The column's value is what a reader sees and therefore what the export carries — a bare 86 in
	// a file loses what it is 86 of. Ordering reads the number back out of that text, because
	// sorting the text itself would put "32%" above "86%" and "100%" below both.
	//
	// Reversing for a descending click is done here rather than left to the grid, for the same
	// reason the band column does it: an item nothing can be said about is neither the safest nor
	// the worst, and it belongs at the bottom whichever way round the column is read.
	getSortComparator: (direction) => {
		const worstFirst = direction === "desc" ? -1 : 1;

		return (first, second) => {
			const firstRisk = sleRiskSortValue(first as string);
			const secondRisk = sleRiskSortValue(second as string);

			if (firstRisk === undefined || secondRisk === undefined) {
				return (
					(firstRisk === undefined ? 1 : 0) - (secondRisk === undefined ? 1 : 0)
				);
			}

			return worstFirst * (firstRisk - secondRisk);
		};
	},
	valueGetter: (_, row) => descriptor.labelFor(row),
	renderCell: ({ value, row }) => {
		const disclosure = descriptor.disclosureFor(row);
		const cell = (
			<Typography
				variant="body2"
				data-testid="sleRiskColumnContent"
				// An aria-label REPLACES the accessible name rather than adding to it, so the value has
				// to be repeated here. A label carrying only the sentence would take the percentage
				// away from the reader it was written to help.
				aria-label={
					disclosure === undefined
						? undefined
						: `${value as string}. ${disclosure}`
				}
				{...judgementCell(
					descriptor.colorForRisk(descriptor.riskFor(row)),
					"text.secondary",
				)}
			>
				{value as string}
			</Typography>
		);

		// The sentence goes in a tooltip rather than in the cell: the cell's text is the column's
		// value, which is what sorts and what the export carries, and the column is only as wide as
		// its header on purpose. A row the answers never mentioned gets no tooltip, because it makes
		// no claim and so has nothing to disclose.
		return disclosure === undefined ? (
			cell
		) : (
			<Tooltip title={disclosure}>{cell}</Tooltip>
		);
	},
});

/**
 * The warnings column. Outside the dialog for the same reason the two above it are: it needs
 * nothing from the dialog but its descriptor, which arrives carrying the sentences themselves.
 */
const warningsGridColumn = (
	descriptor: WarningsColumnDescriptor,
): DataGridColumn<IWorkItem & GridValidRowModel> => ({
	field: "warnings",
	headerName: descriptor.headerName,
	description: descriptor.description,
	width: 260,
	sortable: true,
	// The sentences themselves are the value, so the export carries what the reader saw. A count
	// would sort the column and tell a reader opening the file nothing about what is wrong.
	valueGetter: (_, row) => descriptor.warningsFor(row).join(" "),
	renderCell: ({ value }) => {
		const warnings = value as string;
		const cell = (
			<Typography
				variant="body2"
				data-testid="warningsColumnContent"
				color={warnings ? "warning.main" : "text.secondary"}
				sx={{ overflow: "hidden", textOverflow: "ellipsis" }}
			>
				{warnings}
			</Typography>
		);

		// The column is narrower than the sentences, so the full text is on hover. A row with
		// nothing wrong gets no tooltip, because an empty one is a pointer that promises a reason
		// and then shows a blank box.
		return warnings ? <Tooltip title={warnings}>{cell}</Tooltip> : cell;
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
	sleRiskColumn,
	warningsColumn,
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
