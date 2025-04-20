import { Grid } from "@mui/material";
import type React from "react";
import MilestonesComponent from "../../../components/Common/Milestones/MilestonesComponent";
import type { IMilestone } from "../../../models/Project/Milestone";
import type { IProject } from "../../../models/Project/Project";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import InvolvedTeamsList from "./InvolvedTeamsList";
import ProjectFeatureList from "./ProjectFeatureList";

interface ProjectForecastViewProps {
	project: IProject;
	projectSettings: IProjectSettings | null;
	involvedTeams: ITeamSettings[];
	onMilestonesChanged: (
		updatedProjectSettings: IProjectSettings,
	) => Promise<void>;
	onTeamSettingsChange: (updatedTeamSettings: ITeamSettings) => Promise<void>;
}

const ProjectForecastView: React.FC<ProjectForecastViewProps> = ({
	project,
	projectSettings,
	involvedTeams,
	onMilestonesChanged,
	onTeamSettingsChange,
}) => {
	const handleAddMilestone = async (milestone: IMilestone) => {
		if (!projectSettings) {
			return;
		}

		const updatedProjectSettings: IProjectSettings = {
			...projectSettings,
			milestones: [...(projectSettings.milestones || []), milestone],
		};

		await onMilestonesChanged(updatedProjectSettings);
	};

	const handleRemoveMilestone = async (name: string) => {
		if (!projectSettings) {
			return;
		}

		const updatedProjectSettings: IProjectSettings = {
			...projectSettings,
			milestones: (projectSettings.milestones || []).filter(
				(milestone) => milestone.name !== name,
			),
		};

		await onMilestonesChanged(updatedProjectSettings);
	};

	const handleUpdateMilestone = async (
		name: string,
		updatedMilestone: Partial<IMilestone>,
	) => {
		if (!projectSettings) {
			return;
		}

		const updatedProjectSettings: IProjectSettings = {
			...projectSettings,
			milestones: (projectSettings?.milestones || []).map((milestone) =>
				milestone.name === name
					? { ...milestone, ...updatedMilestone }
					: milestone,
			),
		};

		await onMilestonesChanged(updatedProjectSettings);
	};

	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 12 }}>
				<MilestonesComponent
					milestones={projectSettings?.milestones || []}
					initiallyExpanded={false}
					onAddMilestone={handleAddMilestone}
					onRemoveMilestone={handleRemoveMilestone}
					onUpdateMilestone={handleUpdateMilestone}
				/>
			</Grid>
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
