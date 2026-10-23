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
		// Stryker's coverage runs the whole suite per mutant; sweeping every spec OOMs the node
		// heap, so the mutation run sees only the specs covering the mutated files.
		include: [
			"src/components/Common/ConnectionDefault/*.test.{ts,tsx}",
			"src/components/Common/Connection/ModifyConnectionSettings*.test.tsx",
			"src/components/Common/FormSelectField/*.test.tsx",
			"src/components/Common/ProjectSettings/Advanced/DependenciesComponent*.test.tsx",
			"src/components/Common/EstimationField/EstimationFieldComponent.test.tsx",
			"src/pages/Common/AdvancedInputs/*.test.tsx",
			"src/pages/Common/MetricsView/WidgetShell*.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab*.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal*.test.tsx",
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
