import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import {
	MemoryRouter,
	Route,
	Routes,
	useLocation,
	useNavigationType,
} from "react-router";
import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import ConnectionsSection from "../../../components/App/Header/TaskManager/ConnectionsSection";
import OnboardingStepper from "../../../components/Common/OnboardingStepper/OnboardingStepper";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IConnectionHealth } from "../../../services/Api/ConnectionHealthService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockOAuthService,
	createMockPortfolioService,
	createMockRbacService,
	createMockTeamService,
	createMockTerminologyService,
	createMockUpdateSubscriptionService,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import OverviewDashboard from "../../Overview/OverviewDashboard";
import ConnectionsSettings from "../../Settings/Connections/ConnectionsSettings";
import { settingsRoutes } from "../../Settings/settingsRoutes";

/**
 * Priya sets up the first connection and Tomasz follows an old runbook link. Every way to a connection
 * (the onboarding stepper, the Task Manager, the hint on the Overview, a bookmark from before the
 * settings moved) ends on Settings, System, Connections, and the connection pages lead back there.
 *
 * The routes are the real settings routes. The settings area page itself is a stand-in: it is where the
 * person lands, and the tests only need to know that they landed there.
 */

vi.mock("../../Settings/SettingsAreaPage", () => ({
	default: ({ area, tab }: { area: string; tab: string }) => (
		<div data-testid="settings-area-page">{`${area}/${tab}`}</div>
	),
}));

const { openOAuthPopup } = vi.hoisted(() => ({ openOAuthPopup: vi.fn() }));
vi.mock("../../../hooks/useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup }),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: true },
		canUsePremiumFeatures: true,
		canCreatePortfolio: true,
		canCreateTeam: true,
		createPortfolioTooltip: "",
		createTeamTooltip: "",
		status: "ready",
	}),
}));

vi.mock("../../Settings/Connections/AdditionalFieldsEditor", () => ({
	default: () => <div data-testid="additional-fields-editor" />,
}));

// The routes load the connection page on first use; a cold load must not count against the first test.
beforeAll(async () => {
	await import("../../Connections/Edit/EditConnection");
	await import("../../Settings/SettingsAreaPage");
});

const TheConnectionsTab = "/settings/system/connections";
const TheEditPageOfNorthwindJira = "/settings/system/connections/4/edit";
const TheNewConnectionPage = "/settings/system/connections/new";

const NorthwindJira = "Northwind Jira";

// This instance renames Work Tracking System to Data Source, so every word below is theirs.
const TheirWords = [
	["workTrackingSystem", "Work Tracking System", "Data Source"],
	["workTrackingSystems", "Work Tracking Systems", "Data Sources"],
	["team", "Team", "Squad"],
	["teams", "Teams", "Squads"],
	["portfolio", "Portfolio", "Program"],
	["portfolios", "Programs", "Programs"],
] as const;

const theirTerminology = () => {
	const terminology = createMockTerminologyService();
	vi.mocked(terminology.getAllTerminology).mockResolvedValue(
		TheirWords.map(([key, defaultValue, value], index) => ({
			id: index + 1,
			key,
			defaultValue,
			description: "",
			value,
		})),
	);
	return terminology;
};

const jiraWithAnApiToken = (id: number, name: string) =>
	new WorkTrackingSystemConnection({
		id,
		name,
		workTrackingSystem: "Jira",
		authenticationMethodKey: "jira.cloud",
		availableAuthenticationMethods: [
			{
				key: "jira.cloud",
				displayName: "Jira Cloud",
				options: [
					{
						key: "ApiToken",
						displayName: "API Token",
						isSecret: true,
						isOptional: false,
					},
				],
			},
		],
		options: [
			{ key: "ApiToken", value: "", isSecret: true, isOptional: false },
			{
				key: "Url",
				value: "https://jira.northwind.example",
				isSecret: false,
				isOptional: false,
			},
		],
		additionalFieldDefinitions: [],
	});

const jiraWithOAuth = () =>
	new WorkTrackingSystemConnection({
		id: 0,
		name: "Jira (OAuth)",
		workTrackingSystem: "Jira",
		authenticationMethodKey: "jira.oauth",
		availableAuthenticationMethods: [
			{
				key: "jira.oauth",
				displayName: "Jira Cloud (OAuth)",
				options: [
					{
						key: "Jira Url",
						displayName: "Jira URL",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "oauth.clientId",
						displayName: "Client ID",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "oauth.clientSecret",
						displayName: "Client Secret",
						isSecret: true,
						isOptional: false,
					},
				],
			},
		],
		options: [],
		additionalFieldDefinitions: [],
	});

