// @ts-nocheck
import { defineConfig } from "vitest/config";

export default defineConfig({
	test: {
		globals: true,
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
		// Only the direct tests of the functions this Story moved out, and of the time budget guard.
		// With the page tests in, the initial run took twenty minutes and then crashed the Stryker
		// runner, and the question here is whether the direct tests can fail.
		include: [
			"src/components/Common/Connection/*.test.ts",
			"src/components/Common/ProjectSettings/*.test.ts",
			"src/components/Common/Team/*.test.ts",
			"src/pages/Common/Reports/wizard/*.test.ts",
			"src/pages/Reports/*.test.ts",
			"src/pages/Teams/Detail/Refinement/*.test.ts",
			"src/tests/timeBudget/*.test.ts",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/],
			},
		},
		// Instrumented code on a machine shared with another session runs well past the normal limit.
		testTimeout: 30000,
		pool: "threads",
		isolate: true,
	},
});
