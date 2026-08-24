import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { createMockDeliveryService } from "../../../../../tests/MockApiServiceProvider";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";
import {
	allOptions,
	JIRA_FIX_VERSION_SOURCE,
	JIRA_RELEASE_SOURCE,
	openSourceList,
	RELEASE_44_IN_PROJECT_X,
	renderModal,
	selectionModeButtons,
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

beforeEach(() => {
	vi.clearAllMocks();
	licence.isPremium = true;
	clearDeliverySourceOptionsCache();
});

describe("DeliverySourceTab registration", () => {
	it("shows only the two built-in tabs when the connection offers no source", async () => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi.fn().mockResolvedValue([]);

		renderModal(deliveryService);

		await waitFor(() => {
			expect(deliveryService.getDeliverySources).toHaveBeenCalledWith(1);
		});

		expect(selectionModeButtons().map((b) => b.textContent)).toEqual([
			"Manual",
			"Rule-Based",
		]);
	});

	it("renders one further tab per source the server reports", async () => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE, JIRA_FIX_VERSION_SOURCE]);

		renderModal(deliveryService);

		await waitFor(() => {
			expect(selectionModeButtons().map((b) => b.textContent)).toEqual([
				"Manual",
				"Rule-Based",
				"Jira Release",
				"Jira Fix Version",
			]);
		});
	});

	it("shows the tab with a notice naming Release selection on a Community licence", async () => {
		licence.isPremium = false;
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);

		renderModal(deliveryService);

		const tabButton = await screen.findByRole("button", {
			name: "Jira Release",
		});
		expect(tabButton).toBeInTheDocument();

		expect(tabButton).toBeEnabled();
		await user.click(tabButton);

		const notice = await screen.findByTestId("premium-feature-notice");
		expect(notice).toHaveTextContent(/Jira Release/);
		expect(notice).toHaveTextContent(/launch date/);
		expect(notice).not.toHaveTextContent(/delivery date/i);
		expect(notice).not.toHaveTextContent(/rule-based/i);
		expect(deliveryService.getDeliverySourceOptions).not.toHaveBeenCalled();
	});

	it("asks for a Release, not for the name and date it will not let you type", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderModal(deliveryService);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);

		expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
		expect(screen.getByText(/Pick a Jira Release/i)).toBeInTheDocument();
		expect(screen.queryByText(/is required/i)).toBeNull();
	});

	it("lets it be saved once a Release is picked, with nothing left to fill in", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderModal(deliveryService);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);

		// The Release filled in the name and the date, and picking it is the whole of what this tab has
		// to be told, so there is nothing left to ask for and nothing left to complain about.
		expect(screen.getByLabelText("Launch Name")).toHaveValue("Release 44");
		expect(screen.getByRole("button", { name: "Save" })).toBeEnabled();
		expect(
			screen.queryByText(/only previews|does not change|cannot be saved/i),
		).toBeNull();
	});

	it("fetches the option list once for the lifetime of the modal", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderModal(deliveryService);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await waitFor(() => {
			expect(deliveryService.getDeliverySourceOptions).toHaveBeenCalledTimes(1);
		});

		await user.click(screen.getByRole("button", { name: "Manual" }));
		await user.click(screen.getByRole("button", { name: "Jira Release" }));

		await waitFor(() => {
			expect(
				screen.getByRole("combobox", { name: "Jira Release" }),
			).toBeInTheDocument();
		});
		expect(deliveryService.getDeliverySourceOptions).toHaveBeenCalledTimes(1);
	});
});
