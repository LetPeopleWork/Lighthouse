import React, { useEffect, useState } from 'react';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import DataOverviewTable from '../../../components/Common/DataOverviewTable/DataOverviewTable';
import DeleteConfirmationDialog from '../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog';
import { Project } from '../../../models/Project/Project';
import { IApiService } from '../../../services/Api/IApiService';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { IFeatureOwner } from '../../../models/IFeatureOwner';

const ProjectsOverview: React.FC = () => {
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [hasError, setHasError] = useState<boolean>(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);

  const fetchData = async () => {
    try {
      setIsLoading(true);
      const apiService: IApiService = ApiServiceProvider.getApiService();
      const projectData = await apiService.getProjects();
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
        const apiService: IApiService = ApiServiceProvider.getApiService();
        setIsLoading(true);

        await apiService.deleteProject(selectedProject.id);
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
      <DataOverviewTable
        data={projects}
        api="projects"
        onDelete={handleDelete}
      />
      {selectedProject && (
        <DeleteConfirmationDialog
          open={deleteDialogOpen}
          itemName={selectedProject.name}
          onClose={handleDeleteConfirmation}
        />
      )}
    </LoadingAnimation>
  );
};

export default ProjectsOverview;
