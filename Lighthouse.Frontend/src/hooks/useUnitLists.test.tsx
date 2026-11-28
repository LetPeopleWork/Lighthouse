import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, render, waitFor } from "@testing-library/react";
import type { ReactElement } from "react";
import {
	MemoryRouter,
	type NavigateFunction,
	Route,
	Routes,
	useNavigate,
} from "react-router";
import { describe, expect, it, vi } from "vitest";
import PageMoveRefresh from "../components/App/PageMoveRefresh/PageMoveRefresh";
import SurveyNudge from "../components/SurveyNudge/SurveyNudge";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { Portfolio } from "../models/Portfolio/Portfolio";
import { Team } from "../models/Team/Team";
import { ApiError } from "../services/Api/ApiError";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockPortfolioService,
	createMockTeamService,
} from "../tests/MockApiServiceProvider";
import {
	type LicenseCheckStatus,
	useLicenseRestrictions,
	useLicenseStatusQuery,
} from "./useLicenseRestrictions";

// A shared read gives its answer as `answer` (page-fresh reads) or as `data` (plain queries); a test reads
// whichever the owner offers, so it pins what the user sees and not how the read is wrapped.
interface SharedRead<T> {
	answer?: T | null;
	data?: T | null;
	isLoading: boolean;
	isFailed?: boolean;
	askAgain?: () => void;
}

interface UnitListsModule {
	useTeamsQuery?: () => SharedRead<Team[]>;
	usePortfoliosQuery?: () => SharedRead<Portfolio[]>;
	useUnitListsChanged?: () => () => unknown;
	useTeamsLoader?: () => () => Promise<Team[]>;
}

// The shared lists live in code the product does not have yet. Looking them up this way keeps their absence a
// failed expectation with a message, not a collection error that hides every other test.
const unitListsModules = import.meta.glob<UnitListsModule>(
	"./useUnitLists.ts",
	{
		eager: true,
	},
);

const theUnitLists = (): Required<UnitListsModule> => {
	const module = unitListsModules["./useUnitLists.ts"];
	const missing = [
		"useTeamsQuery",
		"usePortfoliosQuery",
		"useUnitListsChanged",
		"useTeamsLoader",
	].filter(
		(name) => typeof module?.[name as keyof UnitListsModule] !== "function",
	);
	if (missing.length > 0) {
		expect.fail(
			`WHAT: hooks/useUnitLists.ts does not offer ${missing.join(", ")}. WHY: the Teams list and the Portfolios list are fetched once and shared, and the user's own create, rename or delete must show in them at once. HOW: add hooks/useUnitLists.ts exporting useTeamsQuery, usePortfoliosQuery, useUnitListsChanged and useTeamsLoader.`,
		);
	}
	return module as Required<UnitListsModule>;
};

const answerOf = <T,>(read: SharedRead<T>): T | undefined =>
	(read.answer ?? read.data) || undefined;

// The cache is set up as the app sets it up, so a shared read that forgets its own freshness rules shows here.
const theAppsQueryClient = () =>
	new QueryClient({
		defaultOptions: {
			queries: {
				staleTime: 1000 * 60 * 5,
				gcTime: 1000 * 60 * 30,
				retry: 2,
				refetchOnWindowFocus: false,
				refetchOnMount: true,
				refetchOnReconnect: true,
			},
		},
	});

const FIRST_PAGE = "/page-a";
const PAGE_WITHOUT_LISTS = "/page-b";
const THIRD_PAGE = "/page-c";

const aTeam = (id: number, name: string): Team => {
	const team = new Team();
	team.id = id;
	team.name = name;
	return team;
};

const teamsNamed = (...names: string[]): Team[] =>
	names.map((name, index) => aTeam(index + 1, name));

const portfoliosNamed = (...names: string[]): Portfolio[] =>
	names.map((name, index) => {
		const portfolio = new Portfolio();
		portfolio.id = index + 1;
		portfolio.name = name;
		return portfolio;
	});

