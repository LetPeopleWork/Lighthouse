import { TextField } from "@mui/material";
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
			<Grid size={{ xs: 12 }}>
				<Grid container spacing={2}>
					<Grid size={{ xs: 4 }}>
						<TextField
							label="Number of Items to Forecast"
							type="number"
							fullWidth
							value={remainingItems}
							onChange={(e) => onRemainingItemsChange(Number(e.target.value))}
						/>
					</Grid>
					<Grid size={{ xs: 4 }}>
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
							/>
						</LocalizationProvider>
					</Grid>
					<Grid size={{ xs: 4 }}>
						<ActionButton
							onClickHandler={onRunManualForecast}
							buttonText="Forecast"
						/>
					</Grid>
				</Grid>
			</Grid>
			<Grid size={{ xs: 12 }}>
				{manualForecastResult && (
					<Grid container spacing={2}>
						<Grid size={{ xs: 4 }}>
							<ForecastInfoList
								title={`When will ${manualForecastResult.remainingItems} items be done?`}
								forecasts={manualForecastResult.whenForecasts}
							/>
						</Grid>
						<Grid size={{ xs: 4 }}>
							<ForecastInfoList
								title={`How Many Items will you get done till ${manualForecastResult.targetDate.toLocaleDateString()}?`}
								forecasts={manualForecastResult.howManyForecasts}
							/>
						</Grid>
						{manualForecastResult.likelihood > 0 && (
							<Grid size={{ xs: 4 }}>
								<ForecastLikelihood
									remainingItems={manualForecastResult.remainingItems}
									targetDate={manualForecastResult.targetDate}
									likelihood={manualForecastResult.likelihood}
								/>
							</Grid>
						)}
					</Grid>
				)}
			</Grid>
		</Grid>
	);
};

export default ManualForecaster;
