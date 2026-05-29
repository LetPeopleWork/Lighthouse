import { useCallback, useEffect, useRef, useState } from "react";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../services/Api/ApiError";

export interface ModifySettingsBase {
	name: string;
	workTrackingSystemConnectionId: number;
	dataRetrievalValue?: string;
	dataRetrievalSchema?: {
		isRequired?: boolean;
		isWorkItemTypesRequired?: boolean;
	} | null;
	workItemTypes: string[];
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
	stateMappings: unknown[];
}

export type SaveState = "idle" | "saving" | "saved" | "error";

export interface AutoSaveOptions {
	enabled: boolean;
	canSave: boolean;
	debounceMs?: number;
	refreshOnSave?: boolean;
}

interface UseModifySettingsOptions<TSettings extends ModifySettingsBase> {
	getWorkTrackingSystems: () => Promise<IWorkTrackingSystemConnection[]>;
	getSettings: () => Promise<TSettings>;
	saveSettings: (settings: TSettings) => Promise<void>;
	validateSettings: (settings: TSettings) => Promise<boolean>;
	modifyDefaultSettings: boolean;
	validateForm: (
		settings: TSettings,
		selectedSystem: IWorkTrackingSystemConnection | null,
		modifyDefaultSettings: boolean,
	) => boolean;
	getSchemaForSystem: (
		wts: WorkTrackingSystemType,
	) => TSettings["dataRetrievalSchema"];
	additionalFetch?: () => Promise<void>;
	autoSave?: AutoSaveOptions;
}

const DEBOUNCE_MS = 300;

const NULLABLE_FIELDS = new Set([
	"sizeEstimateAdditionalFieldDefinitionId",
	"featureOwnerAdditionalFieldDefinitionId",
	"parentOverrideAdditionalFieldDefinitionId",
	"estimationAdditionalFieldDefinitionId",
	"estimationUnit",
	"owningTeam",
	"forecastFilterRuleSetJson",
]);

function updateListField<T extends ModifySettingsBase>(
	prev: T | null,
	key: keyof T,
	action: "add" | "remove" | "reorder",
	payload: string | string[],
): T | null {
	if (!prev) return prev;

	const currentList = (prev[key] as string[]) || [];

	switch (action) {
		case "add":
			return { ...prev, [key]: [...currentList, (payload as string).trim()] };
		case "remove":
			return { ...prev, [key]: currentList.filter((v) => v !== payload) };
		case "reorder":
			return { ...prev, [key]: payload as string[] };
		default:
			return prev;
	}
}

