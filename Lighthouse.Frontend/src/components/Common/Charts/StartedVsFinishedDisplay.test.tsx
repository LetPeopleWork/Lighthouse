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

	describe("WIP status indicator", () => {
		it("should show 'confident' indicator when absolute difference is less than 1.0 even if percentage difference is high", () => {
			// 10% difference but absolute average difference is only 0.4 items
			const startedItems = new RunChartData([2, 2, 2], 3, 6);
			const closedItems = new RunChartData([1, 2, 2], 3, 5);
			// Averages: 2.0 vs 1.6 = 0.4 difference

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are keeping a steady WIP"),
			).toBeInTheDocument();
			expect(screen.getByText("Good job!")).toBeInTheDocument();
		});

		it("should show 'confident' indicator when difference is less than 1.0", () => {
			// 0.5% difference (confident)
			const startedItems = new RunChartData([10, 10, 10], 3, 200);
			const closedItems = new RunChartData([10, 10, 10], 3, 199);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are keeping a steady WIP"),
			).toBeInTheDocument();
			expect(screen.getByText("Good job!")).toBeInTheDocument();
		});

		it("should show 'good' indicator when started and closed are within 5%", () => {
			// Within 5% difference (good)
			const startedItems = new RunChartData([9, 10, 11], 3, 30);
			const closedItems = new RunChartData([10, 10, 9], 3, 29);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are keeping a steady WIP"),
			).toBeInTheDocument();
			expect(screen.getByText("Good job!")).toBeInTheDocument();
		});

		it("should show 'caution' indicator when difference is between 5% and 10%", () => {
			// ~7% difference (caution) with average difference > 1.0
			const startedItems = new RunChartData([15, 15, 15], 3, 45);
			const closedItems = new RunChartData([14, 14, 14], 3, 42);
			// Averages: 15.0 vs 14.0 = 1.0 difference

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			// Since 45 vs 42 means started > closed
			expect(
				screen.getByText("You are starting more items than you close"),
			).toBeInTheDocument();
			expect(
				screen.getByText("Observe and take action if needed!"),
			).toBeInTheDocument();
		});

		it("should show 'bad' indicator when difference is more than 15%", () => {
			// 20% difference (bad)
			const startedItems = new RunChartData([10, 10, 10], 3, 30);
			const closedItems = new RunChartData([8, 8, 8], 3, 24);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are starting more items than you close"),
			).toBeInTheDocument();
			expect(screen.getByText("Reflect on WIP control!")).toBeInTheDocument();
		});

		it("should show appropriate message when closing more than starting", () => {
			// 25% difference with closed > started
			const startedItems = new RunChartData([6, 6, 6], 3, 18);
			const closedItems = new RunChartData([8, 8, 8], 3, 24);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are closing more items than you start"),
			).toBeInTheDocument();
			expect(screen.getByText("Reflect on WIP control!")).toBeInTheDocument();
		});

		it("should handle zero values properly", () => {
			// One side is zero
			const startedItems = new RunChartData([0, 0, 0], 3, 0);
			const closedItems = new RunChartData([5, 5, 5], 3, 15);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are closing more items than you start"),
			).toBeInTheDocument();
			expect(screen.getByText("Reflect on WIP control!")).toBeInTheDocument();
		});
	});
});
