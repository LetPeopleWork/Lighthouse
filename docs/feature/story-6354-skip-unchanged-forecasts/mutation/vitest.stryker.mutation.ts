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
		// Stryker reruns this set for every mutant, so it lists only the specs that render the Refresh
		// History section and its chart. A spec left out makes the mutants it covers survive for want of a
		// run, not for want of a test.
		include: [
			"src/pages/Settings/SystemInfo/RefreshHistorySection.test.tsx",
			"src/pages/Settings/SystemInfo/RefreshHistorySection.findings.test.tsx",
			"src/pages/Settings/SystemInfo/RefreshHistorySection.skippedForecasts.test.tsx",
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
	},
});
