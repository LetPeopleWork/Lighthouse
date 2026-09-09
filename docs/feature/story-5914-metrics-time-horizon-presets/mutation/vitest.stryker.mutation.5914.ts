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
		// so the mutation run sees only the specs that cover the mutated files (US 5914).
		include: [
			"src/pages/Common/MetricsView/dateWindow.test.ts",
			"src/pages/Common/MetricsView/useDateRange.test.tsx",
			"src/pages/Common/MetricsView/DateWindowStepper.test.tsx",
			"src/pages/Common/MetricsView/DashboardHeader.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
			"src/components/Common/DateRangeSelector/DateRangePresets.test.tsx",
			"src/components/Common/DateRangeSelector/DateRangeSelector.test.tsx",
			"src/pages/Teams/Detail/TeamForecastView.autorun.test.tsx",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
	},
});
