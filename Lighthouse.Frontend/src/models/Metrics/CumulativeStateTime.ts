export interface ICumulativeStateTimeStateRow {
	state: string;
	workflowOrder: number;
	totalDays: number;
	completedContributionDays: number;
	ongoingContributionDays: number;
	itemCount: number;
	completedItemCount: number;
	ongoingItemCount: number;
	meanDays: number;
	medianDays: number | null;
}

export interface ICumulativeStateTimeResponse {
	states: ICumulativeStateTimeStateRow[];
}
