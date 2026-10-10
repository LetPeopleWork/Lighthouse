// RED scaffold written by DISTILL for the Work Items dialog's per-context columns. DELIVER replaces the
// map below with the real entries and removes this marker, the constant and this comment.
export const __SCAFFOLD__ = true;

const NOT_YET_IMPLEMENTED = "Not yet implemented -- RED scaffold";

/** Every column the dialog can offer besides ID, Name, Type and State. */
export type ColumnId =
	| "startedDate"
	| "closedDate"
	| "cycleTime"
	| "workItemAge"
	| "ageOrCycleTime"
	| "parent"
	| "blockedSince"
	| "timeInState"
	| "estimate"
	| "size"
	| "forecast"
	| "ageBand"
	| "sleRisk"
	| "warnings"
	| "daysContributed"
	| `namedCycleTime:${number}`;

/** What was clicked to open the dialog: one row of the per-context defaults table. */
export type WorkItemsDialogContextId =
	| "closedItems"
	| "estimation"
	| "arrivals"
	| "inProgress"
	| "aging"
	| "blocked"
	| "stale"
	| "workDistribution"
	| "featureSize"
	| "featureChildren"
	| "deliveryTimeline"
	| "cumulativeStateTime"
	| "startedAndClosed";

/** Whose items the dialog lists; each keeps its own layout per context. */
export type WorkItemsDialogOwnerKind = "team" | "portfolio";

export interface WorkItemsDialogContextDefaults {
	/** The columns shown by default besides the four fixed ones, in display order. */
	visible: ColumnId[];
	/** The column the rows are sorted by, descending. */
	sortBy: ColumnId;
	/** Whether the rest of the catalogue is offered under Manage columns. */
	catalogue: boolean;
}

const notYetDeclared = (): never => {
	throw new Error(NOT_YET_IMPLEMENTED);
};

export const WORK_ITEMS_DIALOG_CONTEXTS: Record<
	WorkItemsDialogContextId,
	WorkItemsDialogContextDefaults
> = new Proxy(
	{} as Record<WorkItemsDialogContextId, WorkItemsDialogContextDefaults>,
	{
		get: notYetDeclared,
		ownKeys: notYetDeclared,
	},
);
