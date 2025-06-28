import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import { generateWorkItemMapForRunChart } from "../../../tests/TestDataProvider";
import { testTheme } from "../../../tests/testTheme";
import LineRunChart from "./LineRunChart";

// Mock the WorkItemsDialog component
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose }) => {
		if (!open) return null;
		return (
			<dialog open={open} aria-label={title} data-testid="work-items-dialog">
				<h2>{title}</h2>
				<table>
					<thead>
						<tr>
							<th>Name</th>
							<th>Type</th>
							<th>State</th>
							<th>Cycle Time</th>
						</tr>
					</thead>
					<tbody>
						{items.map((item: IWorkItem) => (
							<tr key={item.id}>
								<td>{item.name}</td>
								<td>{item.type}</td>
								<td>{item.state}</td>
								<td>{item.cycleTime} days</td>
							</tr>
						))}
					</tbody>
				</table>
				<button type="button" onClick={onClose}>
					Close
				</button>
			</dialog>
		);
	}),
}));

// Mock Material UI
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

// Helper function for regular rendering
const renderWithTheme = (ui: React.ReactElement) => {
	return render(ui);
};

// Mock Material UI theme - no need to mock the theme since we'll use ThemeProvider
vi.mock("../../../utils/theme/colors", () => ({
	hexToRgba: vi.fn((color, opacity) => `rgba(${color}, ${opacity})`),
}));

// Mock MUI X Charts properly
vi.mock("@mui/x-charts", () => ({
	ChartsReferenceLine: vi.fn(({ y, label }) => (
		<div data-testid={`reference-line-${y}`}>{label}</div>
	)),
	LineChart: vi.fn(({ onLineClick, xAxis, series, children }) => (
		<div data-testid="mock-line-chart">
			{xAxis?.[0]?.data?.map((label: string, index: number) => (
				<button
					type="button"
					key={`line-point-${label}-${String(index)}`}
					data-testid={`line-point-${index}`}
					onClick={(e) => onLineClick?.(e, { dataIndex: index })}
				>
					Point {index} - {label}: {series?.[0]?.data?.[index]}
				</button>
			))}
			<div data-testid="chartProps">
				{JSON.stringify({ seriesLength: series?.length })}
			</div>
			{children}
		</div>
	)),
}));

// Also mock the specific import path for LineChart
vi.mock("@mui/x-charts/LineChart", () => ({
	LineChart: vi.fn(({ onLineClick, xAxis, series, children }) => (
		<div data-testid="mock-line-chart">
			{xAxis?.[0]?.data?.map((label: string, index: number) => (
				<button
					type="button"
					key={`line-point-${label}-${String(index)}`}
					data-testid={`line-point-${index}`}
					onClick={(e) => onLineClick?.(e, { dataIndex: index })}
				>
					Point {index} - {label}: {series?.[0]?.data?.[index]}
				</button>
			))}
			<div data-testid="chartProps">
				{JSON.stringify({ seriesLength: series?.length })}
			</div>
			{children}
		</div>
	)),
}));

// Function to generate mock work items
function generateMockWorkItems(count: number): IWorkItem[] {
	return Array.from({ length: count }, (_, i) => ({
		id: i,
		name: `Work Item ${i}`,
		referenceId: `WI-${i}`,
		url: `https://example.com/work-item/${i}`,
		state: "In Progress",
		stateCategory: "Doing",
		type: "Task",
		workItemAge: 5,
		startedDate: new Date(2025, 0, 1),
		closedDate: new Date(2025, 0, 10),
		cycleTime: 10,
		parentWorkItemReference: "",
	}));
}