const WhereWeAre = () => {
	const location = useLocation();
	return <div data-testid="where-we-are">{location.pathname}</div>;
};

const HowWeGotThere = () => (
	<div data-testid="how-we-got-there">{useNavigationType()}</div>
);

const aQueryClient = () =>
	new QueryClient({
		defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
	});

interface Instance {
	supportedSystems?: WorkTrackingSystemConnection[];
	configured?: WorkTrackingSystemConnection[];
	isSystemAdmin?: boolean;
}

const theServer = ({
	supportedSystems = [jiraWithAnApiToken(0, NorthwindJira)],
	configured = [jiraWithAnApiToken(4, NorthwindJira)],
}: Instance) => {
	const server = createMockWorkTrackingSystemService();
	vi.mocked(server.getWorkTrackingSystems).mockResolvedValue(supportedSystems);
	vi.mocked(server.getConfiguredWorkTrackingSystems).mockResolvedValue(
		configured,
	);
	vi.mocked(server.validateWorkTrackingSystemConnection).mockResolvedValue({
		isValid: true,
	});
	vi.mocked(server.addNewWorkTrackingSystemConnection).mockImplementation(
		async (connection) => connection,
	);
	vi.mocked(server.updateWorkTrackingSystemConnection).mockImplementation(
		async (connection) => connection,
	);
	vi.mocked(server.deleteWorkTrackingSystemConnection).mockResolvedValue(
		undefined,
	);
	return server;
};

const anInstanceWhere = (
	context: Partial<Parameters<typeof createMockApiServiceContext>[0]>,
	isSystemAdmin = true,
) => {
	const rbacService = createMockRbacService();
	vi.mocked(rbacService.getAuthorizationSummary).mockResolvedValue({
		// Only a reader exists with access control on; an instance without it has no readers.
		isRbacEnabled: !isSystemAdmin,
		isSystemAdmin,
		canCreateTeam: isSystemAdmin,
		canCreatePortfolio: isSystemAdmin,
		adminTeamIds: [],
		adminPortfolioIds: [],
	});
	const oauthService = createMockOAuthService();
	vi.mocked(oauthService.initiateConnect).mockResolvedValue({
		authorizationUrl: "https://tracker.example/authorize",
	});
	return createMockApiServiceContext({
		terminologyService: theirTerminology(),
		rbacService,
		oauthService,
		...context,
	});
};

const inTheApp = (
	apiContext: ReturnType<typeof createMockApiServiceContext>,
	queryClient: QueryClient,
	page: React.ReactElement,
	startAt: string,
) =>
	render(
		<QueryClientProvider client={queryClient}>
			<MemoryRouter initialEntries={[startAt]}>
				<ApiServiceContext.Provider value={apiContext}>
					<TerminologyProvider>
						<WhereWeAre />
						<HowWeGotThere />
						{page}
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);

// --- What Priya and Tomasz do ---

const whenTheyOpen = (address: string, instance: Instance = {}) => {
	const server = theServer(instance);
	const queryClient = aQueryClient();
	// The list on System, Connections was read before they set off, so it is the list that can go stale.
	queryClient.setQueryData(["configuredConnections"], []);
	inTheApp(
		anInstanceWhere(
			{ workTrackingSystemService: server },
			instance.isSystemAdmin,
		),
		queryClient,
		<Routes>{settingsRoutes()}</Routes>,
		address,
	);
	return { server, queryClient };
};

const whenTheyFollowAnOldLink = whenTheyOpen;

const whenTheyPickJiraAndFillItIn = async () => {
	fireEvent.click(await screen.findByRole("button", { name: NorthwindJira }));
	fireEvent.change(await screen.findByLabelText(/API Token/), {
		target: { value: "a-new-token" },
	});
	fireEvent.change(screen.getByLabelText(/^Url/), {
		target: { value: "https://jira.northwind.example" },
	});
	fireEvent.click(screen.getByRole("button", { name: "Next" }));
};

const whenTheyNameItAndCreateIt = async () => {
	await screen.findByLabelText(/Connection Name/i);
	const create = await screen.findByRole("button", { name: "Create" });
	await waitFor(() => expect(create).toBeEnabled());
	fireEvent.click(create);
};

const whenTheyCancelTheWizard = async () =>
	fireEvent.click(await screen.findByRole("button", { name: "Cancel" }));

const whenTheyPressSave = async () => {
	const save = await screen.findByRole("button", { name: /^Save$/ });
	await waitFor(() => expect(save).toBeEnabled());
	fireEvent.click(save);
};

