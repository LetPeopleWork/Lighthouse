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
		// run sees only the specs covering the mutated lines (Epic #5511 slice 07 / #6011).
		//
		// A spec that covers the mutated code and is missing from this list leaves every mutant in it
		// alive for want of a test RUN, which reads in the report exactly like a missing test. None of
		// the five files under TaskManager/ has a spec of its own - all are covered through
		// TaskManagerIcon, and Header is what renders the icon for somebody who is not an administrator.
		include: [
			"src/components/App/Header/TaskManagerIcon.test.tsx",
			"src/components/App/Header/Header.test.tsx",
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
