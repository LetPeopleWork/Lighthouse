import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, render, waitFor } from "@testing-library/react";
import type { ComponentType, ReactElement, ReactNode } from "react";
import {
	MemoryRouter,
	type NavigateFunction,
	Route,
	Routes,
	useNavigate,
} from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { UserAuthorizationSummary } from "../models/Authorization/RbacModels";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "../tests/MockApiServiceProvider";
import * as permissionHooks from "./useRbac";
import { type RbacState, useRbac } from "./useRbac";

// The page-move refresh and the refresh after the user's own change live in code the product does not have yet.
// Looking them up this way keeps their absence a failed expectation with a message, not a collection error that
// hides every other test.
const pageMoveRefreshModules = import.meta.glob<{
	PageMoveRefresh?: ComponentType<{ children?: ReactNode }>;
	default?: ComponentType<{ children?: ReactNode }>;
}>("../components/App/PageMoveRefresh/PageMoveRefresh.tsx", { eager: true });

const thePageMoveRefresh = ():
	| ComponentType<{ children?: ReactNode }>
	| undefined => {
	const module =
		pageMoveRefreshModules[
			"../components/App/PageMoveRefresh/PageMoveRefresh.tsx"
		];
	return module?.PageMoveRefresh ?? module?.default;
};

type RefreshPermissions = () => unknown;

const theRefreshAfterOwnChanges = (): (() => RefreshPermissions) => {
	const useRefreshPermissions = (
		permissionHooks as unknown as {
			useRefreshPermissions?: () => RefreshPermissions;
		}
	).useRefreshPermissions;
	if (!useRefreshPermissions) {
		expect.fail(
			"WHAT: useRbac.ts offers no useRefreshPermissions. WHY: after the user creates a Team or changes a grant themselves, the actions that follow from it must show at once, without a page move or a reload. HOW: export a hook useRefreshPermissions from useRbac.ts that returns a function asking for the shared permissions again.",
		);
	}
	return useRefreshPermissions;
};

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

const TEAM_PAGE = "/teams/1";
const PORTFOLIO_PAGE = "/portfolios/2";

const teamAdminOf = (teamId: number): UserAuthorizationSummary => ({
	isRbacEnabled: true,
	isSystemAdmin: false,
	canCreateTeam: true,
	canCreatePortfolio: false,
	adminTeamIds: [teamId],
	adminPortfolioIds: [],
});

const portfolioAdminOf = (portfolioId: number): UserAuthorizationSummary => ({
	isRbacEnabled: true,
	isSystemAdmin: false,
	canCreateTeam: false,
	canCreatePortfolio: true,
	adminTeamIds: [],
	adminPortfolioIds: [portfolioId],
});

const NO_ADMIN_RIGHTS: UserAuthorizationSummary = {
	isRbacEnabled: true,
	isSystemAdmin: false,
	canCreateTeam: false,
	canCreatePortfolio: false,
	adminTeamIds: [],
	adminPortfolioIds: [],
};

interface AnswerOnItsWay {
	arrives: (summary: UserAuthorizationSummary) => Promise<void>;
}

class PermissionsScenario {
	readonly seen = new Map<string, RbacState>();
	private readonly rbacService = createMockRbacService();
	private readonly services = createMockApiServiceContext({
		rbacService: this.rbacService,
	});
	private readonly queryClient = theAppsQueryClient();
	private navigate: NavigateFunction | undefined;
	private refresh: RefreshPermissions | undefined;
	private rerenderWith: ((ui: ReactElement) => void) | undefined;

	theAnswerIs(summary: UserAuthorizationSummary) {
		vi.mocked(this.rbacService.getAuthorizationSummary).mockResolvedValue(
			summary,
		);
	}

	thePermissionsCannotBeLoaded() {
		vi.mocked(this.rbacService.getAuthorizationSummary).mockRejectedValue(
			new Error("my-summary unavailable"),
		);
	}

