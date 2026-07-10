import { BarChart } from "@mui/x-charts";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	BlockedCountHistoryResponseSchema,
	type BlockedCountSnapshot,
} from "../../../models/BlockedCountSnapshot";
import type { IFeature } from "../../../models/Feature";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { errorColor } from "../../../utils/theme/colors";
import BlockedItemsOverTimeChart from "./BlockedItemsOverTimeChart";

// Mock MUI-X BarChart (same pattern as BarRunChart.test.tsx). The mock wires
// the real onItemClick contract: clicking a bar fires
// onItemClick(event, { type, seriesId, dataIndex }) exactly as @mui/x-charts 9.x does.
vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		BarChart: vi.fn(
			({
				dataset,
				onItemClick,
			}: {
				dataset?: { label: string; value: number }[];
				onItemClick?: (
					event: unknown,
					params: { type: "bar"; seriesId: string; dataIndex: number },
				) => void;
			}) => (
				<div data-testid="mock-bar-chart">
					{dataset?.map(
						(item: { label: string; value: number }, index: number) => (
							<button
								type="button"
								key={`bar-${item.label}`}
								data-testid={`bar-${index}`}
								onClick={() =>
									onItemClick?.(new MouseEvent("click"), {
										type: "bar",
										seriesId: "value",
										dataIndex: index,
									})
								}
							>
								Bar {index} - {item.label}: {item.value}
							</button>
						),
					)}
				</div>
			),
		),
	};
});

// Mock the WorkItemsDialog collaborator: assert the chart drives it (open + items),
// not the dialog's own DataGrid internals (covered by WorkItemsDialog.test.tsx).
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: ({
		title,
		items,
		open,
	}: {
		title: string;
		items: IWorkItem[];
		open: boolean;
	}) =>
		open ? (
			<div data-testid="work-items-dialog">
				<span data-testid="dialog-title">{title}</span>
				{items.length > 0 ? (
					items.map((item) => (
						<div key={item.id} data-testid="dialog-row">
							{item.name}
						</div>
					))
				) : (
					<span data-testid="dialog-empty">no items blocked on this date</span>
				)}
			</div>
		) : null,
}));

const EMPTY_MESSAGE =
	"blocked trend builds forward from today — no snapshots yet";

const createMockMetricsService = (
	items: IWorkItem[] = [],
): IMetricsService<IWorkItem | IFeature> =>
	({
		getBlockedItemsAtDate: vi.fn().mockResolvedValue(items),
	}) as unknown as IMetricsService<IWorkItem | IFeature>;

