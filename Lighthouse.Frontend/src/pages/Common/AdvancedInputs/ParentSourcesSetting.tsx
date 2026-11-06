import { FormControlLabel, Radio, RadioGroup } from "@mui/material";
import type React from "react";
import { useState } from "react";
import {
	followsTheConnection,
	trackerHoldingTheDefault,
} from "../../../components/Common/ConnectionDefault/connectionDefaultEntry";
import {
	directionTipTermsFrom,
	parentSourcesTip,
} from "../../../components/Common/ConnectionDefault/fieldDirectionTips";
import ParentSourcesList from "../../../components/Common/ConnectionDefault/ParentSourcesList";
import {
	nameOfParentSource,
	OnlyTheTrackersOwnParent,
	ownerOfParentSourcesDefault,
} from "../../../components/Common/ConnectionDefault/parentSources";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { ParentSourceEntry } from "../../../models/Common/ParentSourceEntry";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import {
	type IWorkTrackingSystemConnection,
	type ParentSourcesDefaultName,
	ParentSourcesListOfDefault,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { useTerminology } from "../../../services/TerminologyContext";

interface ParentSourcesSettingProps {
	settings: IBaseSettings | null;
	onSettingsChange: (
		key: "parentOverrideSource" | "parentSources",
		value: string | readonly ParentSourceEntry[],
	) => void;
	additionalFieldDefinitions: IAdditionalFieldDefinition[];
	connection: IWorkTrackingSystemConnection | null | undefined;
	tracker: WorkTrackingSystemType;
	theDefault: ParentSourcesDefaultName;
}

// The settings carry the default of their stored connection. Once another one is chosen, its default is read off
// that connection.
const defaultListOf = (
	settings: IBaseSettings | null,
	connection: IWorkTrackingSystemConnection | null | undefined,
	theDefault: ParentSourcesDefaultName,
): readonly ParentSourceEntry[] | null | undefined => {
	if (
		!connection ||
		connection.id === settings?.workTrackingSystemConnectionId
	) {
		return settings?.parentSourcesDefault;
	}
	return connection.defaults?.[ParentSourcesListOfDefault[theDefault]] ?? null;
};

const ParentSourcesSetting: React.FC<Readonly<ParentSourcesSettingProps>> = ({
	settings,
	onSettingsChange,
	additionalFieldDefinitions,
	connection,
	tracker,
	theDefault,
}) => {
	const { getTerm } = useTerminology();
	// An unset default is not offered to an owner with its own list, so leaving it would otherwise take away the
	// way back until the page is loaded again.
	const [leftTheDefault, setLeftTheDefault] = useState(false);
	const defaultList = defaultListOf(settings, connection, theDefault);
	const followedList = defaultList ?? OnlyTheTrackersOwnParent;
	const follows = followsTheConnection(
		settings,
		connection,
		theDefault,
		settings?.parentOverrideSource,
	);
	const offersTheDefault =
		follows ||
		leftTheDefault ||
		(!!defaultList &&
			!!trackerHoldingTheDefault(settings, connection, theDefault));

	const followedInWords = followedList
		.map((entry) =>
			nameOfParentSource(entry, tracker, additionalFieldDefinitions),
		)
		.join(", then ");

	const handleSourceChosen = (choice: string) => {
		if (choice === "ConnectionDefault") {
			onSettingsChange("parentOverrideSource", "ConnectionDefault");
			return;
		}
		setLeftTheDefault(true);
		onSettingsChange("parentOverrideSource", "Own");
		onSettingsChange("parentSources", followedList);
	};

	return (
		<ParentSourcesList
			owner={ownerOfParentSourcesDefault(theDefault)}
			value={
				follows
					? followedList
					: (settings?.parentSources ?? OnlyTheTrackersOwnParent)
			}
			tracker={tracker}
			additionalFieldDefinitions={additionalFieldDefinitions}
			onChange={(list) => onSettingsChange("parentSources", list)}
			followed={follows}
			info={parentSourcesTip(
				theDefault,
				tracker,
				directionTipTermsFrom(getTerm),
			)}
			sourceChoice={
				offersTheDefault && (
					<RadioGroup
						row
						aria-label="Where the parent sources are set"
						value={follows ? "ConnectionDefault" : "Own"}
						onChange={(event) => handleSourceChosen(event.target.value)}
					>
						<FormControlLabel
							value="Own"
							control={<Radio size="small" />}
							label="Set here"
						/>
						<FormControlLabel
							value="ConnectionDefault"
							control={<Radio size="small" />}
							label={`Connection default: ${followedInWords}`}
						/>
					</RadioGroup>
				)
			}
		/>
	);
};

export default ParentSourcesSetting;
