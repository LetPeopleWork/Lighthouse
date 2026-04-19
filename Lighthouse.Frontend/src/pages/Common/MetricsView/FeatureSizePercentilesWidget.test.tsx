import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IFeatureSizePercentilesInfo } from "../../../models/Metrics/InfoWidgetData";
import FeatureSizePercentilesWidget from "./FeatureSizePercentilesWidget";

describe("FeatureSizePercentilesWidget", () => {
	const defaultData: IFeatureSizePercentilesInfo = {
		percentiles: [
			{ percentile: 50, value: 5 },
			{ percentile: 70, value: 8 },
			{ percentile: 85, value: 12 },
			{ percentile: 95, value: 20 },
		],
		comparison: {
			direction: "up",
			metricLabel: "Feature Size Percentiles",
			detailRows: [
				{ label: "50th", currentValue: "5", previousValue: "3" },
				{ label: "70th", currentValue: "8", previousValue: "6" },
				{ label: "85th", currentValue: "12", previousValue: "10" },
				{ label: "95th", currentValue: "20", previousValue: "15" },
			],
		},
	};

	it("renders all percentile rows", () => {
		render(<FeatureSizePercentilesWidget data={defaultData} />);
		expect(screen.getByTestId("percentile-row-50")).toHaveTextContent("5");
		expect(screen.getByTestId("percentile-row-70")).toHaveTextContent("8");
		expect(screen.getByTestId("percentile-row-85")).toHaveTextContent("12");
		expect(screen.getByTestId("percentile-row-95")).toHaveTextContent("20");
	});

	it("renders percentile labels", () => {
		render(<FeatureSizePercentilesWidget data={defaultData} />);
		expect(screen.getByText("50th")).toBeInTheDocument();
		expect(screen.getByText("85th")).toBeInTheDocument();
	});

	it("renders title text", () => {
		render(<FeatureSizePercentilesWidget data={defaultData} />);
		expect(screen.getByText("Feature Size Percentiles")).toBeInTheDocument();
	});

	it("exposes comparison payload via getTrendPayload", () => {
		const { trendPayload } =
			FeatureSizePercentilesWidget.getTrendPayload(defaultData);
		expect(trendPayload.direction).toBe("up");
		expect(trendPayload.metricLabel).toBe("Feature Size Percentiles");
	});

	it("renders empty state when no percentiles", () => {
		const emptyData: IFeatureSizePercentilesInfo = {
			percentiles: [],
			comparison: {
				direction: "none",
				metricLabel: "Feature Size Percentiles",
			},
		};
		render(<FeatureSizePercentilesWidget data={emptyData} />);
		expect(screen.getByText("No data")).toBeInTheDocument();
	});
});
