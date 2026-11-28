import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, fireEvent, render, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Portfolio } from "../models/Portfolio/Portfolio";
import type { IPortfolioSettings } from "../models/Portfolio/PortfolioSettings";
import { Team } from "../models/Team/Team";
import type { ITeamSettings } from "../models/Team/TeamSettings";
import { WorkTrackingSystemConnection } from "../models/WorkTracking/WorkTrackingSystemConnection";
import OverviewDashboard from "../pages/Overview/OverviewDashboard";
import PortfolioDetail from "../pages/Portfolios/Detail/PortfolioDetail";
import EditPortfolio from "../pages/Portfolios/Edit/EditPortfolio";
import TeamDetail from "../pages/Teams/Detail/TeamDetail";
import EditTeam from "../pages/Teams/Edit/EditTeam";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockPortfolioService,
	createMockRbacService,
	createMockTeamService,
	createMockTerminologyService,
	createMockUpdateSubscriptionService,
	createMockWorkTrackingSystemService,
} from "../tests/MockApiServiceProvider";
import { aReportList, aReportService } from "../tests/ReportsTestKit";
import {
	usePortfoliosQuery,
	useTeamsLoader,
	useTeamsQuery,
} from "./useUnitLists";

// The settings forms and wizards are large forms of their own. Each stand-in hands over the callback the page
// gives its form for saving, which is what the page wires; the page itself, its hooks and the cache are real.
type SaveSettings<T> = (settings: T) => Promise<unknown>;

const held = vi.hoisted(() => ({
	saveTeam: undefined as undefined | SaveSettings<unknown>,
	savePortfolio: undefined as undefined | SaveSettings<unknown>,
}));

vi.mock("../components/Common/Team/ModifyTeamSettings", () => ({
	default: (props: { saveTeamSettings: SaveSettings<unknown> }) => {
		held.saveTeam = props.saveTeamSettings;
		return <div>Team settings form</div>;
	},
}));

vi.mock("../components/Common/CreateWizards/CreateTeamWizard", () => ({
	default: (props: { saveTeamSettings: SaveSettings<unknown> }) => {
		held.saveTeam = props.saveTeamSettings;
		return <div>Create Team wizard</div>;
	},
}));

vi.mock("../components/Common/ProjectSettings/ModifyProjectSettings", () => ({
	default: (props: { saveProjectSettings: SaveSettings<unknown> }) => {
		held.savePortfolio = props.saveProjectSettings;
		return <div>Portfolio settings form</div>;
	},
}));

vi.mock("../components/Common/CreateWizards/CreatePortfolioWizard", () => ({
	default: (props: { savePortfolioSettings: SaveSettings<unknown> }) => {
		held.savePortfolio = props.savePortfolioSettings;
		return <div>Create Portfolio wizard</div>;
	},
}));

// The cache is set up as the app sets it up, so a shared read that forgets its own freshness rules shows here.
// Retries wait no time, so a read that retries shows as extra asks without slowing the test.
const theAppsQueryClient = () =>
	new QueryClient({
		defaultOptions: {
			queries: {
				staleTime: 1000 * 60 * 5,
				gcTime: 1000 * 60 * 30,
				retry: 2,
				retryDelay: 0,
				refetchOnWindowFocus: false,
				refetchOnMount: true,
				refetchOnReconnect: true,
			},
		},
	});

const aTeam = (id: number, name: string): Team => {
	const team = new Team();
	team.id = id;
	team.name = name;
	team.features = [];
	team.portfolios = [];
	return team;
};

const aPortfolio = (id: number, name: string): Portfolio => {
	const portfolio = new Portfolio();
	portfolio.id = id;
	portfolio.name = name;
	portfolio.features = [];
	portfolio.involvedTeams = [];
	return portfolio;
};

const teamsNamed = (...names: string[]): Team[] =>
	names.map((name, index) => aTeam(index + 1, name));

const portfoliosNamed = (...names: string[]): Portfolio[] =>
	names.map((name, index) => aPortfolio(index + 1, name));

const namesOf = (units: { name: string }[] | undefined) =>
	units?.map((unit) => unit.name);

interface TheLists {
	teams: string[];
	portfolios: string[];
}

const BEFORE: TheLists = { teams: ["Alpha", "Beta"], portfolios: ["Orbit"] };

