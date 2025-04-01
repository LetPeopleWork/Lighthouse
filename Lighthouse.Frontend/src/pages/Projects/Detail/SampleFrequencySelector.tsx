import {
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	type SelectChangeEvent,
	TextField,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useState } from "react";

interface SampleFrequencySelectorProps {
	sampleEveryNthDay: number;
	onSampleEveryNthDayChange: (value: number) => void;
}

const SampleFrequencySelector: React.FC<SampleFrequencySelectorProps> = ({
	sampleEveryNthDay,
	onSampleEveryNthDayChange,
}) => {
	const [isCustom, setIsCustom] = useState<boolean>(
		![1, 7, 30].includes(sampleEveryNthDay),
	);

	const handlePredefinedChange = (event: SelectChangeEvent<string>) => {
		const value = event.target.value;
		if (value === "custom") {
			setIsCustom(true);
			onSampleEveryNthDayChange(sampleEveryNthDay);
		} else {
			setIsCustom(false);
			onSampleEveryNthDayChange(Number.parseInt(value, 10));
		}
	};

	const handleCustomChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		const customValue = Number.parseInt(event.target.value, 10);
		if (!Number.isNaN(customValue)) {
			onSampleEveryNthDayChange(customValue);
		}
	};

	return (
		<Grid size={{ xs: 6 }}>
			<FormControl fullWidth>
				<InputLabel id="sample-frequency-label">Sampling Frequency</InputLabel>
				<Select
					labelId="sample-frequency-label"
					data-testid="frequency-select"
					id="frequency-select"
					value={isCustom ? "custom" : sampleEveryNthDay.toString()}
					label="Sampling Frequency"
					onChange={handlePredefinedChange}
				>
					<MenuItem value={1}>Daily</MenuItem>
					<MenuItem value={7}>Weekly</MenuItem>
					<MenuItem value={30}>Monthly</MenuItem>
					<MenuItem value="custom">Custom</MenuItem>
				</Select>
			</FormControl>

			{isCustom && (
				<TextField
					label="Custom Sampling Interval (Days)"
					type="number"
					value={sampleEveryNthDay}
					onChange={handleCustomChange}
					fullWidth
					margin="normal"
					slotProps={{ htmlInput: { min: 1 } }}
				/>
			)}
		</Grid>
	);
};

export default SampleFrequencySelector;
