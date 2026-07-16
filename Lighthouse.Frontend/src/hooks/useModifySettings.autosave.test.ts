import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../models/WorkTracking/WorkTrackingSystemConnection";
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

const makeArgs = (
	overrides: Partial<
		Parameters<typeof useModifySettings<SimpleSettings>>[0]
	> = {},
	autoSave?: AutoSaveOptions,
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
		.mockImplementation((s: SimpleSettings) => s.name !== ""),
	getSchemaForSystem: vi
		.fn()
		.mockReturnValue({ isRequired: true, isWorkItemTypesRequired: true }),
	autoSave: autoSave ?? {
		enabled: true,
		canSave: true,
		debounceMs: DEBOUNCE_MS,
	},
	...overrides,
});

const teamAdminCanSave: AutoSaveOptions = {
	enabled: true,
	canSave: true,
	debounceMs: DEBOUNCE_MS,
};

const teamAdminCanSaveAndRefresh: AutoSaveOptions = {
	enabled: true,
	canSave: true,
	debounceMs: DEBOUNCE_MS,
	refreshOnSave: true,
};

const viewerCannotSave: AutoSaveOptions = {
	enabled: true,
	canSave: false,
	debounceMs: DEBOUNCE_MS,
};

// The reporter's scenario needs two connections to switch BETWEEN; the shared
// makeArgs fixture only seeds one.
const twoTrackerConnections = () =>
	vi
		.fn()
		.mockResolvedValue([
			makeConnection(1, "Atlas Tracker"),
			makeConnection(2, "Borealis Tracker", "Jira"),
		]);

describe("@US-01 @in-memory auto-save general team settings", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-01 persists a valid edit automatically after the team-admin stops typing", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
	});

	it("@US-01 reports calm progress: idle, then saving, then all-changes-saved", async () => {
		const args = makeArgs({}, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		expect(result.current.saveState).toBe("idle");

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(result.current.saveState).toBe("saved"));
	});

	it("@US-01 @error holds back an invalid edit so the inline error is the only feedback", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("name", "" as never));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.formValid).toBe(false);
		expect(result.current.saveState).toBe("idle");
	});

	it("@US-01 @error keeps the edit and offers a retry when the save fails", async () => {
		const saveSettings = vi
			.fn()
			.mockRejectedValueOnce(new Error("server unavailable"))
			.mockResolvedValueOnce(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});
		await waitFor(() => expect(result.current.saveState).toBe("error"));
		expect(result.current.settings?.dataRetrievalValue).toBe("60");

		await act(async () => {
			result.current.retry();
		});
		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(2));
	});

	it("@US-01 @error never auto-saves for a viewer who lacks the right to save", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, viewerCannotSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.saveState).toBe("idle");
	});

	it("@US-01 @error persists only the latest value when edits arrive in rapid succession", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "40"));
		act(() => result.current.updateSettings("dataRetrievalValue", "50"));
		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		const calls = saveSettings.mock.calls;
		const lastSaved = calls[calls.length - 1][0] as SimpleSettings;
		expect(lastSaved.dataRetrievalValue).toBe("60");
	});

	it("@US-01 @error holds the next save until the in-flight one resolves, then lands on saved", async () => {
		let resolveFirst: (() => void) | undefined;
		const saveSettings = vi
			.fn()
			.mockImplementationOnce(
				() =>
					new Promise<void>((resolve) => {
						resolveFirst = resolve;
					}),
			)
			.mockResolvedValueOnce(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "50"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});
		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});
		expect(saveSettings).toHaveBeenCalledTimes(1);

		await act(async () => {
			resolveFirst?.();
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(2));
		await waitFor(() => expect(result.current.saveState).toBe("saved"));
	});

	it("@US-01 does not fire any save on the initial page load", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		await waitFor(() => expect(result.current.formValid).toBe(true));

		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});
});

describe("@US-02 @in-memory auto-save and auto-refresh state mappings", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-02 auto-saves a valid mapping change and refreshes the dependent metrics", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const refreshDependentData = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{ saveSettings, additionalFetch: refreshDependentData },
			teamAdminCanSaveAndRefresh,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		refreshDependentData.mockClear();

		act(() => result.current.doingHandlers.onAdd("Review"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		await waitFor(() => expect(refreshDependentData).toHaveBeenCalledTimes(1));
	});
});

describe("@US-02 @in-memory auto-save and auto-refresh state mappings (validity gate)", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-02 @error fires no save when a mapping group name is left empty", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const additionalFetch = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{
				saveSettings,
				additionalFetch,
				validateForm: vi.fn().mockReturnValue(false),
			},
			teamAdminCanSaveAndRefresh,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		additionalFetch.mockClear();

		act(() => result.current.doingHandlers.onAdd("Review"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(additionalFetch).not.toHaveBeenCalled();
		expect(result.current.formValid).toBe(false);
	});
});

