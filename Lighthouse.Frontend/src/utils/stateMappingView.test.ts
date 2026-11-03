import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import type { IStateMapping } from "../models/Common/StateMapping";
import { validateStateMappings } from "./stateMappingValidation";
import { displayStatesFor, foldKey, isBlankName } from "./stateMappingView";

const here = dirname(fileURLToPath(import.meta.url));

interface RuleCases {
	sameNameOrNot: { a: string; b: string; same: boolean }[];
	sameOnceTrimmed: { a: string; b: string }[];
	blank: string[];
}

// The server reads the same file, so the page and the server cannot drift in what they call the same name.
const cases: RuleCases = JSON.parse(
	readFileSync(resolve(here, "./stateMappingRuleCases.json"), "utf8"),
);

const NONBREAKING_SPACE = String.fromCharCode(160);

const review: IStateMapping = {
	name: "Review",
	states: ["Code Review", "Testing"],
};

const lists = (doing: string[]) => ({
	toDoStates: ["Backlog"],
	doingStates: doing,
	doneStates: ["Done"],
});

const duplicateNameErrors = (a: string, b: string) =>
	validateStateMappings(
		[
			{ name: a, states: ["Code Review"] },
			{ name: b, states: ["Testing"] },
		],
		lists(["Code Review", "Testing"]),
	).filter((error) => error.startsWith("Duplicate mapping name"));

const isAscii = (text: string) => [...text].every((c) => c.charCodeAt(0) < 128);

describe("displayStatesFor", () => {
	it("shows one group in the place of states that sit next to each other", () => {
		expect(
			displayStatesFor(
				["Analysis", "Code Review", "Testing", "Waiting"],
				[review],
			),
		).toEqual(["Analysis", "Review", "Waiting"]);
	});

	it("puts a group where its first listed state is when its states are apart", () => {
		expect(
			displayStatesFor(
				["Code Review", "Analysis", "Testing", "Waiting"],
				[review],
			),
		).toEqual(["Review", "Analysis", "Waiting"]);
	});

	it("treats the same state in another casing or with spaces around it as one", () => {
		expect(
			displayStatesFor(
				["Analysis", "analysis ", "  CODE REVIEW", "testing "],
				[review],
			),
		).toEqual(["Analysis", "Review"]);
	});

	it("shows a group named after one of its own states once", () => {
		expect(
			displayStatesFor(
				["Active", "Resolved", "Blocked"],
				[{ name: "Active", states: ["Active", "Resolved"] }],
			),
		).toEqual(["Active", "Blocked"]);
	});

	it("leaves a list alone when none of a group's states are in it", () => {
		expect(displayStatesFor(["Analysis", "Waiting"], [review])).toEqual([
			"Analysis",
			"Waiting",
		]);
	});

	it("leaves a state exactly as stored once the group that held it is deleted", () => {
		expect(displayStatesFor(["Y", "X"], [])).toEqual(["Y", "X"]);
	});

	it("leaves out entries that are empty or only whitespace", () => {
		expect(
			displayStatesFor(["Analysis", "", "  ", "\t", "Code Review"], [review]),
		).toEqual(["Analysis", "Review"]);
	});

	it("recognises states padded with tabs and non-breaking spaces as their group", () => {
		expect(
			displayStatesFor(
				[
					"Analysis",
					"\tCode Review",
					`${NONBREAKING_SPACE}Testing${NONBREAKING_SPACE}`,
				],
				[review],
			),
		).toEqual(["Analysis", "Review"]);
	});

	it("shows the list as it is when there are no groups", () => {
		expect(displayStatesFor(["Analysis", "Testing"], [])).toEqual([
			"Analysis",
			"Testing",
		]);
	});
});

describe("the page and the server agree on which names are the same", () => {
	it("tells the same names apart in plain letters", () => {
		for (const { a, b, same } of cases.sameNameOrNot.filter(
			(pair) => isAscii(pair.a) && isAscii(pair.b),
		)) {
			expect(duplicateNameErrors(a, b).length > 0, `${a} and ${b}`).toBe(same);
		}
	});

	it("tells the same names apart in every case of the shared list", () => {
		for (const { a, b, same } of cases.sameNameOrNot) {
			expect(duplicateNameErrors(a, b).length > 0, `${a} and ${b}`).toBe(same);
		}
	});

	it("folds the names of the shared list to the same key exactly when the server does", () => {
		for (const { a, b, same } of cases.sameNameOrNot) {
			expect(foldKey(a) === foldKey(b), `${a} and ${b}`).toBe(same);
		}
	});

	it("ignores whitespace at the ends the way the server does", () => {
		for (const { a, b } of cases.sameOnceTrimmed) {
			expect(foldKey(a), `${JSON.stringify(a)} and ${b}`).toBe(foldKey(b));
			expect(duplicateNameErrors(a, b).length).toBeGreaterThan(0);
		}
	});

	it("calls a name of nothing but whitespace blank, whichever whitespace it is", () => {
		for (const blank of cases.blank) {
			expect(isBlankName(blank), JSON.stringify(blank)).toBe(true);
			expect(
				validateStateMappings(
					[{ name: blank, states: ["Code Review"] }],
					lists(["Code Review"]),
				).some((error) => error.includes("empty name")),
				JSON.stringify(blank),
			).toBe(true);
		}
	});

	it("calls an empty name and a name of ordinary spaces blank", () => {
		for (const blank of ["", " ", "\t"]) {
			expect(
				validateStateMappings(
					[{ name: blank, states: ["Code Review"] }],
					lists(["Code Review"]),
				).some((error) => error.includes("empty name")),
				JSON.stringify(blank),
			).toBe(true);
		}
	});
});
