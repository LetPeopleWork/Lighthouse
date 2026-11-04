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
		// Stryker reruns the suite per mutant; sweeping every spec file OOMs the node heap, so the
		// mutation run sees only the specs covering the board picker and the wizards that offer it.
		include: [
			"src/components/Common/BaseSettings/GeneralSettingsComponent.test.tsx",
			"src/components/Common/CreateWizards/CreateWizard.boardPicker.test.tsx",
			"src/components/Common/CreateWizards/CreateWizardShell.test.tsx",
			"src/components/Common/CreateWizards/CreateTeamWizard.test.tsx",
			"src/components/Common/CreateWizards/CreatePortfolioWizard.test.tsx",
			"src/components/DataRetrievalWizards/BoardWizard.test.tsx",
			"src/components/DataRetrievalWizards/DataRetrievalWizardRegistry.test.ts",
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
