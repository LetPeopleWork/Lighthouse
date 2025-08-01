import { Container, type SelectChangeEvent, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useEffect, useState } from "react";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import type { ITeam } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import AdvancedInputsComponent from "../../../pages/Common/AdvancedInputs/AdvancedInputs";
import { useTerminology } from "../../../services/TerminologyContext";
import FlowMetricsConfigurationComponent from "../BaseSettings/FlowMetricsConfigurationComponent";
import GeneralSettingsComponent from "../BaseSettings/GeneralSettingsComponent";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import StatesList from "../StatesList/StatesList";
import TagsComponent from "../Tags/TagsComponent";
import TeamsList from "../TeamsList/TeamsList";
import ValidationActions from "../ValidationActions/ValidationActions";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";
import WorkTrackingSystemComponent from "../WorkTrackingSystems/WorkTrackingSystemComponent";
import FeatureSizeComponent from "./Advanced/FeatureSizeComponent";
import OwnershipComponent from "./Advanced/OwnershipComponent";
import UnparentedItemsComponent from "./Advanced/UnparentedItemsComponent";

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

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const queryTerm = getTerm(TERMINOLOGY_KEYS.QUERY);

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

	const handleReorderToDoStates = (newOrder: string[]) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						toDoStates: newOrder,
					}
				: prev,
		);
	};

	const handleReorderDoingStates = (newOrder: string[]) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						doingStates: newOrder,
					}
				: prev,
		);
	};

	const handleReorderDoneStates = (newOrder: string[]) => {
		setProjectSettings((prev) =>
			prev
				? {
						...prev,
						doneStates: newOrder,
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

	const handleProjectSettingsChange = <K extends keyof IProjectSettings>(
		key: K,
		value: IProjectSettings[K] | null,
	) => {
		if (value === null) {
			return;
		}

		setProjectSettings((prev) => (prev ? { ...prev, [key]: value } : prev));
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
				{projectSettings && (
					<Grid container spacing={3}>
						<Grid size={{ xs: 12 }}>
							<Typography variant="h4">{title}</Typography>
						</Grid>
						<GeneralSettingsComponent
							settings={projectSettings}
							onSettingsChange={handleProjectSettingsChange}
						/>

						<WorkItemTypesComponent
							workItemTypes={projectSettings?.workItemTypes || []}
							onAddWorkItemType={handleAddWorkItemType}
							onRemoveWorkItemType={handleRemoveWorkItemType}
							isForTeam={false}
						/>

						{!modifyDefaultSettings && (
							<TeamsList
								allTeams={teams}
								selectedTeams={selectedTeams}
								onSelectionChange={handleTeamSelectionChange}
							/>
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
							isForTeam={false}
							onReorderToDoStates={handleReorderToDoStates}
							onReorderDoingStates={handleReorderDoingStates}
							onReorderDoneStates={handleReorderDoneStates}
						/>

						{!modifyDefaultSettings && (
							<WorkTrackingSystemComponent
								workTrackingSystems={workTrackingSystems}
								selectedWorkTrackingSystem={selectedWorkTrackingSystem}
								onWorkTrackingSystemChange={handleWorkTrackingSystemChange}
								onNewWorkTrackingSystemConnectionAdded={
									handleOnNewWorkTrackingSystemConnectionAddedDialogClosed
								}
							/>
						)}

						<TagsComponent
							tags={projectSettings?.tags || []}
							onAddTag={handleAddTag}
							onRemoveTag={handleRemoveTag}
						/>

						<UnparentedItemsComponent
							projectSettings={projectSettings}
							onProjectSettingsChange={handleProjectSettingsChange}
						/>

						<FeatureSizeComponent
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

						<FlowMetricsConfigurationComponent
							settings={projectSettings}
							onSettingsChange={handleProjectSettingsChange}
						/>

						<AdvancedInputsComponent
							settings={projectSettings}
							onSettingsChange={handleProjectSettingsChange}
						/>

						<Grid
							size={{ xs: 12 }}
							sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}
						>
							<ValidationActions
								onValidate={modifyDefaultSettings ? undefined : handleValidate}
								onSave={handleSave}
								inputsValid={formValid}
								validationFailedMessage={`Validation failed - either the connection failed, the ${queryTerm} is invalid, or no ${featuresTerm} could be found. Check the logs for additional details."`}
							/>
						</Grid>
					</Grid>
				)}
			</Container>
		</LoadingAnimation>
	);
};

export default ModifyProjectSettings;
