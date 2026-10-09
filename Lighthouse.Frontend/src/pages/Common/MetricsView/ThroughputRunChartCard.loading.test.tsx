import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { addDays } from "date-fns";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import {
	createHeldMetricsService,
	type HeldMetricsService,
	windowOf,
} from "../../../tests/HeldMetricsService";
import ThroughputRunChartCard from "./ThroughputRunChartCard";
import WidgetShell from "./WidgetShell";

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

function cardFor(window: string, rawTotal: number) {
	return (
		<QueryClientProvider client={queryClient}>
			<WidgetShell widgetKey="throughput" status="ready" hasContentToDim>
				<ThroughputRunChartCard
					entityId={2}
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

describe("the Throughput run chart's filtered series and the selected window", () => {
	beforeEach(() => {
		held = createHeldMetricsService("team");
		queryClient = new QueryClient({
			defaultOptions: { queries: { retry: false } },
		});
	});

	it("with the filter off, a new window shows the dashboard's own series for it", () => {
		const { rerender } = render(cardFor(LAST_30_DAYS, 30));

		rerender(cardFor(LAST_90_DAYS, 90));

		expect(shownTotal()).toHaveTextContent("90");
		expect(held.calls).toHaveLength(0);
	});

	it.skip("turning the filter on keeps the chart loading until the filtered series arrives", async () => {
		render(cardFor(LAST_30_DAYS, 30));

		await theReaderTurnsTheFilterOn();
		expect(frame()).toHaveAttribute("data-widget-status", "loading");

		await act(async () =>
			held.answer(
				{ method: "getThroughput", filtered: true },
				seriesTotalling(3),
			),
		);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
		expect(shownTotal()).toHaveTextContent("3");
	});

	it.skip("with the filter on, a new window fetches the filtered series for that window", async () => {
		const { rerender } = render(cardFor(LAST_30_DAYS, 30));
		await theReaderTurnsTheFilterOn();
		await act(async () =>
			held.answer(
				{ method: "getThroughput", filtered: true },
				seriesTotalling(3),
			),
		);

		rerender(cardFor(LAST_90_DAYS, 90));

		expect(frame()).toHaveAttribute("data-widget-status", "loading");
		expect(
			held.pending({
				method: "getThroughput",
				filtered: true,
				window: LAST_90_DAYS,
			}),
		).toHaveLength(1);

		await act(async () =>
			held.answer(
				{ method: "getThroughput", filtered: true, window: LAST_90_DAYS },
				seriesTotalling(9),
			),
		);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
		expect(shownTotal()).toHaveTextContent("9");
	});

	it.skip("the filtered series of a window the reader has left never replaces the current one", async () => {
		const { rerender } = render(cardFor(LAST_30_DAYS, 30));
		await theReaderTurnsTheFilterOn();
		rerender(cardFor(LAST_90_DAYS, 90));

		await act(async () =>
			held.answer(
				{ method: "getThroughput", filtered: true, window: LAST_90_DAYS },
				seriesTotalling(9),
			),
		);
		await act(async () =>
			held.answer(
				{ method: "getThroughput", filtered: true, window: LAST_30_DAYS },
				seriesTotalling(3),
			),
		);

		expect(shownTotal()).toHaveTextContent("9");
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
	});

	it.skip("a filtered series that cannot be loaded ends in the could-not-load message", async () => {
		render(cardFor(LAST_30_DAYS, 30));
		await theReaderTurnsTheFilterOn();

		await act(async () =>
			held.fail({ method: "getThroughput", filtered: true }),
		);

		expect(frame()).toHaveAttribute("data-widget-status", "error");
		expect(
			screen.getByText(
				"This chart couldn't be loaded. Change the dates or reload to try again.",
			),
		).toBeInTheDocument();
	});
});
