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
		include: [
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/deliveryTimelineModel.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/DeliveryGanttChart.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/DeliveryTimelineTab.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/ganttAdapterBoundary.enforcement.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/TimelineLegend.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/timeline/ganttShapes.test.ts",
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