	theNextAnswerIsOnItsWay(): AnswerOnItsWay {
		let resolve!: (summary: UserAuthorizationSummary) => void;
		const promise = new Promise<UserAuthorizationSummary>((res) => {
			resolve = res;
		});
		vi.mocked(this.rbacService.getAuthorizationSummary).mockImplementationOnce(
			() => promise,
		);
		return {
			arrives: async (summary) => {
				await act(async () => {
					resolve(summary);
					await promise;
				});
			},
		};
	}

	timesAsked(): number {
		return vi.mocked(this.rbacService.getAuthorizationSummary).mock.calls
			.length;
	}

	theUserOpens(page: string, options: { withOwnChanges?: boolean } = {}) {
		const useRefreshPermissions = options.withOwnChanges
			? theRefreshAfterOwnChanges()
			: undefined;
		const { rerender } = render(
			this.theApp(page, useRefreshPermissions, false),
		);
		this.rerenderWith = rerender;
	}

	anotherComponentAppearsOn(page: string) {
		this.rerenderWith?.(this.theApp(page, undefined, true));
	}

	async theUserMovesTo(page: string) {
		await act(async () => {
			await this.navigate?.(page);
		});
	}

	async theUsersOwnChangeCompletes() {
		await act(async () => {
			await this.refresh?.();
		});
	}

	oneComponentRetries(name: string) {
		act(() => {
			this.seen.get(name)?.retry();
		});
	}

	async everyComponentHasItsAnswer(names: string[]) {
		await waitFor(() => {
			for (const name of names) {
				expect(this.seen.get(name)?.isLoading, `${name} is still loading`).toBe(
					false,
				);
			}
		});
	}

