/** One state a Team admin chose as a refinement state. */
export interface IRefinementStateSetting {
	state: string;
}

export interface IRefinementSettings {
	states: IRefinementStateSetting[];
}

/** The category a refinement row's state belongs to. Sent as the name, never a number. */
export type RefinementRowCategory = "ToDo" | "Doing";

export interface IRefinementRow {
	referenceId: string;
	name: string;
	url: string | null;
	state: string;
	stateCategory: RefinementRowCategory;
	/** Work Item Age in days for a Doing row; null for a To Do row, which has not started. */
	workItemAge: number | null;
}

export interface IRefinementView {
	refinementConfigured: boolean;
	workItems: IRefinementRow[];
}
