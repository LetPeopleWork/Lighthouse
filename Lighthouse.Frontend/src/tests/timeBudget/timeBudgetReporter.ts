import { existsSync } from "node:fs";
import { availableParallelism } from "node:os";
import { resolve } from "node:path";
import type { Reporter, TestModule } from "vitest/node";
import {
	type AllowListEntry,
	compareToAllowList,
	type TimeBudgetOptions,
} from "./compareToAllowList.ts";
import {
	observeVitestRun,
	type VitestRunOutcome,
	vitestRunOutcome,
} from "./observedRunFromVitest.ts";
import type { TimeBudgetSwitches } from "./timeBudgetOptions.ts";

export interface TimeBudgetReporterOptions
	extends TimeBudgetOptions,
		Pick<TimeBudgetSwitches, "annotation"> {
	allowList: readonly AllowListEntry[];
	workers: number | undefined;
	frontendRoot: string;
}

export class TimeBudgetReporter implements Reporter {
	constructor(private readonly options: TimeBudgetReporterOptions) {}

	onInit(): void {
		const { mode, runKind, workers } = this.options;
		if (mode === "off") return;
		console.log(
			`Time budget: ${mode} mode, ${runKind} run, ${workers ?? "default"} workers, ${availableParallelism()} processors`,
		);
	}

	onTestRunEnd(
		testModules: ReadonlyArray<TestModule>,
		_unhandledErrors: unknown,
		reason: VitestRunOutcome["reason"],
	): void {
		const { options } = this;
		if (options.mode === "off") return;

		const verdict = compareToAllowList(
			observeVitestRun(vitestRunOutcome(testModules, reason), (file) =>
				existsSync(resolve(options.frontendRoot, file)),
			),
			options.allowList,
			options,
		);

		const prefix = options.annotation === "none" ? "" : options.annotation;
		for (const line of verdict.lines) console.log(`${prefix}${line}`);
		console.log(
			`Time budget: ${options.mode} mode, budget ${options.budgetSeconds} s, limit factor ${options.runKind === "scaled" ? options.limitFactor : 1}, first-test allowance ${options.firstTestAllowanceSeconds} s, ${verdict.findings.length} offenders`,
		);
		if (verdict.failing) process.exitCode = 1;
	}
}
