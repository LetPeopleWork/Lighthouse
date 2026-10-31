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
		// Stryker reruns this set for every mutant, so it lists only the suites that reach the state-mapping
		// reconciliation and validation helpers, the mappings editor and the team and portfolio settings forms.
		include: [
			"src/utils/stateMappingReconciliation.test.ts",
			"src/utils/stateMappingValidation.test.ts",
			"src/components/Common/StateMappings/StateMappingsEditor.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
			"**/*.enforcement.test.ts",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/, /@svar-ui\//],
			},
		},
		pool: "threads",
		isolate: true,
		testTimeout: 30000,
	},
});
