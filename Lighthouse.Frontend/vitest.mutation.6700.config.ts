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
		// Only the files that cover the run-chart list helper run: its own tests, the view-data tests and the repeated-items oracle.
		include: [
			"src/pages/Common/MetricsView/workItemLookup.test.ts",
			"src/pages/Common/MetricsView/widgetViewData.test.ts",
			"src/pages/Common/MetricsView/widgetViewData.repeatedItems.test.ts",
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
		pool: "threads",
		isolate: true,
	},
});