const workItem = (id: number, name: string): IWorkItem =>
	({ id, name, referenceId: `WI-${id}` }) as unknown as IWorkItem;

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

		render(
			<BlockedItemsOverTimeChart
				snapshots={snapshots}
				metricsService={createMockMetricsService()}
				ownerId={1}
			/>,
		);

		expect(screen.getByTestId("mock-bar-chart")).toBeInTheDocument();
		expect(screen.getByTestId("bar-0")).toHaveTextContent("3");
		expect(screen.getByTestId("bar-1")).toHaveTextContent("6");
		expect(screen.getByTestId("bar-2")).toHaveTextContent("9");
	});

	it(`renders "${EMPTY_MESSAGE}" for an empty array and never a flat zero line`, () => {
		render(
			<BlockedItemsOverTimeChart
				snapshots={[]}
				metricsService={createMockMetricsService()}
				ownerId={1}
			/>,
		);

		expect(screen.getByText(EMPTY_MESSAGE)).toBeInTheDocument();
		expect(screen.queryByTestId("mock-bar-chart")).not.toBeInTheDocument();
	});

	it(`renders "${EMPTY_MESSAGE}" for null snapshots`, () => {
		render(
			<BlockedItemsOverTimeChart
				snapshots={null}
				metricsService={createMockMetricsService()}
				ownerId={1}
			/>,
		);

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
			<BlockedItemsOverTimeChart
				snapshots={snapshots}
				metricsService={createMockMetricsService()}
				ownerId={1}
			/>,
		);

		expect(screen.getByTestId("mock-bar-chart")).toBeInTheDocument();

		// Simulate date-range change by re-rendering with different data
		const differentSnapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-07-01", blockedCount: 1 },
			{ recordedAt: "2026-07-02", blockedCount: 2 },
		];

		rerender(
			<BlockedItemsOverTimeChart
				snapshots={differentSnapshots}
				metricsService={createMockMetricsService()}
				ownerId={1}
			/>,
		);

		expect(screen.getByTestId("mock-bar-chart")).toBeInTheDocument();
	});

	it("renders bars using the blocked/error color", () => {
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 3 },
		];

		render(
			<BlockedItemsOverTimeChart
				snapshots={snapshots}
				metricsService={createMockMetricsService()}
				ownerId={1}
			/>,
		);

		const calls = vi.mocked(BarChart).mock.calls;
		const lastCall = calls[calls.length - 1];
		expect(lastCall?.[0]?.series?.[0]?.color).toBe(errorColor);
	});

	it("opens WorkItemsDialog with the blocked item rows when a bar is clicked", async () => {
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 3 },
			{ recordedAt: "2026-06-02", blockedCount: 6 },
			{ recordedAt: "2026-06-03", blockedCount: 9 },
		];
		const items = [workItem(1, "Blocked Alpha"), workItem(2, "Blocked Beta")];
		const service = createMockMetricsService(items);

		render(
			<BlockedItemsOverTimeChart
				snapshots={snapshots}
				metricsService={service}
				ownerId={7}
			/>,
		);

		expect(screen.queryByTestId("work-items-dialog")).not.toBeInTheDocument();

		fireEvent.click(screen.getByTestId("bar-1"));

		await waitFor(() =>
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument(),
		);

		// Resolves the clicked bar to its date and fetches from the 08-03/08-04 endpoint.
		expect(service.getBlockedItemsAtDate).toHaveBeenCalledWith(7, "2026-06-02");

		const rows = screen.getAllByTestId("dialog-row");
		expect(rows).toHaveLength(2);
		expect(screen.getByText("Blocked Alpha")).toBeInTheDocument();
		expect(screen.getByText("Blocked Beta")).toBeInTheDocument();
		expect(screen.getByTestId("dialog-title")).toHaveTextContent("2026-06-02");
	});

	it("opens the dialog with a no-items message for a date with no blocked items", async () => {
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 0 },
		];
		const service = createMockMetricsService([]);

		render(
			<BlockedItemsOverTimeChart
				snapshots={snapshots}
				metricsService={service}
				ownerId={3}
			/>,
		);

		fireEvent.click(screen.getByTestId("bar-0"));

		await waitFor(() =>
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument(),
		);

		expect(service.getBlockedItemsAtDate).toHaveBeenCalledWith(3, "2026-06-01");
		expect(screen.queryByTestId("dialog-row")).not.toBeInTheDocument();
		expect(screen.getByTestId("dialog-empty")).toBeInTheDocument();
	});

	it("drills into the latest bar's live blocked set", async () => {
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 3 },
			{ recordedAt: "2026-06-02", blockedCount: 6 },
			{ recordedAt: "2026-06-03", blockedCount: 9 },
		];
		const items = [workItem(99, "Live Blocked")];
		const service = createMockMetricsService(items);

		render(
			<BlockedItemsOverTimeChart
				snapshots={snapshots}
				metricsService={service}
				ownerId={42}
			/>,
		);

		fireEvent.click(screen.getByTestId("bar-2"));

		await waitFor(() =>
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument(),
		);

		expect(service.getBlockedItemsAtDate).toHaveBeenCalledWith(
			42,
			"2026-06-03",
		);
		expect(screen.getByText("Live Blocked")).toBeInTheDocument();
	});

	it("surfaces a capture-gap note when reconstruction returns nothing for a counted bar", async () => {
		// The bar records 5 blocked on this date, but transition capture doesn't reach that far
		// back, so the endpoint reconstructs none — the dialog must say so, not show a bare empty.
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 5 },
		];
		const service = createMockMetricsService([]);

		render(
			<BlockedItemsOverTimeChart
				snapshots={snapshots}
				metricsService={service}
				ownerId={1}
			/>,
		);

		fireEvent.click(screen.getByTestId("bar-0"));

		await waitFor(() =>
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument(),
		);

		expect(screen.getByTestId("dialog-title")).toHaveTextContent(
			"5 blocked on this date — per-item detail isn't recorded this far back",
		);
	});

	it("surfaces a partial capture-gap note when reconstruction returns fewer than the counted bar", async () => {
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 5 },
		];
		const service = createMockMetricsService([
			workItem(1, "Blocked Alpha"),
			workItem(2, "Blocked Beta"),
		]);

		render(
			<BlockedItemsOverTimeChart
				snapshots={snapshots}
				metricsService={service}
				ownerId={1}
			/>,
		);

		fireEvent.click(screen.getByTestId("bar-0"));

		await waitFor(() =>
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument(),
		);

		expect(screen.getByTestId("dialog-title")).toHaveTextContent(
			"showing 2 of 5 — earlier per-item detail isn't recorded this far back",
		);
	});

	it("shows no capture-gap note when reconstruction matches the counted bar", async () => {
		const snapshots: BlockedCountSnapshot[] = [
			{ recordedAt: "2026-06-01", blockedCount: 2 },
		];
		const service = createMockMetricsService([
			workItem(1, "Blocked Alpha"),
			workItem(2, "Blocked Beta"),
		]);

		render(
			<BlockedItemsOverTimeChart
				snapshots={snapshots}
				metricsService={service}
				ownerId={1}
			/>,
		);

		fireEvent.click(screen.getByTestId("bar-0"));

		await waitFor(() =>
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument(),
		);

		expect(screen.getByTestId("dialog-title")).not.toHaveTextContent(
			"per-item detail isn't recorded",
		);
		// No note means no separator: the empty-string return path must stay empty (not any text).
		expect(screen.getByTestId("dialog-title")).not.toHaveTextContent("·");
	});

	it("ignores a bar click whose dataIndex resolves to no bar", () => {
		const service = createMockMetricsService([workItem(1, "Blocked Alpha")]);
		render(
			<BlockedItemsOverTimeChart
				snapshots={[{ recordedAt: "2026-06-01", blockedCount: 3 }]}
				metricsService={service}
				ownerId={1}
			/>,
		);

		// Fire the real onItemClick contract with an out-of-range index — the guard must swallow it
		// (no fetch, no dialog, no throw) rather than dereference an undefined bar.
		const calls = vi.mocked(BarChart).mock.calls;
		const onItemClick = calls[calls.length - 1]?.[0]?.onItemClick;
		expect(() =>
			onItemClick?.(new MouseEvent("click"), {
				type: "bar",
				seriesId: "value",
				dataIndex: 99,
			}),
		).not.toThrow();

		expect(service.getBlockedItemsAtDate).not.toHaveBeenCalled();
		expect(screen.queryByTestId("work-items-dialog")).not.toBeInTheDocument();
	});
});
