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
		// Stryker's per-test run sweeps the whole suite per mutant; all 327 files OOMs the 4 GB
		// node heap, so the mutation run sees only the specs that cover the mutated files.
		include: [
			"src/models/Delivery/ArchivedDelivery.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/ArchiveConfirmationDialog.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/ArchivedDeliveriesSection.record.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/ArchivedDeliveriesSection.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/ArchivedFeatureGrid.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryNotesPanel.archived.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.archive.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.metrics.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryExportTable.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryExportTable.archived.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/useDeliveryManagement.test.ts",
			"src/pages/Portfolios/Detail/PortfolioDeliveryView.archive.test.tsx",
			"src/pages/Portfolios/Detail/PortfolioDeliveryView.test.tsx",
			"src/services/Api/DeliveryService.test.ts",
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
