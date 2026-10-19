import type React from "react";
import type {
	IBaseSettings,
	SettingSource,
} from "../../../models/Common/BaseSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	ConnectionDefaultName,
	IWorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import FormSelectField from "../FormSelectField/FormSelectField";
import {
	FollowTheConnection,
	followsTheConnection,
	nameOfTheDefault,
	trackerHoldingTheDefault,
} from "./connectionDefaultEntry";

type FieldChoice = number | typeof FollowTheConnection;

interface ConnectionDefaultFieldSelectProps {
	label: string;
	settings: IBaseSettings | null;
	connection: IWorkTrackingSystemConnection | null | undefined;
	theDefault: ConnectionDefaultName;
	defaultNameTheSettingsCarry: string | null | undefined;
	source: SettingSource | undefined;
	ownField: number | null | undefined;
	additionalFieldDefinitions: IAdditionalFieldDefinition[];
	onFollow: () => void;
	onOwn: (field: number | null) => void;
}

/** A field list that offers the connection's default above the fields, where the connection holds one. */
const ConnectionDefaultFieldSelect: React.FC<
	Readonly<ConnectionDefaultFieldSelectProps>
> = ({
	label,
	settings,
	connection,
	theDefault,
	defaultNameTheSettingsCarry,
	source,
	ownField,
	additionalFieldDefinitions,
	onFollow,
	onOwn,
}) => {
	const trackerName = trackerHoldingTheDefault(
		settings,
		connection,
		theDefault,
	);
	const defaultName = nameOfTheDefault(
		settings,
		defaultNameTheSettingsCarry,
		connection,
		theDefault,
	);
	const follows = followsTheConnection(
		settings,
		connection,
		theDefault,
		source,
	);

	const handleChange = (choice: FieldChoice | null) => {
		if (choice === FollowTheConnection) {
			onFollow();
			return;
		}
		onOwn(choice);
	};

	return (
		<FormSelectField<FieldChoice>
			label={label}
			value={follows ? FollowTheConnection : (ownField ?? "")}
			onChange={handleChange}
			firstOption={
				trackerName
					? {
							id: FollowTheConnection,
							label: `Connection default (${defaultName ?? "None"})`,
						}
					: undefined
			}
			helperText={follows ? `Set on the ${trackerName} connection.` : undefined}
			options={additionalFieldDefinitions.map((field) => ({
				id: field.id,
				label: field.displayName,
			}))}
		/>
	);
};

export default ConnectionDefaultFieldSelect;
