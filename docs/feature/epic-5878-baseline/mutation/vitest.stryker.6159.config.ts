// @ts-nocheck
import { defineConfig } from "vitest/config";

// Copy to Lighthouse.Frontend/ before running Stryker; the root pattern is gitignored there.
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
		// Stryker reruns the suite per mutant; sweeping every spec OOMs the node heap, so the run sees
		// only the specs covering the mutated files.
		include: [
			"src/pages/Common/Reports/**/*.test.ts",
			"src/pages/Common/Reports/**/*.test.tsx",
			"src/services/Api/ReportService.test.ts",
			"src/models/Reports/Report.test.ts",
			"src/pages/Teams/Detail/TeamDetail.reportsTab.test.tsx",
			"src/pages/Teams/Detail/TeamDetail.test.tsx",
			"src/pages/Portfolios/Detail/PortfolioDetail.reportsTab.test.tsx",
			"src/components/Common/CreateWizards/CreateWizardShell.test.tsx",
			"src/pages/Common/MetricsView/WidgetShell.test.tsx",
			"src/pages/Common/MetricsView/WidgetShell.status.test.tsx",
			"src/pages/Common/MetricsView/WidgetBody.loadingMessage.test.tsx",
			"src/pages/Common/MetricsView/widgetStatus.test.ts",
			"src/services/UsageData/usageDataReporter.test.ts",
			"src/services/UsageData/usageDataRouteKeys.test.ts",
			"src/services/UsageData/usageDataRouteKeys.reports.test.ts",
			"src/services/UsageData/usageDataEvents.test.ts",
			"src/services/Api/UsageDataService.test.ts",
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
