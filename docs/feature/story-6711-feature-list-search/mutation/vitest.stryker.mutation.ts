// @ts-nocheck
import { defineConfig } from "vitest/config";

// Copied to Lighthouse.Frontend/vitest.stryker.6711.ts for the run. Stryker reruns the suite per mutant,
// so it sees only the specs that cover the mutated files.
export default defineConfig({
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		env: { VITE_API_SERVICE_TYPE: "DEMO" },
		css: { modules: { classNameStrategy: "non-scoped" } },
		include: [
			"src/components/Common/DataGrid/**/*.test.{ts,tsx}",
			"src/components/Common/FeatureListDataGrid/**/*.test.{ts,tsx}",
			"src/pages/Common/featureListSearch.everyList.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryFeatureRow.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryFeatureRow.teamText.test.tsx",
			"src/pages/Teams/Detail/TeamFeatureList*.test.tsx",
			"src/pages/Features/**/*.test.{ts,tsx}",
		],
		exclude: ["**/node_modules/**", "**/dist/**", "**/.stryker-tmp*/**", "**/StrykerOutput/**"],
		server: { deps: { inline: [/@mui\//, /react-transition-group/] } },
		pool: "threads",
		isolate: true,
	},
});
