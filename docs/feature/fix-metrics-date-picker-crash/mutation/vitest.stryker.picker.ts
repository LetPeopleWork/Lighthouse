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
		// Stryker reruns every included spec for each mutant, so the run is split by subject:
		// this half sees only the specs covering the date field and the two date utilities.
		include: [
			"src/utils/date/isValidDate.test.ts",
			"src/utils/date/localDate.test.ts",
			"src/components/Common/DateRangeSelector/DateRangeSelector.test.tsx",
			"src/components/Common/DateRangeSelector/DateRangeSelector.keyboard.test.tsx",
			"src/pages/Common/MetricsView/DashboardHeader.test.tsx",
			"src/pages/Common/MetricsView/DashboardHeader.popover.test.tsx",
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
