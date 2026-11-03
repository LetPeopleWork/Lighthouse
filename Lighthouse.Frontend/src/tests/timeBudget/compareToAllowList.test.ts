import { describe, expect, it } from "vitest";
import {
	type AllowListEntry,
	compareToAllowList,
	type ObservedRun,
	type ObservedTest,
	type TimeBudgetOptions,
} from "./compareToAllowList";

// A test offends only when it takes strictly more than its limit.
const plainRun: TimeBudgetOptions = {
	mode: "fail",
	runKind: "plain",
	budgetSeconds: 2,
	firstTestAllowanceSeconds: 0.5,
	coverageFactor: 2,
};
const coverageRun: TimeBudgetOptions = { ...plainRun, runKind: "coverage" };

const reportsFile = "src/pages/Reports/ReportsPage.test.tsx";
const settingsFile = "src/pages/Teams/ModifyTeamSettings.test.tsx";
const slowOne = "picks at most fifty rows";

function aTest(overrides: Partial<ObservedTest> = {}): ObservedTest {
	return {
		file: reportsFile,
		name: "shows the selected count",
		seconds: 0.2,
		state: "passed",
		firstInFile: false,
		...overrides,
	};
}

function aRun(
	tests: ObservedTest[],
	overrides: Partial<ObservedRun> = {},
): ObservedRun {
	return {
		tests,
		collectedFiles: [...new Set(tests.map((test) => test.file))],
		complete: true,
		fileExists: () => true,
		...overrides,
	};
}

function listed(
	overrides: Partial<AllowListEntry> = {},
): readonly AllowListEntry[] {
	return [
		{
			file: reportsFile,
			name: slowOne,
			reason: "renders fifty rows to hit the pick limit",
			ceilingSeconds: 6,
			...overrides,
		},
	];
}

