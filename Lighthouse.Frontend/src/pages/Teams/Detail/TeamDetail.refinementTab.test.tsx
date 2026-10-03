import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
	createMockTeamService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import TeamDetail from "./TeamDetail";

/**
 * The Refinement tab on a Team page: where it sits, when it is switched on, what it tells somebody who
 * finds it switched off, and the one usage event that says a Team's refinement states were chosen.
 *
 * Who counts as an editor comes from useRbac() alone - the same answer that decides whether the
 * Settings tab is shown - so an admin is pointed to Settings and a reader, who has no Settings tab,
 * is told a Team admin has to act. Whether this browser agreed to usage data is the reporter's
 * business, so the reporter stands in here and every call it receives is one the page chose to make.
 */

const { reportUsage, terms } = vi.hoisted(() => ({
	reportUsage: vi.fn(),
	terms: { current: {} as Record<string, string> },
}));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canUpdateTeamData: true,
		updateTeamDataTooltip: "",
		canUpdateTeamSettings: true,
		updateTeamSettingsTooltip: "",
	}),
}));

vi.mock("./TeamFeaturesView", () => ({
	default: () => <div data-testid="team-features-view" />,
}));

vi.mock("./TeamForecastView", () => ({
	default: () => <div data-testid="team-forecast-view" />,
}));

vi.mock("./TeamMetricsView", () => ({
	default: () => <div data-testid="team-metrics-view" />,
}));

vi.mock("./Refinement/RefinementView", () => ({
	default: () => <div data-testid="team-refinement-view" />,
}));

// Stands in for the settings form: one button that saves the Team's settings with Backlog chosen as
// its refinement state, through the same save the real form is handed.
vi.mock("../../../components/Common/Team/ModifyTeamSettings", () => ({
	default: ({
		getTeamSettings,
		saveTeamSettings,
	}: {
		getTeamSettings: () => Promise<ITeamSettings>;
		saveTeamSettings: (settings: ITeamSettings) => Promise<void>;
	}) => (
		<button
			type="button"
			onClick={async () => {
				const settings = await getTeamSettings();
				try {
					await saveTeamSettings({
						...settings,
						refinement: { states: [{ state: "Backlog" }] },
					});
				} catch {
					// The page under test owns what a refused save shows; the stand-in only stops it escaping.
				}
			}}
		>
			Save Backlog as a refinement state
		</button>
	),
}));

let mockParams: { id: string; tab?: string } = { id: "1", tab: "forecasts" };
const mockNavigate = vi.fn();

vi.mock("react-router", async () => {
	const actual = await vi.importActual("react-router");
	return {
		...actual,
		useParams: () => mockParams,
		useNavigate: () => mockNavigate,
	};
});

const TEAM_REFINEMENT_CONFIGURED = "TeamRefinementConfigured";
const EDITOR_TOOLTIP = "Choose refinement states in Settings → Refinement";
const READER_TOOLTIP = "A Team admin needs to choose refinement states first";

// Spelled out: the keys join the shared list together with the words the server seeds for them.
const REFINEMENT_KEY = "refinement";
const REFINEMENTS_KEY = "refinements";

const defaultTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.TEAM]: "Team",
	[TERMINOLOGY_KEYS.FEATURES]: "Features",
	[TERMINOLOGY_KEYS.PORTFOLIO]: "Portfolio",
	[REFINEMENT_KEY]: "Refinement",
	[REFINEMENTS_KEY]: "Refinements",
};

const aTeam = (refinementConfigured: boolean) => ({
	id: 1,
	name: "Team Gravity",
	features: [{ id: 1, name: "Feature 1", key: "F-1" }],
	portfolios: [],
	tags: [],
	workItemTypes: [],
	lastUpdated: new Date(),
	serviceLevelExpectationProbability: 85,
	serviceLevelExpectationRange: 7,
	systemWIPLimit: 0,
	featureWip: 1,
	useFixedDatesForThroughput: false,
	throughputStartDate: new Date(),
	throughputEndDate: new Date(),
	refinementConfigured,
});

type Viewer = "teamAdmin" | "reader" | "rbacOff";

const summaries: Record<Viewer, object> = {
	teamAdmin: { isRbacEnabled: true, isSystemAdmin: false, adminTeamIds: [1] },
	reader: { isRbacEnabled: true, isSystemAdmin: false, adminTeamIds: [] },
	rbacOff: { isRbacEnabled: false, isSystemAdmin: false, adminTeamIds: [] },
};

