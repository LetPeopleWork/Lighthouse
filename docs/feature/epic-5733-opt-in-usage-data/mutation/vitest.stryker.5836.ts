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
		// mutation run sees only the specs covering the mutated files (Epic 5733 slice 03).
		//
		// Footer's spec is here because the consent provider throws when no provider sits above a
		// consumer and Footer is one - and because Footer is what passes administratorDisabled to
		// the dialog, so a mutant that dropped the wiring would otherwise have nothing to kill it.
		include: [
			"src/services/UsageData/usageDataAdminVetoCopy.test.ts",
			"src/components/UsageData/UsageDataIndicator.test.tsx",
			"src/components/UsageData/UsageDataDialog.test.tsx",
			"src/components/UsageData/UsageDataAsk.test.tsx",
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
