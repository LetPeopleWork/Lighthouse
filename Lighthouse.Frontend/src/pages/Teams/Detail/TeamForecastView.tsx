import { Grid } from "@mui/material";
import dayjs from "dayjs";
import type React from "react";
import { useContext, useState } from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { useErrorSnackbar } from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import BacktestForecaster from "./BacktestForecaster";
import ManualForecaster from "./ManualForecaster";
import NewItemForecaster from "./NewItemForecaster";

interface TeamForecastViewProps {
	team: Team;
}

const TeamForecastView: React.FC<TeamForecastViewProps> = ({ team }) => {
	const [remainingItems, setRemainingItems] = useState<number>(10);
	const [targetDate, setTargetDate] = useState<dayjs.Dayjs | null>(
		dayjs().add(2, "week"),
	);
	const [manualForecastResult, setManualForecastResult] =
		useState<ManualForecast | null>(null);

	const [newItemForecastResult, setNewItemForecastResult] =
		useState<ManualForecast | null>(null);

	const [backtestResult, setBacktestResult] = useState<BacktestResult | null>(
		null,
	);

	const { forecastService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();
	const { canUseNewItemForecaster } = useLicenseRestrictions();

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const onRunManualForecast = async () => {
		if (!team || !targetDate) {
			return;
		}

		try {
			const manualForecast = await forecastService.runManualForecast(
				team.id,
				remainingItems,
				targetDate?.toDate(),
			);
			setManualForecastResult(manualForecast);
		} catch (error) {
			const errorMessage =
				error instanceof Error
					? error.message
					: "Failed to run manual forecast. Please try again.";
			showError(errorMessage);
		}
	};

	const onRunNewItemForecast = async (
		startDate: Date,
		endDate: Date,
		targetDate: Date,
		workItemTypes: string[],
	) => {
		if (!team?.id || !canUseNewItemForecaster) {
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
		historicalWindowDays: number,
	) => {
		if (!team?.id) {
			return;
		}

		try {
			const result = await forecastService.runBacktest(
				team.id,
				startDate,
				endDate,
				historicalWindowDays,
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
					onRemainingItemsChange={setRemainingItems}
					onTargetDateChange={setTargetDate}
					onRunManualForecast={onRunManualForecast}
				/>
			</InputGroup>
			<InputGroup title={`New ${workItemsTerm} Creation Forecast`}>
				<NewItemForecaster
					newItemForecastResult={newItemForecastResult}
					onRunNewItemForecast={onRunNewItemForecast}
					onClearForecastResult={() => setNewItemForecastResult(null)}
					workItemTypes={team.workItemTypes || []}
					isDisabled={!canUseNewItemForecaster}
					disabledMessage="Please obtain a premium license to use new item forecasting."
				/>
			</InputGroup>
			<InputGroup title="Forecast Backtesting">
				<BacktestForecaster
					teamId={team.id}
					onRunBacktest={onRunBacktest}
					backtestResult={backtestResult}
					onClearBacktestResult={() => setBacktestResult(null)}
				/>
			</InputGroup>
		</Grid>
	);
};

export default TeamForecastView;
