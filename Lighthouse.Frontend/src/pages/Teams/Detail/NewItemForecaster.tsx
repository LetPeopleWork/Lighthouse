import Grid from "@mui/material/Grid";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import dayjs from "dayjs";
import type React from "react";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface NewItemForecasterProps {
	targetDate: dayjs.Dayjs | null;
	newItemForecastResult: ManualForecast | null;
	onTargetDateChange: (date: dayjs.Dayjs | null) => void;
	onRunNewItemForecast: () => Promise<void>;
}

const NewItemForecaster: React.FC<NewItemForecasterProps> = ({
	targetDate,
	newItemForecastResult,
	onTargetDateChange,
	onRunNewItemForecast,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

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
				<Grid container spacing={2}>
					<Grid size={{ xs: 6 }}>
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
							/>
						</LocalizationProvider>
					</Grid>
					<Grid size={{ xs: 3 }}>
						<ActionButton
							onClickHandler={onRunNewItemForecast}
							buttonText="Forecast"
						/>
					</Grid>
				</Grid>
			</Grid>
			<Grid size={{ xs: 12 }}>
				{newItemForecastResult && (
					<Grid container spacing={2}>
						<Grid size={{ xs: 12 }}>
							<ForecastInfoList
								title={`How many ${workItemsTerm} will you add by ${targetDate?.format("YYYY-MM-DD")}?`}
								forecasts={newItemForecastResult.howManyForecasts}
							/>
						</Grid>
					</Grid>
				)}
			</Grid>
		</Grid>
	);
};

export default NewItemForecaster;
