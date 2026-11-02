import { describe, expect, it } from "vitest";
import {
	observeVitestRun,
	type VitestFileOutcome,
	type VitestRunOutcome,
} from "./observedRunFromVitest";

const pending = "waits for the time-budget guard to be built";

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

describe.skip(`reading a Vitest run (${pending})`, () => {
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
