import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { parseDeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";
import DeliveryBurnupChart from "./DeliveryBurnupChart";

// Epic #5585 US-05. The sibling spec mocks LineChart and asserts the props the chart hands MUI-X; this
// one renders the real chart, because the defect was about what actually reaches the SVG. DESIGN read
// the cause as paint order — this file records that it is not: MUI-X composes AreaPlot before LinePlot,
// so the estimated line is already on top and no z-order change could have helped it.
const point = (date: string, doneWork: number, estimatedItemCount: number) => ({
	date,
	totalWork: 100,
	doneWork,
	remainingWork: 100 - doneWork,
	estimatedItemCount,
	forecastHowMany: null,
	likelihoodPercentage: 70,
	whenDistribution: null,
	featureBreakdown: [],
});

// Estimated is below Done on every day — the state in which the line used to be unreadable.
const estimatedUnderDone = () =>
	parseDeliveryMetricsHistory({
		deliveryDate: "2026-06-10T00:00:00Z",
		firstSnapshotDate: "2026-06-01T00:00:00Z",
		points: [
			point("2026-06-01T00:00:00Z", 60, 8),
			point("2026-06-02T00:00:00Z", 70, 6),
			point("2026-06-03T00:00:00Z", 80, 4),
		],
	});

const pathsOf = (container: HTMLElement) => [
	...container.querySelectorAll("path"),
];

describe("DeliveryBurnupChart painting", () => {
	it("draws the estimated line, and draws it after the Done area (AC-5.1)", () => {
		const { container } = render(
			<DeliveryBurnupChart history={estimatedUnderDone()} />,
		);

		const paths = pathsOf(container);
		const doneArea = paths.findIndex(
			(node) =>
				node.getAttribute("class")?.includes("MuiLineChart-area") &&
				node.getAttribute("data-series") === "done",
		);
		const estimatedLine = paths.findIndex(
			(node) => node.getAttribute("data-series") === "estimated",
		);

		expect(doneArea).toBeGreaterThanOrEqual(0);
		expect(estimatedLine).toBeGreaterThan(doneArea);
	});

	it("thins the Done fill so the line inside it stays readable (AC-5.1)", () => {
		const { container } = render(
			<DeliveryBurnupChart history={estimatedUnderDone()} />,
		);

		const doneArea = pathsOf(container).find(
			(node) => node.getAttribute("data-series") === "done",
		);
		const fillOpacity = Number(
			getComputedStyle(doneArea as Element).getPropertyValue("fill-opacity"),
		);

		// A band, not a bound: anything up to half-opaque still swallows a 2px dashed line, and anything
		// under a sixth stops reading as a filled area at all (AC-5.2). Review 2026-08-02.
		expect(fillOpacity).toBeGreaterThanOrEqual(0.15);
		expect(fillOpacity).toBeLessThanOrEqual(0.5);
	});

	it("leaves the fill rule alone when the estimate runs above the Done curve", () => {
		// The complement of the defect: nothing overlaps, so the line was always readable here. The rule
		// still has to apply, or fixing one case would have broken the other.
		const estimatedAboveDone = parseDeliveryMetricsHistory({
			deliveryDate: "2026-06-10T00:00:00Z",
			firstSnapshotDate: "2026-06-01T00:00:00Z",
			points: [
				point("2026-06-01T00:00:00Z", 10, 80),
				point("2026-06-02T00:00:00Z", 20, 85),
			],
		});

		const { container } = render(
			<DeliveryBurnupChart history={estimatedAboveDone} />,
		);

		const paths = pathsOf(container);
		expect(
			paths.some((node) => node.getAttribute("data-series") === "estimated"),
		).toBe(true);
		const doneArea = paths.find(
			(node) => node.getAttribute("data-series") === "done",
		);
		expect(
			Number(
				getComputedStyle(doneArea as Element).getPropertyValue("fill-opacity"),
			),
		).toBeLessThanOrEqual(0.5);
	});

	it("keeps the estimated line dashed where it crosses the fill (AC-5.3)", () => {
		const { container } = render(
			<DeliveryBurnupChart history={estimatedUnderDone()} />,
		);

		const estimated = pathsOf(container).find(
			(node) => node.getAttribute("data-series") === "estimated",
		);

		expect(getComputedStyle(estimated as Element).strokeDasharray).toBe("2 4");
	});

	it("still fills the Done curve as an area (AC-5.2)", () => {
		const { container } = render(
			<DeliveryBurnupChart history={estimatedUnderDone()} />,
		);

		const doneArea = pathsOf(container).find(
			(node) =>
				node.getAttribute("class")?.includes("MuiLineChart-area") &&
				node.getAttribute("data-series") === "done",
		);

		expect(doneArea).toBeDefined();
		expect(doneArea?.getAttribute("fill")).not.toBe("none");
	});
});
