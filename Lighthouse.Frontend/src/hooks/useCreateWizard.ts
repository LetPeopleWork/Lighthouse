import { useCallback, useEffect, useMemo, useState } from "react";
import { flushSync } from "react-dom";
import { getWizardsForSystem } from "../components/DataRetrievalWizards";
import type { IBoardInformation } from "../models/Boards/BoardInformation";
import type { IDataRetrievalSchema } from "../models/Common/DataRetrievalSchema";
import type { IDataRetrievalWizard } from "../models/DataRetrievalWizard/DataRetrievalWizard";
import type { IWorkTrackingSystemConnection } from "../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../services/Api/ApiError";
export const STEPS = [
	"Choose Connection",
	"Load Data",
	"Configure",
	"Name & Create",
];
export const STEP_CHOOSE_CONNECTION = 0;
export const STEP_LOAD_DATA = 1;
export const STEP_CONFIGURE = 2;
export const STEP_NAME_CREATE = 3;

export type WizardEntityType = "team" | "portfolio";

interface UseCreateWizardOptions<TDto> {
	entityType: WizardEntityType;
	defaultName: string;
	getConnections: () => Promise<IWorkTrackingSystemConnection[]>;
	getSchema: (
		connection: IWorkTrackingSystemConnection,
	) => IDataRetrievalSchema | null;
	buildDto: (base: WizardDtoBase, name: string) => TDto;
	validateSettings: (settings: TDto) => Promise<boolean>;
	saveSettings: (settings: TDto) => Promise<void>;
}

export interface WizardDtoBase {
	dataRetrievalValue: string;
	workItemTypes: string[];
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
	workTrackingSystemConnectionId: number;
}

// The one definition of "Configure has everything it needs". It takes the values rather than reading
// state so the post-wizard jump can ask it about what the wizard just merged, which the state has not
// caught up with yet (#5610).
const hasEveryConfigInput = (
	base: WizardDtoBase,
	schema: IDataRetrievalSchema | null,
): boolean => {
	if (!schema) return false;
	const hasValidDataRetrieval =
		schema.isRequired === false || base.dataRetrievalValue.trim() !== "";
	const hasValidWorkItemTypes =
		schema.isWorkItemTypesRequired === false || base.workItemTypes.length > 0;
	const hasAllStates =
		base.toDoStates.length > 0 &&
		base.doingStates.length > 0 &&
		base.doneStates.length > 0;
	return hasValidDataRetrieval && hasValidWorkItemTypes && hasAllStates;
};

// A wizard fills in what it could work out and leaves the rest to whatever the user already typed.
const mergedWith = (
	boardInfo: IBoardInformation,
	current: WizardDtoBase,
): WizardDtoBase => ({
	...current,
	dataRetrievalValue:
		boardInfo.dataRetrievalValue.trim() === ""
			? current.dataRetrievalValue
			: boardInfo.dataRetrievalValue,
	workItemTypes:
		boardInfo.workItemTypes.length > 0
			? boardInfo.workItemTypes
			: current.workItemTypes,
	toDoStates:
		boardInfo.toDoStates.length > 0 ? boardInfo.toDoStates : current.toDoStates,
	doingStates:
		boardInfo.doingStates.length > 0
			? boardInfo.doingStates
			: current.doingStates,
	doneStates:
		boardInfo.doneStates.length > 0 ? boardInfo.doneStates : current.doneStates,
});

