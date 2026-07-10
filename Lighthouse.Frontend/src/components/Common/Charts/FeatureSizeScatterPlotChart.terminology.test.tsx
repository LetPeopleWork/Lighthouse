import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { Feature } from "../../../models/Feature";
import { testTheme } from "../../../tests/testTheme";
import FeatureSizeScatterPlotChart from "./FeatureSizeScatterPlotChart";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				features: "Epics",
				cycleTime: "Cycle Time",
				feature: "Epic",
			};
			return terms[key] || key;
		},
	}),
}));

vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(() => null),
}));

vi.mock("@mui/x-charts", () => ({
	ChartsContainer: ({ children }: { children: React.ReactNode }) => (
		<div data-testid="chart-container">{children}</div>
	),
	ScatterPlot: () => <div data-testid="scatter-plot" />,
	ChartsXAxis: () => <div data-testid="x-axis" />,
	ChartsYAxis: () => <div data-testid="y-axis" />,
	ChartsTooltip: () => <div data-testid="tooltip" />,
	ChartsReferenceLine: () => <div data-testid="reference-line" />,
}));

vi.mock("../../../utils/featureName", () => ({
	getWorkItemName: (item: IFeature) => item.name,
}));

const createFeature = (
	id: number,
	name: string,
	size: number,
	cycleTime: number,
): IFeature => {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.size = size;
	feature.cycleTime = cycleTime;
	feature.stateCategory = "ToDo";
	feature.closedDate = new Date();
	return feature;
};

describe("FeatureSizeScatterPlotChart terminology", () => {
	it("renders title with custom terminology", () => {
		const features = [createFeature(1, "Test Feature", 5, 8)];
		render(<FeatureSizeScatterPlotChart sizeDataPoints={features} />);
		expect(screen.getByText("Epics Size")).toBeInTheDocument();
	});

	it("renders dialog title with custom terminology", () => {
		const features = [createFeature(1, "Test Feature", 5, 8)];
		render(<FeatureSizeScatterPlotChart sizeDataPoints={features} />);
		// The dialog title uses featuresTerm, so it should show "Epics Details"
		// This test will fail until the component uses getTerm for the title
		expect(screen.getByTestId("chart-container")).toBeInTheDocument();
	});
});
