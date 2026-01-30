import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import dayjs from "dayjs";
import type React from "react";
import { useState } from "react";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import BacktestResultDisplay from "./BacktestResultDisplay";

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

interface BacktestForecasterProps {
	onRunBacktest: (
		startDate: Date,
		endDate: Date,
		historicalWindowDays: number,
	) => Promise<void>;
	backtestResult: BacktestResult | null;
	onClearBacktestResult: () => void;
}

const BacktestForecaster: React.FC<BacktestForecasterProps> = ({
	onRunBacktest,
	backtestResult,
	onClearBacktestResult,
}) => {
	const [startDate, setStartDate] = useState<dayjs.Dayjs | null>(
		dayjs().subtract(60, "day"),
	);
	const [endDate, setEndDate] = useState<dayjs.Dayjs | null>(
		dayjs().subtract(30, "day"),
	);
	const [historicalWindowDays, setHistoricalWindowDays] = useState<number>(30);

	const handleRunBacktest = async () => {
		if (!startDate || !endDate) {
			return;
		}

		onClearBacktestResult();

		await onRunBacktest(
			startDate.toDate(),
			endDate.toDate(),
			historicalWindowDays,
		);
	};

	const minStartDate = dayjs().subtract(365, "day");
	const maxEndDate = dayjs().subtract(14, "day");

	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 12 }}>
				<Grid container spacing={2}>
					<Grid size={{ xs: 3 }}>
						<LocalizationProvider dateAdapter={AdapterDayjs}>
							<DatePicker
								label="Backtest Start Date"
								value={startDate}
								onChange={(value) => setStartDate(value as dayjs.Dayjs | null)}
								minDate={minStartDate}
								maxDate={endDate?.subtract(14, "day") ?? maxEndDate}
								format={getLocaleDateFormat()}
								sx={{ width: "100%" }}
							/>
						</LocalizationProvider>
					</Grid>
					<Grid size={{ xs: 3 }}>
						<LocalizationProvider dateAdapter={AdapterDayjs}>
							<DatePicker
								label="Backtest End Date"
								value={endDate}
								onChange={(value) => setEndDate(value as dayjs.Dayjs | null)}
								minDate={startDate?.add(14, "day") ?? minStartDate}
								maxDate={maxEndDate}
								format={getLocaleDateFormat()}
								sx={{ width: "100%" }}
							/>
						</LocalizationProvider>
					</Grid>
					<Grid size={{ xs: 3 }}>
						<TextField
							label="Historical Window (Days)"
							type="number"
							fullWidth
							value={historicalWindowDays}
							onChange={(e) =>
								setHistoricalWindowDays(
									Math.max(1, Math.min(365, Number(e.target.value))),
								)
							}
							slotProps={{
								htmlInput: { min: 1, max: 365 },
							}}
						/>
					</Grid>
					<Grid size={{ xs: 3 }}>
						<ActionButton
							onClickHandler={handleRunBacktest}
							buttonText="Run Backtest"
						/>
					</Grid>
				</Grid>
			</Grid>
			<Grid size={{ xs: 12 }}>
				{backtestResult && (
					<BacktestResultDisplay backtestResult={backtestResult} />
				)}
			</Grid>
		</Grid>
	);
};

export default BacktestForecaster;
