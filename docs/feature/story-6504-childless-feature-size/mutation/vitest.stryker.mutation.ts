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
		// Stryker's coverage runs the whole suite per mutant; sweeping all files OOMs the
		// node heap, so the mutation run sees only the specs covering the mutated files (Story 6504).
		include: [
			"src/components/Common/ProjectSettings/Advanced/FeatureSizeComponent.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.usageData.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.sizeNote.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx",
			"src/models/Feature.test.ts",
			"src/models/Feature.decode.test.ts",
			"src/utils/features/featureWarningSentences.test.ts",
			"src/components/Common/FeatureListDataGrid/WarningsIndicator.test.tsx",
			"src/components/Common/FeatureListDataGrid/columns.warnings.test.tsx",
			"src/components/Common/FeatureListDataGrid/columns.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureProgressIndicator.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx",
			"src/components/Common/ProgressIndicator/ProgressIndicator.test.tsx",
			"src/components/Common/CreateWizards/CreatePortfolioWizard.test.tsx",
			"src/components/Common/ConnectionDefault/ConnectionDefaultFieldSelect.noTip.test.tsx",
			"src/components/Common/ItemListManager/ItemListManager.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryExportTable.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryExportTable.archived.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryExportTable.beyondTheHorizon.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/deliveryBarMarks.test.ts",
			"src/pages/Portfolios/Detail/PortfolioFeatureList.test.tsx",
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