export function useCreateWizard<TDto>({
	entityType,
	defaultName,
	getConnections,
	getSchema,
	buildDto,
	validateSettings,
	saveSettings,
}: UseCreateWizardOptions<TDto>) {
	const [activeStep, setActiveStep] = useState(STEP_CHOOSE_CONNECTION);
	const [loading, setLoading] = useState(true);
	const [connections, setConnections] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [selectedConnection, setSelectedConnection] =
		useState<IWorkTrackingSystemConnection | null>(null);

	const [dataRetrievalValue, setDataRetrievalValue] = useState("");
	const [workItemTypes, setWorkItemTypes] = useState<string[]>([]);
	const [toDoStates, setToDoStates] = useState<string[]>([]);
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const [doneStates, setDoneStates] = useState<string[]>([]);
	const [name, setName] = useState(defaultName);

	const [validating, setValidating] = useState(false);
	const [validationError, setValidationError] = useState<string | null>(null);
	const [validationTechnicalDetails, setValidationTechnicalDetails] = useState<
		string | null
	>(null);
	const [saving, setSaving] = useState(false);

	const [availableWizards, setAvailableWizards] = useState<
		IDataRetrievalWizard[]
	>([]);
	const [activeWizard, setActiveWizard] = useState<IDataRetrievalWizard | null>(
		null,
	);

	const schema = useMemo(
		() => (selectedConnection ? getSchema(selectedConnection) : null),
		[selectedConnection, getSchema],
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
		setValidationTechnicalDetails(null);

		const wizards = getWizardsForSystem(
			connection.workTrackingSystem,
			entityType,
		);
		setAvailableWizards(wizards);
		setActiveStep(wizards.length > 0 ? STEP_LOAD_DATA : STEP_CONFIGURE);
	};

	const currentBase = useCallback(
		(): WizardDtoBase => ({
			dataRetrievalValue,
			workItemTypes,
			toDoStates,
			doingStates,
			doneStates,
			workTrackingSystemConnectionId: selectedConnection?.id ?? 0,
		}),
		[
			dataRetrievalValue,
			workItemTypes,
			toDoStates,
			doingStates,
			doneStates,
			selectedConnection,
		],
	);

	const configInputsValid = useMemo(
		() =>
			// Stryker disable next-line ConditionalExpression: schema is null exactly when there is no
			// connection, and hasEveryConfigInput already answers false for a null schema.
			selectedConnection != null && hasEveryConfigInput(currentBase(), schema),
		[selectedConnection, schema, currentBase],
	);

	const runValidation = async (): Promise<boolean> => {
		setValidating(true);
		setValidationError(null);
		setValidationTechnicalDetails(null);
		try {
			const dto = buildDto(currentBase(), name);
			const isValid = await validateSettings(dto);
			if (!isValid) {
				setValidationError(
					"Validation failed. Check your configuration and try again.",
				);
				setValidationTechnicalDetails(null);
			}
			return isValid;
		} catch (error) {
			if (error instanceof ApiError) {
				setValidationError(error.message);
				setValidationTechnicalDetails(error.technicalDetails ?? null);
			} else {
				setValidationError(
					"Validation failed. Check your configuration and try again.",
				);
				setValidationTechnicalDetails(null);
			}
			return false;
		} finally {
			setValidating(false);
		}
	};

	const handleWizardComplete = async (boardInfo: IBoardInformation) => {
		const merged = mergedWith(boardInfo, currentBase());

		setDataRetrievalValue(merged.dataRetrievalValue);
		setWorkItemTypes(merged.workItemTypes);
		setToDoStates(merged.toDoStates);
		setDoingStates(merged.doingStates);
		setDoneStates(merged.doneStates);
		setActiveWizard(null);

		setValidating(true);
		setValidationError(null);
		setValidationTechnicalDetails(null);
		try {
			const isValid = await validateSettings(buildDto(merged, name));

			// #5610. The Next button on Configure is gated on the config inputs and this path was not,
			// so a wizard that could not fill everything in landed the user on Name & Create with the
			// gap intact — ValidateTeamSettings does not look at state mappings, so it says valid.
			const complete = hasEveryConfigInput(merged, schema);
			setActiveStep(isValid && complete ? STEP_NAME_CREATE : STEP_CONFIGURE);
		} catch (error) {
			if (error instanceof ApiError) {
				setValidationError(error.message);
				setValidationTechnicalDetails(error.technicalDetails ?? null);
			}
			setActiveStep(STEP_CONFIGURE);
		} finally {
			setValidating(false);
		}
	};

	const handleNext = async () => {
		if (activeStep === STEP_CONFIGURE) {
			const isValid = await runValidation();
			if (isValid) setActiveStep(STEP_NAME_CREATE);
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
		const dto = buildDto(currentBase(), name);
		flushSync(() => setSaving(true));
		try {
			await saveSettings(dto);
		} finally {
			setSaving(false);
		}
	};

	const showDataRetrievalField = schema != null && schema.inputKind !== "none";

	const getDataRetrievalLabel = () => {
		if (schema?.displayLabel) return schema.displayLabel;
		return (
			selectedConnection?.workTrackingSystemGetDataRetrievalDisplayName() ??
			"Query"
		);
	};

	return {
		// state
		activeStep,
		loading,
		connections,
		selectedConnection,
		dataRetrievalValue,
		setDataRetrievalValue,
		workItemTypes,
		setWorkItemTypes,
		toDoStates,
		setToDoStates,
		doingStates,
		setDoingStates,
		doneStates,
		setDoneStates,
		name,
		setName,
		validating,
		validationError,
		validationTechnicalDetails,
		saving,
		availableWizards,
		activeWizard,
		setActiveWizard,
		schema,
		configInputsValid,
		showDataRetrievalField,
		// actions
		selectConnection,
		handleWizardComplete,
		handleWizardCancel: () => setActiveWizard(null),
		handleNext,
		handleBack,
		handleCreate,
		getDataRetrievalLabel,
		setActiveStep,
	};
}
