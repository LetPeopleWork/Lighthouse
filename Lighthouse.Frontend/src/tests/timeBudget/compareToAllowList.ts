export const __SCAFFOLD__ = true;

export type TimeBudgetMode = "off" | "warn" | "fail";
export type RunKind = "plain" | "coverage";

export interface TimeBudgetOptions {
	mode: TimeBudgetMode;
	runKind: RunKind;
	/** Seconds a test may take on a quiet machine under a plain run. */
	budgetSeconds: number;
	/** Extra seconds granted to the first test of each file. */
	firstTestAllowanceSeconds: number;
	/** Multiplier applied to every limit when the run is a coverage run. */
	coverageFactor: number;
}

export interface AllowListEntry {
	file: string;
	name: string;
	reason: string;
	/** Seconds on a quiet machine under a plain run. */
	ceilingSeconds: number;
}

export interface ObservedTest {
	file: string;
	name: string;
	seconds: number;
	state: "passed" | "failed" | "skipped";
	firstInFile: boolean;
}

export interface ObservedRun {
	tests: readonly ObservedTest[];
	/** Every file Vitest collected, including files that reported no tests. */
	collectedFiles: readonly string[];
	/** False when the run stopped early, so absent tests prove nothing. */
	complete: boolean;
	fileExists: (file: string) => boolean;
}

export type StaleReason = "file-gone" | "test-absent" | "ran-far-under-budget";

export type Finding =
	| {
			kind: "over-budget" | "over-ceiling";
			file: string;
			name: string;
			seconds: number;
			limitSeconds: number;
	  }
	| { kind: "stale"; file: string; name: string; why: StaleReason };

export interface Verdict {
	findings: readonly Finding[];
	/** True only in fail mode with at least one finding. */
	failing: boolean;
	/**
	 * One line per finding. Over a limit: "<file> :: <test> took <seconds> s, limit <limit> s".
	 * Stale: "<file> :: <test> stale allow-list entry (<reason>)".
	 */
	lines: readonly string[];
}

function limitFor(
	test: ObservedTest,
	entry: AllowListEntry | undefined,
	options: TimeBudgetOptions,
): number {
	const base = entry?.ceilingSeconds ?? options.budgetSeconds;
	const scaled =
		options.runKind === "coverage" ? base * options.coverageFactor : base;
	return test.firstInFile ? scaled + options.firstTestAllowanceSeconds : scaled;
}

function describeOverrun(test: ObservedTest, limitSeconds: number): string {
	return `${test.file} :: ${test.name} took ${test.seconds} s, limit ${limitSeconds} s`;
}

export function compareToAllowList(
	run: ObservedRun,
	allowList: readonly AllowListEntry[],
	options: TimeBudgetOptions,
): Verdict {
	const findings: Finding[] = [];
	const lines: string[] = [];

	if (options.mode !== "off") {
		for (const test of run.tests) {
			if (test.state === "skipped") continue;
			const entry = allowList.find(
				(candidate) =>
					candidate.file === test.file && candidate.name === test.name,
			);
			const limitSeconds = limitFor(test, entry, options);
			if (test.seconds <= limitSeconds) continue;
			findings.push({
				kind: entry ? "over-ceiling" : "over-budget",
				file: test.file,
				name: test.name,
				seconds: test.seconds,
				limitSeconds,
			});
			lines.push(describeOverrun(test, limitSeconds));
		}
	}

	return {
		findings,
		failing: options.mode === "fail" && findings.length > 0,
		lines,
	};
}
