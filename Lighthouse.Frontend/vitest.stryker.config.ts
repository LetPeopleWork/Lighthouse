// Vitest config used by Stryker mutation testing.
// Includes only the test files relevant to the rbac-enhancements feature
// to keep memory usage manageable during mutation runs.
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
			"src/services/Api/RbacService.test.ts",
			"src/components/Common/Authorization/ScopedGroupMappingManager.test.tsx",
			"src/hooks/useRbacGate.test.ts",
		],
		server: {
			deps: {
				inline: ["@mui/x-data-grid"],
			},
		},
		pool: "forks",
		isolate: true,
		fileParallelism: false,
	},
});
