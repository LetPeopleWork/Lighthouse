import {
	Alert,
	Box,
	Container,
	FormControl,
	InputLabel,
	Link,
	MenuItem,
	Select,
	type SelectChangeEvent,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Link as RouterLink } from "react-router";
import { useBaseUrl } from "../../../hooks/useBaseUrl";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { IConnectionValidationResult } from "../../../models/WorkTracking/ConnectionValidationResult";
import {
	type ConnectionDefaultName,
	type EstimationDefaultName,
	type FieldDefaultName,
	type IAuthenticationMethod,
	type IConnectionDefaults,
	type IConnectionUsage,
	type IEstimationDefault,
	type IFollowerGroup,
	type IWorkTrackingSystemConnection,
	isEstimationDefaultName,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	type IWorkTrackingSystemOption,
	SecretStates,
} from "../../../models/WorkTracking/WorkTrackingSystemOption";
import type { IWriteBackMappingDefinition } from "../../../models/WorkTracking/WriteBackMappingDefinition";
import AdditionalFieldsEditor from "../../../pages/Settings/Connections/AdditionalFieldsEditor";
import WriteBackMappingsEditor from "../../../pages/Settings/Connections/WriteBackMappingsEditor";
import { ApiError } from "../../../services/Api/ApiError";
import type { ISaveConnectionChoice } from "../../../services/Api/WorkTrackingSystemService";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	type DirectionTipField,
	directionTip,
	directionTipTermsFrom,
} from "../ConnectionDefault/fieldDirectionTips";
import AuthMethodDropdown from "../Connections/AuthMethodDropdown";
import OAuthAuthForm from "../Connections/OAuthAuthForm";
import ReconnectBanner from "../Connections/ReconnectBanner";
import FormSelectField from "../FormSelectField";
import InputGroup from "../InputGroup/InputGroup";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import ValidationActions from "../ValidationActions/ValidationActions";
import EstimationDefaultEditor from "./EstimationDefaultEditor";
import SecretHandlingNotice, {
	containsSecretField,
} from "./SecretHandlingNotice";
import UpdateQuestion, {
	type WhatChanges,
	type WhyUpdate,
} from "./UpdateQuestion";

const OAUTH_KEY_SUFFIX = ".oauth";

const CONFLICT_STATUS = 409;
// The server's own sentence names the field and who still uses it, so it is shown as it stands.
const FIELD_STILL_IN_USE_CODE = "additional-field-in-use";
const CONCURRENCY_CONFLICT_COPY =
	"This connection was changed by someone else since you opened it. Reload to get the latest values, then re-apply your change.";

const OAUTH_LOCKED_COPY =
	"Locked after the OAuth connect handshake. Reconnect to rotate.";

const UNREADABLE_SECRET_COPY =
	"This secret cannot be read with the current encryption key. Enter it again to store it under the key this instance uses now.";

/** Said only when the refusal arrived without a word of its own to say. */
const unexplainedRefusalCopy = (workTrackingSystemTerm: string): string =>
	`Could not connect to the ${workTrackingSystemTerm} with the provided settings. Please review and try again.`;

const isOAuthMethod = (method: IAuthenticationMethod | null): boolean =>
	Boolean(method?.key.endsWith(OAUTH_KEY_SUFFIX));

const secretCannotBeRead = (option: IWorkTrackingSystemOption): boolean =>
	option.secretState === SecretStates.Unreadable;

const resolveAuthOptionHelperText = (
	option: IWorkTrackingSystemOption,
	lockOAuthField: boolean,
): string | undefined => {
	if (secretCannotBeRead(option)) {
		return UNREADABLE_SECRET_COPY;
	}

	if (lockOAuthField) {
		return OAUTH_LOCKED_COPY;
	}

	return undefined;
};

function resolveValidationErrorMessage(
	error: unknown,
	workTrackingSystemTerm: string,
): { message: string; details: string | null } {
	if (error instanceof ApiError && error.code === 403) {
		return {
			message:
				error.message ||
				"You've exceeded the number of additional fields allowed on your plan.",
			details: error.technicalDetails ?? null,
		};
	}

	if (error instanceof ApiError) {
		return { message: error.message, details: error.technicalDetails ?? null };
	}

	return {
		message: unexplainedRefusalCopy(workTrackingSystemTerm),
		details: null,
	};
}

