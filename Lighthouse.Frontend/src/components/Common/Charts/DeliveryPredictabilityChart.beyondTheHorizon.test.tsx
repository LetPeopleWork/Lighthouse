import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { parseDeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";
import { testTheme } from "../../../tests/testTheme";

const lineChartMock = vi.hoisted(() =>
	vi.fn(({ children }) => (
		<svg data-testid="mock-line-chart">
			<title>Test</title>
			{children}
		</svg>
	)),
);

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("@mui/x-charts/LineChart", () => ({
	LineChart: lineChartMock,
}));

vi.mock("@mui/x-charts", () => ({
	ChartsReferenceLine: () => null,
}));

import DeliveryPredictabilityChart from "./DeliveryPredictabilityChart";

interface SeriesEntry {
	id?: string;
	data?: Array<number | null>;
	valueFormatter?: (
		value: number | null,
		context: { dataIndex: number },
	) => string;
}

const aDay = (date: string, whenDistribution: unknown[]) => ({
	date,
	totalWork: 500,
	doneWork: 20,
	remainingWork: 480,
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage: 10,
	whenDistribution,
});

/** On the second day only six in ten runs finished, so its 85% has no day. */
const aHistoryWithABeyondDay = () =>
	parseDeliveryMetricsHistory({
		deliveryDate: "2026-12-15T00:00:00Z",
		firstSnapshotDate: "2026-10-16T00:00:00Z",
		points: [
			aDay("2026-10-16T00:00:00Z", [
				{ probability: 50, expectedDate: "2027-01-10T00:00:00Z" },
				{ probability: 85, expectedDate: "2027-03-01T00:00:00Z" },
			]),
			aDay("2026-10-17T00:00:00Z", [
				{ probability: 50, expectedDate: "2027-01-12T00:00:00Z" },
				{
					probability: 85,
					expectedDate: "2031-10-17T00:00:00Z",
					beyondHorizon: true,
					horizonYears: 5,
				},
			]),
		],
	});

const theSeries = (id: string): SeriesEntry | undefined => {
	const lastCall =
		lineChartMock.mock.calls[lineChartMock.mock.calls.length - 1];
	const props = lastCall?.[0] as { series?: SeriesEntry[] } | undefined;
	return props?.series?.find((entry) => entry.id === id);
};

describe("the Delivery predictability chart on a day a percentile was beyond the horizon", () => {
	beforeEach(() => {
		lineChartMock.mockClear();
		render(<DeliveryPredictabilityChart history={aHistoryWithABeyondDay()} />);
		fireEvent.click(screen.getByRole("button", { name: /^when\?$/i }));
	});

	it(`has no point there, so the line breaks`, () => {
		expect(theSeries("when-85")?.data?.[1]).toBeNull();
	});

	it(`says "More than 5 years" for that day in the tooltip`, () => {
		const formatted = theSeries("when-85")?.valueFormatter?.(null, {
			dataIndex: 1,
		});

		expect(formatted).toBe("More than 5 years");
	});

	/** Already true; the guard that only the beyond percentile loses its point. */
	it("still draws the days and percentiles that had a date", () => {
		expect(theSeries("when-85")?.data?.[0]).toBe(
			new Date("2027-03-01T00:00:00Z").getTime(),
		);
		expect(theSeries("when-50")?.data?.[1]).toBe(
			new Date("2027-01-12T00:00:00Z").getTime(),
		);
	});
});
