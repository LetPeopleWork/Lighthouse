import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type {
	ConnectionDefaultName,
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";

/** The value a field list uses for the entry that follows the connection's default. */
export const FollowTheConnection = "connection-default";

// Only these trackers hold the field defaults a Team or Portfolio can follow on their connection.
const trackersHoldingTheDefaults: Partial<
	Record<WorkTrackingSystemType, string>
> = {
	Jira: "Jira",
	AzureDevOps: "Azure DevOps",
};

/** The tracker's name as the helper text says it, or undefined when its connection holds no defaults. */
export const trackerHoldingTheDefaults = (
	settings: IBaseSettings | null,
	connection: IWorkTrackingSystemConnection | null | undefined,
): string | undefined => {
	const tracker =
		connection?.workTrackingSystem ?? settings?.workTrackingSystem;
	return tracker ? trackersHoldingTheDefaults[tracker] : undefined;
};

// The settings carry the name of their stored connection's default, which an admin who cannot read the
// connection needs. Once another connection is chosen, that name is read off the chosen one instead.
export const nameOfTheDefault = (
	settings: IBaseSettings | null,
	nameTheSettingsCarry: string | null | undefined,
	connection: IWorkTrackingSystemConnection | null | undefined,
	theDefault: ConnectionDefaultName,
): string | null | undefined => {
	if (
		!connection ||
		connection.id === settings?.workTrackingSystemConnectionId
	) {
		return nameTheSettingsCarry;
	}
	const named = connection.defaults?.[theDefault];
	return connection.additionalFieldDefinitions.find(
		(field) => field.id === named,
	)?.displayName;
};
