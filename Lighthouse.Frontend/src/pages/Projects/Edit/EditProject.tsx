import type React from "react";
import { useContext } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ModifyProjectSettings from "../../../components/Common/ProjectSettings/ModifyProjectSettings";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const EditProject: React.FC = () => {
	const { id } = useParams<{ id?: string }>();
	const isNewProject = id === undefined;

	const navigate = useNavigate();
	const {
		settingsService,
		projectService,
		workTrackingSystemService,
		teamService,
	} = useContext(ApiServiceContext);

	const pageTitle = isNewProject ? "Create Project" : "Update Project";

	const getProjectSettings = async () => {
		const urlParams = new URLSearchParams(window.location.search);
		const cloneFromId = urlParams.get("cloneFrom");

		if (isNewProject && cloneFromId) {
			const cloneId = Number.parseInt(cloneFromId, 10);
			if (!Number.isNaN(cloneId)) {
				const sourceSettings = await projectService.getProjectSettings(cloneId);
				return {
					...sourceSettings,
					id: 0,
					name: `Copy of ${sourceSettings.name}`,
				};
			}
		}

		if (!isNewProject && id) {
			return await projectService.getProjectSettings(Number.parseInt(id, 10));
		}
		return await settingsService.getDefaultProjectSettings();
	};

	const getWorkTrackingSystems = async () => {
		return await workTrackingSystemService.getConfiguredWorkTrackingSystems();
	};

	const getAllTeams = async () => {
		return await teamService.getTeams();
	};

	const validateProjectSettings = async (
		updatedProjectSettings: IProjectSettings,
	) => {
		return await projectService.validateProjectSettings(updatedProjectSettings);
	};

	const saveProjectSettings = async (updatedSettings: IProjectSettings) => {
		let savedSettings: IProjectSettings;
		if (isNewProject) {
			savedSettings = await projectService.createProject(updatedSettings);
			await projectService.refreshFeaturesForProject(savedSettings.id);
		} else {
			savedSettings = await projectService.updateProject(updatedSettings);
		}

		navigate(`/projects/${savedSettings.id}`);
	};

	return (
		<SnackbarErrorHandler>
			<ModifyProjectSettings
				title={pageTitle}
				getProjectSettings={getProjectSettings}
				getWorkTrackingSystems={getWorkTrackingSystems}
				getAllTeams={getAllTeams}
				validateProjectSettings={validateProjectSettings}
				saveProjectSettings={saveProjectSettings}
			/>
		</SnackbarErrorHandler>
	);
};

export default EditProject;
