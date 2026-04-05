import { useEffect, useState } from "react";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../models/WorkTracking/WorkTrackingSystemConnection";

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
}

// --- Helpers extracted to level 0 to prevent nesting ---

const NULLABLE_FIELDS = new Set([
	"sizeEstimateAdditionalFieldDefinitionId",
	"featureOwnerAdditionalFieldDefinitionId",
	"parentOverrideAdditionalFieldDefinitionId",
	"estimationAdditionalFieldDefinitionId",
	"estimationUnit",
	"owningTeam",
]);

/** Pure helper to update a specific list within the settings object */
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
}: UseModifySettingsOptions<TSettings>) {
	const [loading, setLoading] = useState(false);
	const [settings, setSettings] = useState<TSettings | null>(null);
	const [workTrackingSystems, setWorkTrackingSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [formValid, setFormValid] = useState(false);

	// Flattening effects by moving logic to named functions
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

	useEffect(() => {
		const fetchData = async () => {
			setLoading(true);
			try {
				const [fetchedSettings, systems] = await Promise.all([
					getSettings(),
					getWorkTrackingSystems(),
				]);
				setSettings(fetchedSettings);
				setWorkTrackingSystems(systems);
				setSelectedWorkTrackingSystem(
					systems.find(
						(s) => s.id === fetchedSettings.workTrackingSystemConnectionId,
					) ?? null,
				);
				await additionalFetch?.();
			} catch (error) {
				console.error("Error fetching data", error);
			} finally {
				setLoading(false);
			}
		};
		fetchData();
	}, [getSettings, getWorkTrackingSystems, additionalFetch]);

	const updateSettings = <K extends keyof TSettings>(
		key: K,
		value: TSettings[K] | null,
	) => {
		if (value === null && !NULLABLE_FIELDS.has(key as string)) return;
		setSettings((prev) => (prev ? { ...prev, [key]: value } : prev));
	};

	const handleWorkTrackingSystemChange = (name: string) => {
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
		const updated = {
			...settings,
			workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0,
		};
		if (
			!modifyDefaultSettings &&
			!(await validateSettings(updated as TSettings))
		)
			return;
		await saveSettings(updated as TSettings);
	};

	// Generic handler that consumes the level-0 helper
	const getListHandlers = (
		key: "workItemTypes" | "toDoStates" | "doingStates" | "doneStates",
	) => ({
		onAdd: (val: string) =>
			val.trim() &&
			setSettings((prev) => updateListField(prev, key, "add", val)),
		onRemove: (val: string) =>
			setSettings((prev) => updateListField(prev, key, "remove", val)),
		onReorder: (vals: string[]) =>
			setSettings((prev) => updateListField(prev, key, "reorder", vals)),
	});

	return {
		loading,
		settings,
		workTrackingSystems,
		selectedWorkTrackingSystem,
		formValid,
		updateSettings,
		handleWorkTrackingSystemChange,
		handleSave,
		workItemTypeHandlers: getListHandlers("workItemTypes"),
		toDoHandlers: getListHandlers("toDoStates"),
		doingHandlers: getListHandlers("doingStates"),
		doneHandlers: getListHandlers("doneStates"),
	};
}
