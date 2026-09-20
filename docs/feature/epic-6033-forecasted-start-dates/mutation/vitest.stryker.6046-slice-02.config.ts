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
		// specs that cover this slice's frontend surface are listed. A spec left out of this list makes
		// every mutant in the code it covers survive for want of a test run rather than for want of a
		// test, and the report cannot be told apart from a real gap.
		include: [
			"src/models/Feature.test.ts",
			"src/models/Feature.decode.test.ts",
			"src/components/Common/FeatureListDataGrid/columns.test.tsx",
			"src/components/Common/FeatureListDataGrid/columns.forecastedStart.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx",
			"src/pages/Teams/Detail/TeamFeatureList.test.tsx",
			"src/pages/Portfolios/Detail/PortfolioFeatureList.test.tsx",
			"src/pages/Features/FeaturesView.test.tsx",
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
