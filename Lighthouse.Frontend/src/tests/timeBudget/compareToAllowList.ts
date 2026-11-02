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

export function compareToAllowList(
	_run: ObservedRun,
	_allowList: readonly AllowListEntry[],
	_options: TimeBudgetOptions,
): Verdict {
	throw new Error("Not yet implemented -- RED scaffold");
}
