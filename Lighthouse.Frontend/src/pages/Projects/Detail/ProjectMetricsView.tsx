import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { IProject } from "../../../models/Project/Project";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";

interface ProjectMetricsViewProps {
	project: IProject;
}

const ProjectMetricsView: React.FC<ProjectMetricsViewProps> = ({ project }) => {
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const { projectMetricsService, projectService } =
		useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

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
			title={featuresTerm}
			defaultDateRange={90}
			doingStates={doingStates}
		/>
	);
};

export default ProjectMetricsView;
