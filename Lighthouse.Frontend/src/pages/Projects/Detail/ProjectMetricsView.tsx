import type React from "react";
import { useContext } from "react";
import type { IProject } from "../../../models/Project/Project";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";

interface ProjectMetricsViewProps {
	project: IProject;
}

const ProjectMetricsView: React.FC<ProjectMetricsViewProps> = ({ project }) => {
	const { projectMetricsService } = useContext(ApiServiceContext);

	return (
		<BaseMetricsView
			entity={project}
			metricsService={projectMetricsService}
			title="Features"
			defaultDateRange={90}
		/>
	);
};

export default ProjectMetricsView;
