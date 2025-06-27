import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";

interface AdvancedInputsComponentProps {
	settings: IBaseSettings | null;
	onSettingsChange: (key: keyof IBaseSettings, value: string) => void;
}

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
	settings: teamSettings,
	onSettingsChange: onTeamSettingsChange,
}) => {
	return (
		<InputGroup title={"Advanced Configuration"} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<TextField
					label="Parent Override Field"
					fullWidth
					margin="normal"
					value={teamSettings?.parentOverrideField ?? ""}
					onChange={(e) =>
						onTeamSettingsChange("parentOverrideField", e.target.value)
					}
				/>
			</Grid>
		</InputGroup>
	);
};

export default AdvancedInputsComponent;
