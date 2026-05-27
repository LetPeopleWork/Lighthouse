import type { StateCategory } from "../WorkItem";

export interface ICumulativeStateTimeItemRow {
	workItemId: number;
	referenceId: string;
	title: string;
	type: string;
	state: string;
	stateCategory: StateCategory;
	url: string | null;
	daysContributed: number;
}

export interface ICumulativeStateTimeItemsResponse {
	state: string;
	items: ICumulativeStateTimeItemRow[];
}
