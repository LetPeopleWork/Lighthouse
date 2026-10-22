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
	settings: teamSettings,
	onSettingsChange: onTeamSettingsChange,
	additionalFieldDefinitions = [],
	connection,
	parentOverrideDefault,
}) => {
	const { getTerm } = useTerminology();
	const keptItemsTerm = getTerm(
		parentOverrideDefault === "parentOverrideForTeams"
			? TERMINOLOGY_KEYS.WORK_ITEMS
			: TERMINOLOGY_KEYS.FEATURES,
	);

	return (
		<InputGroup title={"Advanced Configuration"} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<ConnectionDefaultFieldSelect
					label="Parent Override Field"
					settings={teamSettings}
					connection={connection}
					theDefault={parentOverrideDefault}
					defaultNameTheSettingsCarry={teamSettings?.parentOverrideDefaultName}
					source={teamSettings?.parentOverrideSource}
					ownField={teamSettings?.parentOverrideAdditionalFieldDefinitionId}
					additionalFieldDefinitions={additionalFieldDefinitions}
					{...followOrOwn<IBaseSettings>(
						onTeamSettingsChange,
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
					value={teamSettings?.doneItemsCutoffDays ?? 0}
					onChange={(e) =>
						onTeamSettingsChange(
							"doneItemsCutoffDays",
							Number.parseInt(e.target.value, 10) || 0,
						)
					}
					slotProps={{
						htmlInput: { min: 0 },
					}}
					helperText={`Finished ${keptItemsTerm} older than this many days are not kept. This limits metrics, forecasts and how far back Reports can reach. 0 keeps them all. Raising it brings older ones back with the next update.`}
				/>
			</Grid>
		</InputGroup>
	);
};

export default AdvancedInputsComponent;
