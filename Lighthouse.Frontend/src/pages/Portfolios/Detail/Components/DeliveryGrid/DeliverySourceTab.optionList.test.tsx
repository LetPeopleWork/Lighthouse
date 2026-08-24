import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDeliverySourceOption } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { createMockDeliveryService } from "../../../../../tests/MockApiServiceProvider";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";
import {
	allOptions,
	createFeature,
	fixVersionOption,
	JIRA_FIX_VERSION_SOURCE,
	JIRA_RELEASE_SOURCE,
	openSourceList,
	RELEASE_44_IN_PROJECT_TEST,
	RELEASE_44_IN_PROJECT_X,
	renderModal,
	renderTab,
	retiredOption,
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

describe("DeliverySourceTab option list", () => {
	const tabListing = (options: IDeliverySourceOption[]) => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(options);
		deliveryService.previewDeliverySource = vi.fn().mockResolvedValue({
			name: "Release 44",
			date: new Date("2026-09-30T12:00:00Z"),
			features: [createFeature(1, "Widget rewrite")],
			emptyBecause: "None",
		});

		renderTab(deliveryService);
		return deliveryService;
	};

	it("says a Release that is gone from the board is no longer available, rather than undated", async () => {
		const user = userEvent.setup();
		tabListing([retiredOption]);

		await openSourceList(user);

		const gone = screen.getByRole("option", { name: /Release 40/ });
		expect(gone).toHaveTextContent("No longer available");
		expect(gone).not.toHaveTextContent("No Release Date Set");
		expect(gone).toHaveAttribute("aria-disabled", "true");
	});

	it("keeps the picked Release in the box, so the panel below is never unattributed", async () => {
		const user = userEvent.setup();
		tabListing(allOptions);

		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		);

		expect(screen.getByRole("combobox", { name: "Jira Release" })).toHaveValue(
			RELEASE_44_IN_PROJECT_TEST,
		);
	});

	it("marks the Release that was picked, and only that one, when the list is reopened", async () => {
		const user = userEvent.setup();
		tabListing(allOptions);

		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);
		await openSourceList(user);

		const marked = screen
			.getAllByRole("option")
			.filter((option) => option.getAttribute("aria-selected") === "true");

		expect(marked).toHaveLength(1);
		expect(marked[0]).toHaveAccessibleName(RELEASE_44_IN_PROJECT_X);
	});

	it("survives the picker being emptied, and asks for no preview of nothing", async () => {
		const user = userEvent.setup();
		const deliveryService = tabListing(allOptions);

		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		);
		await screen.findByTestId("delivery-source-preview");

		await user.click(screen.getByTitle("Clear"));

		expect(screen.getByRole("combobox", { name: "Jira Release" })).toHaveValue(
			"",
		);
		expect(deliveryService.previewDeliverySource).toHaveBeenCalledTimes(1);
	});

	it("asks the server again for a second source rather than showing the first one's list", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE, JIRA_FIX_VERSION_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockImplementation((_portfolioId: number, sourceKey: string) => {
				if (sourceKey === JIRA_FIX_VERSION_SOURCE.key) {
					return Promise.resolve([fixVersionOption]);
				}
				return Promise.resolve(allOptions);
			});

		renderModal(deliveryService);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await waitFor(() => {
			expect(deliveryService.getDeliverySourceOptions).toHaveBeenCalledWith(
				1,
				"jira-release",
			);
		});

		await user.click(screen.getByRole("button", { name: "Jira Fix Version" }));
		await waitFor(() => {
			expect(
				screen.getByRole("combobox", { name: "Jira Fix Version" }),
			).toBeInTheDocument();
		});
		await user.click(
			screen.getByRole("combobox", { name: "Jira Fix Version" }),
		);

		expect(
			screen.getByRole("option", { name: /Sprint 9 hotfix/ }),
		).toBeInTheDocument();
		expect(
			screen.queryAllByRole("option", { name: /Release 44/ }),
		).toHaveLength(0);
	});
});
