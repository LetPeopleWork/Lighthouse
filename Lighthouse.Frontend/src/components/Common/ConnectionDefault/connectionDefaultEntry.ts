import type {
	IBaseSettings,
	SettingSource,
} from "../../../models/Common/BaseSettings";
import {
	type ConnectionDefaultName,
	ConnectionDefaultNames,
	type IEstimationDefault,
	type IWorkTrackingSystemConnection,
	isEstimationDefaultName,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";

/** The value a field list uses for the entry that follows the connection's default. */
export const FollowTheConnection = "connection-default";

// Only these trackers hold field defaults a Team or Portfolio can follow on their connection. A CSV file has
// no parent, dependency or size field of its own to point at, but who owns a Feature and how items are
// estimated can come from a column.
const trackersHoldingTheDefaults: Partial<
	Record<
		WorkTrackingSystemType,
		{ name: string; holds: readonly ConnectionDefaultName[] }
	>
> = {
	Jira: { name: "Jira", holds: ConnectionDefaultNames },
	AzureDevOps: { name: "Azure DevOps", holds: ConnectionDefaultNames },
	Csv: {
		name: "CSV",
		holds: [
			"estimationForTeams",
			"featureOwnerFieldForPortfolios",
			"estimationForPortfolios",
		],
	},
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

/** Whether the owner reads the connection's default, which it can only while its tracker holds one. */
export const followsTheConnection = (
	settings: IBaseSettings | null,
	connection: IWorkTrackingSystemConnection | null | undefined,
	theDefault: ConnectionDefaultName,
	source: SettingSource | undefined,
): boolean =>
	!!trackerHoldingTheDefault(settings, connection, theDefault) &&
	source === "ConnectionDefault";

const fieldNamed = (
	connection: IWorkTrackingSystemConnection,
	fieldId: number | null | undefined,
): string | undefined =>
	connection.additionalFieldDefinitions.find((field) => field.id === fieldId)
		?.displayName;

// The unit tells two defaults on the same field apart, so it is named beside the field.
const describeEstimation = (
	connection: IWorkTrackingSystemConnection,
	bundle: IEstimationDefault | null | undefined,
): string | undefined => {
	const field = fieldNamed(connection, bundle?.additionalFieldDefinitionId);
	return field && bundle?.unit ? `${field}, ${bundle.unit}` : field;
};

// The settings carry the name of their stored connection's default, which an admin who cannot read the
// connection needs. Once another connection is chosen, that name is read off the chosen one instead. An
// Estimation default is read off a loaded connection either way, because only that one carries its unit.
export const nameOfTheDefault = (
	settings: IBaseSettings | null,
	nameTheSettingsCarry: string | null | undefined,
	connection: IWorkTrackingSystemConnection | null | undefined,
	theDefault: ConnectionDefaultName,
): string | null | undefined => {
	if (isEstimationDefaultName(theDefault) && connection?.defaults) {
		return describeEstimation(connection, connection.defaults[theDefault]);
	}
	if (
		!connection ||
		connection.id === settings?.workTrackingSystemConnectionId
	) {
		return nameTheSettingsCarry;
	}
	if (isEstimationDefaultName(theDefault)) {
		return undefined;
	}
	return fieldNamed(connection, connection.defaults?.[theDefault]);
};
