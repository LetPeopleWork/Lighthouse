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
		// specs covering the mutated files are listed, and the list is written from `ls` rather than
		// from memory: a spec left out makes every mutant in the code it covers survive for want of a
		// test run rather than for want of a test, and the report cannot tell that apart from a real
		// gap. A spec named here that does not exist has the same effect, from the other side.
		include: [
			"src/pages/Teams/Detail/realityCheckGrading.test.ts",
			"src/pages/Teams/Detail/realityCheckCopy.test.ts",
			"src/pages/Teams/Detail/TeamForecastView.realityCheck.dialog.test.tsx",
			"src/pages/Teams/Detail/TeamForecastView.realityCheck.grading.test.tsx",
			"src/pages/Teams/Detail/TeamForecastView.realityCheck.usageData.test.tsx",
			"src/pages/Teams/Detail/TeamForecastView.realityCheck.trigger.test.tsx",
			"src/pages/Teams/Detail/TeamForecastView.realityCheck.compact.test.tsx",
			"src/pages/Teams/Detail/TeamForecastView.realityCheck.summary.test.tsx",
			"src/utils/theme/colors.test.ts",
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