describe("LineRunChart component", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should render LineChart when chartData.history > 0", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			60,
		);

		renderWithTheme(
			<LineRunChart chartData={mockChartData} startDate={new Date()} />,
		);

		expect(screen.getByTestId("mock-line-chart")).toBeInTheDocument();
	});

	it("should display the correct total value", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			60,
		);

		renderWithTheme(
			<LineRunChart
				chartData={mockChartData}
				startDate={new Date()}
				displayTotal={true}
			/>,
		);

		const totalText = screen.getByText("Total: 60 Items");
		expect(totalText).toBeInTheDocument();
	});

	it("should display 'No data available' when no percentiles are provided", () => {
		const mockChartData: RunChartData = new RunChartData([], 0, 0);

		renderWithTheme(
			<LineRunChart chartData={mockChartData} startDate={new Date()} />,
		);

		expect(screen.getByText("No data available")).toBeInTheDocument();
	});

	it("should render with custom title", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			60,
		);
		const customTitle = "Custom Line Chart";

		renderWithTheme(
			<LineRunChart
				chartData={mockChartData}
				startDate={new Date()}
				title={customTitle}
			/>,
		);

		const titleElement = screen.getByText(customTitle);
		expect(titleElement).toBeInTheDocument();
	});

	it("should open the dialog when clicking on a line point with items", async () => {
		const rawData = [5, 10, 15];

		// Create mock work items for the second day (index 1)
		const mockWorkItems = generateMockWorkItems(10);
		// Initialize all days with empty arrays, then set items for day 1
		const workItemsMap: { [key: number]: IWorkItem[] } = {
			0: [],
			1: mockWorkItems,
			2: [],
		};

		const mockChartData = new RunChartData(workItemsMap, rawData.length, 30);

		renderWithTheme(
			<LineRunChart chartData={mockChartData} startDate={new Date()} />,
		);

		// Simulate clicking on the second point (index 1)
		fireEvent.click(screen.getByTestId("line-point-1"));

		// Verify dialog is opened with the correct title and content
		expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
		expect(screen.getByText(/Items in Progress on/)).toBeInTheDocument();

		// Dialog should contain table headers
		expect(screen.getByText("Name")).toBeInTheDocument();
		expect(screen.getByText("Type")).toBeInTheDocument();
		expect(screen.getByText("State")).toBeInTheDocument();
		expect(screen.getByText("Cycle Time")).toBeInTheDocument();
	});

	it("should not open the dialog when clicking on a line point without items", async () => {
		const rawData = [5, 10, 15];

		// Create empty work items for all days
		const workItemsMap: { [key: number]: IWorkItem[] } = {
			0: [],
			1: [],
			2: [],
		};

		const mockChartData = new RunChartData(workItemsMap, rawData.length, 30);

		renderWithTheme(
			<LineRunChart chartData={mockChartData} startDate={new Date()} />,
		);

		// Simulate clicking on the second point (index 1)
		fireEvent.click(screen.getByTestId("line-point-1"));

		// Dialog should not be present
		expect(screen.queryByTestId("work-items-dialog")).not.toBeInTheDocument();
	});

	it("should close the dialog when clicking the close button", async () => {
		const rawData = [5, 10, 15];

		// Create mock work items for the second day (index 1)
		const mockWorkItems = generateMockWorkItems(10);
		const workItemsMap: { [key: number]: IWorkItem[] } = {
			0: [],
			1: mockWorkItems,
			2: [],
		};

		const mockChartData = new RunChartData(workItemsMap, rawData.length, 30);

		renderWithTheme(
			<LineRunChart chartData={mockChartData} startDate={new Date()} />,
		);

		// Simulate clicking on the second point (index 1)
		fireEvent.click(screen.getByTestId("line-point-1"));

		// Verify dialog is opened
		expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();

		// Find and click the close button
		const closeButton = screen.getByRole("button", { name: /close/i });
		fireEvent.click(closeButton);

		// Dialog should be closed
		expect(screen.queryByTestId("work-items-dialog")).not.toBeInTheDocument();
	});

	it("should not display WIP limit when not provided", () => {
		const rawData = [5, 10, 15];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			30,
		);

		renderWithTheme(
			<LineRunChart chartData={mockChartData} startDate={new Date()} />,
		);

		// WIP limit chip should not be in the document
		expect(screen.queryByText("System WIP Limit")).not.toBeInTheDocument();
	});

	it("should display WIP limit when provided", () => {
		const rawData = [5, 10, 15];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			30,
		);

		renderWithTheme(
			<LineRunChart
				chartData={mockChartData}
				startDate={new Date()}
				wipLimit={10}
			/>,
		);

		// WIP limit chip should be in the document
		expect(screen.getByText("System WIP Limit")).toBeInTheDocument();
	});

	it("should not display WIP limit when value is less than 1", () => {
		const rawData = [5, 10, 15];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			30,
		);

		renderWithTheme(
			<LineRunChart
				chartData={mockChartData}
				startDate={new Date()}
				wipLimit={0}
			/>,
		);

		// WIP limit chip should not be in the document
		expect(screen.queryByText("System WIP Limit")).not.toBeInTheDocument();
	});
});
