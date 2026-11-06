import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	ConnectionDefaultName,
	IConnectionDefaults,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { fieldOfDefault } from "./whatASaveAsks";

// A single-field default left naming a removed field would still be sent, and every Team or Portfolio
// following it would read a field the connection no longer has. A Parent sources list is left as it
// stands instead: saving with a removed field in it is refused by the server, who names the default,
// rather than the page quietly dropping an entry the Teams that follow it still read.
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
				return [name, value];
			}
			const fieldId = fieldOfDefault(name, defaults);
			return [name, fieldId != null && !kept.has(fieldId) ? null : value];
		}),
	);
};
