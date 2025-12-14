import { Grid } from "@mui/material";
import type React from "react";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import InvolvedTeamsList from "./InvolvedTeamsList";
import PortfolioFeatureList from "./PortfolioFeatureList";

interface PortfolioForecastViewProps {
	portfolio: IPortfolio;
	involvedTeams: ITeamSettings[];
	onTeamSettingsChange: (updatedTeamSettings: ITeamSettings) => Promise<void>;
}

const PortfolioForecastView: React.FC<PortfolioForecastViewProps> = ({
	portfolio,
	involvedTeams,
	onTeamSettingsChange,
}) => {
	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 12 }}>
				<InvolvedTeamsList
					teams={involvedTeams}
					onTeamUpdated={onTeamSettingsChange}
				/>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<PortfolioFeatureList portfolio={portfolio} />
			</Grid>
		</Grid>
	);
};

export default PortfolioForecastView;
