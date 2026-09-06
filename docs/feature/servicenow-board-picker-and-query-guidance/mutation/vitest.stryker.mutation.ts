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
		// Stryker reruns the suite per mutant; sweeping all 288 files OOMs the node heap, so the
		// mutation run sees only the specs covering the mutated files (US 5610, both slices).
		include: [
			"src/models/Common/DataRetrievalSchemaDefaults.serviceNow.test.ts",
			"src/models/Common/serviceNowQueryGuidance.enforcement.test.ts",
			"src/components/DataRetrievalWizards/DataRetrievalWizardRegistry.test.ts",
			"src/components/DataRetrievalWizards/BoardWizard.test.tsx",
			"src/components/Common/BaseSettings/GeneralSettingsComponent.test.tsx",
			"src/components/Common/CreateWizards/CreateWizardShell.test.tsx",
			"src/components/Common/CreateWizards/CreateTeamWizard.test.tsx",
			"src/hooks/useCreateWizard.test.ts",
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
