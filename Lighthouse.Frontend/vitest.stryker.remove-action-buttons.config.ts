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
		include: [
			"src/hooks/useModifySettings.test.ts",
			"src/hooks/useModifySettings.autosave.test.ts",
			"src/components/Common/ValidationActions/SaveStateIndicator.test.tsx",
			"src/components/Common/StateMappings/ReloadDependentDataAction.test.tsx",
		],
		server: {
			deps: {
				inline: ["@mui/x-data-grid"],
			},
		},
		pool: "forks",
		isolate: true,
		fileParallelism: false,
	},
});
