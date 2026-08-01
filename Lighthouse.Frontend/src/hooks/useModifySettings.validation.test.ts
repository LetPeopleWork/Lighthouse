import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../services/Api/ApiError";
import {
	type AutoSaveOptions,
	type ModifySettingsBase,
	useModifySettings,
} from "./useModifySettings";

type SimpleSettings = ModifySettingsBase;

const DEBOUNCE_MS = 300;

const makeConnection = (
	id: number,
	name: string,
	workTrackingSystem: WorkTrackingSystemType = "AzureDevOps",
): IWorkTrackingSystemConnection => ({
	id,
	name,
	workTrackingSystem,
	options: [],
	availableAuthenticationMethods: [],
	authenticationMethodKey: "ado.pat",
	workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL Query",
	additionalFieldDefinitions: [],
	writeBackMappingDefinitions: [],
});

const atlasSettings = (
	overrides: Partial<SimpleSettings> = {},
): SimpleSettings => ({
	name: "Atlas Delivery Team",
	workTrackingSystemConnectionId: 1,
	dataRetrievalValue: "my-query",
	dataRetrievalSchema: { isRequired: true, isWorkItemTypesRequired: true },
	workItemTypes: ["Story", "Bug"],
	toDoStates: ["New"],
	doingStates: ["Active"],
	doneStates: ["Done"],
	stateMappings: [],
	...overrides,
});

const teamAdminCanSave: AutoSaveOptions = {
	enabled: true,
	canSave: true,
	debounceMs: DEBOUNCE_MS,
};

const makeArgs = (
	overrides: Partial<
		Parameters<typeof useModifySettings<SimpleSettings>>[0]
	> = {},
	autoSave: AutoSaveOptions = teamAdminCanSave,
) => ({
	getWorkTrackingSystems: vi
		.fn()
		.mockResolvedValue([makeConnection(1, "Atlas Tracker")]),
	getSettings: vi.fn().mockResolvedValue(atlasSettings()),
	saveSettings: vi.fn().mockResolvedValue(undefined),
	validateSettings: vi.fn().mockResolvedValue(true),
	modifyDefaultSettings: false,
	validateForm: vi
		.fn()
		.mockImplementation((s: SimpleSettings) =>
			s.name !== "" ? [] : ["Enter a Name"],
		),
	getSchemaForSystem: vi
		.fn()
		.mockReturnValue({ isRequired: true, isWorkItemTypesRequired: true }),
	autoSave,
	...overrides,
});

const settleDebounce = async () => {
	await act(async () => {
		vi.advanceTimersByTime(DEBOUNCE_MS);
	});
};

describe("@BUG-5628 @in-memory auto-save validates connector settings", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@BUG-5628 @error warns the coach when the settings the auto-save just persisted do not validate", async () => {
		const validateSettings = vi.fn().mockResolvedValue(false);
		const args = makeArgs({ validateSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "bad-query"));
		await settleDebounce();
		await waitFor(() => expect(result.current.saveState).toBe("saved"));

		await waitFor(() =>
			expect(result.current.validationError).toEqual(expect.any(String)),
		);
		expect(result.current.validationError).not.toBe("");
		expect(validateSettings).toHaveBeenCalledTimes(1);
	});

	it("@BUG-5628 still persists the edit even when the validation that follows it fails", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const validateSettings = vi.fn().mockResolvedValue(false);
		const args = makeArgs({ saveSettings, validateSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "bad-query"));
		await settleDebounce();

		await waitFor(() => expect(result.current.saveState).toBe("saved"));
		expect(saveSettings).toHaveBeenCalledTimes(1);
	});

	it("@BUG-5628 stays silent when the persisted settings validate", async () => {
		const validateSettings = vi.fn().mockResolvedValue(true);
		const args = makeArgs({ validateSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() =>
			result.current.updateSettings("dataRetrievalValue", "good-query"),
		);
		await settleDebounce();
		await waitFor(() => expect(result.current.saveState).toBe("saved"));
		await act(async () => {});

		expect(result.current.validationError).toBeNull();
		expect(result.current.validationTechnicalDetails).toBeNull();
	});

	it("@BUG-5628 probes the connector once per settled save, not once per keystroke", async () => {
		const validateSettings = vi.fn().mockResolvedValue(true);
		const args = makeArgs({ validateSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => {
			result.current.updateSettings("dataRetrievalValue", "m");
			result.current.updateSettings("dataRetrievalValue", "my");
			result.current.updateSettings("dataRetrievalValue", "my-");
			result.current.updateSettings("dataRetrievalValue", "my-q");
		});
		await settleDebounce();
		await waitFor(() => expect(result.current.saveState).toBe("saved"));

		await waitFor(() => expect(validateSettings).toHaveBeenCalledTimes(1));
	});

	it("@BUG-5628 does not re-probe when a later save leaves the connector-relevant fields untouched", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const validateSettings = vi.fn().mockResolvedValue(true);
		const args = makeArgs({ saveSettings, validateSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() =>
			result.current.updateSettings("dataRetrievalValue", "my-query-2"),
		);
		await settleDebounce();
		await waitFor(() => expect(result.current.saveState).toBe("saved"));
		await waitFor(() => expect(validateSettings).toHaveBeenCalledTimes(1));

		act(() => result.current.updateSettings("toDoStates", ["New", "Refined"]));
		await settleDebounce();
		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(2));

		act(() => result.current.updateSettings("name", "Atlas Delivery Squad"));
		await settleDebounce();
		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(3));

		await act(async () => {});
		expect(validateSettings).toHaveBeenCalledTimes(1);
	});

	it("@BUG-5628 never probes on the default-settings pages, which have no connector", async () => {
		const validateSettings = vi.fn().mockResolvedValue(true);
		const args = makeArgs({ validateSettings, modifyDefaultSettings: true });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "bad-query"));
		await settleDebounce();
		await waitFor(() => expect(result.current.saveState).toBe("saved"));
		await act(async () => {});

		expect(validateSettings).not.toHaveBeenCalled();
		expect(result.current.validationError).toBeNull();
	});

	it("@BUG-5628 @error carries the connector's own verdict through to the alert", async () => {
		const validateSettings = vi
			.fn()
			.mockRejectedValue(
				new ApiError(
					400,
					"No work items found for Work Item Type 'Storry'.",
					"Probe sysparm_query=... returned 0 records",
				),
			);
		const args = makeArgs({ validateSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("workItemTypes", ["Storry"]));
		await settleDebounce();
		await waitFor(() => expect(result.current.saveState).toBe("saved"));

		await waitFor(() =>
			expect(result.current.validationError).toBe(
				"No work items found for Work Item Type 'Storry'.",
			),
		);
		expect(result.current.validationTechnicalDetails).toBe(
			"Probe sysparm_query=... returned 0 records",
		);
	});

	it("@BUG-5628 clears the warning as soon as the coach starts correcting the settings", async () => {
		const validateSettings = vi.fn().mockResolvedValue(false);
		const args = makeArgs({ validateSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("workItemTypes", ["Storry"]));
		await settleDebounce();
		await waitFor(() => expect(result.current.saveState).toBe("saved"));

		act(() => result.current.updateSettings("workItemTypes", ["Story"]));

		expect(result.current.validationError).toBeNull();
		expect(result.current.validationTechnicalDetails).toBeNull();
	});
});
