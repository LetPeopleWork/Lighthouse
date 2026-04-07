import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../models/WorkTracking/WorkTrackingSystemConnection";
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

			it("onRemove removes a matching value", async () => {
				const args = makeHookArgs();
				const { result } = renderHook(() => useModifySettings(args));
				await waitFor(() => expect(result.current.settings).not.toBeNull());

				act(() => result.current.workItemTypeHandlers.onRemove("Story"));
				expect(result.current.settings?.workItemTypes).not.toContain("Story");
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
});
