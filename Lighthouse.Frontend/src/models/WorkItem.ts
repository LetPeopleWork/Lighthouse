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
}

export interface IWorkItemEstimate {
	value: number | null;
	displayValue: string | null;
	unit: string | null;
}
