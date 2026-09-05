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
		// Stryker reruns every included spec for each mutant, so the run is split by subject:
		// this half sees only the specs covering the blocked trend and the two over-time hooks.
		include: [
			"src/pages/Common/MetricsView/blockedTrend.test.ts",
			"src/pages/Common/MetricsView/usePbcOverTime.test.ts",
			"src/pages/Common/MetricsView/usePercentilesOverTime.test.ts",
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
