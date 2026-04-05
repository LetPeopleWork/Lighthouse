import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getWizardsForSystem } from "../components/DataRetrievalWizards";
import type { IBoardInformation } from "../models/Boards/BoardInformation";
import type { IDataRetrievalSchema } from "../models/Common/DataRetrievalSchema";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../models/WorkTracking/WorkTrackingSystemConnection";
import {
	STEP_CHOOSE_CONNECTION,
	STEP_CONFIGURE,
	STEP_LOAD_DATA,
	STEP_NAME_CREATE,
	useCreateWizard,
	type WizardDtoBase,
} from "./useCreateWizard";

vi.mock("../components/DataRetrievalWizards", () => ({
	getWizardsForSystem: vi.fn().mockReturnValue([]),
}));
const mockGetWizardsForSystem = vi.mocked(getWizardsForSystem);

// ---------- helpers ----------

const makeConnection = (
	id: number,
	workTrackingSystem: WorkTrackingSystemType = "AzureDevOps",
): IWorkTrackingSystemConnection => ({
	id,
	name: `Connection ${id}`,
	workTrackingSystem,
	options: [],
	availableAuthenticationMethods: [],
	authenticationMethodKey: "ado.pat",
	workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL Query",
	additionalFieldDefinitions: [],
	writeBackMappingDefinitions: [],
});

const adoSchema: IDataRetrievalSchema = {
	key: "ado.wiql",
	displayLabel: "WIQL Query",
	inputKind: "freetext",
	isRequired: true,
	isWorkItemTypesRequired: true,
	wizardHint: null,
};

const linearSchema: IDataRetrievalSchema = {
	key: "linear.team",
	displayLabel: "Linear Team",
	inputKind: "none",
	isRequired: false,
	isWorkItemTypesRequired: false,
	wizardHint: null,
};

const fullBoardInfo: IBoardInformation = {
	dataRetrievalValue: "SELECT * FROM WorkItems",
	workItemTypes: ["Story", "Bug"],
	toDoStates: ["New"],
	doingStates: ["Active"],
	doneStates: ["Closed"],
};

const emptyBoardInfo: IBoardInformation = {
	dataRetrievalValue: "",
	workItemTypes: [],
	toDoStates: [],
	doingStates: [],
	doneStates: [],
};

type SimpleDto = WizardDtoBase & { name: string };

const buildDto = (base: WizardDtoBase, name: string): SimpleDto => ({
	...base,
	name,
});

const makeHookArgs = (
	overrides: Partial<Parameters<typeof useCreateWizard<SimpleDto>>[0]> = {},
) => ({
	entityType: "team" as const,
	defaultName: "New Team",
	getConnections: vi
		.fn()
		.mockResolvedValue([makeConnection(1), makeConnection(2)]),
	getSchema: vi.fn().mockReturnValue(adoSchema),
	buildDto,
	validateSettings: vi.fn().mockResolvedValue(true),
	saveSettings: vi.fn().mockResolvedValue(undefined),
	...overrides,
});

// ---------- tests ----------

