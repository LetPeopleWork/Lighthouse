import React, { useContext, useEffect, useState } from 'react';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import DataOverviewTable from '../../../components/Common/DataOverviewTable/DataOverviewTable';
import DeleteConfirmationDialog from '../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog';
import { Project } from '../../../models/Project/Project';
import { IFeatureOwner } from '../../../models/IFeatureOwner';
import ProjectOverviewTutorial from '../../../components/App/LetPeopleWork/Tutorial/Tutorials/ProjectOverviewTutorial';
import TutorialButton from '../../../components/App/LetPeopleWork/Tutorial/TutorialButton';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';

const ProjectsOverview: React.FC = () => {
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [hasError, setHasError] = useState<boolean>(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);

  const { projectService } = useContext(ApiServiceContext);

  const fetchData = async () => {
    try {
      setIsLoading(true);
      const projectData = await projectService.getProjects();
      setProjects(projectData);
      setIsLoading(false);
    } catch (error) {
      console.error('Error fetching team data:', error);
      setHasError(true);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleDelete = (project: IFeatureOwner) => {
    setSelectedProject(project as Project);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirmation = async (confirmed: boolean) => {
    if (confirmed && selectedProject) {
      try {
        setIsLoading(true);

        await projectService.deleteProject(selectedProject.id);
        await fetchData();

      } catch (error) {
        console.error('Error deleting project:', error);
        setHasError(true);
      }
    }

    setDeleteDialogOpen(false);
    setSelectedProject(null);
  };

  return (
    <LoadingAnimation hasError={hasError} isLoading={isLoading}>
      {projects.length === 0 ? (
        <ProjectOverviewTutorial />
      ) : (
        <DataOverviewTable
          data={projects}
          api="projects"
          onDelete={handleDelete}
        />)}
      {selectedProject && (
        <DeleteConfirmationDialog
          open={deleteDialogOpen}
          itemName={selectedProject.name}
          onClose={handleDeleteConfirmation}
        />
      )}
      <TutorialButton
        tutorialComponent={<ProjectOverviewTutorial />}
      />
    </LoadingAnimation>
  );
};

export default ProjectsOverview;
