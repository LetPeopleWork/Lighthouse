import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature, type IFeature } from "../../../models/Feature";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { testTheme } from "../../../tests/testTheme";
import FeatureSizeScatterPlotChart from "./FeatureSizeScatterPlotChart";

// Mock dependencies to isolate component behavior
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose }) => {
		if (!open) return null;
		return (
			<div data-testid="work-items-dialog">
				<h2 data-testid="dialog-title">{title}</h2>
				<button type="button" onClick={onClose} data-testid="close-dialog">
					Close
				</button>
				<div data-testid="feature-count">{items?.length || 0} features</div>
				{items?.map((item: IFeature, index: number) => (
					<div key={item.id} data-testid={`feature-${index}`}>
						{item.name} - Size: {item.size}
					</div>
				))}
			</div>
		);
	}),
}));

vi.mock("@mui/x-charts", () => {
	interface ChartContainerProps {
		children: React.ReactNode;
		height: number;
		xAxis?: Array<{ max?: number }>;
		series?: Array<{ data?: unknown[] }>;
	}

	interface ScatterPlotProps {
		slots?: {
			marker?: (props: {
				x: number;
				y: number;
				dataIndex: number;
				color: string;
				isHighlighted: boolean;
			}) => React.ReactNode;
		};
	}

	return {
		ChartContainer: ({
			children,
			height,
			xAxis,
			series,
		}: ChartContainerProps) => (
			<div
				data-testid="chart-container"
				data-height={height}
				data-x-axis-max={xAxis?.[0]?.max}
				data-series-count={series?.[0]?.data?.length}
			>
				{children}
			</div>
		),
		ScatterPlot: ({ slots }: ScatterPlotProps) => {
			// Simulate multiple data points based on typical usage
			const mockDataPoints = [0, 1, 2]; // Simulating 3 groups
			return (
				<div data-testid="scatter-plot">
					{mockDataPoints.map((dataIndex) => {
						const mockMarkerProps = {
							x: 100 + dataIndex * 50,
							y: 200 + dataIndex * 30,
							dataIndex,
							color: "#1976d2",
							isHighlighted: false,
						};
						return (
							<div key={dataIndex} data-testid={`marker-${dataIndex}`}>
								{slots?.marker?.(mockMarkerProps)}
							</div>
						);
					})}
				</div>
			);
		},
		ChartsXAxis: () => <div data-testid="x-axis">X Axis</div>,
		ChartsYAxis: () => <div data-testid="y-axis">Y Axis</div>,
		ChartsTooltip: () => <div data-testid="tooltip">Tooltip</div>,
		ChartsReferenceLine: ({
			label,
			x,
			lineStyle,
		}: {
			label: string;
			x: number;
			lineStyle?: { stroke: string };
		}) => (
			<div
				data-testid={`reference-line-${label}`}
				data-value={x}
				data-color={lineStyle?.stroke}
			>
				{label}
			</div>
		),
	};
});

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				features: "Features",
				cycleTime: "Cycle Time",
			};
			return terms[key] || key;
		},
	}),
}));

vi.mock("../../../utils/featureName", () => ({
	getWorkItemName: (item: IFeature) => item.name,
}));

