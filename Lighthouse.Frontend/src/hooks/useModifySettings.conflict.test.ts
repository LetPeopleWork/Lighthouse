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

type TokenedSettings = ModifySettingsBase & { concurrencyToken?: string };

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

const makeSettings = (
	overrides: Partial<TokenedSettings> = {},
): TokenedSettings => ({
	name: "Atlas Delivery Team",
	workTrackingSystemConnectionId: 1,
	dataRetrievalValue: "my-query",
	dataRetrievalSchema: { isRequired: true, isWorkItemTypesRequired: true },
	workItemTypes: ["Story", "Bug"],
	toDoStates: ["New"],
	doingStates: ["Active"],
	doneStates: ["Done"],
	stateMappings: [],
	concurrencyToken: "token-1",
	...overrides,
});

const teamAdminCanSave: AutoSaveOptions = {
	enabled: true,
	canSave: true,
	debounceMs: DEBOUNCE_MS,
};

const makeArgs = (
	overrides: Partial<
		Parameters<typeof useModifySettings<TokenedSettings>>[0]
	> = {},
) => ({
	getWorkTrackingSystems: vi
		.fn()
		.mockResolvedValue([makeConnection(1, "Atlas Tracker")]),
	getSettings: vi.fn().mockResolvedValue(makeSettings()),
	saveSettings: vi.fn().mockResolvedValue(undefined),
	validateSettings: vi.fn().mockResolvedValue(true),
	modifyDefaultSettings: false,
	validateForm: vi
		.fn()
		.mockImplementation((s: TokenedSettings) => s.name !== ""),
	getSchemaForSystem: vi
		.fn()
		.mockReturnValue({ isRequired: true, isWorkItemTypesRequired: true }),
	autoSave: teamAdminCanSave,
	...overrides,
});

const conflict = () =>
	new ApiError(409, "This record was modified by someone else.");

describe("@conflict @in-memory optimistic concurrency on settings auto-save", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		vi.clearAllMocks();
	});
	afterEach(() => {
		vi.runOnlyPendingTimers();
		vi.useRealTimers();
	});

	it("carries the concurrency token loaded from the server back out on save", async () => {
		const saveSettings = vi.fn().mockResolvedValue(undefined);
		const args = makeArgs({
			saveSettings,
			getSettings: vi
				.fn()
				.mockResolvedValue(
					makeSettings({ concurrencyToken: "token-from-server" }),
				),
		});
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		const saved = saveSettings.mock.calls[0][0] as TokenedSettings;
		expect(saved.concurrencyToken).toBe("token-from-server");
	});

	it("surfaces a distinct conflict state instead of the generic error when the save returns 409", async () => {
		const saveSettings = vi.fn().mockRejectedValue(conflict());
		const args = makeArgs({ saveSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(result.current.saveState).toBe("conflict"));
		expect(result.current.saveState).not.toBe("error");
	});

	it("does not auto-save again after a conflict until the user reloads", async () => {
		const saveSettings = vi.fn().mockRejectedValue(conflict());
		const args = makeArgs({ saveSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});
		await waitFor(() => expect(result.current.saveState).toBe("conflict"));

		act(() => result.current.updateSettings("dataRetrievalValue", "70"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(saveSettings).toHaveBeenCalledTimes(1);
	});

	it("re-fetches the latest server values and token when the user reloads after a conflict", async () => {
		const saveSettings = vi.fn().mockRejectedValue(conflict());
		const getSettings = vi
			.fn()
			.mockResolvedValueOnce(makeSettings({ concurrencyToken: "stale" }))
			.mockResolvedValueOnce(
				makeSettings({
					dataRetrievalValue: "server-value",
					concurrencyToken: "fresh",
				}),
			);
		const args = makeArgs({ saveSettings, getSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "edited"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});
		await waitFor(() => expect(result.current.saveState).toBe("conflict"));

		await act(async () => {
			await result.current.reloadAfterConflict();
		});

		await waitFor(() =>
			expect(result.current.settings?.dataRetrievalValue).toBe("server-value"),
		);
		expect(result.current.settings?.concurrencyToken).toBe("fresh");
		expect(result.current.saveState).not.toBe("conflict");
	});

	it("keeps the generic error path for non-409 failures", async () => {
		const saveSettings = vi.fn().mockRejectedValue(new ApiError(500, "boom"));
		const args = makeArgs({ saveSettings });
		const { result } = renderHook(() => useModifySettings(args));
		await waitFor(() => expect(result.current.settings).not.toBeNull());

		act(() => result.current.updateSettings("dataRetrievalValue", "60"));
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() => expect(result.current.saveState).toBe("error"));
	});
});
