export interface ICumulativeStateTimeItemRow {
	referenceId: string;
	parentReferenceId: string | null;
	daysContributed: number;
	title?: string;
	type?: string;
	state?: string;
	url?: string | null;
}

export interface ICumulativeStateTimeItemsResponse {
	state: string;
	items: ICumulativeStateTimeItemRow[];
}