const FREE_LICENSE: ILicenseStatus = {
	hasLicense: false,
	isValid: false,
	canUsePremiumFeatures: false,
};

interface SeenLists {
	teams: SharedRead<Team[]>;
	portfolios: SharedRead<Portfolio[]>;
	license: ReturnType<typeof useLicenseStatusQuery>;
}

interface HowTheAppRuns {
	shellReadsTheLists: boolean;
}

class UnitListsScenario {
	readonly seen = new Map<string, SeenLists>();
	readonly gates = new Map<
		string,
		{ status: LicenseCheckStatus; canCreateTeam: boolean }
	>();
	private readonly teamService = createMockTeamService();
	private readonly portfolioService = createMockPortfolioService();
	private readonly licensingService = createMockLicensingService();
	private readonly services = createMockApiServiceContext({
		teamService: this.teamService,
		portfolioService: this.portfolioService,
		licensingService: this.licensingService,
	});
	private readonly queryClient = theAppsQueryClient();
	private navigate: NavigateFunction | undefined;
	private changed: (() => unknown) | undefined;
	private loadTeams: (() => Promise<Team[]>) | undefined;

	constructor() {
		this.theTeamsAre(teamsNamed("Alpha", "Beta"));
		this.thePortfoliosAre(portfoliosNamed("Orbit"));
		vi.mocked(this.licensingService.getLicenseStatus).mockResolvedValue(
			FREE_LICENSE,
		);
	}

	theTeamsAre(teams: Team[]) {
		vi.mocked(this.teamService.getTeams).mockResolvedValue(teams);
	}

	thePortfoliosAre(portfolios: Portfolio[]) {
		vi.mocked(this.portfolioService.getPortfolios).mockResolvedValue(
			portfolios,
		);
	}

	theTeamsCannotBeLoaded() {
		vi.mocked(this.teamService.getTeams).mockRejectedValue(
			new Error("teams unavailable"),
		);
	}

	theUserMayNotReadTheTeams() {
		vi.mocked(this.teamService.getTeams).mockRejectedValue(
			new ApiError(403, "Forbidden"),
		);
	}

	teamsAsked(): number {
		return vi.mocked(this.teamService.getTeams).mock.calls.length;
	}

	portfoliosAsked(): number {
		return vi.mocked(this.portfolioService.getPortfolios).mock.calls.length;
	}

	licenseAsked(): number {
		return vi.mocked(this.licensingService.getLicenseStatus).mock.calls.length;
	}

	theUserOpens(page: string, how: HowTheAppRuns) {
		theUnitLists();
		render(this.theApp(page, how));
	}

	async theUserMovesTo(page: string) {
		await act(async () => {
			await this.navigate?.(page);
		});
	}

	async theUsersOwnChangeCompletes() {
		await act(async () => {
			await this.changed?.();
		});
	}

	async aCallerAsksForTheTeams(): Promise<Team[] | undefined> {
		let teams: Team[] | undefined;
		await act(async () => {
			teams = await this.loadTeams?.();
		});
		return teams;
	}

	oneReaderRetries(name: string) {
		act(() => {
			this.seen.get(name)?.teams.askAgain?.();
		});
	}

	async everyoneShowsTheLists(names: string[]) {
		await waitFor(() => {
			for (const name of names) {
				const lists = this.seen.get(name);
				expect(
					[
						lists?.teams && answerOf(lists.teams),
						lists?.portfolios && answerOf(lists.portfolios),
						lists?.license && answerOf(lists.license),
					].map((answer) => answer !== undefined),
					`${name} has not received the Teams, the Portfolios and the licence`,
				).toEqual([true, true, true]);
			}
		});
	}

	async everyoneShowsTheTeams(names: string[], teamNames: string[]) {
		await waitFor(() => thenTheyShowTheTeams(this, names, teamNames));
	}

	async everyoneHasFailedToLoadTheTeams(names: string[]) {
		await waitFor(() => {
			for (const name of names) {
				expect(
					this.seen.get(name)?.teams.isFailed,
					`${name} has not been told that the Teams could not be loaded`,
				).toBe(true);
			}
		});
	}

