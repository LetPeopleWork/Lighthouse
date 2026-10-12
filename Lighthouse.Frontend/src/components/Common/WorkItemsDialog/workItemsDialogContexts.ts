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

const STARTED: ColumnId = "startedDate";
const CLOSED: ColumnId = "closedDate";
const CYCLE_TIME: ColumnId = "cycleTime";
const AGE: ColumnId = "workItemAge";
const AGE_OR_CYCLE_TIME: ColumnId = "ageOrCycleTime";
const TIME_IN_STATE: ColumnId = "timeInState";
const ESTIMATE: ColumnId = "estimate";
const SLE_RISK: ColumnId = "sleRisk";

const withCatalogue = (
	visible: ColumnId[],
	sortBy: ColumnId,
): WorkItemsDialogContextDefaults => ({ visible, sortBy, catalogue: true });

/** The columns each context opens with and the one it sorts by; change a context's defaults here. */
export const WORK_ITEMS_DIALOG_CONTEXTS: Record<
	WorkItemsDialogContextId,
	WorkItemsDialogContextDefaults
> = {
	closedItems: withCatalogue([CLOSED, CYCLE_TIME], CYCLE_TIME),
	estimation: withCatalogue([ESTIMATE, CYCLE_TIME, CLOSED], CYCLE_TIME),
	arrivals: withCatalogue([STARTED, AGE_OR_CYCLE_TIME], AGE_OR_CYCLE_TIME),
	inProgress: withCatalogue([STARTED, AGE, TIME_IN_STATE, SLE_RISK], AGE),
	aging: withCatalogue([AGE, "ageBand", SLE_RISK, TIME_IN_STATE], AGE),
	blocked: withCatalogue([AGE, "blockedSince"], AGE),
	stale: withCatalogue([AGE, TIME_IN_STATE], AGE),
	workDistribution: withCatalogue(
		["parent", AGE_OR_CYCLE_TIME],
		AGE_OR_CYCLE_TIME,
	),
	featureSize: withCatalogue(["size", AGE_OR_CYCLE_TIME, CLOSED], "size"),
	featureChildren: withCatalogue(
		[STARTED, CLOSED, AGE_OR_CYCLE_TIME, ESTIMATE],
		AGE_OR_CYCLE_TIME,
	),
	deliveryTimeline: withCatalogue(["warnings"], "warnings"),
	// Its rows are built for the chart with no real dates or ages, so no other column would say anything.
	cumulativeStateTime: {
		visible: ["daysContributed"],
		sortBy: "daysContributed",
		catalogue: false,
	},
	startedAndClosed: withCatalogue(
		[STARTED, CLOSED, AGE_OR_CYCLE_TIME],
		AGE_OR_CYCLE_TIME,
	),
};
