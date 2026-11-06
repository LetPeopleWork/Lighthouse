import { FormControlLabel, Radio, RadioGroup, TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import ConnectionDefaultFieldSelect from "../../../components/Common/ConnectionDefault/ConnectionDefaultFieldSelect";
import {
	followOrOwn,
	followsTheConnection,
	trackerHoldingTheDefault,
	trackerOf,
} from "../../../components/Common/ConnectionDefault/connectionDefaultEntry";
import {
	directionTip,
	directionTipTermsFrom,
} from "../../../components/Common/ConnectionDefault/fieldDirectionTips";
import ParentSourcesList from "../../../components/Common/ConnectionDefault/ParentSourcesList";
import {
	nameOfParentSource,
	type ParentSourceEntry,
	parentSourcesAreOffered,
} from "../../../components/Common/ConnectionDefault/parentSources";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { useTerminology } from "../../../services/TerminologyContext";

interface AdvancedInputsComponentProps {
	settings: IBaseSettings | null;
	onSettingsChange: (
		key: keyof IBaseSettings,
		value: string | number | readonly ParentSourceEntry[] | null,
	) => void;
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
	connection?: IWorkTrackingSystemConnection | null;
	parentOverrideDefault: ParentOverrideDefault;
}

type ParentOverrideDefault =
	| "parentOverrideForTeams"
	| "parentOverrideForPortfolios";

interface ParentSourcesSettingProps {
	settings: IBaseSettings | null;
	onSettingsChange: AdvancedInputsComponentProps["onSettingsChange"];
	additionalFieldDefinitions: IAdditionalFieldDefinition[];
	connection: IWorkTrackingSystemConnection | null | undefined;
	tracker: WorkTrackingSystemType;
	theDefault: ParentOverrideDefault;
}

const OnlyTheTrackersOwnParent: readonly ParentSourceEntry[] = [
	{ kind: "TrackerParent" },
];

// The settings carry the default of their stored connection. Once another one is chosen, its default is read off
// that connection, which holds a single field until it holds lists of its own.
const defaultListOf = (
	settings: IBaseSettings | null,
	connection: IWorkTrackingSystemConnection | null | undefined,
	theDefault: ParentOverrideDefault,
): readonly ParentSourceEntry[] | null | undefined => {
	if (
		!connection ||
		connection.id === settings?.workTrackingSystemConnectionId
	) {
		return settings?.parentSourcesDefault;
	}
	const field = connection.defaults?.[theDefault];
	return typeof field === "number"
		? [
				{ kind: "AdditionalField", additionalFieldDefinitionId: field },
				...OnlyTheTrackersOwnParent,
			]
		: null;
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
		onSettingsChange("parentOverrideSource", "Own");
		onSettingsChange("parentSources", followedList);
	};

	return (
		<ParentSourcesList
			value={
				follows
					? followedList
					: (settings?.parentSources ?? OnlyTheTrackersOwnParent)
			}
			tracker={tracker}
			additionalFieldDefinitions={additionalFieldDefinitions}
			onChange={(list) => onSettingsChange("parentSources", list)}
			followed={follows}
			info={directionTip(theDefault, tracker, directionTipTermsFrom(getTerm))}
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

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
	settings,
	onSettingsChange,
	additionalFieldDefinitions = [],
	connection,
	parentOverrideDefault,
}) => {
	const { getTerm } = useTerminology();
	const finishedItemsTerm = getTerm(
		parentOverrideDefault === "parentOverrideForTeams"
			? TERMINOLOGY_KEYS.WORK_ITEMS
			: TERMINOLOGY_KEYS.FEATURES,
	);
	const tracker = trackerOf(settings, connection);

	return (
		<InputGroup title={"Advanced Configuration"} initiallyExpanded={false}>
			{tracker && parentSourcesAreOffered(tracker) && (
				<Grid size={{ xs: 12 }}>
					<ParentSourcesSetting
						settings={settings}
						onSettingsChange={onSettingsChange}
						additionalFieldDefinitions={additionalFieldDefinitions}
						connection={connection}
						tracker={tracker}
						theDefault={parentOverrideDefault}
					/>
				</Grid>
			)}
			{tracker === "AzureDevOps" && (
				<Grid size={{ xs: 12 }}>
					<ConnectionDefaultFieldSelect
						label="Parent sources"
						settings={settings}
						connection={connection}
						theDefault={parentOverrideDefault}
						defaultNameTheSettingsCarry={settings?.parentOverrideDefaultName}
						source={settings?.parentOverrideSource}
						ownField={settings?.parentOverrideAdditionalFieldDefinitionId}
						additionalFieldDefinitions={additionalFieldDefinitions}
						{...followOrOwn<IBaseSettings>(
							onSettingsChange,
							"parentOverrideSource",
							"parentOverrideAdditionalFieldDefinitionId",
						)}
					/>
				</Grid>
			)}
			<Grid size={{ xs: 12 }} data-testid="closed-items-cutoff">
				<TextField
					label="Closed Items Cutoff (days)"
					type="number"
					fullWidth
					margin="normal"
					value={settings?.doneItemsCutoffDays ?? 0}
					onChange={(e) =>
						onSettingsChange(
							"doneItemsCutoffDays",
							Number.parseInt(e.target.value, 10) || 0,
						)
					}
					slotProps={{
						htmlInput: { min: 0 },
					}}
					helperText={`Finished ${finishedItemsTerm} older than this many days are not kept. This limits metrics, forecasts and how far back Reports can reach. 0 keeps them all. Raising it brings older ones back with the next update.`}
				/>
			</Grid>
		</InputGroup>
	);
};

export default AdvancedInputsComponent;
