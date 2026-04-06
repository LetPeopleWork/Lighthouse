import {
	Alert,
	Container,
	type SelectChangeEvent,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useMemo } from "react";
import { useModifySettings } from "../../../hooks/useModifySettings";
import { getDefaultTeamSchema } from "../../../models/Common/DataRetrievalSchemaDefaults";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import AdvancedInputsComponent from "../../../pages/Common/AdvancedInputs/AdvancedInputs";
import ForecastSettingsComponent from "../../../pages/Teams/Edit/ForecastSettingsComponent";
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
	const {
		loading,
		settings: teamSettings,
		workTrackingSystems,
		selectedWorkTrackingSystem,
		formValid,
		validationError,
		validationTechnicalDetails,
		updateSettings,
		handleWorkTrackingSystemChange,
		handleSave,
		workItemTypeHandlers,
		toDoHandlers,
		doingHandlers,
		doneHandlers,
	} = useModifySettings<ITeamSettings>({
		getWorkTrackingSystems,
		getSettings: getTeamSettings,
		saveSettings: saveTeamSettings,
		validateSettings: validateTeamSettings,
		modifyDefaultSettings,
		getSchemaForSystem: getDefaultTeamSchema,
		validateForm: (s, system, isDefault) => {
			if (!s) return false;
			const schema = s.dataRetrievalSchema;
			return (
				s.name !== "" &&
				(s.throughputHistory ?? 0) > 0 &&
				s.featureWIP !== undefined &&
				s.toDoStates.length > 0 &&
				s.doingStates.length > 0 &&
				s.doneStates.length > 0 &&
				(schema?.isWorkItemTypesRequired === false ||
					s.workItemTypes.length > 0) &&
				(isDefault ||
					(system !== null &&
						(schema?.isRequired === false ||
							(s.dataRetrievalValue ?? "") !== "")))
			);
		},
	});

	const stateMappingErrors = useMemo(() => {
		if (!teamSettings) return [];
		return validateStateMappings(teamSettings.stateMappings, [
			...teamSettings.toDoStates,
			...teamSettings.doingStates,
			...teamSettings.doneStates,
		]);
	}, [teamSettings]);

	const onWtsChange = (e: SelectChangeEvent<string>) =>
		handleWorkTrackingSystemChange(e.target.value);

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
							onSettingsChange={updateSettings}
							workTrackingSystems={workTrackingSystems}
							selectedWorkTrackingSystem={selectedWorkTrackingSystem}
							onWorkTrackingSystemChange={onWtsChange}
							showWorkTrackingSystemSelection={!modifyDefaultSettings}
							settingsContext="team"
						/>

						<ForecastSettingsComponent
							teamSettings={teamSettings}
							onTeamSettingsChange={updateSettings}
							isDefaultSettings={modifyDefaultSettings}
						/>

						{teamSettings.dataRetrievalSchema?.isWorkItemTypesRequired !==
							false && (
							<WorkItemTypesComponent
								workItemTypes={teamSettings.workItemTypes || []}
								onAddWorkItemType={workItemTypeHandlers.onAdd}
								onRemoveWorkItemType={workItemTypeHandlers.onRemove}
								isForTeam={true}
							/>
						)}

						<StatesList
							toDoStates={teamSettings.toDoStates || []}
							onAddToDoState={toDoHandlers.onAdd}
							onRemoveToDoState={toDoHandlers.onRemove}
							onReorderToDoStates={toDoHandlers.onReorder}
							doingStates={teamSettings.doingStates || []}
							onAddDoingState={doingHandlers.onAdd}
							onRemoveDoingState={doingHandlers.onRemove}
							onReorderDoingStates={doingHandlers.onReorder}
							doneStates={teamSettings.doneStates || []}
							onAddDoneState={doneHandlers.onAdd}
							onRemoveDoneState={doneHandlers.onRemove}
							onReorderDoneStates={doneHandlers.onReorder}
							isForTeam={true}
							stateMappingNames={
								teamSettings.stateMappings
									?.filter((m) => m.name.trim() !== "")
									.map((m) => m.name) || []
							}
						/>

						<StateMappingsEditor
							stateMappings={teamSettings.stateMappings || []}
							onChange={(mappings) => updateSettings("stateMappings", mappings)}
							validationErrors={stateMappingErrors}
						/>

						<FlowMetricsConfigurationComponent
							settings={teamSettings}
							onSettingsChange={updateSettings}
							showFeatureWip={true}
						/>

						<EstimationFieldComponent
							estimationFieldDefinitionId={
								teamSettings.estimationAdditionalFieldDefinitionId ?? null
							}
							onEstimationFieldChange={(v) =>
								updateSettings("estimationAdditionalFieldDefinitionId", v)
							}
							estimationUnit={teamSettings.estimationUnit ?? null}
							onEstimationUnitChange={(v) =>
								updateSettings("estimationUnit", v || null)
							}
							useNonNumericEstimation={
								teamSettings.useNonNumericEstimation ?? false
							}
							onUseNonNumericEstimationChange={(v) =>
								updateSettings("useNonNumericEstimation", v)
							}
							estimationCategoryValues={
								teamSettings.estimationCategoryValues ?? []
							}
							onAddCategoryValue={(v) =>
								updateSettings("estimationCategoryValues", [
									...(teamSettings.estimationCategoryValues ?? []),
									v.trim(),
								])
							}
							onRemoveCategoryValue={(v) =>
								updateSettings(
									"estimationCategoryValues",
									(teamSettings.estimationCategoryValues ?? []).filter(
										(x) => x !== v,
									),
								)
							}
							onReorderCategoryValues={(v) =>
								updateSettings("estimationCategoryValues", v)
							}
							additionalFieldDefinitions={
								selectedWorkTrackingSystem?.additionalFieldDefinitions ?? []
							}
						/>

						<AdvancedInputsComponent
							settings={teamSettings}
							onSettingsChange={updateSettings}
							additionalFieldDefinitions={
								selectedWorkTrackingSystem?.additionalFieldDefinitions ?? []
							}
						/>

						{validationError && (
							<Grid size={{ xs: 12 }}>
								<Alert severity="error">
									<Typography variant="body2">{validationError}</Typography>
									{validationTechnicalDetails && (
										<Typography
											variant="caption"
											sx={{ display: "block", mt: 1 }}
										>
											{validationTechnicalDetails}
										</Typography>
									)}
								</Alert>
							</Grid>
						)}

						<Grid
							size={{ xs: 12 }}
							sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}
						>
							<ValidationActions
								onSave={handleSave}
								inputsValid={formValid}
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
