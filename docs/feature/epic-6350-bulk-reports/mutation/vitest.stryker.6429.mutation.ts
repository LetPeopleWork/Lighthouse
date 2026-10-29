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
		// Stryker reruns this set for every mutant, so it lists only the suites that reach the files slice 02a changed.
		include: [
			"src/pages/Reports/**/*.test.{ts,tsx}",
			"src/pages/Common/Reports/**/*.test.{ts,tsx}",
			"src/services/Api/ReportService.test.ts",
			"src/models/Reports/**/*.test.{ts,tsx}",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/, /@svar-ui\//],
			},
		},
		pool: "threads",
		isolate: true,
		// Coverage instrumentation, with a backend mutation run sharing the machine, pushes the slowest wizard tests past the 5 s default.
		testTimeout: 30000,
	},
});
