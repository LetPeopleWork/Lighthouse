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
		// Stryker reruns the suite per mutant; sweeping every spec OOMs the node heap, so the
		// mutation run sees only the specs covering the mutated files.
		include: [
			"src/utils/stateMappingValidation.test.ts",
			"src/hooks/useModifySettings.test.ts",
			"src/hooks/useModifySettings.autosave.test.ts",
			"src/hooks/useModifySettings.conflict.test.ts",
			"src/hooks/useModifySettings.validation.test.ts",
			"src/hooks/useModifySettings.nullableFields.test.ts",
			"src/components/Common/Team/ModifyTeamSettings.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx",
			"src/components/Common/StateMappings/CycleTimesEditor.test.tsx",
			"src/components/Common/StateMappings/WaitStatesEditor.test.tsx",
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
