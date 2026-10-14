import {
	Box,
	TextField,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import type React from "react";
import type { IReportOwner } from "../../../../../models/Reports/Report";
import { reportWindowPresetsFor } from "../../panels/reportWindowPresets";
import { lengthOf } from "./thenAndNowDraft";

interface WindowLengthControlProps {
	owner: IReportOwner;
	length: string;
	onChange: (length: string) => void;
	disabled: boolean;
	fieldLabel: string;
	presetsLabel: string;
	minimumLengthDays: number;
	leadingLabel?: string;
	customLabel: string;
}

/** A window's length: the owner's presets with one click, beside a field for any number of days. */
const WindowLengthControl: React.FC<Readonly<WindowLengthControlProps>> = ({
	owner,
	length,
	onChange,
	disabled,
	fieldLabel,
	presetsLabel,
	minimumLengthDays,
	leadingLabel,
	customLabel,
}) => {
	const lengthDays = lengthOf(length);
	const presets = reportWindowPresetsFor(owner);

	return (
		<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
			{leadingLabel !== undefined && (
				<Typography variant="body2">{leadingLabel}</Typography>
			)}
			<ToggleButtonGroup
				exclusive
				size="small"
				aria-label={presetsLabel}
				disabled={disabled}
				value={presets.includes(lengthDays ?? 0) ? lengthDays : null}
				onChange={(_, preset: number | null) => {
					if (preset !== null) onChange(String(preset));
				}}
			>
				{presets.map((preset) => (
					<ToggleButton key={preset} value={preset}>
						{`${preset} days`}
					</ToggleButton>
				))}
			</ToggleButtonGroup>
			<Typography variant="body2">{customLabel}</Typography>
			<TextField
				type="number"
				size="small"
				value={length}
				disabled={disabled}
				onChange={(event) => onChange(event.target.value)}
				slotProps={{
					htmlInput: { "aria-label": fieldLabel, min: minimumLengthDays },
				}}
				sx={{ width: 100 }}
			/>
			<Typography variant="body2">days</Typography>
		</Box>
	);
};

export default WindowLengthControl;
