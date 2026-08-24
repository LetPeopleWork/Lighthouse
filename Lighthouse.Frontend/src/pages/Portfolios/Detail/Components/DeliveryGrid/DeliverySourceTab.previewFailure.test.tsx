import { act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { createMockDeliveryService } from "../../../../../tests/MockApiServiceProvider";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";
import {
	allOptions,
	createFeature,
	openSourceList,
	RELEASE_44_IN_PROJECT_TEST,
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

beforeEach(() => {
	vi.clearAllMocks();
	licence.isPremium = true;
	clearDeliverySourceOptionsCache();
});

describe("DeliverySourceTab when a preview does not arrive", () => {
	const PREVIEW_FAILED = /could not be previewed/i;

	const releaseFortyFourInProjectX = {
		name: "Release 44 in Project X",
		date: new Date("2026-10-15T12:00:00Z"),
		features: [createFeature(2, "Login")],
		emptyBecause: "None",
	};

	it("claims nothing has failed before anyone has asked for a preview", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);

		expect(screen.queryByText(PREVIEW_FAILED)).toBeNull();
	});

	it("says so when the preview cannot be fetched, and takes it back when the next one arrives", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);
		deliveryService.previewDeliverySource = vi
			.fn()
			.mockRejectedValueOnce(new Error("boom"))
			.mockResolvedValue(releaseFortyFourInProjectX);

		renderTab(deliveryService);
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		);

		expect(await screen.findByText(PREVIEW_FAILED)).toHaveTextContent(
			"This Jira Release could not be previewed. Try again in a moment.",
		);

		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);

		await screen.findByTestId("delivery-source-preview");
		expect(screen.queryByText(PREVIEW_FAILED)).toBeNull();
	});

	it("ignores a failure belonging to a Release the reader has already moved on from", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		let failTheFirstPick: (reason: Error) => void = () => {};
		deliveryService.previewDeliverySource = vi
			.fn()
			.mockImplementationOnce(
				() =>
					new Promise((_resolve, reject) => {
						failTheFirstPick = reject;
					}),
			)
			.mockResolvedValue(releaseFortyFourInProjectX);

		renderTab(deliveryService);
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		);
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);
		const preview = await screen.findByTestId("delivery-source-preview");

		failTheFirstPick(new Error("boom"));
		await act(async () => {
			await new Promise((resolve) => setTimeout(resolve, 0));
		});

		expect(screen.queryByText(PREVIEW_FAILED)).toBeNull();
		expect(preview).toHaveTextContent("Release 44 in Project X");
	});

	it("says the Release has nothing to show when neither of the two named reasons applies", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);
		deliveryService.previewDeliverySource = vi.fn().mockResolvedValue({
			name: "Release 44",
			date: new Date("2026-09-30T12:00:00Z"),
			features: [],
			emptyBecause: "None",
		});

		renderTab(deliveryService);
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		);

		const empty = await screen.findByTestId("delivery-source-preview-empty");
		expect(empty).toHaveTextContent(
			"This Jira Release has no Deliverables to show.",
		);
	});
});
