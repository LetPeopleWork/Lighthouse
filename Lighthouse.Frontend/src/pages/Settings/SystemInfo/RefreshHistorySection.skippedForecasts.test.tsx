import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RefreshLog } from "../../../models/SystemInfo/RefreshLog";

// What the page hands the charts, kept so a scenario can read which runs the duration figures are drawn from.
const chartsWereHanded = vi.hoisted(() => ({
	durationLine: [] as Record<string, unknown>[][],
	perEntityBars: [] as Record<string, unknown>[][],
}));

vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		BarChart: (props: { dataset?: Record<string, unknown>[] }) => {
			chartsWereHanded.perEntityBars.push(props.dataset ?? []);
			return <div data-testid="mock-bar-chart" />;
		},
		ChartsContainer: (props: { dataset?: Record<string, unknown>[] }) => {
			chartsWereHanded.durationLine.push(props.dataset ?? []);
			return <div data-testid="mock-duration-chart" />;
		},
	};
});

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({ getTerm: (key: string) => key }),
}));

import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ISystemInfoService } from "../../../services/Api/SystemInfoService";
import {
	createMockApiServiceContext,
	createMockSystemInfoService,
} from "../../../tests/MockApiServiceProvider";
import RefreshHistorySection from "./RefreshHistorySection";

const getRefreshLogs = vi.fn();
const systemInfoService: ISystemInfoService = createMockSystemInfoService();
systemInfoService.getRefreshLogs = getRefreshLogs;

const renderRefreshHistory = () =>
	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ systemInfoService })}
		>
			<RefreshHistorySection />
		</ApiServiceContext.Provider>,
	);

let nextId = 1;

const aForecastOfPaymentsPlatform = (
	executedAt: string,
	durationMs: number,
	skipped?: boolean,
): RefreshLog => ({
	id: nextId++,
	type: "Forecast",
	entityId: 7,
	entityName: "Payments Platform",
	itemCount: 412,
	durationMs,
	executedAt,
	success: true,
	cancelled: false,
	...(skipped === undefined ? {} : { skipped }),
});

const aRefreshOfFss = (executedAt: string, durationMs: number): RefreshLog => ({
	id: nextId++,
	type: "Team",
	entityId: 3,
	entityName: "FSS",
	itemCount: 71,
	durationMs,
	executedAt,
	success: true,
	cancelled: false,
	skipped: false,
});

const theLastRunAt = "2026-10-23T07:15:02Z";

// Three forecasts that computed (240.10 s, 268.20 s, 305.90 s) and five that were skipped after them, the
// last one at 07:15:02. Two refreshes of FSS sit beside them in the All view.
const aQuietMorning = (): RefreshLog[] => [
	aForecastOfPaymentsPlatform("2026-10-21T06:00:00Z", 240100, false),
	aForecastOfPaymentsPlatform("2026-10-22T06:00:00Z", 268200, false),
	aForecastOfPaymentsPlatform("2026-10-23T06:00:00Z", 305900, false),
	aForecastOfPaymentsPlatform("2026-10-23T06:15:01Z", 900, true),
	aForecastOfPaymentsPlatform("2026-10-23T06:30:02Z", 1100, true),
	aForecastOfPaymentsPlatform("2026-10-23T06:45:01Z", 1200, true),
	aForecastOfPaymentsPlatform("2026-10-23T07:00:03Z", 800, true),
	aForecastOfPaymentsPlatform(theLastRunAt, 1500, true),
	aRefreshOfFss("2026-10-23T06:14:40Z", 16448),
	aRefreshOfFss("2026-10-23T07:14:41Z", 17552),
];

const selectPaymentsPlatform = async () => {
	await userEvent.click(await screen.findByRole("combobox"));
	await userEvent.click(
		await screen.findByRole("option", {
			name: /^Forecast: Payments Platform$/,
		}),
	);
	await screen.findByText(/^Total Runs$/);
};

const theFigure = (caption: RegExp) => {
	const figure = screen.getByText(caption).parentElement as HTMLElement;
	return within(figure).getByRole("heading", { level: 6 });
};

const theLatestDurationLine = (): Record<string, unknown>[] =>
	chartsWereHanded.durationLine[chartsWereHanded.durationLine.length - 1] ?? [];

const theLatestBars = (): Record<string, unknown>[] =>
	chartsWereHanded.perEntityBars[chartsWereHanded.perEntityBars.length - 1] ??
	[];

beforeEach(() => {
	nextId = 1;
	chartsWereHanded.durationLine.length = 0;
	chartsWereHanded.perEntityBars.length = 0;
});

