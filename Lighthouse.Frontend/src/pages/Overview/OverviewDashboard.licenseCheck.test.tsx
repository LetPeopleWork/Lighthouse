import { createTheme, ThemeProvider } from "@mui/material";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { LicenseCheckStatus } from "../../hooks/useLicenseRestrictions";
import { Portfolio } from "../../models/Portfolio/Portfolio";
import { Team } from "../../models/Team/Team";
import { WorkTrackingSystemConnection } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockRbacService,
	createMockTeamService,
	createMockUpdateSubscriptionService,
	createMockWorkTrackingSystemService,
} from "../../tests/MockApiServiceProvider";
import OverviewDashboard from "./OverviewDashboard";

const COULD_NOT_CHECK =
	"Couldn't check your license. Reload the page to try again.";

/** What the licence check answers in a scenario; the hook's own tests cover how it gets there. */
const theCheck = vi.hoisted(() => ({
	current: {
		status: "ready" as LicenseCheckStatus,
		canCreateTeam: true,
		canCreatePortfolio: true,
	},
}));

vi.mock("../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		...theCheck.current,
		canUpdateTeamData: true,
		canUpdatePortfolioData: true,
		licenseStatus: null,
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

vi.mock(
	"../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => vi.fn(),
	}),
);

const northwindPlatform = Object.assign(new Portfolio(), {
	id: 1,
	name: "Northwind Platform",
});

const atlas = Object.assign(new Team(), { id: 1, name: "Atlas" });

// Tooltips open the moment they are hovered, so "nothing opened" is read at once rather than after a guessed wait.
const tooltipsOpenAtOnce = createTheme({
	components: {
		MuiTooltip: {
			defaultProps: {
				enterDelay: 0,
				enterNextDelay: 0,
				leaveDelay: 0,
				slotProps: { transition: { timeout: 0 } },
			},
		},
	},
});

beforeEach(() => {
	localStorage.clear();
});

const givenTheLicenceCheck = (
	status: LicenseCheckStatus,
	createAllowed: boolean,
) => {
	theCheck.current = {
		status,
		canCreateTeam: createAllowed,
		canCreatePortfolio: createAllowed,
	};
};

/** The Overview as an administrator opens it; the instance holds a connection and the Teams and Portfolios given. */
const anAdminOpensTheOverview = async ({
	teams = [atlas],
	portfolios = [northwindPlatform],
}: {
	teams?: Team[];
	portfolios?: Portfolio[];
} = {}) => {
	const workTrackingSystemService = {
		...createMockWorkTrackingSystemService(),
		getConfiguredWorkTrackingSystems: vi.fn().mockResolvedValue([
			new WorkTrackingSystemConnection({
				id: 1,
				name: "Northwind Jira",
				workTrackingSystem: "Jira",
				options: [],
			}),
		]),
		getWorkTrackingSystems: vi.fn().mockResolvedValue([]),
	};
	const rbacService = {
		...createMockRbacService(),
		getStatus: vi.fn().mockResolvedValue({
			enabled: false,
			premiumGateSatisfied: true,
			hasSystemAdmin: true,
			hasEmergencyAdminConfigured: false,
			readyForEnablement: true,
		}),
	};
	const updateSubscriptionService = {
		...createMockUpdateSubscriptionService(),
		getGlobalUpdateStatus: vi
			.fn()
			.mockResolvedValue({ hasActiveUpdates: false, activeCount: 0 }),
	};

	render(
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ThemeProvider theme={tooltipsOpenAtOnce}>
				<MemoryRouter>
					<ApiServiceContext.Provider
						value={createMockApiServiceContext({
							portfolioService: {
								...createMockPortfolioService(),
								getPortfolios: vi.fn().mockResolvedValue(portfolios),
							},
							teamService: {
								...createMockTeamService(),
								getTeams: vi.fn().mockResolvedValue(teams),
							},
							rbacService,
							updateSubscriptionService,
							workTrackingSystemService,
						})}
					>
						<TerminologyProvider>
							<OverviewDashboard />
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</MemoryRouter>
			</ThemeProvider>
		</QueryClientProvider>,
	);

	return {
		addTeam: await theHeaderAction("Add Team"),
		addPortfolio: await theHeaderAction("Add Portfolio"),
	};
};

/** The header's action of that name; the Get Started step can carry the same name while it is shown. */
const theHeaderAction = async (name: string): Promise<HTMLElement> => {
	const header = (await screen.findAllByRole("button", { name })).find(
		(button) => button.dataset.testid !== "onboarding-cta",
	);
	if (!header) throw new Error(`The Overview's header has no ${name}.`);
	return header;
};

