import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import FormSelectField from "../../../components/Common/FormSelectField/FormSelectField";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";

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

const FollowTheConnection = "connection-default";

type ParentOverrideChoice = number | typeof FollowTheConnection;

// Only these trackers hold a Parent Override Field default on their connection.
const trackersHoldingTheDefault: Partial<
	Record<WorkTrackingSystemType, string>
> = {
	Jira: "Jira",
	AzureDevOps: "Azure DevOps",
};

// The settings carry the name of their stored connection's default, which a Team admin who cannot read
// the connection needs. Once another connection is chosen, that name is read off the chosen one instead.
const nameOfTheDefault = (
	settings: IBaseSettings | null,
	connection: IWorkTrackingSystemConnection | null | undefined,
	parentOverrideDefault: ParentOverrideDefault,
): string | null | undefined => {
	if (
		!connection ||
		connection.id === settings?.workTrackingSystemConnectionId
	) {
		return settings?.parentOverrideDefaultName;
	}
	const named = connection.defaults?.[parentOverrideDefault];
	return connection.additionalFieldDefinitions.find(
		(field) => field.id === named,
	)?.displayName;
};

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
	settings: teamSettings,
	onSettingsChange: onTeamSettingsChange,
	additionalFieldDefinitions = [],
	connection,
	parentOverrideDefault = "parentOverrideForTeams",
}) => {
	const tracker =
		connection?.workTrackingSystem ?? teamSettings?.workTrackingSystem;
	const trackerName = tracker ? trackersHoldingTheDefault[tracker] : undefined;
	const defaultName = nameOfTheDefault(
		teamSettings,
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
