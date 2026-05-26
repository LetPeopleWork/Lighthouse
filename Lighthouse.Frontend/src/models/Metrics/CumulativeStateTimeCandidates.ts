export interface ICumulativeStateTimeCandidateRow {
	workItemId: number;
	referenceId: string;
	title: string;
	workItemType: string;
	parentReferenceId: string | null;
}

export interface ICumulativeStateTimeCandidatesResponse {
	items: ICumulativeStateTimeCandidateRow[];
}
