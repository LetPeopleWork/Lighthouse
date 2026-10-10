import type { GridValidRowModel } from "@mui/x-data-grid";
import type { IWorkItem } from "../../../models/WorkItem";
import type { DataGridColumn } from "../DataGrid/types";
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
