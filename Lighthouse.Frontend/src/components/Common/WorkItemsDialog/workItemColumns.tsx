import { Tooltip, Typography } from "@mui/material";
import {
	type GridSortCellParams,
	type GridValidRowModel,
	gridNumberComparator,
} from "@mui/x-data-grid";
import type { IFeature } from "../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import {
	type AgeBandColumnDescriptor,
	paceBandSortRank,
} from "../../../utils/charts/paceBands";
import {
	type SleRiskColumnDescriptor,
	sleRiskSortValue,
} from "../../../utils/charts/sleRisk";
import { formatLocalDate } from "../../../utils/date/localDate";
import { hexToRgba } from "../../../utils/theme/colors";
import type { ColumnVisibilityModel, DataGridColumn } from "../DataGrid/types";
import { createForecastsColumn } from "../FeatureListDataGrid/columns";
import ParentWorkItemCell from "../ParentWorkItemCell/ParentWorkItemCell";
import TimeInStateBadge, {
	daysInState,
} from "../TimeInStateBadge/TimeInStateBadge";
import type {
	TimeInStateColumnDefinition,
	WarningsColumnDescriptor,
} from "./WorkItemsDialog";
import {
	type ColumnId,
	WORK_ITEMS_DIALOG_CONTEXTS,
	type WorkItemsDialogContextId,
} from "./workItemsDialogContexts";

type WorkItemRow = IWorkItem & GridValidRowModel;

export interface CatalogueColumnSettings {
	getTerm: (key: string) => string;
	/** The column the rows open sorted by; its cells keep the test id the dialog's sort column has always had. */
	sortBy?: ColumnId;
	/** When a row's time in its state is measured to, and when that time counts as stale. */
	timeInState?: TimeInStateColumnDefinition;
}

interface CatalogueEntry {
	id: ColumnId;
	offeredFor: (items: readonly IWorkItem[]) => boolean;
	build: (settings: CatalogueColumnSettings) => DataGridColumn<WorkItemRow>;
}

const always = () => true;

const isFeatureRow = (item: IWorkItem): item is IFeature =>
	"owningTeam" in item;

const featureRows = (items: readonly IWorkItem[]) => items.some(isFeatureRow);

// Feature rows carry no blocked-since date of their own, so only Work Item rows offer one.
const workItemRows = (items: readonly IWorkItem[]) => !featureRows(items);

const NO_PARENT_NAMES = new Map();

// The rows come straight from the server's JSON, so a date may still be a string, and a date the
// item does not have arrives as null or as the epoch.
const asLocalDay = (value: Date | string | null | undefined): string => {
	if (value === null || value === undefined) return "";
	const date = new Date(value);
	return date.getTime() > 0 ? formatLocalDate(date) : "";
};

const dayColumn = (
	field: ColumnId,
	headerName: string,
	dayOf: (row: IWorkItem) => Date | string | null | undefined,
): DataGridColumn<WorkItemRow> => ({
	field,
	headerName,
	width: 130,
	sortable: true,
	valueGetter: (_, row) => asLocalDay(dayOf(row)),
});

const numberColumn = (
	field: ColumnId,
	headerName: string,
	numberFor: (row: IWorkItem) => number,
	{ sortBy }: CatalogueColumnSettings,
): DataGridColumn<WorkItemRow> => ({
	field,
	headerName,
	width: 130,
	sortable: true,
	valueGetter: (_, row) => numberFor(row),
	renderCell: ({ value }) => (
		<Typography
			variant="body2"
			data-testid={field === sortBy ? "additionalColumnContent" : undefined}
		>
			{value as number}
		</Typography>
	),
});

const ageOrCycleTimeOf = (row: IWorkItem): number =>
	row.stateCategory === "Done" ? row.cycleTime : row.workItemAge;

const parentColumn = (): DataGridColumn<WorkItemRow> => ({
	field: "parent",
	headerName: "Parent",
	width: 200,
	sortable: true,
	valueGetter: (_, row) => row.parentWorkItemReference,
	renderCell: ({ row }) => (
		<ParentWorkItemCell
			parentReference={row.parentWorkItemReference}
			parentMap={NO_PARENT_NAMES}
		/>
	),
});

const plottedEstimateOf = (params: unknown): number | null => {
	const { id, api } = params as GridSortCellParams;
	return (api.getRow(id) as IWorkItem).estimate?.value ?? null;
};

// The cell and the export carry what the chart labels the item with; the order follows the value it
// plots it at, which is how category estimates keep the owner's order rather than the alphabet's.
const estimateColumn = (): DataGridColumn<WorkItemRow> => ({
	field: "estimate",
	headerName: "Estimate",
	width: 120,
	sortable: true,
	valueGetter: (_, row) => row.estimate?.displayValue ?? "",
	sortComparator: (_first, _second, firstParams, secondParams) =>
		gridNumberComparator(
			plottedEstimateOf(firstParams),
			plottedEstimateOf(secondParams),
			firstParams as GridSortCellParams,
			secondParams as GridSortCellParams,
		),
});

// The Features grid's own column, so a forecast reads the same in the dialog as in the list it came from.
const forecastColumn = (): DataGridColumn<WorkItemRow> => {
	const { field, headerName, width, sortable, renderCell } =
		createForecastsColumn();
	return {
		field,
		headerName,
		width,
		sortable,
		renderCell: ({ row, value }) =>
			isFeatureRow(row) ? renderCell?.({ row, value }) : null,
	};
};

