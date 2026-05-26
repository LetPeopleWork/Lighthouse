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
		include: [
			"src/utils/staleness/deriveStaleness.test.ts",
			"src/pages/Common/MetricsView/StaleOverviewWidget.test.tsx",
			"src/pages/Common/MetricsView/ragRules.test.ts",
			"src/utils/charts/scatterMarkerUtils.test.tsx",
			"src/components/Common/TimeInStateBadge/TimeInStateBadge.test.tsx",
		],
		server: {
			deps: {
				inline: ["@mui/x-data-grid"],
			},
		},
		pool: "forks",
		isolate: true,
		fileParallelism: false,
	},
});
