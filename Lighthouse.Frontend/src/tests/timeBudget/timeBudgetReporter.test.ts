import { tmpdir } from "node:os";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { TestModule } from "vitest/node";
import {
	TimeBudgetReporter,
	type TimeBudgetReporterOptions,
} from "./timeBudgetReporter.ts";
import { aCase, aModule, aSuite } from "./vitestFakes.ts";

const aFile = "src/pages/Reports/ReportsPage.test.tsx";

function reporterWith(overrides: Partial<TimeBudgetReporterOptions> = {}) {
	return new TimeBudgetReporter({
		mode: "fail",
		runKind: "plain",
		budgetSeconds: 2,
		firstTestAllowanceSeconds: 0.5,
		limitFactor: 2,
		annotation: "none",
		allowList: [],
		workers: 2,
		frontendRoot: tmpdir(),
		...overrides,
	});
}

function aRunWithOneTest(durationMs: number): TestModule[] {
	const module = aModule(aFile, [
		aCase("renders", aSuite("Reports"), "passed", durationMs),
	]);
	return [module as unknown as TestModule];
}

describe("the time-budget reporter", () => {
	let printed: string[];
	let savedExitCode: typeof process.exitCode;

	beforeEach(() => {
		printed = [];
		savedExitCode = process.exitCode;
		process.exitCode = undefined;
		vi.spyOn(console, "log").mockImplementation((line: unknown) => {
			printed.push(String(line));
		});
	});

	afterEach(() => {
		vi.restoreAllMocks();
		process.exitCode = savedExitCode;
	});

	const finish = (
		reporter: TimeBudgetReporter,
		modules: TestModule[],
		reason: "passed" | "failed" | "interrupted" = "passed",
	) => reporter.onTestRunEnd(modules, [], reason);

	const offenderLine = `${aFile} :: Reports renders took 5 s, limit 2.5 s`;

	it("fails the process and prints the offender with the error mark when enforcing on GitHub Actions", () => {
		finish(reporterWith({ annotation: "::error::" }), aRunWithOneTest(5000));

		expect(process.exitCode).toBe(1);
		expect(printed).toContain(`::error::${offenderLine}`);
	});

	it("prints an offender without any mark outside GitHub Actions", () => {
		finish(reporterWith({ annotation: "none" }), aRunWithOneTest(5000));

		expect(printed).toContain(offenderLine);
	});

	it("closes the report with a summary naming the mode and the number of offenders", () => {
		finish(reporterWith(), aRunWithOneTest(5000));

		expect(printed[printed.length - 1]).toBe(
			"Time budget: fail mode, budget 2 s, limit factor 2, first-test allowance 0.5 s, 1 offenders",
		);
	});

	it("names a scaled run and its limit factor", () => {
		const scaled = reporterWith({ runKind: "scaled", limitFactor: 3 });
		scaled.onInit();
		finish(scaled, aRunWithOneTest(5000));

		expect(printed[0]).toContain("scaled run");
		expect(printed[printed.length - 1]).toContain("limit factor 3");
	});

	it("leaves the exit code alone on a clean run", () => {
		finish(reporterWith(), aRunWithOneTest(100));

		expect(process.exitCode).toBeUndefined();
	});

	it("never resets an exit code that is already failing", () => {
		process.exitCode = 1;

		finish(reporterWith(), aRunWithOneTest(100));

		expect(process.exitCode).toBe(1);
	});

	it("warns with the warning mark and leaves the exit code alone when it only warns", () => {
		finish(
			reporterWith({ mode: "warn", annotation: "::warning::" }),
			aRunWithOneTest(5000),
		);

		expect(process.exitCode).toBeUndefined();
		expect(printed).toContain(`::warning::${offenderLine}`);
	});

	it("prints nothing and leaves the exit code alone when it is off", () => {
		const reporter = reporterWith({ mode: "off" });

		reporter.onInit();
		finish(reporter, aRunWithOneTest(5000));

		expect(printed).toEqual([]);
		expect(process.exitCode).toBeUndefined();
	});

	it("announces its mode, kind of run and worker count at the start", () => {
		reporterWith({ workers: undefined }).onInit();

		expect(printed[0]).toMatch(
			/^Time budget: fail mode, plain run, default workers, \d+ processors$/,
		);
	});

	it("does not call a file stale when it broke while being collected", () => {
		const existingFile = "package.json";
		const broken = aModule(existingFile, [], [new Error("syntax error")]);

		finish(
			reporterWith({
				allowList: [
					{
						file: existingFile,
						name: "Reports renders",
						reason: "slow",
						ceilingSeconds: 6,
					},
				],
				frontendRoot: process.cwd(),
			}),
			[broken as unknown as TestModule],
			"failed",
		);

		expect(printed.some((line) => line.includes("stale"))).toBe(false);
	});
});