/** Every catalogue column, in the one order Manage columns lists those a context does not show. */
const CATALOGUE: readonly CatalogueEntry[] = [
	{
		id: "startedDate",
		offeredFor: always,
		build: () => dayColumn("startedDate", "Started", (row) => row.startedDate),
	},
	{
		id: "closedDate",
		offeredFor: always,
		build: () => dayColumn("closedDate", "Closed", (row) => row.closedDate),
	},
	{
		id: "cycleTime",
		offeredFor: always,
		build: (settings) =>
			numberColumn(
				"cycleTime",
				settings.getTerm(TERMINOLOGY_KEYS.CYCLE_TIME),
				(row) => row.cycleTime,
				settings,
			),
	},
	{
		id: "workItemAge",
		offeredFor: always,
		build: (settings) =>
			numberColumn(
				"workItemAge",
				settings.getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE),
				(row) => row.workItemAge,
				settings,
			),
	},
	{
		id: "ageOrCycleTime",
		offeredFor: always,
		build: (settings) =>
			numberColumn(
				"ageOrCycleTime",
				`${settings.getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE)} / ${settings.getTerm(TERMINOLOGY_KEYS.CYCLE_TIME)}`,
				ageOrCycleTimeOf,
				settings,
			),
	},
	{
		id: "estimate",
		// An older server sends no estimate at all, and one whose owner has no estimation set up sends
		// null; both mean there is nothing for this column to say.
		offeredFor: (items) =>
			items.some(
				(item) => item.estimate !== undefined && item.estimate !== null,
			),
		build: estimateColumn,
	},
	{ id: "parent", offeredFor: always, build: parentColumn },
	{
		id: "blockedSince",
		offeredFor: workItemRows,
		build: (settings) =>
			dayColumn(
				"blockedSince",
				`${settings.getTerm(TERMINOLOGY_KEYS.BLOCKED)} since`,
				(row) => row.blockedSince,
			),
	},
	{
		id: "timeInState",
		offeredFor: always,
		build: (settings) => timeInStateGridColumn(settings.timeInState),
	},
	{
		id: "size",
		offeredFor: featureRows,
		build: (settings) =>
			numberColumn(
				"size",
				"Size",
				(row) => (isFeatureRow(row) ? row.size : 0),
				settings,
			),
	},
	{ id: "forecasts", offeredFor: featureRows, build: forecastColumn },
];

/** The catalogue columns these rows can fill, in the order Manage columns lists them. */
export function availableColumnIds(items: readonly IWorkItem[]): ColumnId[] {
	return CATALOGUE.filter((entry) => entry.offeredFor(items)).map(
		(entry) => entry.id,
	);
}

/** The grid column for one catalogue entry, or nothing for an id the catalogue does not draw. */
export function buildWorkItemColumn(
	columnId: ColumnId,
	settings: CatalogueColumnSettings,
): DataGridColumn<WorkItemRow> | undefined {
	return CATALOGUE.find((entry) => entry.id === columnId)?.build(settings);
}

export interface ContextGridLayout {
	columns: DataGridColumn<WorkItemRow>[];
	visibilityModel?: ColumnVisibilityModel;
	sortBy?: ColumnId;
}

const NO_CONTEXT: ContextGridLayout = { columns: [] };

/**
 * The columns a dialog opened from one context adds to the fixed ones: its defaults first, in their
 * order, shown, then whatever else the rows can fill, hidden. A default these rows cannot fill is
 * left out rather than shown empty.
 */
export function contextGridLayout(
	context: WorkItemsDialogContextId | undefined,
	items: readonly IWorkItem[],
	getTerm: (key: string) => string,
	timeInState?: TimeInStateColumnDefinition,
): ContextGridLayout {
	if (context === undefined) return NO_CONTEXT;

	const { visible, sortBy } = WORK_ITEMS_DIALOG_CONTEXTS[context];
	const available = availableColumnIds(items);
	const ordered = [
		...visible.filter((id) => available.includes(id)),
		...available.filter((id) => !visible.includes(id)),
	];

	return {
		columns: ordered.flatMap(
			(id) => buildWorkItemColumn(id, { getTerm, sortBy, timeInState }) ?? [],
		),
		visibilityModel: Object.fromEntries(
			ordered.map((id) => [id, visible.includes(id)]),
		),
		sortBy,
	};
}

/** How long each row has sat in its current state, flagged once that counts as stale. */
export function timeInStateGridColumn(
	definition: TimeInStateColumnDefinition = {},
): DataGridColumn<WorkItemRow> {
	return {
		field: "timeInState",
		headerName: "Time in State",
		width: 200,
		sortable: true,
		valueGetter: (_, row) =>
			daysInState(row.currentStateEnteredAt ?? null, definition.now),
		renderCell: ({ row }) => (
			<TimeInStateBadge
				currentStateEnteredAt={row.currentStateEnteredAt ?? null}
				currentStateName={row.state}
				stalenessThresholdDays={definition.stalenessThresholdDays}
				blockedStalenessThresholdDays={definition.blockedStalenessThresholdDays}
				isBlocked={row.isBlocked}
				blockedSince={row.blockedSince ?? null}
				now={definition.now}
			/>
		),
	};
}

/**
 * The look shared by every cell whose value carries a judgement: the text takes the colour of that
 * judgement and the cell a tenth-strength wash of it, so a reader spots the bad ones by scanning
 * rather than reading. `plainColor` is what the text falls back to when there is no judgement to
 * show — leave it out to inherit the surrounding text colour.
 */
export const judgementCell = (
	color: string | undefined,
	plainColor?: string,
) => ({
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
export const ageBandGridColumn = (
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
export const sleRiskGridColumn = (
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
export const warningsGridColumn = (
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
