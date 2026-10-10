import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	act,
	fireEvent,
	render,
	screen,
	waitForElementToBeRemoved,
	within,
} from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { ICumulativeStateTimeItemsResponse } from "../../../models/Metrics/CumulativeStateTimeItems";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import {
	createHeldMetricsService,
	type HeldMetricsService,
	type MetricsMethod,
} from "../../../tests/HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
} from "../../../tests/MockApiServiceProvider";
import {
	cellText,
	columnsShown,
	rowOrder,
} from "../../../tests/WorkItemsDialogTestKit";
import { BaseMetricsView } from "./BaseMetricsView";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: true },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

vi.mock("./DashboardHeader", () => ({ default: () => null }));

vi.mock("./Dashboard", () => ({
	default: ({
		items,
	}: {
		items?: Array<{ id: string | number; node: ReactNode }>;
	}) => (
		<div>
			{items?.map((item) => (
				<div key={String(item.id)}>{item.node}</div>
			))}
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/CumulativeStateTimeChart", () => ({
	default: ({ onBarClick }: { onBarClick?: (stateName: string) => void }) => (
		<div>
			<button type="button" onClick={() => onBarClick?.("In Progress")}>
				Open the In Progress bar
			</button>
			<button type="button" onClick={() => onBarClick?.("Review")}>
				Open the Review bar
			</button>
		</div>
	),
}));

vi.mock("@mui/x-charts", async (importOriginal) => ({
	...(await importOriginal<typeof import("@mui/x-charts")>()),
	LineChart: () => null,
	BarChart: () => null,
	ScatterChart: () => null,
}));

// Every other chart on the page is a stand-in: only the Cumulative Time per State bars are clicked.
vi.mock("../../../components/Common/Charts/BarRunChart", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/LineRunChart", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/BlockedItemsOverTimeChart", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/CycleTimeScatterPlotChart", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/CycleTimePercentiles", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/EstimationVsCycleTimeChart", () => ({
	default: () => null,
}));
vi.mock(
	"../../../components/Common/Charts/FeatureSizeScatterPlotChart",
	() => ({ default: () => null }),
);
vi.mock("../../../components/Common/Charts/LoadBalanceMatrixChart", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/StackedAreaChart", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/TotalWorkItemAgeRunChart", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/WorkDistributionChart", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/WorkItemAgePercentiles", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/WorkItemAgingChart", () => ({
	default: () => null,
}));
vi.mock("../../../components/Common/Charts/ProcessBehaviourChart", () => ({
	default: () => null,
	ProcessBehaviourChartType: {
		Throughput: "Throughput",
		WorkInProgress: "WorkInProgress",
		TotalWorkItemAge: "TotalWorkItemAge",
		CycleTime: "CycleTime",
		Arrivals: "Arrivals",
		FeatureSize: "FeatureSize",
	},
}));
vi.mock(
	"../../../components/Common/Charts/CumulativeStateTimeScopeControl",
	() => ({ default: () => null }),
);
vi.mock(
	"../../../components/Common/Charts/CumulativeStateTimeItemPicker",
	() => ({ default: () => null }),
);

const RENDER_HEAVY = 30000;

const team = (() => {
	const t = new Team();
	t.name = "Team Zenith";
	t.id = 2;
	return t;
})();

const portfolio = (() => {
	const p = new Portfolio();
	p.name = "Ocean Explorer";
	p.id = 1;
	return p;
})();

type Owner = "team" | "portfolio";

const itemsMethodFor = (owner: Owner): MetricsMethod =>
	owner === "team"
		? "getCumulativeStateTimeItemsForTeam"
		: "getCumulativeStateTimeItemsForPortfolio";

const contributorsTo = (
	state: string,
	...rows: [string, number][]
): ICumulativeStateTimeItemsResponse => ({
	state,
	items: rows.map(([referenceId, daysContributed], index) => ({
		workItemId: index + 1,
		referenceId,
		title: `Work on ${referenceId}`,
		type: "User Story",
		state,
		stateCategory: "Doing",
		url: null,
		daysContributed,
	})),
});

let held: HeldMetricsService;

