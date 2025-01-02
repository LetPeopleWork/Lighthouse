import { Container } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import TutorialButton from "../../components/App/LetPeopleWork/Tutorial/TutorialButton";
import LighthouseAppOverviewTutorial from "../../components/App/LetPeopleWork/Tutorial/Tutorials/LighthouseAppOverviewTutorial";
import FilterBar from "../../components/Common/FilterBar/FilterBar";
import LoadingAnimation from "../../components/Common/LoadingAnimation/LoadingAnimation";
import type { Project } from "../../models/Project/Project";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import ProjectOverview from "./ProjectOverview";

const OverviewDashboard: React.FC = () => {
	const [projects, setProjects] = useState<Project[]>([]);
	const [isLoading, setIsLoading] = useState(true);
	const [hasError, setHasError] = useState(false);
	const [filterText, setFilterText] = useState("");

	const { projectService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchData = async () => {
			try {
				const projectData = await projectService.getProjects();
				setProjects(projectData);
				setIsLoading(false);
			} catch (error) {
				console.error("Error fetching project overview data:", error);
				setHasError(true);
			}
		};

		fetchData();
	}, [projectService]);

	return (
		<LoadingAnimation isLoading={isLoading} hasError={hasError}>
			<Container maxWidth={false}>
				<FilterBar filterText={filterText} onFilterTextChange={setFilterText} />

				{projects.length === 0 ? (
					<LighthouseAppOverviewTutorial />
				) : (
					<ProjectOverview projects={projects} filterText={filterText} />
				)}
			</Container>

			<TutorialButton tutorialComponent={<LighthouseAppOverviewTutorial />} />
		</LoadingAnimation>
	);
};

export default OverviewDashboard;
