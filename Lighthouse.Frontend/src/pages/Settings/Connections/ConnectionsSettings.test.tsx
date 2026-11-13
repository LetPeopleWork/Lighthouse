import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Suspense } from "react";
import {
	MemoryRouter,
	Route,
	Routes,
	useLocation,
	useNavigate,
} from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockRbacService,
	createMockSystemInfoService,
	createMockTeamService,
	createMockTerminologyService,
	createMockUpdateSubscriptionService,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import EditConnectionPage from "../../Connections/Edit/EditConnection";
import OverviewDashboard from "../../Overview/OverviewDashboard";
import { settingsRoutes } from "../settingsRoutes";

// The other System tabs are not under test: they only need to open without fetching anything.
vi.mock("../DemoData/DemoDataSettings", () => ({
	default: () => <div>demo data panel content</div>,
}));
vi.mock("../DatabaseManagement/DatabaseManagementSettings", () => ({
	default: () => <div>database panel content</div>,
}));
vi.mock("../SystemInfo/SystemInfoDisplay", () => ({
	default: () => <div>system details content</div>,
}));
vi.mock("../SystemInfo/ThirdPartyPackagesSection", () => ({
	default: () => <div>third party packages content</div>,
}));
vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		status: "ready",
		canCreatePortfolio: true,
		canCreateTeam: true,
		createPortfolioTooltip: "",
		createTeamTooltip: "",
		maxPortfoliosWithoutPremium: 1,
		maxTeamsWithoutPremium: 3,
	}),
}));

// ---------------------------------------------------------------------------------------------
// People, terms and connections
// ---------------------------------------------------------------------------------------------

interface Access {
	isSystemAdmin: boolean;
	isRbacEnabled: boolean;
}

const ADMIN: Access = { isSystemAdmin: true, isRbacEnabled: true };
const READER: Access = { isSystemAdmin: false, isRbacEnabled: true };

/** The instance has renamed its connections, so every word on the tab must come from the configured term. */
const THEIR_WORD = "Tracker";
const THEIR_WORD_PLURAL = "Trackers";

const aConnection = (
	id: number,
	name: string,
	workTrackingSystem: "Jira" | "AzureDevOps",
	requiresReconnect = false,
): WorkTrackingSystemConnection => {
	const connection = new WorkTrackingSystemConnection({
		id,
		name,
		workTrackingSystem,
		options: [],
		authenticationMethodKey:
			workTrackingSystem === "Jira" ? "jira.oauth" : "ado.pat",
	});
	connection.requiresReconnect = requiresReconnect;
	return connection;
};

const NORTHWIND_JIRA_NEEDING_RECONNECT = aConnection(
	1,
	"Northwind Jira",
	"Jira",
	true,
);
const NORTHWIND_ADO = aConnection(2, "Northwind ADO", "AzureDevOps");
const NORTHWINDS_CONNECTIONS = [
	NORTHWIND_JIRA_NEEDING_RECONNECT,
	NORTHWIND_ADO,
];

/** What a person picks in the create wizard: Azure DevOps with a personal access token. */
const AZURE_DEVOPS_AS_OFFERED = new WorkTrackingSystemConnection({
	id: 0,
	name: "Azure DevOps",
	workTrackingSystem: "AzureDevOps",
	options: [
		{ key: "Url", value: "", isSecret: false, isOptional: false },
		{ key: "AccessToken", value: "", isSecret: true, isOptional: false },
	],
	availableAuthenticationMethods: [
		{
			key: "ado.pat",
			displayName: "Personal Access Token",
			options: [
				{
					key: "Url",
					displayName: "Organization URL",
					isSecret: false,
					isOptional: false,
				},
				{
					key: "AccessToken",
					displayName: "Access Token",
					isSecret: true,
					isOptional: false,
				},
			],
		},
	],
	additionalFieldDefinitions: [],
	authenticationMethodKey: "ado.pat",
});

const REFUSED_MESSAGE =
	"You don't have access to the connections list. Ask a Lighthouse administrator to check your roles.";
