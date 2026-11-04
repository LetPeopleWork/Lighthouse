import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { WorkTrackingSystemType } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

export type ParentSourceKind = "TrackerParent" | "AdditionalField";

export interface ParentSourceEntry {
	kind: ParentSourceKind;
	additionalFieldDefinitionId?: number | null;
}

export type ParentSourcesAction =
	| { type: "add"; entry: ParentSourceEntry }
	| { type: "remove"; index: number }
	| { type: "moveUp"; index: number }
	| { type: "moveDown"; index: number };

export const MaximumParentSources = 5;

const unbuilt = (member: string): never => {
	throw new Error(`SCAFFOLD: Story 6501, ${member} is not built yet.`);
};

/**
 * The list after one edit. An edit the rules refuse (a sixth entry, an entry already listed, removing the
 * only row, moving the first row up or the last row down) returns the list it was given.
 */
export const reduceParentSources = (
	_list: readonly ParentSourceEntry[],
	_action: ParentSourcesAction,
): readonly ParentSourceEntry[] => unbuilt("reduceParentSources");

/** What the add picker offers: the tracker's own parent and the fields, each only while not yet listed. */
export const entriesStillToAdd = (
	_list: readonly ParentSourceEntry[],
	_additionalFieldDefinitions: readonly IAdditionalFieldDefinition[],
): readonly ParentSourceEntry[] => unbuilt("entriesStillToAdd");

/** How a row reads: the field's name, "Parent (Jira's own)", or "Unknown field (id 12)". */
export const nameOfParentSource = (
	_entry: ParentSourceEntry,
	_tracker: WorkTrackingSystemType,
	_additionalFieldDefinitions: readonly IAdditionalFieldDefinition[],
): string => unbuilt("nameOfParentSource");

/** Only the trackers that read the list show it; the others keep the parents they build themselves. */
export const parentSourcesAreOffered = (
	_tracker: WorkTrackingSystemType,
): boolean => unbuilt("parentSourcesAreOffered");
