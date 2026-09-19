import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

/**
 * Narrowed to the specs that cover the mutated files. Sweeping every spec exhausts the node heap
 * even at 8 GB, and the extra specs cannot kill a mutant in these two units anyway.
 *
 * The dialog's suite is here for the enlarge hook, whose only tests live in it.
 */
export default defineConfig({
	plugins: [react()],
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		include: [
			"src/utils/charts/sleRisk.test.ts",
			"src/components/Common/WorkItemsDialog/WorkItemsDialog.test.tsx",
		],
	},
});
