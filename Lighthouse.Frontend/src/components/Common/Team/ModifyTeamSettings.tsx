import {
	Alert,
	AlertTitle,
	Box,
	Container,
	type SelectChangeEvent,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useContext, useMemo, useRef } from "react";
import { useModifySettings } from "../../../hooks/useModifySettings";
import { getDefaultTeamSchema } from "../../../models/Common/DataRetrievalSchemaDefaults";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import AdvancedInputsComponent from "../../../pages/Common/AdvancedInputs/AdvancedInputs";
import ForecastSettingsComponent from "../../../pages/Teams/Edit/ForecastSettingsComponent";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { reconcileDoingStates } from "../../../utils/stateMappingReconciliation";
import { validateStateMappings } from "../../../utils/stateMappingValidation";
import FlowMetricsConfigurationComponent from "../BaseSettings/FlowMetricsConfigurationComponent";
import GeneralSettingsComponent from "../BaseSettings/GeneralSettingsComponent";
import EstimationFieldComponent from "../EstimationField/EstimationFieldComponent";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import CycleTimesEditor from "../StateMappings/CycleTimesEditor";
import StateMappingsEditor from "../StateMappings/StateMappingsEditor";
import WaitStatesEditor from "../StateMappings/WaitStatesEditor";
import StatesList from "../StatesList/StatesList";
import SaveStateIndicator from "../ValidationActions/SaveStateIndicator";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";

interface ModifyTeamSettingsProps {
	title: string;
	getWorkTrackingSystems: () => Promise<IWorkTrackingSystemConnection[]>;
	getTeamSettings: () => Promise<ITeamSettings>;
	saveTeamSettings: (settings: ITeamSettings) => Promise<ITeamSettings>;
	validateTeamSettings: (settings: ITeamSettings) => Promise<boolean>;
	modifyDefaultSettings?: boolean;
	disableSave?: boolean;
}

// Bug #5613: autosave is the only save trigger, so an invalid form has to name what blocks it.
function teamAutoSaveBlockers(
	s: ITeamSettings,
	system: IWorkTrackingSystemConnection | null,
	isDefault: boolean,
	getTerm: (key: string) => string,
): string[] {
	const schema = s.dataRetrievalSchema;
	const reasons: string[] = [];
	if (s.name === "") {
		reasons.push("Enter a Name");
	}
	if ((s.throughputHistory ?? 0) <= 0) {
		reasons.push(
			`Set a ${getTerm(TERMINOLOGY_KEYS.THROUGHPUT)} History of at least one day`,
		);
	}
	if (s.featureWIP === undefined) {
		reasons.push(
			`Set a ${getTerm(TERMINOLOGY_KEYS.FEATURE)} ${getTerm(TERMINOLOGY_KEYS.WIP)}`,
		);
	}
	if (s.toDoStates.length === 0) {
		reasons.push("Add at least one To Do State");
	}
	if (s.doingStates.length === 0) {
		reasons.push("Add at least one Doing State");
	}
	if (s.doneStates.length === 0) {
		reasons.push("Add at least one Done State");
	}
	if (
		schema?.isWorkItemTypesRequired !== false &&
		s.workItemTypes.length === 0
	) {
		reasons.push(
			`Add at least one ${getTerm(TERMINOLOGY_KEYS.WORK_ITEM)} Type`,
		);
	}
	if (isDefault) {
		return reasons;
	}
	if (system === null) {
		reasons.push(`Select a ${getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM)}`);
	} else if (
		schema?.isRequired !== false &&
		(s.dataRetrievalValue ?? "") === ""
	) {
		reasons.push(
			`Enter a ${schema?.displayLabel ?? system.workTrackingSystemGetDataRetrievalDisplayName()}`,
		);
	}
	return reasons;
}

const ModifyTeamSettings: React.FC<ModifyTeamSettingsProps> = ({
	title,
	getWorkTrackingSystems,
	getTeamSettings,
	saveTeamSettings,
	validateTeamSettings,
	modifyDefaultSettings = false,
	disableSave = false,
}) => {
	const { teamService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const teamIdRef = useRef(0);

	const refreshDependentData = useCallback(async () => {
		if (teamIdRef.current > 0) {
			await teamService.updateTeamData(teamIdRef.current);
		}
	}, [teamService]);

	const {
		loading,
		settings: teamSettings,
		workTrackingSystems,
		selectedWorkTrackingSystem,
		formInvalidReasons,
		validationError,
		validationTechnicalDetails,
		saveState,
		refreshFailed,
		reloadDependentData,
		reloadAfterConflict,
		retry,
		updateSettings,
		handleWorkTrackingSystemChange,
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
		additionalFetch: refreshDependentData,
		autoSave: { enabled: true, canSave: !disableSave, refreshOnSave: true },
		validateForm: (s, system, isDefault) =>
			teamAutoSaveBlockers(s, system, isDefault, getTerm),
	});

	teamIdRef.current = teamSettings?.id ?? 0;

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
							saveState={saveState}
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
							doingStates={teamSettings.doingStates || []}
							onChange={(nextMappings) => {
								const reconciledDoing = reconcileDoingStates(
									teamSettings.stateMappings || [],
									nextMappings,
									teamSettings.doingStates || [],
								);
								updateSettings("stateMappings", nextMappings);
								updateSettings("doingStates", reconciledDoing);
							}}
							validationErrors={stateMappingErrors}
							refreshFailed={refreshFailed}
							onReloadDependentData={reloadDependentData}
						/>

						<WaitStatesEditor
							waitStates={teamSettings.waitStates || []}
							doingStates={teamSettings.doingStates || []}
							stateMappings={teamSettings.stateMappings || []}
							onChange={(nextWaitStates) =>
								updateSettings("waitStates", nextWaitStates)
							}
						/>

						<CycleTimesEditor
							cycleTimeDefinitions={teamSettings.cycleTimeDefinitions || []}
							toDoStates={teamSettings.toDoStates || []}
							doingStates={teamSettings.doingStates || []}
							doneStates={teamSettings.doneStates || []}
							stateMappings={teamSettings.stateMappings || []}
							onChange={(nextDefinitions) =>
								updateSettings("cycleTimeDefinitions", nextDefinitions)
							}
						/>

						<FlowMetricsConfigurationComponent
							settings={teamSettings}
							onSettingsChange={updateSettings}
							showFeatureWip={true}
							stalenessSeedDefault={5}
							blockedStalenessSeedDefault={5}
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

						{!disableSave && formInvalidReasons.length > 0 && (
							<Grid size={{ xs: 12 }}>
								<Alert
									severity="warning"
									data-testid="settings-blocking-warning"
								>
									<AlertTitle>Your changes are not being saved</AlertTitle>
									<Box component="ul" sx={{ m: 0, pl: 2 }}>
										{formInvalidReasons.map((reason) => (
											<li key={reason}>
												<Typography variant="body2">{reason}</Typography>
											</li>
										))}
									</Box>
								</Alert>
							</Grid>
						)}

						<Grid
							size={{ xs: 12 }}
							sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}
						>
							<SaveStateIndicator
								saveState={saveState}
								canSave={!disableSave}
								onRetry={retry}
								onReload={() => void reloadAfterConflict()}
							/>
						</Grid>
					</Grid>
				)}
			</Container>
		</LoadingAnimation>
	);
};

export default ModifyTeamSettings;
