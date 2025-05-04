import { Container, type SelectChangeEvent, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useEffect, useState } from "react";
import type { IMilestone } from "../../../models/Project/Milestone";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import type { ITeam } from "../../../models/Team/Team";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import AdvancedInputsComponent from "../../../pages/Projects/Edit/AdvancedInputs";
import GeneralInputsComponent from "../../../pages/Projects/Edit/GeneralInputs";
import OwnershipComponent from "../../../pages/Projects/Edit/Ownership";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import MilestonesComponent from "../Milestones/MilestonesComponent";
import StatesList from "../StatesList/StatesList";
import TagsComponent from "../Tags/TagsComponent";
import TeamsList from "../TeamsList/TeamsList";
import ValidationActions from "../ValidationActions/ValidationActions";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";
import WorkTrackingSystemComponent from "../WorkTrackingSystems/WorkTrackingSystemComponent";

interface ModifyProjectSettingsProps {
	title: string;
	getWorkTrackingSystems: () => Promise<IWorkTrackingSystemConnection[]>;
	getProjectSettings: () => Promise<IProjectSettings>;
	getAllTeams: () => Promise<ITeam[]>;
	saveProjectSettings: (settings: IProjectSettings) => Promise<void>;
	validateProjectSettings: (settings: IProjectSettings) => Promise<boolean>;
	modifyDefaultSettings?: boolean;
}

