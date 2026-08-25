import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { createMockDeliveryService } from "../../../../../tests/MockApiServiceProvider";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";
import {
	allOptions,
	openSourceList,
	publishSwitch,
	RELEASE_44_IN_PROJECT_X,
	renderTab,
} from "./DeliverySourceTabFixture";

const licence = vi.hoisted(() => ({ isPremium: true }));

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		// Every term here is renamed away from the seeded default on purpose. A tenant who calls a
		// Portfolio a Value Stream must never be shown the word "Portfolio", and a mock that answers
		// with the default lets a hardcoded one through unnoticed.
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.DELIVERY]: "Launch",
				[TERMINOLOGY_KEYS.FEATURES]: "Deliverables",
				[TERMINOLOGY_KEYS.FEATURE]: "Deliverable",
				[TERMINOLOGY_KEYS.PORTFOLIO]: "Value Stream",
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: {
			hasLicense: licence.isPremium,
			isValid: licence.isPremium,
			canUsePremiumFeatures: licence.isPremium,
		},
		canCreateTeam: true,
		canUpdateTeamData: true,
		canUpdateTeamSettings: true,
		canUpdatePortfolioData: true,
		canUpdateAllTeamsAndPortfolios: licence.isPremium,
		createTeamTooltip: "",
		updateTeamDataTooltip: "",
		updateTeamSettingsTooltip: "",
		updatePortfolioDataTooltip: "",
		updateAllTeamsAndPortfoliosTooltip: "",
	}),
}));

vi.mock("../../../../../components/Common/FeatureGrid", () => ({
	FeatureGrid: ({ features }: { features: IFeature[] }) => (
		<div data-testid="feature-grid-mock">
			{features.map((f) => (
				<div key={f.id}>{f.name}</div>
			))}
		</div>
	),
}));

const aServiceOfferingEveryRelease = () => {
	const deliveryService = createMockDeliveryService();
	deliveryService.getDeliverySourceOptions = vi
		.fn()
		.mockResolvedValue(allOptions);
	deliveryService.previewDeliverySource = vi.fn().mockResolvedValue({
		name: "Release 44",
		date: new Date("2026-10-15T00:00:00Z"),
		features: [],
		emptyBecause: "None",
	});

	return deliveryService;
};

beforeEach(() => {
	vi.clearAllMocks();
	licence.isPremium = true;
	clearDeliverySourceOptionsCache();
});

describe("choosing whether the forecast is broadcast to the Release", () => {
	it("is off until somebody asks for it", async () => {
		renderTab(aServiceOfferingEveryRelease());

		await waitFor(() => {
			expect(publishSwitch()).not.toBeChecked();
		});
	});

	/**
	 * There is nowhere to broadcast to until a Release is picked. Offered anyway, the switch would
	 * take an answer about an entry the reader has not chosen and carry it into whichever one they
	 * choose next.
	 */
	it("cannot be switched on before a Release is picked", async () => {
		renderTab(aServiceOfferingEveryRelease());

		await waitFor(() => {
			expect(publishSwitch()).toBeDisabled();
		});
	});

	it("can be switched on once a Release is picked", async () => {
		const user = userEvent.setup();
		const onPublishForecastChange = vi.fn();
		renderTab(aServiceOfferingEveryRelease(), { onPublishForecastChange });

		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);

		await waitFor(() => {
			expect(publishSwitch()).toBeEnabled();
		});
		await user.click(publishSwitch());

		expect(onPublishForecastChange).toHaveBeenCalledWith(true);
	});

	it("can be switched off again", async () => {
		const user = userEvent.setup();
		const onPublishForecastChange = vi.fn();
		renderTab(aServiceOfferingEveryRelease(), {
			publishForecast: true,
			onPublishForecastChange,
		});

		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);
		await waitFor(() => {
			expect(publishSwitch()).toBeEnabled();
		});
		await user.click(publishSwitch());

		expect(onPublishForecastChange).toHaveBeenCalledWith(false);
	});

	/**
	 * Somebody switching this on is about to have Lighthouse write into a field their team also
	 * writes in. What it will and will not touch has to be readable before they decide, not after.
	 */
	it("says what will be written and what will be left alone", async () => {
		renderTab(aServiceOfferingEveryRelease());

		expect(
			await screen.findByText(
				/writes its own block into the Jira Release description/i,
			),
		).toBeInTheDocument();
		expect(
			screen.getByText(/Nothing else in the description is touched/i),
		).toBeInTheDocument();
	});
});
