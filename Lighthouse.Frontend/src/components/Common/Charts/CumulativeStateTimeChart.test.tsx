import { render, screen, within } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ICumulativeStateTimeStateRow } from "../../../models/Metrics/CumulativeStateTime";
import { testTheme } from "../../../tests/testTheme";
import CumulativeStateTimeChart from "./CumulativeStateTimeChart";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		BarChart: vi.fn(({ dataset, series, xAxis, children, onItemClick }) => (
			<div
				data-testid="mock-bar-chart"
				data-dataset={dataset ? JSON.stringify(dataset) : undefined}
				data-series={series ? JSON.stringify(series) : undefined}
				data-x-axis={xAxis ? JSON.stringify(xAxis) : undefined}
			>
				<button
					type="button"
					data-testid="bar-click-proxy"
					onClick={() => onItemClick?.(null, { dataIndex: 0 })}
				>
					bar
				</button>
				{children}
			</div>
		)),
	};
});

const getMockStateRow = (
	overrides?: Partial<ICumulativeStateTimeStateRow>,
): ICumulativeStateTimeStateRow => ({
	state: "Doing",
	workflowOrder: 0,
	totalDays: 12,
	completedContributionDays: 8,
	ongoingContributionDays: 4,
	itemCount: 5,
	completedItemCount: 3,
	ongoingItemCount: 2,
	meanDays: 2.4,
	medianDays: 2,
	...overrides,
});

const threeStatesInOrder: ICumulativeStateTimeStateRow[] = [
	getMockStateRow({ state: "Doing", workflowOrder: 2, totalDays: 30 }),
	getMockStateRow({ state: "Backlog", workflowOrder: 0, totalDays: 5 }),
	getMockStateRow({ state: "Review", workflowOrder: 1, totalDays: 14 }),
];

describe("CumulativeStateTimeChart", () => {
	it("renders one bar per workflow state ordered by workflow order", () => {
		render(<CumulativeStateTimeChart data={{ states: threeStatesInOrder }} />);

		const chart = screen.getByTestId("mock-bar-chart");
		const xAxis = JSON.parse(chart.getAttribute("data-x-axis") ?? "[]");

		expect(xAxis[0].data).toEqual(["Backlog", "Review", "Doing"]);
	});

	it("stacks a completed segment and an ongoing segment per bar", () => {
		render(
			<CumulativeStateTimeChart
				data={{
					states: [
						getMockStateRow({
							completedContributionDays: 8,
							ongoingContributionDays: 4,
						}),
					],
				}}
			/>,
		);

		const chart = screen.getByTestId("mock-bar-chart");
		const series = JSON.parse(chart.getAttribute("data-series") ?? "[]");

		const stackIds = new Set(series.map((s: { stack?: string }) => s.stack));
		expect(stackIds.size).toBe(1);
		expect(series).toHaveLength(2);

		expect(series[0].data[0]).toBe(8);
		expect(series[1].data[0]).toBe(4);
	});

	it("renders an SVG hatch pattern for the ongoing segment", () => {
		const { container } = render(
			<CumulativeStateTimeChart data={{ states: [getMockStateRow()] }} />,
		);

		expect(container.querySelector("pattern")).not.toBeNull();
	});

	it("shows the per-state fields and completed/ongoing counts without an included-items line", () => {
		render(
			<CumulativeStateTimeChart
				data={{
					states: [
						getMockStateRow({
							state: "Review",
							itemCount: 5,
							completedItemCount: 3,
							ongoingItemCount: 2,
							meanDays: 2.4,
							medianDays: 2,
						}),
					],
				}}
			/>,
		);

		const tooltip = screen.getByTestId("cumulative-state-tooltip-Review");
		const completed = within(tooltip).getByTestId("tooltip-completed-count");
		const ongoing = within(tooltip).getByTestId("tooltip-ongoing-count");

		expect(tooltip.textContent).toContain("Review");
		expect(completed.textContent).toContain("3");
		expect(ongoing.textContent).toContain("2");
		expect(tooltip.textContent?.toLowerCase()).not.toContain("included items");
	});

	it("shows an empty-state placeholder when there are no states", () => {
		render(<CumulativeStateTimeChart data={{ states: [] }} />);

		expect(screen.queryByTestId("mock-bar-chart")).toBeNull();
		expect(
			screen.getByTestId("cumulative-state-time-empty"),
		).toBeInTheDocument();
	});

	it("shows a zero-contributing placeholder when every state has no recorded time", () => {
		render(
			<CumulativeStateTimeChart
				data={{
					states: [
						getMockStateRow({
							totalDays: 0,
							completedContributionDays: 0,
							ongoingContributionDays: 0,
						}),
					],
				}}
			/>,
		);

		expect(screen.queryByTestId("mock-bar-chart")).toBeNull();
		expect(
			screen.getByTestId("cumulative-state-time-zero"),
		).toBeInTheDocument();
	});

	it("formats bar labels with one adaptive unit chosen from the largest bar", () => {
		render(<CumulativeStateTimeChart data={{ states: threeStatesInOrder }} />);

		const chart = screen.getByTestId("mock-bar-chart");
		const xAxis = JSON.parse(chart.getAttribute("data-x-axis") ?? "[]");
		const series = JSON.parse(chart.getAttribute("data-series") ?? "[]");

		expect(JSON.stringify(xAxis)).toContain("w");
		expect(series[0].unit ?? series[0].label).toBeDefined();
	});

	it("invokes onBarClick with the state name when a bar is clicked", () => {
		const onBarClick = vi.fn();
		render(
			<CumulativeStateTimeChart
				data={{ states: [getMockStateRow({ state: "Doing" })] }}
				onBarClick={onBarClick}
			/>,
		);

		screen.getByTestId("bar-click-proxy").click();

		expect(onBarClick).toHaveBeenCalledWith("Doing");
	});

	it("renders the picker slot inside the chart toolbar", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: [getMockStateRow()] }}
				pickerSlot={<div data-testid="picker-slot">picker</div>}
			/>,
		);

		expect(screen.getByTestId("picker-slot")).toBeInTheDocument();
	});
});

