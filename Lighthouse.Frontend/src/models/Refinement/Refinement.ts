/** One state a Team admin chose as a refinement state. */
export interface IRefinementStateSetting {
	state: string;
}

export interface IRefinementSettings {
	states: IRefinementStateSetting[];
}

export interface IRefinementRow {
	referenceId: string;
	name: string;
	url: string | null;
	state: string;
	/** The parent's reference, or an empty string when the Work Item has no parent. */
	parentReferenceId: string;
}

export interface IRefinementView {
	refinementConfigured: boolean;
	workItems: IRefinementRow[];
}
