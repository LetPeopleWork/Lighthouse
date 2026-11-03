import { availableParallelism } from "node:os";
import { defineConfig } from "vitest/config";
import { EXPECTED_TEST_FAILURE } from "./src/tests/ExpectedTestFailure.ts";
import { slowTests } from "./src/tests/timeBudget/slowTests.ts";
import { resolveTimeBudgetOptions } from "./src/tests/timeBudget/timeBudgetOptions.ts";
import { TimeBudgetReporter } from "./src/tests/timeBudget/timeBudgetReporter.ts";
import { resolveTestWorkers } from "./src/tests/testWorkers.ts";

// Per-test budget in seconds on a quiet machine.
const BUDGET_SECONDS = 2;
// CI coverage runs are about 2-3x slower per test, so every limit is scaled by this.
const COVERAGE_FACTOR = 3;
// The first test of a file pays for importing the component tree, so it gets extra time.
const FIRST_TEST_ALLOWANCE_SECONDS = 0.31;

const workers = resolveTestWorkers(process.env, availableParallelism());
const budgetSwitches = resolveTimeBudgetOptions(process.env, process.argv);

export default defineConfig({
	test: {
		globals: true,
		onUnhandledError: (error) => error.name !== EXPECTED_TEST_FAILURE,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		env: {
			VITE_API_SERVICE_TYPE: "DEMO",
		},
		css: {
			modules: {
				classNameStrategy: "non-scoped",
			},
		},
		coverage: {
			reporter: ["text", "lcov"],
		},
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			// Trailing wildcard: every stryker config here uses a per-feature suffixed temp dir, so a
			// bare ".stryker-tmp" matched none of them and a run in progress polluted `pnpm test`.
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		reporters: [
			"default",
			["vitest-sonar-reporter", { outputFile: "sonar-report.xml" }],
			["json", { outputFile: "test-results.json" }],
			new TimeBudgetReporter({
				...budgetSwitches,
				budgetSeconds: BUDGET_SECONDS,
				coverageFactor: COVERAGE_FACTOR,
				firstTestAllowanceSeconds: FIRST_TEST_ALLOWANCE_SECONDS,
				allowList: slowTests,
				workers,
				frontendRoot: import.meta.dirname,
			}),
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/],
			},
		},

		pool: "threads",
		isolate: true,
		maxWorkers: workers,
		fileParallelism: true,

		// CI-specific optimizations
		...(process.env.CI && {
			bail: 1, // Optional: fail fast
		}),
	},
});
