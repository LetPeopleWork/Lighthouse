import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { Project } from "../../models/Project/Project";
import ProjectCard from "./ProjectCard";

interface ProjectOverviewProps {
	projects: Project[];
	filterText: string;
}

const ProjectOverview: React.FC<ProjectOverviewProps> = ({
	projects,
	filterText,
}) => {
	const filteredProjects = projects.filter(
		(project) =>
			isMatchingFilterText(project.name) ||
			project.involvedTeams.some((t) => isMatchingFilterText(t.name)),
	);

	function isMatchingFilterText(textToCheck: string) {
		if (!filterText) {
			return true;
		}

		return textToCheck.toLowerCase().includes(filterText.toLowerCase());
	}

	return (
		<Grid container spacing={2}>
			{projects.length === 0 ? (
				<Grid size={{ xs: 12 }} data-testid="empty-projects-message">
					<Typography variant="h6" align="center">
						No Projects Defined.{" "}
						<a
							href="https://docs.lighthouse.letpeople.work"
							target="_blank"
							rel="noopener noreferrer"
						>
							Check the documentation
						</a>{" "}
						for more information.
					</Typography>
				</Grid>
			) : null}
			{filteredProjects.length === 0 ? (
				<Grid size={{ xs: 12 }} data-testid="no-projects-message">
					<Typography variant="h6" align="center">
						No projects found matching the filter.
					</Typography>
				</Grid>
			) : (
				filteredProjects.map((project) => (
					<Grid size={{ xs: 12, sm: 6, md: 4, lg: 4 }} key={project.id}>
						<div
							style={{ width: "100%", maxWidth: 1920 }}
							data-testid={`project-card-${project.id}`}
						>
							<ProjectCard key={project.id} project={project} />
						</div>
					</Grid>
				))
			)}
		</Grid>
	);
};

export default ProjectOverview;