const COULD_NOT_LOAD = `Could not load the ${THEIR_WORD_PLURAL}.`;
const EMPTY_STATE = new RegExp(
	`No ${THEIR_WORD} found\\.\\s*Load Demo Data\\s*or\\s*Check the documentation`,
);
const RECONNECT_REQUIRED = /Reconnect required/;
const SAVED_COLUMN_STATE_KEY =
	"lighthouse:datagrid:overview-connection-table:state";
/** How long the app trusts a cached answer, so a list kept after a write would still be served from memory. */
const THE_APPS_STALE_TIME = 5 * 60 * 1000;

type ConnectionList =
	| WorkTrackingSystemConnection[]
	| Promise<WorkTrackingSystemConnection[]>
	| "refused"
	| "fails";

const theListAnswer = (list: ConnectionList) => {
	if (list === "refused") return Promise.reject(new ApiError(403, "Forbidden"));
	if (list === "fails")
		return Promise.reject(new Error("the connection list call failed"));
	return Promise.resolve(list);
};

const aSlowList = () => {
	let answer: (list: WorkTrackingSystemConnection[]) => void = () => {};
	const promise = new Promise<WorkTrackingSystemConnection[]>((resolve) => {
		answer = resolve;
	});
	return { promise, answer };
};

/** A Team or Portfolio as the Overview lists it. */
const anOwner = (id: number, name: string) => ({
	id,
	name,
	tags: [],
	features: [],
	involvedTeams: [],
	projects: [],
	lastUpdated: new Date(),
	serviceLevelExpectationProbability: 0,
	serviceLevelExpectationRange: 0,
	systemWIPLimit: 0,
	remainingFeatures: 0,
	featureWip: 1,
	useFixedDatesForThroughput: false,
	throughputStartDate: new Date(),
	throughputEndDate: new Date(),
});

// ---------------------------------------------------------------------------------------------
// The app around the tab
// ---------------------------------------------------------------------------------------------

const WhereWeAre = () => {
	const location = useLocation();
	const navigate = useNavigate();
	return (
		<>
			<output data-testid="where-we-are">{location.pathname}</output>
			<button type="button" onClick={() => navigate("/a-new-connection")}>
				go to a new connection
			</button>
			<button
				type="button"
				onClick={() => navigate("/settings/system/connections")}
			>
				go back to the connections
			</button>
		</>
	);
};

const whereWeAre = () => screen.getByTestId("where-we-are").textContent;

const renderTheApp = ({
	address,
	access = ADMIN,
	list,
}: {
	address: string;
	access?: Access;
	list: ConnectionList;
}) => {
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		...access,
		canCreateTeam: access.isSystemAdmin,
		canCreatePortfolio: access.isSystemAdmin,
	});
	const terminologyService = createMockTerminologyService();
	vi.mocked(terminologyService.getAllTerminology).mockResolvedValue([
		{
			id: 1,
			key: TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM,
			defaultValue: "Work Tracking System",
			description: "",
			value: THEIR_WORD,
		},
		{
			id: 2,
			key: TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS,
			defaultValue: "Work Tracking Systems",
			description: "",
			value: THEIR_WORD_PLURAL,
		},
	]);
	const workTrackingSystemService = createMockWorkTrackingSystemService();
	vi.mocked(
		workTrackingSystemService.getConfiguredWorkTrackingSystems,
	).mockImplementation(() => theListAnswer(list));
	vi.mocked(workTrackingSystemService.getWorkTrackingSystems).mockResolvedValue(
		[AZURE_DEVOPS_AS_OFFERED],
	);
	vi.mocked(
		workTrackingSystemService.deleteWorkTrackingSystemConnection,
	).mockResolvedValue(undefined);
	vi.mocked(
		workTrackingSystemService.validateWorkTrackingSystemConnection,
	).mockResolvedValue({ isValid: true });
	const portfolioService = createMockPortfolioService();
	portfolioService.getPortfolios = vi
		.fn()
		.mockResolvedValue([anOwner(1, "Northwind Platform")]);
	const teamService = createMockTeamService();
	teamService.getTeams = vi
		.fn()
		.mockResolvedValue([anOwner(1, "Northwind Warehouse")]);
	const systemInfoService = createMockSystemInfoService();
	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false, staleTime: THE_APPS_STALE_TIME },
			mutations: { retry: false },
		},
	});
	render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					rbacService,
					terminologyService,
					workTrackingSystemService,
					portfolioService,
					teamService,
					systemInfoService,
					updateSubscriptionService: createMockUpdateSubscriptionService(),
				})}
			>
				<TerminologyProvider>
					<MemoryRouter initialEntries={[address]}>
						<Suspense fallback={null}>
							<Routes>
								{settingsRoutes()}
								<Route path="/overview" element={<OverviewDashboard />} />
								<Route
									path="/a-new-connection"
									element={<EditConnectionPage />}
								/>
								<Route path="/" element={<div>back home</div>} />
							</Routes>
						</Suspense>
						<WhereWeAre />
					</MemoryRouter>
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
	return { workTrackingSystemService };
};

