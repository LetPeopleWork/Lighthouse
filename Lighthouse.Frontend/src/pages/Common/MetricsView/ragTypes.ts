export type RagResult = {
	readonly ragStatus: "red" | "amber" | "green";
	readonly tipText: string;
};

export type RagTerms = {
	readonly workItem: string;
	readonly workItems: string;
	readonly feature: string;
	readonly features: string;
	readonly cycleTime: string;
	readonly throughput: string;
	readonly wip: string;
	readonly workItemAge: string;
	readonly blocked: string;
	readonly sle: string;
};