class OwnChangeScenario {
	private readonly teamService = createMockTeamService();
	private readonly portfolioService = createMockPortfolioService();
	private readonly licensingService = createMockLicensingService();
	private readonly workTrackingSystemService =
		createMockWorkTrackingSystemService();
	private readonly updateSubscriptionService =
		createMockUpdateSubscriptionService();
	private readonly services = createMockApiServiceContext({
		teamService: this.teamService,
		portfolioService: this.portfolioService,
		licensingService: this.licensingService,
		rbacService: createMockRbacService(),
		terminologyService: createMockTerminologyService(),
		workTrackingSystemService: this.workTrackingSystemService,
		reportService: aReportService({
			getReports: vi.fn().mockResolvedValue(aReportList([])),
		}),
		updateSubscriptionService: this.updateSubscriptionService,
	});
	private readonly queryClient = theAppsQueryClient();
	private shownTeams: Team[] | undefined;
	private shownPortfolios: Portfolio[] | undefined;
	private deletesHappenOn: "teams" | "portfolios" | undefined;

	constructor() {
		held.saveTeam = undefined;
		held.savePortfolio = undefined;
		this.theServerHolds(BEFORE);
		vi.mocked(this.licensingService.getLicenseStatus).mockResolvedValue({
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		});
		vi.mocked(this.teamService.getTeam).mockResolvedValue(aTeam(1, "Alpha"));
		vi.mocked(this.teamService.getTeamSettings).mockResolvedValue({
			id: 1,
			name: "Alpha",
		} as ITeamSettings);
		vi.mocked(this.teamService.createTeam).mockResolvedValue({
			id: 9,
			name: "Gamma",
		} as ITeamSettings);
		vi.mocked(this.teamService.updateTeam).mockImplementation(
			async (settings) => settings,
		);
		vi.mocked(this.teamService.updateTeamData).mockResolvedValue(undefined);
		vi.mocked(this.teamService.deleteTeam).mockResolvedValue(undefined);
		vi.mocked(this.portfolioService.getPortfolio).mockResolvedValue(
			aPortfolio(1, "Orbit"),
		);
		vi.mocked(this.portfolioService.getPortfolioSettings).mockResolvedValue({
			id: 1,
			name: "Orbit",
		} as IPortfolioSettings);
		vi.mocked(this.portfolioService.createPortfolio).mockResolvedValue({
			id: 9,
			name: "Comet",
		} as IPortfolioSettings);
		vi.mocked(this.portfolioService.updatePortfolio).mockImplementation(
			async (settings) => settings,
		);
		vi.mocked(
			this.portfolioService.refreshFeaturesForPortfolio,
		).mockResolvedValue(undefined);
		vi.mocked(this.portfolioService.deletePortfolio).mockResolvedValue(
			undefined,
		);
		vi.mocked(
			this.workTrackingSystemService.getConfiguredWorkTrackingSystems,
		).mockResolvedValue([
			new WorkTrackingSystemConnection({
				name: "My connection",
				workTrackingSystem: "AzureDevOps",
				options: [],
				id: 1,
			}),
		]);
		vi.mocked(
			this.updateSubscriptionService.unsubscribeFromTeamUpdates,
		).mockResolvedValue(undefined);
		vi.mocked(this.updateSubscriptionService.getUpdateStatus).mockResolvedValue(
			null,
		);
		vi.mocked(
			this.updateSubscriptionService.getGlobalUpdateStatus,
		).mockResolvedValue({ hasActiveUpdates: false, activeCount: 0 });
	}

	theServerHolds(lists: TheLists) {
		vi.mocked(this.teamService.getTeams).mockResolvedValue(
			teamsNamed(...lists.teams),
		);
		vi.mocked(this.portfolioService.getPortfolios).mockResolvedValue(
			portfoliosNamed(...lists.portfolios),
		);
	}

	theTeamsCannotBeLoaded(reason: Error) {
		vi.mocked(this.teamService.getTeams).mockRejectedValue(reason);
	}

	teamsAsked(): number {
		return vi.mocked(this.teamService.getTeams).mock.calls.length;
	}

	portfoliosAsked(): number {
		return vi.mocked(this.portfolioService.getPortfolios).mock.calls.length;
	}

