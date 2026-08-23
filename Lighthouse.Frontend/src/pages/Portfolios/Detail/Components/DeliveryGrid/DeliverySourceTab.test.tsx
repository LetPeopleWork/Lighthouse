import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDeliverySourceOption } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
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

const licence = vi.hoisted(() => ({ isPremium: true }));

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.DELIVERY]: "Delivery",
				[TERMINOLOGY_KEYS.FEATURES]: "Deliverables",
				[TERMINOLOGY_KEYS.FEATURE]: "Deliverable",
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

const createFeature = (id: number, name: string): IFeature =>
	({
		id,
		name,
		referenceId: `FTR-${id}`,
		stateCategory: "ToDo",
		state: "ToDo",
		type: "Feature",
		size: 1,
		owningTeam: "Team A",
		lastUpdated: new Date(),
		isUsingDefaultFeatureSize: false,
		parentWorkItemReference: "",
		projects: [],
		remainingWork: {},
		totalWork: {},
		forecasts: [],
		startedDate: new Date(),
		closedDate: new Date(),
		cycleTime: 0,
		workItemAge: 0,
		url: "",
		isBlocked: false,
		getRemainingWorkForFeature: () => 0,
		getRemainingWorkForTeam: () => 0,
		getTotalWorkForFeature: () => 0,
		getTotalWorkForTeam: () => 0,
	}) as IFeature;

const mockPortfolio = {
	id: 1,
	name: "Test Portfolio",
	features: [],
	involvedTeams: [],
	tags: [],
	totalWorkItems: 0,
	remainingWorkItems: 0,
	forecasts: [],
	lastUpdated: new Date(),
	serviceLevelExpectationProbability: 0,
	serviceLevelExpectationRange: 0,
	systemWIPLimit: 0,
	remainingFeatures: 0,
	featureSizeTargetProbability: 0,
	featureSizeTargetRange: 0,
	fromBackend: vi.fn(),
} as unknown as Portfolio;

const JIRA_RELEASE_SOURCE = {
	key: "jira-release",
	displayName: "Jira Release",
};
const JIRA_FIX_VERSION_SOURCE = {
	key: "jira-fix-version",
	displayName: "Jira Fix Version",
};

const datedInJustATest: IDeliverySourceOption = {
	id: "10044",
	name: "Release 44",
	date: new Date("2026-09-30T00:00:00Z"),
	projectKey: "JUSTATEST",
	projectName: "Just A Test",
	isSelectable: true,
	blockedBecause: null,
};

const datedInProject: IDeliverySourceOption = {
	id: "10144",
	name: "Release 44",
	date: new Date("2026-10-15T00:00:00Z"),
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: true,
	blockedBecause: null,
};

const datelessOption: IDeliverySourceOption = {
	id: "10045",
	name: "Release 45",
	date: null,
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: false,
	blockedBecause: "NoDateSet",
};

const allOptions = [datedInJustATest, datedInProject, datelessOption];

const renderTab = (deliveryService = createMockDeliveryService()) => {
	const context = createMockApiServiceContext({ deliveryService });

	const result = render(
		<ApiServiceContext.Provider value={context}>
			<DeliverySourceTab
				portfolioId={1}
				sourceKey={JIRA_RELEASE_SOURCE.key}
				sourceName={JIRA_RELEASE_SOURCE.displayName}
				featuresTerm="Deliverables"
			/>
		</ApiServiceContext.Provider>,
	);

	return { ...result, deliveryService };
};

const renderModal = (deliveryService = createMockDeliveryService()) => {
	const context = createMockApiServiceContext({
		deliveryService,
		featureService: createMockFeatureService(),
	});

	const result = render(
		<ApiServiceContext.Provider value={context}>
			<DeliveryCreateModal
				open={true}
				portfolio={mockPortfolio}
				onClose={vi.fn()}
				onSave={vi.fn()}
			/>
		</ApiServiceContext.Provider>,
	);

	return { ...result, deliveryService };
};

const selectionModeButtons = () =>
	within(screen.getByRole("group", { name: "Selection Mode" })).getAllByRole(
		"button",
	);

const openSourceList = async (user: ReturnType<typeof userEvent.setup>) => {
	await waitFor(() => {
		expect(
			screen.getByRole("combobox", { name: "Jira Release" }),
		).toBeInTheDocument();
	});
	await user.click(screen.getByRole("combobox", { name: "Jira Release" }));
};

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
		expect(notice).not.toHaveTextContent(/rule-based/i);
		expect(deliveryService.getDeliverySourceOptions).not.toHaveBeenCalled();
	});

	it("blocks saving while a source tab is showing, and says so", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderModal(deliveryService);

		await user.type(screen.getByLabelText("Delivery Name"), "Autumn release");
		fireEvent.change(screen.getByLabelText("Delivery Date"), {
			target: { value: "2099-12-31" },
		});
		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);

		expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
		expect(
			screen.getByText(/only previews|does not change|cannot be saved/i),
		).toBeInTheDocument();
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

describe("DeliverySourceTab picker", () => {
	it("says it is loading while the list fills", async () => {
		const deliveryService = createMockDeliveryService();
		let resolveOptions: (options: IDeliverySourceOption[]) => void = () => {};
		deliveryService.getDeliverySourceOptions = vi.fn().mockReturnValue(
			new Promise<IDeliverySourceOption[]>((resolve) => {
				resolveOptions = resolve;
			}),
		);

		renderTab(deliveryService);

		expect(await screen.findByRole("progressbar")).toBeInTheDocument();

		resolveOptions(allOptions);

		await waitFor(() => {
			expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
		});
	});

	it("groups the options by their Jira project so equally named Releases stay apart", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);

		expect(screen.getByText("Just A Test (JUSTATEST)")).toBeInTheDocument();
		expect(screen.getByText("Project X (PROJ)")).toBeInTheDocument();

		expect(screen.getAllByRole("option", { name: /Release 44/ })).toHaveLength(
			2,
		);
		expect(
			screen.getByRole("option", { name: /Release 44.*JUSTATEST/ }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("option", { name: /Release 44.*PROJ/ }),
		).toBeInTheDocument();
	});

	it("lists a dateless Release but refuses to let it be picked, saying what is missing", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);

		const blocked = screen.getByRole("option", { name: /Release 45/ });
		expect(blocked).toHaveAttribute("aria-disabled", "true");
		expect(blocked).toHaveTextContent(/no date/i);

		fireEvent.click(blocked);

		expect(deliveryService.previewDeliverySource).not.toHaveBeenCalled();
	});

	it("renders selectability from the server even when the dates disagree with it", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi.fn().mockResolvedValue([
			{
				...datedInJustATest,
				isSelectable: false,
				blockedBecause: "NoDateSet",
			},
		]);

		renderTab(deliveryService);
		await openSourceList(user);

		expect(screen.getByRole("option", { name: /Release 44/ })).toHaveAttribute(
			"aria-disabled",
			"true",
		);
	});

	it("shows an error rather than an empty list when the options cannot be fetched", async () => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockRejectedValue(new Error("boom"));

		renderTab(deliveryService);

		const error = await screen.findByRole("alert");
		expect(error).toHaveTextContent(/could not be loaded/i);
		expect(error).not.toHaveTextContent(/no releases/i);
	});
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
			screen.getByRole("option", { name: /Release 44.*JUSTATEST/ }),
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
		expect(empty).not.toHaveTextContent(/Portfolio/);
	});

	it("sends the reader to the Portfolio when the tagged work is out of its scope", async () => {
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
		expect(empty).toHaveTextContent(/Portfolio/);
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
});
