import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import FormSelectField from "../../../components/Common/FormSelectField/FormSelectField";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { WorkTrackingSystemType } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

interface AdvancedInputsComponentProps {
	settings: IBaseSettings | null;
	onSettingsChange: (
		key: keyof IBaseSettings,
		value: string | number | null,
	) => void;
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
}

const FollowTheConnection = "connection-default";

type ParentOverrideChoice = number | typeof FollowTheConnection;

const trackerNames: Record<WorkTrackingSystemType, string> = {
	Jira: "Jira",
	AzureDevOps: "Azure DevOps",
	Linear: "Linear",
	Csv: "CSV",
	ServiceNow: "ServiceNow",
};

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
	settings: teamSettings,
	onSettingsChange: onTeamSettingsChange,
	additionalFieldDefinitions = [],
}) => {
	const defaultName = teamSettings?.parentOverrideDefaultName;
	const followsTheConnection =
		!!defaultName && teamSettings?.parentOverrideSource === "ConnectionDefault";
	const trackerName = teamSettings?.workTrackingSystem
		? (trackerNames[teamSettings.workTrackingSystem] ??
			teamSettings.workTrackingSystem)
		: "";

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
						defaultName
							? {
									id: FollowTheConnection,
									label: `Connection default (${defaultName})`,
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