const efficiencyFixtureRows: ICumulativeStateTimeStateRow[] = [
	getMockStateRow({ state: "In Progress", workflowOrder: 0, totalDays: 184 }),
	getMockStateRow({
		state: "Waiting for Review",
		workflowOrder: 1,
		totalDays: 200,
	}),
	getMockStateRow({
		state: "Ready for Test",
		workflowOrder: 2,
		totalDays: 156,
	}),
];

describe("CumulativeStateTimeChart flow-efficiency figure", () => {
	it("shows the flow-efficiency percentage folded over the displayed wait states", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: efficiencyFixtureRows }}
				waitStates={["Waiting for Review", "Ready for Test"]}
			/>,
		);

		const figure = screen.getByTestId("cumulative-state-time-flow-efficiency");
		expect(figure.textContent).toContain("34");
	});

	it("recomputes the figure over the narrowed set when fewer states are displayed", () => {
		render(
			<CumulativeStateTimeChart
				data={{
					states: [
						getMockStateRow({ state: "In Progress", totalDays: 150 }),
						getMockStateRow({ state: "Waiting for Review", totalDays: 90 }),
					],
				}}
				waitStates={["Waiting for Review"]}
			/>,
		);

		const figure = screen.getByTestId("cumulative-state-time-flow-efficiency");
		expect(figure.textContent).toContain("63");
	});

	it("suppresses the figure when no wait states are configured", () => {
		render(
			<CumulativeStateTimeChart data={{ states: efficiencyFixtureRows }} />,
		);

		expect(
			screen.queryByTestId("cumulative-state-time-flow-efficiency"),
		).toBeNull();
	});
});

const seriesOf = (): { label?: string }[] => {
	const chart = screen.getByTestId("mock-bar-chart");
	return JSON.parse(chart.getAttribute("data-series") ?? "[]");
};

const completedChip = (): HTMLElement =>
	screen.getByRole("button", { name: "Completed visibility toggle" });

const ongoingChip = (): HTMLElement =>
	screen.getByRole("button", { name: "Ongoing visibility toggle" });

