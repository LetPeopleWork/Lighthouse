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
		// specs that cover this slice's frontend surface are listed.
		include: [
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.publishForecast.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.picker.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.sourceBinding.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliverySelectionTabs.test.ts",
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
