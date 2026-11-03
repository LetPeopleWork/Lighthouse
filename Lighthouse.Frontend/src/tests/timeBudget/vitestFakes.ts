import type {
	VitestModuleLike,
	VitestTestCaseLike,
	VitestTestOutcome,
} from "./observedRunFromVitest.ts";

export type FakeParent =
	| { type: "module" }
	| { type: "suite"; name: string; parent: FakeParent };

export type FakeCase = VitestTestCaseLike & { parent: FakeParent };

export function aModule(
	relativeModuleId: string,
	tests: FakeCase[],
	errors: readonly unknown[] = [],
): VitestModuleLike {
	return {
		relativeModuleId,
		children: { allTests: () => tests },
		errors: () => errors,
	};
}

export function aSuite(
	name: string,
	parent: FakeParent = { type: "module" },
): FakeParent {
	return { type: "suite", name, parent };
}

export function aCase(
	name: string,
	parent: FakeParent,
	state: VitestTestOutcome["state"] = "passed",
	duration?: number,
): FakeCase {
	return {
		name,
		parent,
		result: () => ({ state }),
		diagnostic: () => (duration === undefined ? undefined : { duration }),
	};
}
