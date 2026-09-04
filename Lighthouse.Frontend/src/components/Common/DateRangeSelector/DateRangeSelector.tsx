import { Box, Stack, Typography, useTheme } from "@mui/material";
import { DatePicker } from "@mui/x-date-pickers";
import { AdapterDateFns } from "@mui/x-date-pickers/AdapterDateFns";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import type React from "react";
import { useEffect, useRef, useState } from "react";
import { isValidDate } from "../../../utils/date/isValidDate";

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

interface BoundedDatePickerProps {
	label: string;
	value: Date;
	onValueChange: (date: Date) => void;
	format: string;
	minDate?: Date;
	maxDate?: Date;
}

/**
 * A date field that only reports a date its caller can actually use.
 *
 * The picker hands out whatever the field currently spells, including the
 * unparseable half-dates it passes through while someone types — "00.07.2026"
 * after the first keystroke of "05.07.2026". Those are still `Date` objects, so
 * a caller checking for null waves them through and something further away
 * breaks on them. The intermediate dates that do parse are no better: while the
 * year is being typed the field briefly reads "05.07.0002", a perfectly real
 * date nobody asked for and a dashboard nobody wants to load. So the field keeps
 * its own draft, and a date leaves here only once the edit is finished and the
 * result sits inside the allowed range.
 */
const BoundedDatePicker: React.FC<BoundedDatePickerProps> = ({
	label,
	value,
	onValueChange,
	format,
	minDate,
	maxDate,
}) => {
	const theme = useTheme();
	const [draft, setDraft] = useState<Date | null>(value);
	const [calendarOpen, setCalendarOpen] = useState(false);

	useEffect(() => {
		setDraft((current) =>
			current?.getTime() === value.getTime() ? current : value,
		);
	}, [value]);

	const isUsable = (candidate: Date | null): candidate is Date => {
		if (!isValidDate(candidate)) {
			return false;
		}

		if (minDate && candidate < minDate) {
			return false;
		}

		if (maxDate && candidate > maxDate) {
			return false;
		}

		return candidate.getTime() !== value.getTime();
	};

	const finishEdit = (candidate: Date | null) => {
		if (isUsable(candidate)) {
			onValueChange(candidate);
		} else {
			setDraft(value);
		}
	};

	// This field lives inside a popover, and clicking the backdrop tears it down
	// without the browser ever reporting that the field lost focus. Hand the
	// finished date over on the way out, or someone types a date, clicks away,
	// and the dashboard silently ignores what they typed.
	const flushOnUnmount = useRef<() => void>(() => {});

	useEffect(() => {
		flushOnUnmount.current = () => {
			if (isUsable(draft)) {
				onValueChange(draft);
			}
		};
	});

	useEffect(() => () => flushOnUnmount.current(), []);

	return (
		<Stack spacing={1}>
			<Typography
				variant="subtitle2"
				color="text.primary"
				sx={{ fontWeight: "medium" }}
			>
				{label}
			</Typography>
			<DatePicker
				value={draft}
				onChange={(newValue) => setDraft(newValue as Date | null)}
				open={calendarOpen}
				onOpen={() => setCalendarOpen(true)}
				onClose={() => setCalendarOpen(false)}
				onAccept={(newValue) => {
					if (calendarOpen) {
						finishEdit(newValue as Date | null);
					}
				}}
				format={format}
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
						onBlur: () => finishEdit(draft),
					},
					day: {
						sx: {
							"&.Mui-selected": {
								backgroundColor: theme.palette.primary.main,
							},
						},
					},
				}}
				minDate={minDate}
				maxDate={maxDate}
			/>
		</Stack>
	);
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
				<BoundedDatePicker
					label="Start Date"
					value={startDate}
					onValueChange={onStartDateChange}
					format={localDateFormat}
					maxDate={endDate}
				/>

				<BoundedDatePicker
					label="End Date"
					value={endDate}
					onValueChange={onEndDateChange}
					format={localDateFormat}
					minDate={startDate}
				/>
			</Box>
		</LocalizationProvider>
	);
};

export default DateRangeSelector;
