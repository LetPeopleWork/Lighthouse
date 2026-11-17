// @ts-nocheck
import { defineConfig } from "vitest/config";

// Copied into Lighthouse.Frontend/ for the mutation run and removed after it.
export default defineConfig({
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		env: {
			VITE_API_SERVICE_TYPE: "DEMO",
			TZ: "Europe/Zurich",
		},
		css: {
			modules: {
				classNameStrategy: "non-scoped",
			},
		},
		include: [
			"src/utils/charts/outlier*.test.ts",
			"src/components/Common/Charts/CycleTimeScatterPlotChart*.test.tsx",
			"src/components/Common/Charts/OutlierChip*.test.tsx",
			"src/components/Common/Charts/outlier*.test.{ts,tsx}",
			"src/services/Api/MetricsService.test.ts",
			"src/pages/Common/MetricsView/BaseMetricsView*.test.tsx",
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
		pool: "forks",
		isolate: true,
	},
});
