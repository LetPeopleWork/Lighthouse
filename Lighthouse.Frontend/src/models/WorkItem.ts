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
	currentStateEnteredAt?: Date | null;
	approximate?: boolean;
}
