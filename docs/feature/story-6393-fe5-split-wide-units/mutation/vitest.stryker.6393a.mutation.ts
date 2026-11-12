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
		// Only the direct unit tests of the part A logic modules.
		include: [
			"src/hooks/useScopedAccess.test.ts",
			"src/pages/Common/MetricsView/dashboardFrame.test.ts",
			"src/pages/Common/MetricsView/workItemLookup.test.ts",
			"src/pages/Common/MetricsView/widgetViewData.test.ts",
			"src/pages/Common/MetricsView/ragRulesOverview.test.ts",
			"src/pages/Common/MetricsView/ragRulesTimeAndAge.test.ts",
			"src/pages/Common/MetricsView/ragRulesVolumeAndFlow.test.ts",
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
