import { Box, Stack, Typography, useTheme } from "@mui/material";
import { DatePicker } from "@mui/x-date-pickers";
import { AdapterDateFns } from "@mui/x-date-pickers/AdapterDateFns";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import type React from "react";

// Helper function to get the local date format
const getLocaleDateFormat = (): string => {
	const date = new Date(2000, 0, 2); // January 2, 2000
	const formatter = new Intl.DateTimeFormat();
	const parts = formatter.formatToParts(date);
	let format = "";

	for (const part of parts) {
		switch (part.type) {
			case "day":
				format += "dd";
				break;
			case "month":
				format += "MM";
				break;
			case "year":
				format += "yyyy";
				break;
			default:
				format += part.value;
				break;
		}
	}

	return format;
};

export interface DateRangeSelectorProps {
	startDate: Date;
	endDate: Date;
	onStartDateChange: (date: Date | null) => void;
	onEndDateChange: (date: Date | null) => void;
	_testLocalDateFormat?: string; // Only used for testing
}

const DateRangeSelector: React.FC<DateRangeSelectorProps> = ({
	startDate,
	endDate,
	onStartDateChange,
	onEndDateChange,
	_testLocalDateFormat,
}) => {
	const theme = useTheme();
	const localDateFormat = _testLocalDateFormat ?? getLocaleDateFormat();

	return (
		<LocalizationProvider dateAdapter={AdapterDateFns}>
			<Box
				sx={{
					p: { xs: 1.5, sm: 2 },
					display: "flex",
					flexDirection: "column",
					gap: 2,
					width: "100%",
				}}
			>
				<Stack spacing={1}>
					<Typography
						variant="subtitle2"
						color="text.primary"
						fontWeight="medium"
					>
						Start Date
					</Typography>
					<DatePicker
						value={startDate}
						onChange={(newValue) => onStartDateChange(newValue as Date | null)}
						format={localDateFormat}
						sx={{
							width: "100%",
							"& .MuiInputBase-root": {
								borderColor: theme.palette.primary.main,
							},
						}}
						slotProps={{
							textField: {
								size: "small",
								fullWidth: true,
							},
							day: {
								sx: {
									"&.Mui-selected": {
										backgroundColor: theme.palette.primary.main,
									},
								},
							},
						}}
						maxDate={endDate}
					/>
				</Stack>

				<Stack spacing={1}>
					<Typography
						variant="subtitle2"
						color="text.primary"
						fontWeight="medium"
					>
						End Date
					</Typography>
					<DatePicker
						value={endDate}
						onChange={(newValue) => onEndDateChange(newValue as Date | null)}
						format={localDateFormat}
						sx={{
							width: "100%",
							"& .MuiInputBase-root": {
								borderColor: theme.palette.primary.main,
							},
						}}
						slotProps={{
							textField: {
								size: "small",
								fullWidth: true,
							},
							day: {
								sx: {
									"&.Mui-selected": {
										backgroundColor: theme.palette.primary.main,
									},
								},
							},
						}}
						minDate={startDate}
					/>
				</Stack>
			</Box>
		</LocalizationProvider>
	);
};

export default DateRangeSelector;