	private theApp(
		initialPage: string,
		useRefreshPermissions: (() => RefreshPermissions) | undefined,
		withLateComponent: boolean,
	): ReactElement {
		const PageMoveRefresh = thePageMoveRefresh() ?? PassThrough;
		const Navigator = () => {
			this.navigate = useNavigate();
			return null;
		};
		const OwnChanges = ({
			useRefresh,
		}: Readonly<{ useRefresh: () => RefreshPermissions }>) => {
			this.refresh = useRefresh();
			return null;
		};
		return (
			<QueryClientProvider client={this.queryClient}>
				<MemoryRouter initialEntries={[initialPage]}>
					<ApiServiceContext.Provider value={this.services}>
						<PageMoveRefresh>
							<Navigator />
							{useRefreshPermissions ? (
								<OwnChanges useRefresh={useRefreshPermissions} />
							) : null}
							<PermissionsReader name="header" seen={this.seen} />
							<Routes>
								<Route
									path={TEAM_PAGE}
									element={
										<>
											<PermissionsReader name="team-1" seen={this.seen} />
											<PermissionsReader name="team-2" seen={this.seen} />
											{withLateComponent ? (
												<PermissionsReader name="team-late" seen={this.seen} />
											) : null}
										</>
									}
								/>
								<Route
									path={PORTFOLIO_PAGE}
									element={
										<>
											<PermissionsReader name="portfolio-1" seen={this.seen} />
											<PermissionsReader name="portfolio-2" seen={this.seen} />
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

const PassThrough = ({ children }: Readonly<{ children?: ReactNode }>) => (
	<>{children}</>
);

const PermissionsReader = ({
	name,
	seen,
}: Readonly<{ name: string; seen: Map<string, RbacState> }>) => {
	seen.set(name, useRbac());
	return null;
};

const ON_THE_TEAM_PAGE = ["header", "team-1", "team-2"];
const ON_THE_PORTFOLIO_PAGE = ["portfolio-1", "portfolio-2"];

const thenAskedTimes = (
	scenario: PermissionsScenario,
	expected: number,
	why: string,
) => {
	expect(
		scenario.timesAsked(),
		`WHAT: the permissions were asked ${scenario.timesAsked()} times, expected ${expected}. WHY: ${why} HOW: read the permissions through one shared query behind useRbac, asked once per page and once per retry or own change.`,
	).toBe(expected);
};

const thenTheyMayActAsTeamAdminOf = (
	scenario: PermissionsScenario,
	names: string[],
	teamId: number,
) => {
	for (const name of names) {
		const state = scenario.seen.get(name);
		expect(
			state?.isTeamAdmin(teamId),
			`WHAT: ${name} does not offer the Team Admin actions on Team ${teamId}. WHY: the user is Team Admin there in the latest answer. HOW: every component reads the same latest answer.`,
		).toBe(true);
		expect(
			state?.loadFailed,
			`${name} reports the permissions as not loaded`,
		).toBe(false);
	}
};

const thenTheyMayActAsPortfolioAdminOnlyOf = (
	scenario: PermissionsScenario,
	names: string[],
	portfolioId: number,
) => {
	for (const name of names) {
		const state = scenario.seen.get(name);
		expect(
			state?.isPortfolioAdmin(portfolioId),
			`WHAT: ${name} does not offer the Portfolio Admin actions on Portfolio ${portfolioId}. WHY: an administrator granted that right before the user moved. HOW: a page move asks the permissions again once.`,
		).toBe(true);
		expect(
			state?.isTeamAdmin(1),
			`WHAT: ${name} still offers the Team Admin actions on Team 1. WHY: an administrator removed that right before the user moved, and an earlier page's answer is never current on a new page. HOW: show the answer asked after the move.`,
		).toBe(false);
	}
};

const thenTheyAreStillLoading = (
	scenario: PermissionsScenario,
	names: string[],
) => {
	for (const name of names) {
		expect(
			scenario.seen.get(name)?.isLoading,
			`WHAT: ${name} is not loading while the answer asked on its page is still on its way. WHY: an answer asked on an earlier page must never be shown as current on a new page. HOW: a component is loading until an answer asked on or after the page it mounted on arrives.`,
		).toBe(true);
	}
};

describe("Permissions shared by every component on a page", () => {
	it("asks for the permissions once when three components on a page need them", async () => {
		const scenario = new PermissionsScenario();
		scenario.theAnswerIs(teamAdminOf(1));

		scenario.theUserOpens(TEAM_PAGE);

		await scenario.everyComponentHasItsAnswer(ON_THE_TEAM_PAGE);
		thenAskedTimes(
			scenario,
			1,
			"three components on one page share one answer instead of asking once each.",
		);
		thenTheyMayActAsTeamAdminOf(scenario, ON_THE_TEAM_PAGE, 1);
	});

	it("gives a component that appears later on the same page the answer already there, without asking again", async () => {
		const scenario = new PermissionsScenario();
		scenario.theAnswerIs(teamAdminOf(1));
		scenario.theUserOpens(TEAM_PAGE);
		await scenario.everyComponentHasItsAnswer(ON_THE_TEAM_PAGE);

		scenario.anotherComponentAppearsOn(TEAM_PAGE);

		expect(
			scenario.seen.get("team-late")?.isLoading,
			"WHAT: a component that appears on a page that already has its answer is loading. WHY: on the same page the answer is current, so it is reused. HOW: reuse the cached answer of this page.",
		).toBe(false);
		thenAskedTimes(
			scenario,
			1,
			"a component mounted later on the same page reuses the answer of that page.",
		);
		thenTheyMayActAsTeamAdminOf(scenario, ["team-late"], 1);
	});

	it("shows the rights an administrator changed once the user moves to another page", async () => {
		const scenario = new PermissionsScenario();
		scenario.theAnswerIs(teamAdminOf(1));
		scenario.theUserOpens(TEAM_PAGE);
		await scenario.everyComponentHasItsAnswer(ON_THE_TEAM_PAGE);
		const theAnswerAfterTheMove = scenario.theNextAnswerIsOnItsWay();

		await scenario.theUserMovesTo(PORTFOLIO_PAGE);

		thenTheyAreStillLoading(scenario, ON_THE_PORTFOLIO_PAGE);
		await theAnswerAfterTheMove.arrives(portfolioAdminOf(2));
		await scenario.everyComponentHasItsAnswer(ON_THE_PORTFOLIO_PAGE);
		thenAskedTimes(
			scenario,
			2,
			"a page move asks once for all components, not once per new component.",
		);
		thenTheyMayActAsPortfolioAdminOnlyOf(
			scenario,
			[...ON_THE_PORTFOLIO_PAGE, "header"],
			2,
		);
	});

	it("never shows the answer of the page the user left on the page they moved to", async () => {
		const scenario = new PermissionsScenario();
		const theAnswerOfTheLeftPage = scenario.theNextAnswerIsOnItsWay();
		scenario.theUserOpens(TEAM_PAGE);
		const theAnswerAfterTheMove = scenario.theNextAnswerIsOnItsWay();

		await scenario.theUserMovesTo(PORTFOLIO_PAGE);

		await theAnswerOfTheLeftPage.arrives(teamAdminOf(1));
		thenTheyAreStillLoading(scenario, ON_THE_PORTFOLIO_PAGE);
		await theAnswerAfterTheMove.arrives(portfolioAdminOf(2));
		await scenario.everyComponentHasItsAnswer(ON_THE_PORTFOLIO_PAGE);
		thenAskedTimes(
			scenario,
			2,
			"one ask for the first page and one for the page the user moved to.",
		);
		thenTheyMayActAsPortfolioAdminOnlyOf(
			scenario,
			[...ON_THE_PORTFOLIO_PAGE, "header"],
			2,
		);
	});

	it("shows the user's own change of rights at once, without a page move", async () => {
		const scenario = new PermissionsScenario();
		scenario.theAnswerIs(NO_ADMIN_RIGHTS);
		scenario.theUserOpens(TEAM_PAGE, { withOwnChanges: true });
		await scenario.everyComponentHasItsAnswer(ON_THE_TEAM_PAGE);
		scenario.theAnswerIs(teamAdminOf(7));

		await scenario.theUsersOwnChangeCompletes();

		await waitFor(() =>
			thenTheyMayActAsTeamAdminOf(scenario, ON_THE_TEAM_PAGE, 7),
		);
		thenAskedTimes(
			scenario,
			2,
			"the user's own change asks once more for all components on the page.",
		);
	});

	it("gives every component the could-not-load state and today's fallback when the permissions cannot be loaded", async () => {
		const scenario = new PermissionsScenario();
		scenario.thePermissionsCannotBeLoaded();

		scenario.theUserOpens(TEAM_PAGE);

		await waitFor(() => {
			for (const name of ON_THE_TEAM_PAGE) {
				expect(
					scenario.seen.get(name)?.loadFailed,
					`${name} does not report that the permissions could not be loaded`,
				).toBe(true);
			}
		});
		for (const name of ON_THE_TEAM_PAGE) {
			const state = scenario.seen.get(name);
			expect(
				state?.isLoading,
				`${name} is still loading after the failure`,
			).toBe(false);
			expect(
				[
					state?.isSystemAdmin,
					state?.canCreateTeam,
					state?.canCreatePortfolio,
					state?.isTeamAdmin(99),
					state?.isPortfolioAdmin(99),
				],
				`WHAT: ${name} does not show today's permissive fallback. WHY: a failed load behaves as today; the server still refuses what the user may not do. HOW: keep the permissive fallback with loadFailed true.`,
			).toEqual([true, true, true, true, true]);
		}
		thenAskedTimes(
			scenario,
			1,
			"a failed load is asked once for all components, with no automatic retries.",
		);
	});

	it("asks once for every component when one of them retries after a failed load", async () => {
		const scenario = new PermissionsScenario();
		scenario.thePermissionsCannotBeLoaded();
		scenario.theUserOpens(TEAM_PAGE);
		await waitFor(() =>
			expect(scenario.seen.get("team-2")?.loadFailed).toBe(true),
		);
		const askedBeforeTheRetry = scenario.timesAsked();
		scenario.theAnswerIs(teamAdminOf(1));

		scenario.oneComponentRetries("team-1");

		await waitFor(() =>
			thenTheyMayActAsTeamAdminOf(scenario, ON_THE_TEAM_PAGE, 1),
		);
		expect(
			scenario.timesAsked() - askedBeforeTheRetry,
			"WHAT: one retry asked the permissions more or less than once. WHY: a retry asks again once, for all components. HOW: retry refetches the shared query.",
		).toBe(1);
		thenAskedTimes(
			scenario,
			2,
			"one ask for the failed load and one for the retry.",
		);
	});
});
