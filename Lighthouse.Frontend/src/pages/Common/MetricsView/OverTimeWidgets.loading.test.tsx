import { act, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { addDays } from "date-fns";
import type { ReactElement } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import {
	createHeldMetricsService,
	type HeldMetricsService,
	type MetricsMethod,
	windowOf,
} from "../../../tests/HeldMetricsService";
import PbcOverTimeWidget from "./PbcOverTimeWidget";
import PercentilesOverTimeWidget from "./PercentilesOverTimeWidget";
import WidgetShell, { COULD_NOT_LOAD_MESSAGE } from "./WidgetShell";

vi.mock("@mui/x-charts", () => ({
	LineChart: ({ xAxis }: { xAxis?: { data?: string[] }[] }) => (
		<div data-testid="over-time-chart">
			{`${xAxis?.[0]?.data?.length ?? 0} days`}
		</div>
	),
}));

const EMPTY_COPY =
	"Nothing to show for the selected range. Days appear here as Lighthouse records them.";

const LAST_30_DAYS = windowOf(30);
const LAST_90_DAYS = windowOf(90);

const today = new Date();
const windows = {
	[LAST_30_DAYS]: { start: addDays(today, -30), end: today },
	[LAST_90_DAYS]: { start: addDays(today, -90), end: today },
};

type Service = IMetricsService<IWorkItem | IFeature>;

type OverTimeChart = {
	readonly name: string;
	readonly widgetKey: string;
	readonly method: MetricsMethod;
	readonly draw: (service: Service, start: Date, end: Date) => ReactElement;
	readonly seriesOf: (days: number) => unknown[];
};

const recordedDay = (index: number) =>
	`2026-07-${String(10 + index).padStart(2, "0")}`;

const overTimeCharts: readonly OverTimeChart[] = [
	{
		name: "PBC Over Time",
		widgetKey: "pbcOverTime",
		method: "getProcessBehaviorOverTime",
		draw: (service, start, end) => (
			<PbcOverTimeWidget
				ownerId={2}
				metricsService={service}
				startDate={start}
				endDate={end}
				ownerType="team"
			/>
		),
		seriesOf: (days) =>
			Array.from({ length: days }, (_, index) => ({
				recordedAt: recordedDay(index),
				unpl: 14,
				average: 8,
				lnpl: 2,
			})),
	},
	{
		name: "Percentiles Over Time",
		widgetKey: "percentilesOverTime",
		method: "getPercentilesOverTime",
		draw: (service, start, end) => (
			<PercentilesOverTimeWidget
				ownerId={2}
				metricsService={service}
				startDate={start}
				endDate={end}
			/>
		),
		seriesOf: (days) =>
			Array.from({ length: days }, (_, index) => ({
				recordedAt: recordedDay(index),
				metricType: "CycleTime",
				p50: 3,
				p70: 4,
				p85: 6,
				p95: 8,
			})),
	},
];

describe.each(overTimeCharts)("$name while its series loads", (chart) => {
	let held: HeldMetricsService;

	beforeEach(() => {
		held = createHeldMetricsService("team");
	});

	const framed = (window: string) => (
		<WidgetShell
			widgetKey={chart.widgetKey}
			title={chart.name}
			status="ready"
			hasContentToDim={false}
		>
			{chart.draw(
				held.service as Service,
				windows[window].start,
				windows[window].end,
			)}
		</WidgetShell>
	);

	const frame = () => screen.getByTestId(`widget-shell-${chart.widgetKey}`);

	const answer = (window: string, days: number) =>
		act(async () =>
			held.answer({ method: chart.method, window }, chart.seriesOf(days)),
		);

	it("an empty answer reads as nothing recorded yet, never as a spinner", async () => {
		render(framed(LAST_30_DAYS));

		await answer(LAST_30_DAYS, 0);

		expect(await screen.findByText(EMPTY_COPY)).toBeInTheDocument();
		expect(within(frame()).queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("shows a spinner in its frame while the series is on its way, never an empty area", () => {
		render(framed(LAST_30_DAYS));

		expect(frame()).toHaveAttribute("data-widget-status", "loading");
		expect(within(frame()).getByRole("progressbar")).toBeInTheDocument();
		expect(screen.queryByText(EMPTY_COPY)).not.toBeInTheDocument();
		expect(screen.queryByTestId("over-time-chart")).not.toBeInTheDocument();
	});

	it("goes back to its spinner when the reader picks another window", async () => {
		const { rerender } = render(framed(LAST_30_DAYS));
		await answer(LAST_30_DAYS, 3);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");

		rerender(framed(LAST_90_DAYS));

		expect(frame()).toHaveAttribute("data-widget-status", "loading");
		expect(within(frame()).getByRole("progressbar")).toBeInTheDocument();
		expect(screen.queryByText(EMPTY_COPY)).not.toBeInTheDocument();
		expectOutOfSight("over-time-chart");
	});

	it("never shows the series of a window the reader has already left", async () => {
		const { rerender } = render(framed(LAST_30_DAYS));
		rerender(framed(LAST_90_DAYS));

		await answer(LAST_90_DAYS, 1);
		await answer(LAST_30_DAYS, 3);

		expect(screen.getByTestId("over-time-chart")).toHaveTextContent(/^1 days$/);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
	});

	it("ends in the could-not-load message when its series cannot be loaded", async () => {
		render(framed(LAST_30_DAYS));

		await act(async () => held.fail({ method: chart.method }));

		expect(frame()).toHaveAttribute("data-widget-status", "error");
		expect(within(frame()).queryByRole("progressbar")).not.toBeInTheDocument();
		expect(screen.getByText(COULD_NOT_LOAD_MESSAGE)).toBeInTheDocument();
	});

	it("switching to another selection and back within one window asks for nothing new", async () => {
		render(framed(LAST_30_DAYS));
		await answer(LAST_30_DAYS, 3);
		const user = userEvent.setup();
		const firstSelection = within(frame()).getByRole("button", {
			pressed: true,
		});

		await user.click(
			within(frame()).getAllByRole("button", { pressed: false })[0],
		);
		await answer(LAST_30_DAYS, 2);
		const requestsSoFar = held.calls.length;
		await user.click(firstSelection);

		expect(held.calls).toHaveLength(requestsSoFar);
	});
});

// A chart that fetches for itself has to stay mounted to keep fetching, so "gone" may mean
// hidden rather than removed. Either satisfies the reader, who must not see it.
function expectOutOfSight(testId: string) {
	const element = screen.queryByTestId(testId);
	if (element !== null) {
		expect(element).not.toBeVisible();
	}
}
