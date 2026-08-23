import {
	act,
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
	// A real project on the live board is called this. The brackets inside the brackets are ugly and
	// they are still the point: the row stays unambiguous, which two bare "Release 44"s never were.
	projectName: "Project (Test)",
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

// Kept out of allOptions so the tests that count rows keep counting the same three.
const retiredOption: IDeliverySourceOption = {
	id: "10040",
	name: "Release 40",
	date: new Date("2026-08-01T12:00:00Z"),
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: false,
	blockedBecause: "RetiredAtSource",
};

const fixVersionOption: IDeliverySourceOption = {
	id: "20001",
	name: "Sprint 9 hotfix",
	date: new Date("2026-11-20T12:00:00Z"),
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: true,
	blockedBecause: null,
};

const datedOnASingleDigitDay: IDeliverySourceOption = {
	id: "10005",
	name: "Release 5",
	date: new Date("2027-01-05T12:00:00Z"),
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: true,
	blockedBecause: null,
};

// A row reads "<name> (<project>)", and both halves are matched when the reader types. Spelled out
// here because "Release 44" alone names two of the three rows, so a loose query finds two and throws.
const RELEASE_44_IN_PROJECT_TEST = "Release 44 (Project (Test))";
const RELEASE_44_IN_PROJECT_X = "Release 44 (Project X)";
const THE_DATELESS_RELEASE = /^Release 45 \(Project X\)/;

const renderTab = (deliveryService = createMockDeliveryService()) => {
	const context = createMockApiServiceContext({ deliveryService });

	const result = render(
		<ApiServiceContext.Provider value={context}>
			<DeliverySourceTab
				portfolioId={1}
				sourceKey={JIRA_RELEASE_SOURCE.key}
				sourceName={JIRA_RELEASE_SOURCE.displayName}
				featuresTerm="Deliverables"
				portfolioTerm="Value Stream"
				onOptionPicked={vi.fn()}
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

	it("blocks saving once a Release is picked, and says so", async () => {
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

		// The Release filled in a name and a future date, so nothing else is left to complain about:
		// Save is disabled because this tab saves nothing, and the message is the only thing saying so.
		expect(screen.getByLabelText("Launch Name")).toHaveValue("Release 44");
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

	it("names the project on every row so equally named Releases stay apart", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);

		// One row per Release and nothing else. The project used to be a heading over a group of rows,
		// which is one more kind of thing to read past for someone typing to find a name.
		expect(screen.getAllByRole("option")).toHaveLength(allOptions.length);
		expect(screen.queryByText("Project (Test) (JUSTATEST)")).toBeNull();
		expect(screen.queryByText("Project X (PROJ)")).toBeNull();

		expect(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		).toBeInTheDocument();
	});

	it("leaves the date off the rows, so the only date on screen is the one being previewed", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);

		expect(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		).not.toHaveTextContent(
			new Date("2026-09-30T00:00:00Z").toLocaleDateString(),
		);
	});

	it("narrows the list to what the reader types, matching the Release or the project", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);
		const picker = screen.getByRole("combobox", { name: "Jira Release" });

		await user.type(picker, "45");

		expect(screen.getAllByRole("option")).toHaveLength(1);
		expect(
			screen.getByRole("option", { name: THE_DATELESS_RELEASE }),
		).toBeInTheDocument();

		// The project half of the row is searchable too, and it is the only way to reach one of two
		// Releases that share a name.
		await user.clear(picker);
		await user.type(picker, "project x");

		expect(screen.getAllByRole("option")).toHaveLength(2);
		expect(
			screen.queryByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		).toBeNull();
	});

	it("lists a dateless Release but refuses to let it be picked, saying what is missing", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);

		const blocked = screen.getByRole("option", { name: THE_DATELESS_RELEASE });
		expect(blocked).toHaveAttribute("aria-disabled", "true");
		expect(blocked).toHaveTextContent("No Release Date Set");

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
		expect(error).toHaveTextContent(/Value Stream/);
		expect(error).not.toHaveTextContent(/Portfolio/);
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