describe("useCreateWizard", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mockGetWizardsForSystem.mockReturnValue([]);
	});

	describe("initial state", () => {
		it("starts on STEP_CHOOSE_CONNECTION with loading=true then loading=false", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));

			expect(result.current.loading).toBe(true);
			expect(result.current.activeStep).toBe(STEP_CHOOSE_CONNECTION);

			await waitFor(() => expect(result.current.loading).toBe(false));
			expect(result.current.connections).toHaveLength(2);
		});

		it("initialises name to defaultName", async () => {
			const args = makeHookArgs({ defaultName: "My Entity" });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));
			expect(result.current.name).toBe("My Entity");
		});

		it("has empty config state by default", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));
			expect(result.current.dataRetrievalValue).toBe("");
			expect(result.current.workItemTypes).toEqual([]);
			expect(result.current.toDoStates).toEqual([]);
			expect(result.current.doingStates).toEqual([]);
			expect(result.current.doneStates).toEqual([]);
		});
	});

	describe("selectConnection", () => {
		it("advances to STEP_LOAD_DATA when wizards are available", async () => {
			mockGetWizardsForSystem.mockReturnValue([
				{
					id: "w1",
					name: "Wizard",
					applicableSystemTypes: [],
					applicableSettingsContexts: [],
					component: () =>
						React.createElement("div", {
							"data-testid": "mock-wizard-component",
						}),
				},
			]);
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));

			expect(result.current.activeStep).toBe(STEP_LOAD_DATA);
			expect(result.current.availableWizards).toHaveLength(1);
		});

		it("skips to STEP_CONFIGURE when no wizards are available", async () => {
			mockGetWizardsForSystem.mockReturnValue([]);
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));

			expect(result.current.activeStep).toBe(STEP_CONFIGURE);
		});

		it("resets all config state on connection change", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			// Set some state first
			act(() => result.current.setDataRetrievalValue("old value"));
			act(() => result.current.setWorkItemTypes(["OldType"]));
			act(() => result.current.setToDoStates(["OldState"]));

			// Select a new connection — should reset everything
			act(() => result.current.selectConnection(makeConnection(2)));

			expect(result.current.dataRetrievalValue).toBe("");
			expect(result.current.workItemTypes).toEqual([]);
			expect(result.current.toDoStates).toEqual([]);
		});

		it("sets the selected connection", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			const conn = makeConnection(42);
			act(() => result.current.selectConnection(conn));

			expect(result.current.selectedConnection?.id).toBe(42);
		});
	});

	describe("configInputsValid", () => {
		it("is false before a connection is selected", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));
			expect(result.current.configInputsValid).toBe(false);
		});

		it("is false when required fields are empty", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			// no states filled in
			expect(result.current.configInputsValid).toBe(false);
		});

		it("is true when all required fields are filled", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			act(() => result.current.setDataRetrievalValue("query"));
			act(() => result.current.setWorkItemTypes(["Story"]));
			act(() => result.current.setToDoStates(["New"]));
			act(() => result.current.setDoingStates(["Active"]));
			act(() => result.current.setDoneStates(["Done"]));

			expect(result.current.configInputsValid).toBe(true);
		});

		it("treats dataRetrievalValue as optional when schema.isRequired=false", async () => {
			const args = makeHookArgs({
				getSchema: vi.fn().mockReturnValue({ ...adoSchema, isRequired: false }),
			});
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			act(() => result.current.setWorkItemTypes(["Story"]));
			act(() => result.current.setToDoStates(["New"]));
			act(() => result.current.setDoingStates(["Active"]));
			act(() => result.current.setDoneStates(["Done"]));

			expect(result.current.configInputsValid).toBe(true);
		});

		it("treats workItemTypes as optional when schema.isWorkItemTypesRequired=false", async () => {
			const args = makeHookArgs({
				getSchema: vi.fn().mockReturnValue(linearSchema),
			});
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1, "Linear")));
			// no dataRetrievalValue (isRequired=false), no workItemTypes (not required)
			act(() => result.current.setToDoStates(["New"]));
			act(() => result.current.setDoingStates(["Active"]));
			act(() => result.current.setDoneStates(["Done"]));

			expect(result.current.configInputsValid).toBe(true);
		});
	});

	describe("handleNext", () => {
		it("calls validateSettings and advances to STEP_NAME_CREATE on success", async () => {
			const validateSettings = vi.fn().mockResolvedValue(true);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			act(() => result.current.setDataRetrievalValue("q"));
			act(() => result.current.setWorkItemTypes(["S"]));
			act(() => result.current.setToDoStates(["New"]));
			act(() => result.current.setDoingStates(["Active"]));
			act(() => result.current.setDoneStates(["Done"]));

			await act(() => result.current.handleNext());

			expect(validateSettings).toHaveBeenCalledTimes(1);
			expect(result.current.activeStep).toBe(STEP_NAME_CREATE);
			expect(result.current.validationError).toBeNull();
		});

		it("sets validationError and stays on STEP_CONFIGURE when validation fails", async () => {
			const validateSettings = vi.fn().mockResolvedValue(false);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));

			await act(() => result.current.handleNext());

			expect(result.current.activeStep).toBe(STEP_CONFIGURE);
			expect(result.current.validationError).toMatch(/validation failed/i);
		});

		it("sets validationError when validateSettings throws", async () => {
			const validateSettings = vi.fn().mockRejectedValue(new Error("network"));
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));

			await act(() => result.current.handleNext());

			expect(result.current.validationError).toMatch(/validation failed/i);
			expect(result.current.activeStep).toBe(STEP_CONFIGURE);
		});
	});

	describe("handleBack", () => {
		it("goes from STEP_CONFIGURE to STEP_CHOOSE_CONNECTION when no wizards", async () => {
			mockGetWizardsForSystem.mockReturnValue([]);
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			expect(result.current.activeStep).toBe(STEP_CONFIGURE);

			act(() => result.current.handleBack());
			expect(result.current.activeStep).toBe(STEP_CHOOSE_CONNECTION);
		});

		it("goes from STEP_CONFIGURE to STEP_LOAD_DATA when wizards exist", async () => {
			mockGetWizardsForSystem.mockReturnValue([
				{
					id: "w1",
					name: "Wizard",
					applicableSystemTypes: [],
					applicableSettingsContexts: [],
					component: () =>
						React.createElement("div", {
							"data-testid": "mock-wizard-component",
						}),
				},
			]);
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			// navigate to configure manually
			act(() => result.current.setActiveStep(STEP_CONFIGURE));

			act(() => result.current.handleBack());
			expect(result.current.activeStep).toBe(STEP_LOAD_DATA);
		});

		it("clears validationError on back", async () => {
			const validateSettings = vi.fn().mockResolvedValue(false);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			await act(() => result.current.handleNext());
			expect(result.current.validationError).not.toBeNull();

			act(() => result.current.handleBack());
			expect(result.current.validationError).toBeNull();
		});
	});

	describe("handleCreate", () => {
		it("calls saveSettings with the assembled DTO", async () => {
			const saveSettings = vi.fn().mockResolvedValue(undefined);
			const args = makeHookArgs({ saveSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			const conn = makeConnection(7);
			act(() => result.current.selectConnection(conn));
			act(() => result.current.setDataRetrievalValue("my-query"));
			act(() => result.current.setName("My Entity"));

			await act(() => result.current.handleCreate());

			expect(saveSettings).toHaveBeenCalledOnce();
			const dto = saveSettings.mock.calls[0][0] as SimpleDto;
			expect(dto.name).toBe("My Entity");
			expect(dto.workTrackingSystemConnectionId).toBe(7);
			expect(dto.dataRetrievalValue).toBe("my-query");
		});

		it("sets saving=true during save and false after", async () => {
			let resolveSave!: () => void;
			const saveSettings = vi.fn().mockReturnValue(
				new Promise<void>((resolve) => {
					resolveSave = resolve;
				}),
			);
			const args = makeHookArgs({ saveSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			const savePromise = act(() => result.current.handleCreate());
			expect(result.current.saving).toBe(true);

			resolveSave();
			await savePromise;
			expect(result.current.saving).toBe(false);
		});
	});

	describe("handleWizardComplete", () => {
		it("merges board info into state and validates", async () => {
			const validateSettings = vi.fn().mockResolvedValue(true);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));

			await act(() => result.current.handleWizardComplete(fullBoardInfo));

			expect(result.current.dataRetrievalValue).toBe(
				fullBoardInfo.dataRetrievalValue,
			);
			expect(result.current.workItemTypes).toEqual(fullBoardInfo.workItemTypes);
			expect(result.current.toDoStates).toEqual(fullBoardInfo.toDoStates);
			expect(result.current.doingStates).toEqual(fullBoardInfo.doingStates);
			expect(result.current.doneStates).toEqual(fullBoardInfo.doneStates);
			expect(validateSettings).toHaveBeenCalledTimes(1);
		});

		it("advances to STEP_NAME_CREATE when validation passes", async () => {
			const validateSettings = vi.fn().mockResolvedValue(true);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			await act(() => result.current.handleWizardComplete(fullBoardInfo));

			expect(result.current.activeStep).toBe(STEP_NAME_CREATE);
		});

		it("falls back to STEP_CONFIGURE when validation fails", async () => {
			const validateSettings = vi.fn().mockResolvedValue(false);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			await act(() => result.current.handleWizardComplete(fullBoardInfo));

			expect(result.current.activeStep).toBe(STEP_CONFIGURE);
		});

		it("falls back to STEP_CONFIGURE when validation throws", async () => {
			const validateSettings = vi.fn().mockRejectedValue(new Error("network"));
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			await act(() => result.current.handleWizardComplete(fullBoardInfo));

			expect(result.current.activeStep).toBe(STEP_CONFIGURE);
		});

		it("preserves existing state for empty board info fields", async () => {
			const validateSettings = vi.fn().mockResolvedValue(true);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			act(() => result.current.setDataRetrievalValue("existing-query"));
			act(() => result.current.setWorkItemTypes(["ExistingType"]));

			// Board info with empty fields — existing state should be kept
			await act(() => result.current.handleWizardComplete(emptyBoardInfo));

			expect(result.current.dataRetrievalValue).toBe("existing-query");
			expect(result.current.workItemTypes).toEqual(["ExistingType"]);
		});

		it("clears activeWizard after completion", async () => {
			const validateSettings = vi.fn().mockResolvedValue(true);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() =>
				result.current.setActiveWizard({ id: "w1", name: "Wizard" } as never),
			);
			expect(result.current.activeWizard).not.toBeNull();

			await act(() => result.current.handleWizardComplete(fullBoardInfo));
			expect(result.current.activeWizard).toBeNull();
		});
	});

	describe("handleWizardCancel", () => {
		it("clears the active wizard", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() =>
				result.current.setActiveWizard({ id: "w1", name: "Wizard" } as never),
			);
			act(() => result.current.handleWizardCancel());

			expect(result.current.activeWizard).toBeNull();
		});
	});

	describe("schema and label helpers", () => {
		it("showDataRetrievalField is false when schema.inputKind is 'none'", async () => {
			const args = makeHookArgs({
				getSchema: vi.fn().mockReturnValue(linearSchema),
			});
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1, "Linear")));
			expect(result.current.showDataRetrievalField).toBe(false);
		});

		it("showDataRetrievalField is true when schema.inputKind is 'freetext'", async () => {
			const args = makeHookArgs({
				getSchema: vi.fn().mockReturnValue(adoSchema),
			});
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			expect(result.current.showDataRetrievalField).toBe(true);
		});

		it("getDataRetrievalLabel returns schema.displayLabel when present", async () => {
			const args = makeHookArgs({
				getSchema: vi.fn().mockReturnValue(adoSchema),
			});
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			expect(result.current.getDataRetrievalLabel()).toBe("WIQL Query");
		});

		it("getDataRetrievalLabel falls back to connection display name", async () => {
			const args = makeHookArgs({
				getSchema: vi
					.fn()
					.mockReturnValue({ ...adoSchema, displayLabel: undefined }),
			});
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			// makeConnection returns "WIQL Query" from workTrackingSystemGetDataRetrievalDisplayName
			expect(result.current.getDataRetrievalLabel()).toBe("WIQL Query");
		});

		it("getDataRetrievalLabel returns 'Query' when no connection selected", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));
			expect(result.current.getDataRetrievalLabel()).toBe("Query");
		});
	});
});
