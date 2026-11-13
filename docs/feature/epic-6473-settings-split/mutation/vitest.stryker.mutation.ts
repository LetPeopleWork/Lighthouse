// @ts-nocheck
// Copy to Lighthouse.Frontend/vitest.stryker.6473.mutation.ts before a run (stryker.6473.frontend.json names that
// path); `vitest/config` and `./setupTests.ts` resolve only from the frontend directory.
import { defineConfig } from "vitest/config";
import { EXPECTED_TEST_FAILURE } from "./src/tests/ExpectedTestFailure.ts";

export default defineConfig({
	test: {
		globals: true,
		onUnhandledError: (error) => error.name !== EXPECTED_TEST_FAILURE,
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
		// Stryker reruns the scoped suite per mutant; sweeping every spec file OOMs the node heap, so the
		// mutation run sees only the specs covering the files the Settings split changed (Epic 6473).
		include: [
			"src/models/Settings/settingsPaths.enforcement.test.ts",
			"src/models/Settings/settingsAreas.test.ts",
			"src/models/Settings/resolveSettingsLocation.test.ts",
			"src/pages/Settings/settingsAreas.test.tsx",
			"src/pages/Settings/settingsRoutes*.test.tsx",
			"src/pages/Settings/systemInfoPanels.test.tsx",
			"src/pages/Settings/SettingsAreaPage.test.tsx",
			"src/pages/Settings/Organization/*.test.tsx",
			"src/pages/Settings/System/LicenseTab.test.tsx",
			"src/pages/Settings/System/UsageDataTab.test.tsx",
			"src/pages/Settings/System/BlackoutSettings.test.tsx",
			"src/pages/Settings/System/BehaviourSettingsSection.test.tsx",
			"src/pages/Settings/Connections/ConnectionsSettings*.test.tsx",
			"src/pages/Settings/DemoData/DemoDataSettings.test.tsx",
			"src/pages/Connections/Edit/connectionEntryPoints.test.tsx",
			"src/pages/Connections/Edit/EditConnection*.test.tsx",
			"src/pages/Overview/ConnectFirstHint.test.tsx",
			"src/pages/Overview/OverviewDashboard*.test.tsx",
			"src/components/App/Header/Header*.test.tsx",
			"src/components/App/Header/SettingsMenu*.test.tsx",
			"src/components/App/Header/TaskManagerIcon*.test.tsx",
			"src/components/App/Footer/Footer.test.tsx",
			"src/components/UsageData/UsageDataAsk.test.tsx",
			"src/components/SurveyNudge/SurveyNudge.promptSlot.test.tsx",
			"src/components/App/Header/TaskManager/*.test.tsx",
			"src/components/Common/Connection/CreateConnectionWizard*.test.tsx",
			"src/components/Common/Connection/OAuthPremiumNotice*.test.tsx",
			"src/components/Common/DataOverviewTable/DataOverviewTable*.test.tsx",
			"src/components/Common/LicenseStatus/*.test.tsx",
			"src/components/Common/OnboardingStepper/*.test.tsx",
			"src/hooks/useBehaviourSettings*.test.ts?(x)",
			"src/hooks/useConfiguredConnections*.test.ts?(x)",
			"src/hooks/useLicenseRestrictions*.test.ts?(x)",
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
