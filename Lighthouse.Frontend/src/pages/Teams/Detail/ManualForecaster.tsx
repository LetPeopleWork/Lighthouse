import { Close } from "@mui/icons-material";
import {
	Chip,
	IconButton,
	InputAdornment,
	Stack,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import dayjs from "dayjs";
import type React from "react";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import ForecastLikelihood from "../../../components/Common/Forecasts/ForecastLikelihood";
import type { IForecastInputCandidates } from "../../../models/Forecasts/ForecastInputCandidates";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

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

function getDaysUntilFriday(dayOfWeek: number): number {
	if (dayOfWeek === 6) return 6; // Saturday: next Friday is 6 days away
	if (dayOfWeek === 0) return 5; // Sunday: next Friday is 5 days away
	return 5 - dayOfWeek; // Mon–Fri: days remaining until Friday
}

function getEndOfWeek(): dayjs.Dayjs {
	const today = dayjs();
	const dayOfWeek = today.day(); // 0=Sun, 1=Mon, ..., 6=Sat
	return today.add(getDaysUntilFriday(dayOfWeek), "day");
}

function getEndOfMonth(): dayjs.Dayjs {
	return dayjs().endOf("month");
}

interface ManualForecasterProps {
	remainingItems: number;
	targetDate: dayjs.Dayjs | null;
	manualForecastResult: ManualForecast | null;
	forecastInputCandidates: IForecastInputCandidates | null;
	onRemainingItemsChange: (value: number) => void;
	onTargetDateChange: (date: dayjs.Dayjs | null) => void;
}

const ManualForecaster: React.FC<ManualForecasterProps> = ({
	remainingItems,
	targetDate,
	manualForecastResult,
	forecastInputCandidates,
	onRemainingItemsChange,
	onTargetDateChange,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const showZeroHint = remainingItems === 0;

	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 12, md: 6 }}>
				<Typography variant="subtitle2" gutterBottom>
					How many {workItemsTerm} need to be completed?
				</Typography>
				<TextField
					label={`Number of ${workItemsTerm}`}
					type="number"
					fullWidth
					value={remainingItems}
					onChange={(e) => onRemainingItemsChange(Number(e.target.value))}
					error={showZeroHint}
					helperText={
						showZeroHint
							? `Forecasting requires at least 1 remaining ${workItemsTerm.toLowerCase()}.`
							: undefined
					}
					slotProps={{
						input: {
							endAdornment: remainingItems > 0 && (
								<InputAdornment position="end">
									<IconButton
										aria-label="Clear remaining items"
										onClick={() => onRemainingItemsChange(0)}
										edge="end"
										size="small"
									>
										<Close />
									</IconButton>
								</InputAdornment>
							),
						},
					}}
				/>
				{forecastInputCandidates && (
					<Stack
						direction="row"
						spacing={1}
						sx={{ mt: 1, flexWrap: "wrap", gap: 0.5 }}
					>
						<Chip
							label={`Currently in Progress: ${forecastInputCandidates.currentWipCount}`}
							size="small"
							onClick={() =>
								onRemainingItemsChange(forecastInputCandidates.currentWipCount)
							}
							variant="outlined"
						/>
						<Chip
							label={`Backlog: ${forecastInputCandidates.backlogCount}`}
							size="small"
							onClick={() =>
								onRemainingItemsChange(forecastInputCandidates.backlogCount)
							}
							variant="outlined"
						/>
					</Stack>
				)}
				{manualForecastResult?.whenForecasts &&
					manualForecastResult.whenForecasts.length > 0 && (
						<Grid container sx={{ mt: 2 }}>
							<Grid size={{ xs: 12 }}>
								<ForecastInfoList
									title={`When will ${manualForecastResult.remainingItems} ${workItemsTerm} be done?`}
									forecasts={manualForecastResult.whenForecasts}
								/>
							</Grid>
						</Grid>
					)}
			</Grid>
			<Grid size={{ xs: 12, md: 6 }}>
				<Typography variant="subtitle2" gutterBottom>
					What is your target completion date?
				</Typography>
				<LocalizationProvider dateAdapter={AdapterDayjs}>
					<DatePicker
						label="Target Date"
						value={targetDate}
						onChange={(value) =>
							onTargetDateChange(value as dayjs.Dayjs | null)
						}
						minDate={dayjs()}
						format={getLocaleDateFormat()}
						sx={{ width: "100%" }}
						slotProps={{
							field: { clearable: true },
						}}
					/>
				</LocalizationProvider>
				<Stack
					direction="row"
					spacing={1}
					sx={{ mt: 1, flexWrap: "wrap", gap: 0.5 }}
				>
					<Chip
						label="End of week"
						size="small"
						onClick={() => onTargetDateChange(getEndOfWeek())}
						variant="outlined"
					/>
					<Chip
						label="End of month"
						size="small"
						onClick={() => onTargetDateChange(getEndOfMonth())}
						variant="outlined"
					/>
					<Chip
						label="+1 week"
						size="small"
						onClick={() =>
							onTargetDateChange((targetDate ?? dayjs()).add(1, "week"))
						}
						variant="outlined"
					/>
					<Chip
						label="+2 weeks"
						size="small"
						onClick={() =>
							onTargetDateChange((targetDate ?? dayjs()).add(2, "weeks"))
						}
						variant="outlined"
					/>
				</Stack>
				{manualForecastResult?.howManyForecasts &&
					manualForecastResult.howManyForecasts.length > 0 && (
						<Grid container sx={{ mt: 2 }}>
							<Grid size={{ xs: 12 }}>
								<ForecastInfoList
									title={`How Many ${workItemsTerm} will you get done till ${manualForecastResult.targetDate.toLocaleDateString()}?`}
									forecasts={manualForecastResult.howManyForecasts}
								/>
							</Grid>
						</Grid>
					)}
			</Grid>
			{manualForecastResult &&
				manualForecastResult.likelihood > 0 &&
				manualForecastResult.whenForecasts &&
				manualForecastResult.whenForecasts.length > 0 &&
				manualForecastResult.howManyForecasts &&
				manualForecastResult.howManyForecasts.length > 0 && (
					<Grid size={{ xs: 12 }}>
						<ForecastLikelihood
							remainingItems={manualForecastResult.remainingItems}
							targetDate={manualForecastResult.targetDate}
							likelihood={manualForecastResult.likelihood}
						/>
					</Grid>
				)}
		</Grid>
	);
};

export default ManualForecaster;
