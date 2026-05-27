export interface ICumulativeStateTimeCandidateRow {
	workItemId: number;
	referenceId: string;
	title: string;
	workItemType: string;
}

export interface ICumulativeStateTimeCandidatesResponse {
	items: ICumulativeStateTimeCandidateRow[];
}
