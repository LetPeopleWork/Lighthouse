import { Container, type SelectChangeEvent, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useEffect, useMemo, useState } from "react";
import { getDefaultTeamSchema } from "../../../models/Common/DataRetrievalSchemaDefaults";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";

import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import AdvancedInputsComponent from "../../../pages/Common/AdvancedInputs/AdvancedInputs";
import ForecastSettingsComponent from "../../../pages/Teams/Edit/ForecastSettingsComponent";
import {
	emitEditSaveFailed,
	emitEditSaveStarted,
	emitEditSaveSucceeded,
	generateCorrelationId,
} from "../../../services/Telemetry/OnboardingTelemetry";
import { validateStateMappings } from "../../../utils/stateMappingValidation";
import FlowMetricsConfigurationComponent from "../BaseSettings/FlowMetricsConfigurationComponent";
import GeneralSettingsComponent from "../BaseSettings/GeneralSettingsComponent";
import EstimationFieldComponent from "../EstimationField/EstimationFieldComponent";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import StateMappingsEditor from "../StateMappings/StateMappingsEditor";
import StatesList from "../StatesList/StatesList";
import ValidationActions from "../ValidationActions/ValidationActions";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";

interface ModifyTeamSettingsProps {
	title: string;
	getWorkTrackingSystems: () => Promise<IWorkTrackingSystemConnection[]>;
	getTeamSettings: () => Promise<ITeamSettings>;
	saveTeamSettings: (settings: ITeamSettings) => Promise<void>;
	validateTeamSettings: (settings: ITeamSettings) => Promise<boolean>;
	modifyDefaultSettings?: boolean;
	disableSave?: boolean;
	saveTooltip?: string;
}