/** The Get Started step's action, once the stepper shows the step the instance is at. */
const theGetStartedAction = async (): Promise<HTMLElement> =>
	within(await screen.findByTestId("onboarding-stepper")).getByTestId(
		"onboarding-cta",
	);

/**
 * Hovers the action the way a reader does: a disabled button cannot be hovered itself, so its wrapper is. Under
 * this theme a tooltip is on screen as soon as the hover lands, so "nothing opens" is read straight away. The
 * scenarios that expect a reason read it at that same instant, which is what shows the reading is not too early.
 */
const hoveringShows = async (action: HTMLElement, reason: string) => {
	const user = userEvent.setup();
	await user.hover(action.parentElement ?? action);
	expect(screen.getByRole("tooltip")).toHaveTextContent(reason);
	await user.unhover(action.parentElement ?? action);
	await waitFor(() => expect(screen.queryByRole("tooltip")).toBeNull());
};

const hoveringShowsNothing = async (action: HTMLElement) => {
	const user = userEvent.setup();
	await user.hover(action.parentElement ?? action);
	expect(screen.queryByRole("tooltip")).toBeNull();
	await user.unhover(action.parentElement ?? action);
};

describe("Story 6406 (slice 02): the Overview's create actions while the licence is checked", () => {
	it("while the licence is checked, Add Team and Add Portfolio are disabled and say nothing", async () => {
		givenTheLicenceCheck("loading", false);

		const { addTeam, addPortfolio } = await anAdminOpensTheOverview();

		expect(addTeam).toBeDisabled();
		expect(addPortfolio).toBeDisabled();
		await hoveringShowsNothing(addTeam);
		await hoveringShowsNothing(addPortfolio);
	});

	it("when the licence cannot be checked, Add Team and Add Portfolio are disabled and say why", async () => {
		givenTheLicenceCheck("failed", false);

		const { addTeam, addPortfolio } = await anAdminOpensTheOverview();

		expect(addTeam).toBeDisabled();
		expect(addPortfolio).toBeDisabled();
		await hoveringShows(addTeam, COULD_NOT_CHECK);
		await hoveringShows(addPortfolio, COULD_NOT_CHECK);
	});

	it("once the licence is checked and the free limit is reached, the actions still point to a premium license", async () => {
		givenTheLicenceCheck("ready", false);

		const { addTeam } = await anAdminOpensTheOverview();

		expect(addTeam).toBeDisabled();
		await hoveringShows(addTeam, "This feature requires a premium license.");
	});

	it("once the licence is checked and there is room, the actions are enabled", async () => {
		givenTheLicenceCheck("ready", true);

		const { addTeam, addPortfolio } = await anAdminOpensTheOverview();

		expect(addTeam).toBeEnabled();
		expect(addPortfolio).toBeEnabled();
	});
});

describe("Story 6406 (slice 02): the Get Started steps follow the same licence check", () => {
	it("while the licence is checked, the Add Team step is disabled and says nothing", async () => {
		givenTheLicenceCheck("loading", false);

		await anAdminOpensTheOverview({ teams: [], portfolios: [] });
		const addTeam = await theGetStartedAction();

		expect(addTeam).toHaveTextContent("Add Team");
		expect(addTeam).toBeDisabled();
		await hoveringShowsNothing(addTeam);
	});

	it("when the licence cannot be checked, the Add Team step is disabled and says why", async () => {
		givenTheLicenceCheck("failed", false);

		await anAdminOpensTheOverview({ teams: [], portfolios: [] });
		const addTeam = await theGetStartedAction();

		expect(addTeam).toHaveTextContent("Add Team");
		expect(addTeam).toBeDisabled();
		await hoveringShows(addTeam, COULD_NOT_CHECK);
	});

	it("when the licence cannot be checked, the Add Portfolio step is disabled and says why", async () => {
		givenTheLicenceCheck("failed", false);

		await anAdminOpensTheOverview({ teams: [atlas], portfolios: [] });
		const addPortfolio = await theGetStartedAction();

		expect(addPortfolio).toHaveTextContent("Add Portfolio");
		expect(addPortfolio).toBeDisabled();
		await hoveringShows(addPortfolio, COULD_NOT_CHECK);
	});

	it("once the licence is checked and there is room, the Add Team step is enabled, as today", async () => {
		givenTheLicenceCheck("ready", true);

		await anAdminOpensTheOverview({ teams: [], portfolios: [] });
		const addTeam = await theGetStartedAction();

		expect(addTeam).toHaveTextContent("Add Team");
		expect(addTeam).toBeEnabled();
	});
});
