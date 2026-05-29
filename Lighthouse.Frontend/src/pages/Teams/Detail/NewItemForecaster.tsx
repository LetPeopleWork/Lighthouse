import Grid from "@mui/material/Grid";
import Typography from "@mui/material/Typography";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import dayjs from "dayjs";
import type React from "react";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import ItemListManager from "../../../components/Common/ItemListManager/ItemListManager";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface NewItemForecasterProps {
	newItemForecastResult: ManualForecast | null;
	startDate: dayjs.Dayjs | null;
	endDate: dayjs.Dayjs | null;
	targetDate: dayjs.Dayjs | null;
	selectedWorkItemTypes: string[];
	onStartDateChange: (value: dayjs.Dayjs | null) => void;
	onEndDateChange: (value: dayjs.Dayjs | null) => void;
	onTargetDateChange: (value: dayjs.Dayjs | null) => void;
	onWorkItemTypesChange: (types: string[]) => void;
	onInputChange: (complete: boolean) => void;
	workItemTypes: string[];
}

const NewItemForecaster: React.FC<NewItemForecasterProps> = ({
	newItemForecastResult,
	startDate,
	endDate,
	targetDate,
	selectedWorkItemTypes,
	onStartDateChange,
	onEndDateChange,
	onTargetDateChange,
	onWorkItemTypesChange,
	onInputChange,
	workItemTypes,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const reportInputChange = (
		nextStartDate: dayjs.Dayjs | null,
		nextEndDate: dayjs.Dayjs | null,
		nextTargetDate: dayjs.Dayjs | null,
		nextWorkItemTypes: string[],
	) => {
		onInputChange(
			nextStartDate !== null &&
				nextEndDate !== null &&
				nextTargetDate !== null &&
				nextWorkItemTypes.length > 0,
		);
	};

	const handleStartDateChange = (value: dayjs.Dayjs | null) => {
		onStartDateChange(value);
		reportInputChange(value, endDate, targetDate, selectedWorkItemTypes);
	};

	const handleEndDateChange = (value: dayjs.Dayjs | null) => {
		onEndDateChange(value);
		reportInputChange(startDate, value, targetDate, selectedWorkItemTypes);
	};

	const handleTargetDateChange = (value: dayjs.Dayjs | null) => {
		onTargetDateChange(value);
		reportInputChange(startDate, endDate, value, selectedWorkItemTypes);
	};

	const handleAddWorkItemType = (type: string) => {
		if (selectedWorkItemTypes.includes(type)) {
			return;
		}
		const next = [...selectedWorkItemTypes, type];
		onWorkItemTypesChange(next);
		reportInputChange(startDate, endDate, targetDate, next);
	};

	const handleRemoveWorkItemType = (type: string) => {
		const next = selectedWorkItemTypes.filter((t) => t !== type);
		onWorkItemTypesChange(next);
		reportInputChange(startDate, endDate, targetDate, next);
	};

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

	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 12 }}>
				<Grid container spacing={3}>
					<Grid size={{ xs: 6 }}>
						<Typography variant="h6" gutterBottom>
							Historical Data
						</Typography>
						<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
							Historical data that should be used for the forecast
						</Typography>
						<Grid container spacing={2}>
							<Grid size={{ xs: 12 }}>
								<LocalizationProvider dateAdapter={AdapterDayjs}>
									<DatePicker
										label="From"
										value={startDate}
										onChange={(value) =>
											handleStartDateChange(value as dayjs.Dayjs | null)
										}
										maxDate={endDate || undefined}
										format={getLocaleDateFormat()}
										sx={{ width: "100%" }}
									/>
								</LocalizationProvider>
							</Grid>
							<Grid size={{ xs: 12 }}>
								<LocalizationProvider dateAdapter={AdapterDayjs}>
									<DatePicker
										label="To"
										value={endDate}
										onChange={(value) =>
											handleEndDateChange(value as dayjs.Dayjs | null)
										}
										minDate={startDate || undefined}
										maxDate={targetDate || undefined}
										format={getLocaleDateFormat()}
										sx={{ width: "100%" }}
									/>
								</LocalizationProvider>
							</Grid>
						</Grid>

						<Typography variant="h6" gutterBottom sx={{ mt: 3 }}>
							Target Date
						</Typography>
						<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
							How far into the future do you want to forecast
						</Typography>
						<LocalizationProvider dateAdapter={AdapterDayjs}>
							<DatePicker
								label="Target Date"
								value={targetDate}
								onChange={(value) =>
									handleTargetDateChange(value as dayjs.Dayjs | null)
								}
								minDate={endDate || dayjs()}
								format={getLocaleDateFormat()}
								sx={{ width: "100%" }}
							/>
						</LocalizationProvider>
					</Grid>

					<Grid size={{ xs: 6 }}>
						<Typography variant="h6" gutterBottom>
							Work Item Types
						</Typography>
						<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
							Select the work item types to include in the forecast. At least
							one type must be selected.
						</Typography>
						<ItemListManager
							title="Work Item Type"
							items={selectedWorkItemTypes}
							onAddItem={handleAddWorkItemType}
							onRemoveItem={handleRemoveWorkItemType}
							suggestions={workItemTypes}
							isLoading={false}
						/>

						<Grid container spacing={2} sx={{ mt: 1 }}>
							<Grid size={{ xs: 12 }}>
								{newItemForecastResult && selectedWorkItemTypes.length > 0 && (
									<ForecastInfoList
										title={`How many ${selectedWorkItemTypes.join(", ")} ${workItemsTerm} will you add until ${targetDate?.format("YYYY-MM-DD")}?`}
										forecasts={newItemForecastResult.howManyForecasts}
									/>
								)}
							</Grid>
						</Grid>
					</Grid>
				</Grid>
			</Grid>
		</Grid>
	);
};

export default NewItemForecaster;
