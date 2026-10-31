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
		// Stryker reruns this set for every mutant, so it lists only the suites that reach the lines changed after the
		// last slice's run: the Reports page and its bulk wizard and dialogs, the owner helpers, the refusal copy, and
		// the printed page for its one table-layout line.
		// ActionButton.test.tsx stays out: one of its tests leaves a rejected promise unhandled, which crashes the dry run.
		include: [
			"src/pages/Reports/*.test.tsx",
			"src/pages/Common/Reports/reportOwner.test.ts",
			"src/pages/Common/Reports/ReportsTab.test.tsx",
			"src/pages/Common/Reports/wizard/reportRefusalCopy.test.ts",
			"src/pages/Common/Reports/print/ReportPrintPage*.test.tsx",
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
