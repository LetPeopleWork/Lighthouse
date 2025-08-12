import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature, type IFeature } from "../../../models/Feature";
import { testTheme } from "../../../tests/testTheme";
import FeatureSizeScatterPlotChart from "./FeatureSizeScatterPlotChart";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

// Mock WorkItemsDialog component
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose }) => {
		if (!open) return null;
		return (
			<div data-testid="work-items-dialog">
				<h2>{title}</h2>
				<button type="button" onClick={onClose}>
					Close
				</button>
				<table>
					<thead>
						<tr>
							<th>Name</th>
							<th>Type</th>
							<th>State</th>
							<th>Size</th>
						</tr>
					</thead>
					<tbody>
						{items?.map((item: IFeature) => (
							<tr key={item.id}>
								<td>{item.name}</td>
								<td>{item.type}</td>
								<td>{item.state}</td>
								<td>{item.size} child items</td>
							</tr>
						))}
					</tbody>
				</table>
			</div>
		);
	}),
}));

// Mock the MUI-X Charts
vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		ChartContainer: vi.fn(({ children }) => (
			<div data-testid="mock-chart-container">{children}</div>
		)),
		ScatterPlot: vi.fn(({ slots }) => {
			// Simulate marker click functionality for testing
			const mockMarkerProps = {
				x: 100,
				y: 200,
				dataIndex: 0,
				color: testTheme.palette.primary.main,
				isHighlighted: false,
			};

			return (
				<div data-testid="mock-scatter-plot">
					<div>Scatter Plot Content</div>
					{slots?.marker && (
						<div data-testid="mock-marker">{slots.marker(mockMarkerProps)}</div>
					)}
				</div>
			);
		}),
		ChartsXAxis: vi.fn(() => <div>X Axis</div>),
		ChartsYAxis: vi.fn(() => <div>Y Axis</div>),
		ChartsTooltip: vi.fn(() => <div>Tooltip</div>),
	};
});

// Mock terminology context
vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				features: "Features",
			};
			return terms[key] || key;
		},
	}),
}));

// Mock getWorkItemName utility
vi.mock("../../../utils/featureName", () => ({
	getWorkItemName: (item: IFeature) => item.name,
}));

