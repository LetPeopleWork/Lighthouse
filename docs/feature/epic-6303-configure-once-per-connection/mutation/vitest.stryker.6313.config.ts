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
		// Stryker runs the whole include set per mutant, so only the specs covering the mutated lines are
		// listed. A spec left out makes the mutants it covers survive for want of a run, not of a test.
		include: [
			"src/components/Common/Connection/EstimationDefaultEditor.test.tsx",
			"src/components/Common/Connection/ModifyConnectionSettings.test.tsx",
			"src/components/Common/Connection/UpdateQuestion.test.tsx",
			"src/components/Common/Connection/CreateConnectionWizard.test.tsx",
			"src/components/Common/ConnectionDefault/connectionDefaultEntry.test.ts",
			"src/components/Common/EstimationField/EstimationFieldComponent.test.tsx",
			"src/components/Common/ProjectSettings/Advanced/DependenciesComponent.test.tsx",
			"src/components/Common/ProjectSettings/Advanced/FeatureSizeComponent.test.tsx",
			"src/components/Common/ProjectSettings/Advanced/OwnershipComponent.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.test.tsx",
			"src/models/WorkTracking/WorkTrackingSystemConnection.serviceNow.test.ts",
			"src/pages/Common/AdvancedInputs/AdvancedInputsComponent.test.tsx",
			"src/pages/Common/AdvancedInputs/AdvancedInputs.connectionDefault.test.tsx",
			"src/pages/Connections/Edit/EditConnection.test.tsx",
			"src/pages/Connections/Edit/EditConnection.defaults.test.tsx",
			"src/pages/Connections/Edit/EditConnection.fieldChange.test.tsx",
			"src/pages/Portfolios/Edit/EditPortfolio.test.tsx",
			"src/pages/Teams/Edit/EditTeam.test.tsx",
			"src/services/Api/WorkTrackingSystemService.test.ts",
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