	async theGateIsReady(name: string) {
		await waitFor(() => {
			expect(
				this.gates.get(name)?.status,
				`${name} is still waiting for the counts`,
			).toBe("ready");
		});
	}

	private theApp(page: string, how: HowTheAppRuns): ReactElement {
		const lists = theUnitLists();
		const Navigator = () => {
			this.navigate = useNavigate();
			return null;
		};
		const OwnChanges = () => {
			this.changed = lists.useUnitListsChanged();
			return null;
		};
		const CallerOfTheTeamsLoader = () => {
			this.loadTeams = lists.useTeamsLoader();
			return null;
		};
		const ListsReader = ({ name }: Readonly<{ name: string }>) => {
			this.seen.set(name, {
				teams: lists.useTeamsQuery(),
				portfolios: lists.usePortfoliosQuery(),
				license: useLicenseStatusQuery(),
			});
			return null;
		};
		const LicenseGate = ({ name }: Readonly<{ name: string }>) => {
			const restrictions = useLicenseRestrictions();
			this.gates.set(name, {
				status: restrictions.status,
				canCreateTeam: restrictions.canCreateTeam,
			});
			return null;
		};
		// Every page reader has its own key, so it mounts on its page instead of being kept across the move.
		return (
			<QueryClientProvider client={this.queryClient}>
				<MemoryRouter initialEntries={[page]}>
					<ApiServiceContext.Provider value={this.services}>
						<PageMoveRefresh>
							<Navigator />
							<OwnChanges />
							<CallerOfTheTeamsLoader />
							{how.shellReadsTheLists ? (
								<>
									<ListsReader name="header" />
									<LicenseGate name="gate" />
									<SurveyNudge />
								</>
							) : null}
							<Routes>
								<Route
									path={FIRST_PAGE}
									element={
										<>
											<ListsReader key="a-1" name="a-1" />
											<ListsReader key="a-2" name="a-2" />
										</>
									}
								/>
								<Route
									path={PAGE_WITHOUT_LISTS}
									element={<p>Nothing here</p>}
								/>
								<Route
									path={THIRD_PAGE}
									element={
										<>
											<ListsReader key="c-1" name="c-1" />
											<ListsReader key="c-2" name="c-2" />
										</>
									}
								/>
							</Routes>
						</PageMoveRefresh>
					</ApiServiceContext.Provider>
				</MemoryRouter>
			</QueryClientProvider>
		);
	}
}

const WITH_THE_SHELL_READING = { shellReadsTheLists: true };
const WITH_NO_READER_IN_THE_SHELL = { shellReadsTheLists: false };

const ON_THE_FIRST_PAGE = ["header", "a-1", "a-2"];
const READERS_ON_THE_FIRST_PAGE_ONLY = ["a-1", "a-2"];
const READERS_ON_THE_THIRD_PAGE = ["c-1", "c-2"];

const thenTheyShowTheTeams = (
	scenario: UnitListsScenario,
	names: string[],
	teamNames: string[],
) => {
	for (const name of names) {
		const teams = scenario.seen.get(name)?.teams;
		expect(
			teams && answerOf(teams)?.map((team) => team.name),
			`WHAT: ${name} does not show the Teams ${JSON.stringify(teamNames)}. WHY: every component reads the same shared Teams list, so the menus, the overview and the counts agree. HOW: read the Teams through the shared list and refresh it after an own change or a page move.`,
		).toEqual(teamNames);
	}
};

const thenEachServiceWasAskedTimes = (
	scenario: UnitListsScenario,
	expected: { teams: number; portfolios: number; license: number },
	why: string,
) => {
	expect(
		{
			teams: scenario.teamsAsked(),
			portfolios: scenario.portfoliosAsked(),
			license: scenario.licenseAsked(),
		},
		`WHAT: the Teams, the Portfolios and the licence were asked this often. WHY: ${why} HOW: read each through one shared query, asked once per page and once per own change.`,
	).toEqual(expected);
};

