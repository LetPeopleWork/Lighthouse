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
		// Stryker reruns the suite per mutant; sweeping all ~370 files OOMs the node heap, so the
		// mutation run sees only the specs covering the mutated files (Epic 5733 slice 04).
		//
		// The consent specs are here because the reporter reads its answer from that provider, and
		// because withdrawing has to discard what the buffer is holding - a mutant that dropped that
		// discard would otherwise have nothing to kill it.
		include: [
			"src/services/UsageData/usageDataReporter.test.ts",
			"src/services/UsageData/usageDataRouteKeys.test.ts",
			"src/services/UsageData/usageDataBuffer.test.ts",
			"src/services/UsageData/usageDataEvents.test.ts",
			"src/hooks/useUsageDataConsent.test.tsx",
			"src/hooks/UsageDataConsentProvider.test.tsx",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
	},
});