// --- What they see ---

const theyAreOn = (path: string) =>
	waitFor(() =>
		expect(
			screen.getByTestId("where-we-are"),
			`The person should have ended on ${path}. Wherever a connection page sends them or an old link redirects them must be System, Connections, from where they can see the connection.`,
		).toHaveTextContent(new RegExp(`^${path}$`)),
	);

const theyAreOnTheConnectionsTab = async () => {
	await theyAreOn(TheConnectionsTab);
	expect(
		await screen.findByTestId("settings-area-page"),
		"The Connections tab of System should be showing, not the Overview.",
	).toHaveTextContent("system/connections");
};

const theOldAddressWasReplaced = () =>
	expect(
		screen.getByTestId("how-we-got-there"),
		"An old link must replace its entry in the history, or Back leads straight into the redirect again and loops.",
	).toHaveTextContent("REPLACE");

const theBackLinkLeadsToTheConnectionsTab = async () =>
	expect(
		await screen.findByRole("link", { name: "Back to Data Sources" }),
		"The connection page should lead back to System, Connections, in this instance's own word for Work Tracking Systems.",
	).toHaveAttribute("href", TheConnectionsTab);

const theListOfConnectionsMustBeAskedForAgain = (queryClient: QueryClient) =>
	waitFor(() =>
		expect(
			queryClient.getQueryData(["configuredConnections"]),
			"The remembered list of connections must be dropped, or System, Connections shows the list from before the change.",
		).toBeUndefined(),
	);

const theListOfConnectionsIsStillRemembered = (queryClient: QueryClient) =>
	expect(
		queryClient.getQueryData(["configuredConnections"]),
		"Nothing changed, so the remembered list should be left alone.",
	).toEqual([]);

describe("every way to a connection leads to System, Connections", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		localStorage.clear();
	});

	describe("old links", () => {
		it("an old edit link opens the same connection under System, with a way back and no Cancel", async () => {
			whenTheyFollowAnOldLink("/connections/4/edit");

			expect(await screen.findByDisplayValue(NorthwindJira)).toBeVisible();
			await theyAreOn(TheEditPageOfNorthwindJira);
			theOldAddressWasReplaced();
			await theBackLinkLeadsToTheConnectionsTab();
			expect(
				screen.queryByRole("button", { name: "Cancel" }),
				"A connection that already exists is edited in place and saved; it has no Cancel.",
			).not.toBeInTheDocument();
		});

		it("an old new-connection link opens the wizard under System, with a way back", async () => {
			whenTheyFollowAnOldLink("/connections/new");

			expect(
				await screen.findByText("Create Data Source Connection"),
			).toBeVisible();
			await theyAreOn(TheNewConnectionPage);
			theOldAddressWasReplaced();
			await theBackLinkLeadsToTheConnectionsTab();
		});

		it("a reader sent along an old link is still told they have no permission, not moved elsewhere", async () => {
			whenTheyFollowAnOldLink("/connections/4/edit", { isSystemAdmin: false });

			expect(
				await screen.findByTestId("connection-edit-no-access-alert"),
			).toBeVisible();
			await theyAreOn(TheEditPageOfNorthwindJira);
			expect(
				screen.getByRole("link", { name: "Back to Overview" }),
			).toHaveAttribute("href", "/");
			expect(
				screen.queryByTestId("settings-area-page"),
				"A reader must see the no-access alert on the connection page, not be redirected into an area.",
			).not.toBeInTheDocument();
		});
	});

	describe("leaving a connection page", () => {
		it("cancelling the wizard returns to System, Connections and creates nothing", async () => {
			const { server } = whenTheyOpen(TheNewConnectionPage);

			await whenTheyCancelTheWizard();

			await theyAreOnTheConnectionsTab();
			expect(server.addNewWorkTrackingSystemConnection).not.toHaveBeenCalled();
		});

		it("completing the wizard returns to System, Connections and the list asks again", async () => {
			const { server, queryClient } = whenTheyOpen(TheNewConnectionPage);

			await whenTheyPickJiraAndFillItIn();
			await whenTheyNameItAndCreateIt();

			await theyAreOnTheConnectionsTab();
			await theListOfConnectionsMustBeAskedForAgain(queryClient);
			expect(server.addNewWorkTrackingSystemConnection).toHaveBeenCalledTimes(
				1,
			);
		});

		it("saving an existing connection returns to System, Connections and the list asks again", async () => {
			const { server, queryClient } = whenTheyOpen(TheEditPageOfNorthwindJira);
			await screen.findByDisplayValue(NorthwindJira);
			theListOfConnectionsIsStillRemembered(queryClient);

			await whenTheyPressSave();

			await theyAreOnTheConnectionsTab();
			await theListOfConnectionsMustBeAskedForAgain(queryClient);
			expect(server.updateWorkTrackingSystemConnection).toHaveBeenCalledTimes(
				1,
			);
		});

		it("cancelling after the wizard saved a draft deletes it, returns to System, Connections and the list asks again", async () => {
			const draft = jiraWithOAuth();
			draft.id = 88;
			const { server, queryClient } = whenTheyOpen(TheNewConnectionPage, {
				supportedSystems: [jiraWithOAuth()],
			});
			vi.mocked(server.addNewWorkTrackingSystemConnection).mockResolvedValue(
				draft,
			);
			openOAuthPopup.mockResolvedValue({ status: "success", connectionId: 88 });

			fireEvent.click(
				await screen.findByRole("button", { name: "Jira (OAuth)" }),
			);
			fireEvent.change(await screen.findByLabelText("Jira URL"), {
				target: { value: "https://northwind.atlassian.net" },
			});
			fireEvent.change(screen.getByLabelText("Client ID"), {
				target: { value: "client" },
			});
			fireEvent.change(screen.getByLabelText("Client Secret"), {
				target: { value: "secret" },
			});
			fireEvent.click(screen.getByRole("button", { name: "Next" }));
			await screen.findByLabelText(/Connection Name/i);
			await whenTheyCancelTheWizard();

			await theyAreOnTheConnectionsTab();
			await theListOfConnectionsMustBeAskedForAgain(queryClient);
			expect(server.deleteWorkTrackingSystemConnection).toHaveBeenCalledWith(
				88,
			);
		});
	});
});

