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
		// Stryker reruns the suite per mutant, and sweeping all 371 spec files OOMs the node heap - so
		// the mutation run sees only the specs that cover the mutated lines.
		//
		// A spec that covers mutated code and is missing from this list leaves every mutant in it alive
		// for want of a test RUN, which reads in the report exactly like a missing test. EditConnection
		// is here because it is what renders ModifyConnectionSettings for a saved connection.
		include: [
			"src/models/WorkTracking/ConnectionValidationResult.test.ts",
			"src/services/Api/WorkTrackingSystemService.test.ts",
			"src/services/Api/LogService.test.ts",
			"src/components/Common/Connection/ModifyConnectionSettings.test.tsx",
			"src/components/Common/Connection/CreateConnectionWizard.test.tsx",
			"src/pages/Connections/Edit/EditConnection.test.tsx",
			"src/pages/Settings/LogSettings/LogSettings.test.tsx",
			"src/pages/Settings/LogSettings/LighthouseLogViewer.test.tsx",
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
