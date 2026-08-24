import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDelivery } from "../../../../../models/Delivery";
import type { IDeliverySourceOption } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import { DeliveryCreateModal } from "./DeliveryCreateModal";
import {
	clearDeliverySourceOptionsCache,
	DeliverySourceTab,
} from "./DeliverySourceTab";
import {
	allOptions,
	datedInJustATest,
	datedInProject,
	datelessOption,
	JIRA_RELEASE_SOURCE,
	mockPortfolio,
	RELEASE_44_IN_PROJECT_X,
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

describe("DeliveryCreateModal editing a Launch that follows a Release", () => {
	// The server answers with the mode's NAME, never with the number the browser posts back, so a
	// fixture built from the enum would agree with a comparison that disagrees with every real
	// response. Every Launch below is therefore written the way the wire writes it.
	const followingARelease = (): IDelivery =>
		({
			id: 42,
			name: "Release 44",
			date: "2026-10-15T00:00:00Z",
			portfolioId: 1,
			features: [1],
			selectionMode: "SourceBound",
			sourceKey: JIRA_RELEASE_SOURCE.key,
			sourceReference: datedInProject.id,
			concurrencyToken: "v1",
		}) as unknown as IDelivery;

	const chosenByHand = (): IDelivery =>
		({
			id: 42,
			name: "Autumn launch",
			date: "2026-10-15T00:00:00Z",
			portfolioId: 1,
			features: [1],
			selectionMode: "Manual",
			sourceKey: null,
			sourceReference: null,
			concurrencyToken: "v1",
		}) as unknown as IDelivery;

	const renderEditModal = (
		deliveryService: ReturnType<typeof createMockDeliveryService>,
		editingDelivery: IDelivery,
		onUpdate = vi.fn(),
	) => {
		const context = createMockApiServiceContext({
			deliveryService,
			featureService: createMockFeatureService(),
		});

		render(
			<ApiServiceContext.Provider value={context}>
				<DeliveryCreateModal
					open={true}
					portfolio={mockPortfolio}
					editingDelivery={editingDelivery}
					onClose={vi.fn()}
					onSave={vi.fn()}
					onUpdate={onUpdate}
				/>
			</ApiServiceContext.Provider>,
		);

		return { deliveryService, onUpdate };
	};

	const serviceOffering = (options: IDeliverySourceOption[]) => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(options);
		deliveryService.previewDeliverySource = vi.fn().mockResolvedValue({
			name: "Release 44",
			date: new Date("2026-10-15T00:00:00Z"),
			features: [],
			emptyBecause: "None",
		});

		return deliveryService;
	};

	const releaseInTheBox = (): string | null => {
		const box = screen.queryByRole("combobox", { name: "Jira Release" });
		return box === null ? null : (box as HTMLInputElement).value;
	};

	const boundPayload = {
		id: 42,
		name: "Release 44",
		date: "2026-10-15",
		featureIds: [],
		selectionMode: DeliverySelectionMode.SourceBound,
		sourceKey: JIRA_RELEASE_SOURCE.key,
		sourceReference: datedInProject.id,
		rules: undefined,
		mode: undefined,
		concurrencyToken: "v1",
	};

	// The binding is what is at risk, and it is at risk in the payload rather than on the screen, so
	// every row says both what the box shows AND what a save that touched nothing writes down.
	it.each([
		{
			when: "the Release is still on the offered list",
			offered: allOptions,
			delivery: followingARelease,
			shown: RELEASE_44_IN_PROJECT_X,
			expected: boundPayload,
		},
		{
			when: "the Release has shipped, so the offered list no longer holds it",
			offered: [datedInJustATest, datelessOption],
			delivery: followingARelease,
			shown: "Release 44",
			expected: boundPayload,
		},
		{
			when: "the Release was archived and the offered list came back empty",
			offered: [],
			delivery: followingARelease,
			shown: "Release 44",
			expected: boundPayload,
		},
		{
			when: "the Launch follows no Release at all",
			offered: allOptions,
			delivery: chosenByHand,
			shown: null,
			expected: {
				id: 42,
				name: "Autumn launch",
				date: "2026-10-15",
				featureIds: [1],
				selectionMode: DeliverySelectionMode.Manual,
				sourceKey: undefined,
				sourceReference: undefined,
				rules: undefined,
				mode: undefined,
				concurrencyToken: "v1",
			},
		},
	])(
		"shows what it follows and saves it back untouched when $when",
		async ({ offered, delivery, shown, expected }) => {
			const { onUpdate } = renderEditModal(
				serviceOffering(offered),
				delivery(),
			);

			await screen.findByRole("button", { name: "Jira Release" });
			await waitFor(() => {
				expect(releaseInTheBox()).toBe(shown);
			});

			fireEvent.click(screen.getByRole("button", { name: "Update" }));

			expect(onUpdate).toHaveBeenCalledWith(expected);
		},
	);

	// Both fields are greyed out by `readsFromSource` in the modal, which is true for any tab that
	// reads from the work tracking system. On this form nobody chose that tab: it is chosen for them
	// by the Launch already following a Release, so the greying-out is a consequence nothing asserts
	// unless it is asserted here.
	it("greys out the name and the date it takes from the Release", async () => {
		renderEditModal(serviceOffering(allOptions), followingARelease());

		await screen.findByRole("combobox", { name: "Jira Release" });

		expect(screen.getByLabelText("Launch Name")).toBeDisabled();
		expect(screen.getByLabelText("Launch Date")).toBeDisabled();
	});

	// Wandering onto Manual would look like a way to take the Launch back by hand, and it is not one:
	// the server discards everything sent alongside such a save. Releasing it is its own action.
	it("offers no other tab as a way out of the Release it follows", async () => {
		renderEditModal(serviceOffering(allOptions), followingARelease());

		await screen.findByRole("combobox", { name: "Jira Release" });

		expect(selectionModeButtons().map((b) => b.textContent)).toEqual([
			"Jira Release",
		]);
	});

	it("still offers every tab when the Launch was chosen by hand", async () => {
		renderEditModal(serviceOffering(allOptions), chosenByHand());

		await screen.findByRole("button", { name: "Jira Release" });

		expect(selectionModeButtons().map((b) => b.textContent)).toEqual([
			"Manual",
			"Rule-Based",
			"Jira Release",
		]);
	});

	// The list is kept between openings of one form, and the entry a Launch follows is not part of
	// what the server said, so a cached answer must not be able to swallow it.
	it("shows the Release it follows even when the list came out of the cache", async () => {
		const deliveryService = serviceOffering([datedInJustATest]);

		const { unmount } = render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					deliveryService,
					featureService: createMockFeatureService(),
				})}
			>
				<DeliverySourceTab
					portfolioId={1}
					sourceKey={JIRA_RELEASE_SOURCE.key}
					sourceName={JIRA_RELEASE_SOURCE.displayName}
					featuresTerm="Deliverables"
					portfolioTerm="Value Stream"
					currentSelection={null}
					onOptionPicked={vi.fn()}
				/>
			</ApiServiceContext.Provider>,
		);

		await screen.findByRole("combobox", { name: "Jira Release" });
		unmount();

		renderEditModal(deliveryService, followingARelease());

		await waitFor(() => {
			expect(releaseInTheBox()).toBe("Release 44");
		});
		expect(deliveryService.getDeliverySourceOptions).toHaveBeenCalledTimes(1);
	});
});
