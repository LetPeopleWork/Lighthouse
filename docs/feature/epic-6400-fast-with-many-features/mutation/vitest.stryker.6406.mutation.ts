// @ts-nocheck
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
		// Stryker reruns this set for every mutant, so it lists only the suites that reach the license check:
		// its hook, the one-asker guard, and the pages, wizards and settings forms that gate on it. A suite left out
		// makes the mutants it covers survive for want of a run, not for want of a test.
		include: [
			"src/hooks/useLicenseRestrictions.status.test.tsx",
			"src/hooks/useLicenseRestrictions.test.ts",
			"src/hooks/licenseStatusOneAsker.enforcement.test.ts",
			"src/pages/Overview/OverviewDashboard.licenseCheck.test.tsx",
			"src/components/Common/OnboardingStepper/OnboardingStepper.licenseCheck.test.tsx",
			"src/components/Common/OnboardingStepper/OnboardingStepper.test.tsx",
			"src/components/Common/CreateWizards/CreateTeamWizard.test.tsx",
			"src/components/Common/CreateWizards/CreateTeamWizard.licenseCheck.test.tsx",
			"src/components/Common/CreateWizards/CreatePortfolioWizard.test.tsx",
			"src/components/Common/CreateWizards/CreatePortfolioWizard.licenseCheck.test.tsx",
			"src/components/Common/CreateWizards/CreateWizardShell.test.tsx",
			"src/pages/Teams/Edit/EditTeam.test.tsx",
			"src/pages/Teams/Edit/EditTeam.licenseCheck.test.tsx",
			"src/pages/Portfolios/Edit/EditPortfolio.test.tsx",
			"src/pages/Portfolios/Edit/EditPortfolio.licenseCheck.test.tsx",
			"src/components/Common/LicenseStatus/LicenseStatusIcon.test.tsx",
			"src/components/App/License/LicenseToolTip.test.tsx",
			"src/components/Common/ValidationActions/SaveStateIndicator.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx",
			"src/components/Common/ProjectSettings/ModifyProjectSettings.sizeNote.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.refinement.test.tsx",
			"src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/, /@svar-ui\//],
			},
		},
		pool: "threads",
		isolate: true,
	},
});
