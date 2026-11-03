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
		// mutation run sees only the specs covering the mutated files.
		include: [
			"src/components/Common/Charts/CumulativeStateTimeChart.test.tsx",
			"src/components/Common/ItemListManager/ItemListManager.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.sizeNote.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx",
			"src/components/Common/StateMappings/CycleTimesEditor.test.tsx",
			"src/components/Common/StateMappings/StateMappingsEditor.rawLists.test.tsx",
			"src/components/Common/StateMappings/StateMappingsEditor.test.tsx",
			"src/components/Common/StateMappings/WaitStatesEditor.test.tsx",
			"src/components/Common/StatesList/StatesList.stateMappings.test.tsx",
			"src/components/Common/StatesList/StatesList.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.refinement.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.test.tsx",
			"src/hooks/useModifySettings.autosave.test.ts",
			"src/hooks/useModifySettings.conflict.test.ts",
			"src/hooks/useModifySettings.nullableFields.test.ts",
			"src/hooks/useModifySettings.test.ts",
			"src/hooks/useModifySettings.validation.test.ts",
			"src/pages/Common/MetricsView/BaseMetricsView.cumulativeStateTimeDialog.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
			"src/pages/Portfolios/Detail/PortfolioMetricsView.agingColumns.test.tsx",
			"src/pages/Teams/Detail/TeamMetricsView.agingColumns.test.tsx",
			"src/pages/Teams/Detail/TeamMetricsView.test.tsx",
			"src/utils/flowEfficiency.test.ts",
			"src/utils/isCycleTimeDefinitionValid.test.ts",
			"src/utils/stateMappingValidation.rawLists.test.ts",
			"src/utils/stateMappingValidation.storedOwner.test.ts",
			"src/utils/stateMappingValidation.test.ts",
			"src/utils/stateMappingValidation.waitStates.test.ts",
			"src/utils/stateMappingView.test.ts",
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
