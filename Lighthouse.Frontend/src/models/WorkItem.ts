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
	workItemAge: number;
	parentWorkItemReference: string;
}
