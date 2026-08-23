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
		// Stryker reruns the suite per mutant; sweeping every spec file OOMs the node heap, so the
		// mutation run sees only the specs covering the mutated files. A spec missing from this list
		// means every mutant in the code it covers survives for want of a test run, which reads in
		// the report exactly like a real gap.
		include: [
			"src/models/Delivery/DeliverySource.test.ts",
			"src/services/Api/DeliveryService.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliverySelectionTabs.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.edit.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.rulebased.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/useDeliveryManagement.test.ts",
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
