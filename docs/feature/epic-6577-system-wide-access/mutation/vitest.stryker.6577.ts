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
		// Stryker reruns the suite per mutant; sweeping every spec runs the node heap out, so the mutation
		// run sees only the specs that cover the mutated files. A spec missing here leaves its mutants
		// alive for want of a run, which reads in the report exactly like a missing test.
		include: [
			"src/components/Common/Authorization/*.test.tsx",
			"src/pages/Settings/Rbac/*.test.tsx",
			"src/pages/Teams/Detail/TeamDetailAccessTab.test.tsx",
			"src/pages/Portfolios/Detail/PortfolioDetailAccessTab.test.tsx",
			"src/services/Api/RbacService.test.ts",
			"src/hooks/useRbac.test.ts",
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
