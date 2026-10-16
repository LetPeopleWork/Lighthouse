import type { INamedCycleTimeValue } from "./Metrics/NamedCycleTime";

export type StateCategory = "Unknown" | "ToDo" | "Doing" | "Done";

export interface IWorkItem {
	name: string;
	id: number;
	state: string;
	stateCategory: StateCategory;
	type: string;
	referenceId: string;
	url: string | null;
	startedDate: Date;
	closedDate: Date;
	cycleTime: number;
	namedCycleTimes?: INamedCycleTimeValue[];
	workItemAge: number;
	parentWorkItemReference: string;
	isBlocked: boolean;
	blockedSince?: string;
	currentStateEnteredAt?: Date | null;
	approximate?: boolean;
	/** Null when the owner has no estimation set up; absent from an older server, which means the same. */
	estimate?: IWorkItemEstimate | null;
	/**
	 * Lighthouse's own answer, on the reads that judge staleness. Absent means Lighthouse does not
	 * say - an older server, or a read that never judges it - which is not the same as "not stale".
	 */
	isStale?: boolean;
	staleReasons?: IStaleReason[];
}

export type StaleReasonKind =
	| "TimeInState"
	| "BlockedDuration"
	| "TimeInStateWhileBlocked";

export interface IStaleReason {
	kind: StaleReasonKind;
	days: number;
	/** Present for the two time-in-state kinds; Blocked time belongs to no state. */
	stateName?: string;
}

export interface IWorkItemEstimate {
	value: number | null;
	displayValue: string | null;
	unit: string | null;
}
