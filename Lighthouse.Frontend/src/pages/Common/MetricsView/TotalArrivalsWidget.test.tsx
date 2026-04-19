import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IArrivalsInfo } from "../../../models/Metrics/InfoWidgetData";
import TotalArrivalsWidget from "./TotalArrivalsWidget";

describe("TotalArrivalsWidget", () => {
	const defaultData: IArrivalsInfo = {
		total: 38,
		dailyAverage: 3.8,
		comparison: {
			direction: "down",
			metricLabel: "Total Arrivals",
			currentLabel: "2026-04-01 – 2026-04-10",
			currentValue: "38",
			previousLabel: "2026-03-22 – 2026-03-31",
			previousValue: "45",
			percentageDelta: "-15.6%",
		},
	};

	it("renders total count", () => {
		render(<TotalArrivalsWidget data={defaultData} />);
		expect(screen.getByTestId("arrivals-info-total")).toHaveTextContent("38");
	});

	it("renders daily average with one decimal", () => {
		render(<TotalArrivalsWidget data={defaultData} />);
		expect(screen.getByTestId("arrivals-info-average")).toHaveTextContent(
			"3.8",
		);
	});

	it("renders title text", () => {
		render(<TotalArrivalsWidget data={defaultData} />);
		expect(screen.getByText("Total Arrivals")).toBeInTheDocument();
	});

	it("exposes comparison payload via getTrendPayload for parent wiring", () => {
		const { trendPayload } = TotalArrivalsWidget.getTrendPayload(defaultData);
		expect(trendPayload.direction).toBe("down");
		expect(trendPayload.metricLabel).toBe("Total Arrivals");
	});

	it("renders zero values correctly", () => {
		const zeroData: IArrivalsInfo = {
			total: 0,
			dailyAverage: 0,
			comparison: {
				direction: "none",
				metricLabel: "Total Arrivals",
			},
		};
		render(<TotalArrivalsWidget data={zeroData} />);
		expect(screen.getByTestId("arrivals-info-total")).toHaveTextContent("0");
		expect(screen.getByTestId("arrivals-info-average")).toHaveTextContent(
			"0.0",
		);
	});
});
