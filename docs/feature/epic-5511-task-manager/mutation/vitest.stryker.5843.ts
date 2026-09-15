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
		// Stryker reruns the suite per mutant; sweeping every spec OOMs the node heap, so the mutation
		// run sees only the specs covering the mutated lines (Epic #5511 slice 06 / #5843).
		//
		// A spec that covers the mutated code and is missing from this list leaves every mutant in it
		// alive for want of a test RUN, which reads in the report exactly like a missing test.
		// RecentProblemsSection has no spec of its own - it is covered through TaskManagerIcon, which is
		// where the popover's other two sections are covered too. Listing a file that does not exist
		// leaves the runner with nothing to run and reports every mutant as alive.
		include: [
			"src/components/App/Header/TaskManagerIcon.test.tsx",
			"src/components/App/Header/UpdateAllButton.test.tsx",
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
