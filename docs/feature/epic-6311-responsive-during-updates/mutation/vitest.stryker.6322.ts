// @ts-nocheck
import { defineConfig } from "vitest/config";
import { EXPECTED_TEST_FAILURE } from "./src/tests/ExpectedTestFailure.ts";

export default defineConfig({
	test: {
		globals: true,
		onUnhandledError: (error) => error.name !== EXPECTED_TEST_FAILURE,
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
		// Stryker's coverage pass runs every included file; the whole suite OOMs the node heap, so the
		// mutation run sees only the specs that render the Task Manager or call the update service.
		include: [
			"src/components/App/Header/Header.featuresNav.test.tsx",
			"src/components/App/Header/Header.test.tsx",
			"src/components/App/Header/TaskManager/ActivitySection.test.tsx",
			"src/components/App/Header/TaskManagerIcon.cancelAll.test.tsx",
			"src/components/App/Header/TaskManagerIcon.test.tsx",
			"src/services/UpdateSubscriptionService.test.ts",
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
