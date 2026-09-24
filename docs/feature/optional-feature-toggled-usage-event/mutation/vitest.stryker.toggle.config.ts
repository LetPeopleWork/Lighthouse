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
		// Stryker runs the whole include set for every mutant, so only the specs that execute the
		// mutated code are listed. The two usageData specs drive the new call site and the key mapping;
		// the two existing settings-tab specs are what would notice the switch itself breaking.
		include: [
			"src/pages/Settings/System/SystemSettingsTab.usageData.test.tsx",
			"src/pages/Settings/System/SystemSettingsTab.usageDataHandIn.test.tsx",
			"src/pages/Settings/System/SystemSettingsTab.test.tsx",
			"src/pages/Settings/System/SystemSettingsTab.behaviourSettings.test.tsx",
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
