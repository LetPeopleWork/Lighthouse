import {
	Box,
	CircularProgress,
	FormControlLabel,
	Switch,
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
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { ITeam } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
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

export type HistoricalMode = "rolling" | "dateRange";

interface BacktestForecasterProps {
	team: ITeam;
	backtestResult: BacktestResult | null;
	onClearBacktestResult: () => void;
	hasForecastFilter?: boolean;
	applyFilterOverride?: boolean;
	onApplyFilterOverrideChange?: (apply: boolean) => void;
	startDate: dayjs.Dayjs | null;
	endDate: dayjs.Dayjs | null;
	historicalMode: HistoricalMode;
	historicalWindowDays: number | "";
	historicalFixedStartDate: dayjs.Dayjs | null;
	historicalFixedEndDate: dayjs.Dayjs | null;
	onStartDateChange: (value: dayjs.Dayjs | null) => void;
	onEndDateChange: (value: dayjs.Dayjs | null) => void;
	onHistoricalModeChange: (mode: HistoricalMode) => void;
	onHistoricalWindowDaysChange: (value: number | "") => void;
	onHistoricalFixedStartDateChange: (value: dayjs.Dayjs | null) => void;
	onHistoricalFixedEndDateChange: (value: dayjs.Dayjs | null) => void;
	onInputChange: (complete: boolean) => void;
}

const BacktestForecaster: React.FC<BacktestForecasterProps> = ({
	team,
	backtestResult,
	onClearBacktestResult,
	hasForecastFilter = false,
	applyFilterOverride = true,
	onApplyFilterOverrideChange,
	startDate,
	endDate,
	historicalMode,
	historicalWindowDays,
	historicalFixedStartDate,
	historicalFixedEndDate,
	onStartDateChange,
	onEndDateChange,
	onHistoricalModeChange,
	onHistoricalWindowDaysChange,
	onHistoricalFixedStartDateChange,
	onHistoricalFixedEndDateChange,
	onInputChange,
}) => {
	const { licenseStatus } = useLicenseRestrictions();
	const isPremium = licenseStatus?.canUsePremiumFeatures ?? false;
	const showFilterToggle = isPremium && hasForecastFilter;
	const { getTerm } = useTerminology();
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);
	const filterToggleLabel = `Use filtered ${throughputTerm}`;
	const [activeTab, setActiveTab] = useState<number>(0);

	// Historical Throughput state
	const [historicalThroughput, setHistoricalThroughput] =
		useState<RunChartData | null>(null);
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

	const reportInputChange = (
		nextStartDate: dayjs.Dayjs | null,
		nextEndDate: dayjs.Dayjs | null,
		nextMode: HistoricalMode,
		nextWindowDays: number | "",
		nextFixedStartDate: dayjs.Dayjs | null,
		nextFixedEndDate: dayjs.Dayjs | null,
	) => {
		const datesComplete = nextStartDate !== null && nextEndDate !== null;
		const historicalComplete =
			nextMode === "dateRange"
				? nextFixedStartDate !== null && nextFixedEndDate !== null
				: typeof nextWindowDays === "number" &&
					Number.isFinite(nextWindowDays) &&
					nextWindowDays > 0;
		onInputChange(datesComplete && historicalComplete);
	};

	const handleStartDateChange = (value: dayjs.Dayjs | null) => {
		onStartDateChange(value);
		reportInputChange(
			value,
			endDate,
			historicalMode,
			historicalWindowDays,
			historicalFixedStartDate,
			historicalFixedEndDate,
		);
	};

	const handleEndDateChange = (value: dayjs.Dayjs | null) => {
		onEndDateChange(value);
		reportInputChange(
			startDate,
			value,
			historicalMode,
			historicalWindowDays,
			historicalFixedStartDate,
			historicalFixedEndDate,
		);
	};

	const handleHistoricalModeChange = (mode: HistoricalMode) => {
		onHistoricalModeChange(mode);
		reportInputChange(
			startDate,
			endDate,
			mode,
			historicalWindowDays,
			historicalFixedStartDate,
			historicalFixedEndDate,
		);
	};

	const handleHistoricalWindowDaysChange = (value: number | "") => {
		onHistoricalWindowDaysChange(value);
		reportInputChange(
			startDate,
			endDate,
			historicalMode,
			value,
			historicalFixedStartDate,
			historicalFixedEndDate,
		);
	};

	const handleHistoricalFixedStartDateChange = (value: dayjs.Dayjs | null) => {
		onHistoricalFixedStartDateChange(value);
		reportInputChange(
			startDate,
			endDate,
			historicalMode,
			historicalWindowDays,
			value,
			historicalFixedEndDate,
		);
	};

	const handleHistoricalFixedEndDateChange = (value: dayjs.Dayjs | null) => {
		onHistoricalFixedEndDateChange(value);
		reportInputChange(
			startDate,
			endDate,
			historicalMode,
			historicalWindowDays,
			historicalFixedStartDate,
			value,
		);
	};

	// Fetch historical and actual period data when backtest result changes
	useEffect(() => {
		if (!backtestResult) {
			setHistoricalThroughput(null);
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

				const shouldRequestFiltered =
					applyFilterOverride === true && hasForecastFilter && isPremium;
				const fetchThroughput = (start: Date, end: Date) =>
					shouldRequestFiltered
						? teamMetricsService.getThroughput(team.id, start, end, "filtered")
						: teamMetricsService.getThroughput(team.id, start, end);

				const [throughputData, actualThroughputData] = await Promise.all([
					fetchThroughput(historyStartDate, historyEndDate),
					fetchThroughput(backtestResult.startDate, backtestResult.endDate),
				]);

				setHistoricalThroughput(throughputData);
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
	}, [
		backtestResult,
		team,
		teamMetricsService,
		applyFilterOverride,
		hasForecastFilter,
		isPremium,
	]);

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
								onChange={(value) =>
									handleStartDateChange(value as dayjs.Dayjs | null)
								}
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
								onChange={(value) =>
									handleEndDateChange(value as dayjs.Dayjs | null)
								}
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
										handleHistoricalModeChange(newMode);
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
									onChange={(e) => {
										const raw = e.target.value;
										handleHistoricalWindowDaysChange(
											raw === "" ? "" : Number(raw),
										);
									}}
									onBlur={() => {
										const n =
											typeof historicalWindowDays === "number"
												? historicalWindowDays
												: 30;
										handleHistoricalWindowDaysChange(
											Math.max(1, Math.min(365, Number.isFinite(n) ? n : 30)),
										);
									}}
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
												handleHistoricalFixedStartDateChange(
													value as dayjs.Dayjs | null,
												)
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
												handleHistoricalFixedEndDateChange(
													value as dayjs.Dayjs | null,
												)
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
					{showFilterToggle && (
						<Grid size={{ xs: 12 }}>
							<FormControlLabel
								control={
									<Switch
										checked={applyFilterOverride}
										onChange={(event) => {
											onApplyFilterOverrideChange?.(event.target.checked);
											onClearBacktestResult();
										}}
									/>
								}
								label={filterToggleLabel}
							/>
						</Grid>
					)}
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
