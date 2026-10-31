import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../models/WorkTracking/WorkTrackingSystemConnection";
import { actAndSettle } from "../tests/actAndSettle";
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
		.mockImplementation((s: SimpleSettings) =>
			s.name === "" ? ["Enter a Name"] : [],
		),
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

describe("auto-save general team settings", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("persists a valid edit automatically after the team-admin stops typing", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
	});

	it("reports calm progress: idle, then saving, then all-changes-saved", async () => {
		const args = makeArgs({}, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		expect(result.current.saveState).toBe("idle");

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(result.current.saveState).toBe("saved"));
	});

	it("holds back an invalid edit so the inline error is the only feedback", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("name", "" as never));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.formValid).toBe(false);
		expect(result.current.saveState).toBe("idle");
	});

	it("keeps the edit and offers a retry when the save fails", async () => {
		const saveSettings = vi
			.fn()
			.mockRejectedValueOnce(new Error("server unavailable"))
			.mockResolvedValueOnce(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});
		await waitFor(() => expect(result.current.saveState).toBe("error"));
		expect(result.current.settings?.dataRetrievalValue).toBe("60");

		await actAndSettle(() => {
			result.current.retry();
		});
		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(2));
	});

	it("never auto-saves for a viewer who lacks the right to save", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, viewerCannotSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.saveState).toBe("idle");
	});

	it("persists only the latest value when edits arrive in rapid succession", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "40"));
		act(() => result.current.updateSettings("dataRetrievalValue", "50"));
		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		const calls = saveSettings.mock.calls;
		const lastSaved = calls[calls.length - 1][0] as SimpleSettings;
		expect(lastSaved.dataRetrievalValue).toBe("60");
	});

	it("holds the next save until the in-flight one resolves, then lands on saved", async () => {
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
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});
		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});
		expect(saveSettings).toHaveBeenCalledTimes(1);

		await actAndSettle(() => {
			resolveFirst?.();
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(2));
		await waitFor(() => expect(result.current.saveState).toBe("saved"));
	});

	it("does not fire any save on the initial page load", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		await waitFor(() => expect(result.current.formValid).toBe(true));

		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});
});

describe("auto-save and auto-refresh state mappings", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("auto-saves a valid mapping change and refreshes the dependent metrics", async () => {
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
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		await waitFor(() => expect(refreshDependentData).toHaveBeenCalledTimes(1));
	});
});

describe("auto-save and auto-refresh state mappings (validity gate)", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("fires no save when a mapping group name is left empty", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const additionalFetch = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{
				saveSettings,
				additionalFetch,
				validateForm: vi.fn().mockReturnValue(["Enter a Name"]),
			},
			teamAdminCanSaveAndRefresh,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		additionalFetch.mockClear();

		act(() => result.current.doingHandlers.onAdd("Review"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(additionalFetch).not.toHaveBeenCalled();
		expect(result.current.formValid).toBe(false);
	});
});

describe("auto-save skips a payload that has not changed", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("sends no save and no refresh when only an empty mapping is added", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const additionalFetch = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{ saveSettings, additionalFetch },
			teamAdminCanSaveAndRefresh,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		additionalFetch.mockClear();

		act(() =>
			result.current.updateSettings("stateMappings", [
				{ name: "", states: [] },
			]),
		);
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(additionalFetch).not.toHaveBeenCalled();
	});

	it("sends one save once the added mapping is filled in", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() =>
			result.current.updateSettings("stateMappings", [
				{ name: "", states: [] },
			]),
		);
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});
		act(() =>
			result.current.updateSettings("stateMappings", [
				{ name: "Review", states: ["QA"] },
			]),
		);
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		expect(saveSettings).toHaveBeenCalledWith(
			expect.objectContaining({
				stateMappings: [{ name: "Review", states: ["QA"] }],
			}),
		);
	});

	it("saves again when a value goes back to what was loaded after a later save", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});
		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		await waitFor(() => expect(result.current.saveState).toBe("saved"));

		act(() => result.current.updateSettings("dataRetrievalValue", "my-query"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(2));
	});
});

describe("auto-save and auto-refresh state mappings (reload fallback)", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("offers a one-click reload when the automatic refresh fails", async () => {
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
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(result.current.saveState).toBe("saved"));
		await waitFor(() => expect(result.current.refreshFailed).toBe(true));
	});
});

describe("auto-save forecast filter rules", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("auto-saves a valid filter rule and never triggers an automatic throughput recompute", async () => {
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
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		expect(recomputeThroughput).not.toHaveBeenCalled();
	});
});

describe("auto-save forecast filter rules (pending)", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("rejects an unknown filter field without clobbering the existing rule set", async () => {
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
				validateForm: vi.fn().mockReturnValue(["Enter a Name"]),
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
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.formValid).toBe(false);
	});

	it("keeps the filter editor read-only with no auto-save for a viewer", async () => {
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
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});
});

describe("list-handler interaction marks the form as edited", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("auto-saves after a state is removed", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.doingHandlers.onRemove("Active"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
	});

	it("auto-saves after a state list is reordered", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.doingHandlers.onReorder(["Active", "Review"]));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
	});
});

