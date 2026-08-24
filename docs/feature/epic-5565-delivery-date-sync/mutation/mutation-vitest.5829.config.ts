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
		// specs that cover this slice's files are listed.
		include: [
			"src/models/Delivery.test.ts",
			"src/models/WorkItemRules.test.ts",
			"src/models/Delivery/DeliverySource.test.ts",
			"src/services/Api/DeliveryService.test.ts",
			"src/pages/Portfolios/Detail/PortfolioDeliveryView.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.edit.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.rulebased.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.sourceBinding.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.sourceValidation.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliverySelectionTabs.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.picker.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.preview.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.previewFailure.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.nameAndDate.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.optionList.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.provenance.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.archive.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.likelihoodCopy.test.tsx",
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
