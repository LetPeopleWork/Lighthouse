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

const CUSTOM = "custom";

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
	presets?: readonly number[];
	/** Offers "Custom" beside the presets, chosen whenever the length is not one of them. */
	customChoice?: boolean;
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
	presets = reportWindowPresetsFor(owner),
	customChoice = false,
}) => {
	const lengthDays = lengthOf(length);
	const isPreset = presets.includes(lengthDays ?? 0);
	const selected = customChoice && !isPreset ? CUSTOM : null;

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
				value={isPreset ? lengthDays : selected}
				onChange={(_, choice: number | typeof CUSTOM | null) => {
					if (choice === CUSTOM) onChange("");
					else if (choice !== null) onChange(String(choice));
				}}
			>
				{presets.map((preset) => (
					<ToggleButton key={preset} value={preset}>
						{`${preset} days`}
					</ToggleButton>
				))}
				{customChoice && <ToggleButton value={CUSTOM}>Custom</ToggleButton>}
			</ToggleButtonGroup>
			{customLabel !== "" && (
				<Typography variant="body2">{customLabel}</Typography>
			)}
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