	theUserOpens(page: string) {
		// The create-as-a-copy pages read the copy source from the browser address, not from the router.
		globalThis.history.replaceState({}, "", page);
		const ListsReader = () => {
			this.shownTeams = useTeamsQuery().answer ?? undefined;
			this.shownPortfolios = usePortfoliosQuery().answer ?? undefined;
			return null;
		};
		render(
			<QueryClientProvider client={this.queryClient}>
				<MemoryRouter initialEntries={[page]}>
					<ApiServiceContext.Provider value={this.services}>
						<TerminologyProvider>
							<ListsReader />
							<Routes>
								<Route path="/" element={<OverviewDashboard />} />
								<Route path="/teams/new" element={<EditTeam />} />
								<Route path="/teams/edit/:id" element={<EditTeam />} />
								<Route path="/teams/:id/:tab" element={<TeamDetail />} />
								<Route path="/portfolios/new" element={<EditPortfolio />} />
								<Route
									path="/portfolios/edit/:id"
									element={<EditPortfolio />}
								/>
								<Route
									path="/portfolios/:id/:tab"
									element={<PortfolioDetail />}
								/>
							</Routes>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</MemoryRouter>
			</QueryClientProvider>,
		);
	}

	async theListsAreShown() {
		await waitFor(() => {
			expect(namesOf(this.shownTeams)).toEqual(BEFORE.teams);
			expect(namesOf(this.shownPortfolios)).toEqual(BEFORE.portfolios);
		});
	}

	async theUserSavesTheTeamForm() {
		await waitFor(() => expect(held.saveTeam).toBeDefined());
		await act(async () => {
			await held.saveTeam?.({ id: 1, name: "Bravo" });
		});
	}

	async theUserSavesThePortfolioForm() {
		await waitFor(() => expect(held.savePortfolio).toBeDefined());
		await act(async () => {
			await held.savePortfolio?.({ id: 1, name: "Comet" });
		});
	}

	async theUserDeletesTheFirst(section: "teams" | "portfolios") {
		this.deletesHappenOn = section;
		await waitFor(() => {
			expect(
				document.querySelectorAll("[data-testid=datagrid-container]"),
			).toHaveLength(2);
		});
		const grids = document.querySelectorAll("[data-testid=datagrid-container]");
		const grid = grids[section === "portfolios" ? 0 : 1];
		fireEvent.click(
			grid.querySelectorAll("[data-testid=delete-item-button]")[0],
		);
		const dialog = await waitFor(() => {
			const found = document.querySelector("[role=dialog]");
			expect(found).not.toBeNull();
			return found as HTMLElement;
		});
		const confirm = await waitFor(() => {
			const found = Array.from(dialog.querySelectorAll("button")).find(
				(button) => button.textContent === "Delete",
			);
			expect(found).toBeDefined();
			expect(found?.disabled).toBe(false);
			return found as HTMLElement;
		});
		act(() => {
			fireEvent.click(confirm);
		});
	}

	async aCallerAsksForTheTeams(): Promise<unknown> {
		let outcome: unknown;
		const Caller = () => {
			const load = useTeamsLoader();
			this.loadTeams = load;
			return null;
		};
		render(
			<QueryClientProvider client={this.queryClient}>
				<ApiServiceContext.Provider value={this.services}>
					<Caller />
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);
		await act(async () => {
			outcome = await this.loadTeams?.().then(
				(teams) => teams,
				(reason: unknown) => reason,
			);
		});
		return outcome;
	}

	private loadTeams: (() => Promise<Team[]>) | undefined;

	async everyoneShows(lists: TheLists) {
		await waitFor(() => {
			expect(
				{
					teams: namesOf(this.shownTeams),
					portfolios: namesOf(this.shownPortfolios),
				},
				`WHAT: the lists the user sees do not carry the change ${JSON.stringify(lists)}${this.deletesHappenOn ? " after the delete" : ""}. WHY: the user's own create, rename or delete must show at once, without a page move. HOW: ask the shared lists again after the page's own change.`,
			).toEqual(lists);
		});
	}
}

const thenTheServicesWereAskedTimes = (
	scenario: OwnChangeScenario,
	expected: { teams: number; portfolios: number },
) => {
	expect(
		{ teams: scenario.teamsAsked(), portfolios: scenario.portfoliosAsked() },
		"WHAT: the Teams and the Portfolios were not each asked once on opening and once after the user's own change. WHY: the page's own change must reach the shared lists at once, once, and the page must not ask more often than that. HOW: let the page call the shared lists' own-change refresh after its change completes.",
	).toEqual(expected);
};

const TEAMS_BEFORE_AND_AFTER_ONE_ASK_EACH = { teams: 2, portfolios: 2 };