const renderTheTeamPage = ({
	viewer,
	teamReads,
	saveRefused = false,
}: {
	viewer: Viewer;
	teamReads: ReturnType<typeof aTeam>[];
	saveRefused?: boolean;
}) => {
	const teamService = createMockTeamService();
	const getTeam = vi.fn();
	for (const read of teamReads) {
		getTeam.mockResolvedValueOnce(read);
	}
	getTeam.mockResolvedValue(teamReads[teamReads.length - 1]);
	teamService.getTeam = getTeam;
	teamService.getTeamSettings = vi.fn().mockResolvedValue({
		id: 1,
		name: "Team Gravity",
		toDoStates: ["Backlog"],
		doingStates: ["Next", "Analysing"],
		doneStates: ["Done"],
		refinement: null,
	});
	teamService.updateTeam = saveRefused
		? vi.fn().mockRejectedValue(new Error("refused"))
		: vi.fn().mockImplementation(async (settings: ITeamSettings) => settings);

	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		...summaries[viewer],
		canCreateTeam: false,
		canCreatePortfolio: false,
	});

	const updateSubscriptionService = createMockUpdateSubscriptionService();
	updateSubscriptionService.subscribeToTeamUpdates = vi.fn();
	updateSubscriptionService.unsubscribeFromTeamUpdates = vi
		.fn()
		.mockResolvedValue(undefined);
	updateSubscriptionService.getUpdateStatus = vi.fn().mockResolvedValue(null);

	render(
		<BrowserRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					teamService,
					rbacService,
					updateSubscriptionService,
				})}
			>
				<TeamDetail />
			</ApiServiceContext.Provider>
		</BrowserRouter>,
	);

	return { teamService };
};

const theRefinementTab = async (name = "Refinement") =>
	screen.findByRole("tab", { name });

const theTooltipOf = async (tab: HTMLElement, label: string) => {
	fireEvent.mouseOver(within(tab).getByText(label));
	return screen.findByRole("tooltip");
};

