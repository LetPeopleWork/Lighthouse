import { Close } from "@mui/icons-material";
import {
	IconButton,
	InputAdornment,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import dayjs from "dayjs";
import type React from "react";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import ForecastLikelihood from "../../../components/Common/Forecasts/ForecastLikelihood";
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

interface ManualForecasterProps {
	remainingItems: number;
	targetDate: dayjs.Dayjs | null;
	manualForecastResult: ManualForecast | null;
	onRemainingItemsChange: (value: number) => void;
	onTargetDateChange: (date: dayjs.Dayjs | null) => void;
	onRunManualForecast: () => Promise<void>;
}

const ManualForecaster: React.FC<ManualForecasterProps> = ({
	remainingItems,
	targetDate,
	manualForecastResult,
	onRemainingItemsChange,
	onTargetDateChange,
	onRunManualForecast,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 12, md: 6 }}>
				<Typography variant="subtitle2" gutterBottom>
					When?
				</Typography>
				<TextField
					label={`Number of ${workItemsTerm}`}
					type="number"
					fullWidth
					value={remainingItems}
					onChange={(e) => onRemainingItemsChange(Number(e.target.value))}
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
					How Many?
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
			<Grid
				size={{ xs: 12 }}
				sx={{ display: "flex", justifyContent: "flex-end", mt: 2 }}
			>
				<ActionButton
					onClickHandler={onRunManualForecast}
					buttonText="Forecast"
					disabled={!remainingItems && !targetDate}
				/>
			</Grid>
		</Grid>
	);
};

export default ManualForecaster;
