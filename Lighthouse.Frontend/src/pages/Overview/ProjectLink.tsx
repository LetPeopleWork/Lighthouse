import AccountTreeIcon from "@mui/icons-material/AccountTree";
import type React from "react";
import type { Project } from "../../models/Project/Project";
import StyleCardNavLink from "./StyleCardNavLink";

interface ProjectLinkProps {
	project: Project;
}

const ProjectLink: React.FC<ProjectLinkProps> = ({ project }) => {
	return (
		<StyleCardNavLink
			text={project.name}
			link={`/projects/${project.id}`}
			icon={AccountTreeIcon}
			isTitle={true}
		/>
	);
};

export default ProjectLink;