describe("time-budget comparison", () => {
	describe("a test that is not on the allow-list", () => {
		it("fails naming the file, the test, the duration and the threshold when it is over the budget", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: "walks the whole wizard", seconds: 2.4 })]),
				[],
				plainRun,
			);

			expect(verdict.failing).toBe(true);
			expect(verdict.findings).toEqual([
				{
					kind: "over-budget",
					file: reportsFile,
					name: "walks the whole wizard",
					seconds: 2.4,
					limitSeconds: 2,
				},
			]);
			expect(verdict.lines).toEqual([
				`${reportsFile} :: walks the whole wizard took 2.4 s, limit 2 s`,
			]);
		});

		it("passes when it is under the budget", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ seconds: 1.9 })]),
				[],
				plainRun,
			);

			expect(verdict.failing).toBe(false);
			expect(verdict.findings).toEqual([]);
			expect(verdict.lines).toEqual([]);
		});

		it("names every offender, not only the first", () => {
			const verdict = compareToAllowList(
				aRun([
					aTest({ name: "first slow one", seconds: 3 }),
					aTest({ file: settingsFile, name: "second slow one", seconds: 5 }),
					aTest({ name: "quick one", seconds: 0.1 }),
				]),
				[],
				plainRun,
			);

			expect(verdict.findings.map((finding) => finding.name)).toEqual([
				"first slow one",
				"second slow one",
			]);
		});

		it("judges a failed test by its duration like a passed one", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: "fails slowly", seconds: 4, state: "failed" })]),
				[],
				plainRun,
			);

			expect(verdict.findings).toEqual([
				expect.objectContaining({ kind: "over-budget", name: "fails slowly" }),
			]);
		});

		it.each([
			["a later test at exactly the budget", false, 2, false],
			["a later test just over the budget", false, 2.1, true],
			[
				"a first test over the budget but within the allowance",
				true,
				2.4,
				false,
			],
			["a later test at the same time gets no allowance", false, 2.4, true],
			["a first test at exactly budget plus allowance", true, 2.5, false],
			["a first test over budget plus allowance", true, 2.6, true],
		])("plain run, %s", (_name, firstInFile, seconds, offends) => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: "the one judged", seconds, firstInFile })]),
				[],
				plainRun,
			);

			expect(verdict.findings.map((finding) => finding.name)).toEqual(
				offends ? ["the one judged"] : [],
			);
		});

		it.each([
			["a later test under the scaled budget", false, 3.9, false],
			["a later test at exactly the scaled budget", false, 4, false],
			["a later test over the scaled budget", false, 4.1, true],
			[
				"a first test at exactly the scaled limit plus allowance",
				true,
				4.5,
				false,
			],
			["a first test over the scaled limit plus allowance", true, 4.6, true],
		])(
			"coverage run scales the budget by the coverage factor: %s",
			(_name, firstInFile, seconds, offends) => {
				const verdict = compareToAllowList(
					aRun([aTest({ name: "the one judged", seconds, firstInFile })]),
					[],
					coverageRun,
				);

				expect(verdict.findings.map((finding) => finding.name)).toEqual(
					offends ? ["the one judged"] : [],
				);
			},
		);

		it("reports the scaled limit in the finding", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ seconds: 5 })]),
				[],
				coverageRun,
			);

			expect(verdict.findings).toEqual([
				expect.objectContaining({ kind: "over-budget", limitSeconds: 4 }),
			]);
		});
	});

	describe("tests are matched on their exact file and name", () => {
		it.each([
			[
				"a listed row beside an unlisted slow row of the same table",
				[
					aTest({ name: "opens panel for context a", seconds: 5 }),
					aTest({ name: "opens panel for context b", seconds: 2.5 }),
				],
				listed({ name: "opens panel for context a" }),
				[{ kind: "over-budget", name: "opens panel for context b" }],
			],
			[
				"a listed name that the run's name only starts with",
				[aTest({ name: `${slowOne} (later)`, seconds: 5 })],
				listed(),
				[{ kind: "over-budget", name: `${slowOne} (later)` }],
			],
			[
				"a listed name that the run's name only ends with",
				[aTest({ name: `later ${slowOne}`, seconds: 5 })],
				listed(),
				[{ kind: "over-budget", name: `later ${slowOne}` }],
			],
			[
				"a listed file path that the run's path extends at the end",
				[aTest({ file: `${reportsFile}x`, name: slowOne, seconds: 5 })],
				listed(),
				[{ kind: "over-budget", file: `${reportsFile}x` }],
			],
			[
				"a listed file path that the run's path extends at the start",
				[aTest({ file: `old/${reportsFile}`, name: slowOne, seconds: 5 })],
				listed(),
				[{ kind: "over-budget", file: `old/${reportsFile}` }],
			],
		])("%s", (_name, tests, allowList, expected) => {
			const verdict = compareToAllowList(aRun(tests), allowList, plainRun);

			expect(verdict.findings).toEqual(
				expected.map((match) => expect.objectContaining(match)),
			);
		});
	});

	describe("a test that is on the allow-list", () => {
		it.each([
			["well within its ceiling", 5.5, false, "passed", false],
			["at exactly its ceiling", 6, false, "passed", false],
			["just over its ceiling", 6.1, false, "passed", true],
			["over its ceiling when it failed", 6.4, false, "failed", true],
			[
				"over its ceiling but within the first-test allowance",
				6.4,
				true,
				"passed",
				false,
			],
			[
				"over its ceiling plus the first-test allowance",
				6.6,
				true,
				"passed",
				true,
			],
		] as const)("%s", (_name, seconds, firstInFile, state, offends) => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: slowOne, seconds, firstInFile, state })]),
				listed(),
				plainRun,
			);

			expect(verdict.failing).toBe(offends);
			expect(verdict.findings).toEqual(
				offends
					? [
							{
								kind: "over-ceiling",
								file: reportsFile,
								name: slowOne,
								seconds,
								limitSeconds: firstInFile ? 6.5 : 6,
							},
						]
					: [],
			);
		});

		it("names the test and both numbers when it grows past its ceiling", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: slowOne, seconds: 6.4 })]),
				listed(),
				plainRun,
			);

			expect(verdict.lines).toEqual([
				`${reportsFile} :: ${slowOne} took 6.4 s, limit 6 s`,
			]);
		});

		it.each([
			["within the scaled ceiling", 11.5, false, false],
			["at exactly the scaled ceiling", 12, false, false],
			["over the scaled ceiling", 12.5, false, true],
			[
				"a first test at exactly the scaled ceiling plus allowance",
				12.5,
				true,
				false,
			],
			["a first test over the scaled ceiling plus allowance", 12.6, true, true],
		])(
			"in a coverage run its ceiling is scaled like the budget: %s",
			(_name, seconds, firstInFile, offends) => {
				const verdict = compareToAllowList(
					aRun([aTest({ name: slowOne, seconds, firstInFile })]),
					listed(),
					coverageRun,
				);

				expect(verdict.findings).toEqual(
					offends
						? [expect.objectContaining({ kind: "over-ceiling", name: slowOne })]
						: [],
				);
			},
		);

		it("is matched on file and name together, so the same title in another file is judged against the budget", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ file: settingsFile, name: slowOne, seconds: 5 })]),
				listed(),
				plainRun,
			);

			expect(verdict.findings).toEqual([
				expect.objectContaining({ kind: "over-budget", file: settingsFile }),
			]);
		});
	});

	describe("an allow-list entry that no longer earns its place", () => {
		it("is stale when its file ran and the test is not among the tests that ran", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: "some other test" })]),
				listed(),
				plainRun,
			);

			expect(verdict.failing).toBe(true);
			expect(verdict.findings).toEqual([
				{
					kind: "stale",
					file: reportsFile,
					name: slowOne,
					why: "test-absent",
				},
			]);
			expect(verdict.lines).toEqual([
				`${reportsFile} :: ${slowOne} stale allow-list entry (test-absent)`,
			]);
		});

		it("is stale when its file was collected but reported no tests at all", () => {
			const verdict = compareToAllowList(
				aRun([], { collectedFiles: [reportsFile] }),
				listed(),
				plainRun,
			);

			expect(verdict.findings).toEqual([
				{
					kind: "stale",
					file: reportsFile,
					name: slowOne,
					why: "test-absent",
				},
			]);
		});

		it("is stale when the test now runs in under a quarter of the budget", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: slowOne, seconds: 0.4 })]),
				listed(),
				plainRun,
			);

			expect(verdict.findings).toEqual([
				{
					kind: "stale",
					file: reportsFile,
					name: slowOne,
					why: "ran-far-under-budget",
				},
			]);
		});

		it("stays listed when the test runs at exactly a quarter of the budget", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: slowOne, seconds: 0.5 })]),
				listed(),
				plainRun,
			);

			expect(verdict.findings).toEqual([]);
		});

		it("is stale when its file is gone from disk, whether or not the run touched it", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ file: settingsFile })], {
					fileExists: (file) => file !== reportsFile,
				}),
				listed(),
				plainRun,
			);

			expect(verdict.findings).toEqual([
				{
					kind: "stale",
					file: reportsFile,
					name: slowOne,
					why: "file-gone",
				},
			]);
		});

		it("is ignored when its file was not collected in the run", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ file: settingsFile })]),
				listed(),
				plainRun,
			);

			expect(verdict.findings).toEqual([]);
		});

		it("is ignored when a filter skipped the test", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: slowOne, seconds: 0, state: "skipped" })]),
				listed(),
				plainRun,
			);

			expect(verdict.findings).toEqual([]);
		});

		it("is not judged at all when the run stopped early, not even for a file gone from disk", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: "some other test" })], {
					complete: false,
					fileExists: () => false,
				}),
				listed(),
				plainRun,
			);

			expect(verdict.findings).toEqual([]);
		});

		it("still names a slow test in a run that stopped early", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: "slow before the stop", seconds: 9 })], {
					complete: false,
				}),
				[],
				plainRun,
			);

			expect(verdict.findings).toEqual([
				expect.objectContaining({
					kind: "over-budget",
					name: "slow before the stop",
				}),
			]);
		});

		it("is measured against a quarter of the unscaled budget in a coverage run too", () => {
			const verdict = compareToAllowList(
				aRun([aTest({ name: slowOne, seconds: 0.8 })]),
				listed(),
				coverageRun,
			);

			expect(verdict.findings).toEqual([]);
		});
	});

	describe("the guard's mode", () => {
		const offending = aRun([aTest({ name: "far too slow", seconds: 9 })]);

		it("judges nothing when it is off, as under mutation testing or in watch mode", () => {
			const verdict = compareToAllowList(offending, listed(), {
				...plainRun,
				mode: "off",
			});

			expect(verdict.failing).toBe(false);
			expect(verdict.findings).toEqual([]);
			expect(verdict.lines).toEqual([]);
		});

		it("reports every finding but does not fail when it only warns", () => {
			const verdict = compareToAllowList(offending, [], {
				...plainRun,
				mode: "warn",
			});

			expect(verdict.failing).toBe(false);
			expect(verdict.findings).toEqual([
				expect.objectContaining({ kind: "over-budget", name: "far too slow" }),
			]);
			expect(verdict.lines).toEqual([
				`${reportsFile} :: far too slow took 9 s, limit 2 s`,
			]);
		});

		it("does not fail a clean run even when it enforces", () => {
			const verdict = compareToAllowList(aRun([aTest()]), [], plainRun);

			expect(verdict.failing).toBe(false);
			expect(verdict.findings).toEqual([]);
		});
	});
});
