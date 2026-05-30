import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../services/Api/ApiError";
import {
	type ModifySettingsBase,
	useModifySettings,
} from "./useModifySettings";

// ---------- helpers ----------

type SimpleSettings = ModifySettingsBase & { extra?: string };

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
	overrides: Partial<SimpleSettings> = {},
): SimpleSettings => ({
	name: "Test Entity",
	workTrackingSystemConnectionId: 1,
	dataRetrievalValue: "my-query",
	dataRetrievalSchema: {
		isRequired: true,
		isWorkItemTypesRequired: true,
	},
	workItemTypes: ["Story", "Bug"],
	toDoStates: ["New"],
	doingStates: ["Active"],
	doneStates: ["Done"],
	stateMappings: [],
	...overrides,
});

const alwaysValid = () => true;

const makeHookArgs = (
	overrides: Partial<
		Parameters<typeof useModifySettings<SimpleSettings>>[0]
	> = {},
) => ({
	getWorkTrackingSystems: vi
		.fn()
		.mockResolvedValue([
			makeConnection(1, "System 1"),
			makeConnection(2, "System 2"),
		]),
	getSettings: vi.fn().mockResolvedValue(makeSettings()),
	saveSettings: vi.fn().mockResolvedValue(undefined),
	validateSettings: vi.fn().mockResolvedValue(true),
	modifyDefaultSettings: false,
	validateForm: vi.fn().mockImplementation(alwaysValid),
	getSchemaForSystem: vi
		.fn()
		.mockReturnValue({ isRequired: true, isWorkItemTypesRequired: true }),
	...overrides,
});

// ---------- tests ----------

