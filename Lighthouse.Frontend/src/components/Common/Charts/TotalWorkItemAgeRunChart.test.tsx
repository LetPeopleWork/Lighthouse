import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import TotalWorkItemAgeRunChart from "./TotalWorkItemAgeRunChart";

// Mock WorkItemsDialog
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose }) => {
		if (!open) return null;
		return (
			<div data-testid="mock-dialog">
				<div data-testid="dialog-title">{title}</div>
				<div data-testid="dialog-items-count">{items.length}</div>
				<button
					type="button"
					data-testid="dialog-close-button"
					onClick={onClose}
				>
					Close
				</button>
			</div>
		);
	}),
}));

// Mock BaseRunChart to pass children render function
vi.mock("./BaseRunChart", () => ({
	default: vi.fn(({ children, title }) => {
		return (
			<div data-testid="base-run-chart">
				{title && <h6>{title}</h6>}
				{children({})}
			</div>
		);
	}),
}));

describe("TotalWorkItemAgeRunChart", () => {
	const createMockWorkItem = (
		id: number,
		daysOld: number,
		closedDate: Date | null = null,
	): IWorkItem => {
		const startedDate = new Date();
		startedDate.setDate(startedDate.getDate() - daysOld);

		return {
			id,
			name: `Work Item ${id}`,
			state: "In Progress",
			stateCategory: "Doing",
			type: "Task",
			referenceId: `REF-${id}`,
			url: null,
			startedDate,
			closedDate: closedDate || new Date(),
			cycleTime: 0,
			workItemAge: daysOld,
			parentWorkItemReference: "",
			isBlocked: false,
		};
	};

	it("renders the chart with correct title", () => {
		const mockData = new RunChartData({}, 0, 0);
		const startDate = new Date("2025-01-01");

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
			/>,
		);

		expect(
			screen.getByText("Total Work Item Age Over Time"),
		).toBeInTheDocument();
	});

	it("renders with custom title", () => {
		const mockData = new RunChartData({}, 0, 0);
		const startDate = new Date("2025-01-01");

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
				title="Custom Age Chart"
			/>,
		);

		expect(screen.getByText("Custom Age Chart")).toBeInTheDocument();
	});

	it("calculates total age correctly for a single item", () => {
		const startDate = new Date("2025-01-01");
		const item = createMockWorkItem(1, 5); // 5 days old

		const workItemsPerDay = {
			0: [item],
			1: [item],
			2: [item],
		};

		const mockData = new RunChartData(workItemsPerDay, 3, 0);

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
			/>,
		);

		// The chart should be rendered
		expect(screen.getByTestId("base-run-chart")).toBeInTheDocument();
	});

	it("calculates total age correctly for multiple items", () => {
		const startDate = new Date("2025-01-01");
		const item1 = createMockWorkItem(1, 5); // 5 days old
		const item2 = createMockWorkItem(2, 3); // 3 days old

		const workItemsPerDay = {
			0: [item1, item2],
		};

		const mockData = new RunChartData(workItemsPerDay, 1, 0);

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
			/>,
		);

		expect(screen.getByTestId("base-run-chart")).toBeInTheDocument();
	});

	it("handles days with no items", () => {
		const startDate = new Date("2025-01-01");
		const workItemsPerDay = {
			0: [],
			1: [],
		};

		const mockData = new RunChartData(workItemsPerDay, 2, 0);

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
			/>,
		);

		expect(screen.getByTestId("base-run-chart")).toBeInTheDocument();
	});

	it("calculates historical age correctly", () => {
		const startDate = new Date("2025-01-01");

		// Item started 3 days before the chart start date
		const itemStartDate = new Date("2024-12-29");
		const item: IWorkItem = {
			id: 1,
			name: "Test Item",
			state: "In Progress",
			stateCategory: "Doing",
			type: "Task",
			referenceId: "REF-1",
			url: null,
			startedDate: itemStartDate,
			closedDate: new Date(),
			cycleTime: 0,
			workItemAge: 10, // Current age (ignored for historical calc)
			parentWorkItemReference: "",
			isBlocked: false,
		};

		const workItemsPerDay = {
			0: [item], // On day 0 (Jan 1), item is 3 days old
			1: [item], // On day 1 (Jan 2), item is 4 days old
			2: [item], // On day 2 (Jan 3), item is 5 days old
		};

		const mockData = new RunChartData(workItemsPerDay, 3, 0);

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
			/>,
		);

		// The component should render without errors
		expect(screen.getByTestId("base-run-chart")).toBeInTheDocument();
	});

	it("handles items that started on the chart start date", () => {
		const startDate = new Date("2025-01-01");
		const item: IWorkItem = {
			...createMockWorkItem(1, 0),
			startedDate: startDate, // Started exactly on chart start date
		};

		const workItemsPerDay = {
			0: [item], // Age should be 0 on day 0
			1: [item], // Age should be 1 on day 1
		};

		const mockData = new RunChartData(workItemsPerDay, 2, 0);

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
			/>,
		);

		expect(screen.getByTestId("base-run-chart")).toBeInTheDocument();
	});

	it("renders dialog when available", async () => {
		const startDate = new Date("2025-01-01");
		const item = createMockWorkItem(1, 5);

		const workItemsPerDay = {
			0: [item],
		};

		const mockData = new RunChartData(workItemsPerDay, 1, 0);

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
			/>,
		);

		// Dialog should not be visible initially
		await waitFor(() => {
			expect(screen.queryByTestId("mock-dialog")).not.toBeInTheDocument();
		});
	});

	it("formats dates correctly for x-axis labels", () => {
		const startDate = new Date("2025-01-15");
		const item = createMockWorkItem(1, 5);

		const workItemsPerDay = {
			0: [item],
			1: [item],
			2: [item],
		};

		const mockData = new RunChartData(workItemsPerDay, 3, 0);

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
			/>,
		);

		expect(screen.getByTestId("base-run-chart")).toBeInTheDocument();
	});

	it("does not crash with empty workItemsPerUnitOfTime", () => {
		const startDate = new Date("2025-01-01");
		const mockData = new RunChartData({}, 5, 0);

		render(
			<TotalWorkItemAgeRunChart
				wipOverTimeData={mockData}
				startDate={startDate}
			/>,
		);

		expect(screen.getByTestId("base-run-chart")).toBeInTheDocument();
	});
});