const thenTheyAreLoadingNoLonger = (
	scenario: UnitListsScenario,
	names: string[],
) => {
	for (const name of names) {
		const lists = scenario.seen.get(name);
		expect(
			[lists?.teams.isLoading, lists?.portfolios.isLoading],
			`WHAT: ${name} is still loading after the answer arrived. WHY: a reader that appears after a page without readers must ask and show the list, not wait for ever. HOW: a mounting reader asks when the latest answer is from an earlier page.`,
		).toEqual([false, false]);
	}
};

const thenTheGateAllowsCreatingATeam = (
	scenario: UnitListsScenario,
	name: string,
	allowed: boolean,
) => {
	expect(
		scenario.gates.get(name)?.canCreateTeam,
		`WHAT: the create-Team gate is ${allowed ? "closed" : "open"}, expected ${allowed ? "open" : "closed"}. WHY: without a Premium license the gate counts the Teams the user sees. HOW: count the shared Teams list.`,
	).toBe(allowed);
};

describe("Teams, Portfolios and the license shared by every component on a page", () => {
	it("asks for each of them once when the menus, the page and the nudge all need them", async () => {
		const scenario = new UnitListsScenario();

		scenario.theUserOpens(FIRST_PAGE, WITH_THE_SHELL_READING);

		await scenario.everyoneShowsTheLists(ON_THE_FIRST_PAGE);
		await scenario.theGateIsReady("gate");
		thenEachServiceWasAskedTimes(
			scenario,
			{ teams: 1, portfolios: 1, license: 1 },
			"components on one page share one answer instead of asking once each.",
		);
		thenTheyShowTheTeams(scenario, ON_THE_FIRST_PAGE, ["Alpha", "Beta"]);
	});

	it.each([
		{
			change: "creates a Team",
			before: ["Alpha", "Beta"],
			after: ["Alpha", "Beta", "Gamma"],
			createAllowedAfter: false,
		},
		{
			change: "renames a Team",
			before: ["Alpha", "Beta"],
			after: ["Alpha", "Bravo"],
			createAllowedAfter: true,
		},
		{
			change: "deletes a Team",
			before: ["Alpha", "Beta", "Gamma"],
			after: ["Alpha", "Beta"],
			createAllowedAfter: true,
		},
	])(
		"shows the lists at once, without a page move, when the user $change",
		async ({ before, after, createAllowedAfter }) => {
			const scenario = new UnitListsScenario();
			scenario.theTeamsAre(teamsNamed(...before));
			scenario.theUserOpens(FIRST_PAGE, WITH_THE_SHELL_READING);
			await scenario.everyoneShowsTheTeams(ON_THE_FIRST_PAGE, before);
			await scenario.theGateIsReady("gate");
			scenario.theTeamsAre(teamsNamed(...after));

			await scenario.theUsersOwnChangeCompletes();

			await scenario.everyoneShowsTheTeams(ON_THE_FIRST_PAGE, after);
			await waitFor(() =>
				thenTheGateAllowsCreatingATeam(scenario, "gate", createAllowedAfter),
			);
			expect(
				scenario.teamsAsked(),
				"WHAT: the Teams were asked more or less than twice. WHY: the first page and the own change each ask once for all components. HOW: refresh the shared Teams list once after an own change.",
			).toBe(2);
		},
	);

	it("shows a change made by someone else on the user's next move to another page, not before", async () => {
		const scenario = new UnitListsScenario();
		scenario.theUserOpens(FIRST_PAGE, WITH_THE_SHELL_READING);
		await scenario.everyoneShowsTheTeams(ON_THE_FIRST_PAGE, ["Alpha", "Beta"]);
		scenario.theTeamsAre(teamsNamed("Alpha", "Beta", "Gamma"));

		thenTheyShowTheTeams(scenario, ON_THE_FIRST_PAGE, ["Alpha", "Beta"]);
		await scenario.theUserMovesTo(THIRD_PAGE);

		await scenario.everyoneShowsTheTeams(
			["header", ...READERS_ON_THE_THIRD_PAGE],
			["Alpha", "Beta", "Gamma"],
		);
		thenEachServiceWasAskedTimes(
			scenario,
			{ teams: 2, portfolios: 2, license: 2 },
			"a page move asks once for all components, not once per new component.",
		);
	});

	it("gives the reader on the third page its list after a page that had no reader, asking once", async () => {
		const scenario = new UnitListsScenario();
		scenario.theUserOpens(FIRST_PAGE, WITH_NO_READER_IN_THE_SHELL);
		await scenario.everyoneShowsTheLists(READERS_ON_THE_FIRST_PAGE_ONLY);
		await scenario.theUserMovesTo(PAGE_WITHOUT_LISTS);
		thenEachServiceWasAskedTimes(
			scenario,
			{ teams: 1, portfolios: 1, license: 1 },
			"a page without readers asks for nothing.",
		);
		scenario.theTeamsAre(teamsNamed("Alpha", "Beta", "Gamma"));

		await scenario.theUserMovesTo(THIRD_PAGE);

		await scenario.everyoneShowsTheTeams(READERS_ON_THE_THIRD_PAGE, [
			"Alpha",
			"Beta",
			"Gamma",
		]);
		thenTheyAreLoadingNoLonger(scenario, READERS_ON_THE_THIRD_PAGE);
		thenEachServiceWasAskedTimes(
			scenario,
			{ teams: 2, portfolios: 2, license: 2 },
			"the readers that appear on the third page share one ask.",
		);
	});

	it("tells every reader the Teams could not be loaded, and one retry asks once for all of them", async () => {
		const scenario = new UnitListsScenario();
		scenario.theTeamsCannotBeLoaded();
		scenario.theUserOpens(FIRST_PAGE, WITH_NO_READER_IN_THE_SHELL);
		await scenario.everyoneHasFailedToLoadTheTeams(
			READERS_ON_THE_FIRST_PAGE_ONLY,
		);
		expect(
			scenario.teamsAsked(),
			"WHAT: the Teams were asked more than once for the failure. WHY: readers share one failed ask. HOW: do not retry per reader.",
		).toBe(1);
		scenario.theTeamsAre(teamsNamed("Alpha", "Beta"));

		scenario.oneReaderRetries("a-1");

		await scenario.everyoneShowsTheTeams(READERS_ON_THE_FIRST_PAGE_ONLY, [
			"Alpha",
			"Beta",
		]);
		expect(
			scenario.teamsAsked(),
			"WHAT: one retry asked the Teams more or less than once. WHY: a retry asks again once, for all readers. HOW: retry refetches the shared query.",
		).toBe(2);
	});

	it("counts no Teams and still lets the user create one when the user may not read the Teams list", async () => {
		const scenario = new UnitListsScenario();
		scenario.theUserMayNotReadTheTeams();

		scenario.theUserOpens(FIRST_PAGE, WITH_THE_SHELL_READING);

		await scenario.theGateIsReady("gate");
		thenTheGateAllowsCreatingATeam(scenario, "gate", true);
	});

	it("hands a caller that needs the Teams as a promise the list a reader already loaded, without asking again", async () => {
		const scenario = new UnitListsScenario();
		scenario.theUserOpens(FIRST_PAGE, WITH_NO_READER_IN_THE_SHELL);
		await scenario.everyoneShowsTheTeams(READERS_ON_THE_FIRST_PAGE_ONLY, [
			"Alpha",
			"Beta",
		]);

		const teams = await scenario.aCallerAsksForTheTeams();

		expect(
			teams?.map((team) => team.name),
			"WHAT: the caller did not get the shared Teams list. WHY: the settings editors need the Teams as a promise. HOW: let the loader return the shared Teams answer.",
		).toEqual(["Alpha", "Beta"]);
		expect(
			scenario.teamsAsked(),
			"WHAT: the loader asked the service again. WHY: a fresh shared answer is reused. HOW: let the loader read the shared query.",
		).toBe(1);
	});
});
