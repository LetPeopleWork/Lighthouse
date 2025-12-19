import { Grid } from "@mui/material";
import type React from "react";
import type { Team } from "../../../models/Team/Team";
import TeamFeatureList from "./TeamFeatureList";

interface TeamFeaturesViewProps {
	team: Team;
}

const TeamFeaturesView: React.FC<TeamFeaturesViewProps> = ({ team }) => {
	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 12 }}>
				<TeamFeatureList team={team} />
			</Grid>
		</Grid>
	);
};

export default TeamFeaturesView;
