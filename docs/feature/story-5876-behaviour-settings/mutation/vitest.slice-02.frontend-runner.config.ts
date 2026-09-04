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
		// Stryker runs the whole include set per mutant, so a full sweep OOMs the node heap. Only the
		// specs that cover this slice's frontend surface are listed. A spec left out of this list makes
		// every mutant in the code it covers survive for want of a test run rather than for want of a
		// test, and the report cannot be told apart from a real gap.
		include: [
			"src/services/Terminology/resolveTerms.test.ts",
			"src/services/TerminologyContext.test.tsx",
			"src/components/Common/DataOverviewTable/DeliveriesChips.test.tsx",
			"src/services/Api/OptionalFeatureService.test.ts",
			"src/pages/Settings/System/SystemSettingsTab.behaviourSettings.test.tsx",
			"src/pages/Settings/System/SystemSettingsTab.test.tsx",
			"src/hooks/useFeatureOrdering.test.tsx",
			"src/hooks/useFeatureOrdering.moveGate.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx",
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
