import type { RunKind, TimeBudgetMode } from "./compareToAllowList";

export const __SCAFFOLD__ = true;

export const TIME_BUDGET_VARIABLE = "LIGHTHOUSE_TIME_BUDGET";

export interface TimeBudgetSwitches {
	mode: TimeBudgetMode;
	runKind: RunKind;
	/** Prefix that marks a finding line in the CI log; none outside GitHub Actions. */
	annotation: "::error::" | "::warning::" | "none";
}

export function resolveTimeBudgetOptions(
	_env: Readonly<Record<string, string | undefined>>,
	_argv: readonly string[],
): TimeBudgetSwitches {
	throw new Error("Not yet implemented -- RED scaffold");
}
