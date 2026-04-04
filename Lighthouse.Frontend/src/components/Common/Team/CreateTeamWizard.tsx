import {
	Alert,
	Box,
	Button,
	Step,
	StepLabel,
	Stepper,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import type { IBoardInformation } from "../../../models/Boards/BoardInformation";
import { getDefaultTeamSchema } from "../../../models/Common/DataRetrievalSchemaDefaults";
import type { IDataRetrievalWizard } from "../../../models/DataRetrievalWizard/DataRetrievalWizard";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	emitCreateFailed,
	emitCreateStarted,
	emitCreateSucceeded,
	emitCreateValidationFailed,
	emitCreateValidationStarted,
	emitCreateValidationSucceeded,
	generateCorrelationId,
} from "../../../services/Telemetry/OnboardingTelemetry";
import { getWizardsForSystem } from "../../DataRetrievalWizards";
import ActionButton from "../ActionButton/ActionButton";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import StatesList from "../StatesList/StatesList";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";

const STEPS = ["Choose Connection", "Load Data", "Configure", "Name & Create"];
const STEP_CHOOSE_CONNECTION = 0;
const STEP_LOAD_DATA = 1;
const STEP_CONFIGURE = 2;
const STEP_NAME_CREATE = 3;

interface CreateTeamWizardProps {
	getConnections: () => Promise<IWorkTrackingSystemConnection[]>;
	validateTeamSettings: (settings: ITeamSettings) => Promise<boolean>;
	saveTeamSettings: (settings: ITeamSettings) => Promise<void>;
	onCancel: () => void;
}

