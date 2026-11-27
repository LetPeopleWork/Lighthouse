import { createTheme, ThemeProvider } from "@mui/material/styles";
import {
	notifyManager,
	QueryClient,
	QueryClientProvider,
} from "@tanstack/react-query";
import { act, fireEvent, render, screen, within } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IWithinSleInfo } from "../../../models/Metrics/WithinSleInfo";
import { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import {
	createHeldMetricsService,
	type HeldMetricsService,
} from "../../../tests/HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import { BaseMetricsView } from "./BaseMetricsView";

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));

vi.mock("../../../services/UsageData/usageDataReporter", async (original) => ({
	...(await original<
		typeof import("../../../services/UsageData/usageDataReporter")
	>()),
	useUsageDataReporter: () => reportUsage,
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: false },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

vi.mock("./DashboardHeader", () => ({ default: () => <div /> }));

// Only the Within SLE widget is placed, so no other chart is drawn.
vi.mock("./Dashboard", () => ({
	default: ({
		items,
	}: {
		items?: Array<{ id: string | number; node: ReactNode }>;
	}) => (
		<div>
			{items
				?.filter((item) => String(item.id) === "withinSle")
				.map((item) => (
					<div key={String(item.id)} data-testid="dashboard-item-withinSle">
						{item.node}
					</div>
				))}
		</div>
	),
}));

const team = (() => {
	const t = new Team();
	t.name = "Team Zenith";
	t.id = 2;
	t.serviceLevelExpectationProbability = 70;
	t.serviceLevelExpectationRange = 10;
	return t;
})();

const measured = (
	sharePercent: number,
	comparison: IWithinSleInfo["comparison"],
): IWithinSleInfo => ({
	verdict: "Measured",
	closedCount: 11,
	withinCount: 8,
	slePercentile: 70,
	sleDays: 10,
	sharePercent,
	comparison,
});

const noComparison = { direction: "none", metricLabel: "Within SLE" } as const;

const improvedByFourPoints = {
	direction: "up",
	metricLabel: "Within SLE",
	percentageDelta: "+4.1 pts",
} as const;

let held: HeldMetricsService;

async function openTheFlowOverviewAnswering(info: IWithinSleInfo) {
	held = createHeldMetricsService("team");
	localStorage.setItem(
		`lighthouse:metrics:team:${team.id}:category`,
		"flow-overview",
	);
	const blackoutPeriodService = createMockBlackoutPeriodService();
	blackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);

	render(
		<ThemeProvider theme={createTheme()}>
			<MemoryRouter>
				<QueryClientProvider
					client={
						new QueryClient({ defaultOptions: { queries: { retry: false } } })
					}
				>
					<ApiServiceContext.Provider
						value={createMockApiServiceContext({
							blackoutPeriodService,
							terminologyService: createMockTerminologyService(),
						})}
					>
						<BaseMetricsView
							entity={team}
							metricsService={held.service as IMetricsService<IWorkItem>}
							title="Work Items"
							defaultDateRange={30}
							doingStates={["In Progress"]}
							hasForecastFilter
							featuresInProgress={[]}
						/>
					</ApiServiceContext.Provider>
				</QueryClientProvider>
			</MemoryRouter>
		</ThemeProvider>,
	);

	await act(async () => held.answer({ method: "getWithinSleInfo" }, info));
}

const theHeader = () => screen.getByTestId("widget-shell-header-withinSle");
const theFrame = () => screen.getByTestId("widget-shell-withinSle");
const theTrend = () => screen.queryByTestId("widget-trend-withinSle");

notifyManager.setScheduler(queueMicrotask);

beforeEach(() => {
	vi.clearAllMocks();
	localStorage.clear();
});

describe("the Within SLE widget's verdict", () => {
	it("says Sustain and shows the change against the preceding period when the share is at or above the target", async () => {
		await openTheFlowOverviewAnswering(measured(72.7, improvedByFourPoints));

		const status = within(theHeader()).getByTestId("rag-status");
		expect(status).toHaveTextContent("Sustain");
		expect(status).toHaveAttribute("data-rag", "green");
		expect(theTrend()).toBeInTheDocument();
		expect(within(theFrame()).getByText("72.7 % (8 / 11)")).toBeInTheDocument();
	});

	it.each([
		{ share: 60, status: "Observe", rag: "amber" },
		{ share: 50, status: "Observe", rag: "amber" },
		{ share: 49.9, status: "Act", rag: "red" },
	])(
		"says $status for a share of $share % against a target of 70 %",
		async ({ share, status, rag }) => {
			await openTheFlowOverviewAnswering(measured(share, noComparison));

			const shown = within(theHeader()).getByTestId("rag-status");
			expect(shown).toHaveTextContent(status);
			expect(shown).toHaveAttribute("data-rag", rag);
			expect(theTrend()).not.toBeInTheDocument();
		},
	);

	it("reads that no SLE is set, says Act and shows no change", async () => {
		await openTheFlowOverviewAnswering({
			verdict: "NoSle",
			closedCount: 11,
			withinCount: 0,
			comparison: noComparison,
		});

		expect(within(theFrame()).getByText(/No SLE set/)).toBeInTheDocument();
		const status = within(theHeader()).getByTestId("rag-status");
		expect(status).toHaveTextContent("Act");
		expect(status).toHaveAttribute("data-rag", "red");
		expect(theTrend()).not.toBeInTheDocument();
	});

	it("says how many closed and that this is too few to judge, with no status and no change", async () => {
		await openTheFlowOverviewAnswering({
			verdict: "TooFew",
			closedCount: 3,
			withinCount: 0,
			slePercentile: 70,
			sleDays: 10,
			comparison: noComparison,
		});

		expect(
			within(theFrame()).getByText(
				"3 Work Items closed — too few to judge (needs at least 5)",
			),
		).toBeInTheDocument();
		expect(
			within(theHeader()).queryByTestId("rag-status"),
		).not.toBeInTheDocument();
		expect(theTrend()).not.toBeInTheDocument();
	});
});

describe("the Within SLE widget's usage report", () => {
	it("reports the list opened once when the reader opens View Data, and nothing while it only renders", async () => {
		await openTheFlowOverviewAnswering(measured(72.7, noComparison));
		await act(async () => held.answer());

		expect(reportUsage).not.toHaveBeenCalled();

		fireEvent.click(screen.getByTestId("widget-view-data-withinSle"));

		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith({ name: "WithinSleListOpened" });
	});
});