describe("@US-02 @in-memory auto-save and auto-refresh state mappings (reload fallback)", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-02 @error offers a one-click reload when the automatic refresh fails", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const refreshDependentData = vi
			.fn()
			.mockResolvedValueOnce(undefined)
			.mockRejectedValueOnce(new Error("refresh failed"));
		const args = makeArgs(
			{ saveSettings, additionalFetch: refreshDependentData },
			teamAdminCanSaveAndRefresh,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		refreshDependentData.mockClear();

		act(() => result.current.doingHandlers.onAdd("Review"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(result.current.saveState).toBe("saved"));
		await waitFor(() => expect(result.current.refreshFailed).toBe(true));
	});
});

describe("@US-03 @in-memory auto-save forecast filter rules", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-03 auto-saves a valid filter rule and never triggers an automatic throughput recompute", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const recomputeThroughput = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{ saveSettings, additionalFetch: recomputeThroughput },
			teamAdminCanSave,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		recomputeThroughput.mockClear();

		act(() =>
			result.current.updateSettings(
				"forecastFilterRuleSetJson" as never,
				'{"rules":[{"field":"Type","op":"equals","value":"Bug","exclude":true}]}' as never,
			),
		);
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		expect(recomputeThroughput).not.toHaveBeenCalled();
	});
});

describe("@US-03 @in-memory auto-save forecast filter rules (pending)", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-03 @error rejects an unknown filter field without clobbering the existing rule set", async () => {
		const persistedRuleSet =
			'{"rules":[{"field":"Type","op":"equals","value":"Bug","exclude":true}]}';
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{
				saveSettings,
				getSettings: vi.fn().mockResolvedValue(
					atlasSettings({
						forecastFilterRuleSetJson: persistedRuleSet,
					} as Partial<SimpleSettings>),
				),
				validateForm: vi.fn().mockReturnValue(false),
			},
			teamAdminCanSave,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() =>
			result.current.updateSettings(
				"forecastFilterRuleSetJson" as never,
				'{"rules":[{"field":"UnknownKey"}]}' as never,
			),
		);
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.formValid).toBe(false);
	});

	it("@US-03 @error keeps the filter editor read-only with no auto-save for a viewer", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, viewerCannotSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() =>
			result.current.updateSettings(
				"forecastFilterRuleSetJson" as never,
				'{"rules":[{"field":"Type","value":"Bug"}]}' as never,
			),
		);
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});
});

describe("@US-02 @in-memory list-handler interaction marks the form as edited", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-02 auto-saves after a state is removed", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.doingHandlers.onRemove("Active"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
	});

	it("@US-02 auto-saves after a state list is reordered", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.doingHandlers.onReorder(["Active", "Review"]));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
	});
});

describe("@US-01 @in-memory retry and in-flight progress", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-01 retry does nothing before any save has happened", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		await act(async () => {
			result.current.retry();
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});

	it("@US-01 reports a transient saving state while a save is in flight", async () => {
		let resolveSave: (() => void) | undefined;
		const saveSettings = vi.fn().mockImplementation(
			() =>
				new Promise<void>((resolve) => {
					resolveSave = resolve;
				}),
		);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		expect(result.current.saveState).toBe("saving");

		await act(async () => {
			resolveSave?.();
		});
		await waitFor(() => expect(result.current.saveState).toBe("saved"));
	});
});

describe("@US-04 @in-memory auto-save portfolio settings", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-04 auto-saves a valid portfolio field the moment it is valid", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("name", "Atlas Program"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
	});

	it("@US-04 @error suppresses auto-save where portfolio data updates are not permitted", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, viewerCannotSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("name", "Atlas Program"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});

	it("@US-04 @error fires no save while the portfolio form is invalid", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{
				saveSettings,
				validateForm: vi.fn().mockReturnValue(false),
			},
			teamAdminCanSave,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("name", "" as never));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});
});

describe("@US-01 @in-memory switching the work tracking system connection", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("@US-01 auto-saves the newly picked connection when the work tracking system is the only change", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{ saveSettings, getWorkTrackingSystems: twoTrackerConnections() },
			teamAdminCanSave,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		await waitFor(() =>
			expect(result.current.selectedWorkTrackingSystem?.id).toBe(1),
		);

		act(() =>
			result.current.handleWorkTrackingSystemChange("Borealis Tracker"),
		);
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		const saved = saveSettings.mock.calls[0][0] as SimpleSettings;
		expect(saved.workTrackingSystemConnectionId).toBe(2);
	});

	it("@US-01 @error stays silent on mount and after a conflict reload re-selects the connection", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{ saveSettings, getWorkTrackingSystems: twoTrackerConnections() },
			teamAdminCanSave,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		await waitFor(() => expect(result.current.formValid).toBe(true));

		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});
		expect(saveSettings).not.toHaveBeenCalled();

		await act(async () => {
			await result.current.reloadAfterConflict();
		});
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.selectedWorkTrackingSystem?.id).toBe(1);
	});

	it("@US-01 @error ignores an unknown system name instead of auto-saving a missing connection", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{ saveSettings, getWorkTrackingSystems: twoTrackerConnections() },
			teamAdminCanSave,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		await waitFor(() =>
			expect(result.current.selectedWorkTrackingSystem?.id).toBe(1),
		);

		act(() =>
			result.current.handleWorkTrackingSystemChange("Nonexistent System"),
		);
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.selectedWorkTrackingSystem).toBeNull();
	});
});
