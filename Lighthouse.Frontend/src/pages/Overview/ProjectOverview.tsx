import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Fade from "@mui/material/Fade";
import Grid from "@mui/material/Grid";
import { useTheme } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
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
	const theme = useTheme();

	const filteredProjects = projects.filter(
		(project) =>
			isMatchingFilterText(project.name) ||
			project.tags?.some((tag) => isMatchingFilterText(tag)) ||
			project.involvedTeams.some((t) => isMatchingFilterText(t.name)),
	);

	const sortedProjects = [...filteredProjects].sort((a, b) =>
		a.name.localeCompare(b.name),
	);

	function isMatchingFilterText(textToCheck: string) {
		if (!filterText) {
			return true;
		}

		return textToCheck.toLowerCase().includes(filterText.toLowerCase());
	}

	return (
		<Box sx={{ py: 2 }}>
			{projects.length === 0 ? (
				<Fade in={true} timeout={800}>
					<Alert
						severity="info"
						variant="outlined"
						sx={{
							mb: 3,
							borderRadius: 2,
							boxShadow: theme.shadows[1],
						}}
						data-testid="empty-projects-message"
					>
						<Typography variant="body1">
							No Projects Defined.{" "}
							<a
								href="https://docs.lighthouse.letpeople.work"
								target="_blank"
								rel="noopener noreferrer"
								style={{
									color: theme.palette.primary.main,
									textDecoration: "none",
									fontWeight: 500,
								}}
								onMouseOver={(e) => {
									e.currentTarget.style.textDecoration = "underline";
								}}
								onMouseOut={(e) => {
									e.currentTarget.style.textDecoration = "none";
								}}
								onFocus={(e) => {
									e.currentTarget.style.textDecoration = "underline";
								}}
								onBlur={(e) => {
									e.currentTarget.style.textDecoration = "none";
								}}
							>
								Check the documentation
							</a>{" "}
							for more information.
						</Typography>
					</Alert>
				</Fade>
			) : filterText && filteredProjects.length === 0 ? (
				<Fade in={true} timeout={500}>
					<Alert
						severity="warning"
						variant="outlined"
						sx={{
							mb: 3,
							borderRadius: 2,
							boxShadow: theme.shadows[1],
						}}
						data-testid="no-projects-message"
					>
						No projects found matching the filter.
					</Alert>
				</Fade>
			) : (
				<Grid container spacing={3}>
					{sortedProjects.map((project) => (
						<Grid
							size={{ xs: 12, sm: 6, md: 4, lg: 4, xl: 3 }}
							key={project.id}
						>
							<Box
								sx={{
									height: "100%",
									opacity: 0,
									animation: "fadeIn 0.5s forwards",
									animationDelay: `${(sortedProjects.indexOf(project) % 10) * 0.1}s`,
									"@keyframes fadeIn": {
										"0%": { opacity: 0, transform: "translateY(10px)" },
										"100%": { opacity: 1, transform: "translateY(0)" },
									},
								}}
								data-testid={`project-card-${project.id}`}
							>
								<ProjectCard project={project} />
							</Box>
						</Grid>
					))}
				</Grid>
			)}
		</Box>
	);
};

export default ProjectOverview;