// Stored values and rules are keyed by a field's id, so a field that comes back under a new id is a
// change even when its name and reference are the same. The order of the list still does not matter.
const whatTheFieldsLoad = (fields: IAdditionalFieldDefinition[]): string =>
	fields
		.map((field) =>
			JSON.stringify([field.id, field.displayName, field.reference]),
		)
		.sort((a, b) => a.localeCompare(b))
		.join("\n");

const fieldsChangedSince = (
	loaded: IAdditionalFieldDefinition[],
	current: IAdditionalFieldDefinition[],
): boolean => whatTheFieldsLoad(loaded) !== whatTheFieldsLoad(current);

const NOBODY: IConnectionUsage = { teams: 0, portfolios: 0 };

const isUsed = (usage: IConnectionUsage): boolean =>
	usage.teams + usage.portfolios > 0;

type DefaultsGroup = "teams" | "portfolios";

// A default read while updating changes nothing until its followers are updated, so changing it asks
// whether to update them. A default read when a page loads takes effect by itself, so there is nothing to ask.
type ReadWhen = "updating" | "pageLoads";

const WhereEachDefaultSits: Record<
	ConnectionDefaultName,
	{
		group: DefaultsGroup;
		readWhen: ReadWhen;
		changes?: WhatChanges;
		label: (featureTerm: string) => string;
		// A field read off one end of a link carries a tip saying which end, since either end looks plausible.
		tip?: true;
	}
> = {
	parentOverrideForTeams: {
		group: "teams",
		readWhen: "updating",
		changes: "parents",
		label: () => "Parent Override Field",
		tip: true,
	},
	estimationForTeams: {
		group: "teams",
		readWhen: "pageLoads",
		label: () => "Estimation Field",
	},
	parentOverrideForPortfolios: {
		group: "portfolios",
		readWhen: "updating",
		changes: "parents",
		label: () => "Parent Override Field",
		tip: true,
	},
	dependencyFieldForPortfolios: {
		group: "portfolios",
		readWhen: "updating",
		changes: "dependencies",
		label: () => "Dependency Field",
		tip: true,
	},
	sizeEstimateFieldForPortfolios: {
		group: "portfolios",
		readWhen: "updating",
		changes: "sizes",
		label: () => "Size Estimate Field",
	},
	featureOwnerFieldForPortfolios: {
		group: "portfolios",
		readWhen: "updating",
		changes: "owners",
		label: (featureTerm) => `${featureTerm} Owner Field`,
	},
	estimationForPortfolios: {
		group: "portfolios",
		readWhen: "pageLoads",
		label: () => "Estimation Field",
	},
};

const carriesTip = (name: ConnectionDefaultName): name is DirectionTipField =>
	WhereEachDefaultSits[name].tip === true;

// A server that sends no groups is read as if every owner followed a single default.
const followerGroupsOf = (usage: IConnectionUsage): IFollowerGroup[] =>
	usage.followerGroups ??
	(
		Object.entries(usage.followers ?? {}) as [ConnectionDefaultName, number][]
	).map(([name, count]) => ({ follows: [name], count }));

const followersOfDefault = (
	usage: IConnectionUsage,
	name: ConnectionDefaultName,
): number =>
	followerGroupsOf(usage)
		.filter((group) => group.follows.includes(name))
		.reduce((sum, group) => sum + group.count, 0);

const changedDefaultsToUpdateFollowersOf = (
	loaded: IConnectionDefaults,
	current: IConnectionDefaults,
	usage: IConnectionUsage,
): ConnectionDefaultName[] =>
	(Object.keys(WhereEachDefaultSits) as ConnectionDefaultName[]).filter(
		(name) =>
			WhereEachDefaultSits[name].readWhen === "updating" &&
			followersOfDefault(usage, name) > 0 &&
			(loaded[name] ?? null) !== (current[name] ?? null),
	);

// The Additional Field a default points at, which for an Estimation default sits inside its bundle.
const fieldOfDefault = (
	name: ConnectionDefaultName,
	defaults: IConnectionDefaults,
): number | null | undefined =>
	isEstimationDefaultName(name)
		? defaults[name]?.additionalFieldDefinitionId
		: defaults[name];

const describedInOrder = (bundle: IEstimationDefault) => [
	bundle.additionalFieldDefinitionId,
	bundle.unit,
	bundle.useNonNumericEstimation,
	bundle.estimationCategoryValues,
];

