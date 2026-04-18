import { Grid } from "@mui/material";
import type dayjs from "dayjs";
import type React from "react";
import { useCallback, useContext, useEffect, useRef, useState } from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { useErrorSnackbar } from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import type { IForecastInputCandidates } from "../../../models/Forecasts/ForecastInputCandidates";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import BacktestForecaster from "./BacktestForecaster";
import ManualForecaster from "./ManualForecaster";
import NewItemForecaster from "./NewItemForecaster";

const DEBOUNCE_MS = 300;

interface TeamForecastViewProps {
	team: Team;
}

const TeamForecastView: React.FC<TeamForecastViewProps> = ({ team }) => {
	const [remainingItems, setRemainingItems] = useState<number>(10);
	const [targetDate, setTargetDate] = useState<dayjs.Dayjs | null>(null);
	const [manualForecastResult, setManualForecastResult] =
		useState<ManualForecast | null>(null);
	const [forecastInputCandidates, setForecastInputCandidates] =
		useState<IForecastInputCandidates | null>(null);

	const [newItemForecastResult, setNewItemForecastResult] =
		useState<ManualForecast | null>(null);

	const [backtestResult, setBacktestResult] = useState<BacktestResult | null>(
		null,
	);

	// Track whether the user has made at least one change (auto-run does NOT fire on mount)
	const hasInteractedRef = useRef(false);
	// Sequence counter to guard against stale responses
	const requestSeqRef = useRef(0);

	const { forecastService, teamMetricsService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	// Fetch quick-selection candidates once on mount
	useEffect(() => {
		if (!team?.id || !teamMetricsService) {
			return;
		}

		teamMetricsService
			.getForecastInputCandidates(team.id)
			.then(setForecastInputCandidates)
			.catch(() => {
				// Non-fatal: quick picks simply won't be shown
			});
	}, [team?.id, teamMetricsService]);

	const runForecast = useCallback(
		async (items: number, date: dayjs.Dayjs | null) => {
			if (!team?.id) {
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
					items,
					date?.toDate() ?? null,
				);

				// Discard stale responses
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

	// Auto-run on input changes with debounce — skips the initial render
	useEffect(() => {
		if (!hasInteractedRef.current) {
			return;
		}

		const timer = setTimeout(() => {
			runForecast(remainingItems, targetDate);
		}, DEBOUNCE_MS);

		return () => clearTimeout(timer);
	}, [remainingItems, targetDate, runForecast]);

	const handleRemainingItemsChange = useCallback((value: number) => {
		hasInteractedRef.current = true;
		setRemainingItems(value);
	}, []);

	const handleTargetDateChange = useCallback((date: dayjs.Dayjs | null) => {
		hasInteractedRef.current = true;
		setTargetDate(date);
	}, []);

	const onRunNewItemForecast = async (
		startDate: Date,
		endDate: Date,
		targetDate: Date,
		workItemTypes: string[],
	) => {
		if (!team?.id) {
			return;
		}

		try {
			const newItemForecast = await forecastService.runItemPrediction(
				team.id,
				startDate,
				endDate,
				targetDate,
				workItemTypes,
			);
			setNewItemForecastResult(newItemForecast);
		} catch (error) {
			const errorMessage =
				error instanceof Error
					? error.message
					: "Failed to run new item forecast. Please try again.";
			showError(errorMessage);
		}
	};

	const onRunBacktest = async (
		startDate: Date,
		endDate: Date,
		historicalStartDate: Date,
		historicalEndDate: Date,
	) => {
		if (!team?.id) {
			return;
		}

		try {
			const result = await forecastService.runBacktest(
				team.id,
				startDate,
				endDate,
				historicalStartDate,
				historicalEndDate,
			);
			setBacktestResult(result);
		} catch (error) {
			const errorMessage =
				error instanceof Error
					? error.message
					: "Failed to run backtest. Please try again.";
			showError(errorMessage);
		}
	};

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
				/>
			</InputGroup>
			<InputGroup title={`New ${workItemsTerm} Creation Forecast`}>
				<NewItemForecaster
					newItemForecastResult={newItemForecastResult}
					onRunNewItemForecast={onRunNewItemForecast}
					onClearForecastResult={() => setNewItemForecastResult(null)}
					workItemTypes={team.workItemTypes || []}
				/>
			</InputGroup>
			<InputGroup title="Forecast Backtesting">
				<BacktestForecaster
					team={team}
					onRunBacktest={onRunBacktest}
					backtestResult={backtestResult}
					onClearBacktestResult={() => setBacktestResult(null)}
				/>
			</InputGroup>
		</Grid>
	);
};

export default TeamForecastView;
