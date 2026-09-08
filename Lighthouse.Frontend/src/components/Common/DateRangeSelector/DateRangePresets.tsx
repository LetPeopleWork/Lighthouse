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
}) => {
	throw new Error(
		`DateRangePresets(${presets.length}, ${selectedDays}, ${typeof onSelectPreset}) is not implemented`,
	);
};

export default DateRangePresets;
