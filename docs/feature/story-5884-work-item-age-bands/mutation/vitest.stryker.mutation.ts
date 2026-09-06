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
		// Stryker reruns the suite once per mutant; sweeping all ~350 spec files OOMs the node heap,
		// so the mutation run sees only the specs that cover the mutated files (US 5884).
		include: [
			"src/utils/charts/paceBands.test.ts",
			"src/components/Common/Charts/WorkItemAgingChart.test.tsx",
			"src/components/Common/WorkItemsDialog/WorkItemsDialog.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
			"src/pages/Common/MetricsView/WidgetShell.test.tsx",
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
