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
		// Stryker's coverage runs the whole suite per mutant; sweeping all 282 files OOMs the
		// node heap, so the mutation run sees only the specs covering the mutated files (US 5611).
		include: [
			"src/models/Common/DataRetrievalSchemaDefaults.serviceNow.test.ts",
			"src/hooks/useCreateWizard.test.ts",
			"src/hooks/useModifySettings.test.ts",
			"src/hooks/useModifySettings.autosave.test.ts",
			"src/hooks/useModifySettings.conflict.test.ts",
			"src/components/Common/CreateWizards/CreateWizardShell.test.tsx",
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
