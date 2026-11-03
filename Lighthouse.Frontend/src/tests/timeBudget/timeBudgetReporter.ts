import { existsSync } from "node:fs";
import { availableParallelism } from "node:os";
import { resolve } from "node:path";
import type { Reporter, TestModule } from "vitest/node";
import {
	type AllowListEntry,
	compareToAllowList,
	type TimeBudgetOptions,
} from "./compareToAllowList";
import {
	observeVitestRun,
	type VitestFileOutcome,
	type VitestRunOutcome,
} from "./observedRunFromVitest";
import type { TimeBudgetSwitches } from "./timeBudgetOptions";

export interface TimeBudgetReporterOptions
	extends TimeBudgetOptions,
		Pick<TimeBudgetSwitches, "annotation"> {
	allowList: readonly AllowListEntry[];
	workers: number | undefined;
	frontendRoot: string;
}

function fileOutcome(module: TestModule): VitestFileOutcome {
	return {
		file: module.relativeModuleId,
		tests: Array.from(module.children.allTests()).map((test) => ({
			name: test.fullName,
			state: test.result().state,
			durationMs: test.diagnostic()?.duration ?? 0,
		})),
	};
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

		const outcome: VitestRunOutcome = {
			files: testModules.map(fileOutcome),
			reason,
		};
		const verdict = compareToAllowList(
			observeVitestRun(outcome, (file) =>
				existsSync(resolve(options.frontendRoot, file)),
			),
			options.allowList,
			options,
		);

		const prefix = options.annotation === "none" ? "" : options.annotation;
		for (const line of verdict.lines) console.log(`${prefix}${line}`);
		console.log(
			`Time budget: ${options.mode} mode, budget ${options.budgetSeconds} s, coverage factor ${options.coverageFactor}, first-test allowance ${options.firstTestAllowanceSeconds} s, ${verdict.findings.length} offenders`,
		);
		if (verdict.failing) process.exitCode = 1;
	}
}