// ---------------------------------------------------------------------------------------------
// Steps
// ---------------------------------------------------------------------------------------------

const theTabNames = () =>
	screen.getAllByRole("tab").map((tab) => tab.textContent);

const theSelectedTab = () =>
	screen
		.getAllByRole("tab")
		.filter((tab) => tab.getAttribute("aria-selected") === "true")
		.map((tab) => tab.textContent);

const theConnectionsTab = async () => {
	await screen.findByRole("heading", { level: 1, name: "Settings › System" });
	return within(await screen.findByRole("tabpanel"));
};

const expectTheListShows = async (
	panel: ReturnType<typeof within>,
	names: string[],
) => {
	const found = await Promise.all(names.map((name) => panel.findByText(name)));
	for (const [index, element] of found.entries()) {
		expect(
			element,
			`${names[index]} must be listed on System, Connections, because that is now the one place connections are managed`,
		).toBeVisible();
	}
};

const expectNoEmptyState = (panel: ReturnType<typeof within>, why: string) => {
	expect(
		panel.queryByText(EMPTY_STATE),
		`The empty state says no connection exists, which is false ${why}. Show it only after the list has loaded empty.`,
	).not.toBeInTheDocument();
};

afterEach(() => {
	localStorage.clear();
	vi.restoreAllMocks();
});

// ---------------------------------------------------------------------------------------------
// System, Connections
// ---------------------------------------------------------------------------------------------

