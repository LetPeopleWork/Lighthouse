import {
	notifyManager,
	QueryClient,
	QueryClientProvider,
} from "@tanstack/react-query";
import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { addDays } from "date-fns";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MetricsOwnerRequest } from "../../../hooks/useMetricsData";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import {
	createHeldMetricsService,
	type HeldMetricsService,
	windowOf,
} from "../../../tests/HeldMetricsService";
import ThroughputRunChartCard from "./ThroughputRunChartCard";
import WidgetShell, { COULD_NOT_LOAD_MESSAGE } from "./WidgetShell";
import type { WidgetStatus } from "./widgetStatus";

vi.mock("../../../components/Common/Charts/BarRunChart", () => ({
	default: ({
		chartData,
		filterToggle,
	}: {
		chartData: RunChartData;
		filterToggle?: React.ReactNode;
	}) => (
		<div data-testid="throughput-chart">
			<div data-testid="throughput-total">{chartData.total}</div>
			{filterToggle}
		</div>
	),
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => (key === "throughput" ? "Throughput" : key),
	}),
}));

const LAST_30_DAYS = windowOf(30);
const LAST_90_DAYS = windowOf(90);

const today = new Date();
const windows = {
	[LAST_30_DAYS]: { start: addDays(today, -30), end: today },
	[LAST_90_DAYS]: { start: addDays(today, -90), end: today },
};

const seriesTotalling = (total: number) =>
	new RunChartData({ 0: [], 1: [] }, 2, total, []);

let held: HeldMetricsService;
let queryClient: QueryClient;

const theTeam: MetricsOwnerRequest = {
	ownerType: "team",
	ownerId: 2,
	ownerUpdatedAt: 0,
};

function cardFor(
	window: string,
	rawTotal: number,
	statusFromThePage: WidgetStatus = "ready",
	owner: MetricsOwnerRequest = theTeam,
) {
	return (
		<QueryClientProvider client={queryClient}>
			<WidgetShell
				widgetKey="throughput"
				status={statusFromThePage}
				hasContentToDim
			>
				<ThroughputRunChartCard
					owner={owner}
					metricsService={held.service as IMetricsService<IWorkItem>}
					startDate={windows[window].start}
					endDate={windows[window].end}
					rawData={seriesTotalling(rawTotal)}
					title="Work Items Completed"
					isPremium
					hasForecastFilter
				/>
			</WidgetShell>
		</QueryClientProvider>
	);
}

const frame = () => screen.getByTestId("widget-shell-throughput");
const shownTotal = () => screen.getByTestId("throughput-total");

async function theReaderTurnsTheFilterOn() {
	await userEvent
		.setup()
		.click(screen.getByLabelText("Use filtered Throughput"));
}

async function theFilteredSeriesAnswers(window: string, total: number) {
	await act(async () =>
		held.answer(
			{ method: "getThroughput", filtered: true, window },
			seriesTotalling(total),
		),
	);
}

// The query library tells components about an answer on a later timer tick, which act() does not
// wait for. Telling them in the same tick lets each answered request show before the next step.
notifyManager.setScheduler(queueMicrotask);

beforeEach(() => {
	held = createHeldMetricsService("team");
	queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
});

describe("the Throughput run chart's filtered series follows the selected window", () => {
	it("with the filter off, a new window shows the dashboard's own series for it", () => {
		const { rerender } = render(cardFor(LAST_30_DAYS, 30));

		rerender(cardFor(LAST_90_DAYS, 90));

		expect(shownTotal()).toHaveTextContent(/^90$/);
		// The requests the card sends are all it shows of its own fetching.
		expect(held.calls).toHaveLength(0);
	});

	it("with the filter on, a new window fetches the filtered series for that window", async () => {
		const { rerender } = render(cardFor(LAST_30_DAYS, 30));
		await theReaderTurnsTheFilterOn();
		await theFilteredSeriesAnswers(LAST_30_DAYS, 3);

		rerender(cardFor(LAST_90_DAYS, 90));

		expect(
			held.pending({
				method: "getThroughput",
				filtered: true,
				window: LAST_90_DAYS,
			}),
		).toHaveLength(1);
		await theFilteredSeriesAnswers(LAST_90_DAYS, 9);
		expect(shownTotal()).toHaveTextContent(/^9$/);
	});

	it("with the filter on, the filtered series is asked again once the owner has updated", async () => {
		const { rerender } = render(cardFor(LAST_30_DAYS, 30));
		await theReaderTurnsTheFilterOn();
		await theFilteredSeriesAnswers(LAST_30_DAYS, 3);

		rerender(
			cardFor(LAST_30_DAYS, 30, "ready", {
				...theTeam,
				ownerUpdatedAt: theTeam.ownerUpdatedAt + 60_000,
			}),
		);

		expect(
			held.pending({ method: "getThroughput", filtered: true }),
		).toHaveLength(1);
		await theFilteredSeriesAnswers(LAST_30_DAYS, 4);
		expect(shownTotal()).toHaveTextContent(/^4$/);
	});

	it("with the filter on, a portfolio sharing the team's id is asked for its own filtered series", async () => {
		const { rerender } = render(cardFor(LAST_30_DAYS, 30));
		await theReaderTurnsTheFilterOn();
		await theFilteredSeriesAnswers(LAST_30_DAYS, 3);

		rerender(
			cardFor(LAST_30_DAYS, 30, "ready", {
				...theTeam,
				ownerType: "portfolio",
			}),
		);

		expect(
			held.pending({ method: "getThroughput", filtered: true }),
		).toHaveLength(1);
	});

	it("the filtered series of a window the reader has left never replaces the current one", async () => {
		const { rerender } = render(cardFor(LAST_30_DAYS, 30));
		await theReaderTurnsTheFilterOn();
		rerender(cardFor(LAST_90_DAYS, 90));

		await theFilteredSeriesAnswers(LAST_90_DAYS, 9);
		await theFilteredSeriesAnswers(LAST_30_DAYS, 3);

		expect(shownTotal()).toHaveTextContent(/^9$/);
	});
});

