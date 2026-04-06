import {
	Alert,
	Container,
	type SelectChangeEvent,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useMemo, useState } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useModifySettings } from "../../../hooks/useModifySettings";
import { getDefaultPortfolioSchema } from "../../../models/Common/DataRetrievalSchemaDefaults";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import type { ITeam } from "../../../models/Team/Team";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import AdvancedInputsComponent from "../../../pages/Common/AdvancedInputs/AdvancedInputs";
import { validateStateMappings } from "../../../utils/stateMappingValidation";
import FlowMetricsConfigurationComponent from "../BaseSettings/FlowMetricsConfigurationComponent";
import GeneralSettingsComponent from "../BaseSettings/GeneralSettingsComponent";
import EstimationFieldComponent from "../EstimationField/EstimationFieldComponent";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import StateMappingsEditor from "../StateMappings/StateMappingsEditor";
import StatesList from "../StatesList/StatesList";
import ValidationActions from "../ValidationActions/ValidationActions";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";
import FeatureSizeComponent from "./Advanced/FeatureSizeComponent";
import OwnershipComponent from "./Advanced/OwnershipComponent";

interface ModifyProjectSettingsProps {
	title: string;
	getWorkTrackingSystems: () => Promise<IWorkTrackingSystemConnection[]>;
	getProjectSettings: () => Promise<IPortfolioSettings>;
	getAllTeams: () => Promise<ITeam[]>;
	saveProjectSettings: (settings: IPortfolioSettings) => Promise<void>;
	validateProjectSettings: (settings: IPortfolioSettings) => Promise<boolean>;
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
	const [teams, setTeams] = useState<ITeam[]>([]);
	const { canUpdatePortfolioData, maxPortfoliosWithoutPremium } =
		useLicenseRestrictions();

	const fetchTeams = useCallback(async () => {
		const fetchedTeams = await getAllTeams();
		setTeams(fetchedTeams);
	}, [getAllTeams]);

	const {
		loading,
		settings: projectSettings,
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
	} = useModifySettings<IPortfolioSettings>({
		getWorkTrackingSystems,
		getSettings: getProjectSettings,
		saveSettings: saveProjectSettings,
		validateSettings: validateProjectSettings,
		modifyDefaultSettings,
		getSchemaForSystem: getDefaultPortfolioSchema,
		validateForm: (s, system, isDefault) => {
			if (!s) return false;
			const schema = s.dataRetrievalSchema;
			return (
				s.name !== "" &&
				s.defaultAmountOfWorkItemsPerFeature !== undefined &&
				(!s.usePercentileToCalculateDefaultAmountOfWorkItems ||
					(s.defaultWorkItemPercentile > 0 &&
						s.percentileHistoryInDays >= 30)) &&
				(schema?.isWorkItemTypesRequired === false ||
					s.workItemTypes.length > 0) &&
				s.toDoStates.length > 0 &&
				s.doingStates.length > 0 &&
				s.doneStates.length > 0 &&
				(isDefault ||
					(system !== null &&
						(schema?.isRequired === false ||
							(s.dataRetrievalValue ?? "") !== "")))
			);
		},
		additionalFetch: fetchTeams,
	});

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
							onChange={(mappings) => updateSettings("stateMappings", mappings)}
							validationErrors={stateMappingErrors}
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

						<Grid
							size={{ xs: 12 }}
							sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}
						>
							<ValidationActions
								onSave={handleSave}
								inputsValid={formValid}
								disableSave={!canUpdatePortfolioData}
								saveTooltip={`Free users can only update portfolio data for up to ${maxPortfoliosWithoutPremium} portfolio`}
							/>
						</Grid>
					</Grid>
				)}
			</Container>
		</LoadingAnimation>
	);
};

export default ModifyProjectSettings;
