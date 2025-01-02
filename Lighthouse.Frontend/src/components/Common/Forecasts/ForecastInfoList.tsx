import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid2";
import { styled } from "@mui/system";
import type React from "react";
import type { IForecast } from "../../../models/Forecasts/IForecast";
import ForecastInfo from "./ForecastInfo";

const ForecastsHeader = styled(Typography)({
	marginBottom: "inherit",
	fontWeight: "bold",
});

interface ForecastInfoListProps {
	title: string;
	forecasts: IForecast[];
}

const ForecastInfoList: React.FC<ForecastInfoListProps> = ({
	title,
	forecasts,
}) => {
	return (
		<Grid size={{ xs: 12 }}>
			<ForecastsHeader variant="body1">{title}</ForecastsHeader>
			{forecasts
				.slice()
				.reverse()
				.map((forecast) => (
					<ForecastInfo key={forecast.probability} forecast={forecast} />
				))}
		</Grid>
	);
};

export default ForecastInfoList;
