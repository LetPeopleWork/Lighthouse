/**
 * One state a Team admin chose as a refinement state. `isMapped` is only ever read back: it is false
 * when the state is still chosen but the Team no longer maps it as To Do or Doing, so its Work Items
 * cannot appear.
 */
export interface IRefinementStateSetting {
	state: string;
	isMapped?: boolean;
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
