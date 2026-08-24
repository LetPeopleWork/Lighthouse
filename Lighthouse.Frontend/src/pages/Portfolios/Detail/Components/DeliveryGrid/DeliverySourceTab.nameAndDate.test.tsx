import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { createMockDeliveryService } from "../../../../../tests/MockApiServiceProvider";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";
import {
	allOptions,
	JIRA_RELEASE_SOURCE,
	openSourceList,
	RELEASE_44_IN_PROJECT_TEST,
	RELEASE_44_IN_PROJECT_X,
	renderModal,
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

describe("DeliverySourceTab and the name and date it fills in", () => {
	const modalShowingReleases = () => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);
		deliveryService.previewDeliverySource = vi.fn().mockResolvedValue({
			name: "Release 44",
			date: new Date("2026-09-30T00:00:00Z"),
			features: [],
			emptyBecause: "None",
		});

		renderModal(deliveryService);
		return deliveryService;
	};

	const openTheReleaseList = async (
		user: ReturnType<typeof userEvent.setup>,
	) => {
		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await openSourceList(user);
	};

	it("greys out the name and the date, because the Release decides both", async () => {
		const user = userEvent.setup();
		modalShowingReleases();

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);

		expect(screen.getByLabelText("Launch Name")).toBeDisabled();
		expect(screen.getByLabelText("Launch Date")).toBeDisabled();
	});

	it("fills both from the picked Release, and refills them when another is picked", async () => {
		const user = userEvent.setup();
		modalShowingReleases();
		await openTheReleaseList(user);

		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		);

		expect(screen.getByLabelText("Launch Name")).toHaveValue("Release 44");
		expect(screen.getByLabelText("Launch Date")).toHaveValue("2026-09-30");

		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);

		expect(screen.getByLabelText("Launch Date")).toHaveValue("2026-10-15");
	});

	it("hands the filled-in name and date back, editable, on the tab that can save them", async () => {
		const user = userEvent.setup();
		modalShowingReleases();
		await openTheReleaseList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);

		await user.click(screen.getByRole("button", { name: "Manual" }));

		const nameField = screen.getByLabelText("Launch Name");
		expect(nameField).toBeEnabled();
		expect(nameField).toHaveValue("Release 44");
		expect(screen.getByLabelText("Launch Date")).toBeEnabled();
		expect(screen.getByLabelText("Launch Date")).toHaveValue("2026-10-15");

		await user.clear(nameField);
		await user.type(nameField, "Autumn release");

		expect(nameField).toHaveValue("Autumn release");
	});
});
