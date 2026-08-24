import { screen, waitFor, within } from "@testing-library/react";
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

describe("DeliverySourceTab preview", () => {
	const previewDeliveryService = (
		preview: Record<string, unknown>,
		options = allOptions,
	) => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(options);
		deliveryService.previewDeliverySource = vi.fn().mockResolvedValue(preview);
		return deliveryService;
	};

	const pickReleaseFortyFourInJustATest = async (
		user: ReturnType<typeof userEvent.setup>,
	) => {
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		);
	};

	it("shows the date it would take and the work that would come along", async () => {
		const user = userEvent.setup();
		const deliveryService = previewDeliveryService({
			name: "Release 44",
			date: new Date("2026-09-30T00:00:00Z"),
			features: [createFeature(1, "Widget rewrite"), createFeature(2, "Login")],
			emptyBecause: "None",
		});

		renderTab(deliveryService);
		await pickReleaseFortyFourInJustATest(user);

		await waitFor(() => {
			expect(deliveryService.previewDeliverySource).toHaveBeenCalledWith(
				1,
				"jira-release",
				"10044",
			);
		});

		const preview = await screen.findByTestId("delivery-source-preview");
		expect(preview).toHaveTextContent(
			new Date("2026-09-30T00:00:00Z").toLocaleDateString(),
		);
		expect(within(preview).getByText("Widget rewrite")).toBeInTheDocument();
		expect(within(preview).getByText("Login")).toBeInTheDocument();
	});

	it("sends the reader to Jira when nothing is tagged against the Release", async () => {
		const user = userEvent.setup();
		const deliveryService = previewDeliveryService({
			name: "Release 44",
			date: new Date("2026-09-30T00:00:00Z"),
			features: [],
			emptyBecause: "NothingTaggedAgainstTheSource",
		});

		renderTab(deliveryService);
		await pickReleaseFortyFourInJustATest(user);

		const empty = await screen.findByTestId("delivery-source-preview-empty");
		expect(empty).toHaveTextContent(/Deliverables/);
		expect(empty).toHaveTextContent(/tagged/i);
		expect(empty).not.toHaveTextContent(/Value Stream/);
	});

	it("sends the reader to the Portfolio, in the tenant’s own word for it, when the tagged work is out of its scope", async () => {
		const user = userEvent.setup();
		const deliveryService = previewDeliveryService({
			name: "Release 44",
			date: new Date("2026-09-30T00:00:00Z"),
			features: [],
			emptyBecause: "TaggedWorkNotTrackedByThisPortfolio",
		});

		renderTab(deliveryService);
		await pickReleaseFortyFourInJustATest(user);

		const empty = await screen.findByTestId("delivery-source-preview-empty");
		expect(empty).toHaveTextContent(/Deliverables/);
		expect(empty).toHaveTextContent(/Value Stream/);
		expect(empty).not.toHaveTextContent(/Portfolio/);
	});

	it("offers nothing that would persist the picked Release", async () => {
		const user = userEvent.setup();
		const deliveryService = previewDeliveryService({
			name: "Release 44",
			date: new Date("2026-09-30T00:00:00Z"),
			features: [createFeature(1, "Widget rewrite")],
			emptyBecause: "None",
		});

		const { container } = renderTab(deliveryService);
		await pickReleaseFortyFourInJustATest(user);
		await screen.findByTestId("delivery-source-preview");

		const buttons = within(container).queryAllByRole("button", {
			name: /save|bind|apply|use this/i,
		});
		expect(buttons).toHaveLength(0);
	});

	it("shows the Release that is selected when an earlier preview arrives after a later one", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		let answerTheFirstPick: (preview: Record<string, unknown>) => void =
			() => {};
		deliveryService.previewDeliverySource = vi
			.fn()
			.mockImplementationOnce(
				() =>
					new Promise((resolve) => {
						answerTheFirstPick = resolve;
					}),
			)
			.mockResolvedValue({
				name: "Release 44 in Project X",
				date: new Date("2026-10-15T00:00:00Z"),
				features: [createFeature(2, "Login")],
				emptyBecause: "None",
			});

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
		expect(preview).toHaveTextContent("Release 44 in Project X");

		answerTheFirstPick({
			name: "Release 44 in Just A Test",
			date: new Date("2026-09-30T00:00:00Z"),
			features: [createFeature(1, "Widget rewrite")],
			emptyBecause: "None",
		});

		await waitFor(() => {
			expect(deliveryService.previewDeliverySource).toHaveBeenCalledTimes(2);
		});
		expect(preview).toHaveTextContent("Release 44 in Project X");
		expect(preview).not.toHaveTextContent("Release 44 in Just A Test");
		expect(within(preview).queryByText("Widget rewrite")).toBeNull();
	});
});