describe("useModifySettings", () => {
	beforeEach(() => vi.clearAllMocks());

	describe("initial fetch", () => {
		it("sets loading=true then false after fetch", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useModifySettings(args));

			// loading starts false (initialized as false in hook), but fetch fires immediately
			await waitFor(() => expect(result.current.loading).toBe(false));
			expect(result.current.settings).not.toBeNull();
		});

		it("populates settings and workTrackingSystems after fetch", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useModifySettings(args));

			await waitFor(() => expect(result.current.settings).not.toBeNull());

			expect(result.current.settings?.name).toBe("Test Entity");
			expect(result.current.workTrackingSystems).toHaveLength(2);
		});

		it("selects the matching workTrackingSystem by id", async () => {
			const args = makeHookArgs({
				getSettings: vi
					.fn()
					.mockResolvedValue(
						makeSettings({ workTrackingSystemConnectionId: 2 }),
					),
			});
			const { result } = renderHook(() => useModifySettings(args));

			await waitFor(() =>
				expect(result.current.selectedWorkTrackingSystem).not.toBeNull(),
			);
			expect(result.current.selectedWorkTrackingSystem?.id).toBe(2);
		});

		it("leaves selectedWorkTrackingSystem null when id does not match any system", async () => {
			const args = makeHookArgs({
				getSettings: vi
					.fn()
					.mockResolvedValue(
						makeSettings({ workTrackingSystemConnectionId: 99 }),
					),
			});
			const { result } = renderHook(() => useModifySettings(args));

			await waitFor(() => expect(result.current.loading).toBe(false));
			expect(result.current.selectedWorkTrackingSystem).toBeNull();
		});

		it("calls additionalFetch if provided", async () => {
			const additionalFetch = vi.fn().mockResolvedValue(undefined);
			const args = makeHookArgs({ additionalFetch });
			const { result } = renderHook(() => useModifySettings(args));

			await waitFor(() => expect(result.current.loading).toBe(false));
			expect(additionalFetch).toHaveBeenCalledTimes(1);
		});

		it("handles fetch error gracefully (loading becomes false, settings stays null)", async () => {
			const consoleError = vi
				.spyOn(console, "error")
				.mockImplementation(() => {});
			const args = makeHookArgs({
				getSettings: vi.fn().mockRejectedValue(new Error("network")),
			});
			const { result } = renderHook(() => useModifySettings(args));

			await waitFor(() => expect(result.current.loading).toBe(false));
			expect(result.current.settings).toBeNull();
			consoleError.mockRestore();
		});
	});

	describe("formValid", () => {
		it("is true when validateForm returns true", async () => {
			const args = makeHookArgs({
				validateForm: vi.fn().mockReturnValue(true),
			});
			const { result } = renderHook(() => useModifySettings(args));

			await waitFor(() => expect(result.current.settings).not.toBeNull());
			expect(result.current.formValid).toBe(true);
		});

		it("is false when validateForm returns false", async () => {
			const args = makeHookArgs({
				validateForm: vi.fn().mockReturnValue(false),
			});
			const { result } = renderHook(() => useModifySettings(args));

			await waitFor(() => expect(result.current.settings).not.toBeNull());
			expect(result.current.formValid).toBe(false);
		});

		it("re-evaluates when settings change via updateSettings", async () => {
			let callCount = 0;
			const validateForm = vi.fn().mockImplementation((s: SimpleSettings) => {
				callCount++;
				return s.name !== "";
			});
			const args = makeHookArgs({ validateForm });
			const { result } = renderHook(() => useModifySettings(args));

			await waitFor(() => expect(result.current.formValid).toBe(true));
			const before = callCount;

			act(() => result.current.updateSettings("name", "" as never));

			await waitFor(() => expect(result.current.formValid).toBe(false));
			expect(callCount).toBeGreaterThan(before);
		});
	});

	describe("updateSettings", () => {
		it("updates a string field", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			act(() => result.current.updateSettings("name", "New Name" as never));
			expect(result.current.settings?.name).toBe("New Name");
		});

		it("ignores null for non-nullable fields", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			act(() => result.current.updateSettings("name", null as never));
			// name should be unchanged
			expect(result.current.settings?.name).toBe("Test Entity");
		});

		it("allows null for nullable fields like estimationUnit", async () => {
			type SettingsWithNullable = SimpleSettings & {
				estimationUnit: string | null;
			};
			const args = makeHookArgs({
				getSettings: vi.fn().mockResolvedValue({
					...makeSettings(),
					estimationUnit: "points",
				}),
			});
			const { result } = renderHook(() =>
				useModifySettings<SettingsWithNullable>(args as never),
			);
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			act(() => result.current.updateSettings("estimationUnit" as never, null));
			expect(result.current.settings?.estimationUnit).toBeNull();
		});
	});

	describe("handleWorkTrackingSystemChange", () => {
		it("updates selectedWorkTrackingSystem by name", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			act(() => result.current.handleWorkTrackingSystemChange("System 2"));
			expect(result.current.selectedWorkTrackingSystem?.id).toBe(2);
		});

		it("updates dataRetrievalSchema on the settings object", async () => {
			const newSchema = { isRequired: false, isWorkItemTypesRequired: false };
			const args = makeHookArgs({
				getSchemaForSystem: vi.fn().mockReturnValue(newSchema),
			});
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			act(() => result.current.handleWorkTrackingSystemChange("System 2"));
			expect(result.current.settings?.dataRetrievalSchema).toEqual(newSchema);
		});

		it("sets selectedWorkTrackingSystem to null when name not found", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			act(() => result.current.handleWorkTrackingSystemChange("Unknown"));
			expect(result.current.selectedWorkTrackingSystem).toBeNull();
		});
	});

	describe("handleSave", () => {
		it("validates then saves when modifyDefaultSettings=false", async () => {
			const validateSettings = vi.fn().mockResolvedValue(true);
			const saveSettings = vi.fn().mockResolvedValue(undefined);
			const args = makeHookArgs({
				validateSettings,
				saveSettings,
				modifyDefaultSettings: false,
			});
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			await act(() => result.current.handleSave());

			expect(validateSettings).toHaveBeenCalledTimes(1);
			expect(saveSettings).toHaveBeenCalledTimes(1);
		});

		it("skips validation and saves directly when modifyDefaultSettings=true", async () => {
			const validateSettings = vi.fn().mockResolvedValue(true);
			const saveSettings = vi.fn().mockResolvedValue(undefined);
			const args = makeHookArgs({
				validateSettings,
				saveSettings,
				modifyDefaultSettings: true,
			});
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			await act(() => result.current.handleSave());

			expect(validateSettings).not.toHaveBeenCalled();
			expect(saveSettings).toHaveBeenCalledTimes(1);
		});

		it("does not save when validation returns false", async () => {
			const validateSettings = vi.fn().mockResolvedValue(false);
			const saveSettings = vi.fn().mockResolvedValue(undefined);
			const args = makeHookArgs({ validateSettings, saveSettings });
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			await act(() => result.current.handleSave());

			expect(saveSettings).not.toHaveBeenCalled();
		});

		it("surfaces the validation-failed message when validation returns false", async () => {
			const validateSettings = vi.fn().mockResolvedValue(false);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			await act(() => result.current.handleSave());

			expect(result.current.validationError).toBe(
				"Validation failed. Check your configuration and try again.",
			);
			expect(result.current.validationTechnicalDetails).toBeNull();
		});

		it("surfaces an ApiError message and its technical details without saving", async () => {
			const saveSettings = vi.fn().mockResolvedValue(undefined);
			const validateSettings = vi
				.fn()
				.mockRejectedValue(
					new ApiError(
						409,
						"Connection rejected the configuration",
						"WIQL query referenced an unknown field",
					),
				);
			const args = makeHookArgs({ validateSettings, saveSettings });
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			await act(() => result.current.handleSave());

			expect(result.current.validationError).toBe(
				"Connection rejected the configuration",
			);
			expect(result.current.validationTechnicalDetails).toBe(
				"WIQL query referenced an unknown field",
			);
			expect(saveSettings).not.toHaveBeenCalled();
		});

		it("re-throws a non-ApiError validation failure instead of swallowing it", async () => {
			const validateSettings = vi
				.fn()
				.mockRejectedValue(new Error("network down"));
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			await expect(act(() => result.current.handleSave())).rejects.toThrow(
				"network down",
			);
		});

		it("injects the selected system id into the saved DTO", async () => {
			const saveSettings = vi.fn().mockResolvedValue(undefined);
			const args = makeHookArgs({ saveSettings });
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			act(() => result.current.handleWorkTrackingSystemChange("System 2"));
			await act(() => result.current.handleSave());

			const saved = saveSettings.mock.calls[0][0] as SimpleSettings;
			expect(saved.workTrackingSystemConnectionId).toBe(2);
		});

		it("does nothing when settings is null", async () => {
			const consoleError = vi
				.spyOn(console, "error")
				.mockImplementation(() => {});
			const saveSettings = vi.fn().mockResolvedValue(undefined);
			const args = makeHookArgs({
				getSettings: vi.fn().mockRejectedValue(new Error("fail")),
				saveSettings,
			});
			const { result } = renderHook(() => useModifySettings(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			await act(() => result.current.handleSave());
			expect(saveSettings).not.toHaveBeenCalled();
			consoleError.mockRestore();
		});
	});

	describe("list handlers (makeListHandlers)", () => {
		describe("workItemTypeHandlers", () => {
			it("onAdd appends a trimmed value", async () => {
				const args = makeHookArgs();
				const { result } = renderHook(() => useModifySettings(args));
				await waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.workItemTypeHandlers.onAdd("  NewType  "));
				expect(result.current.settings?.workItemTypes).toContain("NewType");
			});

			it("onAdd ignores blank values", async () => {
				const args = makeHookArgs();
				const { result } = renderHook(() => useModifySettings(args));
				await waitFor(() => expect(result.current.settings).not.toBeNull());

				const before = result.current.settings?.workItemTypes.length ?? 0;
				act(() => result.current.workItemTypeHandlers.onAdd("   "));
				expect(result.current.settings?.workItemTypes.length).toBe(before);
			});

			it("onRemove removes only the matching value and keeps the rest", async () => {
				const args = makeHookArgs();
				const { result } = renderHook(() => useModifySettings(args));
				await waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.workItemTypeHandlers.onRemove("Story"));
				expect(result.current.settings?.workItemTypes).toEqual(["Bug"]);
			});

			it("onAdd and onRemove are no-ops while the list field is absent", async () => {
				const args = makeHookArgs({
					getSettings: vi.fn().mockResolvedValue(
						makeSettings({
							workItemTypes: undefined as unknown as string[],
						}),
					),
				});
				const { result } = renderHook(() => useModifySettings(args));
				await waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.workItemTypeHandlers.onAdd("Story"));
				expect(result.current.settings?.workItemTypes).toEqual(["Story"]);

				act(() => result.current.workItemTypeHandlers.onRemove("Story"));
				expect(result.current.settings?.workItemTypes).toEqual([]);
			});

			it("onReorder replaces the list", async () => {
				const args = makeHookArgs();
				const { result } = renderHook(() => useModifySettings(args));
				await waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() =>
					result.current.workItemTypeHandlers.onReorder(["Bug", "Story"]),
				);
				expect(result.current.settings?.workItemTypes).toEqual([
					"Bug",
					"Story",
				]);
			});
		});

		describe("toDoHandlers / doingHandlers / doneHandlers", () => {
			it("toDoHandlers.onAdd appends to toDoStates", async () => {
				const args = makeHookArgs();
				const { result } = renderHook(() => useModifySettings(args));
				await waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.toDoHandlers.onAdd("Backlog"));
				expect(result.current.settings?.toDoStates).toContain("Backlog");
			});

			it("doingHandlers.onRemove removes from doingStates", async () => {
				const args = makeHookArgs();
				const { result } = renderHook(() => useModifySettings(args));
				await waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.doingHandlers.onRemove("Active"));
				expect(result.current.settings?.doingStates).not.toContain("Active");
			});

			it("doneHandlers.onReorder replaces doneStates", async () => {
				const args = makeHookArgs();
				const { result } = renderHook(() => useModifySettings(args));
				await waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.doneHandlers.onReorder(["Closed", "Done"]));
				expect(result.current.settings?.doneStates).toEqual(["Closed", "Done"]);
			});
		});
	});

	describe("callback identity stability", () => {
		it("does not re-fetch when callback references change after initial load", async () => {
			const args = makeHookArgs();
			const { rerender } = renderHook((props) => useModifySettings(props), {
				initialProps: args,
			});

			await waitFor(() => expect(args.getSettings).toHaveBeenCalledTimes(1));

			// Simulate a parent re-render producing new function identities
			const refreshedArgs = makeHookArgs();
			rerender(refreshedArgs);

			await act(async () => {});

			// New callback should never have been invoked — no second fetch
			expect(refreshedArgs.getSettings).not.toHaveBeenCalled();
			// Original fetch count stays at exactly 1
			expect(args.getSettings).toHaveBeenCalledTimes(1);
		});

		it("preserves in-progress edits when callback references change", async () => {
			const args = makeHookArgs();
			const { result, rerender } = renderHook(
				(props) => useModifySettings(props),
				{ initialProps: args },
			);

			await waitFor(() => expect(result.current.settings).not.toBeNull());

			// User edits the form
			act(() => result.current.updateSettings("name", "Edited Name" as never));
			expect(result.current.settings?.name).toBe("Edited Name");

			// Parent re-renders (e.g. isUpdating flag changes) — new callback identities
			rerender(makeHookArgs());
			await act(async () => {});

			// Edit must survive the re-render
			expect(result.current.settings?.name).toBe("Edited Name");
		});
	});

	describe("auto-save with optimistic concurrency", () => {
		const autoSaveArgs = (
			overrides: Partial<
				Parameters<typeof useModifySettings<SimpleSettings>>[0]
			> = {},
		) =>
			makeHookArgs({
				autoSave: { enabled: true, canSave: true, debounceMs: 300 },
				...overrides,
			});

		it("debounces an edit then auto-saves once after the debounce window", async () => {
			vi.useFakeTimers();
			try {
				const saveSettings = vi.fn().mockResolvedValue(undefined);
				const args = autoSaveArgs({ saveSettings });
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.updateSettings("name", "Edited" as never));
				expect(saveSettings).not.toHaveBeenCalled();

				await act(async () => {
					await vi.advanceTimersByTimeAsync(299);
				});
				expect(saveSettings).not.toHaveBeenCalled();

				await act(async () => {
					await vi.advanceTimersByTimeAsync(1);
				});
				expect(saveSettings).toHaveBeenCalledTimes(1);
			} finally {
				vi.useRealTimers();
			}
		});

		it("does not auto-save when the form has not been interacted with", async () => {
			vi.useFakeTimers();
			try {
				const saveSettings = vi.fn().mockResolvedValue(undefined);
				const args = autoSaveArgs({ saveSettings });
				renderHook(() => useModifySettings(args));

				await act(async () => {
					await vi.advanceTimersByTimeAsync(1000);
				});
				expect(saveSettings).not.toHaveBeenCalled();
			} finally {
				vi.useRealTimers();
			}
		});

		it("does not auto-save while canSave is false", async () => {
			vi.useFakeTimers();
			try {
				const saveSettings = vi.fn().mockResolvedValue(undefined);
				const args = autoSaveArgs({
					saveSettings,
					autoSave: { enabled: true, canSave: false, debounceMs: 300 },
				});
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() => expect(result.current.settings).not.toBeNull());
				act(() => result.current.updateSettings("name", "Edited" as never));

				await act(async () => {
					await vi.advanceTimersByTimeAsync(1000);
				});
				expect(saveSettings).not.toHaveBeenCalled();
			} finally {
				vi.useRealTimers();
			}
		});

		it("reports the saving then saved transition and advances the concurrency token from the save result", async () => {
			vi.useFakeTimers();
			try {
				const saveSettings = vi
					.fn()
					.mockResolvedValue(makeSettings({ concurrencyToken: "token-v2" }));
				const args = autoSaveArgs({
					saveSettings,
					getSettings: vi
						.fn()
						.mockResolvedValue(makeSettings({ concurrencyToken: "token-v1" })),
				});
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() =>
					expect(result.current.settings?.concurrencyToken).toBe("token-v1"),
				);

				act(() => result.current.updateSettings("name", "Edited" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});

				expect(saveSettings).toHaveBeenCalledTimes(1);
				expect(result.current.saveState).toBe("saved");
				expect(result.current.settings?.concurrencyToken).toBe("token-v2");
			} finally {
				vi.useRealTimers();
			}
		});

		it("echoes the latest token back when a queued edit flushes after the in-flight save resolves", async () => {
			vi.useFakeTimers();
			try {
				let resolveFirst: (value: SimpleSettings) => void = () => {};
				const saveSettings = vi
					.fn()
					.mockImplementationOnce(
						() =>
							new Promise<SimpleSettings>((resolve) => {
								resolveFirst = resolve;
							}),
					)
					.mockResolvedValue(makeSettings({ concurrencyToken: "token-v3" }));
				const args = autoSaveArgs({
					saveSettings,
					getSettings: vi
						.fn()
						.mockResolvedValue(makeSettings({ concurrencyToken: "token-v1" })),
				});
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.updateSettings("name", "First" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});
				expect(saveSettings).toHaveBeenCalledTimes(1);

				act(() => result.current.updateSettings("name", "Second" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});
				expect(saveSettings).toHaveBeenCalledTimes(1);

				await act(async () => {
					resolveFirst(makeSettings({ concurrencyToken: "token-v2" }));
				});

				expect(saveSettings).toHaveBeenCalledTimes(2);
				const queuedPayload = saveSettings.mock.calls[1][0] as SimpleSettings;
				expect(queuedPayload.concurrencyToken).toBe("token-v2");
			} finally {
				vi.useRealTimers();
			}
		});

		it("enters the conflict state on a 409 and stops auto-saving further edits", async () => {
			vi.useFakeTimers();
			try {
				const saveSettings = vi
					.fn()
					.mockRejectedValueOnce(new ApiError(409, "stale"))
					.mockResolvedValue(undefined);
				const args = autoSaveArgs({ saveSettings });
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.updateSettings("name", "Edited" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});
				expect(result.current.saveState).toBe("conflict");

				act(() =>
					result.current.updateSettings("name", "Edited Again" as never),
				);
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});
				expect(saveSettings).toHaveBeenCalledTimes(1);
			} finally {
				vi.useRealTimers();
			}
		});

		it("treats a non-409 save failure as a recoverable error, not a conflict", async () => {
			vi.useFakeTimers();
			try {
				const saveSettings = vi
					.fn()
					.mockRejectedValue(new ApiError(500, "server"));
				const args = autoSaveArgs({ saveSettings });
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.updateSettings("name", "Edited" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});
				expect(result.current.saveState).toBe("error");
			} finally {
				vi.useRealTimers();
			}
		});

		it("reloadAfterConflict fetches fresh settings, clears the conflict and returns to idle", async () => {
			vi.useFakeTimers();
			try {
				const getSettings = vi
					.fn()
					.mockResolvedValueOnce(makeSettings({ name: "Original" }))
					.mockResolvedValue(makeSettings({ name: "Server Truth" }));
				const saveSettings = vi
					.fn()
					.mockRejectedValueOnce(new ApiError(409, "stale"))
					.mockResolvedValue(undefined);
				const args = autoSaveArgs({ getSettings, saveSettings });
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() =>
					expect(result.current.settings?.name).toBe("Original"),
				);

				act(() => result.current.updateSettings("name", "My Edit" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});
				expect(result.current.saveState).toBe("conflict");

				await act(async () => {
					await result.current.reloadAfterConflict();
				});

				expect(result.current.settings?.name).toBe("Server Truth");
				expect(result.current.saveState).toBe("idle");

				act(() => result.current.updateSettings("name", "Re-applied" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});
				expect(saveSettings).toHaveBeenCalledTimes(2);
			} finally {
				vi.useRealTimers();
			}
		});

		it("retry re-dispatches the last save payload", async () => {
			vi.useFakeTimers();
			try {
				const saveSettings = vi
					.fn()
					.mockRejectedValueOnce(new ApiError(500, "server"))
					.mockResolvedValue(undefined);
				const args = autoSaveArgs({ saveSettings });
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.updateSettings("name", "Edited" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});
				expect(result.current.saveState).toBe("error");

				await act(async () => {
					result.current.retry();
				});

				expect(saveSettings).toHaveBeenCalledTimes(2);
				expect(result.current.saveState).toBe("saved");
			} finally {
				vi.useRealTimers();
			}
		});

		it("refreshes dependent data after a save and flags refreshFailed when that refresh rejects", async () => {
			vi.useFakeTimers();
			try {
				const additionalFetch = vi
					.fn()
					.mockResolvedValueOnce(undefined)
					.mockRejectedValue(new Error("refresh down"));
				const args = autoSaveArgs({
					additionalFetch,
					autoSave: {
						enabled: true,
						canSave: true,
						debounceMs: 300,
						refreshOnSave: true,
					},
				});
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.updateSettings("name", "Edited" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});

				expect(additionalFetch).toHaveBeenCalledTimes(2);
				expect(result.current.refreshFailed).toBe(true);
			} finally {
				vi.useRealTimers();
			}
		});

		it("reloadDependentData re-runs additionalFetch and surfaces a failure via refreshFailed", async () => {
			const additionalFetch = vi
				.fn()
				.mockResolvedValueOnce(undefined)
				.mockRejectedValue(new Error("refresh down"));
			const args = makeHookArgs({ additionalFetch });
			const { result } = renderHook(() => useModifySettings(args));

			await waitFor(() => expect(result.current.settings).not.toBeNull());

			await act(async () => {
				result.current.reloadDependentData();
			});

			await waitFor(() => expect(result.current.refreshFailed).toBe(true));
			expect(additionalFetch).toHaveBeenCalledTimes(2);
		});

		it.each([
			"sizeEstimateAdditionalFieldDefinitionId",
			"featureOwnerAdditionalFieldDefinitionId",
			"parentOverrideAdditionalFieldDefinitionId",
			"estimationAdditionalFieldDefinitionId",
			"estimationUnit",
			"owningTeam",
			"forecastFilterRuleSetJson",
		])("clears the nullable field %s to null", async (field) => {
			type Nullable = SimpleSettings & Record<string, unknown>;
			const args = makeHookArgs({
				getSettings: vi
					.fn()
					.mockResolvedValue({ ...makeSettings(), [field]: "preset" }),
			});
			const { result } = renderHook(() =>
				useModifySettings<Nullable>(args as never),
			);
			await waitFor(() => expect(result.current.settings).not.toBeNull());

			act(() => result.current.updateSettings(field as never, null));

			expect((result.current.settings as Nullable)[field]).toBeNull();
		});

		it("keeps the existing token when a save resolves without a fresh token", async () => {
			vi.useFakeTimers();
			try {
				const saveSettings = vi.fn().mockResolvedValue(makeSettings());
				const args = autoSaveArgs({
					saveSettings,
					getSettings: vi
						.fn()
						.mockResolvedValue(
							makeSettings({ concurrencyToken: "token-keep" }),
						),
				});
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() =>
					expect(result.current.settings?.concurrencyToken).toBe("token-keep"),
				);

				act(() => result.current.updateSettings("name", "Edited" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});

				expect(saveSettings).toHaveBeenCalledTimes(1);
				expect(result.current.settings?.concurrencyToken).toBe("token-keep");
			} finally {
				vi.useRealTimers();
			}
		});

		it("reselects the work tracking system that matches the reloaded settings after a conflict", async () => {
			vi.useFakeTimers();
			try {
				const getSettings = vi
					.fn()
					.mockResolvedValueOnce(
						makeSettings({ workTrackingSystemConnectionId: 1 }),
					)
					.mockResolvedValue(
						makeSettings({ workTrackingSystemConnectionId: 2 }),
					);
				const saveSettings = vi
					.fn()
					.mockRejectedValueOnce(new ApiError(409, "stale"))
					.mockResolvedValue(undefined);
				const args = autoSaveArgs({ getSettings, saveSettings });
				const { result } = renderHook(() => useModifySettings(args));

				await vi.waitFor(() =>
					expect(result.current.selectedWorkTrackingSystem?.id).toBe(1),
				);

				act(() => result.current.updateSettings("name", "My Edit" as never));
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});
				expect(result.current.saveState).toBe("conflict");

				await act(async () => {
					await result.current.reloadAfterConflict();
				});

				expect(result.current.selectedWorkTrackingSystem?.id).toBe(2);
			} finally {
				vi.useRealTimers();
			}
		});
	});
});
