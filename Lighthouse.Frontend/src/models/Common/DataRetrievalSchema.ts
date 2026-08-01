export interface IDataRetrievalSchema {
	key: string;
	displayLabel: string;
	inputKind: "freetext" | "wizard-select" | "file-upload" | "none";
	isRequired: boolean;
	isWorkItemTypesRequired: boolean;
	wizardHint: string | null;
	// Rendered as the query field's placeholder and helper text. Absent for a connector with
	// nothing to explain, which then renders exactly what it renders today (#5610, DD-5).
	placeholder?: string | null;
	helpText?: string | null;
}
