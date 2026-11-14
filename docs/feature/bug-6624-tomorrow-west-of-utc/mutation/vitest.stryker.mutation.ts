// @ts-nocheck
import { defineConfig } from "vitest/config";

// The launch-date rule only goes wrong west of UTC, so the mutation run gives every spec a New York
// clock, in forked processes so the zone is set per process. Both specs pass there once the rule is fixed.
export default defineConfig({
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		env: {
			VITE_API_SERVICE_TYPE: "DEMO",
			TZ: "America/New_York",
		},
		css: {
			modules: {
				classNameStrategy: "non-scoped",
			},
		},
		include: [
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryFormRules.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryFormRules.westOfUtc.test.ts",
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
		pool: "forks",
		isolate: true,
	},
});