describe("The Refinement tab on a Team page", () => {
	beforeEach(() => {
		mockParams = { id: "1", tab: "forecasts" };
		mockNavigate.mockClear();
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-01 @slice-01 @driving_port @contract-shape:pure-function
	it("sits between Metrics and Settings", async () => {
		renderTheTeamPage({ viewer: "teamAdmin", teamReads: [aTeam(true)] });

		await theRefinementTab();

		expect(
			screen.getAllByRole("tab").map((tab) => tab.textContent?.trim()),
		).toEqual([
			"Features",
			"Forecasts",
			"Metrics",
			"Refinement",
			"Settings",
			"Access",
		]);
	});

	// @us-01 @slice-01 @error @contract-shape:pure-function
	it("is switched off for a Team without refinement states and points its admin to Settings", async () => {
		renderTheTeamPage({ viewer: "teamAdmin", teamReads: [aTeam(false)] });

		const tab = await theRefinementTab();
		const tooltip = await theTooltipOf(tab, "Refinement");

		expect(tab).toBeDisabled();
		expect(tooltip).toHaveTextContent(EDITOR_TOOLTIP);
	});

	// @us-01 @slice-01 @error @contract-shape:pure-function
	it("tells a reader, who has no Settings tab, that a Team admin has to choose the states", async () => {
		renderTheTeamPage({ viewer: "reader", teamReads: [aTeam(false)] });

		const tab = await theRefinementTab();
		const tooltip = await theTooltipOf(tab, "Refinement");

		expect(tab).toBeDisabled();
		expect(tooltip).toHaveTextContent(READER_TOOLTIP);
		expect(tooltip).not.toHaveTextContent(/settings/i);
	});

	// @us-01 @slice-01 @error @contract-shape:pure-function
	// Every Community instance runs without sign-in, where everybody may edit the Team.
	it("points everybody to Settings when roles are not enforced", async () => {
		renderTheTeamPage({ viewer: "rbacOff", teamReads: [aTeam(false)] });

		const tab = await theRefinementTab();
		const tooltip = await theTooltipOf(tab, "Refinement");

		expect(tooltip).toHaveTextContent(EDITOR_TOOLTIP);
	});

	// @us-01 @slice-01 @contract-shape:pure-function
	it("is switched on for a reader once the Team has refinement states", async () => {
		renderTheTeamPage({ viewer: "reader", teamReads: [aTeam(true)] });

		expect(await theRefinementTab()).toBeEnabled();
	});

	// @us-02 @slice-01 @boundary @contract-shape:pure-function
	it("says the Team's own word for Refinement on the tab and in its tooltip", async () => {
		terms.current = {
			...defaultTerms,
			[REFINEMENT_KEY]: "Replenishment",
			[REFINEMENTS_KEY]: "Replenishments",
		};
		renderTheTeamPage({ viewer: "teamAdmin", teamReads: [aTeam(false)] });

		const tab = await theRefinementTab("Replenishment");
		const tooltip = await theTooltipOf(tab, "Replenishment");

		expect(screen.queryByRole("tab", { name: "Refinement" })).toBeNull();
		expect(tooltip).toHaveTextContent(
			/choose replenishment states in settings → replenishment/i,
		);
	});

	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it("opens the Refinement view and puts the tab in the address", async () => {
		renderTheTeamPage({ viewer: "reader", teamReads: [aTeam(true)] });

		await userEvent.click(await theRefinementTab());

		expect(await screen.findByTestId("team-refinement-view")).toBeVisible();
		expect(mockNavigate).toHaveBeenCalledWith("/teams/1/refinement", {
			replace: true,
		});
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it("opens straight onto the Refinement view from an address that names it", async () => {
		mockParams = { id: "1", tab: "refinement" };
		renderTheTeamPage({ viewer: "reader", teamReads: [aTeam(true)] });

		expect(await screen.findByTestId("team-refinement-view")).toBeVisible();
	});

	// @us-01 @slice-01 @error @contract-shape:pure-function
	// The same way an address naming the Features tab of a Team without Features lands on Forecasts.
	it("lands on Forecasts from an address naming the Refinement tab of a Team without refinement states", async () => {
		mockParams = { id: "1", tab: "refinement" };
		renderTheTeamPage({ viewer: "reader", teamReads: [aTeam(false)] });

		expect(await screen.findByTestId("team-forecast-view")).toBeVisible();
		expect(screen.queryByTestId("team-refinement-view")).toBeNull();
		expect(mockNavigate).toHaveBeenCalledWith("/teams/1/forecasts", {
			replace: true,
		});
	});
});

describe("Choosing the first refinement states switches the tab on", () => {
	beforeEach(() => {
		mockParams = { id: "1", tab: "settings" };
		mockNavigate.mockClear();
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("switches the tab on as soon as the save is accepted, without leaving Settings", async () => {
		renderTheTeamPage({
			viewer: "teamAdmin",
			teamReads: [aTeam(false), aTeam(true)],
		});
		expect(await theRefinementTab()).toBeDisabled();

		await userEvent.click(
			screen.getByRole("button", {
				name: "Save Backlog as a refinement state",
			}),
		);

		await waitFor(async () => expect(await theRefinementTab()).toBeEnabled());
	});

	// @us-01 @slice-01 @kpi-OUT-5510-K1-refinement-set-up @contract-shape:bounded-change
	it("reports that the Team's refinement was set up, once", async () => {
		renderTheTeamPage({
			viewer: "teamAdmin",
			teamReads: [aTeam(false), aTeam(true)],
		});
		await theRefinementTab();

		await userEvent.click(
			screen.getByRole("button", {
				name: "Save Backlog as a refinement state",
			}),
		);

		await waitFor(() =>
			expect(reportUsage).toHaveBeenCalledWith({
				name: TEAM_REFINEMENT_CONFIGURED,
			}),
		);
		expect(
			reportUsage.mock.calls.filter(
				([use]) => use.name === TEAM_REFINEMENT_CONFIGURED,
			),
		).toHaveLength(1);
	});

	// @us-01 @slice-01 @kpi-OUT-5510-K1-refinement-set-up @error @contract-shape:bounded-change
	// The event counts Teams being set up, not saves; every later autosave would otherwise count again.
	it("reports nothing when a Team that already had refinement states is saved again", async () => {
		const { teamService } = renderTheTeamPage({
			viewer: "teamAdmin",
			teamReads: [aTeam(true), aTeam(true)],
		});
		await theRefinementTab();

		await userEvent.click(
			screen.getByRole("button", {
				name: "Save Backlog as a refinement state",
			}),
		);

		await waitFor(() => expect(teamService.updateTeam).toHaveBeenCalled());
		expect(reportUsage).not.toHaveBeenCalledWith({
			name: TEAM_REFINEMENT_CONFIGURED,
		});
	});

	// @us-01 @slice-01 @kpi-OUT-5510-K1-refinement-set-up @error @contract-shape:bounded-change
	it("reports nothing and keeps the tab switched off when the save is refused", async () => {
		const { teamService } = renderTheTeamPage({
			viewer: "teamAdmin",
			teamReads: [aTeam(false)],
			saveRefused: true,
		});
		await theRefinementTab();

		await userEvent.click(
			screen.getByRole("button", {
				name: "Save Backlog as a refinement state",
			}),
		);

		await waitFor(() => expect(teamService.updateTeam).toHaveBeenCalled());
		expect(reportUsage).not.toHaveBeenCalledWith({
			name: TEAM_REFINEMENT_CONFIGURED,
		});
		expect(await theRefinementTab()).toBeDisabled();
	});
});