describe("the Throughput run chart's frame while its filtered series loads", () => {
	it("the filter switch does not respond while the dashboard has the chart loading", async () => {
		render(cardFor(LAST_30_DAYS, 30, "loading"));

		expect(screen.getByTestId("widget-shell-body-throughput")).toHaveStyle({
			pointerEvents: "none",
		});
		await expect(theReaderTurnsTheFilterOn()).rejects.toThrow(
			/pointer-events: none/,
		);
		expect(held.calls).toHaveLength(0);
	});

	it("turning the filter on keeps the chart loading until the filtered series arrives", async () => {
		render(cardFor(LAST_30_DAYS, 30));

		await theReaderTurnsTheFilterOn();
		expect(frame()).toHaveAttribute("data-widget-status", "loading");

		await theFilteredSeriesAnswers(LAST_30_DAYS, 3);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
		expect(shownTotal()).toHaveTextContent(/^3$/);
	});

	it("with the filter on, a new window keeps the chart loading until that window's filtered series arrives", async () => {
		const { rerender } = render(cardFor(LAST_30_DAYS, 30));
		await theReaderTurnsTheFilterOn();
		await theFilteredSeriesAnswers(LAST_30_DAYS, 3);

		rerender(cardFor(LAST_90_DAYS, 90));
		expect(frame()).toHaveAttribute("data-widget-status", "loading");

		await theFilteredSeriesAnswers(LAST_90_DAYS, 9);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
	});

	it("turning the filter on keeps the unfiltered series on screen, dimmed under a spinner, until the filtered series arrives", async () => {
		render(cardFor(LAST_30_DAYS, 30));

		await theReaderTurnsTheFilterOn();

		expect(shownTotal()).toBeVisible();
		expect(shownTotal()).toHaveTextContent(/^30$/);
		expect(screen.getByTestId("widget-shell-body-throughput")).toHaveStyle({
			opacity: "0.4",
		});
		expect(screen.getByRole("progressbar")).toBeInTheDocument();

		await theFilteredSeriesAnswers(LAST_30_DAYS, 3);
		expect(screen.getByTestId("widget-shell-body-throughput")).not.toHaveStyle({
			opacity: "0.4",
		});
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("a filtered series that cannot be loaded ends in the could-not-load message", async () => {
		render(cardFor(LAST_30_DAYS, 30));
		await theReaderTurnsTheFilterOn();

		await act(async () =>
			held.fail({ method: "getThroughput", filtered: true }),
		);

		expect(frame()).toHaveAttribute("data-widget-status", "error");
		expect(screen.getByText(COULD_NOT_LOAD_MESSAGE)).toBeInTheDocument();
	});
});

describe("the Throughput run chart's filtered request", () => {
	it("is asked for without retrying, and kept for no other window", async () => {
		queryClient = new QueryClient({
			defaultOptions: {
				queries: { staleTime: 300_000, gcTime: 1_800_000, retry: 2 },
			},
		});
		render(cardFor(LAST_30_DAYS, 30));

		await theReaderTurnsTheFilterOn();

		const queries = queryClient.getQueryCache().getAll();
		expect(queries.length).toBeGreaterThan(0);
		for (const query of queries) {
			expect(query.options.retry).toBe(false);
			expect(query.options.gcTime).toBe(0);
			for (const observer of query.observers) {
				expect(observer.options.staleTime).toBe(0);
				expect(observer.options.refetchOnWindowFocus).toBe(false);
			}
		}
	});
});
