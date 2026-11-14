import { availableParallelism } from "node:os";
import { defineConfig } from "vitest/config";
import { EXPECTED_TEST_FAILURE } from "./src/tests/ExpectedTestFailure.ts";
import { resolveTestWorkers } from "./src/tests/testWorkers.ts";
import { slowTests } from "./src/tests/timeBudget/slowTests.ts";
import {
	isCiRun,
	resolveTimeBudgetOptions,
} from "./src/tests/timeBudget/timeBudgetOptions.ts";
import { TimeBudgetReporter } from "./src/tests/timeBudget/timeBudgetReporter.ts";

// Per-test budget in seconds on a quiet machine, for a test in a .test.tsx file.
const BUDGET_SECONDS = 2;
// The same for a test in any other file: those render no component, so they can be held to less.
const FAST_BUDGET_SECONDS = 0.7;
// CI machines are slower than a developer's, and a run with coverage switched on is slower again, so every limit
// is multiplied by this on CI and on any coverage run. A plain run on a developer machine keeps the limits as they are.
const LIMIT_FACTOR = 3;
// The first test of a file pays for the cold first render and module warm-up inside the test, so it gets extra time.
const FIRST_TEST_ALLOWANCE_SECONDS = 0.31;

const ignoredPaths = [
	"**/node_modules/**",
	"**/dist/**",
	// Trailing wildcard: every stryker config here uses a per-feature suffixed temp dir, so a
	// bare ".stryker-tmp" matched none of them and a run in progress polluted `pnpm test`.
	"**/.stryker-tmp*/**",
	"**/StrykerOutput/**",
];

// A time zone is process-wide. The default project runs in worker threads pinned to one zone by the
// test scripts, so tests that need another zone run in a project of their own, in forked processes.
const WEST_OF_UTC_TESTS = "**/*.westOfUtc.test.{ts,tsx}";
const WEST_OF_UTC_ZONE = "America/New_York";

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
		exclude: ignoredPaths,
		projects: [
			{
				extends: true,
				test: {
					name: "default",
					exclude: [...ignoredPaths, WEST_OF_UTC_TESTS],
				},
			},
			{
				extends: true,
				test: {
					name: "west-of-utc",
					include: [WEST_OF_UTC_TESTS],
					exclude: ignoredPaths,
					pool: "forks",
					env: {
						VITE_API_SERVICE_TYPE: "DEMO",
						TZ: WEST_OF_UTC_ZONE,
					},
				},
			},
		],
		reporters: [
			"default",
			["vitest-sonar-reporter", { outputFile: "sonar-report.xml" }],
			["json", { outputFile: "test-results.json" }],
			new TimeBudgetReporter({
				...budgetSwitches,
				budgetSeconds: BUDGET_SECONDS,
				fastBudgetSeconds: FAST_BUDGET_SECONDS,
				limitFactor: LIMIT_FACTOR,
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
		...(isCiRun(process.env) && {
			bail: 1, // Optional: fail fast
		}),
	},
});
