import { Grid } from "@mui/material";
import type React from "react";
import type { IProject } from "../../../models/Project/Project";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import InvolvedTeamsList from "./InvolvedTeamsList";
import ProjectFeatureList from "./ProjectFeatureList";

interface ProjectForecastViewProps {
	project: IProject;
	involvedTeams: ITeamSettings[];
	onTeamSettingsChange: (updatedTeamSettings: ITeamSettings) => Promise<void>;
}

const ProjectForecastView: React.FC<ProjectForecastViewProps> = ({
	project,
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
				<ProjectFeatureList project={project} />
			</Grid>
		</Grid>
	);
};

export default ProjectForecastView;
