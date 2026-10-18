import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import {
	FollowTheConnection,
	nameOfTheDefault,
	trackerHoldingTheDefaults,
} from "../../../components/Common/ConnectionDefault/connectionDefaultEntry";
import FormSelectField from "../../../components/Common/FormSelectField/FormSelectField";
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

type ParentOverrideChoice = number | typeof FollowTheConnection;

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
	settings: teamSettings,
	onSettingsChange: onTeamSettingsChange,
	additionalFieldDefinitions = [],
	connection,
	parentOverrideDefault = "parentOverrideForTeams",
}) => {
	const trackerName = trackerHoldingTheDefaults(teamSettings, connection);
	const defaultName = nameOfTheDefault(
		teamSettings,
		teamSettings?.parentOverrideDefaultName,
		connection,
		parentOverrideDefault,
	);
	const followsTheConnection =
		!!trackerName && teamSettings?.parentOverrideSource === "ConnectionDefault";

	const handleParentOverrideChange = (choice: ParentOverrideChoice | null) => {
		if (choice === FollowTheConnection) {
			onTeamSettingsChange("parentOverrideSource", "ConnectionDefault");
			return;
		}
		onTeamSettingsChange("parentOverrideSource", "Own");
		onTeamSettingsChange("parentOverrideAdditionalFieldDefinitionId", choice);
	};

	return (
		<InputGroup title={"Advanced Configuration"} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<FormSelectField<ParentOverrideChoice>
					label="Parent Override Field"
					value={
						followsTheConnection
							? FollowTheConnection
							: (teamSettings?.parentOverrideAdditionalFieldDefinitionId ?? "")
					}
					onChange={handleParentOverrideChange}
					firstOption={
						trackerName
							? {
									id: FollowTheConnection,
									label: `Connection default (${defaultName ?? "None"})`,
								}
							: undefined
					}
					helperText={
						followsTheConnection
							? `Set on the ${trackerName} connection.`
							: undefined
					}
					options={additionalFieldDefinitions.map((field) => ({
						id: field.id,
						label: field.displayName,
					}))}
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
