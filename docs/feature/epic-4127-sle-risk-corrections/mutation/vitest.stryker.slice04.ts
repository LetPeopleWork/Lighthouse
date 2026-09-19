import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

/**
 * Narrowed to the specs covering the mutated regions. Sweeping every spec exhausts the node heap
 * even at 8 GB, and the extra specs cannot kill a mutant in these three anyway.
 */
export default defineConfig({
	plugins: [react()],
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		include: [
			"src/pages/Common/MetricsView/ragRules.test.ts",
			"src/pages/Common/MetricsView/SleRiskWidget.test.tsx",
			"src/utils/charts/sleRisk.test.ts",
		],
	},
});
