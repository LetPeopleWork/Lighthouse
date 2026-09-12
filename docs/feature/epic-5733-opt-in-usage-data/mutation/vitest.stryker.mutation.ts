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
		// Stryker reruns the suite per mutant; sweeping all ~356 files OOMs the node heap, so the
		// mutation run sees only the specs covering the mutated files (Epic 5733 slice 01a).
		include: [
			"src/components/UsageData/UsageDataIndicator.test.tsx",
			"src/components/UsageData/UsageDataDialog.test.tsx",
			"src/hooks/useUsageDataConsent.test.tsx",
			"src/services/Api/UsageDataService.test.ts",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
	},
});
