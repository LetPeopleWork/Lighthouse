import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import InputGroup from "../InputGroup/InputGroup";

interface GeneralSettingsComponentProps<T extends IBaseSettings> {
	settings: T | null;
	onSettingsChange: <K extends keyof T>(key: K, value: T[K]) => void;
	title?: string;
}

const GeneralSettingsComponent = <T extends IBaseSettings>({
	settings,
	onSettingsChange,
	title = "General Configuration",
}: GeneralSettingsComponentProps<T>) => {
	return (
		<InputGroup title={title}>
			<Grid size={{ xs: 12 }}>
				<TextField
					label="Name"
					fullWidth
					margin="normal"
					value={settings?.name ?? ""}
					onChange={(e) =>
						onSettingsChange("name" as keyof T, e.target.value as T[keyof T])
					}
				/>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<TextField
					label="Work Item Query"
					multiline
					rows={4}
					fullWidth
					margin="normal"
					value={settings?.workItemQuery ?? ""}
					onChange={(e) =>
						onSettingsChange(
							"workItemQuery" as keyof T,
							e.target.value as T[keyof T],
						)
					}
				/>
			</Grid>
		</InputGroup>
	);
};

export default GeneralSettingsComponent;
