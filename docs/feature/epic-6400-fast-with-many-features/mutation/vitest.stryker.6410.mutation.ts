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
		// Stryker reruns this set for every mutant, so it lists only the suites that reach the Portfolio size note and its service call.
		include: [
			"src/components/Common/ProjectSettings/PortfolioSizeNote.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.sizeNote.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx",
			"src/services/Api/PortfolioService.test.ts",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/, /@svar-ui\//],
			},
		},
		pool: "threads",
		isolate: true,
	},
});