const CreateTeamWizard: React.FC<CreateTeamWizardProps> = ({
	getConnections,
	validateTeamSettings,
	saveTeamSettings,
	onCancel,
}) => {
	const [activeStep, setActiveStep] = useState(STEP_CHOOSE_CONNECTION);
	const [loading, setLoading] = useState(true);
	const [connections, setConnections] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [selectedConnection, setSelectedConnection] =
		useState<IWorkTrackingSystemConnection | null>(null);

	// Team config state
	const [dataRetrievalValue, setDataRetrievalValue] = useState("");
	const [workItemTypes, setWorkItemTypes] = useState<string[]>([]);
	const [toDoStates, setToDoStates] = useState<string[]>([]);
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const [doneStates, setDoneStates] = useState<string[]>([]);
	const [teamName, setTeamName] = useState("New Team");

	const [validating, setValidating] = useState(false);
	const [validationError, setValidationError] = useState<string | null>(null);
	const [saving, setSaving] = useState(false);
	const [correlationId] = useState(generateCorrelationId);

	// Wizard state
	const [availableWizards, setAvailableWizards] = useState<
		IDataRetrievalWizard[]
	>([]);
	const [activeWizard, setActiveWizard] = useState<IDataRetrievalWizard | null>(
		null,
	);

	const telemetryProps = useMemo(
		() => ({
			entityType: "team" as const,
			workTrackingSystem: selectedConnection?.workTrackingSystem,
			wizardUsed: true,
			correlationId,
		}),
		[selectedConnection, correlationId],
	);

	const schema = useMemo(
		() =>
			selectedConnection
				? getDefaultTeamSchema(selectedConnection.workTrackingSystem)
				: null,
		[selectedConnection],
	);

	useEffect(() => {
		const fetchConnections = async () => {
			setLoading(true);
			try {
				const conns = await getConnections();
				setConnections(conns);
			} finally {
				setLoading(false);
			}
		};
		fetchConnections();
	}, [getConnections]);

	const selectConnection = (connection: IWorkTrackingSystemConnection) => {
		setSelectedConnection(connection);
		setDataRetrievalValue("");
		setWorkItemTypes([]);
		setToDoStates([]);
		setDoingStates([]);
		setDoneStates([]);
		setValidationError(null);

		emitCreateStarted({
			...telemetryProps,
			workTrackingSystem: connection.workTrackingSystem,
		});

		const wizards = getWizardsForSystem(connection.workTrackingSystem, "team");
		setAvailableWizards(wizards);

		if (wizards.length > 0) {
			setActiveStep(STEP_LOAD_DATA);
		} else {
			// No wizards available — skip directly to manual configure
			setActiveStep(STEP_CONFIGURE);
		}
	};

	const configInputsValid = useMemo(() => {
		if (!selectedConnection || !schema) return false;

		const hasValidDataRetrieval =
			schema.isRequired === false || dataRetrievalValue.trim() !== "";
		const hasValidWorkItemTypes =
			schema.isWorkItemTypesRequired === false || workItemTypes.length > 0;
		const hasAllStates =
			toDoStates.length > 0 && doingStates.length > 0 && doneStates.length > 0;

		return hasValidDataRetrieval && hasValidWorkItemTypes && hasAllStates;
	}, [
		selectedConnection,
		schema,
		dataRetrievalValue,
		workItemTypes,
		toDoStates,
		doingStates,
		doneStates,
	]);

	const buildTeamSettingsDto = useCallback((): ITeamSettings => {
		return {
			id: 0,
			name: teamName,
			workTrackingSystemConnectionId: selectedConnection?.id ?? 0,
			dataRetrievalValue,
			workItemTypes,
			toDoStates,
			doingStates,
			doneStates,
			throughputHistory: 90,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			featureWIP: 0,
			automaticallyAdjustFeatureWIP: false,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
			parentOverrideAdditionalFieldDefinitionId: null,
			blockedStates: [],
			blockedTags: [],
			stateMappings: [],
			doneItemsCutoffDays: 365,
			processBehaviourChartBaselineStartDate: null,
			processBehaviourChartBaselineEndDate: null,
			estimationAdditionalFieldDefinitionId: null,
			estimationUnit: null,
			useNonNumericEstimation: false,
			estimationCategoryValues: [],
		};
	}, [
		teamName,
		selectedConnection,
		dataRetrievalValue,
		workItemTypes,
		toDoStates,
		doingStates,
		doneStates,
	]);

	const runValidation = async (): Promise<boolean> => {
		setValidating(true);
		setValidationError(null);
		emitCreateValidationStarted(telemetryProps);
		try {
			const dto = buildTeamSettingsDto();
			const isValid = await validateTeamSettings(dto);
			if (isValid) {
				emitCreateValidationSucceeded(telemetryProps);
			} else {
				emitCreateValidationFailed({
					...telemetryProps,
					failureCategory: "validation",
				});
				setValidationError(
					"Validation failed. Check your configuration and try again.",
				);
			}
			return isValid;
		} catch {
			emitCreateValidationFailed({
				...telemetryProps,
				failureCategory: "network",
			});
			setValidationError(
				"Validation failed. Check your configuration and try again.",
			);
			return false;
		} finally {
			setValidating(false);
		}
	};

	const handleWizardComplete = async (boardInfo: IBoardInformation) => {
		// Apply wizard output — only non-empty fields
		if (boardInfo.dataRetrievalValue.trim() !== "") {
			setDataRetrievalValue(boardInfo.dataRetrievalValue);
		}
		if (boardInfo.workItemTypes.length > 0) {
			setWorkItemTypes(boardInfo.workItemTypes);
		}
		if (boardInfo.toDoStates.length > 0) {
			setToDoStates(boardInfo.toDoStates);
		}
		if (boardInfo.doingStates.length > 0) {
			setDoingStates(boardInfo.doingStates);
		}
		if (boardInfo.doneStates.length > 0) {
			setDoneStates(boardInfo.doneStates);
		}
		setActiveWizard(null);

		// Build a temporary DTO with the wizard data to validate
		// (state setters are async, so build inline)
		const tempDto: ITeamSettings = {
			...buildTeamSettingsDto(),
			dataRetrievalValue:
				boardInfo.dataRetrievalValue.trim() === ""
					? dataRetrievalValue
					: boardInfo.dataRetrievalValue,
			workItemTypes:
				boardInfo.workItemTypes.length > 0
					? boardInfo.workItemTypes
					: workItemTypes,
			toDoStates:
				boardInfo.toDoStates.length > 0 ? boardInfo.toDoStates : toDoStates,
			doingStates:
				boardInfo.doingStates.length > 0 ? boardInfo.doingStates : doingStates,
			doneStates:
				boardInfo.doneStates.length > 0 ? boardInfo.doneStates : doneStates,
		};

		setValidating(true);
		setValidationError(null);
		emitCreateValidationStarted(telemetryProps);
		try {
			const isValid = await validateTeamSettings(tempDto);
			if (isValid) {
				emitCreateValidationSucceeded(telemetryProps);
				setActiveStep(STEP_NAME_CREATE);
			} else {
				emitCreateValidationFailed({
					...telemetryProps,
					failureCategory: "validation",
				});
				setActiveStep(STEP_CONFIGURE);
			}
		} catch {
			emitCreateValidationFailed({
				...telemetryProps,
				failureCategory: "network",
			});
			setActiveStep(STEP_CONFIGURE);
		} finally {
			setValidating(false);
		}
	};

	const handleWizardCancel = () => {
		setActiveWizard(null);
	};

	const handleNext = async () => {
		if (activeStep === STEP_CONFIGURE) {
			const isValid = await runValidation();
			if (isValid) {
				setActiveStep(STEP_NAME_CREATE);
			}
		}
	};

	const handleBack = () => {
		setValidationError(null);
		if (activeStep === STEP_CONFIGURE && availableWizards.length === 0) {
			// No wizards → skip Load Data step when going back
			setActiveStep(STEP_CHOOSE_CONNECTION);
		} else {
			setActiveStep((prev) => prev - 1);
		}
	};

	const handleCreate = async () => {
		const dto = buildTeamSettingsDto();
		setSaving(true);
		try {
			await saveTeamSettings(dto);
			emitCreateSucceeded(telemetryProps);
		} catch {
			emitCreateFailed({ ...telemetryProps, failureCategory: "unknown" });
		} finally {
			setSaving(false);
		}
	};

	const handleAddWorkItemType = (type: string) => {
		if (type.trim()) {
			setWorkItemTypes((prev) => [...prev, type.trim()]);
		}
	};

	const handleRemoveWorkItemType = (type: string) => {
		setWorkItemTypes((prev) => prev.filter((t) => t !== type));
	};

	const showDataRetrievalField = schema != null && schema.inputKind !== "none";
	const isDataRetrievalReadOnly = schema?.inputKind !== "freetext";

	const getDataRetrievalLabel = () => {
		if (schema?.displayLabel) return schema.displayLabel;
		return (
			selectedConnection?.workTrackingSystemGetDataRetrievalDisplayName() ??
			"Query"
		);
	};

	const renderWizardButtons = () => (
		<>
			{availableWizards.map((wizard) => (
				<Button
					key={wizard.id}
					variant="outlined"
					onClick={() => setActiveWizard(wizard)}
					sx={{ mr: 1 }}
				>
					{wizard.name}
				</Button>
			))}
		</>
	);

	const renderActiveWizardDialog = () =>
		activeWizard &&
		selectedConnection?.id && (
			<activeWizard.component
				open={true}
				workTrackingSystemConnectionId={selectedConnection.id}
				onComplete={handleWizardComplete}
				onCancel={handleWizardCancel}
				boardType={activeWizard.id === "linear.team" ? "Team" : "Board"}
			/>
		);

	const renderStep1ChooseConnection = () => (
		<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
			<Typography variant="body1">
				Select the connection to use for this team:
			</Typography>
			{connections.map((connection) => (
				<Button
					key={connection.id}
					variant="outlined"
					onClick={() => selectConnection(connection)}
					sx={{ justifyContent: "flex-start", textTransform: "none" }}
				>
					{connection.name}
				</Button>
			))}
		</Box>
	);

	const renderStep2LoadData = () => (
		<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
			<Typography variant="body1">
				Use a wizard to quickly load your team data, or configure manually:
			</Typography>
			<Box sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}>
				{renderWizardButtons()}
			</Box>
			<Button
				variant="text"
				onClick={() => setActiveStep(STEP_CONFIGURE)}
				sx={{ alignSelf: "flex-start" }}
			>
				Configure Manually
			</Button>
			{renderActiveWizardDialog()}
		</Box>
	);

	const renderStep3Configure = () => (
		<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
			{showDataRetrievalField && (
				<TextField
					label={getDataRetrievalLabel()}
					multiline
					rows={4}
					fullWidth
					value={dataRetrievalValue}
					onChange={(e) => setDataRetrievalValue(e.target.value)}
					slotProps={{
						input: {
							readOnly: isDataRetrievalReadOnly,
						},
					}}
				/>
			)}

			{schema?.isWorkItemTypesRequired !== false && (
				<WorkItemTypesComponent
					workItemTypes={workItemTypes}
					onAddWorkItemType={handleAddWorkItemType}
					onRemoveWorkItemType={handleRemoveWorkItemType}
					isForTeam={true}
				/>
			)}

			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={(s) => setToDoStates((prev) => [...prev, s])}
				onRemoveToDoState={(s) =>
					setToDoStates((prev) => prev.filter((st) => st !== s))
				}
				onReorderToDoStates={setToDoStates}
				doingStates={doingStates}
				onAddDoingState={(s) => setDoingStates((prev) => [...prev, s])}
				onRemoveDoingState={(s) =>
					setDoingStates((prev) => prev.filter((st) => st !== s))
				}
				onReorderDoingStates={setDoingStates}
				doneStates={doneStates}
				onAddDoneState={(s) => setDoneStates((prev) => [...prev, s])}
				onRemoveDoneState={(s) =>
					setDoneStates((prev) => prev.filter((st) => st !== s))
				}
				onReorderDoneStates={setDoneStates}
				isForTeam={true}
			/>

			{/* Wizard buttons on Configure step for re-loading data */}
			{availableWizards.length > 0 && (
				<Box sx={{ display: "flex", gap: 1, flexWrap: "wrap", mt: 1 }}>
					{renderWizardButtons()}
				</Box>
			)}

			{renderActiveWizardDialog()}

			{validationError && (
				<Alert severity="error" sx={{ mt: 1 }}>
					{validationError}
				</Alert>
			)}
		</Box>
	);

	const renderStep4NameCreate = () => (
		<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
			<TextField
				label="Team Name"
				fullWidth
				value={teamName}
				onChange={(e) => setTeamName(e.target.value)}
			/>
		</Box>
	);

	return (
		<LoadingAnimation isLoading={loading} hasError={false}>
			<Box sx={{ width: "100%", p: 3 }}>
				<Stepper activeStep={activeStep} sx={{ mb: 3 }}>
					{STEPS.map((label) => (
						<Step key={label}>
							<StepLabel>{label}</StepLabel>
						</Step>
					))}
				</Stepper>

				{activeStep === STEP_CHOOSE_CONNECTION && renderStep1ChooseConnection()}
				{activeStep === STEP_LOAD_DATA && renderStep2LoadData()}
				{activeStep === STEP_CONFIGURE && renderStep3Configure()}
				{activeStep === STEP_NAME_CREATE && renderStep4NameCreate()}

				<Box
					sx={{ display: "flex", justifyContent: "flex-end", gap: 1, mt: 3 }}
				>
					<Button onClick={onCancel} variant="outlined">
						Cancel
					</Button>
					{activeStep > STEP_CHOOSE_CONNECTION && (
						<Button onClick={handleBack} variant="outlined">
							Back
						</Button>
					)}
					{activeStep === STEP_CONFIGURE && (
						<ActionButton
							buttonText="Next"
							onClickHandler={handleNext}
							buttonVariant="contained"
							disabled={!configInputsValid || validating}
						/>
					)}
					{activeStep === STEP_NAME_CREATE && (
						<ActionButton
							buttonText="Create"
							onClickHandler={handleCreate}
							buttonVariant="contained"
							disabled={teamName.trim() === "" || saving}
						/>
					)}
				</Box>
			</Box>
		</LoadingAnimation>
	);
};

export default CreateTeamWizard;