const sameEstimation = (
	loaded: IEstimationDefault | null | undefined,
	current: IEstimationDefault | null | undefined,
): boolean =>
	JSON.stringify(loaded ? describedInOrder(loaded) : null) ===
	JSON.stringify(current ? describedInOrder(current) : null);

const changedEstimationDefaultsWithFollowers = (
	loaded: IConnectionDefaults,
	current: IConnectionDefaults,
	usage: IConnectionUsage,
): EstimationDefaultName[] =>
	(Object.keys(WhereEachDefaultSits) as ConnectionDefaultName[])
		.filter(isEstimationDefaultName)
		.filter(
			(name) =>
				followersOfDefault(usage, name) > 0 &&
				!sameEstimation(loaded[name], current[name]),
		);

// An owner following several of the changed defaults is one owner to update, so it is counted once.
const followersIn = (
	group: DefaultsGroup,
	changed: ConnectionDefaultName[],
	usage: IConnectionUsage,
): number =>
	followerGroupsOf(usage)
		.filter((followers) =>
			followers.follows.some(
				(name) =>
					changed.includes(name) && WhereEachDefaultSits[name].group === group,
			),
		)
		.reduce((sum, followers) => sum + followers.count, 0);

// One question per save: a changed Additional Field updates everyone on the connection, which already
// covers whoever follows a changed default, and a question about updating also saves a changed Estimation
// default. Only a save that changes nothing but Estimation defaults asks just whether to save.
const whyUpdate = (
	fieldsChanged: boolean,
	changedDefaults: ConnectionDefaultName[],
	changedEstimations: EstimationDefaultName[],
	usage: IConnectionUsage,
	featureTerm: string,
): WhyUpdate | null => {
	if (fieldsChanged && isUsed(usage)) {
		return {
			because: "fields",
			teams: usage.teams,
			portfolios: usage.portfolios,
		};
	}
	if (changedDefaults.length === 0 && changedEstimations.length > 0) {
		return {
			because: "estimation",
			teams: followersIn("teams", changedEstimations, usage),
			portfolios: followersIn("portfolios", changedEstimations, usage),
			defaults: changedEstimations.length,
		};
	}
	if (changedDefaults.length === 0) {
		return null;
	}
	return {
		because: "defaults",
		teams: followersIn("teams", changedDefaults, usage),
		portfolios: followersIn("portfolios", changedDefaults, usage),
		defaults: [
			...new Set(
				changedDefaults.map((name) =>
					WhereEachDefaultSits[name].label(featureTerm),
				),
			),
		],
		changes: [
			...new Set(
				changedDefaults.flatMap(
					(name) => WhereEachDefaultSits[name].changes ?? [],
				),
			),
		],
	};
};

interface ConnectionDefaultsSectionProps {
	supportedDefaults: ConnectionDefaultName[];
	defaults: IConnectionDefaults;
	additionalFields: IAdditionalFieldDefinition[];
	onChange: (name: FieldDefaultName, fieldId: number | null) => void;
	onEstimationChange: (
		name: EstimationDefaultName,
		bundle: IEstimationDefault | null,
	) => void;
	tracker: WorkTrackingSystemType | undefined;
}

