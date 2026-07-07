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
		include: [
			"src/utils/staleness/deriveStaleness.test.ts",
			"src/components/Common/BaseSettings/FlowMetricsConfigurationComponent.test.tsx",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/],
			},
		},
		pool: "forks",
		isolate: true,
		fileParallelism: false,
	},
});
