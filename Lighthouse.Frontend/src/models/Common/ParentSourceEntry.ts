export type ParentSourceKind = "TrackerParent" | "AdditionalField";

export interface ParentSourceEntry {
	kind: ParentSourceKind;
	additionalFieldDefinitionId?: number | null;
}
