import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
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
					entityId={2}
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

	it.skip("with the filter on, a new window fetches the filtered score for that window", async () => {
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

	it.skip("the filtered score of a window the reader has left never replaces the current one", async () => {
		const { rerender } = render(detailsFor(LAST_30_DAYS, 0.3));
		await theReaderTurnsTheFilterOn();
		rerender(detailsFor(LAST_90_DAYS, 0.9));

		await theFilteredScoreAnswers(LAST_90_DAYS, 0.91);
		await theFilteredScoreAnswers(LAST_30_DAYS, 0.31);

		expect(shownScore()).toHaveTextContent(/^0\.91$/);
	});
});

describe("the Predictability Score details' frame while its filtered score loads", () => {
	it("the filter switch still works while the dashboard has the score loading", async () => {
		render(detailsFor(LAST_30_DAYS, 0.3, "loading"));

		await theReaderTurnsTheFilterOn();

		expect(
			held.pending({
				method: "getMultiItemForecastPredictabilityScore",
				filtered: true,
			}),
		).toHaveLength(1);
	});

	it.skip("turning the filter on keeps the score loading until the filtered score arrives", async () => {
		render(detailsFor(LAST_30_DAYS, 0.3));

		await theReaderTurnsTheFilterOn();
		expect(frame()).toHaveAttribute("data-widget-status", "loading");

		await theFilteredScoreAnswers(LAST_30_DAYS, 0.31);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
		expect(shownScore()).toHaveTextContent(/^0\.31$/);
	});

	it.skip("with the filter on, a new window keeps the score loading until that window's filtered score arrives", async () => {
		const { rerender } = render(detailsFor(LAST_30_DAYS, 0.3));
		await theReaderTurnsTheFilterOn();
		await theFilteredScoreAnswers(LAST_30_DAYS, 0.31);

		rerender(detailsFor(LAST_90_DAYS, 0.9));
		expect(frame()).toHaveAttribute("data-widget-status", "loading");

		await theFilteredScoreAnswers(LAST_90_DAYS, 0.91);
		expect(frame()).toHaveAttribute("data-widget-status", "ready");
	});

	it.skip("a filtered score that cannot be loaded ends in the could-not-load message", async () => {
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