async function openTheFlowMetrics(owner: Owner): Promise<void> {
	held = createHeldMetricsService(owner);
	const entity = owner === "team" ? team : portfolio;
	localStorage.clear();
	localStorage.setItem(
		`lighthouse:metrics:${owner}:${entity.id}:category`,
		"flow-metrics",
	);
	const blackoutPeriodService = createMockBlackoutPeriodService();
	blackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);

	render(
		<MemoryRouter>
			<QueryClientProvider
				client={
					new QueryClient({ defaultOptions: { queries: { retry: false } } })
				}
			>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({ blackoutPeriodService })}
				>
					<BaseMetricsView
						entity={entity}
						metricsService={held.service as IMetricsService<IWorkItem>}
						title="Work Items"
						defaultDateRange={30}
						doingStates={["In Progress", "Review"]}
						featuresInProgress={[]}
					/>
				</ApiServiceContext.Provider>
			</QueryClientProvider>
		</MemoryRouter>,
	);

	// The page asks in waves (the window settles, then each category's requests go out), so it is
	// answered until the bars are on screen rather than a fixed number of times.
	for (let wave = 0; wave < 10 && !theBars(); wave++) {
		await act(async () => held.answer({ except: itemsMethodFor(owner) }));
	}
}

const theBars = () =>
	screen.queryByRole("button", { name: "Open the In Progress bar" });

function theCoachClicks(bar: string): void {
	fireEvent.click(screen.getByRole("button", { name: `Open the ${bar} bar` }));
}

/** The Work Items dialog, which must be open: a bar that opens nothing is the failure. */
const theOpenDialog = (): HTMLElement => {
	const dialog = screen.queryByRole("dialog");
	expect(dialog, "the Work Items dialog is open").not.toBeNull();
	return dialog as HTMLElement;
};

const COULD_NOT_LOAD =
	"These Work Items couldn't be loaded. Close and reopen to try again.";

describe("the items behind a Cumulative Time per State bar", () => {
	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it.skip.each(["team", "portfolio"] as const)(
		"on a %s's dashboard, the dialog opens at once in its loading look",
		async (owner) => {
			await openTheFlowMetrics(owner);

			theCoachClicks("In Progress");

			expect(
				within(theOpenDialog()).getByRole("progressbar"),
			).toBeInTheDocument();
			expect(columnsShown()).toEqual([
				"referenceId",
				"name",
				"type",
				"state",
				"daysContributed",
			]);
			expect(
				within(theOpenDialog()).queryByText("No items to display"),
			).toBeNull();
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it.skip.each(["team", "portfolio"] as const)(
		"on a %s's dashboard, the items arrive with the days each contributed",
		async (owner) => {
			await openTheFlowMetrics(owner);
			theCoachClicks("In Progress");

			await act(async () =>
				held.answer(
					{ method: itemsMethodFor(owner) },
					contributorsTo("In Progress", ["ST-1", 2.54], ["ST-2", 7]),
				),
			);

			expect(within(theOpenDialog()).queryByRole("progressbar")).toBeNull();
			expect(rowOrder()).toEqual(["ST-2", "ST-1"]);
			expect(cellText("daysContributed", "ST-1")).toBe("2.5");
			expect(cellText("daysContributed", "ST-2")).toBe("7");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it.skip(
		"says the items couldn't be loaded instead of opening nothing when the request fails",
		async () => {
			await openTheFlowMetrics("team");
			theCoachClicks("In Progress");

			await act(async () =>
				held.fail({ method: "getCumulativeStateTimeItemsForTeam" }),
			);

			expect(
				within(theOpenDialog()).getByText(COULD_NOT_LOAD),
			).toBeInTheDocument();
			expect(rowOrder()).toEqual([]);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it.skip(
		"a late answer for the bar clicked before never lands in the next bar's dialog",
		async () => {
			await openTheFlowMetrics("team");
			theCoachClicks("In Progress");
			fireEvent.click(
				within(theOpenDialog()).getByRole("button", { name: "Close" }),
			);
			await waitForElementToBeRemoved(() => screen.queryByRole("dialog"));
			theCoachClicks("Review");
			const [inProgressRequest, reviewRequest] = held.pending({
				method: "getCumulativeStateTimeItemsForTeam",
			});

			await act(async () =>
				inProgressRequest.answer.resolve(
					contributorsTo("In Progress", ["ST-1", 3]),
				),
			);

			expect(
				within(theOpenDialog()).getByRole("progressbar"),
			).toBeInTheDocument();
			expect(rowOrder()).toEqual([]);

			await act(async () =>
				reviewRequest.answer.resolve(contributorsTo("Review", ["ST-9", 4])),
			);

			expect(rowOrder()).toEqual(["ST-9"]);
		},
		RENDER_HEAVY,
	);
});
