import {
	notifyManager,
	QueryClient,
	QueryClientProvider,
} from "@tanstack/react-query";
import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { addDays } from "date-fns";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	ForecastPredictabilityScore,
	type IForecastPredictabilityScore,
} from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import {
	createHeldMetricsService,
	type HeldMetricsService,
	windowOf,
} from "../../../tests/HeldMetricsService";
import PredictabilityScoreDetailsWidget from "./PredictabilityScoreDetailsWidget";
import WidgetShell, { COULD_NOT_LOAD_MESSAGE } from "./WidgetShell";
import type { WidgetStatus } from "./widgetStatus";

vi.mock("../../../components/Common/Charts/PredictabilityScore", () => ({
	default: ({ data }: { data: IForecastPredictabilityScore }) => (
		<div data-testid="score-shown">{data.predictabilityScore}</div>
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

const scoreOf = (score: number) =>
	new ForecastPredictabilityScore([], score, new Map());

let held: HeldMetricsService;
let queryClient: QueryClient;

function detailsFor(
	window: string,
	pageScore: number,
	statusFromThePage: WidgetStatus = "ready",
) {
	return (
		<QueryClientProvider client={queryClient}>
			<WidgetShell
				widgetKey="predictabilityScoreDetails"
				status={statusFromThePage}
				hasContentToDim
			>
				<PredictabilityScoreDetailsWidget
					predictabilityData={scoreOf(pageScore)}
					owner={{ ownerId: 2, ownerUpdatedAt: 0 }}
					metricsService={held.service as IMetricsService<IWorkItem>}
					startDate={windows[window].start}
					endDate={windows[window].end}
					isPremium
					hasForecastFilter
				/>
			</WidgetShell>
		</QueryClientProvider>
	);
}

const frame = () =>
	screen.getByTestId("widget-shell-predictabilityScoreDetails");
const shownScore = () => screen.getByTestId("score-shown");

async function theReaderTurnsTheFilterOn() {
	await userEvent
		.setup()
		.click(screen.getByLabelText("Use filtered Throughput"));
}

async function theFilteredScoreAnswers(window: string, score: number) {
	await act(async () =>
		held.answer(
			{
				method: "getMultiItemForecastPredictabilityScore",
				filtered: true,
				window,
			},
			scoreOf(score),
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

describe("the Predictability Score details' filtered score follows the selected window", () => {
	it("with the filter off, a new window shows the dashboard's own score for it", () => {
		const { rerender } = render(detailsFor(LAST_30_DAYS, 0.3));

		rerender(detailsFor(LAST_90_DAYS, 0.9));

		expect(shownScore()).toHaveTextContent(/^0\.9$/);
		// The requests the widget sends are all it shows of its own fetching.
		expect(held.calls).toHaveLength(0);
	});

	it("with the filter on, a new window fetches the filtered score for that window", async () => {
		const { rerender } = render(detailsFor(LAST_30_DAYS, 0.3));
		await theReaderTurnsTheFilterOn();
		await theFilteredScoreAnswers(LAST_30_DAYS, 0.31);

		rerender(detailsFor(LAST_90_DAYS, 0.9));

		expect(
			held.pending({
				method: "getMultiItemForecastPredictabilityScore",
				filtered: true,
				window: LAST_90_DAYS,
			}),
		).toHaveLength(1);
		await theFilteredScoreAnswers(LAST_90_DAYS, 0.91);
		expect(shownScore()).toHaveTextContent(/^0\.91$/);
	});

	it("the filtered score of a window the reader has left never replaces the current one", async () => {
		const { rerender } = render(detailsFor(LAST_30_DAYS, 0.3));
		await theReaderTurnsTheFilterOn();
		rerender(detailsFor(LAST_90_DAYS, 0.9));

		await theFilteredScoreAnswers(LAST_90_DAYS, 0.91);
		await theFilteredScoreAnswers(LAST_30_DAYS, 0.31);

		expect(shownScore()).toHaveTextContent(/^0\.91$/);
	});
});

describe("the Predictability Score details' frame while its filtered score loads", () => {
	it("the filter switch does not respond while the dashboard has the score loading", async () => {
		render(detailsFor(LAST_30_DAYS, 0.3, "loading"));

		expect(
			screen.getByTestId("widget-shell-body-predictabilityScoreDetails"),
		).toHaveStyle({ pointerEvents: "none" });
		await expect(theReaderTurnsTheFilterOn()).rejects.toThrow(
			/pointer-events: none/,
		);
		expect(held.calls).toHaveLength(0);
	});

	it("turning the filter on keeps the score loading until the filtered score arrives", async () => {
		render(detailsFor(LAST_30_DAYS, 0.3));

		await theReaderTurnsTheFilterOn();
		expect(frame()).toHaveAttribute("data-widget-status", "loading");

		await theFilteredScoreAnswers(LAST_30_DAYS, 0.31);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
		expect(shownScore()).toHaveTextContent(/^0\.31$/);
	});

	it("with the filter on, a new window keeps the score loading until that window's filtered score arrives", async () => {
		const { rerender } = render(detailsFor(LAST_30_DAYS, 0.3));
		await theReaderTurnsTheFilterOn();
		await theFilteredScoreAnswers(LAST_30_DAYS, 0.31);

		rerender(detailsFor(LAST_90_DAYS, 0.9));
		expect(frame()).toHaveAttribute("data-widget-status", "loading");

		await theFilteredScoreAnswers(LAST_90_DAYS, 0.91);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
	});

	it("turning the filter on keeps the unfiltered score on screen, dimmed under a spinner, until the filtered score arrives", async () => {
		render(detailsFor(LAST_30_DAYS, 0.3));

		await theReaderTurnsTheFilterOn();

		expect(shownScore()).toBeVisible();
		expect(shownScore()).toHaveTextContent(/^0\.3$/);
		expect(
			screen.getByTestId("widget-shell-body-predictabilityScoreDetails"),
		).toHaveStyle({
			opacity: "0.4",
		});
		expect(screen.getByRole("progressbar")).toBeInTheDocument();

		await theFilteredScoreAnswers(LAST_30_DAYS, 0.31);
		expect(
			screen.getByTestId("widget-shell-body-predictabilityScoreDetails"),
		).not.toHaveStyle({
			opacity: "0.4",
		});
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("a filtered score that cannot be loaded ends in the could-not-load message", async () => {
		render(detailsFor(LAST_30_DAYS, 0.3));
		await theReaderTurnsTheFilterOn();

		await act(async () =>
			held.fail({
				method: "getMultiItemForecastPredictabilityScore",
				filtered: true,
			}),
		);

		expect(frame()).toHaveAttribute("data-widget-status", "error");
		expect(screen.getByText(COULD_NOT_LOAD_MESSAGE)).toBeInTheDocument();
	});
});

describe("the Predictability Score details' filtered request", () => {
	it("is asked for without retrying, and kept for no other window", async () => {
		queryClient = new QueryClient({
			defaultOptions: {
				queries: { staleTime: 300_000, gcTime: 1_800_000, retry: 2 },
			},
		});
		render(detailsFor(LAST_30_DAYS, 0.3));

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
