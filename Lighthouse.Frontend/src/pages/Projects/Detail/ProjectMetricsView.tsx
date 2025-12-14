import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { IPortfolio } from "../../../models/Project/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";

interface ProjectMetricsViewProps {
	project: IPortfolio;
}

const ProjectMetricsView: React.FC<ProjectMetricsViewProps> = ({ project }) => {
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const { projectMetricsService, portfolioService: projectService } =
		useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	useEffect(() => {
		const fetchProjectSettings = async () => {
			try {
				const settings = await projectService.getPortfolioSettings(project.id);
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
