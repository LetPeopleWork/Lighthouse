import { Button, Container, Stack, Tab, Tabs } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import DetailHeader from "../../../components/Common/DetailHeader/DetailHeader";
import FeatureOwnerHeader from "../../../components/Common/FeatureOwnerHeader/FeatureOwnerHeader";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import ServiceLevelExpectation from "../../../components/Common/ServiceLevelExpectation/ServiceLevelExpectation";
import SystemWipLimitDisplay from "../../../components/Common/SystemWipLimitDisplay/SystemWipLimitDisplay";
import type { IProject, Project } from "../../../models/Project/Project";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IUpdateStatus } from "../../../services/UpdateSubscriptionService";
import ProjectForecastView from "./ProjectForecastView";
import ProjectMetricsView from "./ProjectMetricsView";

const ProjectDetail: React.FC = () => {
	const navigate = useNavigate();
	const { id } = useParams<{ id: string }>();
	const projectId = Number(id);

	let subscribedToUpdates = false;

	const [project, setProject] = useState<Project>();

	const [isLoading, setIsLoading] = useState<boolean>(true);
	const [isProjectUpdating, setIsProjectUpdating] = useState<boolean>(false);
	const [activeView, setActiveView] = useState<"forecast" | "metrics">(
		"forecast",
	);

	const [projectSettings, setProjectSettings] =
		useState<IProjectSettings | null>(null);
	const [involvedTeams, setInvolvedTeams] = useState<ITeamSettings[]>([]);

	const { projectService, teamService, updateSubscriptionService } =
		useContext(ApiServiceContext);

	const fetchProject = useCallback(async () => {
		const fetchInvolvedTeams = async (projectData: IProject | null) => {
			const teamSettings: ITeamSettings[] = [];

			for (const involvedTeam of projectData?.involvedTeams ?? []) {
				const involvedTeamSetting = await teamService.getTeamSettings(
					involvedTeam.id,
				);
				teamSettings.push(involvedTeamSetting);
			}

			return teamSettings;
		};

		const projectData = await projectService.getProject(projectId);
		const settings = await projectService.getProjectSettings(projectId);
		const involvedTeamData = await fetchInvolvedTeams(projectData);

		if (projectData && settings) {
			setProject(projectData);
			setProjectSettings(settings);
			setInvolvedTeams(involvedTeamData);
		}

		setIsLoading(false);
	}, [projectService, teamService, projectId]);

	const onRefreshFeatures = async () => {
		if (project == null) {
			return;
		}

		setIsProjectUpdating(true);
		await projectService.refreshFeaturesForProject(project.id);
	};

	const onMilestonesChanged = async (
		updatedProjectSettings: IProjectSettings,
	) => {
		setProjectSettings(updatedProjectSettings);
		await projectService.updateProject(updatedProjectSettings);

		await projectService.refreshForecastsForProject(projectId);
	};

	const onEditProject = () => {
		navigate(`/projects/edit/${id}`);
	};

	const onTeamSettingsChange = async (updatedTeamSettings: ITeamSettings) => {
		await teamService.updateTeam(updatedTeamSettings);

		await projectService.refreshForecastsForProject(projectId);
	};

	const handleViewChange = (
		_event: React.SyntheticEvent,
		newView: "forecast" | "metrics",
	) => {
		setActiveView(newView);
	};

	useEffect(() => {
		const setUpProjectUpdateSubscription = async () => {
			const handleProjectUpdate = async (update: IUpdateStatus) => {
				if (update.status === "Completed") {
					// Project was updated - reload data!
					await fetchProject();
				}

				updateProjectRefreshButton(update);
			};

			const updateProjectRefreshButton = (update: IUpdateStatus | null) => {
				const isFeatureUpdate =
					update?.updateType === "Features" ||
					update?.updateType === "Forecasts";

				if (isFeatureUpdate) {
					const isUpdating =
						update?.status === "Queued" || update?.status === "InProgress";
					setIsProjectUpdating(isUpdating);
				}
			};

			await updateSubscriptionService.subscribeToFeatureUpdates(
				projectId,
				handleProjectUpdate,
			);
			await updateSubscriptionService.subscribeToForecastUpdates(
				projectId,
				handleProjectUpdate,
			);

			const projectUpdateStatus =
				await updateSubscriptionService.getUpdateStatus("Features", projectId);
			updateProjectRefreshButton(projectUpdateStatus);

			const forecastUpdateStatus =
				await updateSubscriptionService.getUpdateStatus("Forecasts", projectId);
			updateProjectRefreshButton(forecastUpdateStatus);
		};

		if (project && !subscribedToUpdates) {
			subscribedToUpdates = true;
			setUpProjectUpdateSubscription();
		} else {
			fetchProject();
		}

		return () => {
			updateSubscriptionService.unsubscribeFromFeatureUpdates(projectId);
			updateSubscriptionService.unsubscribeFromForecastUpdates(projectId);
		};
	}, [
		project,
		projectId,
		fetchProject,
		updateSubscriptionService,
		subscribedToUpdates,
	]);

	return (
		<LoadingAnimation hasError={false} isLoading={isLoading}>
			<Container maxWidth={false}>
				{project == null ? (
					<></>
				) : (
					<Grid container spacing={3}>
						<Grid size={{ xs: 12 }}>
							<DetailHeader
								leftContent={
									<Stack spacing={1} direction="row">
										<FeatureOwnerHeader featureOwner={project} />
										<Stack
											direction={{ xs: "column", sm: "row" }}
											spacing={1}
											alignItems={{ xs: "flex-start", sm: "center" }}
										>
											<ServiceLevelExpectation
												featureOwner={project}
												hide={activeView !== "forecast"}
											/>
											<SystemWipLimitDisplay
												featureOwner={project}
												hide={activeView !== "forecast"}
											/>
										</Stack>
									</Stack>
								}
								centerContent={
									<Tabs
										value={activeView}
										onChange={handleViewChange}
										aria-label="project view tabs"
									>
										<Tab label="Forecasts" value="forecast" />
										<Tab label="Metrics" value="metrics" />
									</Tabs>
								}
								rightContent={
									<>
										<ActionButton
											buttonText="Refresh Features"
											onClickHandler={onRefreshFeatures}
											maxHeight="40px"
											minWidth="120px"
											externalIsWaiting={isProjectUpdating}
										/>
										<Button
											variant="contained"
											onClick={onEditProject}
											sx={{ maxHeight: "40px", minWidth: "120px" }}
										>
											Edit Project
										</Button>
									</>
								}
							/>
						</Grid>

						<Grid size={{ xs: 12 }}>
							{activeView === "forecast" && project && projectSettings && (
								<ProjectForecastView
									project={project}
									projectSettings={projectSettings}
									involvedTeams={involvedTeams}
									onMilestonesChanged={onMilestonesChanged}
									onTeamSettingsChange={onTeamSettingsChange}
								/>
							)}

							{activeView === "metrics" && project && (
								<ProjectMetricsView project={project} />
							)}
						</Grid>
					</Grid>
				)}
			</Container>
		</LoadingAnimation>
	);
};

export default ProjectDetail;