export function useModifySettings<TSettings extends ModifySettingsBase>({
	getWorkTrackingSystems,
	getSettings,
	saveSettings,
	validateSettings,
	modifyDefaultSettings,
	validateForm,
	getSchemaForSystem,
	additionalFetch,
	autoSave,
}: UseModifySettingsOptions<TSettings>) {
	const [saveState, setSaveState] = useState<SaveState>("idle");
	const [loading, setLoading] = useState(false);
	const lastSavePayloadRef = useRef<TSettings | null>(null);
	const requestSeqRef = useRef(0);
	const hasInteractedRef = useRef(false);
	const [settings, setSettings] = useState<TSettings | null>(null);
	const [workTrackingSystems, setWorkTrackingSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [formValid, setFormValid] = useState(false);
	const [validationError, setValidationError] = useState<string | null>(null);
	const [validationTechnicalDetails, setValidationTechnicalDetails] = useState<
		string | null
	>(null);

	const getSettingsRef = useRef(getSettings);
	const getWorkTrackingSystemsRef = useRef(getWorkTrackingSystems);
	const additionalFetchRef = useRef(additionalFetch);
	const saveSettingsRef = useRef(saveSettings);
	getSettingsRef.current = getSettings;
	getWorkTrackingSystemsRef.current = getWorkTrackingSystems;
	additionalFetchRef.current = additionalFetch;
	saveSettingsRef.current = saveSettings;

	useEffect(() => {
		if (settings) {
			setFormValid(
				validateForm(
					settings,
					selectedWorkTrackingSystem,
					modifyDefaultSettings,
				),
			);
		}
	}, [
		settings,
		selectedWorkTrackingSystem,
		modifyDefaultSettings,
		validateForm,
	]);

	const autoSaveEnabled = autoSave?.enabled ?? false;
	const autoSaveCanSave = autoSave?.canSave ?? false;
	const autoSaveDebounceMs = autoSave?.debounceMs ?? DEBOUNCE_MS;
	const autoRefreshOnSave = autoSave?.refreshOnSave ?? false;
	const selectedConnectionId = selectedWorkTrackingSystem?.id ?? 0;

	const dispatchSave = useCallback(
		(payload: TSettings) => {
			lastSavePayloadRef.current = payload;
			requestSeqRef.current += 1;
			const seq = requestSeqRef.current;
			const isLatest = () => seq === requestSeqRef.current;
			const applyIfLatest = (next: SaveState) => {
				if (isLatest()) setSaveState(next);
			};
			setSaveState("saving");
			return saveSettingsRef.current(payload).then(
				() => {
					applyIfLatest("saved");
					if (autoRefreshOnSave && isLatest()) {
						void additionalFetchRef.current?.();
					}
				},
				() => applyIfLatest("error"),
			);
		},
		[autoRefreshOnSave],
	);

	const retry = useCallback(() => {
		const payload = lastSavePayloadRef.current;
		if (!payload) return;
		void dispatchSave(payload);
	}, [dispatchSave]);

	useEffect(() => {
		if (!autoSaveEnabled || !autoSaveCanSave || !formValid) {
			return;
		}
		if (!hasInteractedRef.current || !settings) {
			return;
		}

		const settingsToSave = {
			...settings,
			workTrackingSystemConnectionId: selectedConnectionId,
		} as TSettings;

		const timer = setTimeout(() => {
			void dispatchSave(settingsToSave);
		}, autoSaveDebounceMs);

		return () => clearTimeout(timer);
	}, [
		autoSaveEnabled,
		autoSaveCanSave,
		autoSaveDebounceMs,
		formValid,
		settings,
		selectedConnectionId,
		dispatchSave,
	]);

	useEffect(() => {
		const fetchData = async () => {
			setLoading(true);
			try {
				const [fetchedSettings, systems] = await Promise.all([
					getSettingsRef.current(),
					getWorkTrackingSystemsRef.current(),
				]);
				setSettings(fetchedSettings);
				setWorkTrackingSystems(systems);
				setSelectedWorkTrackingSystem(
					systems.find(
						(s) => s.id === fetchedSettings.workTrackingSystemConnectionId,
					) ?? null,
				);
				await additionalFetchRef.current?.();
			} catch (error) {
				console.error("Error fetching data", error);
			} finally {
				setLoading(false);
			}
		};
		fetchData();
		// Intentionally empty: load once on mount.
		// Callbacks are always current via refs; no re-fetch on identity changes.
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, []);

	const updateSettings = <K extends keyof TSettings>(
		key: K,
		value: TSettings[K] | null,
	) => {
		if (value === null && !NULLABLE_FIELDS.has(key as string)) return;
		hasInteractedRef.current = true;
		setValidationError(null);
		setValidationTechnicalDetails(null);
		setSettings((prev) => (prev ? { ...prev, [key]: value } : prev));
	};

	const handleWorkTrackingSystemChange = (name: string) => {
		setValidationError(null);
		setValidationTechnicalDetails(null);
		const system = workTrackingSystems.find((s) => s.name === name) ?? null;
		setSelectedWorkTrackingSystem(system);
		if (system) {
			setSettings((prev) =>
				prev
					? {
							...prev,
							dataRetrievalSchema: getSchemaForSystem(
								system.workTrackingSystem,
							),
						}
					: prev,
			);
		}
	};

	const handleSave = async () => {
		if (!settings) return;
		setValidationError(null);
		setValidationTechnicalDetails(null);
		const updated = {
			...settings,
			workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0,
		};

		if (!modifyDefaultSettings) {
			try {
				const isValid = await validateSettings(updated as TSettings);
				if (!isValid) {
					setValidationError(
						"Validation failed. Check your configuration and try again.",
					);
					setValidationTechnicalDetails(null);
					return;
				}
			} catch (error) {
				if (error instanceof ApiError) {
					setValidationError(error.message);
					setValidationTechnicalDetails(error.technicalDetails ?? null);
					return;
				}

				throw error;
			}
		}

		await saveSettings(updated as TSettings);
	};

	const getListHandlers = (
		key: "workItemTypes" | "toDoStates" | "doingStates" | "doneStates",
	) => ({
		onAdd: (val: string) => {
			if (!val.trim()) return;
			hasInteractedRef.current = true;
			setSettings((prev) => updateListField(prev, key, "add", val));
		},
		onRemove: (val: string) => {
			hasInteractedRef.current = true;
			setSettings((prev) => updateListField(prev, key, "remove", val));
		},
		onReorder: (vals: string[]) => {
			hasInteractedRef.current = true;
			setSettings((prev) => updateListField(prev, key, "reorder", vals));
		},
	});

	return {
		loading,
		settings,
		workTrackingSystems,
		selectedWorkTrackingSystem,
		formValid,
		validationError,
		validationTechnicalDetails,
		updateSettings,
		handleWorkTrackingSystemChange,
		handleSave,
		saveState,
		retry,
		workItemTypeHandlers: getListHandlers("workItemTypes"),
		toDoHandlers: getListHandlers("toDoStates"),
		doingHandlers: getListHandlers("doingStates"),
		doneHandlers: getListHandlers("doneStates"),
	};
}
