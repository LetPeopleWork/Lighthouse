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
		// Stryker reruns this set for every mutant, so it lists only the suites that reach the files slice 01 changed.
		include: [
			"src/pages/Reports/**/*.test.{ts,tsx}",
			"src/pages/Common/Reports/**/*.test.{ts,tsx}",
			"src/services/Api/ReportService.test.ts",
			"src/components/App/Header/**/*.test.{ts,tsx}",
			"src/components/Common/StyledLink/**/*.test.{ts,tsx}",
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
