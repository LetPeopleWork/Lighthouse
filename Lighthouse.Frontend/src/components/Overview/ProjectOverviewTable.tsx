import React from 'react';
import ProjectOverviewRow from "./ProjectOverviewRow";
import { Project } from '../../models/Project';

interface ProjectOverviewTableProps {
    projects: Project[];
}

const ProjectOverviewTable: React.FC<ProjectOverviewTableProps> = ({ projects }) => {
    let projectRows: JSX.Element[] = [];

    projects.forEach((project) => {
        projectRows.push(
            <ProjectOverviewRow key={project.id} project={project} />
        )
    })

    return (
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Remaining Work</th>
              <th>Involved Teams</th>
              <th>50%</th>
              <th>70%</th>
              <th>85%</th>
              <th>95%</th>
              <th>Last Updated on:</th>
            </tr>
          </thead>
          <tbody>{projectRows}</tbody>
        </table>
      );
}

export default ProjectOverviewTable