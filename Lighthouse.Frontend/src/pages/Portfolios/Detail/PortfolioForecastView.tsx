import { Grid } from "@mui/material";
import type React from "react";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import PortfolioFeatureList from "./PortfolioFeatureList";

interface PortfolioForecastViewProps {
	portfolio: IPortfolio;
}

const PortfolioForecastView: React.FC<PortfolioForecastViewProps> = ({
	portfolio,
}) => {
	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 12 }}>
				<PortfolioFeatureList portfolio={portfolio} />
			</Grid>
		</Grid>
	);
};

export default PortfolioForecastView;
