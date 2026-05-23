import { Button, FormControlLabel, Stack, Switch } from "@mui/material";
import type React from "react";
import { useContext, useState } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import FilteredThroughputChip from "../../Common/Forecasting/FilteredThroughputChip";

interface BacktestFormProps {
	teamId: number;
	hasFilter: boolean;
}

const TOGGLE_LABEL = "Apply forecast-throughput filter";

const daysAgo = (days: number): Date => {
	const date = new Date();
	date.setDate(date.getDate() - days);
	return date;
};

const BacktestForm: React.FC<BacktestFormProps> = ({ teamId, hasFilter }) => {
	const { forecastService } = useContext(ApiServiceContext);
	const { licenseStatus } = useLicenseRestrictions();
	const isPremium = licenseStatus?.canUsePremiumFeatures ?? false;
	const toggleVisible = isPremium && hasFilter;

	const [applyFilter, setApplyFilter] = useState(true);
	const [result, setResult] = useState<BacktestResult | null>(null);

	const handleSubmit = async () => {
		const overrideForRequest = toggleVisible ? applyFilter : undefined;
		const startDate = daysAgo(31);
		const endDate = daysAgo(1);
		const historicalEndDate = daysAgo(32);
		const historicalStartDate = daysAgo(62);
		const backtestResult = await forecastService.runBacktest(
			teamId,
			startDate,
			endDate,
			historicalStartDate,
			historicalEndDate,
			overrideForRequest,
		);
		setResult(backtestResult);
	};

	return (
		<Stack spacing={2}>
			{toggleVisible && (
				<FormControlLabel
					control={
						<Switch
							checked={applyFilter}
							onChange={(event) => setApplyFilter(event.target.checked)}
						/>
					}
					label={TOGGLE_LABEL}
				/>
			)}
			<Button onClick={handleSubmit} variant="contained">
				Run Backtest
			</Button>
			{result !== null && (
				<FilteredThroughputChip
					visible={result.filterApplied ?? false}
					excludedSummary={result.excludedSummary}
				/>
			)}
		</Stack>
	);
};

export default BacktestForm;
