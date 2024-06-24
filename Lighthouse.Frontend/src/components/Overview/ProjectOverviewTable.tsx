import React from 'react';
import ProjectOverviewRow from "./ProjectOverviewRow";
import { Project } from '../../models/Project';

interface ProjectOverviewTableProps {
  projects: Project[];
  filterText: string;
}

const ProjectOverviewTable: React.FC<ProjectOverviewTableProps> = ({ projects, filterText }) => {
  const projectRows: JSX.Element[] = [];

  function isMatchingFilterText(textToCheck: string) {
    if (filterText === null || filterText === undefined) {
      return true;
    }

    if (textToCheck.toLowerCase().includes(filterText.toLowerCase())) {
      return true;
    }

    return false;
  }

  projects.forEach((project) => {
    if (isMatchingFilterText(project.name) || project.involvedTeams.some(t => isMatchingFilterText(t.name))) {
      projectRows.push(
        <ProjectOverviewRow key={project.id} project={project} />
      )
    }
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