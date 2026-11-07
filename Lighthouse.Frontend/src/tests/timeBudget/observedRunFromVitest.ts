import type { ObservedRun, ObservedTest } from "./compareToAllowList.ts";

export interface VitestTestOutcome {
	name: string;
	state: "passed" | "failed" | "skipped" | "pending";
	durationMs: number;
	/** When the test started, in milliseconds. Absent when Vitest did not record it. */
	startedAtMs?: number;
}

export interface VitestFileOutcome {
	file: string;
	tests: readonly VitestTestOutcome[];
	/** True when the file broke while Vitest was collecting its tests, so its tests are unknown. */
	collectionFailed?: boolean;
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

/**
 * Vitest lists a file's tests in the order they were collected. When the test
 * order is shuffled they run in another order, so the one that pays for the
 * file's setup is the one that started first, not the first in the list.
 * Tests that started in the same millisecond keep their listed order.
 */
function indexOfFirstTestThatRan(file: VitestFileOutcome): number {
	let first = -1;
	file.tests.forEach((test, index) => {
		if (observedState(test.state) === "skipped") return;
		if (first === -1) {
			first = index;
			return;
		}
		const firstStarted = file.tests[first].startedAtMs;
		if (
			test.startedAtMs !== undefined &&
			firstStarted !== undefined &&
			test.startedAtMs < firstStarted
		) {
			first = index;
		}
	});
	return first;
}

function observeFile(file: VitestFileOutcome): ObservedTest[] {
	const firstRan = indexOfFirstTestThatRan(file);

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
		collectedFiles: outcome.files
			.filter((file) => !file.collectionFailed)
			.map((file) => file.file),
		complete: outcome.reason !== "interrupted",
		fileExists,
	};
}

interface VitestParent {
	type: string;
	name?: string;
	parent?: VitestParent;
}

export interface VitestTestCaseLike {
	name: string;
	parent: VitestParent;
	result(): { state: VitestTestOutcome["state"] };
	diagnostic(): { duration: number; startTime?: number } | undefined;
}

export interface VitestModuleLike {
	relativeModuleId: string;
	children: { allTests(): Iterable<VitestTestCaseLike> };
	errors(): readonly unknown[];
}

/**
 * The name the JSON reporter writes as `fullName`: the enclosing describe
 * titles, outermost first, then the test title, joined by single spaces.
 */
function jsonReporterFullName(test: VitestTestCaseLike): string {
	const titles = [test.name];
	let node: VitestParent | undefined = test.parent;
	while (node && node.type !== "module") {
		titles.unshift(node.name ?? "");
		node = node.parent;
	}
	return titles.join(" ");
}

export function vitestRunOutcome(
	modules: readonly VitestModuleLike[],
	reason: VitestRunOutcome["reason"],
): VitestRunOutcome {
	return {
		files: modules.map((module) => ({
			file: module.relativeModuleId,
			collectionFailed: module.errors().length > 0,
			tests: Array.from(module.children.allTests(), (test) => ({
				name: jsonReporterFullName(test),
				state: test.result().state,
				durationMs: test.diagnostic()?.duration ?? 0,
				startedAtMs: test.diagnostic()?.startTime,
			})),
		})),
		reason,
	};
}
