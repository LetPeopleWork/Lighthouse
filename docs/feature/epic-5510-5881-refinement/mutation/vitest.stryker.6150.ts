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
		// specs covering the mutated files are listed, written from `ls`: a spec left out makes every
		// mutant in the code it covers survive for want of a test run, and the report cannot tell that
		// apart from a real gap.
		include: [
			"src/components/Common/FeatureListDataGrid/WarningsIndicator.test.tsx",
			"src/components/Common/WarningsIcon/WarningsIcon.test.tsx",
			"src/hooks/useVoterIdentity.test.ts",
			"src/services/Api/SizingLogService.test.ts",
			"src/pages/Teams/Detail/Refinement/refinementColumns.test.tsx",
			"src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx",
			"src/pages/Teams/Detail/Refinement/RefinementView.readiness.test.tsx",
			"src/pages/Teams/Detail/Refinement/RefinementView.stages.test.tsx",
			"src/pages/Teams/Detail/Refinement/RefinementView.test.tsx",
			"src/pages/Teams/Detail/Refinement/RefinementView.votes.test.tsx",
			"src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts",
			"src/pages/Teams/Detail/Refinement/useRefinement.test.tsx",
			"src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx",
			"src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx",
			"src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx",
			"src/pages/Teams/Detail/Refinement/VoteControl.test.tsx",
			"src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx",
			"src/pages/Teams/Detail/Refinement/voteWording.test.ts",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/, /@svar-ui\//],
			},
		},
		pool: "threads",
		isolate: true,
	},
});
