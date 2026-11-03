import { describe, expect, it } from "vitest";
import { resolveTimeBudgetOptions } from "./timeBudgetOptions.ts";

const plainArgv = ["node", "vitest.mjs", "run", "--reporter=default"];
const coverageArgv = ["node", "vitest.mjs", "run", "--coverage"];
const noRunArgv = ["node", "vitest.mjs"];

describe("time-budget options", () => {
	describe("enforcement", () => {
		it.each([
			["nothing is set", {}],
			["the switch is empty", { LIGHTHOUSE_TIME_BUDGET: "" }],
			["it is a CI run with the switch unset", { CI: "true" }],
		])("is on when %s", (_name, env) => {
			expect(resolveTimeBudgetOptions(env, plainArgv).mode).toBe("fail");
		});

		it.each([
			["warn", "warn"],
			["off", "off"],
		])("follows the switch value %s", (value, expected) => {
			expect(
				resolveTimeBudgetOptions(
					{ LIGHTHOUSE_TIME_BUDGET: value, CI: "true" },
					coverageArgv,
				).mode,
			).toBe(expected);
		});

		it("refuses a switch value it does not know, naming the variable and the value", () => {
			expect(() =>
				resolveTimeBudgetOptions({ LIGHTHOUSE_TIME_BUDGET: "sometimes" }, []),
			).toThrow(/LIGHTHOUSE_TIME_BUDGET.*sometimes/);
		});
	});

	describe("the kind of run", () => {
		it("is a plain run for the ordinary test command", () => {
			expect(resolveTimeBudgetOptions({}, plainArgv).runKind).toBe("plain");
		});

		it("is a coverage run when coverage is asked for", () => {
			expect(
				resolveTimeBudgetOptions({ CI: "true" }, coverageArgv).runKind,
			).toBe("coverage");
		});

		it.each([
			["--coverage", "coverage"],
			["--coverage=true", "coverage"],
			["--coverage.enabled", "coverage"],
			["--coverage.enabled=true", "coverage"],
			["--coverage=false", "plain"],
			["--coverage.enabled=false", "plain"],
			["--coverage.reporter=text", "plain"],
		] as const)("reads %s as a %s run", (flag, expected) => {
			expect(
				resolveTimeBudgetOptions({}, ["node", "vitest.mjs", "run", flag])
					.runKind,
			).toBe(expected);
		});
	});

	describe("how findings are marked in the log", () => {
		it.each([
			["GitHub Actions, enforcing", { GITHUB_ACTIONS: "true" }, "::error::"],
			[
				"GitHub Actions, warning only",
				{ GITHUB_ACTIONS: "true", LIGHTHOUSE_TIME_BUDGET: "warn" },
				"::warning::",
			],
			[
				"GitHub Actions, switched off",
				{ GITHUB_ACTIONS: "true", LIGHTHOUSE_TIME_BUDGET: "off" },
				"none",
			],
			["a developer machine, enforcing", {}, "none"],
			[
				"a developer machine, warning only",
				{ LIGHTHOUSE_TIME_BUDGET: "warn" },
				"none",
			],
			[
				"a developer machine, switched off",
				{ LIGHTHOUSE_TIME_BUDGET: "off" },
				"none",
			],
		])("on %s the mark is %s", (_name, env, expected) => {
			expect(resolveTimeBudgetOptions(env, plainArgv).annotation).toBe(
				expected,
			);
		});

		it("is unmarked under mutation testing even on GitHub Actions", () => {
			expect(
				resolveTimeBudgetOptions(
					{ GITHUB_ACTIONS: "true", STRYKER_MUTATOR_WORKER: "1" },
					plainArgv,
				).annotation,
			).toBe("none");
		});
	});

	describe("runs where timings mean nothing", () => {
		it("is off under mutation testing, whatever the switch says", () => {
			expect(
				resolveTimeBudgetOptions(
					{ STRYKER_MUTATOR_WORKER: "1", LIGHTHOUSE_TIME_BUDGET: "warn" },
					plainArgv,
				).mode,
			).toBe("off");
		});

		it.each([
			["the watch flag is given", {}, ["node", "vitest.mjs", "--watch"]],
			["no run command is given on a developer machine", {}, noRunArgv],
		])("is off in watch mode when %s", (_name, env, argv) => {
			expect(resolveTimeBudgetOptions(env, argv).mode).toBe("off");
		});

		it("is never watch mode in CI, so a CI command without a run argument still enforces", () => {
			expect(resolveTimeBudgetOptions({ CI: "true" }, noRunArgv).mode).toBe(
				"fail",
			);
		});

		it.each([
			["CI=1", "1", "fail"],
			["CI=TRUE", "TRUE", "fail"],
			["CI=false", "false", "off"],
			["an empty CI", "", "off"],
		])(
			"reads %s like Vitest does when deciding whether a bare command is a watch",
			(_name, ci, expected) => {
				expect(resolveTimeBudgetOptions({ CI: ci }, noRunArgv).mode).toBe(
					expected,
				);
			},
		);

		it("stays on for a one-off run given as --run", () => {
			expect(
				resolveTimeBudgetOptions({}, ["node", "vitest.mjs", "--run"]).mode,
			).toBe("fail");
		});
	});
});
