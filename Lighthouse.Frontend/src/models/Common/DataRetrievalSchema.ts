export interface IDataRetrievalSchema {
	key: string;
	displayLabel: string;
	inputKind: "freetext" | "wizard-select" | "file-upload" | "none";
	isRequired: boolean;
	isWorkItemTypesRequired: boolean;
	wizardHint: string | null;
}
