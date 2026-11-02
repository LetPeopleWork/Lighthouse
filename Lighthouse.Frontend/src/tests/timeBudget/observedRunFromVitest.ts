import type { ObservedRun } from "./compareToAllowList";

export const __SCAFFOLD__ = true;

export interface VitestTestOutcome {
	name: string;
	state: "passed" | "failed" | "skipped" | "pending";
	durationMs: number;
}

export interface VitestFileOutcome {
	file: string;
	tests: readonly VitestTestOutcome[];
}

export interface VitestRunOutcome {
	files: readonly VitestFileOutcome[];
	reason: "passed" | "failed" | "interrupted";
}

export function observeVitestRun(
	_outcome: VitestRunOutcome,
	_fileExists: (file: string) => boolean,
): ObservedRun {
	throw new Error("Not yet implemented -- RED scaffold");
}