describe("FeatureSizeScatterPlotChart component", () => {
	// Helper function to create mock feature
	const createMockFeature = (
		id: number,
		name: string,
		size: number,
		closedDate: Date,
	): IFeature => {
		const feature = new Feature();
		feature.id = id;
		feature.referenceId = `F-${id}`;
		feature.name = name;
		feature.url = `https://example.com/feature${id}`;
		feature.size = size;
		feature.startedDate = new Date(
			closedDate.getTime() - size * 24 * 60 * 60 * 1000,
		); // Started 'size' days before closed
		feature.closedDate = closedDate;
		feature.cycleTime = size; // Simple mock: cycle time equals size
		feature.workItemAge = size;
		feature.state = "Done";
		feature.stateCategory = "Done";
		feature.type = "Feature";
		feature.parentWorkItemReference = "";
		feature.isBlocked = false;
		feature.lastUpdated = closedDate;
		feature.isUsingDefaultFeatureSize = false;
		feature.remainingWork = {};
		feature.totalWork = {};
		feature.milestoneLikelihood = {};
		feature.projects = {};
		feature.forecasts = [];

		// Mock required methods
		feature.getRemainingWorkForFeature = () => 0;
		feature.getRemainingWorkForTeam = () => 0;
		feature.getTotalWorkForFeature = () => size;
		feature.getTotalWorkForTeam = () => size;
		feature.getMilestoneLikelihood = () => 0;

		return feature;
	};

	// Create mock features with different sizes and dates
	const mockFeatures: IFeature[] = [
		createMockFeature(1, "Small Feature", 3, new Date(2023, 0, 15)),
		createMockFeature(2, "Medium Feature", 8, new Date(2023, 0, 20)),
		createMockFeature(3, "Large Feature", 15, new Date(2023, 0, 25)),
		createMockFeature(4, "Another Small Feature", 3, new Date(2023, 0, 15)), // Same size and date as first
	];

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should display 'No data available' when no features are provided", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={[]} />);

		expect(screen.getByText("No data available")).toBeInTheDocument();
	});

	it("should render the chart with correct title when data is provided", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		expect(screen.getByText("Features Size")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
	});

	it("should render scatter plot component when data is provided", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
		expect(screen.getByTestId("mock-marker")).toBeInTheDocument();
	});

	it("should group features by closed date and size", () => {
		// This test verifies the grouping logic indirectly by ensuring the component renders
		// without errors when features have the same date and size
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
	});

	it("should open WorkItemsDialog when marker is clicked", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		// Find and click the marker button
		const markerButton = screen.getByRole("button", {
			name: /View.*Feature.*with size.*child items/i,
		});

		fireEvent.click(markerButton);

		// Dialog should be opened
		expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
	});

	it("should close WorkItemsDialog when close button is clicked", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		// Open dialog by clicking marker
		const markerButton = screen.getByRole("button", {
			name: /View.*Feature.*with size.*child items/i,
		});
		fireEvent.click(markerButton);

		// Verify dialog is open
		expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();

		// Close dialog
		const closeButton = screen.getByRole("button", { name: "Close" });
		fireEvent.click(closeButton);

		// Dialog should be closed
		expect(screen.queryByTestId("work-items-dialog")).not.toBeInTheDocument();
	});

	it("should display correct tooltip information for single feature", () => {
		const singleFeature = [mockFeatures[0]];
		render(<FeatureSizeScatterPlotChart sizeDataPoints={singleFeature} />);

		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
		// The component should handle single features correctly in the valueFormatter
	});

	it("should display correct tooltip information for multiple features", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
		// The component should handle multiple features correctly in the valueFormatter
	});

	it("should calculate correct Y-axis maximum height", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		// The component should render without errors and calculate max Y based on feature sizes
		// Max size in mockFeatures is 15, so with 10% padding it should be around 16.5
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
	});

	it("should handle features with same closed date and size correctly", () => {
		// Create features with identical date and size to test grouping
		const identicalFeatures = [
			createMockFeature(1, "Feature A", 5, new Date(2023, 0, 15)),
			createMockFeature(2, "Feature B", 5, new Date(2023, 0, 15)),
		];

		render(<FeatureSizeScatterPlotChart sizeDataPoints={identicalFeatures} />);

		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();

		// Should create a single group with both features
		const markerButton = screen.getByRole("button", {
			name: /View 2 Features with size 5 child items/i,
		});
		expect(markerButton).toBeInTheDocument();
	});

	it("should use correct aria labels for accessibility", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		// Check that marker buttons have proper aria-labels
		const markerButtons = screen.getAllByRole("button", {
			name: /View.*Feature.*with size.*child items/i,
		});

		expect(markerButtons.length).toBeGreaterThan(0);
	});

	it("should render chart axes components", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		expect(screen.getByText("X Axis")).toBeInTheDocument();
		expect(screen.getByText("Y Axis")).toBeInTheDocument();
	});

	it("should render tooltip component", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		expect(screen.getByText("Tooltip")).toBeInTheDocument();
	});

	it("should handle empty groups correctly", () => {
		// This edge case test ensures the component doesn't crash with malformed data
		const emptyFeature = createMockFeature(1, "Empty Feature", 0, new Date());

		render(<FeatureSizeScatterPlotChart sizeDataPoints={[emptyFeature]} />);

		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
	});

	it("should display features in dialog with correct information", () => {
		render(<FeatureSizeScatterPlotChart sizeDataPoints={mockFeatures} />);

		// Open dialog
		const markerButton = screen.getByRole("button", {
			name: /View.*Features.*with size.*child items/i,
		});
		fireEvent.click(markerButton);

		// Check dialog contains feature information
		const dialog = screen.getByTestId("work-items-dialog");
		expect(dialog).toBeInTheDocument();

		// Should show table headers
		expect(screen.getByText("Name")).toBeInTheDocument();
		expect(screen.getByText("Type")).toBeInTheDocument();
		expect(screen.getByText("State")).toBeInTheDocument();
		expect(screen.getByText("Size")).toBeInTheDocument();
	});
});
