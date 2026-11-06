import type { ParentSourceEntry } from "../../../models/Common/ParentSourceEntry";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	ConnectionDefaultName,
	IConnectionDefaults,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { fieldOfDefault } from "./whatASaveAsks";

const namesAKeptField = (
	entry: ParentSourceEntry,
	kept: ReadonlySet<number>,
): boolean =>
	entry.kind === "TrackerParent" ||
	kept.has(entry.additionalFieldDefinitionId ?? Number.NaN);

// A list loses the entries naming a removed field and is cleared when none is left.
const listKeepingOnlyFields = (
	list: readonly ParentSourceEntry[],
	kept: ReadonlySet<number>,
): readonly ParentSourceEntry[] | null => {
	const remaining = list.filter((entry) => namesAKeptField(entry, kept));
	return remaining.length > 0 ? remaining : null;
};

// A default left naming a removed field would still be sent, and every Team or Portfolio following it
// would read a field the connection no longer has.
export const defaultsKeepingOnlyFields = (
	defaults: IConnectionDefaults,
	fields: IAdditionalFieldDefinition[],
): IConnectionDefaults => {
	const kept = new Set(fields.map((field) => field.id));
	return Object.fromEntries(
		(
			Object.entries(defaults) as [
				ConnectionDefaultName,
				IConnectionDefaults[ConnectionDefaultName],
			][]
		).map(([name, value]) => {
			if (Array.isArray(value)) {
				return [name, listKeepingOnlyFields(value, kept)];
			}
			const fieldId = fieldOfDefault(name, defaults);
			return [name, fieldId != null && !kept.has(fieldId) ? null : value];
		}),
	);
};
