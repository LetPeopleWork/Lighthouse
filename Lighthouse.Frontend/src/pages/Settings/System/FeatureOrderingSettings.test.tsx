import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ITerminology } from "../../../models/Terminology";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ISettingsService } from "../../../services/Api/SettingsService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockSettingsService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import FeatureOrderingSettings from "./FeatureOrderingSettings";

// Epic 5375 slice 02 — US-02 AC-2.5, US-05 AC-5.4/AC-5.5 and D16. The switch that decides who owns the
// order, the affordance that says why it is unavailable, and the sentence that makes turning it off a
// thing somebody would dare try.
const renderTheSwitch = (options: {
	settingsService: ISettingsService;
	isPremium: boolean;
	featuresAreCalled?: string;
}) => {
	const terminology: ITerminology[] = [
		{
			id: 1,
			key: TERMINOLOGY_KEYS.FEATURES,
			defaultValue: "Features",
			value: options.featuresAreCalled ?? "Features",
			description: "Term used for multiple features",
		},
	];

	const terminologyService = createMockTerminologyService();
	terminologyService.getAllTerminology = vi.fn().mockResolvedValue(terminology);

	const apiContext = createMockApiServiceContext({
		settingsService: options.settingsService,
		terminologyService,
	});

	return render(
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider value={apiContext}>
				<TerminologyProvider>
					<FeatureOrderingSettings isPremium={options.isPremium} />
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

const aLicensedInstanceFollowingTheTracker = () => {
	const settingsService = createMockSettingsService();
	settingsService.getFeatureOrdering = vi.fn().mockResolvedValue("SourceOrder");
	settingsService.updateFeatureOrdering = vi.fn().mockResolvedValue(undefined);
	return settingsService;
};

describe("FeatureOrderingSettings — who owns the order", () => {
	it("shows the tracker owning the order until somebody decides otherwise", async () => {
		const settingsService = aLicensedInstanceFollowingTheTracker();

		renderTheSwitch({ settingsService, isPremium: true });

		const toggle = await screen.findByTestId("feature-ordering-toggle");
		await waitFor(() => {
			expect(toggle.querySelector("input")).not.toBeChecked();
		});
	});

	it("hands the order to this instance when the switch is turned on", async () => {
		const settingsService = aLicensedInstanceFollowingTheTracker();

		renderTheSwitch({ settingsService, isPremium: true });

		const toggle = await screen.findByTestId("feature-ordering-toggle");
		await userEvent.click(toggle);

		await waitFor(() => {
			expect(settingsService.updateFeatureOrdering).toHaveBeenCalledWith(
				"ManualOrder",
			);
		});
	});

	it("gives the order back to the tracker when the switch is turned off", async () => {
		const settingsService = aLicensedInstanceFollowingTheTracker();
		settingsService.getFeatureOrdering = vi
			.fn()
			.mockResolvedValue("ManualOrder");

		renderTheSwitch({ settingsService, isPremium: true });

		const toggle = await screen.findByTestId("feature-ordering-toggle");
		await userEvent.click(toggle);

		await waitFor(() => {
			expect(settingsService.updateFeatureOrdering).toHaveBeenCalledWith(
				"SourceOrder",
			);
		});
	});

	// AC-2.5 — the view is free, the ownership is not. An unlicensed instance must be refused visibly,
	// not silently: a switch that flips and does nothing is worse than one that will not flip.
	it("cannot be flipped on an instance without a premium licence", async () => {
		const settingsService = aLicensedInstanceFollowingTheTracker();

		renderTheSwitch({ settingsService, isPremium: false });

		const toggle = await screen.findByTestId("feature-ordering-toggle");
		expect(toggle.querySelector("input")).toBeDisabled();
		expect(settingsService.updateFeatureOrdering).not.toHaveBeenCalled();
	});

	// AC-5.5 — the sentence that turns a one-way door into an experiment. Naming the retention is the
	// whole point: without it, nobody flips the switch on an instance whose forecasts are being read.
	it("says the places this instance chose are kept if the order is given back", async () => {
		const settingsService = aLicensedInstanceFollowingTheTracker();

		renderTheSwitch({ settingsService, isPremium: true });

		const helpText = await screen.findByTestId("feature-ordering-help-text");
		expect(helpText.textContent).toMatch(/kept|keep|retain/i);
	});

	// D16 — an instance that calls them Deliverables must be told about its Deliverables. A hard-coded
	// "Features" passes every test above and fails this one.
	it("wears the word this instance uses for its features", async () => {
		const settingsService = aLicensedInstanceFollowingTheTracker();

		renderTheSwitch({
			settingsService,
			isPremium: true,
			featuresAreCalled: "Deliverables",
		});

		const helpText = await screen.findByTestId("feature-ordering-help-text");

		// The terminology arrives on its own request, so the panel renders once with the seeded default
		// before it lands. What matters is what it settles on.
		await waitFor(() => {
			expect(helpText.textContent).toContain("Deliverables");
		});
		expect(helpText.textContent).not.toContain("Features");

		const toggle = await screen.findByTestId("feature-ordering-toggle");
		expect(toggle.textContent).toContain("Deliverables");
		expect(toggle.textContent).not.toContain("Features");
	});

	// Flipping twice before the first answer lands would send the instance a decision it never made.
	it("cannot be flipped again while the first flip is still in flight", async () => {
		const settingsService = aLicensedInstanceFollowingTheTracker();
		let finishTheFirstFlip: () => void = () => {};
		settingsService.updateFeatureOrdering = vi.fn().mockReturnValue(
			new Promise<void>((resolve) => {
				finishTheFirstFlip = resolve;
			}),
		);

		renderTheSwitch({ settingsService, isPremium: true });

		const toggle = await screen.findByTestId("feature-ordering-toggle");
		await userEvent.click(toggle);

		await waitFor(() => {
			expect(toggle.querySelector("input")).toBeDisabled();
		});

		finishTheFirstFlip();
		await waitFor(() => {
			expect(toggle.querySelector("input")).not.toBeDisabled();
		});
		expect(settingsService.updateFeatureOrdering).toHaveBeenCalledTimes(1);
	});
});
