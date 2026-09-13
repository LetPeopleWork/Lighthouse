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
		// Stryker reruns the suite per mutant; sweeping all ~361 files OOMs the node heap, so the
		// mutation run sees only the specs covering the mutated files. Footer's spec is here because
		// the consent provider throws when no provider is above a consumer, and Footer is one.
		include: [
			"src/services/UsageData/usageDataEvents.test.ts",
			"src/services/UsageData/usageDataBuffer.test.ts",
			"src/services/UsageData/usageDataRouteKeys.test.ts",
			"src/hooks/useUsageDataConsent.test.tsx",
			"src/hooks/UsageDataConsentProvider.test.tsx",
			"src/services/Api/UsageDataService.test.ts",
			"src/components/App/Footer/Footer.test.tsx",
			"src/components/UsageData/UsageDataDialog.test.tsx",
			"src/components/UsageData/UsageDataIndicator.test.tsx",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
	},
});