describe("Refresh History tells a skipped forecast from a computed one", () => {
	it("averages, and finds the shortest and longest forecast, over the forecasts that computed", async () => {
		getRefreshLogs.mockResolvedValue(aQuietMorning());
		renderRefreshHistory();

		await selectPaymentsPlatform();

		expect(theFigure(/^Avg Duration$/)).toHaveTextContent(/^271\.40 s$/);
		expect(theFigure(/^Min Duration$/)).toHaveTextContent(/^240\.10 s$/);
		expect(theFigure(/^Max Duration$/)).toHaveTextContent(/^305\.90 s$/);
	});

	it("says how many of the runs were skipped because nothing changed", async () => {
		getRefreshLogs.mockResolvedValue(aQuietMorning());
		renderRefreshHistory();

		await selectPaymentsPlatform();

		expect(
			screen.getByText(/^Skipped \(nothing changed\): 5 of the last 8 runs$/),
		).toBeInTheDocument();
	});

	it("says the last run was skipped because nothing changed, and when it ran", async () => {
		getRefreshLogs.mockResolvedValue(aQuietMorning());
		renderRefreshHistory();

		await selectPaymentsPlatform();

		expect(theFigure(/^Last Run$/).textContent).toBe(
			`${new Date(theLastRunAt).toLocaleString()} · skipped (nothing changed)`,
		);
	});

	it("shows a dash for the durations when every run of the forecast was skipped", async () => {
		getRefreshLogs.mockResolvedValue([
			aForecastOfPaymentsPlatform("2026-10-23T06:15:01Z", 900, true),
			aForecastOfPaymentsPlatform("2026-10-23T06:30:02Z", 1100, true),
		]);
		renderRefreshHistory();

		await selectPaymentsPlatform();

		expect(theFigure(/^Avg Duration$/)).toHaveTextContent(/^—$/);
		expect(theFigure(/^Min Duration$/)).toHaveTextContent(/^—$/);
		expect(theFigure(/^Max Duration$/)).toHaveTextContent(/^—$/);
	});

	it("leaves a gap in the duration line at each skipped forecast and still draws its items", async () => {
		getRefreshLogs.mockResolvedValue(aQuietMorning());
		renderRefreshHistory();

		await selectPaymentsPlatform();

		const line = theLatestDurationLine();
		expect(line.map((point) => point.durationS)).toEqual([
			240.1,
			268.2,
			305.9,
			null,
			null,
			null,
			null,
			null,
		]);
		expect(line.map((point) => point.itemCount)).toEqual(
			Array.from({ length: 8 }, () => 412),
		);
	});

	it("averages and finds the longest run of the whole instance over the runs that computed", async () => {
		getRefreshLogs.mockResolvedValue(aQuietMorning());
		renderRefreshHistory();

		await screen.findByText(/^Total Items Fetched$/);

		// Computed: 240.10 + 268.20 + 305.90 s of forecasts and 16.448 + 17.552 s of FSS refreshes = 848.20 s, over five runs.
		expect(theFigure(/^Avg Duration$/)).toHaveTextContent(/^169\.64 s$/);
		expect(theFigure(/^Max Duration$/)).toHaveTextContent(/^305\.90 s$/);
	});

	it("draws each entity's average duration over the runs that computed", async () => {
		getRefreshLogs.mockResolvedValue(aQuietMorning());
		renderRefreshHistory();

		await screen.findByText(/^Total Items Fetched$/);

		const forecastBar = theLatestBars().find(
			(bar) => bar.label === "Forecast: Payments Platform",
		);
		expect(forecastBar?.avgDuration).toBe(271.4);
	});

	it("draws no duration bar for an entity whose every run was skipped, rather than a bar of zero", async () => {
		getRefreshLogs.mockResolvedValue([
			aForecastOfPaymentsPlatform("2026-10-23T06:15:01Z", 900, true),
			aForecastOfPaymentsPlatform("2026-10-23T06:30:02Z", 1100, true),
			aRefreshOfFss("2026-10-23T06:14:40Z", 16448),
		]);
		renderRefreshHistory();

		await screen.findByText(/^Total Items Fetched$/);

		const forecastBar = theLatestBars().find(
			(bar) => bar.label === "Forecast: Payments Platform",
		);
		expect(forecastBar?.avgDuration).toBeNull();
		expect(forecastBar?.avgItems).toBe(412);
	});
});

describe("Story 6354: Refresh History reads as before wherever nothing was skipped", () => {
	it("says nothing about skipped runs when every forecast computed", async () => {
		getRefreshLogs.mockResolvedValue(
			aQuietMorning().map((log) => ({ ...log, skipped: false })),
		);
		renderRefreshHistory();

		await selectPaymentsPlatform();

		expect(screen.queryByText(/^Skipped/)).not.toBeInTheDocument();
		expect(theFigure(/^Last Run$/)).not.toHaveTextContent(/skipped/);
		expect(theFigure(/^Last Run$/).textContent).toBe(
			new Date(theLastRunAt).toLocaleString(),
		);
	});

	it("counts a skipped forecast as a run that succeeded", async () => {
		getRefreshLogs.mockResolvedValue(aQuietMorning());
		renderRefreshHistory();

		await selectPaymentsPlatform();

		expect(theFigure(/^Total Runs$/)).toHaveTextContent(/^8$/);
		expect(theFigure(/^Success Rate$/)).toHaveTextContent(/^100%$/);
	});

	it("reads the forecasts of a server too old to say whether they were skipped as computed ones", async () => {
		getRefreshLogs.mockResolvedValue([
			aForecastOfPaymentsPlatform("2026-10-21T06:00:00Z", 240100),
			aForecastOfPaymentsPlatform("2026-10-22T06:00:00Z", 2000),
		]);
		renderRefreshHistory();

		await selectPaymentsPlatform();

		expect(theFigure(/^Avg Duration$/)).toHaveTextContent(/^121\.05 s$/);
		expect(screen.queryByText(/^Skipped/)).not.toBeInTheDocument();
		await waitFor(() =>
			expect(theLatestDurationLine().map((point) => point.durationS)).toEqual([
				240.1, 2,
			]),
		);
	});
});
