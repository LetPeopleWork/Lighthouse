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

const isSameSource = (
	first: ParentSourceEntry,
	second: ParentSourceEntry,
): boolean =>
	first.kind === second.kind &&
	(first.additionalFieldDefinitionId ?? null) ===
		(second.additionalFieldDefinitionId ?? null);

const isListed = (
	list: readonly ParentSourceEntry[],
	entry: ParentSourceEntry,
): boolean => list.some((listed) => isSameSource(listed, entry));

const holdsPosition = (
	list: readonly ParentSourceEntry[],
	index: number,
): boolean => index >= 0 && index < list.length;

const withEntryAdded = (
	list: readonly ParentSourceEntry[],
	entry: ParentSourceEntry,
): readonly ParentSourceEntry[] => {
	if (list.length >= MaximumParentSources || isListed(list, entry)) {
		return list;
	}
	return [...list, entry];
};

const withRowRemoved = (
	list: readonly ParentSourceEntry[],
	index: number,
): readonly ParentSourceEntry[] => {
	if (list.length <= 1 || !holdsPosition(list, index)) {
		return list;
	}
	return list.filter((_, position) => position !== index);
};

const withRowsSwapped = (
	list: readonly ParentSourceEntry[],
	upper: number,
	lower: number,
): readonly ParentSourceEntry[] => {
	if (!holdsPosition(list, upper) || !holdsPosition(list, lower)) {
		return list;
	}
	const swapped = [...list];
	swapped[upper] = list[lower];
	swapped[lower] = list[upper];
	return swapped;
};

/**
 * The list after one edit. An edit the rules refuse (a sixth entry, an entry already listed, removing the
 * only row, moving the first row up or the last row down) returns the list it was given.
 */
export const reduceParentSources = (
	list: readonly ParentSourceEntry[],
	action: ParentSourcesAction,
): readonly ParentSourceEntry[] => {
	switch (action.type) {
		case "add":
			return withEntryAdded(list, action.entry);
		case "remove":
			return withRowRemoved(list, action.index);
		case "moveUp":
			return withRowsSwapped(list, action.index - 1, action.index);
		case "moveDown":
			return withRowsSwapped(list, action.index, action.index + 1);
	}
};

/** What the add picker offers: the tracker's own parent and the fields, each only while not yet listed. */
export const entriesStillToAdd = (
	list: readonly ParentSourceEntry[],
	additionalFieldDefinitions: readonly IAdditionalFieldDefinition[],
): readonly ParentSourceEntry[] => {
	const everySource: ParentSourceEntry[] = [
		{ kind: "TrackerParent" },
		...additionalFieldDefinitions.map(
			(definition): ParentSourceEntry => ({
				kind: "AdditionalField",
				additionalFieldDefinitionId: definition.id,
			}),
		),
	];
	return everySource.filter((entry) => !isListed(list, entry));
};

const definitionOf = (
	entry: ParentSourceEntry,
	additionalFieldDefinitions: readonly IAdditionalFieldDefinition[],
): IAdditionalFieldDefinition | undefined =>
	additionalFieldDefinitions.find(
		(candidate) => candidate.id === entry.additionalFieldDefinitionId,
	);

/** Whose parent the tracker's own entry reads, in words: "Jira's own". */
export const theTrackersOwn = (tracker: WorkTrackingSystemType): string =>
	`${tracker}'s own`;

/** How a row reads: the field's name, "Parent (Jira's own)", or "Unknown field (id 12)". */
export const nameOfParentSource = (
	entry: ParentSourceEntry,
	tracker: WorkTrackingSystemType,
	additionalFieldDefinitions: readonly IAdditionalFieldDefinition[],
): string => {
	if (entry.kind === "TrackerParent") {
		return `Parent (${theTrackersOwn(tracker)})`;
	}
	return (
		definitionOf(entry, additionalFieldDefinitions)?.displayName ??
		`Unknown field (id ${entry.additionalFieldDefinitionId})`
	);
};

/** True for a saved field the definitions no longer hold, the row that reads "Unknown field (id 12)". */
export const namesAnUnknownField = (
	entry: ParentSourceEntry,
	additionalFieldDefinitions: readonly IAdditionalFieldDefinition[],
): boolean =>
	entry.kind === "AdditionalField" &&
	definitionOf(entry, additionalFieldDefinitions) === undefined;

/** Only the trackers that read the list show it; the others keep the parents they build themselves. */
export const parentSourcesAreOffered = (
	tracker: WorkTrackingSystemType,
): boolean => tracker === "Jira";
