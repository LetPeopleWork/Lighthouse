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
		// Stryker reruns the included suite per mutant, and sweeping every spec file OOMs the node
		// heap, so the mutation run sees only the specs covering the mutated files.
		include: [
			"src/pages/Common/MetricsView/overTimeEmptyState.test.ts",
			"src/pages/Common/MetricsView/PbcOverTimeWidget.test.tsx",
			"src/pages/Common/MetricsView/PercentilesOverTimeWidget.test.tsx",
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
