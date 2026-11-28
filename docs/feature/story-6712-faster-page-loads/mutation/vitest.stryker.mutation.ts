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
		// Stryker's coverage runs the whole suite per mutant; sweeping the whole suite OOMs the
		// node heap, so the mutation run sees only the specs covering the mutated files (Story 6712).
		include: [
			"src/hooks/usePageFreshQuery.test.tsx",
			"src/components/App/LetPeopleWork/LighthouseVersion.notification.test.tsx",
			"src/hooks/useRbac.sharedAnswer.test.tsx",
			"src/hooks/useRbac.test.ts",
			"src/hooks/useRbacGate.test.ts",
			"src/hooks/useUnitLists.test.tsx",
			"src/hooks/unitListsOwnChanges.test.tsx",
			"src/hooks/useLicenseRestrictions.test.ts",
			"src/hooks/useLicenseRestrictions.status.test.tsx",
			"src/components/App/LetPeopleWork/LighthouseVersion.test.tsx",
			"src/components/App/LetPeopleWork/LighthouseVersion.updateCheck.test.tsx",
			"src/components/Common/LicenseStatus/LicenseStatusPopover.test.tsx",
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
