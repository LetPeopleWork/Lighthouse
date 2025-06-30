import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { IProject } from "../../../models/Project/Project";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";

interface ProjectMetricsViewProps {
	project: IProject;
}

const ProjectMetricsView: React.FC<ProjectMetricsViewProps> = ({ project }) => {
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const { projectMetricsService, projectService } =
		useContext(ApiServiceContext);

	useEffect(() => {
		const fetchProjectSettings = async () => {
			try {
				const settings = await projectService.getProjectSettings(project.id);
				setDoingStates(settings.doingStates);
			} catch (err) {
				console.error("Error fetching project settings:", err);
			}
		};

		fetchProjectSettings();
	}, [project.id, projectService]);

	return (
		<BaseMetricsView
			entity={project}
			metricsService={projectMetricsService}
			title="Features"
			defaultDateRange={90}
			doingStates={doingStates}
		/>
	);
};

export default ProjectMetricsView;
