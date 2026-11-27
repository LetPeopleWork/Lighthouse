import { createTheme, ThemeProvider } from "@mui/material/styles";
import {
	notifyManager,
	QueryClient,
	QueryClientProvider,
} from "@tanstack/react-query";
import { act, render, screen, within } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
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
import { COULD_NOT_LOAD_MESSAGE } from "./WidgetShell";

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

type Owner = "team" | "portfolio";

const team = (() => {
	const t = new Team();
	t.name = "Team Zenith";
	t.id = 2;
	t.serviceLevelExpectationProbability = 70;
	t.serviceLevelExpectationRange = 10;
	return t;
})();

const portfolio = (() => {
	const p = new Portfolio();
	p.name = "Ocean Explorer";
	p.id = 1;
	return p;
})();

let held: HeldMetricsService;

function openTheFlowOverview(owner: Owner): void {
	held = createHeldMetricsService(owner);
	const entity = owner === "team" ? team : portfolio;
	localStorage.setItem(
		`lighthouse:metrics:${owner}:${entity.id}:category`,
		"flow-overview",
	);
	const blackoutPeriodService = createMockBlackoutPeriodService();
	blackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);
	const terminologyService = createMockTerminologyService();

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
							terminologyService,
						})}
					>
						<BaseMetricsView
							entity={entity}
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
}

const theFrame = () => screen.getByTestId("widget-shell-withinSle");
const theStatus = () => theFrame().dataset.widgetStatus;

notifyManager.setScheduler(queueMicrotask);

beforeEach(() => {
	vi.clearAllMocks();
	localStorage.clear();
});

describe.each<Owner>(["team", "portfolio"])(
	"the Within SLE widget on a %s's Flow Overview",
	(owner) => {
		it("reads loading, never an empty area, while its answer is on the way, and ready once it arrived", async () => {
			openTheFlowOverview(owner);

			expect(theStatus()).toBe("loading");

			await act(async () => held.answer({ method: "getWithinSleInfo" }));

			expect(theStatus()).toBe("ready");
		});

		it("reads could-not-load with its message when its answer fails, and shows no answer as current", async () => {
			openTheFlowOverview(owner);

			await act(async () => held.fail({ method: "getWithinSleInfo" }));

			expect(theStatus()).toBe("error");
			expect(
				within(theFrame()).getByText(COULD_NOT_LOAD_MESSAGE),
			).toBeInTheDocument();
		});
	},
);
