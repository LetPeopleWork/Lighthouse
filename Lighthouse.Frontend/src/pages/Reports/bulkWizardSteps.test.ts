import { describe, expect, it, vi } from "vitest";
import type { IReportTemplates } from "../../models/Reports/Report";
import { newBackToBackDraft } from "../Common/Reports/wizard/thenAndNow/thenAndNowDraft";
import {
	createActionOf,
	createLabelOf,
	nextActionOf,
	OWNER_STEP,
	runOf,
	STEP_LABELS,
	typeStepAt,
} from "./bulkWizardSteps";
import { OVER_THE_CEILING_ID } from "./OwnerStep";
import type { useBulkReportCreation } from "./useBulkReportCreation";

const templates: IReportTemplates = {
	today: "2026-10-02",
	earliestStart: "2025-10-02",
	minimumLengthDays: 14,
	templates: [],
};

const THEN_STEP = 1;
const NOW_STEP = 2;

describe("the steps of the bulk wizard, in order", () => {
	it("lists Type, then the type's own steps, then Owners", () => {
		expect(STEP_LABELS).toEqual([
			"Type",
			"Then",
			"Now",
			"Metrics & name",
			"Owners",
		]);
	});

	it("puts the owner step last", () => {
		expect(OWNER_STEP).toBe(STEP_LABELS.length - 1);
	});

	it("has no step of the type's own at the Type step or at the owner step", () => {
		expect(typeStepAt(0)).toBeUndefined();
		expect(typeStepAt(OWNER_STEP)).toBeUndefined();
	});

	it("answers each step in between with the type's step of that label", () => {
		expect([1, 2, 3].map((at) => typeStepAt(at)?.label)).toEqual([
			"Then",
			"Now",
			"Metrics & name",
		]);
	});
});

describe("what Next does on each step", () => {
	const onNext = vi.fn();

	it("is offered on the Type step, enabled", () => {
		expect(nextActionOf(0, newBackToBackDraft(), templates, onNext)).toEqual({
			label: "Next",
			onClick: onNext,
			disabled: false,
		});
	});

	it("is not offered on the owner step", () => {
		expect(
			nextActionOf(OWNER_STEP, newBackToBackDraft(), templates, onNext),
		).toBeUndefined();
	});

	it.each([
		["Then's length is no number of days", THEN_STEP, { thenLength: "" }],
		["Then's length is not a whole number", THEN_STEP, { thenLength: "14.5" }],
		["Then is shorter than the shortest Then", THEN_STEP, { thenLength: "13" }],
		[
			"Then ends after today",
			THEN_STEP,
			{ thenEnd: "2026-10-03", thenLength: "30" },
		],
		["Now's length is not a whole number", NOW_STEP, { nowLength: "14.5" }],
		["Now is shorter than the shortest Now", NOW_STEP, { nowLength: "13" }],
	])("is held while %s", (_why, step, entered) => {
		const draft = { ...newBackToBackDraft(), ...entered };

		expect(nextActionOf(step, draft, templates, onNext)?.disabled).toBe(true);
	});

	it.each([THEN_STEP, NOW_STEP])(
		"is enabled on step %i for the draft a new wizard starts with",
		(step) => {
			expect(
				nextActionOf(step, newBackToBackDraft(), templates, onNext)?.disabled,
			).toBe(false);
		},
	);
});

describe("what Create does on the owner step", () => {
	const onCreate = vi.fn();

	it("is not offered before the owner step", () => {
		expect(createActionOf(0, 3, "ready", onCreate)).toBeUndefined();
		expect(createActionOf(NOW_STEP, 3, "ready", onCreate)).toBeUndefined();
	});

	it("counts the picked owners in its label, in the singular for one", () => {
		expect(createLabelOf(1)).toBe("Create 1 report");
		expect(createLabelOf(0)).toBe("Create 0 reports");
		expect(createLabelOf(50)).toBe("Create 50 reports");
	});

	it("is held while nothing is picked", () => {
		expect(createActionOf(OWNER_STEP, 0, "ready", onCreate)?.disabled).toBe(
			true,
		);
	});

	it.each(["loading", "failed"] as const)(
		"is held while the owners' roles are %s",
		(status) => {
			expect(createActionOf(OWNER_STEP, 3, status, onCreate)?.disabled).toBe(
				true,
			);
		},
	);

	it("is enabled for one pick and for exactly 50, with nothing to describe it", () => {
		for (const count of [1, 50]) {
			const action = createActionOf(OWNER_STEP, count, "ready", onCreate);

			expect(action?.disabled).toBe(false);
			expect(action?.describedBy).toBeUndefined();
			expect(action?.label).toBe(createLabelOf(count));
		}
	});

	it("is held at 51 and points at the sentence that says at most 50", () => {
		const action = createActionOf(OWNER_STEP, 51, "ready", onCreate);

		expect(action?.disabled).toBe(true);
		expect(action?.describedBy).toBe(OVER_THE_CEILING_ID);
	});

	it("calls what it was given when pressed", async () => {
		await createActionOf(OWNER_STEP, 2, "ready", onCreate)?.onClick();

		expect(onCreate).toHaveBeenCalledTimes(1);
	});
});

describe("how a creation run is shown", () => {
	const creation = (state: object) =>
		state as ReturnType<typeof useBulkReportCreation>;
	const idle = {
		isPending: false,
		isError: false,
		isSuccess: false,
		data: undefined,
	};

	it("shows no run before anything was sent", () => {
		expect(runOf(creation(idle))).toBeNull();
	});

	it("shows a run under way while the answer is awaited", () => {
		expect(runOf(creation({ ...idle, isPending: true }))).toEqual({
			status: "pending",
		});
	});

	it("shows a run that got no answer as an error", () => {
		expect(runOf(creation({ ...idle, isError: true }))).toEqual({
			status: "error",
		});
	});

	it("shows the answered outcomes of a run that succeeded", () => {
		const outcomes = [{ ownerKind: "team", ownerId: 1 }];

		expect(
			runOf(creation({ ...idle, isSuccess: true, data: outcomes })),
		).toEqual({ status: "success", outcomes });
	});
});