const ConnectionDefaultsSection: React.FC<ConnectionDefaultsSectionProps> = ({
	supportedDefaults,
	defaults,
	additionalFields,
	onChange,
	onEstimationChange,
	tracker,
}) => {
	const { getTerm } = useTerminology();
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const tipTerms = directionTipTermsFrom(getTerm);
	const title = `Defaults for ${teamsTerm} and ${portfoliosTerm}`;

	const fieldOptions = additionalFields.map((field) => ({
		id: field.id,
		label: field.displayName,
	}));
	const fieldIds = new Set(additionalFields.map((field) => field.id));
	// A field removed from the list above has no entry to show, so its default reads as None.
	const chosenFieldOf = (name: ConnectionDefaultName): number | "" => {
		const fieldId = fieldOfDefault(name, defaults);
		return fieldId != null && fieldIds.has(fieldId) ? fieldId : "";
	};

	const chosenFieldIdOf = (name: ConnectionDefaultName): number | null => {
		const chosen = chosenFieldOf(name);
		return chosen === "" ? null : chosen;
	};

	const groups: { group: DefaultsGroup; heading: string }[] = [
		{ group: "teams", heading: teamsTerm },
		{ group: "portfolios", heading: portfoliosTerm },
	];
	const sections = groups
		.map(({ group, heading }) => ({
			heading,
			rows: supportedDefaults.filter(
				(name) => WhereEachDefaultSits[name].group === group,
			),
		}))
		.filter(({ rows }) => rows.length > 0);

	return (
		<Box component="section" aria-label={title}>
			<InputGroup title={title}>
				<Typography variant="body2" color="text.secondary">
					{`${teamsTerm} and ${portfoliosTerm} on this connection use these unless they set their own.`}
				</Typography>
				{sections.map(({ heading, rows }) => (
					<Box
						key={heading}
						component="fieldset"
						sx={{ border: 0, m: 0, p: 0, minWidth: 0 }}
					>
						<Typography
							component="legend"
							variant="subtitle2"
							color="text.secondary"
						>
							{heading}
						</Typography>
						{rows.map((name) =>
							isEstimationDefaultName(name) ? (
								<EstimationDefaultEditor
									key={name}
									bundle={defaults[name]}
									chosenField={chosenFieldIdOf(name)}
									additionalFields={additionalFields}
									onChange={(bundle) => onEstimationChange(name, bundle)}
								/>
							) : (
								<FormSelectField<number>
									key={name}
									label={WhereEachDefaultSits[name].label(featureTerm)}
									value={chosenFieldOf(name)}
									options={fieldOptions}
									onChange={(fieldId) => onChange(name, fieldId)}
									info={
										carriesTip(name)
											? directionTip(name, tracker, tipTerms)
											: undefined
									}
								/>
							),
						)}
					</Box>
				))}
			</InputGroup>
		</Box>
	);
};

interface ModifyConnectionSettingsProps {
	title: string;
	getSupportedSystems: () => Promise<IWorkTrackingSystemConnection[]>;
	getConnectionSettings: () => Promise<IWorkTrackingSystemConnection | null>;
	saveConnectionSettings: (
		connection: IWorkTrackingSystemConnection,
		choice?: ISaveConnectionChoice,
	) => Promise<unknown>;
	validateConnectionSettings: (
		connection: IWorkTrackingSystemConnection,
	) => Promise<IConnectionValidationResult>;
	disableSave?: boolean;
	saveTooltip?: string;
}