const ModifyTeamSettings: React.FC<ModifyTeamSettingsProps> = ({
	title,
	getWorkTrackingSystems,
	getTeamSettings,
	saveTeamSettings,
	validateTeamSettings,
	modifyDefaultSettings = false,
	disableSave = false,
	saveTooltip = "",
}) => {
	const [loading, setLoading] = useState<boolean>(false);
	const [teamSettings, setTeamSettings] = useState<ITeamSettings | null>(null);
	const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [workTrackingSystems, setWorkTrackingSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [inputsValid, setInputsValid] = useState<boolean>(false);

	const stateMappingErrors = useMemo(() => {
		if (!teamSettings) return [];
		const directStates = [
			...teamSettings.toDoStates,
			...teamSettings.doingStates,
			...teamSettings.doneStates,
		];
		return validateStateMappings(teamSettings.stateMappings, directStates);
	}, [teamSettings]);

	const handleTeamSettingsChange = <K extends keyof ITeamSettings>(
		key: K,
		value: ITeamSettings[K],
	) => {
		setTeamSettings((prev) => (prev ? { ...prev, [key]: value } : prev));
	};

	const handleAddWorkItemType = (newWorkItemType: string) => {
		if (newWorkItemType.trim()) {
			setTeamSettings((prev) =>
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
		setTeamSettings((prev) =>
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
			setTeamSettings((prev) =>
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
		setTeamSettings((prev) =>
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
			setTeamSettings((prev) =>
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
		setTeamSettings((prev) =>
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
			setTeamSettings((prev) =>
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
		setTeamSettings((prev) =>
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
		setTeamSettings((prev) =>
			prev
				? {
						...prev,
						toDoStates: newOrder,
					}
				: prev,
		);
	};

	const handleReorderDoingStates = (newOrder: string[]) => {
		setTeamSettings((prev) =>
			prev
				? {
						...prev,
						doingStates: newOrder,
					}
				: prev,
		);
	};

	const handleReorderDoneStates = (newOrder: string[]) => {
		setTeamSettings((prev) =>
			prev
				? {
						...prev,
						doneStates: newOrder,
					}
				: prev,
		);
	};

	const handleWorkTrackingSystemChange = (event: SelectChangeEvent<string>) => {
		const selectedWorkTrackingSystemName = event.target.value;
		const selectedWorkTrackingSystem =
			workTrackingSystems.find(
				(system) => system.name === selectedWorkTrackingSystemName,
			) ?? null;

		setSelectedWorkTrackingSystem(selectedWorkTrackingSystem);

		if (selectedWorkTrackingSystem) {
			setTeamSettings((prev) =>
				prev
					? {
							...prev,
							dataRetrievalSchema: getDefaultTeamSchema(
								selectedWorkTrackingSystem.workTrackingSystem,
							),
						}
					: prev,
			);
		}
	};

	const handleSave = async () => {
		if (!teamSettings) {
			return;
		}

		const saveCorrelationId = generateCorrelationId();
		const telemetryProps = {
			entityType: "team" as const,
			workTrackingSystem: selectedWorkTrackingSystem?.workTrackingSystem,
			correlationId: saveCorrelationId,
		};
		emitEditSaveStarted(telemetryProps);

		const updatedSettings: ITeamSettings = {
			...teamSettings,
			workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0,
		};

		if (!modifyDefaultSettings) {
			const isValid = await validateTeamSettings(updatedSettings);
			if (!isValid) {
				emitEditSaveFailed({
					...telemetryProps,
					failureCategory: "validation",
				});
				return;
			}
		}

		try {
			await saveTeamSettings(updatedSettings);
			emitEditSaveSucceeded(telemetryProps);
		} catch {
			emitEditSaveFailed({
				...telemetryProps,
				failureCategory: "unknown",
			});
		}
	};

	useEffect(() => {
		const handleStateChange = () => {
			let areInputsValid = false;
			if (teamSettings) {
				const hasValidName = teamSettings.name !== "";
				const hasValidThroughputHistory =
					(teamSettings.throughputHistory ?? 0) > 0;
				const hasValidFeatureWIP = teamSettings.featureWIP !== undefined;
				const hasAllNecessaryStates =
					teamSettings.toDoStates.length > 0 &&
					teamSettings.doingStates.length > 0 &&
					teamSettings.doneStates.length > 0;

				const schema = teamSettings.dataRetrievalSchema;
				const hasValidWorkItemTypes =
					schema?.isWorkItemTypesRequired === false ||
					teamSettings.workItemTypes.length > 0;

				// Check that dataRetrievalValue is not empty when required
				const hasValidDataSource =
					modifyDefaultSettings ||
					(selectedWorkTrackingSystem !== null &&
						(schema?.isRequired === false ||
							(teamSettings.dataRetrievalValue ?? "") !== ""));

				areInputsValid =
					hasValidName &&
					hasValidThroughputHistory &&
					hasValidFeatureWIP &&
					hasAllNecessaryStates &&
					hasValidWorkItemTypes &&
					hasValidDataSource &&
					(modifyDefaultSettings || selectedWorkTrackingSystem !== null);
			}

			setInputsValid(areInputsValid);
		};

		handleStateChange();
	}, [teamSettings, selectedWorkTrackingSystem, modifyDefaultSettings]);

	useEffect(() => {
		const fetchData = async () => {
			setLoading(true);
			try {
				const settings = await getTeamSettings();
				setTeamSettings(settings);

				const workTrackingSystemConnections = await getWorkTrackingSystems();
				setWorkTrackingSystems(workTrackingSystemConnections);

				setSelectedWorkTrackingSystem(
					workTrackingSystemConnections.find(
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
	}, [getTeamSettings, getWorkTrackingSystems]);

	return (
		<LoadingAnimation isLoading={loading} hasError={false}>
			<Container maxWidth={false}>
				{teamSettings && (
					<Grid container spacing={3}>
						<Grid size={{ xs: 12 }}>
							<Typography variant="h4">{title}</Typography>
						</Grid>

						<GeneralSettingsComponent
							settings={teamSettings}
							onSettingsChange={handleTeamSettingsChange}
							workTrackingSystems={workTrackingSystems}
							selectedWorkTrackingSystem={selectedWorkTrackingSystem}
							onWorkTrackingSystemChange={handleWorkTrackingSystemChange}
							showWorkTrackingSystemSelection={!modifyDefaultSettings}
							settingsContext="team"
						/>

						<ForecastSettingsComponent
							teamSettings={teamSettings}
							onTeamSettingsChange={handleTeamSettingsChange}
							isDefaultSettings={modifyDefaultSettings}
						/>

						{teamSettings?.dataRetrievalSchema?.isWorkItemTypesRequired !==
							false && (
							<WorkItemTypesComponent
								workItemTypes={teamSettings?.workItemTypes || []}
								onAddWorkItemType={handleAddWorkItemType}
								onRemoveWorkItemType={handleRemoveWorkItemType}
								isForTeam={true}
							/>
						)}

						<StatesList
							toDoStates={teamSettings?.toDoStates || []}
							onAddToDoState={handleAddToDoState}
							onRemoveToDoState={handleRemoveToDoState}
							onReorderToDoStates={handleReorderToDoStates}
							doingStates={teamSettings?.doingStates || []}
							onAddDoingState={handleAddDoingState}
							onRemoveDoingState={handleRemoveDoingState}
							onReorderDoingStates={handleReorderDoingStates}
							doneStates={teamSettings?.doneStates || []}
							onAddDoneState={handleAddDoneState}
							onRemoveDoneState={handleRemoveDoneState}
							onReorderDoneStates={handleReorderDoneStates}
							isForTeam={true}
							stateMappingNames={
								teamSettings?.stateMappings
									?.filter((m) => m.name.trim() !== "")
									.map((m) => m.name) || []
							}
						/>

						<StateMappingsEditor
							stateMappings={teamSettings?.stateMappings || []}
							onChange={(mappings: IStateMapping[]) =>
								handleTeamSettingsChange("stateMappings", mappings)
							}
							validationErrors={stateMappingErrors}
						/>

						<FlowMetricsConfigurationComponent
							settings={teamSettings}
							onSettingsChange={handleTeamSettingsChange}
							showFeatureWip={true}
						/>

						<EstimationFieldComponent
							estimationFieldDefinitionId={
								teamSettings?.estimationAdditionalFieldDefinitionId ?? null
							}
							onEstimationFieldChange={(value) =>
								handleTeamSettingsChange(
									"estimationAdditionalFieldDefinitionId",
									value,
								)
							}
							estimationUnit={teamSettings?.estimationUnit ?? null}
							onEstimationUnitChange={(value) =>
								handleTeamSettingsChange("estimationUnit", value || null)
							}
							useNonNumericEstimation={
								teamSettings?.useNonNumericEstimation ?? false
							}
							onUseNonNumericEstimationChange={(value) =>
								handleTeamSettingsChange("useNonNumericEstimation", value)
							}
							estimationCategoryValues={
								teamSettings?.estimationCategoryValues ?? []
							}
							onAddCategoryValue={(value) => {
								const current = teamSettings?.estimationCategoryValues ?? [];
								handleTeamSettingsChange("estimationCategoryValues", [
									...current,
									value.trim(),
								]);
							}}
							onRemoveCategoryValue={(value) => {
								const current = teamSettings?.estimationCategoryValues ?? [];
								handleTeamSettingsChange(
									"estimationCategoryValues",
									current.filter((v) => v !== value),
								);
							}}
							onReorderCategoryValues={(values) =>
								handleTeamSettingsChange("estimationCategoryValues", values)
							}
							additionalFieldDefinitions={
								selectedWorkTrackingSystem?.additionalFieldDefinitions ?? []
							}
						/>

						<AdvancedInputsComponent
							settings={teamSettings}
							onSettingsChange={handleTeamSettingsChange}
							additionalFieldDefinitions={
								selectedWorkTrackingSystem?.additionalFieldDefinitions ?? []
							}
						/>

						<Grid
							size={{ xs: 12 }}
							sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}
						>
							<ValidationActions
								onSave={handleSave}
								inputsValid={inputsValid}
								disableSave={disableSave}
								saveTooltip={saveTooltip}
							/>
						</Grid>
					</Grid>
				)}
			</Container>
		</LoadingAnimation>
	);
};

export default ModifyTeamSettings;
