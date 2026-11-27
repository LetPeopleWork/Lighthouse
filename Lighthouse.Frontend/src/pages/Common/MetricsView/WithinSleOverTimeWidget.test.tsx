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
import { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { createHeldMetricsService } from "../../../tests/HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import { BaseMetricsView } from "./BaseMetricsView";
import { COULD_NOT_LOAD_MESSAGE } from "./WidgetShell";

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

// The chart library draws to SVG, which the test cannot read. The stand-in shows what the widget asked it to
// draw: the dates, every line's values and both axes.
vi.mock("@mui/x-charts", () => ({
	LineChart: ({
		xAxis,
		yAxis,
		series,
	}: {
		xAxis?: { data?: unknown[]; label?: string }[];
		yAxis?: { min?: number; max?: number; label?: string }[];
		series?: { label?: string; data?: (number | null)[] }[];
	}) => (
		<div data-testid="drawn-chart">
			{JSON.stringify({
				dates: (xAxis?.[0]?.data ?? []).map(dayOf),
				xLabel: xAxis?.[0]?.label,
				yAxis: yAxis?.[0],
				lines: series?.map((line) => line.data ?? []) ?? [],
			})}
		</div>
	),
}));

function dayOf(value: unknown): string {
	if (typeof value === "string") {
		return value.slice(0, 10);
	}
	const date = new Date(value as Date | number);
	const month = String(date.getMonth() + 1).padStart(2, "0");
	const day = String(date.getDate()).padStart(2, "0");
	return `${date.getFullYear()}-${month}-${day}`;
}

type Drawn = {
	dates: string[];
	xLabel?: string;
	yAxis?: { min?: number; max?: number; label?: string };
	lines: (number | null)[][];
};

type Day = {
	recordedAt: string;
	horizon: number;
	closedCount: number;
	withinCount: number;
	slePercentile: number;
	sleDays: number;
	origin: "Recorded" | "FilledIn";
	verdict: "Measured" | "TooFew";
	sharePercent: number | null;
};

type Answer = {
	currentSlePercentile: number | null;
	currentSleDays: number | null;
	days: Day[];
};

const recorded = (
	recordedAt: string,
	closedCount: number,
	withinCount: number,
	sharePercent: number,
	slePercentile: number,
	sleDays: number,
): Day => ({
	recordedAt,
	horizon: 30,
	closedCount,
	withinCount,
	slePercentile,
	sleDays,
	origin: "Recorded",
	verdict: "Measured",
	sharePercent,
});

const tooFew = (recordedAt: string, closedCount: number): Day => ({
	recordedAt,
	horizon: 30,
	closedCount,
	withinCount: 0,
	slePercentile: 85,
	sleDays: 14,
	origin: "Recorded",
	verdict: "TooFew",
	sharePercent: null,
});

// The 20th was recorded at 70 % within 10 days, the SLE then changed to 85 % within 14 days, the 21st was
// recorded against it, the 22nd had only 3 closed Work Items, and the 23rd was never recorded.
const theSleChangedOnTheTwentyFirst: Answer = {
	currentSlePercentile: 85,
	currentSleDays: 14,
	days: [
		recorded("2026-11-20", 11, 8, 72.7, 70, 10),
		recorded("2026-11-21", 11, 10, 90.9, 85, 14),
		tooFew("2026-11-22", 3),
	],
};

const noSleSet: Answer = {
	currentSlePercentile: null,
	currentSleDays: null,
	days: [],
};

const nothingRecorded: Answer = {
	currentSlePercentile: 70,
	currentSleDays: 10,
	days: [],
};

// The same sentence Percentiles Over Time shows for a range with nothing recorded, written out so a reworded
// sentence fails here.
const NOTHING_RECORDED_SENTENCE =
	"Nothing to show for the selected range. Days appear here as Lighthouse records them.";

type Asked = {
	horizon: number;
	resolve: (answer: Answer) => void;
	reject: (error: Error) => void;
};

let asked: Asked[] = [];

const team = (() => {
	const t = new Team();
	t.name = "Team Zenith";
	t.id = 2;
	t.serviceLevelExpectationProbability = 85;
	t.serviceLevelExpectationRange = 14;
	return t;
})();

function openPredictability(): void {
	const held = createHeldMetricsService("team");
	const metricsService = {
		...held.service,
		getWithinSleOverTime: vi.fn(
			(_id: number, horizon: number) =>
				new Promise<Answer>((resolve, reject) => {
					asked.push({ horizon, resolve, reject });
				}),
		),
	};
	localStorage.setItem(
		`lighthouse:metrics:team:${team.id}:category`,
		"predictability",
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
							metricsService={
								metricsService as unknown as IMetricsService<IWorkItem>
							}
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

function theLatestAsk(): Asked {
	const latest = asked.at(-1);
	expect(
		latest,
		"The Within SLE Over Time chart never asked for its daily record. Predictability shows the chart, and the chart asks for the record of the horizon it shows.",
	).toBeDefined();
	return latest as Asked;
}

async function answerTheLatestAsk(answer: Answer): Promise<void> {
	const latest = theLatestAsk();
	await act(async () => latest.resolve(answer));
}

async function failTheLatestAsk(): Promise<void> {
	const latest = theLatestAsk();
	await act(async () =>
		latest.reject(new Error("within-sle-over-time failed")),
	);
}

function theFrame(): HTMLElement {
	const frame = screen
		.getByText("Within SLE Over Time")
		.closest<HTMLElement>("[data-widget-status]");
	if (!frame) {
		throw new Error(
			"The Within SLE Over Time chart is not inside a widget frame that reports its status. Render it in WidgetShell.",
		);
	}
	return frame;
}

const theStatus = () => theFrame().dataset.widgetStatus;

function theDrawnChart(): Drawn {
	return JSON.parse(
		within(theFrame()).getByTestId("drawn-chart").textContent ?? "{}",
	) as Drawn;
}

function valueOn(line: (number | null)[], dates: string[], day: string) {
	const at = dates.indexOf(day);
	return at === -1 ? undefined : line[at];
}

function theLineThatReads(
	drawn: Drawn,
	expected: Record<string, number>,
): (number | null)[] | undefined {
	return drawn.lines.find((line) =>
		Object.entries(expected).every(
			([day, value]) => valueOn(line, drawn.dates, day) === value,
		),
	);
}

const horizonButton = (days: 30 | 60 | 90) =>
	within(theFrame()).getByRole("button", { name: `${days} days` });

notifyManager.setScheduler(queueMicrotask);

beforeEach(() => {
	vi.clearAllMocks();
	localStorage.clear();
	asked = [];
});

describe("the Within SLE Over Time chart in Predictability", () => {
	it("draws each day's share against the target in force that day, with no point for a day too few to judge or never recorded", async () => {
		openPredictability();
		await answerTheLatestAsk(theSleChangedOnTheTwentyFirst);

		const drawn = theDrawnChart();
		const share = theLineThatReads(drawn, {
			"2026-11-20": 72.7,
			"2026-11-21": 90.9,
		});
		const target = theLineThatReads(drawn, {
			"2026-11-20": 70,
			"2026-11-21": 85,
		});

		expect(
			share,
			`No line reads 72.7 % on the 20th and 90.9 % on the 21st among ${JSON.stringify(drawn)}. Draw each day's share as the server answered it.`,
		).toBeDefined();
		expect(
			target,
			`No line steps from a 70 % target on the 20th to 85 % on the 21st among ${JSON.stringify(drawn)}. Draw the target each day was held to, stepping on the day the SLE changed.`,
		).toBeDefined();
		const shareLine = share ?? [];
		expect(
			[
				valueOn(shareLine, drawn.dates, "2026-11-22") ?? null,
				valueOn(shareLine, drawn.dates, "2026-11-23") ?? null,
			],
			"A day with 3 closed Work Items, and a day never recorded, must draw no point: not a share, and never 0 %.",
		).toEqual([null, null]);
		expect(
			within(theFrame()).getByText(/fewer than 5 closed Work Items/i),
		).toBeInTheDocument();
		expect(
			{ min: drawn.yAxis?.min, max: drawn.yAxis?.max },
			"The share axis is fixed from 0 to 100, so a small change does not look like a large one.",
		).toEqual({ min: 0, max: 100 });
		expect(
			[drawn.yAxis?.label, drawn.xLabel],
			"Both axes carry a label: the share in percent and the date.",
		).toEqual(["Within SLE (%)", "Date"]);
		expect(theStatus()).toBe("ready");
	});

	it("says that no SLE is set instead of drawing a line", async () => {
		openPredictability();
		await answerTheLatestAsk(noSleSet);

		expect(within(theFrame()).getByText(/No SLE set/)).toBeInTheDocument();
		expect(
			within(theFrame()).queryByTestId("drawn-chart"),
		).not.toBeInTheDocument();
	});

	it("shows the Percentiles Over Time sentence when nothing was recorded in the range", async () => {
		openPredictability();
		await answerTheLatestAsk(nothingRecorded);

		expect(
			within(theFrame()).getByText(NOTHING_RECORDED_SENTENCE),
		).toBeInTheDocument();
		expect(
			within(theFrame()).queryByTestId("drawn-chart"),
		).not.toBeInTheDocument();
	});

	it("reads loading while its answer is on the way, and could-not-load with its message when the answer fails", async () => {
		openPredictability();

		expect(theStatus()).toBe("loading");

		await failTheLatestAsk();

		expect(theStatus()).toBe("error");
		expect(
			within(theFrame()).getByText(COULD_NOT_LOAD_MESSAGE),
		).toBeInTheDocument();
	});

	it("asks for the trailing 30 days first, then the horizon the reader switches to, never showing the old horizon's answer as current, and reports each switch", async () => {
		openPredictability();
		await answerTheLatestAsk(theSleChangedOnTheTwentyFirst);

		expect(asked.map((ask) => ask.horizon)).toEqual([30]);
		expect(horizonButton(30)).toHaveAttribute("aria-pressed", "true");
		expect(reportUsage).not.toHaveBeenCalled();

		fireEvent.click(horizonButton(60));

		expect(asked.map((ask) => ask.horizon)).toEqual([30, 60]);
		expect(theStatus()).toBe("loading");
		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith({
			name: "WithinSleHorizonSwitched",
		});

		await answerTheLatestAsk({
			...theSleChangedOnTheTwentyFirst,
			days: [recorded("2026-11-20", 20, 15, 75, 70, 10)],
		});

		expect(theStatus()).toBe("ready");
		expect(
			theLineThatReads(theDrawnChart(), { "2026-11-20": 75 }),
		).toBeDefined();
	});
});