describe("DeliveryCreateModal and the one thing it asks for next", () => {
	// The date field holds a day on this browser's calendar, not a UTC instant, so the string typed
	// into it has to be built the same way or a test run either side of midnight names a different day.
	const dayOnThisBrowsersCalendar = (daysFromToday: number): string => {
		const day = new Date();
		day.setDate(day.getDate() + daysFromToday);
		const month = `${day.getMonth() + 1}`.padStart(2, "0");
		const dayOfMonth = `${day.getDate()}`.padStart(2, "0");

		return `${day.getFullYear()}-${month}-${dayOfMonth}`;
	};

	const blockingMessage = () =>
		within(screen.getByRole("dialog")).getByRole("alert").textContent;

	const typeTheDate = async (
		user: ReturnType<typeof userEvent.setup>,
		value: string,
	) => {
		await user.clear(screen.getByLabelText("Launch Date"));
		await user.type(screen.getByLabelText("Launch Date"), value);
	};

	const modalOfferingNoSource = () => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi.fn().mockResolvedValue([]);
		deliveryService.getRuleSchema = vi.fn().mockResolvedValue({
			fields: [
				{
					fieldKey: "fixVersion",
					displayName: "Fix Version",
					isMultiValue: false,
				},
			],
			operators: ["equals"],
			maxRules: 5,
			maxValueLength: 100,
		});

		renderModal(deliveryService);
		return deliveryService;
	};

	const modalShowingOneRelease = (options: IDeliverySourceOption[]) => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(options);
		deliveryService.previewDeliverySource = vi.fn().mockResolvedValue({
			name: "Release 44",
			date: new Date("2026-10-15T12:00:00Z"),
			features: [createFeature(1, "Widget rewrite")],
			emptyBecause: "None",
		});

		renderModal(deliveryService);
		return deliveryService;
	};

	it("names one missing thing at a time, in the order the reader can fix them", async () => {
		const user = userEvent.setup();
		modalOfferingNoSource();

		await waitFor(() => {
			expect(screen.getByLabelText("Launch Name")).toBeInTheDocument();
		});
		expect(blockingMessage()).toBe("Launch name is required");

		await user.type(screen.getByLabelText("Launch Name"), "Autumn launch");
		expect(blockingMessage()).toBe("Launch date is required");

		await typeTheDate(user, dayOnThisBrowsersCalendar(-7));
		expect(blockingMessage()).toBe("Launch date must be in the future");

		await typeTheDate(user, dayOnThisBrowsersCalendar(7));
		expect(blockingMessage()).toBe("At least one deliverable must be selected");
	});

	it("does not take a name of nothing but spaces for a name", async () => {
		const user = userEvent.setup();
		modalOfferingNoSource();

		await waitFor(() => {
			expect(screen.getByLabelText("Launch Name")).toBeInTheDocument();
		});
		await user.type(screen.getByLabelText("Launch Name"), "   ");

		expect(blockingMessage()).toBe("Launch name is required");
	});

	it("does not treat rules as matched just because the tab was opened", async () => {
		const user = userEvent.setup();
		modalOfferingNoSource();

		await waitFor(() => {
			expect(screen.getByLabelText("Launch Name")).toBeInTheDocument();
		});
		await user.type(screen.getByLabelText("Launch Name"), "Autumn launch");
		await typeTheDate(user, dayOnThisBrowsersCalendar(7));

		await user.click(screen.getByRole("button", { name: "Rule-Based" }));

		expect(
			await screen.findByText("Rules must be validated before saving"),
		).toBeInTheDocument();
		expect(screen.queryByText("No features match the rules")).toBeNull();
	});

	it("refuses today, because a date to launch on has to be a day still to come", async () => {
		const user = userEvent.setup();
		modalOfferingNoSource();

		await waitFor(() => {
			expect(screen.getByLabelText("Launch Name")).toBeInTheDocument();
		});
		await user.type(screen.getByLabelText("Launch Name"), "Autumn launch");
		await typeTheDate(user, dayOnThisBrowsersCalendar(0));

		expect(blockingMessage()).toBe("Launch date must be in the future");
	});

	it("writes a single-digit month and day into the date field with a leading zero", async () => {
		const user = userEvent.setup();
		modalShowingOneRelease([datedOnASingleDigitDay]);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await openSourceList(user);
		await user.click(screen.getByRole("option", { name: /Release 5/ }));

		expect(screen.getByLabelText("Launch Date")).toHaveValue("2027-01-05");
	});

	it("leaves the picked Release alone when its own tab button is clicked again", async () => {
		const user = userEvent.setup();
		modalShowingOneRelease(allOptions);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);
		await screen.findByTestId("delivery-source-preview");

		await user.click(screen.getByRole("button", { name: "Jira Release" }));

		expect(blockingMessage()).toBe(
			"Picking a Jira Release only previews it. Switch to Manual or Rule-Based to save.",
		);
	});
});
