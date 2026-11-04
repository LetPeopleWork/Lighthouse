import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	ConnectionDefaultName,
	IConnectionDefaults,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { fieldOfDefault } from "./whatASaveAsks";

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
			const fieldId = fieldOfDefault(name, defaults);
			return [name, fieldId != null && !kept.has(fieldId) ? null : value];
		}),
	);
};
