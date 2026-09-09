import { Chip, Stack } from "@mui/material";
import type React from "react";
import type { DateWindowPreset } from "../../../pages/Common/MetricsView/dateWindow";

export interface DateRangePresetsProps {
	presets: readonly DateWindowPreset[];
	/** The preset whose window is currently showing, or null after a hand-typed edit. */
	selectedDays: number | null;
	onSelectPreset: (days: number) => void;
}

const DateRangePresets: React.FC<DateRangePresetsProps> = ({
	presets,
	selectedDays,
	onSelectPreset,
}) =>
	presets.length === 0 ? null : (
		// Stryker disable next-line all: how the row wraps is styling, and jsdom computes no layout.
		<Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 0.5 }}>
			{presets.map((preset) => {
				const isSelected = preset.days === selectedDays;

				return (
					<Chip
						key={preset.days}
						label={preset.label}
						size="small"
						variant={isSelected ? "filled" : "outlined"}
						color={isSelected ? "primary" : "default"}
						// Colour alone would leave the chosen window unreadable to a screen reader,
						// and only assertable through computed style.
						aria-pressed={isSelected}
						onClick={() => onSelectPreset(preset.days)}
					/>
				);
			})}
		</Stack>
	);

export default DateRangePresets;
