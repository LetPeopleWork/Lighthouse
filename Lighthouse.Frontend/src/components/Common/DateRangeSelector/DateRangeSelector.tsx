import { Box, Grid } from "@mui/material";
import type React from "react";
import { DateRange } from "react-date-range";
import "react-date-range/dist/styles.css";
import "react-date-range/dist/theme/default.css";

export interface DateRangeSelectorProps {
	startDate: Date;
	endDate: Date;
	onStartDateChange: (date: Date | null) => void;
	onEndDateChange: (date: Date | null) => void;
}

const DateRangeSelector: React.FC<DateRangeSelectorProps> = ({
	startDate,
	endDate,
	onStartDateChange,
	onEndDateChange,
}) => {
	interface RangeSelection {
		selection?: {
			startDate: Date;
			endDate: Date;
		};
	}

	const handleRangeChange = (ranges: RangeSelection) => {
		if (ranges.selection) {
			onStartDateChange(ranges.selection.startDate);
			onEndDateChange(ranges.selection.endDate);
		}
	};

	const selectionRange = {
		startDate: startDate,
		endDate: endDate,
		key: "selection",
	};

	return (
		<DateRange
			ranges={[selectionRange]}
			onChange={handleRangeChange}
			moveRangeOnFirstSelection={false}
			editableDateInputs={true}
			months={1}
			color="rgba(48, 87, 78, 1)"
			rangeColors={["rgba(48, 87, 78, 1)"]}
			direction="horizontal"
		/>
	);
};

export default DateRangeSelector;
