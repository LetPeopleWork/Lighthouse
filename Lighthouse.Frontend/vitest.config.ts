import { defineConfig } from "vitest/config";
import { EXPECTED_TEST_FAILURE } from "./src/tests/ExpectedTestFailure.ts";

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
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/],
			},
		},

		pool: "threads",
		isolate: true,
		maxWorkers: undefined,
		fileParallelism: true,

		// CI-specific optimizations
		...(process.env.CI && {
			// Reduce memory usage on CI
			bail: 1, // Optional: fail fast
		}),
	},
});
