import { Box, Card, Stack, Typography, useTheme } from "@mui/material";
import { DatePicker } from "@mui/x-date-pickers";
import { AdapterDateFns } from "@mui/x-date-pickers/AdapterDateFns";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
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
	const theme = useTheme();

	return (
		<LocalizationProvider dateAdapter={AdapterDateFns}>
			<Card
				elevation={1}
				sx={{
					width: "100%",
					m: 2,
					borderRadius: 2,
					p: 1,
					overflow: "visible",
				}}
			>
				<Box
					sx={{
						p: { xs: 1.5, sm: 2 },
						display: "flex",
						flexDirection: "column",
						gap: 2,
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
							onChange={(newValue) =>
								onStartDateChange(newValue as Date | null)
							}
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
			</Card>
		</LocalizationProvider>
	);
};

export default DateRangeSelector;
