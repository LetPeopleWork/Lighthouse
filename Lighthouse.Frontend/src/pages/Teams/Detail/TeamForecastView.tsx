import { Grid } from "@mui/material";
import dayjs from "dayjs";
import type React from "react";
import { useContext, useState } from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import ManualForecaster from "./ManualForecaster";
import TeamFeatureList from "./TeamFeatureList";

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

	const { forecastService } = useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

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
			console.error("Error running manual forecast:", error);
		}
	};

	return (
		<Grid container spacing={3}>
			<InputGroup title={featuresTerm}>
				<TeamFeatureList team={team} />
			</InputGroup>
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
		</Grid>
	);
};

export default TeamForecastView;
