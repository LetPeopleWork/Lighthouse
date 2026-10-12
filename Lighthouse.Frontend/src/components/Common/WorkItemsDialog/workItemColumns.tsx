import { Tooltip, Typography } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type { IWorkItem } from "../../../models/WorkItem";
import {
	type AgeBandColumnDescriptor,
	paceBandSortRank,
} from "../../../utils/charts/paceBands";
import {
	type SleRiskColumnDescriptor,
	sleRiskSortValue,
} from "../../../utils/charts/sleRisk";
import { hexToRgba } from "../../../utils/theme/colors";
import type { DataGridColumn } from "../DataGrid/types";
import type { WarningsColumnDescriptor } from "./WorkItemsDialog";
import type { ColumnId } from "./workItemsDialogContexts";

// RED scaffold written by DISTILL for the Work Items dialog's column catalogue. DELIVER replaces both
// functions with the real builders and removes this marker, the constant and this comment.
export const __SCAFFOLD__ = true;

const NOT_YET_IMPLEMENTED = "Not yet implemented -- RED scaffold";

/** The catalogue columns these rows can fill, in the order Manage columns lists them. */
export function availableColumnIds(items: readonly IWorkItem[]): ColumnId[] {
	throw new Error(`${NOT_YET_IMPLEMENTED} (${items.length} rows)`);
}

/** The grid column for one catalogue entry. */
export function buildWorkItemColumn(
	columnId: ColumnId,
): DataGridColumn<IWorkItem & GridValidRowModel> {
	throw new Error(`${NOT_YET_IMPLEMENTED} (${columnId})`);
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
