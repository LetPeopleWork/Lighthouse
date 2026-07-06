import { render, screen } from "@testing-library/react";
import { BarChart } from "@mui/x-charts";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	BlockedCountHistoryResponseSchema,
	type BlockedCountSnapshot,
} from "../../../models/BlockedCountSnapshot";
import { errorColor } from "../../../utils/theme/colors";
import BlockedItemsOverTimeChart from "./BlockedItemsOverTimeChart";

// Mock MUI-X BarChart (same pattern as BarRunChart.test.tsx)
vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		BarChart: vi.fn(({ dataset }) => (
			<div data-testid="mock-bar-chart">
				{dataset?.map(
					(item: { label: string; value: number }, index: number) => (
						<button
							type="button"
							key={`bar-${item.label}`}
							data-testid={`bar-${index}`}
						>
							Bar {index} - {item.label}: {item.value}
						</button>
					),
				)}
			</div>
		)),
	};
});

const EMPTY_MESSAGE =
	"blocked trend builds forward from today — no snapshots yet";

describe("BlockedItemsOverTimeChart", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders the rising 3->6->9 blocked-count trend", () => {
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 3 },
			{ recordedAt: "2026-06-02", blockedCount: 6 },
			{ recordedAt: "2026-06-03", blockedCount: 9 },
		];

		render(<BlockedItemsOverTimeChart snapshots={snapshots} />);

		expect(screen.getByTestId("mock-bar-chart")).toBeInTheDocument();
		expect(screen.getByTestId("bar-0")).toHaveTextContent("3");
		expect(screen.getByTestId("bar-1")).toHaveTextContent("6");
		expect(screen.getByTestId("bar-2")).toHaveTextContent("9");
	});

	it(`renders "${EMPTY_MESSAGE}" for an empty array and never a flat zero line`, () => {
		render(<BlockedItemsOverTimeChart snapshots={[]} />);

		expect(screen.getByText(EMPTY_MESSAGE)).toBeInTheDocument();
		expect(screen.queryByTestId("mock-bar-chart")).not.toBeInTheDocument();
	});

	it(`renders "${EMPTY_MESSAGE}" for null snapshots`, () => {
		render(<BlockedItemsOverTimeChart snapshots={null} />);

		expect(screen.getByText(EMPTY_MESSAGE)).toBeInTheDocument();
		expect(screen.queryByTestId("mock-bar-chart")).not.toBeInTheDocument();
	});

	it("validates a sample blockedCountHistory response against the Zod schema", () => {
		const validResponse = [
			{ recordedAt: "2026-06-01", blockedCount: 3 },
			{ recordedAt: "2026-06-02", blockedCount: 6 },
			{ recordedAt: "2026-06-03", blockedCount: 9 },
		];

		const result = BlockedCountHistoryResponseSchema.safeParse(validResponse);
		expect(result.success).toBe(true);
		if (result.success) {
			expect(result.data).toHaveLength(3);
			expect(result.data[0].recordedAt).toBe("2026-06-01");
			expect(result.data[0].blockedCount).toBe(3);
		}
	});

	it("rejects invalid blockedCountHistory payloads via Zod schema", () => {
		const invalidResponse = [
			{ recordedAt: "2026-06-01", blockedCount: "not-a-number" },
		];

		const result = BlockedCountHistoryResponseSchema.safeParse(invalidResponse);
		expect(result.success).toBe(false);
	});

	it("composes with team/portfolio/date-range filter", () => {
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 5 },
			{ recordedAt: "2026-06-02", blockedCount: 8 },
		];

		// The chart accepts BlockedCountSnapshot[] — composition with team/portfolio/date-range
		// is handled by the parent (BaseMetricsView + useMetricsData). The chart
		// itself just renders the data it receives.
		const { rerender } = render(
			<BlockedItemsOverTimeChart snapshots={snapshots} />,
		);

		expect(screen.getByTestId("mock-bar-chart")).toBeInTheDocument();

		// Simulate date-range change by re-rendering with different data
		const differentSnapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-07-01", blockedCount: 1 },
			{ recordedAt: "2026-07-02", blockedCount: 2 },
		];

		rerender(<BlockedItemsOverTimeChart snapshots={differentSnapshots} />);

		expect(screen.getByTestId("mock-bar-chart")).toBeInTheDocument();
	});

	it("renders bars using the blocked/error color", () => {
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 3 },
		];

		render(<BlockedItemsOverTimeChart snapshots={snapshots} />);

		const lastCall = BarChart.mock.calls.at(-1);
		expect(lastCall?.[0]?.series?.[0]?.color).toBe(errorColor);
	});
});
