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
import { getDefaultPortfolioSchema } from "../../../models/Common/DataRetrievalSchemaDefaults";
import type { IDataRetrievalWizard } from "../../../models/DataRetrievalWizard/DataRetrievalWizard";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
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

interface CreatePortfolioWizardProps {
	getConnections: () => Promise<IWorkTrackingSystemConnection[]>;
	validatePortfolioSettings: (settings: IPortfolioSettings) => Promise<boolean>;
	savePortfolioSettings: (settings: IPortfolioSettings) => Promise<void>;
	onCancel: () => void;
}

const CreatePortfolioWizard: React.FC<CreatePortfolioWizardProps> = ({
	getConnections,
	validatePortfolioSettings,
	savePortfolioSettings,
	onCancel,
}) => {
	const [activeStep, setActiveStep] = useState(STEP_CHOOSE_CONNECTION);
	const [loading, setLoading] = useState(true);
	const [connections, setConnections] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [selectedConnection, setSelectedConnection] =
		useState<IWorkTrackingSystemConnection | null>(null);

	// Portfolio config state
	const [dataRetrievalValue, setDataRetrievalValue] = useState("");
	const [workItemTypes, setWorkItemTypes] = useState<string[]>([]);
	const [toDoStates, setToDoStates] = useState<string[]>([]);
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const [doneStates, setDoneStates] = useState<string[]>([]);
	const [portfolioName, setPortfolioName] = useState("New Portfolio");

	const [validating, setValidating] = useState(false);
	const [validationError, setValidationError] = useState<string | null>(null);
	const [saving, setSaving] = useState(false);

	// Wizard state
	const [availableWizards, setAvailableWizards] = useState<
		IDataRetrievalWizard[]
	>([]);
	const [activeWizard, setActiveWizard] = useState<IDataRetrievalWizard | null>(
		null,
	);

	const schema = useMemo(
		() =>
			selectedConnection
				? getDefaultPortfolioSchema(selectedConnection.workTrackingSystem)
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

		const wizards = getWizardsForSystem(
			connection.workTrackingSystem,
			"portfolio",
		);
		setAvailableWizards(wizards);

		if (wizards.length > 0) {
			setActiveStep(STEP_LOAD_DATA);
		} else {
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

	const buildPortfolioSettingsDto = useCallback((): IPortfolioSettings => {
		return {
			id: 0,
			name: portfolioName,
			workTrackingSystemConnectionId: selectedConnection?.id ?? 0,
			dataRetrievalValue,
			workItemTypes,
			toDoStates,
			doingStates,
			doneStates,
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 0,
			percentileHistoryInDays: 0,
			sizeEstimateAdditionalFieldDefinitionId: null,
			featureOwnerAdditionalFieldDefinitionId: null,
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
		portfolioName,
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
		try {
			const dto = buildPortfolioSettingsDto();
			const isValid = await validatePortfolioSettings(dto);
			if (!isValid) {
				setValidationError(
					"Validation failed. Check your configuration and try again.",
				);
			}
			return isValid;
		} catch {
			setValidationError(
				"Validation failed. Check your configuration and try again.",
			);
			return false;
		} finally {
			setValidating(false);
		}
	};

	const handleWizardComplete = async (boardInfo: IBoardInformation) => {
		if (boardInfo.dataRetrievalValue.trim()) {
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

		const tempDto: IPortfolioSettings = {
			...buildPortfolioSettingsDto(),
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
		try {
			const isValid = await validatePortfolioSettings(tempDto);
			if (isValid) {
				setActiveStep(STEP_NAME_CREATE);
			} else {
				setActiveStep(STEP_CONFIGURE);
			}
		} catch {
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
			setActiveStep(STEP_CHOOSE_CONNECTION);
		} else {
			setActiveStep((prev) => prev - 1);
		}
	};

	const handleCreate = async () => {
		const dto = buildPortfolioSettingsDto();
		setSaving(true);
		try {
			await savePortfolioSettings(dto);
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

	const handleRemoveToDoState = (s: string) =>
		setToDoStates((prev) => prev.filter((st) => st !== s));
	const handleRemoveDoingState = (s: string) =>
		setDoingStates((prev) => prev.filter((st) => st !== s));
	const handleRemoveDoneState = (s: string) =>
		setDoneStates((prev) => prev.filter((st) => st !== s));

	const showDataRetrievalField = schema != null && schema.inputKind !== "none";

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
					variant="contained"
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
		selectedConnection?.id != null && (
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
				Select the connection to use for this portfolio:
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
				Use a wizard to quickly load your portfolio data, or configure manually:
			</Typography>
			<Box sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}>
				{renderWizardButtons()}
			</Box>
			<Button
				variant="outlined"
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
			{availableWizards.length > 0 && (
				<Box sx={{ display: "flex", gap: 1, flexWrap: "wrap", mt: 1 }}>
					{renderWizardButtons()}
				</Box>
			)}

			{showDataRetrievalField && (
				<TextField
					label={getDataRetrievalLabel()}
					multiline
					rows={4}
					fullWidth
					value={dataRetrievalValue}
					onChange={(e) => setDataRetrievalValue(e.target.value)}
				/>
			)}

			{schema?.isWorkItemTypesRequired !== false && (
				<WorkItemTypesComponent
					workItemTypes={workItemTypes}
					onAddWorkItemType={handleAddWorkItemType}
					onRemoveWorkItemType={handleRemoveWorkItemType}
					isForTeam={false}
				/>
			)}

			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={(s) => setToDoStates((prev) => [...prev, s])}
				onRemoveToDoState={handleRemoveToDoState}
				onReorderToDoStates={setToDoStates}
				doingStates={doingStates}
				onAddDoingState={(s) => setDoingStates((prev) => [...prev, s])}
				onRemoveDoingState={handleRemoveDoingState}
				onReorderDoingStates={setDoingStates}
				doneStates={doneStates}
				onAddDoneState={(s) => setDoneStates((prev) => [...prev, s])}
				onRemoveDoneState={handleRemoveDoneState}
				onReorderDoneStates={setDoneStates}
				isForTeam={false}
			/>

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
				label="Portfolio Name"
				fullWidth
				value={portfolioName}
				onChange={(e) => setPortfolioName(e.target.value)}
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
							disabled={portfolioName.trim() === "" || saving}
						/>
					)}
				</Box>
			</Box>
		</LoadingAnimation>
	);
};

export default CreatePortfolioWizard;
