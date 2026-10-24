// @ts-nocheck
import { defineConfig } from "vitest/config";

const deliveryGrid = "src/pages/Portfolios/Detail/Components/DeliveryGrid";

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
		// Stryker reruns this set for every mutant, so it lists only the unit and component suites that render the
		// Delivery dialog, its source tab, the hooks behind them and the two Feature grid components slice 04 touches.
		// A suite left out makes the mutants it covers survive for want of a run, not for want of a test. The
		// acceptance suites and the Playwright specs stay out on purpose: this run must stay fast enough to repeat.
		include: [
			`${deliveryGrid}/DeliveryCreateModal.test.tsx`,
			`${deliveryGrid}/DeliveryCreateModal.edit.test.tsx`,
			`${deliveryGrid}/DeliveryCreateModal.rulebased.test.tsx`,
			`${deliveryGrid}/DeliveryCreateModal.sourceBinding.test.tsx`,
			`${deliveryGrid}/DeliveryCreateModal.sourceValidation.test.tsx`,
			`${deliveryGrid}/DeliveryCreateModal.neverAGuess.test.tsx`,
			`${deliveryGrid}/DeliveryCreateModal.loadsOnce.test.tsx`,
			`${deliveryGrid}/DeliveryCreateModal.featureListAndRuleFields.test.tsx`,
			`${deliveryGrid}/DeliveryCreateModal.licenceNeverKnown.test.tsx`,
			`${deliveryGrid}/DeliverySourceTab.test.tsx`,
			`${deliveryGrid}/DeliverySourceTab.nameAndDate.test.tsx`,
			`${deliveryGrid}/DeliverySourceTab.optionList.test.tsx`,
			`${deliveryGrid}/DeliverySourceTab.picker.test.tsx`,
			`${deliveryGrid}/DeliverySourceTab.preview.test.tsx`,
			`${deliveryGrid}/DeliverySourceTab.previewFailure.test.tsx`,
			`${deliveryGrid}/DeliverySourceTab.publishForecast.test.tsx`,
			`${deliveryGrid}/DeliverySourceTab.whyAndSearch.test.tsx`,
			`${deliveryGrid}/premiumVerdict.test.ts`,
			`${deliveryGrid}/dialogQueryStatus.test.ts`,
			`${deliveryGrid}/useDeliverySources.test.tsx`,
			`${deliveryGrid}/useDeliveryManagement.test.ts`,
			`${deliveryGrid}/deliverySelectionTabs.test.ts`,
			`${deliveryGrid}/ArchivedFeatureGrid.test.tsx`,
			"src/components/Common/FeatureGrid/FeatureGrid.test.tsx",
			"src/components/Common/FeatureSelector/FeatureSelector.test.tsx",
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
