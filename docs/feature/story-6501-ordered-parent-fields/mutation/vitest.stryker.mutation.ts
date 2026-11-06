// @ts-nocheck
import { defineConfig } from "vitest/config";

export default defineConfig({
	test: {
		globals: true,
		onUnhandledError: (error) => error.name !== "ExpectedTestFailure",
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
			"src/components/Common/ConnectionDefault/*.test.{ts,tsx}",
			"src/components/Common/Connection/defaultsKeepingOnlyFields.test.ts",
			"src/components/Common/Connection/ModifyConnectionSettings*.test.tsx",
			"src/components/Common/Connection/UpdateQuestion.test.tsx",
			"src/components/Common/Connection/whatASaveAsks.test.ts",
			"src/components/Common/FormSelectField/*.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.usageData.test.tsx",
			"src/hooks/useModifySettings*.test.ts",
			"src/pages/Common/AdvancedInputs/*.test.tsx",
			"src/pages/Connections/Edit/*.test.tsx",
			"src/pages/Settings/SystemInfo/refreshFindingSentences.test.ts",
			"src/pages/Settings/SystemInfo/RefreshHistorySection.findings.test.tsx",
			"src/services/Api/WorkTrackingSystemService.test.ts",
			"src/services/Api/UsageDataService.test.ts",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/],
			},
		},
		pool: "threads",
		isolate: true,
	},
});
