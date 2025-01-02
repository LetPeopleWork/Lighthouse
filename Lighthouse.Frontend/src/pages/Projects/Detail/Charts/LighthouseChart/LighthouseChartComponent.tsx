import { Alert } from "@mui/material";
import Grid from "@mui/material/Grid2";
import dayjs, { type Dayjs } from "dayjs";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import DatePickerComponent from "../../../../../components/Common/DatePicker/DatePickerComponent";
import LoadingAnimation from "../../../../../components/Common/LoadingAnimation/LoadingAnimation";
import type { ILighthouseChartData } from "../../../../../models/Charts/LighthouseChartData";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import SampleFrequencySelector from "../../SampleFrequencySelector";
import LighthouseChart from "./LighthouseChart";

interface LighthouseChartComponentProps {
	projectId: number;
}

const LighthouseChartComponent: React.FC<LighthouseChartComponentProps> = ({
	projectId,
}) => {
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const [hasError, setHasError] = useState<boolean>(false);
	const [chartData, setChartData] = useState<ILighthouseChartData>();
	const [startDate, setStartDate] = useState<Dayjs>(
		dayjs().subtract(30, "day"),
	);
	const [sampleRate, setSampleRate] = useState<number>(1);

	const { chartService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchLighthouseData = async () => {
			try {
				setIsLoading(true);
				const lighthouseChartData = await chartService.getLighthouseChartData(
					projectId,
					startDate.toDate(),
					sampleRate,
				);

				if (lighthouseChartData) {
					setChartData(lighthouseChartData);
				} else {
					setHasError(true);
				}

				setIsLoading(false);
			} catch (error) {
				console.error("Error fetching project data:", error);
				setHasError(true);
			}
		};

		fetchLighthouseData();
	}, [startDate, sampleRate, chartService, projectId]);

	const onStartDateChanged = (newStartDate: Dayjs | null) => {
		if (newStartDate) {
			setStartDate(newStartDate);
		}
	};

	return (
		<LoadingAnimation hasError={hasError} isLoading={isLoading}>
			{chartData?.features && chartData.features.length > 0 ? (
				<Grid container spacing={3}>
					<Grid size={{ xs: 6 }}>
						<DatePickerComponent
							label="Burndown Start Date"
							value={startDate}
							onChange={onStartDateChanged}
						/>
					</Grid>
					<SampleFrequencySelector
						sampleEveryNthDay={sampleRate}
						onSampleEveryNthDayChange={setSampleRate}
					/>
					<Grid size={{ xs: 12 }}>
						<LighthouseChart data={chartData} />
					</Grid>
				</Grid>
			) : (
				<Alert severity="warning">
					Can't display Burndown as no Feature information is available for this
					Project.
				</Alert>
			)}
		</LoadingAnimation>
	);
};

export default LighthouseChartComponent;