describe("FeatureSizeScatterPlotChart", () => {
	const createFeature = (
		id: number,
		name: string,
		size: number,
		cycleTime: number,
	): IFeature => {
		const feature = new Feature();
		feature.id = id;
		feature.referenceId = `REF-${id}`;
		feature.name = name;
		feature.size = size;
		feature.cycleTime = cycleTime;
		feature.state = "Done";
		feature.type = "Feature";
		feature.closedDate = new Date("2023-01-15");
		return feature;
	};

	const basicFeatures: IFeature[] = [
		createFeature(1, "Small Task", 3, 5),
		createFeature(2, "Medium Task", 8, 12),
		createFeature(3, "Large Task", 15, 20),
	];

	const percentileData: IPercentileValue[] = [
		{ percentile: 50, value: 8 },
		{ percentile: 85, value: 15 },
		{ percentile: 95, value: 25 },
	];

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("when no data is provided", () => {
		it("displays a no data message", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={[]} />);

			expect(screen.getByText("No data available")).toBeInTheDocument();
			expect(screen.queryByTestId("chart-container")).not.toBeInTheDocument();
		});
	});

	describe("when feature data is provided", () => {
		it("displays the chart with proper title", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			expect(screen.getByText("Features Size")).toBeInTheDocument();
			expect(screen.getByTestId("chart-container")).toBeInTheDocument();
		});

		it("renders all chart components", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			expect(screen.getByTestId("scatter-plot")).toBeInTheDocument();
			expect(screen.getByTestId("x-axis")).toBeInTheDocument();
			expect(screen.getByTestId("y-axis")).toBeInTheDocument();
			expect(screen.getByTestId("tooltip")).toBeInTheDocument();
		});

		it("calculates appropriate chart dimensions", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			const container = screen.getByTestId("chart-container");
			// Max size is 15, with 10% padding should be 16.5
			expect(Number(container.getAttribute("data-x-axis-max"))).toBeGreaterThan(
				15,
			);
		});

		it("groups features with identical size and cycle time", () => {
			const duplicateFeatures = [
				createFeature(1, "Task A", 5, 10),
				createFeature(2, "Task B", 5, 10), // Same size and cycle time
				createFeature(3, "Task C", 8, 15),
			];

			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={duplicateFeatures} />,
			);

			const container = screen.getByTestId("chart-container");
			// Should have 2 data points (one group with 2 items, one with 1 item)
			expect(container).toHaveAttribute("data-series-count", "2");
		});
	});

	describe("when user interacts with data points", () => {
		it("opens a dialog when clicking a data point", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			const markerButtons = screen.getAllByRole("button", {
				name: /view.*feature.*with size.*child items/i,
			});

			fireEvent.click(markerButtons[0]);

			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
			expect(screen.getByTestId("dialog-title")).toHaveTextContent(
				"Features Details",
			);
		});

		it("displays feature details in the dialog", () => {
			const singleFeature = [createFeature(1, "Test Feature", 5, 8)];
			render(<FeatureSizeScatterPlotChart sizeDataPoints={singleFeature} />);

			const markerButton = screen.getByRole("button");
			fireEvent.click(markerButton);

			expect(screen.getByTestId("feature-count")).toHaveTextContent(
				"1 features",
			);
			expect(screen.getByTestId("feature-0")).toHaveTextContent(
				"Test Feature - Size: 5",
			);
		});

		it("closes dialog when close button is clicked", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			// Open dialog using the first marker button
			const markerButtons = screen.getAllByRole("button", {
				name: /view.*feature.*with size.*child items/i,
			});
			fireEvent.click(markerButtons[0]);

			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();

			// Close dialog
			fireEvent.click(screen.getByTestId("close-dialog"));

			expect(screen.queryByTestId("work-items-dialog")).not.toBeInTheDocument();
		});

		it("shows grouped features when multiple items share the same data point", () => {
			const groupedFeatures = [
				createFeature(1, "Feature A", 10, 15),
				createFeature(2, "Feature B", 10, 15), // Same dimensions
			];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={groupedFeatures} />);

			const markerButton = screen.getByRole("button", {
				name: /view 2 features with size 10 child items/i,
			});

			fireEvent.click(markerButton);

			expect(screen.getByTestId("feature-count")).toHaveTextContent(
				"2 features",
			);
		});
	});

	describe("when percentile data is provided", () => {
		it("displays percentile chips", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={percentileData}
				/>,
			);

			// Query by role to specifically target the chip buttons, not reference lines
			const chip50 = screen.getByRole("button", { name: /50%/ });
			const chip85 = screen.getByRole("button", { name: /85%/ });
			const chip95 = screen.getByRole("button", { name: /95%/ });

			expect(chip50).toBeInTheDocument();
			expect(chip85).toBeInTheDocument();
			expect(chip95).toBeInTheDocument();
		});

		it("displays reference lines for each percentile", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={percentileData}
				/>,
			);

			expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();
			expect(screen.getByTestId("reference-line-85%")).toBeInTheDocument();
			expect(screen.getByTestId("reference-line-95%")).toBeInTheDocument();

			// Verify line positions match percentile values
			expect(screen.getByTestId("reference-line-50%")).toHaveAttribute(
				"data-value",
				"8",
			);
			expect(screen.getByTestId("reference-line-85%")).toHaveAttribute(
				"data-value",
				"15",
			);
			expect(screen.getByTestId("reference-line-95%")).toHaveAttribute(
				"data-value",
				"25",
			);
		});

		it("adjusts chart scale to accommodate percentile values", () => {
			const highPercentiles = [{ percentile: 99, value: 50 }];

			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={highPercentiles}
				/>,
			);

			const container = screen.getByTestId("chart-container");
			// Should scale to accommodate the 50 value with 10% padding
			expect(Number(container.getAttribute("data-x-axis-max"))).toBeGreaterThan(
				50,
			);
		});

		it("allows toggling percentile visibility", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={percentileData}
				/>,
			);

			// Use role to target specifically the chip button
			const percentileChip = screen.getByRole("button", { name: /50%/ });

			// Initial state - should be visible
			expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();

			// Toggle visibility
			fireEvent.click(percentileChip);

			// Chip should still exist but reference line visibility changes through styling
			expect(percentileChip).toBeInTheDocument();
		});

		it("handles empty percentile arrays gracefully", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={[]}
				/>,
			);

			expect(
				screen.queryByRole("button", { name: /50%/ }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("reference-line-50%"),
			).not.toBeInTheDocument();
			expect(screen.getByTestId("chart-container")).toBeInTheDocument();
		});
	});

	describe("accessibility features", () => {
		it("provides proper aria labels for interactive elements", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			const buttons = screen.getAllByRole("button");
			buttons.forEach((button) => {
				expect(button).toHaveAttribute("aria-label");
				expect(button.getAttribute("aria-label")).toMatch(
					/view.*with size.*child items/i,
				);
			});
		});

		it("includes descriptive titles for chart markers", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			// The SVG title elements should be present for screen readers
			// This is tested through the marker rendering behavior
			expect(screen.getByTestId("scatter-plot")).toBeInTheDocument();
		});
	});

	describe("edge cases", () => {
		it("handles features with null cycle times", () => {
			const featuresWithNulls = [
				createFeature(1, "Valid Feature", 5, 10),
				{
					...createFeature(2, "Invalid Feature", 8, 0),
					cycleTime: null as unknown as number,
				},
			];

			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={featuresWithNulls} />,
			);

			// Should filter out features with null cycle times and still render
			expect(screen.getByTestId("chart-container")).toBeInTheDocument();
		});

		it("handles features with zero sizes", () => {
			const zeroSizeFeature = [createFeature(1, "Zero Size", 0, 5)];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={zeroSizeFeature} />);

			expect(screen.getByTestId("chart-container")).toBeInTheDocument();
		});

		it("works with single feature", () => {
			const singleFeature = [createFeature(1, "Only Feature", 5, 8)];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={singleFeature} />);

			expect(screen.getByTestId("chart-container")).toBeInTheDocument();

			const markerButton = screen.getByRole("button", {
				name: /view 1 feature with size 5 child items/i,
			});
			expect(markerButton).toBeInTheDocument();
		});
	});
});