describe("The Teams and Portfolios lists after the user's own change on each page", () => {
	it.each([
		{
			change: "creates a Team in the creation wizard",
			page: "/teams/new",
			after: { teams: ["Alpha", "Beta", "Gamma"], portfolios: ["Orbit"] },
			save: (scenario: OwnChangeScenario) => scenario.theUserSavesTheTeamForm(),
		},
		{
			change: "creates a Team as a copy of another Team",
			page: "/teams/new?cloneFrom=1",
			after: { teams: ["Alpha", "Beta", "Gamma"], portfolios: ["Orbit"] },
			save: (scenario: OwnChangeScenario) => scenario.theUserSavesTheTeamForm(),
		},
		{
			change: "saves an edited Team on the edit page",
			page: "/teams/edit/1",
			after: { teams: ["Bravo", "Beta"], portfolios: ["Orbit"] },
			save: (scenario: OwnChangeScenario) => scenario.theUserSavesTheTeamForm(),
		},
		{
			change: "renames a Team in the Settings tab of its page",
			page: "/teams/1/settings",
			after: { teams: ["Bravo", "Beta"], portfolios: ["Orbit"] },
			save: (scenario: OwnChangeScenario) => scenario.theUserSavesTheTeamForm(),
		},
		{
			change: "creates a Portfolio in the creation wizard",
			page: "/portfolios/new",
			after: { teams: ["Alpha", "Beta"], portfolios: ["Orbit", "Comet"] },
			save: (scenario: OwnChangeScenario) =>
				scenario.theUserSavesThePortfolioForm(),
		},
		{
			change: "creates a Portfolio as a copy of another Portfolio",
			page: "/portfolios/new?cloneFrom=1",
			after: { teams: ["Alpha", "Beta"], portfolios: ["Orbit", "Comet"] },
			save: (scenario: OwnChangeScenario) =>
				scenario.theUserSavesThePortfolioForm(),
		},
		{
			change: "saves an edited Portfolio on the edit page",
			page: "/portfolios/edit/1",
			after: { teams: ["Alpha", "Beta"], portfolios: ["Comet"] },
			save: (scenario: OwnChangeScenario) =>
				scenario.theUserSavesThePortfolioForm(),
		},
		{
			change: "renames a Portfolio in the Settings tab of its page",
			page: "/portfolios/1/settings",
			after: { teams: ["Alpha", "Beta"], portfolios: ["Comet"] },
			save: (scenario: OwnChangeScenario) =>
				scenario.theUserSavesThePortfolioForm(),
		},
		{
			change: "deletes a Team on the overview",
			page: "/",
			after: { teams: ["Beta"], portfolios: ["Orbit"] },
			save: (scenario: OwnChangeScenario) =>
				scenario.theUserDeletesTheFirst("teams"),
		},
		{
			change: "deletes a Portfolio on the overview",
			page: "/",
			after: { teams: ["Alpha", "Beta"], portfolios: [] },
			save: (scenario: OwnChangeScenario) =>
				scenario.theUserDeletesTheFirst("portfolios"),
		},
	])(
		"shows the lists with the change at once, asking each list once more, when the user $change",
		async ({ page, after, save }) => {
			const scenario = new OwnChangeScenario();
			scenario.theUserOpens(page);
			await scenario.theListsAreShown();
			scenario.theServerHolds(after);

			await save(scenario);

			await scenario.everyoneShows(after);
			thenTheServicesWereAskedTimes(
				scenario,
				TEAMS_BEFORE_AND_AFTER_ONE_ASK_EACH,
			);
		},
	);
});

describe("The Teams a settings editor asks for as a promise", () => {
	it("fails with the reason the Teams could not be loaded, after asking once", async () => {
		const scenario = new OwnChangeScenario();
		const reason = new Error("teams unavailable");
		scenario.theTeamsCannotBeLoaded(reason);

		const outcome = await scenario.aCallerAsksForTheTeams();

		expect(
			outcome,
			"WHAT: the caller was not told the underlying reason. WHY: the settings editor shows why its Teams are missing, not an internal wrapper. HOW: reject with the reason the service gave.",
		).toBe(reason);
		expect(
			scenario.teamsAsked(),
			"WHAT: the Teams were asked more than once for one failed ask. WHY: the shared Teams read does not retry, so the promise-style caller must not either, or one failure costs three asks. HOW: ask without retries, like the reader.",
		).toBe(1);
	});
});
