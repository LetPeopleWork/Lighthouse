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
		// Stryker reruns this set for every mutant, so it lists only the suites that reach the band of
		// Features: the band itself, the hand-in hook, the tab-opening detector, and the Team and Portfolio
		// detail pages the band test renders. A suite left out makes the mutants it covers survive for want
		// of a run, not for want of a test.
		include: [
			"src/models/UsageData/featuresHeldOf.test.ts",
			"src/services/UsageData/usageDataEvents.featuresHeld.test.tsx",
			"src/services/UsageData/usageDataEvents.test.ts",
			"src/services/UsageData/usageDataReporter.test.ts",
			"src/pages/Settings/System/SystemSettingsTab.usageDataHandIn.test.tsx",
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
