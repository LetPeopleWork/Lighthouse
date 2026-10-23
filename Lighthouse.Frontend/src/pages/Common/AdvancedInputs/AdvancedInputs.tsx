import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import ConnectionDefaultFieldSelect from "../../../components/Common/ConnectionDefault/ConnectionDefaultFieldSelect";
import { followOrOwn } from "../../../components/Common/ConnectionDefault/connectionDefaultEntry";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { useTerminology } from "../../../services/TerminologyContext";

interface AdvancedInputsComponentProps {
	settings: IBaseSettings | null;
	onSettingsChange: (
		key: keyof IBaseSettings,
		value: string | number | null,
	) => void;
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
	connection?: IWorkTrackingSystemConnection | null;
	parentOverrideDefault: ParentOverrideDefault;
}

type ParentOverrideDefault =
	| "parentOverrideForTeams"
	| "parentOverrideForPortfolios";

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

	return (
		<InputGroup title={"Advanced Configuration"} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<ConnectionDefaultFieldSelect
					label="Parent Override Field"
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
