import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

/**
 * Narrows the suite to the specs that cover the one file this slice changed. Sweeping every spec
 * exhausts the node heap even at 8 GB, and the extra specs cannot kill a mutant in this file anyway.
 */
export default defineConfig({
	plugins: [react()],
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		include: ["src/hooks/useAgingBackground.test.ts"],
	},
});
