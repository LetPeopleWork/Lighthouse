import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IThroughputInfo } from "../../../models/Metrics/InfoWidgetData";
import TotalThroughputWidget from "./TotalThroughputWidget";

describe("TotalThroughputWidget", () => {
	const defaultData: IThroughputInfo = {
		total: 42,
		dailyAverage: 4.2,
		comparison: {
			direction: "up",
			metricLabel: "Total Throughput",
			currentLabel: "2026-04-01 – 2026-04-10",
			currentValue: "42",
			previousLabel: "2026-03-22 – 2026-03-31",
			previousValue: "35",
			percentageDelta: "+20.0%",
		},
	};

	it("renders total count", () => {
		render(<TotalThroughputWidget data={defaultData} />);
		expect(screen.getByTestId("throughput-info-total")).toHaveTextContent("42");
	});

	it("renders daily average with one decimal", () => {
		render(<TotalThroughputWidget data={defaultData} />);
		expect(screen.getByTestId("throughput-info-average")).toHaveTextContent(
			"4.2",
		);
	});

	it("renders title text", () => {
		render(<TotalThroughputWidget data={defaultData} />);
		expect(screen.getByText("Total Throughput")).toBeInTheDocument();
	});

	it("exposes comparison payload via trendPayload prop for parent wiring", () => {
		const { trendPayload } = TotalThroughputWidget.getTrendPayload(defaultData);
		expect(trendPayload.direction).toBe("up");
		expect(trendPayload.metricLabel).toBe("Total Throughput");
	});

	it("renders zero values correctly", () => {
		const zeroData: IThroughputInfo = {
			total: 0,
			dailyAverage: 0,
			comparison: {
				direction: "none",
				metricLabel: "Total Throughput",
			},
		};
		render(<TotalThroughputWidget data={zeroData} />);
		expect(screen.getByTestId("throughput-info-total")).toHaveTextContent("0");
		expect(screen.getByTestId("throughput-info-average")).toHaveTextContent(
			"0.0",
		);
	});
});
