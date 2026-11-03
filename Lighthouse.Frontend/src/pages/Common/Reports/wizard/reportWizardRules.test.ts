import { describe, expect, it } from "vitest";
import type { IReportTemplates } from "../../../../models/Reports/Report";
import { ApiError } from "../../../../services/Api/ApiError";
import {
	COULD_NOT_SAVE,
	NO_LONGER_EXISTS,
	type RefusalWords,
} from "./reportRefusalCopy";
import { reportTypes } from "./reportTypes";
import {
	canMoveOn,
	firstStepOf,
	isGone,
	isStaleSave,
	isThenBeforeCutoff,
	STALE_SAVE,
	saveFailureCopyFor,
} from "./reportWizardRules";

const type = reportTypes["then-and-now"];
const [thenStep, nowStep] = type.steps;

const templates: IReportTemplates = {
	today: "2026-10-02",
	earliestStart: "2025-10-02",
	minimumLengthDays: 14,
	templates: [],
};

const words: RefusalWords = {
	minimumLengthDays: 14,
	earliestStart: "2025-10-02",
	ownerTerm: "Team",
	workItemsTerm: "Work Items",
};

const refusedWith = (code: number, problemCode?: string) =>
	new ApiError(code, "refused", undefined, undefined, problemCode);

describe("where the wizard opens", () => {
	it("opens a new report on the Type step", () => {
		expect(firstStepOf(false)).toBe(0);
	});

	it("opens an edit on Now, because Type and Then are locked", () => {
		expect(firstStepOf(true)).toBe(2);
	});
});

describe("whether a step lets the reader move on", () => {
	const draft = type.newDraft();

	it("holds every step until today's bounds have been read", () => {
		expect(canMoveOn(undefined, false, draft, undefined)).toBe(false);
		expect(canMoveOn(thenStep, true, draft, undefined)).toBe(false);
		expect(canMoveOn(nowStep, false, draft, undefined)).toBe(false);
	});

	it("lets the reader move on from the Type step once the bounds are read", () => {
		expect(canMoveOn(undefined, false, draft, templates)).toBe(true);
	});

	it("lets the reader move on from a step whose draft is complete", () => {
		expect(canMoveOn(thenStep, false, draft, templates)).toBe(true);
	});

	it("holds a step whose draft is not complete", () => {
		const tooShort = { ...draft, thenLength: "13" };

		expect(canMoveOn(thenStep, false, tooShort, templates)).toBe(false);
	});

	it("lets the reader past a locked step whatever its draft says", () => {
		const tooShort = { ...draft, thenLength: "13" };

		expect(canMoveOn(thenStep, true, tooShort, templates)).toBe(true);
	});
});

describe("which failures of a save are told apart", () => {
	it("takes a 409 for somebody else's change, and nothing else", () => {
		expect(isStaleSave(refusedWith(409))).toBe(true);
		expect(isStaleSave(refusedWith(404))).toBe(false);
		expect(isStaleSave(new Error("409"))).toBe(false);
	});

	it("takes a 404 for a report somebody else deleted, and nothing else", () => {
		expect(isGone(refusedWith(404))).toBe(true);
		expect(isGone(refusedWith(409))).toBe(false);
		expect(isGone(new Error("404"))).toBe(false);
	});

	it("takes only the refusal naming the oldest kept day for a Then before the cutoff", () => {
		expect(isThenBeforeCutoff(refusedWith(422, "then-before-cutoff"))).toBe(
			true,
		);
		expect(isThenBeforeCutoff(refusedWith(422, "then-too-short"))).toBe(false);
		expect(isThenBeforeCutoff(new Error("then-before-cutoff"))).toBe(false);
	});
});

describe("which sentence a failed save gets", () => {
	it("says the report was changed by someone else for a 409", () => {
		expect(saveFailureCopyFor(refusedWith(409), words)).toBe(STALE_SAVE);
	});

	it("says the report no longer exists for a 404", () => {
		expect(saveFailureCopyFor(refusedWith(404), words)).toBe(NO_LONGER_EXISTS);
	});

	it("gives the sentence of a refusal the server named", () => {
		expect(saveFailureCopyFor(refusedWith(422, "then-too-short"), words)).toBe(
			"Then needs at least 14 days.",
		);
	});

	it("says the report could not be saved for any other failure", () => {
		expect(saveFailureCopyFor(new Error("the network dropped"), words)).toBe(
			COULD_NOT_SAVE,
		);
	});
});
