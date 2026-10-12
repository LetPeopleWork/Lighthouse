// @ts-nocheck
import { defineConfig } from "vitest/config";

// Copy to Lighthouse.Frontend/vitest.stryker.6202.ts before the run; the frontend config points there.
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
		// Stryker runs the include set per mutant, so a full sweep OOMs the node heap. Only the specs
		// that exercise a mutated file are listed: a spec left out makes every mutant in the code it covers
		// survive for want of a test run, and the report cannot tell that apart from a real gap.
		include: [
			"src/components/Common/DataGrid/*.test.tsx",
			"src/components/Common/WorkItemsDialog/WorkItemsDialog.paging.test.tsx",
			"src/pages/Settings/SystemInfo/*.test.tsx",
			"src/pages/Teams/Detail/Refinement/*.test.tsx",
			"src/pages/Teams/Detail/Refinement/*.test.ts",
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
