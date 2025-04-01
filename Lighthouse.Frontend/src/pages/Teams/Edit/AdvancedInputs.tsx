import { Checkbox, FormControlLabel, TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";

interface AdvancedInputsComponentProps {
	teamSettings: ITeamSettings | null;
	onTeamSettingsChange: (
		key: keyof ITeamSettings,
		value: string | number | boolean,
	) => void;
}

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
	teamSettings,
	onTeamSettingsChange,
}) => {
	return (
		<InputGroup title={"Advanced Configuration"} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<TextField
					label="Feature WIP"
					type="number"
					fullWidth
					margin="normal"
					value={teamSettings?.featureWIP ?? 1}
					slotProps={{
						htmlInput: {
							min: 1,
						},
					}}
					onChange={(e) =>
						onTeamSettingsChange(
							"featureWIP",
							Number.parseInt(e.target.value, 10),
						)
					}
				/>
				<FormControlLabel
					control={
						<Checkbox
							checked={teamSettings?.automaticallyAdjustFeatureWIP ?? false}
							onChange={(e) =>
								onTeamSettingsChange(
									"automaticallyAdjustFeatureWIP",
									e.target.checked,
								)
							}
						/>
					}
					label="Automatically Adjust Feature WIP based on actual WIP"
				/>
			</Grid>

			<Grid size={{ xs: 12 }}>
				<TextField
					label="Relation Custom Field"
					fullWidth
					margin="normal"
					value={teamSettings?.relationCustomField ?? ""}
					onChange={(e) =>
						onTeamSettingsChange("relationCustomField", e.target.value)
					}
				/>
			</Grid>
		</InputGroup>
	);
};

export default AdvancedInputsComponent;
