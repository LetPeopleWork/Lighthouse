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
		// specs covering this slice's surface are listed, and the list is written from `ls` rather
		// than from memory: a spec left out makes every mutant in the code it covers survive for want
		// of a test run rather than for want of a test, and the report cannot tell that apart from a
		// real gap. A spec named here that does not exist has the same effect, from the other side.
		//
		// The colour key and the preference store each have a spec of their own now. They did not
		// on the first run, and being reachable only through the tab is most of why they came back
		// at 20 % and 59 % - a component's own decisions are hard to reach through something that
		// composes it.
		//
		// The dependency overlay's spec is here although this slice never opened it, because the
		// translation layer and the bar's content carry the previous slice's code as well as this
		// one's, and mutants land in both.
		include: [
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/deliveryTeamLanes.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/deliveryTimelineModel.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/TimelineTeamLegend.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/useShowTeams.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/ganttShapes.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/DeliveryGanttChart.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/DeliveryGanttChart.config.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/TimelineBarContent.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/DeliveryTimelineTab.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/deliveryDependencyOverlay.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/ganttAdapterBoundary.enforcement.test.ts",
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
