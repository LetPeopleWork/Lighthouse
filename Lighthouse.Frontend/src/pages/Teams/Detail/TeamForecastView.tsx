import { Grid } from "@mui/material";
import dayjs from "dayjs";
import type React from "react";
import {
	useCallback,
	useContext,
	useEffect,
	useMemo,
	useRef,
	useState,
} from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { useErrorSnackbar } from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import type {
	IFeatureCandidate,
	IForecastInputCandidates,
} from "../../../models/Forecasts/ForecastInputCandidates";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import BacktestForecaster, { type HistoricalMode } from "./BacktestForecaster";
import ManualForecaster from "./ManualForecaster";
import NewItemForecaster from "./NewItemForecaster";

const DEBOUNCE_MS = 300;

function useDebouncedRevisionRun(revision: number, run: () => void) {
	useEffect(() => {
		if (revision === 0) {
			return;
		}

		const timer = setTimeout(run, DEBOUNCE_MS);
		return () => clearTimeout(timer);
	}, [revision, run]);
}

interface TeamForecastViewProps {
	team: Team;
}

const TeamForecastView: React.FC<TeamForecastViewProps> = ({ team }) => {
	const [remainingItems, setRemainingItems] = useState<number | null>(null);
	const [targetDate, setTargetDate] = useState<dayjs.Dayjs | null>(null);
	const [manualForecastResult, setManualForecastResult] =
		useState<ManualForecast | null>(null);
	const [forecastInputCandidates, setForecastInputCandidates] =
		useState<IForecastInputCandidates | null>(null);
	const [forecastMode, setForecastMode] = useState<"manual" | "features">(
		"manual",
	);
	const [selectedFeatures, setSelectedFeatures] = useState<IFeatureCandidate[]>(
		[],
	);
	const [hasForecastFilter, setHasForecastFilter] = useState<boolean>(false);
	const [applyFilterOverride, setApplyFilterOverride] = useState<boolean>(true);
	const [applyBacktestFilterOverride, setApplyBacktestFilterOverride] =
		useState<boolean>(true);

	const featureAggregateRemainingWork = useMemo(
		() => selectedFeatures.reduce((sum, f) => sum + f.remainingWork, 0),
		[selectedFeatures],
	);

	const effectiveRemainingItems =
		forecastMode === "features"
			? featureAggregateRemainingWork
			: remainingItems;

	const [newItemForecastResult, setNewItemForecastResult] =
		useState<ManualForecast | null>(null);

	const [backtestResult, setBacktestResult] = useState<BacktestResult | null>(
		null,
	);

	const [backtestStartDate, setBacktestStartDate] =
		useState<dayjs.Dayjs | null>(() => dayjs().subtract(31, "day"));
	const [backtestEndDate, setBacktestEndDate] = useState<dayjs.Dayjs | null>(
		() => dayjs().subtract(1, "day"),
	);
	const [backtestHistoricalMode, setBacktestHistoricalMode] =
		useState<HistoricalMode>("rolling");
	const [backtestHistoricalWindowDays, setBacktestHistoricalWindowDays] =
		useState<number | "">(30);
	const [
		backtestHistoricalFixedStartDate,
		setBacktestHistoricalFixedStartDate,
	] = useState<dayjs.Dayjs | null>(() => dayjs().subtract(90, "day"));
	const [backtestHistoricalFixedEndDate, setBacktestHistoricalFixedEndDate] =
		useState<dayjs.Dayjs | null>(() => dayjs().subtract(60, "day"));
	const [backtestForecastRevision, setBacktestForecastRevision] = useState(0);
	const backtestRequestSeqRef = useRef(0);

	const hasInteractedRef = useRef(false);
	const requestSeqRef = useRef(0);

	const [newItemStartDate, setNewItemStartDate] = useState<dayjs.Dayjs | null>(
		() => dayjs().subtract(30, "day"),
	);
	const [newItemEndDate, setNewItemEndDate] = useState<dayjs.Dayjs | null>(() =>
		dayjs(),
	);
	const [newItemTargetDate, setNewItemTargetDate] =
		useState<dayjs.Dayjs | null>(() => dayjs().add(30, "day"));
	const [newItemWorkItemTypes, setNewItemWorkItemTypes] = useState<string[]>(
		[],
	);
	const [newItemForecastRevision, setNewItemForecastRevision] = useState(0);
	const newItemRequestSeqRef = useRef(0);

	const { forecastService, teamMetricsService, teamService } =
		useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();

	useEffect(() => {
		if (!team?.id || !teamService) {
			return;
		}

		let cancelled = false;
		teamService
			.getTeamSettings(team.id)
			.then((settings) => {
				if (cancelled) return;
				const json = settings.forecastFilterRuleSetJson;
				if (!json || json.trim() === "") {
					setHasForecastFilter(false);
					return;
				}
				try {
					const parsed = JSON.parse(json) as {
						conditions?: unknown[];
					};
					setHasForecastFilter((parsed.conditions ?? []).length > 0);
				} catch {
					setHasForecastFilter(false);
				}
			})
			.catch(() => {
				setHasForecastFilter(false);
			});

		return () => {
			cancelled = true;
		};
	}, [team?.id, teamService]);

	useEffect(() => {
		try {
			const rollingThroughputWindow =
				dayjs(team.throughputEndDate).diff(
					dayjs(team.throughputStartDate),
					"day",
				) + 1;

			if (team.useFixedDatesForThroughput) {
				setBacktestHistoricalMode("dateRange");
				setBacktestHistoricalFixedStartDate(dayjs(team.throughputStartDate));
				setBacktestHistoricalFixedEndDate(dayjs(team.throughputEndDate));
			} else {
				setBacktestHistoricalMode("rolling");
				setBacktestHistoricalWindowDays(rollingThroughputWindow);
			}
		} catch {
			// Defaults already initialise the backtest window; nothing to recover.
		}
	}, [team]);

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	useEffect(() => {
		if (!team?.id || !teamMetricsService) {
			return;
		}

		teamMetricsService
			.getForecastInputCandidates(team.id)
			.then(setForecastInputCandidates)
			.catch(() => {
				// Non-fatal: quick picks simply won't be shown.
			});
	}, [team?.id, teamMetricsService]);

	const runForecast = useCallback(
		async (
			items: number | null,
			date: dayjs.Dayjs | null,
			filterOverride: boolean | undefined,
		) => {
			if (!team?.id) {
				return;
			}

			if ((items === null || items === 0) && !date) {
				setManualForecastResult(null);
				return;
			}

			if (items === 0) {
				setManualForecastResult(null);
				return;
			}

			const seq = ++requestSeqRef.current;

			try {
				const result = await forecastService.runManualForecast(
					team.id,
					items ?? undefined,
					date?.toDate() ?? null,
					filterOverride,
				);

				if (seq === requestSeqRef.current) {
					setManualForecastResult(result);
				}
			} catch (error) {
				if (seq === requestSeqRef.current) {
					const errorMessage =
						error instanceof Error
							? error.message
							: "Failed to run manual forecast. Please try again.";
					showError(errorMessage);
				}
			}
		},
		[team?.id, forecastService, showError],
	);

	const isPremiumFilterActive = hasForecastFilter;
	const effectiveFilterOverride = isPremiumFilterActive
		? applyFilterOverride
		: undefined;

	useEffect(() => {
		if (!hasInteractedRef.current) {
			return;
		}

		const timer = setTimeout(() => {
			runForecast(effectiveRemainingItems, targetDate, effectiveFilterOverride);
		}, DEBOUNCE_MS);

		return () => clearTimeout(timer);
	}, [
		effectiveRemainingItems,
		targetDate,
		effectiveFilterOverride,
		runForecast,
	]);

	const handleRemainingItemsChange = useCallback((value: number | null) => {
		hasInteractedRef.current = true;
		setRemainingItems(value);
	}, []);

	const handleTargetDateChange = useCallback((date: dayjs.Dayjs | null) => {
		hasInteractedRef.current = true;
		setTargetDate(date);
	}, []);

	const handleModeChange = useCallback((newMode: "manual" | "features") => {
		hasInteractedRef.current = true;
		setForecastMode(newMode);
		setManualForecastResult(null);
	}, []);

	const handleFeatureSelectionChange = useCallback(
		(features: IFeatureCandidate[]) => {
			hasInteractedRef.current = true;
			setSelectedFeatures(features);
		},
		[],
	);

	const handleApplyFilterOverrideChange = useCallback((apply: boolean) => {
		hasInteractedRef.current = true;
		setApplyFilterOverride(apply);
	}, []);

	const runNewItemForecast = useCallback(async () => {
		if (
			!team?.id ||
			!newItemStartDate ||
			!newItemEndDate ||
			!newItemTargetDate
		) {
			return;
		}

		const seq = ++newItemRequestSeqRef.current;

		try {
			const newItemForecast = await forecastService.runItemPrediction(
				team.id,
				newItemStartDate.toDate(),
				newItemEndDate.toDate(),
				newItemTargetDate.toDate(),
				newItemWorkItemTypes,
			);
			if (seq === newItemRequestSeqRef.current) {
				setNewItemForecastResult(newItemForecast);
			}
		} catch (error) {
			if (seq === newItemRequestSeqRef.current) {
				const errorMessage =
					error instanceof Error
						? error.message
						: "Failed to run new item forecast. Please try again.";
				showError(errorMessage);
			}
		}
	}, [
		team?.id,
		forecastService,
		showError,
		newItemStartDate,
		newItemEndDate,
		newItemTargetDate,
		newItemWorkItemTypes,
	]);

	useDebouncedRevisionRun(newItemForecastRevision, runNewItemForecast);

	const handleNewItemInputChange = useCallback((complete: boolean) => {
		if (!complete) {
			setNewItemForecastResult(null);
			return;
		}

		setNewItemForecastRevision((revision) => revision + 1);
	}, []);

	const runBacktest = useCallback(async () => {
		if (!team?.id || !backtestStartDate || !backtestEndDate) {
			return;
		}

		let historicalStartDate: Date;
		let historicalEndDate: Date;

		if (
			backtestHistoricalMode === "dateRange" &&
			backtestHistoricalFixedStartDate &&
			backtestHistoricalFixedEndDate
		) {
			historicalStartDate = backtestHistoricalFixedStartDate.toDate();
			historicalEndDate = backtestHistoricalFixedEndDate.toDate();
		} else {
			const effectiveWindow =
				typeof backtestHistoricalWindowDays === "number" &&
				Number.isFinite(backtestHistoricalWindowDays)
					? backtestHistoricalWindowDays
					: 30;
			historicalEndDate = backtestStartDate.subtract(1, "day").toDate();
			historicalStartDate = backtestStartDate
				.subtract(effectiveWindow, "day")
				.toDate();
		}

		const seq = ++backtestRequestSeqRef.current;

		try {
			const filterOverride = isPremiumFilterActive
				? applyBacktestFilterOverride
				: undefined;
			const result = await forecastService.runBacktest(
				team.id,
				backtestStartDate.toDate(),
				backtestEndDate.toDate(),
				historicalStartDate,
				historicalEndDate,
				filterOverride,
			);
			if (seq === backtestRequestSeqRef.current) {
				setBacktestResult(result);
			}
		} catch (error) {
			if (seq === backtestRequestSeqRef.current) {
				const errorMessage =
					error instanceof Error
						? error.message
						: "Failed to run backtest. Please try again.";
				showError(errorMessage);
			}
		}
	}, [
		team?.id,
		forecastService,
		showError,
		backtestStartDate,
		backtestEndDate,
		backtestHistoricalMode,
		backtestHistoricalWindowDays,
		backtestHistoricalFixedStartDate,
		backtestHistoricalFixedEndDate,
		isPremiumFilterActive,
		applyBacktestFilterOverride,
	]);

	useDebouncedRevisionRun(backtestForecastRevision, runBacktest);

	const handleBacktestInputChange = useCallback((complete: boolean) => {
		if (!complete) {
			setBacktestResult(null);
			return;
		}

		setBacktestForecastRevision((revision) => revision + 1);
	}, []);

	return (
		<Grid container spacing={3}>
			<InputGroup title={`${teamTerm} Forecast`}>
				<ManualForecaster
					remainingItems={remainingItems}
					targetDate={targetDate}
					manualForecastResult={manualForecastResult}
					forecastInputCandidates={forecastInputCandidates}
					onRemainingItemsChange={handleRemainingItemsChange}
					onTargetDateChange={handleTargetDateChange}
					mode={forecastMode}
					selectedFeatures={selectedFeatures}
					onModeChange={handleModeChange}
					onFeatureSelectionChange={handleFeatureSelectionChange}
					hasForecastFilter={hasForecastFilter}
					applyFilterOverride={applyFilterOverride}
					onApplyFilterOverrideChange={handleApplyFilterOverrideChange}
				/>
			</InputGroup>
			<InputGroup title={`New ${workItemsTerm} Creation Forecast`}>
				<NewItemForecaster
					newItemForecastResult={newItemForecastResult}
					startDate={newItemStartDate}
					endDate={newItemEndDate}
					targetDate={newItemTargetDate}
					selectedWorkItemTypes={newItemWorkItemTypes}
					onStartDateChange={setNewItemStartDate}
					onEndDateChange={setNewItemEndDate}
					onTargetDateChange={setNewItemTargetDate}
					onWorkItemTypesChange={setNewItemWorkItemTypes}
					onInputChange={handleNewItemInputChange}
					workItemTypes={team.workItemTypes || []}
				/>
			</InputGroup>
			<InputGroup title="Forecast Backtesting">
				<BacktestForecaster
					team={team}
					backtestResult={backtestResult}
					onClearBacktestResult={() => setBacktestResult(null)}
					hasForecastFilter={hasForecastFilter}
					applyFilterOverride={applyBacktestFilterOverride}
					onApplyFilterOverrideChange={setApplyBacktestFilterOverride}
					startDate={backtestStartDate}
					endDate={backtestEndDate}
					historicalMode={backtestHistoricalMode}
					historicalWindowDays={backtestHistoricalWindowDays}
					historicalFixedStartDate={backtestHistoricalFixedStartDate}
					historicalFixedEndDate={backtestHistoricalFixedEndDate}
					onStartDateChange={setBacktestStartDate}
					onEndDateChange={setBacktestEndDate}
					onHistoricalModeChange={setBacktestHistoricalMode}
					onHistoricalWindowDaysChange={setBacktestHistoricalWindowDays}
					onHistoricalFixedStartDateChange={setBacktestHistoricalFixedStartDate}
					onHistoricalFixedEndDateChange={setBacktestHistoricalFixedEndDate}
					onInputChange={handleBacktestInputChange}
				/>
			</InputGroup>
		</Grid>
	);
};

export default TeamForecastView;
