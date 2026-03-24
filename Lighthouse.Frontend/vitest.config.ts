import { defineConfig } from "vitest/config";

export default defineConfig({
	test: {
		globals: true,
		environment: "jsdom",
		env: {
			VITE_API_SERVICE_TYPE: "DEMO",
		},
		css: {
			modules: {
				classNameStrategy: "non-scoped",
			},
		},
		coverage: {
			reporter: ["text", "lcov"],
		},
		reporters: [
			"default",
			["vitest-sonar-reporter", { outputFile: "sonar-report.xml" }],
		],
		server: {
			deps: {
				inline: ["@mui/x-data-grid"],
			},
		},

		// GitHub Actions optimized settings
		pool: "threads",
		isolate: true,
		maxWorkers: process.env.CI ? undefined : 4,
		fileParallelism: true,

		// CI-specific optimizations
		...(process.env.CI && {
			// Reduce memory usage on CI
			pool: "forks", // More stable than threads on CI
			bail: 1, // Optional: fail fast
		}),
	},
});
