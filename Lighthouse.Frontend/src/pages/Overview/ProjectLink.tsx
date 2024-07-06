import React from "react";
import { Project } from "../../models/Project";
import AccountTreeIcon from '@mui/icons-material/AccountTree';
import StyleCardNavLink from "./StyleCardNavLink";

interface ProjectLinkProps {
    project: Project
}

const ProjectLink: React.FC<ProjectLinkProps> = ({ project }) => {

    return (
        <StyleCardNavLink text={project.name} link={`/projects/${project.id}`} icon={AccountTreeIcon} isTitle={true} />
    );
};

export default ProjectLink;
