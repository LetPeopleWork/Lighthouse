import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import DataOverviewTable from "../../../components/Common/DataOverviewTable/DataOverviewTable";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { Project } from "../../../models/Project/Project";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const ProjectsOverview: React.FC = () => {
	const [projects, setProjects] = useState<Project[]>([]);
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const [hasError, setHasError] = useState<boolean>(false);
	const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
	const [selectedProject, setSelectedProject] = useState<Project | null>(null);

	const { projectService } = useContext(ApiServiceContext);

	const fetchData = useCallback(async () => {
		try {
			setIsLoading(true);
			const projectData = await projectService.getProjects();
			setProjects(projectData);
			setIsLoading(false);
		} catch (error) {
			console.error("Error fetching team data:", error);
			setHasError(true);
		}
	}, [projectService]);

	useEffect(() => {
		fetchData();
	}, [fetchData]);

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
				console.error("Error deleting project:", error);
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
