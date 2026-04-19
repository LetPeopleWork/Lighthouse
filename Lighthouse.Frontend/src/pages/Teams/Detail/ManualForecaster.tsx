import { Close } from "@mui/icons-material";
import {
	Autocomplete,
	Chip,
	IconButton,
	InputAdornment,
	Stack,
	TextField,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import dayjs from "dayjs";
import type React from "react";
import { useState } from "react";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import ForecastLikelihood from "../../../components/Common/Forecasts/ForecastLikelihood";
import type {
	IFeatureCandidate,
	IForecastInputCandidates,
} from "../../../models/Forecasts/ForecastInputCandidates";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

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

function getDaysUntilFriday(dayOfWeek: number): number {
	if (dayOfWeek === 6) return 6; // Saturday: next Friday is 6 days away
	if (dayOfWeek === 0) return 5; // Sunday: next Friday is 5 days away
	return 5 - dayOfWeek; // Mon–Fri: days remaining until Friday
}

function getEndOfWeek(): dayjs.Dayjs {
	const today = dayjs();
	const dayOfWeek = today.day(); // 0=Sun, 1=Mon, ..., 6=Sat
	return today.add(getDaysUntilFriday(dayOfWeek), "day");
}

function getEndOfMonth(): dayjs.Dayjs {
	return dayjs().endOf("month");
}

interface ManualForecasterProps {
	remainingItems: number | null;
	targetDate: dayjs.Dayjs | null;
	manualForecastResult: ManualForecast | null;
	forecastInputCandidates: IForecastInputCandidates | null;
	onRemainingItemsChange: (value: number | null) => void;
	onTargetDateChange: (date: dayjs.Dayjs | null) => void;
	mode?: "manual" | "features";
	selectedFeatures?: IFeatureCandidate[];
	onModeChange?: (mode: "manual" | "features") => void;
	onFeatureSelectionChange?: (features: IFeatureCandidate[]) => void;
}

const ManualForecaster: React.FC<ManualForecasterProps> = ({
	remainingItems,
	targetDate,
	manualForecastResult,
	forecastInputCandidates,
	onRemainingItemsChange,
	onTargetDateChange,
	mode = "manual",
	selectedFeatures = [],
	onModeChange,
	onFeatureSelectionChange,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const [featureInputValue, setFeatureInputValue] = useState("");

	const featureAggregate = selectedFeatures.reduce(
		(sum, f) => sum + f.remainingWork,
		0,
	);
	const showZeroHint =
		mode === "manual" ? remainingItems === 0 : featureAggregate === 0;

	const availableFeatures = (forecastInputCandidates?.features ?? []).filter(
		(f) => !selectedFeatures.some((s) => s.id === f.id),
	);

	const handleFeatureClick = (feature: IFeatureCandidate) => {
		const alreadySelected = selectedFeatures.some((f) => f.id === feature.id);
		if (!alreadySelected) {
			onFeatureSelectionChange?.([...selectedFeatures, feature]);
		}
	};

	const handleFeatureRemove = (feature: IFeatureCandidate) => {
		onFeatureSelectionChange?.(
			selectedFeatures.filter((f) => f.id !== feature.id),
		);
	};

	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 12 }}>
				<ToggleButtonGroup
					value={mode}
					exclusive
					onChange={(_, newMode) => {
						if (newMode) {
							if (mode === "features" && newMode === "manual") {
								onRemainingItemsChange(featureAggregate);
							}
							onModeChange?.(newMode);
						}
					}}
					aria-label="Forecast mode"
					size="small"
				>
					<ToggleButton value="manual" aria-label="Manual">
						Manual
					</ToggleButton>
					<ToggleButton value="features" aria-label="Features">
						Features
					</ToggleButton>
				</ToggleButtonGroup>
			</Grid>
			<Grid size={{ xs: 12, md: 6 }}>
				<Typography variant="subtitle2" gutterBottom>
					How many {workItemsTerm} need to be completed?
				</Typography>

				{mode === "manual" && (
					<>
						<TextField
							label={`Number of ${workItemsTerm}`}
							type="number"
							fullWidth
							value={remainingItems ?? ""}
							onChange={(e) => {
								const rawValue = e.target.value;
								if (rawValue === "") {
									onRemainingItemsChange(null);
									return;
								}

								const parsedValue = Number(rawValue);
								onRemainingItemsChange(
									Number.isNaN(parsedValue) ? null : parsedValue,
								);
							}}
							error={showZeroHint}
							helperText={
								showZeroHint
									? `Forecasting requires at least 1 remaining ${workItemsTerm.toLowerCase()}.`
									: undefined
							}
							slotProps={{
								input: {
									endAdornment: remainingItems !== null && (
										<InputAdornment position="end">
											<IconButton
												aria-label="Clear remaining items"
												onClick={() => onRemainingItemsChange(null)}
												edge="end"
												size="small"
											>
												<Close />
											</IconButton>
										</InputAdornment>
									),
								},
							}}
						/>
						{forecastInputCandidates && (
							<Stack
								direction="row"
								spacing={1}
								sx={{ mt: 1, flexWrap: "wrap", gap: 0.5 }}
							>
								<Chip
									label={`Currently in Progress: ${forecastInputCandidates.currentWipCount}`}
									size="small"
									onClick={() =>
										onRemainingItemsChange(
											forecastInputCandidates.currentWipCount,
										)
									}
									variant="outlined"
								/>
								<Chip
									label={`Backlog: ${forecastInputCandidates.backlogCount}`}
									size="small"
									onClick={() =>
										onRemainingItemsChange(forecastInputCandidates.backlogCount)
									}
									variant="outlined"
								/>
							</Stack>
						)}
					</>
				)}

				{mode === "features" && (
					<>
						{(forecastInputCandidates?.features ?? []).length === 0 ? (
							<Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
								No features available
							</Typography>
						) : (
							<Autocomplete
								options={availableFeatures}
								getOptionLabel={(option) => option.name}
								value={null}
								inputValue={featureInputValue}
								onInputChange={(_, newValue, reason) => {
									if (reason !== "reset") setFeatureInputValue(newValue);
								}}
								onChange={(_, selectedOption) => {
									if (selectedOption) {
										handleFeatureClick(selectedOption);
										setFeatureInputValue("");
									}
								}}
								noOptionsText="No more features to add"
								disableCloseOnSelect={false}
								blurOnSelect={true}
								renderInput={(params) => (
									<TextField
										{...params}
										label="Add Feature"
										size="small"
										fullWidth
									/>
								)}
							/>
						)}

						{selectedFeatures.length > 0 && (
							<Stack
								direction="row"
								spacing={1}
								sx={{ mt: 1, flexWrap: "wrap", gap: 0.5 }}
							>
								{selectedFeatures.map((feature) => (
									<Chip
										key={feature.id}
										label={feature.name}
										size="small"
										variant="filled"
										onDelete={() => handleFeatureRemove(feature)}
										deleteIcon={
											<IconButton
												aria-label={`Remove ${feature.name}`}
												size="small"
											>
												<Close fontSize="small" />
											</IconButton>
										}
									/>
								))}
							</Stack>
						)}

						<Typography variant="body2" sx={{ mt: 1 }}>
							{featureAggregate} {workItemsTerm}
						</Typography>

						{showZeroHint && (
							<Typography variant="caption" color="error" sx={{ mt: 0.5 }}>
								{`Forecasting requires at least 1 remaining ${workItemsTerm.toLowerCase()}.`}
							</Typography>
						)}
					</>
				)}

				{manualForecastResult?.whenForecasts &&
					manualForecastResult.whenForecasts.length > 0 && (
						<Grid container sx={{ mt: 2 }}>
							<Grid size={{ xs: 12 }}>
								<ForecastInfoList
									title={`When will ${manualForecastResult.remainingItems} ${workItemsTerm} be done?`}
									forecasts={manualForecastResult.whenForecasts}
								/>
							</Grid>
						</Grid>
					)}
			</Grid>
			<Grid size={{ xs: 12, md: 6 }}>
				<Typography variant="subtitle2" gutterBottom>
					What is your target completion date?
				</Typography>
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
						slotProps={{
							field: { clearable: true },
						}}
					/>
				</LocalizationProvider>
				<Stack
					direction="row"
					spacing={1}
					sx={{ mt: 1, flexWrap: "wrap", gap: 0.5 }}
				>
					<Chip
						label="End of week"
						size="small"
						onClick={() => onTargetDateChange(getEndOfWeek())}
						variant="outlined"
					/>
					<Chip
						label="End of month"
						size="small"
						onClick={() => onTargetDateChange(getEndOfMonth())}
						variant="outlined"
					/>
					<Chip
						label="+1 week"
						size="small"
						onClick={() =>
							onTargetDateChange((targetDate ?? dayjs()).add(1, "week"))
						}
						variant="outlined"
					/>
					<Chip
						label="+2 weeks"
						size="small"
						onClick={() =>
							onTargetDateChange((targetDate ?? dayjs()).add(2, "weeks"))
						}
						variant="outlined"
					/>
				</Stack>
				{manualForecastResult?.howManyForecasts &&
					manualForecastResult.howManyForecasts.length > 0 && (
						<Grid container sx={{ mt: 2 }}>
							<Grid size={{ xs: 12 }}>
								<ForecastInfoList
									title={`How Many ${workItemsTerm} will you get done till ${manualForecastResult.targetDate.toLocaleDateString()}?`}
									forecasts={manualForecastResult.howManyForecasts}
								/>
							</Grid>
						</Grid>
					)}
			</Grid>
			{manualForecastResult &&
				manualForecastResult.likelihood > 0 &&
				manualForecastResult.whenForecasts &&
				manualForecastResult.whenForecasts.length > 0 &&
				manualForecastResult.howManyForecasts &&
				manualForecastResult.howManyForecasts.length > 0 && (
					<Grid size={{ xs: 12 }}>
						<ForecastLikelihood
							remainingItems={manualForecastResult.remainingItems}
							targetDate={manualForecastResult.targetDate}
							likelihood={manualForecastResult.likelihood}
						/>
					</Grid>
				)}
		</Grid>
	);
};

export default ManualForecaster;
