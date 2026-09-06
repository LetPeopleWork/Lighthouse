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
		// Stryker's coverage runs the whole suite per mutant; sweeping all 288 files OOMs the
		// node heap, so the mutation run sees only the specs covering the mutated file (Bug #5628).
		include: [
			"src/hooks/useModifySettings.validation.test.ts",
			"src/hooks/useModifySettings.autosave.test.ts",
			"src/hooks/useModifySettings.conflict.test.ts",
			"src/hooks/useModifySettings.test.ts",
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
