import {
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	TextField,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";

interface AdvancedInputsComponentProps {
	settings: IBaseSettings | null;
	onSettingsChange: (
		key: keyof IBaseSettings,
		value: string | number | null,
	) => void;
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
}

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
	settings: teamSettings,
	onSettingsChange: onTeamSettingsChange,
	additionalFieldDefinitions = [],
}) => {
	return (
		<InputGroup title={"Advanced Configuration"} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<FormControl fullWidth margin="normal">
					<InputLabel>Parent Override Field</InputLabel>
					<Select<number | "">
						value={
							teamSettings?.parentOverrideAdditionalFieldDefinitionId ?? ""
						}
						label="Parent Override Field"
						onChange={(e) => {
							const value = e.target.value;
							onTeamSettingsChange(
								"parentOverrideAdditionalFieldDefinitionId",
								value === "" ? null : value,
							);
						}}
					>
						<MenuItem value="">
							<em>None</em>
						</MenuItem>
						{additionalFieldDefinitions.map((field) => (
							<MenuItem key={field.id} value={field.id}>
								{field.displayName}
							</MenuItem>
						))}
					</Select>
				</FormControl>
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
