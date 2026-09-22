import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

/**
 * Narrowed to the specs that can actually kill a mutant in sleRisk.ts. Sweeping every spec exhausts
 * the node heap even at 8 GB.
 *
 * The dialog's own suite is deliberately absent: it builds a hand-written descriptor stub rather
 * than calling this module, so it cannot kill anything here.
 */
export default defineConfig({
	plugins: [react()],
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		include: [
			"src/utils/charts/sleRisk.test.ts",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
			"src/components/Common/Charts/WorkItemAgingChart.test.tsx",
		],
	},
});
