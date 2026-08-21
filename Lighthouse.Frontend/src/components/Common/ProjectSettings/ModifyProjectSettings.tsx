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
import { useCallback, useContext, useMemo, useRef, useState } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useModifySettings } from "../../../hooks/useModifySettings";
import { getDefaultPortfolioSchema } from "../../../models/Common/DataRetrievalSchemaDefaults";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import type { ITeam } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import AdvancedInputsComponent from "../../../pages/Common/AdvancedInputs/AdvancedInputs";
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
import DependenciesComponent from "./Advanced/DependenciesComponent";
import FeatureSizeComponent from "./Advanced/FeatureSizeComponent";
import OwnershipComponent from "./Advanced/OwnershipComponent";

interface ModifyProjectSettingsProps {
	title: string;
	getWorkTrackingSystems: () => Promise<IWorkTrackingSystemConnection[]>;
	getProjectSettings: () => Promise<IPortfolioSettings>;
	getAllTeams: () => Promise<ITeam[]>;
	saveProjectSettings: (
		settings: IPortfolioSettings,
	) => Promise<IPortfolioSettings>;
	validateProjectSettings: (settings: IPortfolioSettings) => Promise<boolean>;
	modifyDefaultSettings?: boolean;
}

// Bug #5613: autosave is the only save trigger, so an invalid form has to name what blocks it.
function portfolioAutoSaveBlockers(
	s: IPortfolioSettings,
	system: IWorkTrackingSystemConnection | null,
	isDefault: boolean,
	getTerm: (key: string) => string,
): string[] {
	const schema = s.dataRetrievalSchema;
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const reasons: string[] = [];
	if (s.name === "") {
		reasons.push("Enter a Name");
	}
	if (s.defaultAmountOfWorkItemsPerFeature === undefined) {
		reasons.push(
			`Set a Default Number of ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} per ${featureTerm}`,
		);
	}
	if (
		s.usePercentileToCalculateDefaultAmountOfWorkItems &&
		!(s.defaultWorkItemPercentile > 0 && s.percentileHistoryInDays >= 30)
	) {
		reasons.push(
			`Set a ${featureTerm} Size Percentile above 0 and a History in Days of at least 30`,
		);
	}
	if (
		schema?.isWorkItemTypesRequired !== false &&
		s.workItemTypes.length === 0
	) {
		reasons.push(
			`Add at least one ${getTerm(TERMINOLOGY_KEYS.WORK_ITEM)} Type`,
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

const ModifyProjectSettings: React.FC<ModifyProjectSettingsProps> = ({
	title,
	getWorkTrackingSystems,
	getProjectSettings,
	getAllTeams,
	saveProjectSettings,
	validateProjectSettings,
	modifyDefaultSettings = false,
}) => {
	const [teams, setTeams] = useState<ITeam[]>([]);
	const { canUpdatePortfolioData } = useLicenseRestrictions();
	const { portfolioService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const portfolioIdRef = useRef(0);

	const loadTeams = useCallback(async () => {
		setTeams(await getAllTeams());
	}, [getAllTeams]);

	const refreshDependentData = useCallback(async () => {
		await loadTeams();
		if (portfolioIdRef.current > 0) {
			await portfolioService.refreshFeaturesForPortfolio(
				portfolioIdRef.current,
			);
		}
	}, [loadTeams, portfolioService]);

	const {
		loading,
		settings: projectSettings,
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
	} = useModifySettings<IPortfolioSettings>({
		getWorkTrackingSystems,
		getSettings: getProjectSettings,
		saveSettings: saveProjectSettings,
		validateSettings: validateProjectSettings,
		modifyDefaultSettings,
		getSchemaForSystem: getDefaultPortfolioSchema,
		autoSave: {
			enabled: true,
			canSave: canUpdatePortfolioData,
			refreshOnSave: true,
		},
		validateForm: (s, system, isDefault) =>
			portfolioAutoSaveBlockers(s, system, isDefault, getTerm),
		additionalFetch: refreshDependentData,
		initialFetch: loadTeams,
	});

	portfolioIdRef.current = projectSettings?.id ?? 0;

	const stateMappingErrors = useMemo(() => {
		if (!projectSettings) return [];
		return validateStateMappings(projectSettings.stateMappings, [
			...projectSettings.toDoStates,
			...projectSettings.doingStates,
			...projectSettings.doneStates,
		]);
	}, [projectSettings]);

	const onWtsChange = (e: SelectChangeEvent<string>) =>
		handleWorkTrackingSystemChange(e.target.value);

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
							onSettingsChange={updateSettings}
							workTrackingSystems={workTrackingSystems}
							selectedWorkTrackingSystem={selectedWorkTrackingSystem}
							onWorkTrackingSystemChange={onWtsChange}
							showWorkTrackingSystemSelection={!modifyDefaultSettings}
							settingsContext="portfolio"
						/>

						{projectSettings.dataRetrievalSchema?.isWorkItemTypesRequired !==
							false && (
							<WorkItemTypesComponent
								workItemTypes={projectSettings.workItemTypes || []}
								onAddWorkItemType={workItemTypeHandlers.onAdd}
								onRemoveWorkItemType={workItemTypeHandlers.onRemove}
								isForTeam={false}
							/>
						)}

						<StatesList
							toDoStates={projectSettings.toDoStates || []}
							onAddToDoState={toDoHandlers.onAdd}
							onRemoveToDoState={toDoHandlers.onRemove}
							onReorderToDoStates={toDoHandlers.onReorder}
							doingStates={projectSettings.doingStates || []}
							onAddDoingState={doingHandlers.onAdd}
							onRemoveDoingState={doingHandlers.onRemove}
							onReorderDoingStates={doingHandlers.onReorder}
							doneStates={projectSettings.doneStates || []}
							onAddDoneState={doneHandlers.onAdd}
							onRemoveDoneState={doneHandlers.onRemove}
							onReorderDoneStates={doneHandlers.onReorder}
							isForTeam={false}
							stateMappingNames={
								projectSettings.stateMappings
									?.filter((m) => m.name.trim() !== "")
									.map((m) => m.name) || []
							}
						/>

						<StateMappingsEditor
							stateMappings={projectSettings.stateMappings || []}
							doingStates={projectSettings.doingStates || []}
							onChange={(nextMappings) => {
								const reconciledDoing = reconcileDoingStates(
									projectSettings.stateMappings || [],
									nextMappings,
									projectSettings.doingStates || [],
								);
								updateSettings("stateMappings", nextMappings);
								updateSettings("doingStates", reconciledDoing);
							}}
							validationErrors={stateMappingErrors}
							refreshFailed={refreshFailed}
							onReloadDependentData={reloadDependentData}
						/>

						<WaitStatesEditor
							waitStates={projectSettings.waitStates || []}
							doingStates={projectSettings.doingStates || []}
							stateMappings={projectSettings.stateMappings || []}
							onChange={(nextWaitStates) =>
								updateSettings("waitStates", nextWaitStates)
							}
						/>

						<CycleTimesEditor
							cycleTimeDefinitions={projectSettings.cycleTimeDefinitions || []}
							toDoStates={projectSettings.toDoStates || []}
							doingStates={projectSettings.doingStates || []}
							doneStates={projectSettings.doneStates || []}
							stateMappings={projectSettings.stateMappings || []}
							onChange={(nextDefinitions) =>
								updateSettings("cycleTimeDefinitions", nextDefinitions)
							}
						/>

						<FeatureSizeComponent
							projectSettings={projectSettings}
							onProjectSettingsChange={updateSettings}
							additionalFieldDefinitions={
								selectedWorkTrackingSystem?.additionalFieldDefinitions ?? []
							}
						/>

						<OwnershipComponent
							projectSettings={projectSettings}
							onProjectSettingsChange={updateSettings}
							availableTeams={teams}
							additionalFieldDefinitions={
								selectedWorkTrackingSystem?.additionalFieldDefinitions ?? []
							}
						/>

						<FlowMetricsConfigurationComponent
							settings={projectSettings}
							onSettingsChange={updateSettings}
							stalenessSeedDefault={14}
							blockedStalenessSeedDefault={14}
						/>

						<EstimationFieldComponent
							estimationFieldDefinitionId={
								projectSettings.estimationAdditionalFieldDefinitionId ?? null
							}
							onEstimationFieldChange={(v) =>
								updateSettings("estimationAdditionalFieldDefinitionId", v)
							}
							estimationUnit={projectSettings.estimationUnit ?? null}
							onEstimationUnitChange={(v) =>
								updateSettings("estimationUnit", v || null)
							}
							useNonNumericEstimation={
								projectSettings.useNonNumericEstimation ?? false
							}
							onUseNonNumericEstimationChange={(v) =>
								updateSettings("useNonNumericEstimation", v)
							}
							estimationCategoryValues={
								projectSettings.estimationCategoryValues ?? []
							}
							onAddCategoryValue={(v) =>
								updateSettings("estimationCategoryValues", [
									...(projectSettings.estimationCategoryValues ?? []),
									v.trim(),
								])
							}
							onRemoveCategoryValue={(v) =>
								updateSettings(
									"estimationCategoryValues",
									(projectSettings.estimationCategoryValues ?? []).filter(
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

						<DependenciesComponent
							projectSettings={projectSettings}
							onProjectSettingsChange={updateSettings}
							additionalFieldDefinitions={
								selectedWorkTrackingSystem?.additionalFieldDefinitions ?? []
							}
						/>

						<AdvancedInputsComponent
							settings={projectSettings}
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

						{canUpdatePortfolioData && formInvalidReasons.length > 0 && (
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
								canSave={canUpdatePortfolioData}
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

export default ModifyProjectSettings;