describe("the entry points on the other pages", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		localStorage.clear();
	});

	describe("System, Connections", () => {
		const theConnectionsTabListing = () =>
			inTheApp(
				anInstanceWhere({ workTrackingSystemService: theServer({}) }),
				aQueryClient(),
				<Routes>
					<Route path="/" element={<ConnectionsSettings />} />
					{settingsRoutes()}
				</Routes>,
				"/",
			);

		it("Connect {Work Tracking System} opens the new connection page under System", async () => {
			theConnectionsTabListing();

			fireEvent.click(
				await screen.findByRole("button", { name: "Connect Data Source" }),
			);

			expect(
				await screen.findByText("Create Data Source Connection"),
			).toBeVisible();
			await theyAreOn(TheNewConnectionPage);
		});

		it("a connection's Edit opens its edit page under System", async () => {
			theConnectionsTabListing();

			fireEvent.click(await screen.findByTestId("edit-connection-button"));

			expect(await screen.findByDisplayValue(NorthwindJira)).toBeVisible();
			await theyAreOn(TheEditPageOfNorthwindJira);
		});
	});

	it("the onboarding stepper offers to connect a Data Source and opens the new connection page under System", async () => {
		inTheApp(
			anInstanceWhere({}),
			aQueryClient(),
			<Routes>
				<Route
					path="/"
					element={
						<OnboardingStepper
							hasConnections={false}
							hasTeams={false}
							hasPortfolios={false}
							canCreateTeam={true}
							canCreatePortfolio={true}
							licenseCheckStatus="ready"
							teamTerm="Squad"
							portfolioTerm="Program"
							connectionTerm="Data Source"
						/>
					}
				/>
				{settingsRoutes()}
			</Routes>,
			"/",
		);

		const connect = await screen.findByRole("button", {
			name: "Connect Data Source",
		});
		fireEvent.click(connect);

		expect(
			await screen.findByText("Create Data Source Connection"),
		).toBeVisible();
		await theyAreOn(TheNewConnectionPage);
	});

	describe("the Overview", () => {
		const theOverviewOf = (
			connections: IWorkTrackingSystemConnection[],
			isSystemAdmin = true,
		) => {
			const server = theServer({ configured: connections });
			const portfolioService = createMockPortfolioService();
			vi.mocked(portfolioService.getPortfolios).mockResolvedValue([]);
			const teamService = createMockTeamService();
			vi.mocked(teamService.getTeams).mockResolvedValue([]);
			const updates = createMockUpdateSubscriptionService();
			vi.mocked(updates.getGlobalUpdateStatus).mockResolvedValue({
				hasActiveUpdates: false,
				activeCount: 0,
			});
			inTheApp(
				anInstanceWhere(
					{
						workTrackingSystemService: server,
						portfolioService,
						teamService,
						updateSubscriptionService: updates,
						licensingService: {
							getLicenseStatus: vi
								.fn()
								.mockResolvedValue({ canUsePremiumFeatures: true }),
							importLicense: vi.fn(),
							clearLicense: vi.fn(),
						},
					},
					isSystemAdmin,
				),
				aQueryClient(),
				<Routes>
					<Route path="/" element={<OverviewDashboard />} />
					{settingsRoutes()}
				</Routes>,
				"/",
			);
		};

		const theyDismissTheStepper = async () =>
			fireEvent.click(await screen.findByTestId("onboarding-dismiss"));

		const theLineSayingWhereToConnect = () =>
			screen.findByRole("link", {
				name: /Settings\s*›\s*System\s*›\s*Connections/,
			});

		const theLineAsksInTheirWords = (link: HTMLElement) => {
			expect(
				link.parentElement,
				"The line holding the link should ask for a connection in this instance's own word.",
			).toHaveTextContent(/Connect a Data Source first:/);
			expect(
				link.closest("[role='tooltip'], .MuiTooltip-popper"),
				"The link must sit in the text line, not inside a hover tooltip.",
			).toBeNull();
		};

		it("with no connection, a line under the disabled Add buttons says where to connect one, with the stepper showing and after it is put away", async () => {
			theOverviewOf([]);
			await screen.findByTestId("onboarding-stepper");

			const line = await theLineSayingWhereToConnect();
			expect(line).toHaveAttribute("href", TheConnectionsTab);
			theLineAsksInTheirWords(line);
			expect(screen.getByRole("button", { name: "Add Squad" })).toBeDisabled();

			await theyDismissTheStepper();

			expect(await theLineSayingWhereToConnect()).toHaveAttribute(
				"href",
				TheConnectionsTab,
			);
		});

		it("a reader, who cannot open System, Connections, is not offered a link to it", async () => {
			theOverviewOf([], false);
			await waitFor(() =>
				expect(
					screen.queryByTestId("loading-animation-progress-indicator"),
				).not.toBeInTheDocument(),
			);

			expect(
				screen.queryByRole("link", {
					name: /Settings\s*›\s*System\s*›\s*Connections/,
				}),
			).not.toBeInTheDocument();
		});

		it("once a connection exists the line is gone", async () => {
			theOverviewOf([jiraWithAnApiToken(4, NorthwindJira)]);

			await screen.findByTestId("onboarding-stepper");

			expect(
				screen.queryByRole("link", {
					name: /Settings\s*›\s*System\s*›\s*Connections/,
				}),
			).not.toBeInTheDocument();
		});
	});

	describe("the Task Manager's connections section", () => {
		const aFailingConnection: IConnectionHealth = {
			connectionId: 4,
			connectionName: NorthwindJira,
			workTrackingSystem: "Jira",
			state: "AuthenticationFailed",
			message: "The token was refused.",
		};

		const theSectionShowing = (connections: IConnectionHealth[]) =>
			inTheApp(
				anInstanceWhere({}),
				aQueryClient(),
				<Routes>
					<Route
						path="/"
						element={
							<div data-testid="the-section">
								<ConnectionsSection
									connections={connections}
									onTest={vi.fn()}
								/>
							</div>
						}
					/>
					{settingsRoutes()}
				</Routes>,
				"/",
			);

		const theSectionUsesNoHardCodedWord = () => {
			const section = within(screen.getByTestId("the-section"));
			expect(
				section.queryAllByText(/connection/i),
				"The section should say Data Source, this instance's word, not the word connection.",
			).toEqual([]);
			expect(section.queryAllByLabelText(/connection/i)).toEqual([]);
		};

		it("a failing connection can be managed and edited under System, in this instance's words", async () => {
			theSectionShowing([aFailingConnection]);

			const manage = await screen.findByRole("link", {
				name: "Manage Data Sources",
			});
			expect(manage).toHaveAttribute("href", TheConnectionsTab);
			theSectionUsesNoHardCodedWord();

			fireEvent.click(
				screen.getByRole("button", {
					name: new RegExp(`Edit.*${NorthwindJira}`),
				}),
			);

			await theyAreOn(TheEditPageOfNorthwindJira);
		});

		it("with nothing configured it points to System, Connections, in this instance's words", async () => {
			theSectionShowing([]);

			const links = await screen.findAllByRole("link", {
				name: /Data Sources/,
			});
			for (const link of links) {
				expect(link).toHaveAttribute("href", TheConnectionsTab);
			}
			theSectionUsesNoHardCodedWord();
		});
	});
});
