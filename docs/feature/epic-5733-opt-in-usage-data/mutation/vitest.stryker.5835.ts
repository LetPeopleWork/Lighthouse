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
		// Stryker reruns the suite per mutant; sweeping all ~367 files OOMs the node heap, so the
		// mutation run sees only the specs covering the mutated files (Epic 5733 slice 02).
		//
		// SurveyNudge's own two specs are here even though the slice barely touches that component:
		// it now takes and yields the session's one prompt slot, and a mutant that broke the rest of
		// its eligibility would otherwise have nothing to kill it. Footer's spec is here because the
		// consent provider throws when no provider sits above a consumer, and Footer is one.
		include: [
			"src/services/UsageData/usageDataAskEligibility.test.ts",
			"src/services/UsageData/usageDataAskMarker.test.ts",
			"src/services/UsageData/promptSession.test.ts",
			"src/services/UsageData/usageDataEvents.test.ts",
			"src/components/UsageData/UsageDataAsk.test.tsx",
			"src/components/UsageData/UsageDataDialog.test.tsx",
			"src/components/UsageData/UsageDataIndicator.test.tsx",
			"src/components/SurveyNudge/SurveyNudge.test.tsx",
			"src/components/SurveyNudge/SurveyNudge.cadence.test.tsx",
			"src/components/SurveyNudge/SurveyNudge.promptSlot.test.tsx",
			"src/hooks/useUsageDataConsent.test.tsx",
			"src/hooks/UsageDataConsentProvider.test.tsx",
			"src/components/App/Footer/Footer.test.tsx",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
	},
});
