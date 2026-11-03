import { describe, expect, it } from "vitest";
import {
	observeVitestRun,
	type VitestFileOutcome,
	type VitestRunOutcome,
	type VitestTestOutcome,
	vitestRunOutcome,
} from "./observedRunFromVitest.ts";

interface FakeCase {
	name: string;
	parent: FakeParent;
	result(): { state: VitestTestOutcome["state"] };
	diagnostic(): { duration: number } | undefined;
}

interface FakeModule {
	relativeModuleId: string;
	children: { allTests(): Iterable<FakeCase> };
}

const reportsFile = "src/pages/Reports/ReportsPage.test.tsx";
const settingsFile = "src/pages/Teams/ModifyTeamSettings.test.tsx";

function aFile(
	file: string,
	tests: VitestFileOutcome["tests"],
): VitestFileOutcome {
	return { file, tests };
}

function aRunOutcome(
	files: VitestFileOutcome[],
	reason: VitestRunOutcome["reason"] = "passed",
): VitestRunOutcome {
	return { files, reason };
}

const everythingExists = () => true;

describe("reading a Vitest run", () => {
	it("turns each test into a file, a full name and seconds", () => {
		const observed = observeVitestRun(
			aRunOutcome([
				aFile(reportsFile, [
					{
						name: "Reports > shows the count",
						state: "passed",
						durationMs: 1250,
					},
				]),
			]),
			everythingExists,
		);

		expect(observed.tests).toEqual([
			{
				file: reportsFile,
				name: "Reports > shows the count",
				seconds: 1.25,
				state: "passed",
				firstInFile: true,
			},
		]);
	});

	it.each([
		["passed", "passed"],
		["failed", "failed"],
		["skipped", "skipped"],
		["pending", "skipped"],
	] as const)("reads Vitest's %s state as %s", (vitestState, expected) => {
		const observed = observeVitestRun(
			aRunOutcome([
				aFile(reportsFile, [
					{ name: "the test", state: vitestState, durationMs: 10 },
				]),
			]),
			everythingExists,
		);

		expect(observed.tests.map((test) => test.state)).toEqual([expected]);
	});

	it("marks only the first test that ran in each file as the file's first", () => {
		const observed = observeVitestRun(
			aRunOutcome([
				aFile(reportsFile, [
					{ name: "filtered out", state: "skipped", durationMs: 0 },
					{ name: "runs first", state: "passed", durationMs: 900 },
					{ name: "runs second", state: "passed", durationMs: 100 },
				]),
				aFile(settingsFile, [
					{ name: "other file first", state: "failed", durationMs: 700 },
					{ name: "other file second", state: "passed", durationMs: 50 },
				]),
			]),
			everythingExists,
		);

		expect(
			observed.tests
				.filter((test) => test.firstInFile)
				.map((test) => `${test.file} :: ${test.name}`),
		).toEqual([
			`${reportsFile} :: runs first`,
			`${settingsFile} :: other file first`,
		]);
	});

	it("lists every collected file, including one that reported no tests", () => {
		const observed = observeVitestRun(
			aRunOutcome([
				aFile(reportsFile, [
					{ name: "the test", state: "passed", durationMs: 10 },
				]),
				aFile(settingsFile, []),
			]),
			everythingExists,
		);

		expect(observed.collectedFiles).toEqual([reportsFile, settingsFile]);
	});

	it.each([
		["passed", true],
		["failed", true],
		["interrupted", false],
	] as const)("a run that ended as %s is complete: %s", (reason, complete) => {
		const observed = observeVitestRun(
			aRunOutcome([], reason),
			everythingExists,
		);

		expect(observed.complete).toBe(complete);
	});

	it("asks the file system about a file through the function it was given", () => {
		const observed = observeVitestRun(
			aRunOutcome([]),
			(file) => file === reportsFile,
		);

		expect(observed.fileExists(reportsFile)).toBe(true);
		expect(observed.fileExists(settingsFile)).toBe(false);
	});
});

type FakeParent =
	| { type: "module" }
	| { type: "suite"; name: string; parent: FakeParent };

function aModule(relativeModuleId: string, tests: FakeCase[]): FakeModule {
	return {
		relativeModuleId,
		children: { allTests: () => tests },
	};
}

function aSuite(
	name: string,
	parent: FakeParent = { type: "module" },
): FakeParent {
	return { type: "suite", name, parent };
}

function aCase(
	name: string,
	parent: FakeParent,
	state: VitestTestOutcome["state"] = "passed",
	duration?: number,
): FakeCase {
	return {
		name,
		parent,
		result: () => ({ state }),
		diagnostic: () => (duration === undefined ? undefined : { duration }),
	};
}

describe("naming Vitest tests like the JSON reporter", () => {
	const nameOf = (test: FakeCase) =>
		vitestRunOutcome([aModule("a.test.ts", [test])], "passed").files[0].tests[0]
			.name;

	it("joins nested describe titles and the test title with single spaces", () => {
		const outer = aSuite("Query field");
		const inner = aSuite("on blur", outer);

		expect(nameOf(aCase("saves once", inner))).toBe(
			"Query field on blur saves once",
		);
	});

	it("uses the bare title for a test at module level", () => {
		expect(nameOf(aCase("works", { type: "module" }))).toBe("works");
	});

	it("keeps an it.each row title as written", () => {
		expect(nameOf(aCase("handles 3 items", aSuite("Counting")))).toBe(
			"Counting handles 3 items",
		);
	});

	it("does not touch a title that contains the old separator", () => {
		expect(nameOf(aCase("a > b", aSuite("Arrows > tab")))).toBe(
			"Arrows > tab a > b",
		);
	});

	it("maps file, state and duration, defaulting a missing duration to zero", () => {
		const outcome = vitestRunOutcome(
			[
				aModule("src/x.test.ts", [
					aCase("ran", aSuite("S"), "passed", 12.5),
					aCase("skipped", aSuite("S"), "skipped"),
				]),
			],
			"failed",
		);

		expect(outcome).toEqual({
			files: [
				{
					file: "src/x.test.ts",
					tests: [
						{ name: "S ran", state: "passed", durationMs: 12.5 },
						{ name: "S skipped", state: "skipped", durationMs: 0 },
					],
				},
			],
			reason: "failed",
		});
	});
});