const ModifyProjectSettings: React.FC<ModifyProjectSettingsProps> = ({
	title,
	getWorkTrackingSystems,
	getProjectSettings,
	getAllTeams,
	saveProjectSettings,
	validateProjectSettings,
	modifyDefaultSettings = false,
}) => {
	const [loading, setLoading] = useState<boolean>(false);
	const [projectSettings, setProjectSettings] =
		useState<IProjectSettings | null>(null);
	const [workTrackingSystems, setWorkTrackingSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [teams, setTeams] = useState<ITeam[]>([]);
	const [selectedTeams, setSelectedTeams] = useState<number[]>([]);
	const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [formValid, setFormValid] = useState<boolean>(false);

	const handleTeamSelectionChange = (teamIds: number[]) => {
		setSelectedTeams(teamIds);

		// Clear owning team if it's no longer in the selected teams
		if (
			projectSettings?.owningTeam &&
			!teamIds.includes(projectSettings.owningTeam.id)
		) {
			setProjectSettings((prev) =>
				prev ? { ...prev, owningTeam: undefined } : prev,
			);
		}
	};

	const handleWorkTrackingSystemChange = (event: SelectChangeEvent<string>) => {
		const selectedWorkTrackingSystemName = event.target.value;
		const selectedWorkTrackingSystem =
			workTrackingSystems.find(
				(system) => system.name === selectedWorkTrackingSystemName,
			) ?? null;

		setSelectedWorkTrackingSystem(selectedWorkTrackingSystem);
	};

	const handleOnNewWorkTrackingSystemConnectionAddedDialogClosed = async (
		newConnection: IWorkTrackingSystemConnection,
	) => {
		setWorkTrackingSystems((prevSystems) => [...prevSystems, newConnection]);
		setSelectedWorkTrackingSystem(newConnection);
	};

	const handleAddWorkItemType = (newWorkItemType: string) => {
		if (newWorkItemType.trim()) {
			setProjectSettings((prev) =>
				prev
					? {
							...prev,
							workItemTypes: [
								...(prev.workItemTypes || []),
								newWorkItemType.trim(),
							],
						}
					: prev,
			);
		}
	};

	const handleRemoveWorkItemType = (type: string) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						workItemTypes: (prev.workItemTypes || []).filter(
							(item) => item !== type,
						),
					}
				: prev,
		);
	};

	const handleAddToDoState = (toDoState: string) => {
		if (toDoState.trim()) {
			setProjectSettings((prev) =>
				prev
					? {
							...prev,
							toDoStates: [...(prev.toDoStates || []), toDoState.trim()],
						}
					: prev,
			);
		}
	};

	const handleRemoveToDoState = (toDoState: string) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						toDoStates: (prev.toDoStates || []).filter(
							(item) => item !== toDoState,
						),
					}
				: prev,
		);
	};

	const handleAddDoingState = (doingState: string) => {
		if (doingState.trim()) {
			setProjectSettings((prev) =>
				prev
					? {
							...prev,
							doingStates: [...(prev.doingStates || []), doingState.trim()],
						}
					: prev,
			);
		}
	};

	const handleRemoveDoingState = (doingState: string) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						doingStates: (prev.doingStates || []).filter(
							(item) => item !== doingState,
						),
					}
				: prev,
		);
	};

	const handleAddDoneState = (doneState: string) => {
		if (doneState.trim()) {
			setProjectSettings((prev) =>
				prev
					? {
							...prev,
							doneStates: [...(prev.doneStates || []), doneState.trim()],
						}
					: prev,
			);
		}
	};

	const handleRemoveDoneState = (doneState: string) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						doneStates: (prev.doneStates || []).filter(
							(item) => item !== doneState,
						),
					}
				: prev,
		);
	};

	const handleAddTag = (tag: string) => {
		if (tag.trim()) {
			setProjectSettings((prev) =>
				prev
					? {
							...prev,
							tags: [...(prev.tags || []), tag.trim()],
						}
					: prev,
			);
		}
	};

	const handleRemoveTag = (tag: string) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						tags: (prev.tags || []).filter((item) => item !== tag),
					}
				: prev,
		);
	};

	const handleProjectSettingsChange = (
		key: keyof IProjectSettings,
		value: string | number | boolean | string[] | ITeam | null,
	) => {
		setProjectSettings((prev) => (prev ? { ...prev, [key]: value } : prev));
	};

	const handleAddMilestone = (milestone: IMilestone) => {
		setProjectSettings((prev) =>
			prev
				? { ...prev, milestones: [...(prev.milestones || []), milestone] }
				: prev,
		);
	};

	const handleRemoveMilestone = (name: string) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						milestones: (prev.milestones || []).filter(
							(milestone) => milestone.name !== name,
						),
					}
				: prev,
		);
	};

	const handleUpdateMilestone = (
		name: string,
		updatedMilestone: Partial<IMilestone>,
	) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						milestones: (prev.milestones || []).map((milestone) =>
							milestone.name === name
								? { ...milestone, ...updatedMilestone }
								: milestone,
						),
					}
				: prev,
		);
	};

	const handleSave = async () => {
		if (!projectSettings) {
			return;
		}

		const updatedSettings: IProjectSettings = {
			...projectSettings,
			workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0,
			involvedTeams: teams.filter((team) => selectedTeams.includes(team.id)),
		};

		await saveProjectSettings(updatedSettings);
	};

	const handleValidate = async () => {
		if (!projectSettings || modifyDefaultSettings) {
			return false;
		}

		const updatedSettings: IProjectSettings = {
			...projectSettings,
			workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0,
			involvedTeams: teams.filter((team) => selectedTeams.includes(team.id)),
		};
		return await validateProjectSettings(updatedSettings);
	};

	useEffect(() => {
		const handleStateChange = () => {
			let isFormValid = false;

			if (projectSettings) {
				const hasValidName = projectSettings.name !== "";
				const hasValidDefaultAmountOfFeatures =
					projectSettings.defaultAmountOfWorkItemsPerFeature !== undefined;
				const hasValidHistoricalWorkItemQuery =
					!projectSettings.usePercentileToCalculateDefaultAmountOfWorkItems ||
					(projectSettings.defaultWorkItemPercentile > 0 &&
						projectSettings.historicalFeaturesWorkItemQuery !== "");
				const hasValidAmountOfWorkItemTypes =
					projectSettings.workItemTypes.length > 0;
				const hasAllNecessaryStates =
					projectSettings.toDoStates.length > 0 &&
					projectSettings.doingStates.length > 0 &&
					projectSettings.doneStates.length > 0;
				const hasValidInvolvedTeams = selectedTeams.length > 0;

				isFormValid =
					hasValidName &&
					hasValidDefaultAmountOfFeatures &&
					hasValidHistoricalWorkItemQuery &&
					hasValidAmountOfWorkItemTypes &&
					hasAllNecessaryStates &&
					(modifyDefaultSettings ||
						(hasValidInvolvedTeams &&
							projectSettings?.workItemQuery !== "" &&
							selectedWorkTrackingSystem !== null));
			}

			setFormValid(isFormValid);
		};

		handleStateChange();
	}, [
		projectSettings,
		selectedWorkTrackingSystem,
		modifyDefaultSettings,
		selectedTeams,
	]);

	useEffect(() => {
		const fetchData = async () => {
			setLoading(true);
			try {
				const settings = await getProjectSettings();
				setProjectSettings(settings);
				setSelectedTeams(settings.involvedTeams.map((team) => team.id));

				const systems = await getWorkTrackingSystems();
				setWorkTrackingSystems(systems);

				const fetchedTeams = await getAllTeams();
				setTeams(fetchedTeams);

				setSelectedWorkTrackingSystem(
					systems.find(
						(system) => system.id === settings.workTrackingSystemConnectionId,
					) ?? null,
				);
			} catch (error) {
				console.error("Error fetching data", error);
			} finally {
				setLoading(false);
			}
		};

		fetchData();
	}, [getProjectSettings, getWorkTrackingSystems, getAllTeams]);

	return (
		<LoadingAnimation isLoading={loading} hasError={false}>
			<Container maxWidth={false}>
				<Grid container spacing={3}>
					<Grid size={{ xs: 12 }}>
						<Typography variant="h4">{title}</Typography>
					</Grid>

					<GeneralInputsComponent
						projectSettings={projectSettings}
						onProjectSettingsChange={handleProjectSettingsChange}
					/>

					<WorkItemTypesComponent
						workItemTypes={projectSettings?.workItemTypes || []}
						onAddWorkItemType={handleAddWorkItemType}
						onRemoveWorkItemType={handleRemoveWorkItemType}
					/>

					{!modifyDefaultSettings ? (
						<TeamsList
							allTeams={teams}
							selectedTeams={selectedTeams}
							onSelectionChange={handleTeamSelectionChange}
						/>
					) : (
						<></>
					)}

					<StatesList
						toDoStates={projectSettings?.toDoStates || []}
						onAddToDoState={handleAddToDoState}
						onRemoveToDoState={handleRemoveToDoState}
						doingStates={projectSettings?.doingStates || []}
						onAddDoingState={handleAddDoingState}
						onRemoveDoingState={handleRemoveDoingState}
						doneStates={projectSettings?.doneStates || []}
						onAddDoneState={handleAddDoneState}
						onRemoveDoneState={handleRemoveDoneState}
					/>

					<TagsComponent
						tags={projectSettings?.tags || []}
						onAddTag={handleAddTag}
						onRemoveTag={handleRemoveTag}
					/>

					{!modifyDefaultSettings ? (
						<>
							<WorkTrackingSystemComponent
								workTrackingSystems={workTrackingSystems}
								selectedWorkTrackingSystem={selectedWorkTrackingSystem}
								onWorkTrackingSystemChange={handleWorkTrackingSystemChange}
								onNewWorkTrackingSystemConnectionAdded={
									handleOnNewWorkTrackingSystemConnectionAddedDialogClosed
								}
							/>

							<MilestonesComponent
								milestones={projectSettings?.milestones || []}
								onAddMilestone={handleAddMilestone}
								onRemoveMilestone={handleRemoveMilestone}
								onUpdateMilestone={handleUpdateMilestone}
							/>
						</>
					) : (
						<></>
					)}

					<AdvancedInputsComponent
						projectSettings={projectSettings}
						onProjectSettingsChange={handleProjectSettingsChange}
					/>

					<OwnershipComponent
						projectSettings={projectSettings}
						onProjectSettingsChange={handleProjectSettingsChange}
						currentInvolvedTeams={teams.filter((team) =>
							selectedTeams.includes(team.id),
						)}
					/>

					<Grid
						size={{ xs: 12 }}
						sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}
					>
						<ValidationActions
							onValidate={modifyDefaultSettings ? undefined : handleValidate}
							onSave={handleSave}
							inputsValid={formValid}
							validationFailedMessage="Validation failed - either the connection failed, the query is invalid, or no Features could be found. Check the logs for additional details."
						/>
					</Grid>
				</Grid>
			</Container>
		</LoadingAnimation>
	);
};

export default ModifyProjectSettings;
