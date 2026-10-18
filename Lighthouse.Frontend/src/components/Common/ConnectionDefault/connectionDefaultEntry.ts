import type {
	IBaseSettings,
	SettingSource,
} from "../../../models/Common/BaseSettings";
import {
	type ConnectionDefaultName,
	ConnectionDefaultNames,
	type IWorkTrackingSystemConnection,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";

/** The value a field list uses for the entry that follows the connection's default. */
export const FollowTheConnection = "connection-default";

// Only these trackers hold field defaults a Team or Portfolio can follow on their connection. A CSV file has
// no parent, dependency or size field of its own to point at, but who owns a Feature can come from a column.
const trackersHoldingTheDefaults: Partial<
	Record<
		WorkTrackingSystemType,
		{ name: string; holds: readonly ConnectionDefaultName[] }
	>
> = {
	Jira: { name: "Jira", holds: ConnectionDefaultNames },
	AzureDevOps: { name: "Azure DevOps", holds: ConnectionDefaultNames },
	Csv: { name: "CSV", holds: ["featureOwnerFieldForPortfolios"] },
};

type KeysHolding<Settings, Value> = NoInfer<
	{
		[Key in keyof Settings]-?: NonNullable<Settings[Key]> extends Value
			? Key
			: never;
	}[keyof Settings]
>;

/**
 * What picking an entry writes into the settings: following sets only the source, while owning sets the
 * source and the field, where a null field is a deliberate None. Name the settings type, as in
 * followOrOwn<IPortfolioSettings>(...), so that handing the two keys over in the wrong order does not compile.
 */
export const followOrOwn = <Settings = never>(
	change: (
		key: NoInfer<keyof Settings>,
		value: SettingSource | number | null,
	) => void,
	sourceKey: KeysHolding<Settings, SettingSource>,
	ownFieldKey: KeysHolding<Settings, number>,
) => ({
	onFollow: () => change(sourceKey, "ConnectionDefault"),
	onOwn: (field: number | null) => {
		change(sourceKey, "Own");
		change(ownFieldKey, field);
	},
});

/**
 * The tracker's name as the helper text says it, or undefined when its connection cannot hold this default.
 * A loaded connection says itself which defaults it offers; the table above only answers when it has not.
 */
export const trackerHoldingTheDefault = (
	settings: IBaseSettings | null,
	connection: IWorkTrackingSystemConnection | null | undefined,
	theDefault: ConnectionDefaultName,
): string | undefined => {
	const tracker =
		connection?.workTrackingSystem ?? settings?.workTrackingSystem;
	const holding = tracker ? trackersHoldingTheDefaults[tracker] : undefined;
	const offered = connection?.supportedDefaults ?? holding?.holds;
	return offered?.includes(theDefault) ? holding?.name : undefined;
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