describe("retry and in-flight progress", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("retry does nothing before any save has happened", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		await actAndSettle(() => {
			result.current.retry();
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});

	it("reports a transient saving state while a save is in flight", async () => {
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
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		expect(result.current.saveState).toBe("saving");

		await actAndSettle(() => {
			resolveSave?.();
		});
		await waitFor(() => expect(result.current.saveState).toBe("saved"));
	});
});

describe("auto-save portfolio settings", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("auto-saves a valid portfolio field the moment it is valid", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, teamAdminCanSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("name", "Atlas Program"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
	});

	it("suppresses auto-save where portfolio data updates are not permitted", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({ saveSettings }, viewerCannotSave);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("name", "Atlas Program"));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});

	it("fires no save while the portfolio form is invalid", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{
				saveSettings,
				validateForm: vi.fn().mockReturnValue(["Enter a Name"]),
			},
			teamAdminCanSave,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("name", "" as never));
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(saveSettings).not.toHaveBeenCalled();
	});
});

describe("switching the work tracking system connection", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("auto-saves the newly picked connection when the work tracking system is the only change", async () => {
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
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		const saved = saveSettings.mock.calls[0][0] as SimpleSettings;
		expect(saved.workTrackingSystemConnectionId).toBe(2);
	});

	it("stays silent on mount and after a conflict reload re-selects the connection", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs(
			{ saveSettings, getWorkTrackingSystems: twoTrackerConnections() },
			teamAdminCanSave,
		);
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());
		await waitFor(() => expect(result.current.formValid).toBe(true));

		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});
		expect(saveSettings).not.toHaveBeenCalled();

		await act(async () => {
			await result.current.reloadAfterConflict();
		});
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.selectedWorkTrackingSystem?.id).toBe(1);
	});

	it("ignores an unknown system name instead of auto-saving a missing connection", async () => {
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
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(saveSettings).not.toHaveBeenCalled();
		expect(result.current.selectedWorkTrackingSystem).toBeNull();
	});
});

describe("saving while a state mapping is still being filled in", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	const complete = { name: "In Review", states: ["Review", "QA"] };
	const another = { name: "Blocked", states: ["Waiting"] };

	const loadedHook = async (
		saveSettings = vi.fn().mockResolvedValue(undefined),
		validateSettings = vi.fn().mockResolvedValue(true),
	) => {
		const args = makeArgs(
			{
				saveSettings,
				validateSettings,
				getSettings: vi
					.fn()
					.mockResolvedValue(atlasSettings({ stateMappings: [complete] })),
			},
			teamAdminCanSave,
		);
		const rendered = renderHook(() => useModifySettings(args));
		await waitFor(() =>
			expect(rendered.result.current.settings).not.toBeNull(),
		);
		return { ...rendered, args, saveSettings };
	};

	it("autosaves without the empty mapping and keeps the row in the editor", async () => {
		const { result, saveSettings } = await loadedHook();
		const draft = { name: "", states: [] };

		act(() =>
			result.current.updateSettings("stateMappings", [
				complete,
				another,
				draft,
			]),
		);
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		expect(saveSettings.mock.calls[0][0].stateMappings).toEqual([
			complete,
			another,
		]);
		await waitFor(() => expect(result.current.saveState).toBe("saved"));
		expect(result.current.settings?.stateMappings).toEqual([
			complete,
			another,
			draft,
		]);
	});

	it.each([
		["a name but no states", { name: "In", states: [] }],
		["states but a blank name", { name: "", states: ["X"] }],
		["states but a whitespace-only name", { name: "   ", states: ["X"] }],
	])(
		"leaves out a mapping with %s, keeping the others in order",
		async (_label, draft) => {
			const { result, saveSettings } = await loadedHook();

			act(() =>
				result.current.updateSettings("stateMappings", [
					complete,
					draft,
					another,
				]),
			);
			await actAndSettle(() => {
				vi.advanceTimersByTime(DEBOUNCE_MS);
			});

			await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
			expect(saveSettings.mock.calls[0][0].stateMappings).toEqual([
				complete,
				another,
			]);
		},
	);

	it("sends a complete mapping unchanged", async () => {
		const { result, saveSettings } = await loadedHook();

		act(() =>
			result.current.updateSettings("stateMappings", [complete, another]),
		);
		await actAndSettle(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		expect(saveSettings.mock.calls[0][0].stateMappings).toEqual([
			complete,
			another,
		]);
	});

	it("flushes the unsent autosave without the empty mapping when the page is left", async () => {
		const { result, unmount, saveSettings } = await loadedHook();

		act(() =>
			result.current.updateSettings("stateMappings", [
				complete,
				another,
				{ name: "", states: [] },
			]),
		);
		unmount();

		expect(saveSettings).toHaveBeenCalledTimes(1);
		expect(saveSettings.mock.calls[0][0].stateMappings).toEqual([
			complete,
			another,
		]);
	});

	it("saves and validates manually without the empty mapping", async () => {
		const validateSettings = vi.fn().mockResolvedValue(true);
		const { result, saveSettings } = await loadedHook(
			undefined,
			validateSettings,
		);

		act(() =>
			result.current.updateSettings("stateMappings", [
				complete,
				{ name: "", states: [] },
			]),
		);
		await act(() => result.current.handleSave());

		expect(validateSettings.mock.calls[0][0].stateMappings).toEqual([complete]);
		expect(
			saveSettings.mock.calls[saveSettings.mock.calls.length - 1][0]
				.stateMappings,
		).toEqual([complete]);
	});
});
