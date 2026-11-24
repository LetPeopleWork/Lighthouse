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
		// Only the specs that cover the mutated files run: the whole suite per mutant OOMs the node heap.
		include: [
			"src/components/Common/Connections/WriteConsentAlert.test.tsx",
			"src/components/Common/Connections/WriteConsentAlert.outcomes.test.tsx",
			"src/hooks/useOAuthSignIn.test.ts",
			"src/components/Common/Connection/ModifyConnectionSettings.signInRights.test.tsx",
			"src/components/Common/Connections/ReconnectBanner.test.tsx",
			"src/pages/Settings/Connections/ConnectionsSettings.test.tsx",
			"src/pages/Settings/Connections/ConnectionsSettings.list.test.tsx",
			"src/pages/Settings/Connections/ConnectionsSettings.delete.test.tsx",
			"src/pages/Settings/Connections/ConnectionsSettings.deleteFailure.test.tsx",
			"src/components/Common/Connection/ModifyConnectionSettings.test.tsx",
			"src/components/Common/Connection/ModifyConnectionSettings.directionTips.test.tsx",
			"src/components/Common/Connection/ModifyConnectionSettings.parentSources.test.tsx",
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
