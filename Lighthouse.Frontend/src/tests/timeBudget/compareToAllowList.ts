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

function printedSeconds(seconds: number): string {
	return String(Number(seconds.toFixed(2)));
}

function describeOverrun(test: ObservedTest, limitSeconds: number): string {
	return `${test.file} :: ${test.name} took ${printedSeconds(test.seconds)} s, limit ${printedSeconds(limitSeconds)} s`;
}

function staleReason(
	entry: AllowListEntry,
	run: ObservedRun,
	options: TimeBudgetOptions,
): StaleReason | undefined {
	if (!run.fileExists(entry.file)) return "file-gone";
	if (!run.collectedFiles.includes(entry.file)) return undefined;
	const ran = run.tests.find(
		(test) => test.file === entry.file && test.name === entry.name,
	);
	if (!ran) return "test-absent";
	// A coverage run slows tests down, so it can keep an entry but never retire it.
	// Only a passed test can retire an entry: a failed one may simply have failed fast.
	const retiresEntries = options.runKind === "plain" && ran.state === "passed";
	if (retiresEntries && ran.seconds < options.budgetSeconds / 4) {
		return "ran-far-under-budget";
	}
	return undefined;
}

type StaleFinding = Extract<Finding, { kind: "stale" }>;

function staleFindings(
	run: ObservedRun,
	allowList: readonly AllowListEntry[],
	options: TimeBudgetOptions,
): StaleFinding[] {
	const stale: StaleFinding[] = [];
	for (const entry of allowList) {
		const why = staleReason(entry, run, options);
		if (why)
			stale.push({ kind: "stale", file: entry.file, name: entry.name, why });
	}
	return stale;
}

export function compareToAllowList(
	run: ObservedRun,
	allowList: readonly AllowListEntry[],
	options: TimeBudgetOptions,
): Verdict {
	if (options.mode === "off")
		return { findings: [], failing: false, lines: [] };

	const findings: Finding[] = [];
	const lines: string[] = [];

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

	if (run.complete) {
		for (const finding of staleFindings(run, allowList, options)) {
			findings.push(finding);
			lines.push(
				`${finding.file} :: ${finding.name} stale allow-list entry (${finding.why})`,
			);
		}
	}

	return {
		findings,
		failing: options.mode === "fail" && findings.length > 0,
		lines,
	};
}