describe("the connections live in System, Connections", () => {
	it("a System Admin finds Connections as System's first tab, listing every connection with Edit, Delete and its reconnect banner, under the configured term", async () => {
		renderTheApp({
			address: "/settings/system/connections",
			list: NORTHWINDS_CONNECTIONS,
		});

		const panel = await theConnectionsTab();
		await expectTheListShows(panel, ["Northwind Jira", "Northwind ADO"]);
		expect(
			theTabNames()[0],
			"Connections is the first tab of System, before Database",
		).toBe("Connections");
		expect(theSelectedTab()).toEqual(["Connections"]);
		expect(screen.getByTestId("connections-tab")).toHaveAttribute(
			"aria-selected",
			"true",
		);
		expect(
			panel.getByRole("heading", { name: THEIR_WORD_PLURAL }),
			"The section keeps its title, in the instance's own word",
		).toBeVisible();
		expect(
			panel.getByRole("button", { name: `Connect ${THEIR_WORD}` }),
			"The button that starts a new connection says Connect, in the instance's own word",
		).toBeVisible();
		expect(panel.getAllByTestId("edit-connection-button")).toHaveLength(2);
		expect(panel.getAllByTestId("delete-connection-button")).toHaveLength(2);
		expect(
			panel.getAllByText(RECONNECT_REQUIRED),
			"Only Northwind Jira needs reconnecting, so exactly one reconnect banner shows",
		).toHaveLength(1);
		expect(
			panel.queryByRole("textbox"),
			"The tab has no text filter: the list is short and the Overview's filter stays on the Overview",
		).not.toBeInTheDocument();
		expectNoEmptyState(panel, "while two connections exist");
	});

	it("keeps the columns a person hid on the Overview's list hidden on the tab", async () => {
		localStorage.setItem(
			SAVED_COLUMN_STATE_KEY,
			JSON.stringify({ columnVisibilityModel: { workTrackingSystem: false } }),
		);
		renderTheApp({
			address: "/settings/system/connections",
			list: [NORTHWIND_ADO],
		});

		const panel = await theConnectionsTab();
		await expectTheListShows(panel, ["Northwind ADO"]);
		expect(
			panel.queryByText("AzureDevOps"),
			"The Type column was hidden on the old list; the tab reads the same saved column state, so it stays hidden",
		).not.toBeInTheDocument();
	});

	it("deleting a connection asks the same question and takes it off the list", async () => {
		const { workTrackingSystemService } = renderTheApp({
			address: "/settings/system/connections",
			list: NORTHWINDS_CONNECTIONS,
		});
		const panel = await theConnectionsTab();
		await expectTheListShows(panel, ["Northwind ADO"]);
		vi.mocked(
			workTrackingSystemService.getConfiguredWorkTrackingSystems,
		).mockResolvedValue([NORTHWIND_JIRA_NEEDING_RECONNECT]);

		fireEvent.click(panel.getAllByTestId("delete-connection-button")[1]);
		const dialog = await screen.findByRole("dialog");
		expect(within(dialog).getByText("Confirm Delete")).toBeVisible();
		expect(within(dialog).getByText(/Northwind ADO/)).toBeVisible();
		fireEvent.click(within(dialog).getByRole("button", { name: "Delete" }));

		await waitFor(() =>
			expect(
				workTrackingSystemService.deleteWorkTrackingSystemConnection,
			).toHaveBeenCalledWith(2),
		);
		await waitFor(() =>
			expect(
				panel.queryByText("Northwind ADO"),
				"A deleted connection must leave the list",
			).not.toBeInTheDocument(),
		);
		expect(panel.getByText("Northwind Jira")).toBeVisible();
	});

	it("a fresh instance is told no connection exists, with a link to load demo data that opens System, Demo Data, and a link to the documentation", async () => {
		renderTheApp({ address: "/settings/system/connections", list: [] });

		const panel = await theConnectionsTab();
		expect(await panel.findByText(EMPTY_STATE)).toBeVisible();
		expect(
			panel.getByRole("link", { name: "Check the documentation" }),
		).toHaveAttribute("href", "https://docs.lighthouse.letpeople.work");
		expect(
			panel.getByRole("button", { name: `Connect ${THEIR_WORD}` }),
		).toBeVisible();

		fireEvent.click(panel.getByRole("link", { name: "Load Demo Data" }));

		await waitFor(() =>
			expect(whereWeAre()).toBe("/settings/system/demo-data"),
		);
		expect(await screen.findByText("demo data panel content")).toBeVisible();
	});

	it("shows a lone spinner while the list is on its way, never the empty state, then the list", async () => {
		const slow = aSlowList();
		renderTheApp({
			address: "/settings/system/connections",
			list: slow.promise,
		});

		const panel = await theConnectionsTab();
		expect(await panel.findByRole("progressbar")).toBeInTheDocument();
		expectNoEmptyState(panel, "while the list is still loading");
		expect(
			panel.queryByRole("button", { name: `Connect ${THEIR_WORD}` }),
			"Nothing but the spinner shows until the list has arrived",
		).not.toBeInTheDocument();

		await act(async () => slow.answer([NORTHWIND_ADO]));

		await expectTheListShows(panel, ["Northwind ADO"]);
		expect(panel.queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("a list that cannot be loaded says so in the instance's own word, never 'no connection found'", async () => {
		renderTheApp({ address: "/settings/system/connections", list: "fails" });

		const panel = await theConnectionsTab();
		expect(await panel.findByText(COULD_NOT_LOAD)).toBeVisible();
		expectNoEmptyState(panel, "after a failed load");
		expect(panel.queryByText(REFUSED_MESSAGE)).not.toBeInTheDocument();
	});

	it("a refused list shows today's refused message, never the empty state", async () => {
		renderTheApp({ address: "/settings/system/connections", list: "refused" });

		const panel = await theConnectionsTab();
		expect(await panel.findByText(REFUSED_MESSAGE)).toBeVisible();
		expectNoEmptyState(panel, "when the list was refused");
		expect(panel.queryByText(COULD_NOT_LOAD)).not.toBeInTheDocument();
	});

	it("a reader sees no Connections tab, and its address leads to a tab they may open", async () => {
		const { workTrackingSystemService } = renderTheApp({
			address: "/settings/system/connections",
			access: READER,
			list: NORTHWINDS_CONNECTIONS,
		});

		await screen.findByRole("heading", { level: 1, name: "Settings › System" });
		await waitFor(() =>
			expect(whereWeAre()).toBe("/settings/system/system-info"),
		);
		expect(
			theTabNames(),
			"Connections is for System Admins only, as the Overview's section was",
		).not.toContain("Connections");
		expect(screen.queryByText("Northwind Jira")).not.toBeInTheDocument();
		expect(
			workTrackingSystemService.getConfiguredWorkTrackingSystems,
		).not.toHaveBeenCalled();
	});

	it("a connection created in the wizard is listed on return to the tab, not the list from before", async () => {
		const user = userEvent.setup();
		const { workTrackingSystemService } = renderTheApp({
			address: "/settings/system/connections",
			list: [NORTHWIND_JIRA_NEEDING_RECONNECT],
		});
		await expectTheListShows(await theConnectionsTab(), ["Northwind Jira"]);
		const created = aConnection(3, "Northwind Azure", "AzureDevOps");
		vi.mocked(
			workTrackingSystemService.addNewWorkTrackingSystemConnection,
		).mockResolvedValue(created);
		vi.mocked(
			workTrackingSystemService.getConfiguredWorkTrackingSystems,
		).mockResolvedValue([NORTHWIND_JIRA_NEEDING_RECONNECT, created]);

		fireEvent.click(
			screen.getByRole("button", { name: "go to a new connection" }),
		);
		fireEvent.click(
			await screen.findByRole("button", { name: /Azure DevOps/ }),
		);
		fireEvent.change(await screen.findByLabelText("Organization URL"), {
			target: { value: "https://dev.azure.com/northwind" },
		});
		fireEvent.change(screen.getByLabelText("Access Token"), {
			target: { value: "a-token" },
		});
		await user.click(screen.getByRole("button", { name: /Next/ }));
		await user.click(await screen.findByRole("button", { name: /^Create$/ }));
		await waitFor(() => expect(whereWeAre()).toBe("/"));
		fireEvent.click(
			screen.getByRole("button", { name: "go back to the connections" }),
		);

		const panel = await theConnectionsTab();
		expect(
			await panel.findByText("Northwind Azure"),
			"Creating a connection must drop the remembered list, or the tab shows the list from before as if it were current",
		).toBeVisible();
	});
});

// ---------------------------------------------------------------------------------------------
// The Overview without the connections
// ---------------------------------------------------------------------------------------------

describe("the Overview without the connections section", () => {
	it("shows a System Admin the Portfolios, Teams and reconnect banners, but no connections section and no button to add a connection", async () => {
		renderTheApp({ address: "/overview", list: NORTHWINDS_CONNECTIONS });

		expect(await screen.findByText("Northwind Platform")).toBeVisible();
		expect(screen.getByText("Northwind Warehouse")).toBeVisible();
		expect(
			await screen.findAllByText(RECONNECT_REQUIRED),
			"The Overview keeps one reconnect banner for each connection that needs it",
		).toHaveLength(1);
		expect(
			screen.queryByRole("heading", { name: THEIR_WORD_PLURAL }),
			"The connections section moved to System, Connections",
		).not.toBeInTheDocument();
		expect(screen.queryByText("Northwind ADO")).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("edit-connection-button"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", {
				name: new RegExp(`^(Add|Connect) ${THEIR_WORD}$`),
			}),
			"The Overview's own add-connection button went with the section",
		).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Add Team" })).toBeEnabled();

		fireEvent.change(screen.getByRole("textbox"), {
			target: { value: "matches nothing" },
		});

		expect(
			screen.getAllByText(RECONNECT_REQUIRED),
			"The filter box narrows Portfolios and Teams only; a connection needing reconnecting stays in view",
		).toHaveLength(1);
	});

	it("still asks whether a connection exists: with none, Add Team stays disabled for a System Admin", async () => {
		renderTheApp({ address: "/overview", list: [] });

		expect(await screen.findByText("Northwind Platform")).toBeVisible();
		await waitFor(() =>
			expect(screen.getByRole("button", { name: "Add Team" })).toBeDisabled(),
		);
		expect(screen.queryByText(EMPTY_STATE)).not.toBeInTheDocument();
	});
});
