import { describe, expect, it } from "vitest";
import {
	observeVitestRun,
	type VitestFileOutcome,
	type VitestRunOutcome,
	vitestRunOutcome,
} from "./observedRunFromVitest.ts";
import {
	aCase,
	aModule,
	aSuite,
	type FakeCase,
	type FakeParent,
} from "./vitestFakes.ts";

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

	it("does not count a file that failed while being collected as collected", () => {
		const observed = observeVitestRun(
			aRunOutcome([
				aFile(reportsFile, [
					{ name: "the test", state: "passed", durationMs: 10 },
				]),
				{ file: settingsFile, tests: [], collectionFailed: true },
			]),
			everythingExists,
		);

		expect(observed.collectedFiles).toEqual([reportsFile]);
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

	it("counts a suite that has no title as an empty title", () => {
		const untitled = {
			type: "suite",
			parent: { type: "module" },
		} as unknown as FakeParent;

		expect(nameOf(aCase("works", untitled))).toBe(" works");
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

	it("flags a module that reported errors during collection", () => {
		const outcome = vitestRunOutcome(
			[
				aModule("src/broken.test.ts", [], [new Error("syntax error")]),
				aModule("src/fine.test.ts", []),
			],
			"failed",
		);

		expect(outcome.files.map((file) => file.collectionFailed)).toEqual([
			true,
			false,
		]);
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
					collectionFailed: false,
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
