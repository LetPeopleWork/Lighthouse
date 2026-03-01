import {
	Box,
	CircularProgress,
	Tab,
	Tabs,
	TextField,
	ToggleButton,
	ToggleButtonGroup,
} from "@mui/material";
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
import type { ITeam } from "../../../models/Team/Team";
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

type HistoricalMode = "rolling" | "dateRange";

interface BacktestForecasterProps {
	team: ITeam;
	onRunBacktest: (
		startDate: Date,
		endDate: Date,
		historicalStartDate: Date,
		historicalEndDate: Date,
	) => Promise<void>;
	backtestResult: BacktestResult | null;
	onClearBacktestResult: () => void;
}

const BacktestForecaster: React.FC<BacktestForecasterProps> = ({
	team,
	onRunBacktest,
	backtestResult,
	onClearBacktestResult,
}) => {
	const [startDate, setStartDate] = useState<dayjs.Dayjs | null>(
		dayjs().subtract(31, "day"),
	);
	const [endDate, setEndDate] = useState<dayjs.Dayjs | null>(
		dayjs().subtract(1, "day"),
	);
	const [historicalWindowDays, setHistoricalWindowDays] = useState<number>(30);
	const [historicalMode, setHistoricalMode] =
		useState<HistoricalMode>("rolling");
	const [historicalFixedStartDate, setHistoricalFixedStartDate] =
		useState<dayjs.Dayjs | null>(dayjs().subtract(90, "day"));
	const [historicalFixedEndDate, setHistoricalFixedEndDate] =
		useState<dayjs.Dayjs | null>(dayjs().subtract(60, "day"));
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

	// Actual Period Throughput state
	const [actualPeriodThroughput, setActualPeriodThroughput] =
		useState<RunChartData | null>(null);
	const [isLoadingActual, setIsLoadingActual] = useState<boolean>(false);
	const [actualError, setActualError] = useState<string | null>(null);

	const { teamMetricsService } = useContext(ApiServiceContext);

	// Fetch team settings on mount to pre-fill the historical window
	useEffect(() => {
		try {
			const rollingThroughputWindow =
				Math.floor(
					(team.throughputEndDate.getTime() -
						team.throughputStartDate.getTime()) /
						(1000 * 60 * 60 * 24),
				) + 1;

			if (team.useFixedDatesForThroughput) {
				setHistoricalMode("dateRange");
				setHistoricalFixedStartDate(dayjs(team.throughputStartDate));
				setHistoricalFixedEndDate(dayjs(team.throughputEndDate));
			} else {
				setHistoricalMode("rolling");
				setHistoricalWindowDays(rollingThroughputWindow);
			}
		} catch {
			// Keep default values on error
		}
	}, [team]);

	// Fetch historical and actual period data when backtest result changes
	useEffect(() => {
		if (!backtestResult) {
			setHistoricalThroughput(null);
			setPredictabilityScore(null);
			setHistoricalError(null);
			setHistoricalStartDate(null);
			setActualPeriodThroughput(null);
			setActualError(null);
			return;
		}

		const fetchData = async () => {
			setIsLoadingHistorical(true);
			setIsLoadingActual(true);
			setHistoricalError(null);
			setActualError(null);

			try {
				const historyStartDate = backtestResult.historicalStartDate;
				const historyEndDate = backtestResult.historicalEndDate;

				setHistoricalStartDate(historyStartDate);

				const [throughputData, predictability, actualThroughputData] =
					await Promise.all([
						teamMetricsService.getThroughput(
							team.id,
							historyStartDate,
							historyEndDate,
						),
						teamMetricsService.getMultiItemForecastPredictabilityScore(
							team.id,
							historyStartDate,
							historyEndDate,
						),
						teamMetricsService.getThroughput(
							team.id,
							backtestResult.startDate,
							backtestResult.endDate,
						),
					]);

				setHistoricalThroughput(throughputData);
				setPredictabilityScore(predictability);
				setActualPeriodThroughput(actualThroughputData);
			} catch (error) {
				const errorMessage =
					error instanceof Error
						? error.message
						: "Failed to load throughput data.";
				setHistoricalError(errorMessage);
				setActualError(errorMessage);
			} finally {
				setIsLoadingHistorical(false);
				setIsLoadingActual(false);
			}
		};

		fetchData();
	}, [backtestResult, team, teamMetricsService]);

	const handleRunBacktest = async () => {
		if (!startDate || !endDate) {
			return;
		}

		onClearBacktestResult();

		let effectiveHistoricalStartDate: Date;
		let effectiveHistoricalEndDate: Date;

		if (
			historicalMode === "dateRange" &&
			historicalFixedStartDate &&
			historicalFixedEndDate
		) {
			effectiveHistoricalStartDate = historicalFixedStartDate.toDate();
			effectiveHistoricalEndDate = historicalFixedEndDate.toDate();
		} else {
			effectiveHistoricalEndDate = startDate.toDate();
			effectiveHistoricalStartDate = startDate
				.subtract(historicalWindowDays, "day")
				.toDate();
		}

		await onRunBacktest(
			startDate.toDate(),
			endDate.toDate(),
			effectiveHistoricalStartDate,
			effectiveHistoricalEndDate,
		);
	};

	const minStartDate = dayjs().subtract(365, "day");
	const maxStartDate = dayjs().subtract(14, "day");
	const maxEndDate = dayjs();

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
								maxDate={endDate?.subtract(14, "day") ?? maxStartDate}
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
						<Box sx={{ display: "flex", flexDirection: "column", gap: 1 }}>
							<ToggleButtonGroup
								value={historicalMode}
								exclusive
								onChange={(_event, newMode: HistoricalMode | null) => {
									if (newMode !== null) {
										setHistoricalMode(newMode);
									}
								}}
								size="small"
								aria-label="Historical window mode"
								sx={{ width: "100%" }}
							>
								<ToggleButton
									value="rolling"
									aria-label="Rolling Period"
									data-testid="toggle-rolling"
									sx={{ flex: 1 }}
								>
									Rolling Period
								</ToggleButton>
								<ToggleButton
									value="dateRange"
									aria-label="Date Range"
									data-testid="toggle-date-range"
									sx={{ flex: 1 }}
								>
									Date Range
								</ToggleButton>
							</ToggleButtonGroup>
							{historicalMode === "rolling" ? (
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
							) : (
								<LocalizationProvider dateAdapter={AdapterDayjs}>
									<Box sx={{ display: "flex", flexDirection: "row", gap: 1 }}>
										<DatePicker
											label="Historical Start Date"
											value={historicalFixedStartDate}
											onChange={(value) =>
												setHistoricalFixedStartDate(value as dayjs.Dayjs | null)
											}
											maxDate={
												historicalFixedEndDate?.subtract(1, "day") ??
												minStartDate
											}
											format={getLocaleDateFormat()}
											sx={{ width: "100%" }}
										/>
										<DatePicker
											label="Historical End Date"
											value={historicalFixedEndDate}
											onChange={(value) =>
												setHistoricalFixedEndDate(value as dayjs.Dayjs | null)
											}
											minDate={
												historicalFixedStartDate?.add(1, "day") ?? minStartDate
											}
											maxDate={startDate?.subtract(1, "day") ?? maxStartDate}
											format={getLocaleDateFormat()}
											sx={{ width: "100%" }}
										/>
									</Box>
								</LocalizationProvider>
							)}
						</Box>
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
							<Tab
								label="Actual Throughput"
								id="backtest-tab-2"
								aria-controls="backtest-tabpanel-2"
							/>
						</Tabs>
					</Box>
					<Box
						role="tabpanel"
						hidden={activeTab !== 0}
						id="backtest-tabpanel-0"
						aria-labelledby="backtest-tab-0"
						sx={{ minHeight: 550 }}
					>
						{activeTab === 0 && (
							<BacktestResultDisplay
								backtestResult={backtestResult}
								historicalThroughput={historicalThroughput}
							/>
						)}
					</Box>
					<Box
						role="tabpanel"
						hidden={activeTab !== 1}
						id="backtest-tabpanel-1"
						aria-labelledby="backtest-tab-1"
						sx={{ minHeight: 550 }}
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
										<Box sx={{ height: 550 }}>
											<BarRunChart
												chartData={historicalThroughput}
												startDate={historicalStartDate}
												title="Historical Throughput"
												displayTotal={true}
												predictabilityData={predictabilityScore}
											/>
										</Box>
									)}
							</>
						)}
					</Box>
					<Box
						role="tabpanel"
						hidden={activeTab !== 2}
						id="backtest-tabpanel-2"
						aria-labelledby="backtest-tab-2"
						sx={{ minHeight: 550 }}
					>
						{activeTab === 2 && (
							<>
								{isLoadingActual && (
									<Box sx={{ display: "flex", justifyContent: "center", p: 4 }}>
										<CircularProgress data-testid="loading-animation-actual" />
									</Box>
								)}
								{actualError && (
									<Box sx={{ color: "error.main", p: 2 }}>{actualError}</Box>
								)}
								{actualPeriodThroughput && !isLoadingActual && (
									<Box sx={{ height: 550 }}>
										<BarRunChart
											chartData={actualPeriodThroughput}
											startDate={backtestResult.startDate}
											title="Actual Throughput"
											displayTotal={true}
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
