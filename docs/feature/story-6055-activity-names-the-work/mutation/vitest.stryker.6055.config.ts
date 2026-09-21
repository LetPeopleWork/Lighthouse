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
		// Stryker runs the whole include set per mutant, so a full sweep OOMs the node heap. Only the
		// specs that actually execute ActivitySection.tsx are listed, and the list was written from a
		// search for the symbol rather than from memory: a spec left out makes every mutant in the code
		// it covers survive for want of a test run rather than for want of a test, and the report cannot
		// tell those two apart.
		//
		// Both entries earn their place. ActivitySection.test.tsx renders the component directly.
		// TaskManagerIcon.test.tsx renders it through the popover and is the only spec that exercises
		// taskKey, which useTaskManagerPopover imports from the same file; drop it and every mutant in
		// taskKey survives silently.
		include: [
			"src/components/App/Header/TaskManager/ActivitySection.test.tsx",
			"src/components/App/Header/TaskManagerIcon.test.tsx",
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
