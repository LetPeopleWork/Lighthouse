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
import { ApiError } from "../services/Api/ApiError";
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

// A ServiceNow board whose lanes could not be split: query and kind of work filled in, no states.
// The kind of work arrives as the label the coach reads, not the record class (#5610 OC-4).
const boardInfoWithNoStates: IBoardInformation = {
	dataRetrievalValue: "correlation_id=LIGHTHOUSE_DEMO",
	workItemTypes: ["Incident"],
	toDoStates: [],
	doingStates: [],
	doneStates: [],
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

type WizardHook = ReturnType<typeof useCreateWizard<SimpleDto>>;

type ConfigInputs = Pick<
	WizardDtoBase,
	| "dataRetrievalValue"
	| "workItemTypes"
	| "toDoStates"
	| "doingStates"
	| "doneStates"
>;

const everyConfigInputFilled: ConfigInputs = {
	dataRetrievalValue: "query",
	workItemTypes: ["Story"],
	toDoStates: ["New"],
	doingStates: ["Active"],
	doneStates: ["Done"],
};

const fillConfig = (
	result: { current: WizardHook },
	values: ConfigInputs,
): void => {
	act(() => result.current.setDataRetrievalValue(values.dataRetrievalValue));
	act(() => result.current.setWorkItemTypes(values.workItemTypes));
	act(() => result.current.setToDoStates(values.toDoStates));
	act(() => result.current.setDoingStates(values.doingStates));
	act(() => result.current.setDoneStates(values.doneStates));
};

// A validateSettings the test holds open, so the flag raised around it can be observed.
const aValidationHeldOpen = () => {
	let settle: (isValid: boolean) => void = () => {};
	const validateSettings = vi.fn(
		() =>
			new Promise<boolean>((resolve) => {
				settle = resolve;
			}),
	);

	return { validateSettings, answer: (isValid: boolean) => settle(isValid) };
};

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
			fillConfig(result, everyConfigInputFilled);

			expect(result.current.configInputsValid).toBe(true);
		});

		// The only false case this suite had was "nothing filled in at all", under which no single
		// clause of the gate can be shown to carry its weight. Each row satisfies every other clause
		// and breaks exactly one.
		it.each<[string, Partial<ConfigInputs>]>([
			["the query is empty", { dataRetrievalValue: "" }],
			["the query is nothing but whitespace", { dataRetrievalValue: "   " }],
			["no kind of work is chosen", { workItemTypes: [] }],
			["nothing says where work starts", { toDoStates: [] }],
			["nothing says work is under way", { doingStates: [] }],
			["nothing says where work ends", { doneStates: [] }],
		])("is false when %s", async (_label, broken) => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			fillConfig(result, { ...everyConfigInputFilled, ...broken });

			expect(result.current.configInputsValid).toBe(false);
		});

		// A connection Lighthouse has no schema for cannot be judged complete, and the guard that says
		// so is also what stands between the gate and a read of `isRequired` on nothing.
		it("is false when the connection has no data retrieval schema", async () => {
			const args = makeHookArgs({ getSchema: vi.fn().mockReturnValue(null) });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			fillConfig(result, everyConfigInputFilled);

			expect(result.current.configInputsValid).toBe(false);
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

		// Next is disabled and a spinner shown while the instance is being asked, and neither survives
		// the answer.
		it("marks itself validating for as long as the instance is being asked", async () => {
			const { validateSettings, answer } = aValidationHeldOpen();
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));

			let finished: Promise<void> = Promise.resolve();
			act(() => {
				finished = result.current.handleNext();
			});
			expect(result.current.validating).toBe(true);

			await act(async () => {
				answer(true);
				await finished;
			});
			expect(result.current.validating).toBe(false);
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

		// #5610. The Next button on Configure is gated on configInputsValid; this path was not, so a
		// wizard that could not fill in every field landed the user on Name & Create with nothing
		// mapped — and the backend's ValidateTeamSettings does not look at state mappings, so it said
		// valid. Pre-existing and not ServiceNow's: it is only the first board that returned no states.
		it("stays on STEP_CONFIGURE when the wizard could not fill in every config input", async () => {
			const validateSettings = vi.fn().mockResolvedValue(true);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			await act(() =>
				result.current.handleWizardComplete(boardInfoWithNoStates),
			);

			expect(result.current.activeStep).toBe(STEP_CONFIGURE);
			expect(result.current.configInputsValid).toBe(false);
			expect(result.current.dataRetrievalValue).toBe(
				boardInfoWithNoStates.dataRetrievalValue,
			);
			expect(result.current.workItemTypes).toEqual(
				boardInfoWithNoStates.workItemTypes,
			);
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
			expect(result.current.validationError).toBeNull();
		});

		// ADR-126 decision 1. A refusal the backend worded reaches the browser as an ApiError, and its
		// words are the only ones that name what an administrator has to go and fix.
		it("shows the reason when the instance refuses what the wizard filled in", async () => {
			const refusal = new ApiError(
				403,
				"ServiceNow refused to read the table 'incident' with this account.",
				"ServiceNow returned 403 for the table 'incident'.",
			);
			const args = makeHookArgs({
				validateSettings: vi.fn().mockRejectedValue(refusal),
			});
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			await act(() => result.current.handleWizardComplete(fullBoardInfo));

			expect(result.current.validationError).toBe(refusal.message);
			expect(result.current.validationTechnicalDetails).toBe(
				refusal.technicalDetails,
			);
			expect(result.current.activeStep).toBe(STEP_CONFIGURE);
		});

		it("preserves existing state for empty board info fields", async () => {
			const validateSettings = vi.fn().mockResolvedValue(true);
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			fillConfig(result, {
				...everyConfigInputFilled,
				dataRetrievalValue: "existing-query",
				workItemTypes: ["ExistingType"],
			});

			// Board info with empty fields — existing state should be kept
			await act(() => result.current.handleWizardComplete(emptyBoardInfo));

			expect(result.current.dataRetrievalValue).toBe("existing-query");
			expect(result.current.workItemTypes).toEqual(["ExistingType"]);
			expect(result.current.toDoStates).toEqual(
				everyConfigInputFilled.toDoStates,
			);
			expect(result.current.doingStates).toEqual(
				everyConfigInputFilled.doingStates,
			);
			expect(result.current.doneStates).toEqual(
				everyConfigInputFilled.doneStates,
			);
		});

		// A board whose filter is nothing but whitespace has no query to hand over, so it must not
		// overwrite the one the administrator typed.
		it("keeps the typed query when the board's own filter is only whitespace", async () => {
			const args = makeHookArgs();
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));
			act(() => result.current.setDataRetrievalValue("existing-query"));

			await act(() =>
				result.current.handleWizardComplete({
					...emptyBoardInfo,
					dataRetrievalValue: "   ",
				}),
			);

			expect(result.current.dataRetrievalValue).toBe("existing-query");
		});

		// The spinner the administrator watches while the instance is asked.
		it("marks itself validating for as long as the instance is being asked", async () => {
			const { validateSettings, answer } = aValidationHeldOpen();
			const args = makeHookArgs({ validateSettings });
			const { result } = renderHook(() => useCreateWizard(args));
			await waitFor(() => expect(result.current.loading).toBe(false));

			act(() => result.current.selectConnection(makeConnection(1)));

			let finished: Promise<void> = Promise.resolve();
			act(() => {
				finished = result.current.handleWizardComplete(fullBoardInfo);
			});
			expect(result.current.validating).toBe(true);

			await act(async () => {
				answer(true);
				await finished;
			});
			expect(result.current.validating).toBe(false);
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
