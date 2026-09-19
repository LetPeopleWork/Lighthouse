import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

/**
 * Narrowed to the specs that cover the mutated file. Sweeping every spec exhausts the node heap
 * even at 8 GB, and the extra specs cannot kill a mutant here anyway.
 */
export default defineConfig({
	plugins: [react()],
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		include: ["src/utils/charts/sleRisk.test.ts"],
	},
});
