// @ts-nocheck
import { defineConfig } from "vitest/config";
import { EXPECTED_TEST_FAILURE } from "./src/tests/ExpectedTestFailure.ts";

// Copy this file into Lighthouse.Frontend/ before a run: Stryker resolves `vitest.configFile` from
// the run directory, and the imports above resolve from wherever the file sits.
export default defineConfig({
	test: {
		globals: true,
		// Some specs make a click handler fail on purpose; only that error may go unhandled.
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
		// Stryker runs the include set per mutant, so only the specs covering the mutated files are
		// listed. A spec left out makes the mutants it covers survive for want of a run, not of a test.
		include: [
			"src/components/Common/TimeInStateBadge/TimeInStateBadge.test.tsx",
			"src/components/Common/Charts/WorkItemAgingChart.test.tsx",
			"src/components/Common/WorkItemsDialog/WorkItemsDialog.test.tsx",
			"src/components/Common/WorkItemsDialog/WorkItemsDialog.contexts.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
			"src/pages/Common/MetricsView/WidgetShellTimeInState.test.tsx",
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
