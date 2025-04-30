import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import StartedVsFinishedDisplay from "./StartedVsFinishedDisplay";

describe("FlowInformationDisplay component", () => {
	const mockStartedItems = new RunChartData([3, 2, 5], 3, 10);
	const mockClosedItems = new RunChartData([2, 4, 1], 3, 7);

	it("should render with title and flow information", () => {
		render(
			<StartedVsFinishedDisplay
				startedItems={mockStartedItems}
				closedItems={mockClosedItems}
			/>,
		);

		expect(screen.getByText("Started vs. Closed Items")).toBeInTheDocument();
		expect(screen.getByText("Started:")).toBeInTheDocument();
		expect(screen.getByText("Closed:")).toBeInTheDocument();

		// Test for numbers with their context using exact element structure
		expect(screen.getByText("10")).toBeInTheDocument();
		expect(screen.getByText("7")).toBeInTheDocument();
		expect(screen.getByText("3.3")).toBeInTheDocument();
		expect(screen.getByText("2.3")).toBeInTheDocument();
	});

	it("should display zeros when no data is provided", () => {
		render(<StartedVsFinishedDisplay startedItems={null} closedItems={null} />);

		expect(screen.getByText("Started vs. Closed Items")).toBeInTheDocument();

		// Check for zeros in both rows
		const startedRow = screen.getByText("Started:").closest("tr");
		const closedRow = screen.getByText("Closed:").closest("tr");

		expect(startedRow).not.toBeNull();
		expect(closedRow).not.toBeNull();

		if (startedRow && closedRow) {
			expect(within(startedRow).getByText("0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0")).toBeInTheDocument();
			expect(within(startedRow).getByText("0.0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0.0")).toBeInTheDocument();
		}
	});

	it("should calculate averages correctly", () => {
		const startedItems = new RunChartData([5, 10, 15], 3, 30);
		const closedItems = new RunChartData([2, 3, 4, 5], 4, 14);

		render(
			<StartedVsFinishedDisplay
				startedItems={startedItems}
				closedItems={closedItems}
			/>,
		);

		// Check for calculated values
		expect(screen.getByText("30")).toBeInTheDocument();
		expect(screen.getByText("14")).toBeInTheDocument();
		expect(screen.getByText("10.0")).toBeInTheDocument();
		expect(screen.getByText("3.5")).toBeInTheDocument();
	});

	it("should handle empty arrays gracefully", () => {
		const emptyStartedItems = new RunChartData([], 0, 0);
		const emptyClosedItems = new RunChartData([], 0, 0);

		render(
			<StartedVsFinishedDisplay
				startedItems={emptyStartedItems}
				closedItems={emptyClosedItems}
			/>,
		);

		expect(screen.getByText("Started vs. Closed Items")).toBeInTheDocument();

		const startedRow = screen.getByText("Started:").closest("tr");
		const closedRow = screen.getByText("Closed:").closest("tr");

		expect(startedRow).not.toBeNull();
		expect(closedRow).not.toBeNull();

		if (startedRow && closedRow) {
			expect(within(startedRow).getByText("0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0")).toBeInTheDocument();
			expect(within(startedRow).getByText("0.0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0.0")).toBeInTheDocument();
		}
	});

	it("should format numbers with one decimal place", () => {
		const startedItems = new RunChartData([1, 2, 3], 3, 6);

		render(
			<StartedVsFinishedDisplay
				startedItems={startedItems}
				closedItems={null}
			/>,
		);

		expect(screen.getByText("6")).toBeInTheDocument();
		expect(screen.getByText("2.0")).toBeInTheDocument();
	});

	it("should handle mixed data cases", () => {
		const startedItems = new RunChartData([1, 2, 3], 3, 6);

		render(
			<StartedVsFinishedDisplay
				startedItems={startedItems}
				closedItems={null}
			/>,
		);

		const startedRow = screen.getByText("Started:").closest("tr");
		const closedRow = screen.getByText("Closed:").closest("tr");

		expect(startedRow).not.toBeNull();
		expect(closedRow).not.toBeNull();

		if (startedRow && closedRow) {
			expect(within(startedRow).getByText("6")).toBeInTheDocument();
			expect(within(closedRow).getByText("0")).toBeInTheDocument();
			expect(within(startedRow).getByText("2.0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0.0")).toBeInTheDocument();
		}
	});
});
