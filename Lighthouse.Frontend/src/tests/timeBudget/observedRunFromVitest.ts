import type { ObservedRun, ObservedTest } from "./compareToAllowList";

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

const MILLISECONDS_PER_SECOND = 1000;

function observedState(
	state: VitestTestOutcome["state"],
): ObservedTest["state"] {
	return state === "pending" ? "skipped" : state;
}

function observeFile(file: VitestFileOutcome): ObservedTest[] {
	const firstRan = file.tests.findIndex(
		(test) => observedState(test.state) !== "skipped",
	);

	return file.tests.map((test, index) => ({
		file: file.file,
		name: test.name,
		seconds: test.durationMs / MILLISECONDS_PER_SECOND,
		state: observedState(test.state),
		firstInFile: index === firstRan,
	}));
}

export function observeVitestRun(
	outcome: VitestRunOutcome,
	fileExists: (file: string) => boolean,
): ObservedRun {
	return {
		tests: outcome.files.flatMap(observeFile),
		collectedFiles: outcome.files.map((file) => file.file),
		complete: outcome.reason !== "interrupted",
		fileExists,
	};
}
