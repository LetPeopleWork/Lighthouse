import { Box, TextField } from "@mui/material";
import { DatePicker } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import dayjs, { type Dayjs } from "dayjs";
import type React from "react";

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
	const startDateDayjs = dayjs(startDate);
	const endDateDayjs = dayjs(endDate);

	const handleStartDateChange = (date: Dayjs | null) => {
		onStartDateChange(date ? date.toDate() : null);
	};

	const handleEndDateChange = (date: Dayjs | null) => {
		onEndDateChange(date ? date.toDate() : null);
	};

	return (
		<LocalizationProvider dateAdapter={AdapterDayjs}>
			<Box
				sx={{
					display: "flex",
					alignItems: "center",
					gap: 2,
					mb: 3,
					width: "100%",
				}}
			>
				<DatePicker
					label="From"
					value={startDateDayjs}
					onChange={handleStartDateChange}
					slots={{ textField: TextField }}
					slotProps={{ textField: { fullWidth: true } }}
					maxDate={endDateDayjs}
				/>
				<DatePicker
					label="To"
					value={endDateDayjs}
					onChange={handleEndDateChange}
					slots={{ textField: TextField }}
					slotProps={{ textField: { fullWidth: true } }}
					minDate={startDateDayjs}
				/>
			</Box>
		</LocalizationProvider>
	);
};

export default DateRangeSelector;