describe("CumulativeStateTimeChart completion-class legend toggle (US-5144-01)", () => {
	const stateWithBothClasses = [
		getMockStateRow({
			state: "Doing",
			completedContributionDays: 12,
			ongoingContributionDays: 20,
		}),
	];

	it("offers no completion chips unless the filter is explicitly enabled", () => {
		render(
			<CumulativeStateTimeChart data={{ states: stateWithBothClasses }} />,
		);

		expect(
			screen.queryByRole("button", { name: "Completed visibility toggle" }),
		).toBeNull();
		expect(seriesOf()).toHaveLength(2);
	});

	it("shows both Completed and Ongoing chips active by default when the filter is enabled", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: stateWithBothClasses }}
				completionFilterEnabled
			/>,
		);

		expect(completedChip()).toHaveAttribute("aria-pressed", "true");
		expect(ongoingChip()).toHaveAttribute("aria-pressed", "true");
		expect(seriesOf()).toHaveLength(2);
	});

	it("hides the completed segment when the Completed chip is clicked", async () => {
		const user = userEvent.setup();
		render(
			<CumulativeStateTimeChart
				data={{ states: stateWithBothClasses }}
				completionFilterEnabled
			/>,
		);

		await user.click(completedChip());

		const series = seriesOf();
		expect(series).toHaveLength(1);
		expect(series[0].label).toContain("Ongoing");
		expect(completedChip()).toHaveAttribute("aria-pressed", "false");
	});

	it("hides the ongoing segment when the Ongoing chip is clicked", async () => {
		const user = userEvent.setup();
		render(
			<CumulativeStateTimeChart
				data={{ states: stateWithBothClasses }}
				completionFilterEnabled
			/>,
		);

		await user.click(ongoingChip());

		const series = seriesOf();
		expect(series).toHaveLength(1);
		expect(series[0].label).toContain("Completed");
		expect(ongoingChip()).toHaveAttribute("aria-pressed", "false");
	});

	it("restores a hidden segment when its chip is clicked again", async () => {
		const user = userEvent.setup();
		render(
			<CumulativeStateTimeChart
				data={{ states: stateWithBothClasses }}
				completionFilterEnabled
			/>,
		);

		await user.click(completedChip());
		await user.click(completedChip());

		expect(seriesOf()).toHaveLength(2);
		expect(completedChip()).toHaveAttribute("aria-pressed", "true");
	});

	it("keeps at least one segment visible when the user tries to hide both", async () => {
		const user = userEvent.setup();
		render(
			<CumulativeStateTimeChart
				data={{ states: stateWithBothClasses }}
				completionFilterEnabled
			/>,
		);

		await user.click(completedChip());
		await user.click(ongoingChip());

		const series = seriesOf();
		expect(series).toHaveLength(1);
		expect(series[0].label).toContain("Ongoing");
	});

	it("offers no chips and shows both segments when the filter is disabled (an item is picked)", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: stateWithBothClasses }}
				completionFilterEnabled={false}
			/>,
		);

		expect(
			screen.queryByRole("button", { name: "Completed visibility toggle" }),
		).toBeNull();
		expect(seriesOf()).toHaveLength(2);
	});

	it("resets hide-state when the filter is disabled and re-enabled (selection clears prior hide)", async () => {
		const user = userEvent.setup();
		const { rerender } = render(
			<CumulativeStateTimeChart
				data={{ states: stateWithBothClasses }}
				completionFilterEnabled
			/>,
		);

		await user.click(completedChip());
		expect(seriesOf()).toHaveLength(1);

		rerender(
			<CumulativeStateTimeChart
				data={{ states: stateWithBothClasses }}
				completionFilterEnabled={false}
			/>,
		);
		expect(seriesOf()).toHaveLength(2);

		rerender(
			<CumulativeStateTimeChart
				data={{ states: stateWithBothClasses }}
				completionFilterEnabled
			/>,
		);

		expect(completedChip()).toHaveAttribute("aria-pressed", "true");
		expect(ongoingChip()).toHaveAttribute("aria-pressed", "true");
		expect(seriesOf()).toHaveLength(2);
	});
});

const waitHighlightRows: ICumulativeStateTimeStateRow[] = [
	getMockStateRow({
		state: "In Progress",
		workflowOrder: 0,
		totalDays: 30,
		completedContributionDays: 18,
		ongoingContributionDays: 12,
	}),
	getMockStateRow({
		state: "Waiting for Review",
		workflowOrder: 1,
		totalDays: 20,
		completedContributionDays: 14,
		ongoingContributionDays: 6,
	}),
	getMockStateRow({
		state: "Ready for Test",
		workflowOrder: 2,
		totalDays: 10,
		completedContributionDays: 7,
		ongoingContributionDays: 3,
	}),
];

interface RenderedSeries {
	id?: string;
	label?: string;
	color?: string;
	stack?: string;
	data: (number | null)[];
}

const fullSeriesOf = (): RenderedSeries[] => {
	const chart = screen.getByTestId("mock-bar-chart");
	return JSON.parse(chart.getAttribute("data-series") ?? "[]");
};

const stateIndex = (state: string): number =>
	[...waitHighlightRows]
		.sort((a, b) => a.workflowOrder - b.workflowOrder)
		.findIndex((row) => row.state === state);

const seriesById = (id: string): RenderedSeries | undefined =>
	fullSeriesOf().find((entry) => entry.id === id);

const waitColoured = (): string[] => {
	const completedWait = seriesById("completedWait");
	if (!completedWait) {
		return [];
	}
	const ordered = [...waitHighlightRows].sort(
		(a, b) => a.workflowOrder - b.workflowOrder,
	);
	return ordered
		.filter((row) => completedWait.data[stateIndex(row.state)] !== null)
		.map((row) => row.state);
};

const waitKey = (): HTMLElement | null =>
	screen.queryByTestId("cumulative-state-time-wait-legend");

