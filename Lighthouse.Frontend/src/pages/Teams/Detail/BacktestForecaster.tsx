import { Box, CircularProgress, Tab, Tabs, TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import dayjs from "dayjs";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
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
	teamId: number;
	onRunBacktest: (
		startDate: Date,
		endDate: Date,
		historicalWindowDays: number,
	) => Promise<void>;
	backtestResult: BacktestResult | null;
	onClearBacktestResult: () => void;
}

const BacktestForecaster: React.FC<BacktestForecasterProps> = ({
	teamId,
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
	const [activeTab, setActiveTab] = useState<number>(0);

	// Historical Throughput state
	const [historicalThroughput, setHistoricalThroughput] =
		useState<RunChartData | null>(null);
	const [predictabilityScore, setPredictabilityScore] =
		useState<IForecastPredictabilityScore | null>(null);
	const [isLoadingHistorical, setIsLoadingHistorical] =
		useState<boolean>(false);
	const [historicalError, setHistoricalError] = useState<string | null>(null);
	const [historicalStartDate, setHistoricalStartDate] = useState<Date | null>(
		null,
	);

	const { teamMetricsService } = useContext(ApiServiceContext);

	// Fetch historical data when backtest result changes
	useEffect(() => {
		if (!backtestResult) {
			setHistoricalThroughput(null);
			setPredictabilityScore(null);
			setHistoricalError(null);
			setHistoricalStartDate(null);
			return;
		}

		const fetchHistoricalData = async () => {
			setIsLoadingHistorical(true);
			setHistoricalError(null);

			try {
				// Compute historical window dates (date-only semantics)
				const historyEnd = dayjs(backtestResult.startDate).startOf("day");
				const historyStart = historyEnd.subtract(
					backtestResult.historicalWindowDays,
					"day",
				);
				const historyStartDate = historyStart.toDate();
				const historyEndDate = historyEnd.toDate();

				setHistoricalStartDate(historyStartDate);

				const [throughputData, predictability] = await Promise.all([
					teamMetricsService.getThroughput(
						teamId,
						historyStartDate,
						historyEndDate,
					),
					teamMetricsService.getMultiItemForecastPredictabilityScore(
						teamId,
						historyStartDate,
						historyEndDate,
					),
				]);

				setHistoricalThroughput(throughputData);
				setPredictabilityScore(predictability);
			} catch (error) {
				const errorMessage =
					error instanceof Error
						? error.message
						: "Failed to load historical throughput data.";
				setHistoricalError(errorMessage);
			} finally {
				setIsLoadingHistorical(false);
			}
		};

		fetchHistoricalData();
	}, [backtestResult, teamId, teamMetricsService]);

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
			{backtestResult && (
				<Grid size={{ xs: 12 }}>
					<Box sx={{ borderBottom: 1, borderColor: "divider", mb: 2 }}>
						<Tabs
							value={activeTab}
							onChange={(_event, newValue) => setActiveTab(newValue)}
							aria-label="Backtest result tabs"
						>
							<Tab
								label="Results"
								id="backtest-tab-0"
								aria-controls="backtest-tabpanel-0"
							/>
							<Tab
								label="Historical Throughput"
								id="backtest-tab-1"
								aria-controls="backtest-tabpanel-1"
							/>
						</Tabs>
					</Box>
					<Box
						role="tabpanel"
						hidden={activeTab !== 0}
						id="backtest-tabpanel-0"
						aria-labelledby="backtest-tab-0"
					>
						{activeTab === 0 && (
							<BacktestResultDisplay backtestResult={backtestResult} />
						)}
					</Box>
					<Box
						role="tabpanel"
						hidden={activeTab !== 1}
						id="backtest-tabpanel-1"
						aria-labelledby="backtest-tab-1"
					>
						{activeTab === 1 && (
							<>
								{isLoadingHistorical && (
									<Box sx={{ display: "flex", justifyContent: "center", p: 4 }}>
										<CircularProgress data-testid="loading-animation" />
									</Box>
								)}
								{historicalError && (
									<Box sx={{ color: "error.main", p: 2 }}>
										{historicalError}
									</Box>
								)}
								{historicalThroughput &&
									!isLoadingHistorical &&
									historicalStartDate && (
										<Box sx={{ height: 400 }}>
											<BarRunChart
												chartData={historicalThroughput}
												startDate={historicalStartDate}
												title="Historical Throughput"
												predictabilityData={predictabilityScore}
											/>
										</Box>
									)}
							</>
						)}
					</Box>
				</Grid>
			)}
		</Grid>
	);
};

export default BacktestForecaster;
