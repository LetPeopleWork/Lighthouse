import type { RunKind, TimeBudgetMode } from "./compareToAllowList.ts";

export const TIME_BUDGET_VARIABLE = "LIGHTHOUSE_TIME_BUDGET";

const STRYKER_WORKER_VARIABLE = "STRYKER_MUTATOR_WORKER";

export interface TimeBudgetSwitches {
	mode: TimeBudgetMode;
	runKind: RunKind;
	/** Prefix that marks a finding line in the CI log; none outside GitHub Actions. */
	annotation: "::error::" | "::warning::" | "none";
}

type Environment = Readonly<Record<string, string | undefined>>;

function requestedMode(env: Environment): TimeBudgetMode {
	const requested = env[TIME_BUDGET_VARIABLE];

	if (requested === undefined || requested === "") {
		return "fail";
	}
	if (requested === "warn" || requested === "off") {
		return requested;
	}
	throw new Error(
		`${TIME_BUDGET_VARIABLE} must be "fail", "warn" or "off", but was "${requested}"`,
	);
}

/** Vitest's own reading: CI counts as set unless it is empty or "false". */
export function isCiRun(env: Environment): boolean {
	const ci = env.CI;
	return ci !== undefined && ci !== "" && ci.toLowerCase() !== "false";
}

const COVERAGE_SWITCHED_ON = new Set([
	"--coverage",
	"--coverage=true",
	"--coverage.enabled",
	"--coverage.enabled=true",
]);

function isWatchRun(env: Environment, argv: readonly string[]): boolean {
	if (isCiRun(env)) {
		return false;
	}
	if (argv.includes("--watch")) {
		return true;
	}
	return !argv.includes("run") && !argv.includes("--run");
}

function annotationFor(
	env: Environment,
	mode: TimeBudgetMode,
): TimeBudgetSwitches["annotation"] {
	if (env.GITHUB_ACTIONS === undefined || mode === "off") {
		return "none";
	}
	return mode === "fail" ? "::error::" : "::warning::";
}

/**
 * Decides how the time budget behaves for a test run from the environment and the
 * command-line arguments. Timings mean nothing under mutation testing or in watch mode,
 * so the guard is off there whatever the switch says.
 */
export function resolveTimeBudgetOptions(
	env: Environment,
	argv: readonly string[],
): TimeBudgetSwitches {
	const requested = requestedMode(env);
	const timingsMeaningless =
		env[STRYKER_WORKER_VARIABLE] !== undefined || isWatchRun(env, argv);
	const mode = timingsMeaningless ? "off" : requested;

	return {
		mode,
		runKind: argv.some((argument) => COVERAGE_SWITCHED_ON.has(argument))
			? "coverage"
			: "plain",
		annotation: annotationFor(env, mode),
	};
}
