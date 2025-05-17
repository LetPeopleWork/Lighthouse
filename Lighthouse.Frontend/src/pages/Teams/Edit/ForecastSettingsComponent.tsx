import { FormControlLabel, Switch, TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";

interface ForecastSettingsComponentProps {
	teamSettings: ITeamSettings | null;
	isDefaultSettings: boolean;
	onTeamSettingsChange: (
		key: keyof ITeamSettings,
		value: string | number | boolean | Date,
	) => void;
}

const ForecastSettingsComponent: React.FC<ForecastSettingsComponentProps> = ({
	teamSettings,
	isDefaultSettings,
	onTeamSettingsChange,
}) => {
	const handleDateChange = (name: keyof ITeamSettings, newDate: string) => {
		onTeamSettingsChange(name, new Date(newDate));
	};

	return (
		<InputGroup title={"Forecast Configuration"}>
			{!isDefaultSettings && (
				<Grid size={{ xs: 12 }}>
					<FormControlLabel
						control={
							<Switch
								checked={teamSettings?.useFixedDatesForThroughput ?? false}
								onChange={(e) =>
									onTeamSettingsChange(
										"useFixedDatesForThroughput",
										e.target.checked,
									)
								}
							/>
						}
						label="Use Fixed Dates for Throughput"
					/>
				</Grid>
			)}

			{!teamSettings?.useFixedDatesForThroughput ? (
				<Grid size={{ xs: 12 }}>
					<TextField
						label="Throughput History"
						type="number"
						fullWidth
						margin="normal"
						value={teamSettings?.throughputHistory ?? ""}
						onChange={(e) =>
							onTeamSettingsChange(
								"throughputHistory",
								Number.parseInt(e.target.value, 10),
							)
						}
					/>
				</Grid>
			) : (
				<Grid size={{ xs: 12, md: 12 }}>
					<TextField
						label="Start Date"
						type="date"
						slotProps={{
							inputLabel: { shrink: true },
							htmlInput: {
								// Max date is 10 days from today
								max: new Date(Date.now() - 10 * 24 * 60 * 60 * 1000)
									.toISOString()
									.slice(0, 10),
							},
						}}
						defaultValue={teamSettings.throughputHistoryStartDate
							.toISOString()
							.slice(0, 10)}
						onChange={(e) =>
							handleDateChange("throughputHistoryStartDate", e.target.value)
						}
					/>
					<TextField
						label="End Date"
						type="date"
						slotProps={{
							inputLabel: { shrink: true },
							htmlInput: {
								// Min date should be 10 days after start date
								min: new Date(
									teamSettings.throughputHistoryStartDate.getTime() +
										10 * 24 * 60 * 60 * 1000,
								)
									.toISOString()
									.slice(0, 10),
								max: new Date().toISOString().slice(0, 10),
							},
						}}
						defaultValue={teamSettings.throughputHistoryEndDate
							.toISOString()
							.slice(0, 10)} // Convert date to yyyy-MM-dd format
						onChange={(e) =>
							handleDateChange("throughputHistoryEndDate", e.target.value)
						}
					/>
				</Grid>
			)}
		</InputGroup>
	);
};

export default ForecastSettingsComponent;