const ModifyConnectionSettings: React.FC<ModifyConnectionSettingsProps> = ({
	title,
	getSupportedSystems,
	getConnectionSettings,
	saveConnectionSettings,
	validateConnectionSettings,
	disableSave = false,
	saveTooltip = "",
}) => {
	const [loading, setLoading] = useState(true);
	const [name, setName] = useState("");
	const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [selectedAuthMethod, setSelectedAuthMethod] =
		useState<IAuthenticationMethod | null>(null);
	const [originalAuthMethodKey, setOriginalAuthMethodKey] = useState<
		string | null
	>(null);
	const [authOptions, setAuthOptions] = useState<IWorkTrackingSystemOption[]>(
		[],
	);
	const [otherOptions, setOtherOptions] = useState<IWorkTrackingSystemOption[]>(
		[],
	);
	const [additionalFields, setAdditionalFields] = useState<
		IAdditionalFieldDefinition[]
	>([]);
	const [writeBackMappings, setWriteBackMappings] = useState<
		IWriteBackMappingDefinition[]
	>([]);
	const [supportedSystems, setSupportedSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [isEditMode, setIsEditMode] = useState(false);
	const [inputsValid, setInputsValid] = useState(false);
	const [loadedFields, setLoadedFields] = useState<
		IAdditionalFieldDefinition[]
	>([]);
	const [usage, setUsage] = useState<IConnectionUsage>(NOBODY);
	const [defaults, setDefaults] = useState<IConnectionDefaults | undefined>(
		undefined,
	);
	const [loadedDefaults, setLoadedDefaults] = useState<IConnectionDefaults>({});
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
	const { getTerm } = useTerminology();
	const workTrackingSystemTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const { licenseStatus } = useLicenseRestrictions();
	const canUsePremiumFeatures = licenseStatus?.canUsePremiumFeatures ?? true;
	const baseUrl = useBaseUrl();

	const getAuthOptionKeys = useCallback(
		(authMethod: IAuthenticationMethod | null): Set<string> => {
			if (!authMethod) return new Set();
			return new Set(authMethod.options.map((opt) => opt.key));
		},
		[],
	);

	const getAllAuthOptionKeys = useCallback(
		(connection: IWorkTrackingSystemConnection | null): Set<string> => {
			if (!connection) return new Set();
			const allKeys = new Set<string>();
			for (const method of connection.availableAuthenticationMethods ?? []) {
				for (const option of method.options) {
					allKeys.add(option.key);
				}
			}
			return allKeys;
		},
		[],
	);

	const getEmptyAuthOptions = useCallback(
		(authMethod: IAuthenticationMethod | null): IWorkTrackingSystemOption[] => {
			if (!authMethod) return [];
			return authMethod.options.map((opt) => ({
				key: opt.key,
				value: "",
				isSecret: opt.isSecret,
				isOptional: opt.isOptional,
			}));
		},
		[],
	);

	const showAuthSection = useMemo(() => {
		const methods =
			selectedWorkTrackingSystem?.availableAuthenticationMethods ?? [];
		if (methods.length === 0) return false;
		if (methods.length === 1 && methods[0].options.length === 0) return false;
		return true;
	}, [selectedWorkTrackingSystem]);

	useEffect(() => {
		const fetchData = async () => {
			setLoading(true);
			try {
				const [systems, existingConnection] = await Promise.all([
					getSupportedSystems(),
					getConnectionSettings(),
				]);

				setSupportedSystems(systems);

				if (existingConnection) {
					// Edit mode
					setIsEditMode(true);
					setName(existingConnection.name);
					setSelectedWorkTrackingSystem(existingConnection);
					setOriginalAuthMethodKey(existingConnection.authenticationMethodKey);

					const availableMethods =
						existingConnection.availableAuthenticationMethods ?? [];
					const initialMethod =
						availableMethods.find(
							(m) => m.key === existingConnection.authenticationMethodKey,
						) ??
						availableMethods[0] ??
						null;
					setSelectedAuthMethod(initialMethod);

					const currentAuthKeys = getAuthOptionKeys(initialMethod);
					const allAuthKeys = getAllAuthOptionKeys(existingConnection);

					// Partition existing options
					const existingAuthOptions: IWorkTrackingSystemOption[] = [];
					const existingOtherOptions: IWorkTrackingSystemOption[] = [];

					for (const option of existingConnection.options) {
						const mappedOption: IWorkTrackingSystemOption = {
							key: option.key,
							value: option.isSecret ? "" : option.value,
							isSecret: option.isSecret,
							isOptional: option.isOptional,
							secretState: option.secretState,
						};
						if (currentAuthKeys.has(option.key)) {
							existingAuthOptions.push(mappedOption);
						} else if (!allAuthKeys.has(option.key)) {
							existingOtherOptions.push(mappedOption);
						}
					}

					setAuthOptions(existingAuthOptions);
					setOtherOptions(existingOtherOptions);
					setAdditionalFields(
						existingConnection.additionalFieldDefinitions ?? [],
					);
					setLoadedFields(existingConnection.additionalFieldDefinitions ?? []);
					setUsage(existingConnection.usedBy ?? NOBODY);
					setDefaults(existingConnection.defaults);
					setLoadedDefaults(existingConnection.defaults ?? {});
					setWriteBackMappings(
						existingConnection.writeBackMappingDefinitions ?? [],
					);
				} else if (systems.length > 0) {
					// Create mode — select the first system as default
					setIsEditMode(false);
					const firstSystem = systems[0];
					setSelectedWorkTrackingSystem(firstSystem);
					setName(firstSystem.name);
					setOriginalAuthMethodKey(null);

					const availableMethods =
						firstSystem.availableAuthenticationMethods ?? [];
					const defaultMethod = availableMethods[0] ?? null;
					setSelectedAuthMethod(defaultMethod);

					setAuthOptions(getEmptyAuthOptions(defaultMethod));

					const allAuthKeys = getAllAuthOptionKeys(firstSystem);
					const nonAuthOptions = firstSystem.options
						.filter((opt) => !allAuthKeys.has(opt.key))
						.map((opt) => ({
							key: opt.key,
							value: opt.value,
							isSecret: opt.isSecret,
							isOptional: opt.isOptional,
						}));
					setOtherOptions(nonAuthOptions);
					setAdditionalFields(firstSystem.additionalFieldDefinitions ?? []);
					setWriteBackMappings(firstSystem.writeBackMappingDefinitions ?? []);
				}
			} catch (error) {
				console.error("Error fetching connection data:", error);
			} finally {
				setLoading(false);
			}
		};

		fetchData().catch((error) => {
			console.error("Error fetching connection data:", error);
		});
	}, [
		getSupportedSystems,
		getConnectionSettings,
		getAuthOptionKeys,
		getAllAuthOptionKeys,
		getEmptyAuthOptions,
	]);

	const allOptions = useMemo(
		() => [...authOptions, ...otherOptions],
		[authOptions, otherOptions],
	);

	const reloadConnection = useCallback(async () => {
		const refreshed = await getConnectionSettings();
		if (refreshed !== null) {
			setSelectedWorkTrackingSystem(refreshed);
		}
	}, [getConnectionSettings]);

	useEffect(() => {
		const authMethodChanged =
			isEditMode &&
			originalAuthMethodKey !== null &&
			selectedAuthMethod?.key !== originalAuthMethodKey;

		const optionsValid = allOptions.every((option) => {
			if (
				isEditMode &&
				option.isSecret &&
				option.value === "" &&
				!authMethodChanged
			) {
				return true;
			}
			return option.isOptional || option.value !== "";
		});
		const nameValid = name !== "";

		setInputsValid(optionsValid && nameValid);
	}, [name, allOptions, isEditMode, originalAuthMethodKey, selectedAuthMethod]);

	const clearValidationFeedback = () => {
		setValidationErrorMessage(null);
		setValidationTechnicalDetails(null);
	};

	const handleSystemChange = (event: SelectChangeEvent<string>) => {
		clearValidationFeedback();
		const system = supportedSystems.find(
			(s) => s.workTrackingSystem === event.target.value,
		);
		if (system) {
			setSelectedWorkTrackingSystem(system);
			setName(system.name);

			const availableMethods = system.availableAuthenticationMethods ?? [];
			const defaultMethod = availableMethods[0] ?? null;
			setSelectedAuthMethod(defaultMethod);

			const allAuthKeys = getAllAuthOptionKeys(system);
			setAuthOptions(getEmptyAuthOptions(defaultMethod));

			const nonAuthOptions = system.options
				.filter((opt) => !allAuthKeys.has(opt.key))
				.map((opt) => ({
					key: opt.key,
					value: opt.value,
					isSecret: opt.isSecret,
					isOptional: opt.isOptional,
				}));
			setOtherOptions(nonAuthOptions);
			setAdditionalFields(system.additionalFieldDefinitions ?? []);
			setWriteBackMappings(system.writeBackMappingDefinitions ?? []);
		}
	};

	const handleAuthMethodKeyChange = (key: string) => {
		clearValidationFeedback();
		const availableMethods =
			selectedWorkTrackingSystem?.availableAuthenticationMethods ?? [];
		const method = availableMethods.find((m) => m.key === key);
		if (method) {
			setSelectedAuthMethod(method);
			setAuthOptions(getEmptyAuthOptions(method));
		}
	};

	const handleAuthOptionChange = (
		changedOption: IWorkTrackingSystemOption,
		newValue: string,
	) => {
		clearValidationFeedback();
		setAuthOptions((prev) =>
			prev.map((option) =>
				option.key === changedOption.key
					? { ...option, value: newValue }
					: option,
			),
		);
	};

	const handleOtherOptionChange = (
		changedOption: IWorkTrackingSystemOption,
		newValue: string,
	) => {
		clearValidationFeedback();
		setOtherOptions((prev) =>
			prev.map((option) =>
				option.key === changedOption.key
					? { ...option, value: newValue }
					: option,
			),
		);
	};

	const handleNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		clearValidationFeedback();
		setName(event.target.value);
	};

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
				defaults,
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

			const why = isEditMode
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
						: CONCURRENCY_CONFLICT_COPY,
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

	const usesOAuth = isOAuthMethod(selectedAuthMethod);
	const showAuthFields =
		showAuthSection &&
		(!usesOAuth || canUsePremiumFeatures) &&
		authOptions.length > 0;
	const showOAuthUpgradeNotice =
		showAuthSection && usesOAuth && !canUsePremiumFeatures;
	const showOAuthConnectForm =
		showAuthSection && usesOAuth && canUsePremiumFeatures;
	const showSecretHandlingNotice = containsSecretField(
		showAuthFields ? allOptions : otherOptions,
	);
	const lockOAuthField = isEditMode && usesOAuth;
	const supportedDefaults = isEditMode
		? (selectedWorkTrackingSystem?.supportedDefaults ?? [])
		: [];

	const handleDefaultChange = (
		name: ConnectionDefaultName,
		value: number | IEstimationDefault | null,
	) => {
		clearValidationFeedback();
		setDefaults((previous) => ({ ...previous, [name]: value }));
	};

	// A default left naming a removed field would still be sent, and every Team or Portfolio following it
	// would read a field the connection no longer has.
	const handleAdditionalFieldsChange = (
		fields: IAdditionalFieldDefinition[],
	) => {
		setAdditionalFields(fields);
		const kept = new Set(fields.map((field) => field.id));
		setDefaults((previous) =>
			previous === undefined
				? previous
				: Object.fromEntries(
						(
							Object.entries(previous) as [
								ConnectionDefaultName,
								IConnectionDefaults[ConnectionDefaultName],
							][]
						).map(([name, value]) => {
							const fieldId = fieldOfDefault(name, previous);
							return [
								name,
								fieldId != null && !kept.has(fieldId) ? null : value,
							];
						}),
					),
		);
	};

	return (
		<LoadingAnimation isLoading={loading} hasError={false}>
			<Container maxWidth={false}>
				<Grid container spacing={3}>
					<Grid size={{ xs: 12 }}>
						<Typography variant="h4">{title}</Typography>
					</Grid>

					{isEditMode &&
						selectedWorkTrackingSystem?.requiresReconnect === true && (
							<Grid size={{ xs: 12 }}>
								<ReconnectBanner
									connection={selectedWorkTrackingSystem}
									onReconnected={reloadConnection}
								/>
							</Grid>
						)}

					<Grid size={{ xs: 12, md: 6 }}>
						<TextField
							label="Connection Name"
							fullWidth
							value={name}
							onChange={handleNameChange}
						/>
					</Grid>

					{!isEditMode && (
						<Grid size={{ xs: 12, md: 6 }}>
							<FormControl fullWidth>
								<InputLabel>{`Select ${workTrackingSystemTerm}`}</InputLabel>
								<Select
									value={selectedWorkTrackingSystem?.workTrackingSystem ?? ""}
									onChange={handleSystemChange}
									label={`Select ${workTrackingSystemTerm}`}
								>
									{supportedSystems.map((system) => (
										<MenuItem
											key={system.workTrackingSystem}
											value={system.workTrackingSystem}
										>
											{system.workTrackingSystem}
										</MenuItem>
									))}
								</Select>
							</FormControl>
						</Grid>
					)}

					{isEditMode && (
						<Grid size={{ xs: 12, md: 6 }}>
							<TextField
								label={workTrackingSystemTerm}
								fullWidth
								disabled
								value={selectedWorkTrackingSystem?.workTrackingSystem ?? ""}
								slotProps={{ input: { readOnly: true } }}
							/>
						</Grid>
					)}

					{(selectedWorkTrackingSystem?.availableAuthenticationMethods
						?.length ?? 0) > 1 && (
						<Grid size={{ xs: 12, md: 6 }}>
							<AuthMethodDropdown
								methods={
									selectedWorkTrackingSystem?.availableAuthenticationMethods ??
									[]
								}
								selectedKey={selectedAuthMethod?.key ?? ""}
								canUsePremiumFeatures={canUsePremiumFeatures}
								onChange={handleAuthMethodKeyChange}
							/>
						</Grid>
					)}

					{showSecretHandlingNotice && (
						<Grid size={{ xs: 12 }}>
							<SecretHandlingNotice />
						</Grid>
					)}

					{showOAuthUpgradeNotice && (
						<Grid size={{ xs: 12 }}>
							<Typography
								variant="subtitle2"
								color="text.secondary"
								sx={{ mb: 1 }}
							>
								Authentication
							</Typography>
							<Alert
								severity="info"
								data-testid="oauth-premium-upgrade-affordance"
							>
								<Typography variant="body2">
									OAuth 2.0 authentication is a Premium feature. Upgrade to
									Premium to connect via OAuth.{" "}
									<Link component={RouterLink} to="/settings/license">
										View license options
									</Link>
								</Typography>
							</Alert>
						</Grid>
					)}

					{showAuthFields && (
						<Grid size={{ xs: 12 }}>
							<Typography
								variant="subtitle2"
								color="text.secondary"
								sx={{ mb: 1 }}
							>
								Authentication
							</Typography>
							<Grid container spacing={2}>
								{authOptions.map((option) => {
									const displayName =
										selectedAuthMethod?.options.find(
											(o) => o.key === option.key,
										)?.displayName ?? option.key;
									return (
										<Grid size={{ xs: 12, md: 6 }} key={option.key}>
											<TextField
												label={displayName}
												type={option.isSecret ? "password" : "text"}
												fullWidth
												value={option.value}
												placeholder={
													isEditMode && option.isSecret && option.value === ""
														? "Leave empty to keep existing value"
														: ""
												}
												slotProps={
													lockOAuthField
														? { input: { readOnly: true } }
														: undefined
												}
												error={secretCannotBeRead(option)}
												helperText={resolveAuthOptionHelperText(
													option,
													lockOAuthField,
												)}
												onChange={(e) =>
													handleAuthOptionChange(option, e.target.value)
												}
											/>
										</Grid>
									);
								})}
							</Grid>
						</Grid>
					)}

					{showOAuthConnectForm && (
						<Grid size={{ xs: 12 }}>
							<OAuthAuthForm
								connectionId={selectedWorkTrackingSystem?.id ?? 0}
								providerKey={selectedAuthMethod?.key ?? ""}
								baseUrl={baseUrl}
							/>
						</Grid>
					)}

					{/* Additional Fields */}
					<Grid size={{ xs: 12 }}>
						<AdditionalFieldsEditor
							workTrackingSystemType={
								selectedWorkTrackingSystem
									? selectedWorkTrackingSystem.workTrackingSystem
									: null
							}
							fields={additionalFields}
							onChange={handleAdditionalFieldsChange}
						/>
					</Grid>

					{supportedDefaults.length > 0 && (
						<Grid size={{ xs: 12 }}>
							<ConnectionDefaultsSection
								supportedDefaults={supportedDefaults}
								defaults={defaults ?? {}}
								additionalFields={additionalFields}
								onChange={handleDefaultChange}
								onEstimationChange={handleDefaultChange}
								tracker={selectedWorkTrackingSystem?.workTrackingSystem}
							/>
						</Grid>
					)}

					{/* Sync with Source (Write-Back Mappings) */}
					<Grid size={{ xs: 12 }}>
						<WriteBackMappingsEditor
							additionalFields={additionalFields}
							mappings={writeBackMappings}
							onChange={setWriteBackMappings}
							workTrackingSystemType={
								selectedWorkTrackingSystem
									? selectedWorkTrackingSystem.workTrackingSystem
									: null
							}
						/>
					</Grid>

					{/* Other Options */}
					{otherOptions.length > 0 && (
						<Grid size={{ xs: 12 }}>
							<Typography
								variant="subtitle2"
								color="text.secondary"
								sx={{ mb: 1 }}
							>
								Options
							</Typography>
							<Grid container spacing={2}>
								{otherOptions.map((option) => (
									<Grid size={{ xs: 12, md: 6 }} key={option.key}>
										<TextField
											label={option.key}
											type={option.isSecret ? "password" : "text"}
											fullWidth
											value={option.value}
											onChange={(e) =>
												handleOtherOptionChange(option, e.target.value)
											}
										/>
									</Grid>
								))}
							</Grid>
						</Grid>
					)}

					{/* Validation + Save Actions */}
					{validationErrorMessage && (
						<Grid size={{ xs: 12 }}>
							<Alert severity="error">
								<Typography variant="body2">
									{validationErrorMessage}
								</Typography>
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
						sx={{
							display: "flex",
							gap: 2,
							justifyContent: "flex-end",
						}}
					>
						<ValidationActions
							onSave={handleSave}
							inputsValid={inputsValid}
							validationFailedMessage={undefined}
							disableSave={disableSave}
							saveTooltip={saveTooltip}
						/>
					</Grid>
				</Grid>
				<UpdateQuestion
					open={connectionAwaitingAnswer !== null}
					why={whyAsked}
					onUpdate={() => saveAnswering(true)}
					onSaveOnly={() => saveAnswering(false)}
					onCancel={() => setConnectionAwaitingAnswer(null)}
				/>
			</Container>
		</LoadingAnimation>
	);
};

export default ModifyConnectionSettings;
