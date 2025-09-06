import Alert from "@mui/material/Alert";
import Grid from "@mui/material/Grid";
import Typography from "@mui/material/Typography";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import dayjs from "dayjs";
import type React from "react";
import { useState } from "react";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface NewItemForecasterProps {
	newItemForecastResult: ManualForecast | null;
	onRunNewItemForecast: (
		startDate: Date,
		endDate: Date,
		targetDate: Date,
		workItemTypes: string[],
	) => Promise<void>;
	workItemTypes: string[];
	isDisabled?: boolean;
	disabledMessage?: string;
}

const NewItemForecaster: React.FC<NewItemForecasterProps> = ({
	newItemForecastResult,
	onRunNewItemForecast,
	workItemTypes,
	isDisabled = false,
	disabledMessage,
}) => {
	const [startDate, setStartDate] = useState<dayjs.Dayjs | null>(
		dayjs().subtract(30, "day"),
	);
	const [endDate, setEndDate] = useState<dayjs.Dayjs | null>(dayjs());
	const [targetDate, setTargetDate] = useState<dayjs.Dayjs | null>(
		dayjs().add(30, "day"),
	);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const handleRunForecast = async () => {
		if (!targetDate || !startDate || !endDate) return;

		const start = startDate.toDate();
		const end = endDate.toDate();
		const target = targetDate.toDate();

		await onRunNewItemForecast(start, end, target, workItemTypes);
	};

	function getLocaleDateFormat(): string {
		const date = new Date(2000, 0, 2);
		const formatter = new Intl.DateTimeFormat(undefined);
		const parts = formatter.formatToParts(date);
		const format = parts
			.map((part) => {
				switch (part.type) {
					case "day":
						return "DD";
					case "month":
						return "MM";
					case "year":
						return "YYYY";
					default:
						return part.value;
				}
			})
			.join("");

		return format;
	}

	return (
		<Grid container spacing={3}>
			{isDisabled && disabledMessage && (
				<Grid size={{ xs: 12 }}>
					<Alert severity="info" sx={{ mb: 2 }}>
						{disabledMessage}
					</Alert>
				</Grid>
			)}
			<Grid size={{ xs: 12 }}>
				<Grid container spacing={3}>
					{/* Historical Data Section */}
					<Grid size={{ xs: 6 }}>
						<Typography variant="h6" gutterBottom>
							Historical Data
						</Typography>
						<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
							Historical data that should be used for the forecast
						</Typography>
						<Grid container spacing={2}>
							<Grid size={{ xs: 12 }}>
								<LocalizationProvider dateAdapter={AdapterDayjs}>
									<DatePicker
										label="Start Date"
										value={startDate}
										onChange={(value) =>
											setStartDate(value as dayjs.Dayjs | null)
										}
										maxDate={endDate || undefined}
										format={getLocaleDateFormat()}
										sx={{ width: "100%" }}
										disabled={isDisabled}
									/>
								</LocalizationProvider>
							</Grid>
							<Grid size={{ xs: 12 }}>
								<LocalizationProvider dateAdapter={AdapterDayjs}>
									<DatePicker
										label="End Date"
										value={endDate}
										onChange={(value) =>
											setEndDate(value as dayjs.Dayjs | null)
										}
										minDate={startDate || undefined}
										maxDate={targetDate || undefined}
										format={getLocaleDateFormat()}
										sx={{ width: "100%" }}
										disabled={isDisabled}
									/>
								</LocalizationProvider>
							</Grid>
						</Grid>
					</Grid>

					{/* Target Date Section */}
					<Grid size={{ xs: 6 }}>
						<Typography variant="h6" gutterBottom>
							Target Date
						</Typography>
						<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
							How far into the future do you want to forecast
						</Typography>
						<LocalizationProvider dateAdapter={AdapterDayjs}>
							<DatePicker
								label="Target Date"
								value={targetDate}
								onChange={(value) => setTargetDate(value as dayjs.Dayjs | null)}
								minDate={endDate || dayjs()}
								format={getLocaleDateFormat()}
								sx={{ width: "100%" }}
								disabled={isDisabled}
							/>
						</LocalizationProvider>
					</Grid>
				</Grid>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<Grid
					container
					spacing={2}
					justifyContent="space-between"
					alignItems="flex-start"
				>
					<Grid size={{ xs: 8 }}>
						{newItemForecastResult && !isDisabled && (
							<ForecastInfoList
								title={`How many ${workItemsTerm} will you add until ${targetDate?.format("YYYY-MM-DD")}?`}
								forecasts={newItemForecastResult.howManyForecasts}
							/>
						)}
					</Grid>
					<Grid
						size={{ xs: 4 }}
						sx={{ display: "flex", justifyContent: "flex-end" }}
					>
						<ActionButton
							onClickHandler={handleRunForecast}
							buttonText="Forecast"
							disabled={isDisabled || !startDate || !endDate || !targetDate}
						/>
					</Grid>
				</Grid>
			</Grid>
		</Grid>
	);
};

export default NewItemForecaster;
