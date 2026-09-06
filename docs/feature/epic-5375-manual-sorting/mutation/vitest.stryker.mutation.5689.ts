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
		// Stryker's coverage runs the whole suite per mutant; sweeping all ~297 files OOMs the
		// node heap, so the mutation run sees only the specs covering the mutated files (Story 5689).
		include: [
			"src/hooks/useFeatureOrdering.test.tsx",
			"src/pages/Settings/System/FeatureOrderingSettings.test.tsx",
			"src/pages/Settings/System/SystemSettingsTab.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx",
			"src/components/Common/FeatureListDataGrid/columns.position.test.tsx",
			"src/services/Api/SettingsService.test.ts",
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
