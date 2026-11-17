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
			"src/pages/Common/Reports/wizard/**/*.test.{ts,tsx}",
			"src/pages/Common/Reports/panels/**/*.test.{ts,tsx}",
			"src/pages/Common/Reports/ReportsTab.createWizard.test.tsx",
			"src/pages/Common/Reports/ReportsTab.test.tsx",
			"src/pages/Common/Reports/ReportView.editWizard.test.tsx",
			"src/pages/Common/Reports/ReportView.test.tsx",
			"src/pages/Reports/BulkReportWizard*.test.tsx",
			"src/pages/Reports/bulkWizardSteps.test.ts",
			"src/pages/Reports/ownerLimitOfWindow.test.ts",
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
