import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import ConnectionDefaultFieldSelect from "../../../components/Common/ConnectionDefault/ConnectionDefaultFieldSelect";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

interface AdvancedInputsComponentProps {
	settings: IBaseSettings | null;
	onSettingsChange: (
		key: keyof IBaseSettings,
		value: string | number | null,
	) => void;
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
	connection?: IWorkTrackingSystemConnection | null;
	parentOverrideDefault?: ParentOverrideDefault;
}

type ParentOverrideDefault =
	| "parentOverrideForTeams"
	| "parentOverrideForPortfolios";

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
	settings: teamSettings,
	onSettingsChange: onTeamSettingsChange,
	additionalFieldDefinitions = [],
	connection,
	parentOverrideDefault = "parentOverrideForTeams",
}) => {
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
					onFollow={() =>
						onTeamSettingsChange("parentOverrideSource", "ConnectionDefault")
					}
					onOwn={(field) => {
						onTeamSettingsChange("parentOverrideSource", "Own");
						onTeamSettingsChange(
							"parentOverrideAdditionalFieldDefinitionId",
							field,
						);
					}}
				/>
			</Grid>
			<Grid size={{ xs: 12 }}>
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
					helperText="Number of days to retain closed/done items. Items older than this will be filtered out."
				/>
			</Grid>
		</InputGroup>
	);
};

export default AdvancedInputsComponent;
