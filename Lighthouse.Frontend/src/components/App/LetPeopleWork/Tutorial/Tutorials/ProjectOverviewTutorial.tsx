import { Container, Typography } from "@mui/material";
import type React from "react";
import { Link, useNavigate } from "react-router-dom";

import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";

import DeleteProject from "../../../../../assets/Tutorial/Project/DeleteProject.gif";
import ProjectForecast from "../../../../../assets/Tutorial/Project/ProjectForecast.png";
import ProjectOverview from "../../../../../assets/Tutorial/Project/ProjectOverview.png";

const ProjectsOverview: React.FC = () => (
	<TutorialStep
		title="About Projects"
		description="Projects are an essential building block for using Lighthouse. You can use Projects to keep track of when a set of Features that one or more teams are working on will be done."
		imageSrc={ProjectForecast}
	>
		<Container maxWidth={false}>
			<Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
				{`A feature that sets Lighthouse apart is that you can forecast Projects where one or more teams contribute to a set of Features.
Whether you are in a scaled setup where many teams contribute to a shared goal, or simply want to keep track on potential target dates for multiple features, you want to use a Project.

Continue reading to learn how to work with Projects in Lighthouse, or directly `}
				<Link to="/projects/new">create a new Project</Link>
				{"."}
			</Typography>
		</Container>
	</TutorialStep>
);

const ProjectsList: React.FC = () => (
	<TutorialStep
		title="Projects Overview"
		description="See all your configured Projects at a glance."
		imageSrc={ProjectOverview}
	>
		<Container maxWidth={false}>
			<Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
				{
					"Once you have at least one project defined, it will appear in a grid view on the "
				}
				<Link to="/projects">Teams</Link>
				{` page.

From this overview, you can add new projects, delete or modify existing ones, or view project details.

You can also search for specific projects and see the number of remaining work items and features for each project.`}
			</Typography>
		</Container>
	</TutorialStep>
);

const DeleteProjectStep: React.FC = () => (
	<TutorialStep
		title="Delete Project"
		description="Delete a project via the Delete button in the overview."
		imageSrc={DeleteProject}
	>
		<Container maxWidth={false}>
			<Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
				{
					"Please be aware that deleting a project is permanent and cannot be undone. If you need the same project later, you will have to re-add it. Deleting a project removes all information about that project from Lighthouse."
				}
			</Typography>
		</Container>
	</TutorialStep>
);

const ProjectDetailStep: React.FC = () => (
	<TutorialStep
		title="Project Details"
		description="Dive into the details of a specific Project."
		imageSrc={ProjectForecast}
	>
		<Container maxWidth={false}>
			<Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
				{`To see more details for a project, you can click on the project name or the info icon in the overview.

This will bring you to the specific project page, where you can see which features are part of this project, which teams are involved, and when each feature is forecasted to be completed.

Note: You can directly link to a project detail page by storing the specific URL.`}
			</Typography>
		</Container>
	</TutorialStep>
);

const steps = [
	{ title: "About Projects", component: ProjectsOverview },
	{ title: "Projects Overview", component: ProjectsList },
	{ title: "Delete Project", component: DeleteProjectStep },
	{ title: "Project Details", component: ProjectDetailStep },
];

const ProjectOverviewTutorial: React.FC = () => {
	const navigate = useNavigate();

	const createNewProject = () => {
		navigate("/projects/new");
	};

	return (
		<LighthouseTutorial
			steps={steps}
			tutorialTitle="Projects"
			finalButtonText="Create a new Project"
			onFinalButtonClick={createNewProject}
		/>
	);
};

export default ProjectOverviewTutorial;
