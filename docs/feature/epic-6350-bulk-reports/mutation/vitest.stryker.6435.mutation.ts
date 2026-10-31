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
		// Stryker reruns this set for every mutant, so it lists only the suites that reach the files slice 07 changed.
		// The BulkReportWizard suites are in because they open the wizard from the Reports page, through useBulkCreation.
		// ActionButton.test.tsx stays out: one of its tests leaves a rejected promise unhandled, which crashes the dry run.
		include: [
			"src/pages/Reports/ReportsPage*.test.tsx",
			"src/pages/Reports/BulkReportWizard*.test.tsx",
			"src/pages/Common/Reports/print/*.test.{ts,tsx}",
			"src/pages/Common/Reports/ReportList.test.tsx",
			"src/pages/Common/Reports/ReportsTab.exportPdf.test.tsx",
			"src/pages/Common/Reports/ReportView.exportPdf*.test.tsx",
			"src/components/Common/DataGrid/DataGridBase*.test.tsx",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
			// These read source text off disk, which in-place mutation rewrites; they kill no mutant.
			"**/*.enforcement.test.ts",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/, /@svar-ui\//],
			},
		},
		pool: "threads",
		isolate: true,
		// Coverage instrumentation pushes the slowest page tests past the 5 s default.
		testTimeout: 30000,
	},
});
