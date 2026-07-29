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
		// Stryker's perTest coverage runs the whole suite per mutant; sweeping all 282 files
		// OOMs the 4 GB node heap, so the mutation run sees only the specs that cover the
		// mutated files (Bug #5586).
		include: [
			"src/components/Common/Forecasts/ForecastLikelihood.test.tsx",
			"src/models/Forecasts/ManualForecast.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.likelihoodCopy.test.tsx",
			"src/pages/Portfolios/Detail/PortfolioFeatureList.test.tsx",
			"src/pages/Teams/Detail/ManualForecaster.test.tsx",
			"src/pages/Teams/Detail/NewItemForecaster.test.tsx",
			"src/pages/Teams/Detail/TeamForecastView.test.tsx",
			"src/pages/Teams/Detail/TeamForecastView.autorun.test.tsx",
			"src/services/Api/ForecastService.test.ts",
			"src/utils/forecast/cannotForecast.test.ts",
			"src/utils/forecast/formatLikelihood.enforcement.test.ts",
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
