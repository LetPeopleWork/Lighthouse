import {
	Button,
	FormControlLabel,
	Stack,
	Switch,
	TextField,
} from "@mui/material";
import type React from "react";
import { useContext, useState } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import FilteredThroughputChip from "../../Common/Forecasting/FilteredThroughputChip";

interface TeamForecastFormProps {
	teamId: number;
	hasFilter: boolean;
}

const TOGGLE_LABEL = "Apply forecast-throughput filter";

const TeamForecastForm: React.FC<TeamForecastFormProps> = ({
	teamId,
	hasFilter,
}) => {
	const { forecastService } = useContext(ApiServiceContext);
	const { licenseStatus } = useLicenseRestrictions();
	const isPremium = licenseStatus?.canUsePremiumFeatures ?? false;
	const toggleVisible = isPremium && hasFilter;

	const [remainingItems, setRemainingItems] = useState<number | null>(null);
	const [applyFilter, setApplyFilter] = useState(true);
	const [forecast, setForecast] = useState<ManualForecast | null>(null);

	const handleSubmit = async () => {
		const overrideForRequest = toggleVisible ? applyFilter : undefined;
		const result = await forecastService.runManualForecast(
			teamId,
			remainingItems ?? undefined,
			null,
			overrideForRequest,
		);
		setForecast(result);
	};

	return (
		<Stack spacing={2}>
			<TextField
				label="Remaining items"
				type="number"
				value={remainingItems ?? ""}
				onChange={(event) => {
					const raw = event.target.value;
					if (raw === "") {
						setRemainingItems(null);
						return;
					}
					const parsed = Number(raw);
					setRemainingItems(Number.isNaN(parsed) ? null : parsed);
				}}
			/>
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
				Forecast
			</Button>
			{forecast !== null && (
				<FilteredThroughputChip
					visible={forecast.filterApplied}
					excludedSummary={forecast.excludedSummary}
				/>
			)}
		</Stack>
	);
};

export default TeamForecastForm;
