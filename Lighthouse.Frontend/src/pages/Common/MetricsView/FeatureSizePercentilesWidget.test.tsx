import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IFeatureSizePercentilesInfo } from "../../../models/Metrics/InfoWidgetData";
import FeatureSizePercentilesWidget from "./FeatureSizePercentilesWidget";

vi.mock("../../../services/TerminologyContext", () => ({
    useTerminology: () => ({
        getTerm: (key: string) => key === "WORK_ITEM" ? "work item" : "work items",
    }),
}));

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

	it("renders all percentile rows with formatted work item values", () => {
		render(<FeatureSizePercentilesWidget data={defaultData} />);
		expect(screen.getByTestId("percentile-row-50")).toHaveTextContent(
			"5 work items",
		);
		expect(screen.getByTestId("percentile-row-70")).toHaveTextContent(
			"8 work items",
		);
		expect(screen.getByTestId("percentile-row-85")).toHaveTextContent(
			"12 work items",
		);
		expect(screen.getByTestId("percentile-row-95")).toHaveTextContent(
			"20 work items",
		);
	});

	it("renders singular 'work item' for a value of 1", () => {
		const singularData: IFeatureSizePercentilesInfo = {
			...defaultData,
			percentiles: [{ percentile: 50, value: 1 }],
		};
		render(<FeatureSizePercentilesWidget data={singularData} />);
		expect(screen.getByTestId("percentile-row-50")).toHaveTextContent(
			"1 work item",
		);
	});

	it("renders percentile labels", () => {
		render(<FeatureSizePercentilesWidget data={defaultData} />);
		expect(screen.getByText("50th")).toBeInTheDocument();
		expect(screen.getByText("85th")).toBeInTheDocument();
	});

	it("renders percentiles sorted descending", () => {
		render(<FeatureSizePercentilesWidget data={defaultData} />);
		const rows = screen.getAllByRole("row");
		const labels = rows.map((r) => r.textContent);
		const ninetyFiveIdx = labels.findIndex((t) => t?.includes("95th"));
		const fiftyIdx = labels.findIndex((t) => t?.includes("50th"));
		expect(ninetyFiveIdx).toBeLessThan(fiftyIdx);
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
		expect(screen.getByText("No data available")).toBeInTheDocument();
	});
});