describe("CumulativeStateTimeChart wait-state colouring (US-04-03)", () => {
	it("colours wait bars in the amber family while leaving non-wait bars teal", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: waitHighlightRows }}
				waitStates={["Waiting for Review"]}
			/>,
		);

		const completedNonWait = seriesById("completedNonWait");
		const completedWait = seriesById("completedWait");

		expect(completedNonWait?.color).toBe(testTheme.palette.primary.main);
		expect(completedWait?.color).toBe(testTheme.palette.warning.main);
	});

	it("keeps the completed-solid vs ongoing-hatch distinction within wait bars", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: waitHighlightRows }}
				waitStates={["Waiting for Review"]}
			/>,
		);

		const completedWait = seriesById("completedWait");
		const ongoingWait = seriesById("ongoingWait");

		expect(completedWait?.color).toBe(testTheme.palette.warning.main);
		expect(ongoingWait?.color).toContain("url(#");
		expect(completedWait?.color).not.toEqual(ongoingWait?.color);
	});

	it("routes each bar's value into exactly one of the wait/non-wait pairs", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: waitHighlightRows }}
				waitStates={["Waiting for Review"]}
			/>,
		);

		const waitIdx = stateIndex("Waiting for Review");
		const activeIdx = stateIndex("In Progress");

		expect(seriesById("completedWait")?.data[waitIdx]).toBe(14);
		expect(seriesById("completedNonWait")?.data[waitIdx]).toBeNull();
		expect(seriesById("completedNonWait")?.data[activeIdx]).toBe(18);
		expect(seriesById("completedWait")?.data[activeIdx]).toBeNull();
	});

	it("colours every underlying raw bar when a mapping name is configured as wait", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: waitHighlightRows }}
				waitStates={["Waiting"]}
				stateMappings={[
					{
						name: "Waiting",
						states: ["Waiting for Review", "Ready for Test"],
					},
				]}
			/>,
		);

		expect(waitColoured()).toEqual(["Waiting for Review", "Ready for Test"]);
	});

	it("shows a non-clickable wait colour key labelled wait when a wait state is present", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: waitHighlightRows }}
				waitStates={["Waiting for Review"]}
			/>,
		);

		const key = waitKey();
		expect(key).not.toBeNull();
		expect(key?.textContent?.toLowerCase()).toContain("wait");
		expect(key?.tagName).not.toBe("BUTTON");
	});

	it("drives the colouring from the same resolveWaitRawStates used by the figure", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: waitHighlightRows }}
				waitStates={["Waiting"]}
				stateMappings={[{ name: "Waiting", states: ["Ready for Test"] }]}
			/>,
		);

		const figure = screen.getByTestId("cumulative-state-time-flow-efficiency");
		expect(figure.textContent).toContain("83");
		expect(waitColoured()).toEqual(["Ready for Test"]);
	});

	it("emits no wait series, no amber colour and no wait key when no wait states are configured", () => {
		render(<CumulativeStateTimeChart data={{ states: waitHighlightRows }} />);

		expect(seriesById("completedWait")).toBeUndefined();
		expect(seriesById("ongoingWait")).toBeUndefined();
		expect(seriesById("completedNonWait")?.color).toBe(
			testTheme.palette.primary.main,
		);
		expect(waitKey()).toBeNull();
	});

	it("never builds an xAxis colorMap or a dotted wait pattern", () => {
		const { container } = render(
			<CumulativeStateTimeChart
				data={{ states: waitHighlightRows }}
				waitStates={["Waiting for Review"]}
			/>,
		);

		const chart = screen.getByTestId("mock-bar-chart");
		const xAxis = JSON.parse(chart.getAttribute("data-x-axis") ?? "[]");
		expect(xAxis[0]?.colorMap).toBeUndefined();
		expect(
			container.querySelector("pattern#cumulative-state-time-wait-pattern"),
		).toBeNull();
	});
});

describe("CumulativeStateTimeChart header layout (US-04-03)", () => {
	it("places the flow-efficiency figure below the title, not beside it", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: waitHighlightRows }}
				waitStates={["Waiting for Review"]}
			/>,
		);

		const titleBlock = screen.getByTestId("cumulative-state-time-title-block");
		const figure = within(titleBlock).getByTestId(
			"cumulative-state-time-flow-efficiency",
		);

		expect(titleBlock.textContent).toContain("Cumulative Time per State");
		expect(figure).toBeInTheDocument();
	});

	it("removes the wait chip and the header completion chips", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: waitHighlightRows }}
				waitStates={["Waiting for Review"]}
				completionFilterEnabled
			/>,
		);

		expect(
			screen.queryByText("Wait", { selector: ".MuiChip-label" }),
		).toBeNull();
	});
});
