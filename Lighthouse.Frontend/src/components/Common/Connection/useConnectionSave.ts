import { useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { IConnectionValidationResult } from "../../../models/WorkTracking/ConnectionValidationResult";
import type {
	IAuthenticationMethod,
	IConnectionDefaults,
	IConnectionUsage,
	IWorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";
import type { IWriteBackMappingDefinition } from "../../../models/WorkTracking/WriteBackMappingDefinition";
import { ApiError } from "../../../services/Api/ApiError";
import type { ISaveConnectionChoice } from "../../../services/Api/WorkTrackingSystemService";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	CONFLICT_STATUS,
	CONNECTION_COPY,
	defaultsWithoutTheOldParentNumbers,
	FIELD_STILL_IN_USE_CODE,
	resolveValidationErrorMessage,
	unexplainedRefusalCopy,
} from "./connectionFormRules";
import type { WhyUpdate } from "./UpdateQuestion";
import {
	changedDefaultsToUpdateFollowersOf,
	changedEstimationDefaultsWithFollowers,
	fieldsChangedSince,
	whyUpdate,
} from "./whatASaveAsks";

interface ConnectionSaveInputs {
	name: string;
	selectedWorkTrackingSystem: IWorkTrackingSystemConnection | null;
	selectedAuthMethod: IAuthenticationMethod | null;
	allOptions: IWorkTrackingSystemOption[];
	additionalFields: IAdditionalFieldDefinition[];
	writeBackMappings: IWriteBackMappingDefinition[];
	defaults: IConnectionDefaults | undefined;
	loadedFields: IAdditionalFieldDefinition[];
	loadedDefaults: IConnectionDefaults;
	usage: IConnectionUsage;
	isEditMode: boolean;
	validateConnectionSettings: (
		connection: IWorkTrackingSystemConnection,
	) => Promise<IConnectionValidationResult>;
	saveConnectionSettings: (
		connection: IWorkTrackingSystemConnection,
		choice?: ISaveConnectionChoice,
	) => Promise<unknown>;
}

export const useConnectionSave = ({
	name,
	selectedWorkTrackingSystem,
	selectedAuthMethod,
	allOptions,
	additionalFields,
	writeBackMappings,
	defaults,
	loadedFields,
	loadedDefaults,
	usage,
	isEditMode,
	validateConnectionSettings,
	saveConnectionSettings,
}: ConnectionSaveInputs) => {
	const { getTerm } = useTerminology();
	const workTrackingSystemTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);

	const [whyAsked, setWhyAsked] = useState<WhyUpdate>({
		because: "fields",
		teams: 0,
		portfolios: 0,
	});
	const [connectionAwaitingAnswer, setConnectionAwaitingAnswer] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [validationErrorMessage, setValidationErrorMessage] = useState<
		string | null
	>(null);
	const [validationTechnicalDetails, setValidationTechnicalDetails] = useState<
		string | null
	>(null);

	const clearValidationFeedback = () => {
		setValidationErrorMessage(null);
		setValidationTechnicalDetails(null);
	};

	const whatTheSaveAsks = (): WhyUpdate | null =>
		isEditMode
			? whyUpdate(
					fieldsChangedSince(loadedFields, additionalFields),
					changedDefaultsToUpdateFollowersOf(
						loadedDefaults,
						defaults ?? {},
						usage,
					),
					changedEstimationDefaultsWithFollowers(
						loadedDefaults,
						defaults ?? {},
						usage,
					),
					usage,
					featureTerm,
				)
			: null;

	const handleSave = async () => {
		if (selectedWorkTrackingSystem && selectedAuthMethod) {
			clearValidationFeedback();

			const connection: IWorkTrackingSystemConnection = {
				id: selectedWorkTrackingSystem.id,
				name,
				workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
				options: allOptions,
				authenticationMethodKey: selectedAuthMethod.key,
				workTrackingSystemGetDataRetrievalDisplayName:
					selectedWorkTrackingSystem.workTrackingSystemGetDataRetrievalDisplayName,
				additionalFieldDefinitions: additionalFields,
				writeBackMappingDefinitions: writeBackMappings,
				defaults: defaults && defaultsWithoutTheOldParentNumbers(defaults),
			};

			try {
				const verdict = await validateConnectionSettings(connection);
				if (!verdict.isValid) {
					// The connector knows which field it could not find, which credential was refused,
					// what the work tracking system answered. Saying our own sentence over the top of
					// that leaves the person with nothing to act on.
					setValidationErrorMessage(
						verdict.message ?? unexplainedRefusalCopy(workTrackingSystemTerm),
					);
					setValidationTechnicalDetails(verdict.technicalDetails ?? null);
					return;
				}
			} catch (error) {
				const { message, details } = resolveValidationErrorMessage(
					error,
					workTrackingSystemTerm,
				);
				setValidationErrorMessage(message);
				setValidationTechnicalDetails(details);
				return;
			}

			const why = whatTheSaveAsks();
			if (why) {
				setWhyAsked(why);
				setConnectionAwaitingAnswer(connection);
				return;
			}

			await save(connection);
		}
	};

	const save = async (
		connection: IWorkTrackingSystemConnection,
		choice?: ISaveConnectionChoice,
	) => {
		try {
			await saveConnectionSettings(connection, choice);
		} catch (error_) {
			if (error_ instanceof ApiError && error_.code === CONFLICT_STATUS) {
				setValidationErrorMessage(
					error_.problemCode === FIELD_STILL_IN_USE_CODE
						? error_.message
						: CONNECTION_COPY.concurrencyConflict,
				);
				setValidationTechnicalDetails(null);
				return;
			}
			throw error_;
		}
	};

	const saveAnswering = async (startUpdates: boolean) => {
		if (!connectionAwaitingAnswer) {
			return;
		}
		try {
			await save(connectionAwaitingAnswer, {
				startUpdates,
				additionalFieldsChanged: fieldsChangedSince(
					loadedFields,
					additionalFields,
				),
			});
		} finally {
			setConnectionAwaitingAnswer(null);
		}
	};

	return {
		validationErrorMessage,
		validationTechnicalDetails,
		clearValidationFeedback,
		handleSave,
		whyAsked,
		isAskingWhetherToUpdate: connectionAwaitingAnswer !== null,
		saveAnswering,
		cancelAnswer: () => setConnectionAwaitingAnswer(null),
	};
};
