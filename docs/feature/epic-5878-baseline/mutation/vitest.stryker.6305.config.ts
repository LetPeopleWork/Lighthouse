// @ts-nocheck
import { defineConfig } from "vitest/config";

// Copy to Lighthouse.Frontend/ before running Stryker; the root pattern is gitignored there.
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
		// Stryker reruns the suite per mutant; sweeping every spec OOMs the node heap, so the run sees
		// only the specs covering the mutated files.
		include: [
			"src/components/Common/DataOverviewTable/*.test.tsx",
			"src/pages/Overview/OverviewDashboard.test.tsx",
			"src/pages/Common/Reports/ReportsTab*.test.tsx",
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
