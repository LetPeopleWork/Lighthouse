import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IProjectSettings } from "../../../../models/Project/ProjectSettings";
import InputGroup from "../../InputGroup/InputGroup";

interface UnparentedItemsComponentProps {
	projectSettings: IProjectSettings | null;
	onProjectSettingsChange: (
		key: keyof IProjectSettings,
		value: string | number | boolean | string[],
	) => void;
}

const UnparentedItemsComponent: React.FC<UnparentedItemsComponentProps> = ({
	projectSettings,
	onProjectSettingsChange,
}) => {
	return (
		<InputGroup title={"Unparented Work Items"} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<TextField
					label="Unparented Work Items Query"
					fullWidth
					multiline
					rows={4}
					margin="normal"
					value={projectSettings?.unparentedItemsQuery ?? ""}
					onChange={(e) =>
						onProjectSettingsChange("unparentedItemsQuery", e.target.value)
					}
				/>
			</Grid>
		</InputGroup>
	);
};

export default UnparentedItemsComponent;
