import { createTheme } from "@mui/material";
import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import WidgetShell from "../../../pages/Common/MetricsView/WidgetShell";
import type { WidgetStatus } from "../../../pages/Common/MetricsView/widgetStatus";
import { testTheme } from "../../../tests/testTheme";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));

vi.mock("../../../services/UsageData/usageDataReporter", async (original) => ({
	...(await original<
		typeof import("../../../services/UsageData/usageDataReporter")
	>()),
	useUsageDataReporter: () => reportUsage,
}));

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return { ...actual, useTheme: () => testTheme };
});

// The real dialog is a modal, so focus behaves as it does in the product; its grid is replaced by the ids it was handed.
vi.mock("../WorkItemsDialog/WorkItemsDialog", async () => {
	const { Dialog, DialogTitle } =
		await vi.importActual<typeof import("@mui/material")>("@mui/material");
	return {
		default: ({
			title,
			items,
			open,
			onClose,
		}: {
			title: string;
			items: IWorkItem[];
			open: boolean;
			onClose: () => void;
		}) =>
			open ? (
				<Dialog open onClose={onClose}>
					<DialogTitle>{title}</DialogTitle>
					<ul aria-label="Listed Work Items">
						{items.map((item) => (
							<li key={item.id}>{item.referenceId}</li>
						))}
					</ul>
					<button type="button" onClick={onClose}>
						Close the list
					</button>
				</Dialog>
			) : null,
	};
});

vi.mock("./TimeBlackoutOverlay", () => ({ default: () => null }));

vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		ChartsContainer: ({ children }: { children: React.ReactNode }) => (
			<div>{children}</div>
		),
		ScatterPlot: () => null,
		ChartsXAxis: () => null,
		ChartsYAxis: () => null,
		ChartsTooltip: () => null,
		useDrawingArea: () => ({ left: 0, top: 0, width: 400, height: 200 }),
		ChartsReferenceLine: () => null,
	};
});

const WIDGET_KEY = "cycleScatter";
const AMBER = createTheme().palette.warning.main;

const closedItem = (
	referenceId: string,
	cycleTime: number,
	day: number,
): IWorkItem => ({
	id: Number(referenceId.replace(/\D/g, "")),
	referenceId,
	name: referenceId,
	url: null,
	cycleTime,
	startedDate: new Date(2026, 1, 1),
	closedDate: new Date(2026, 2, day),
	workItemAge: cycleTime,
	state: "Done",
	stateCategory: "Done",
	type: "Task",
	parentWorkItemReference: "",
	isBlocked: false,
});

// Team Zenith: five typical items put the median at 6 days, so the cut is 30 days; TZ-301 took 58.
const teamZenith = (): IWorkItem[] =>
	[
		closedItem("TZ-101", 2, 2),
		closedItem("TZ-102", 4, 3),
		closedItem("TZ-103", 6, 4),
		closedItem("TZ-104", 6, 5),
		closedItem("TZ-105", 8, 6),
		closedItem("TZ-301", 58, 12),
	].map((item) => Object.freeze(item));

const withSecondOutlier = (): IWorkItem[] => [
	...teamZenith(),
	closedItem("TZ-302", 40, 11),
];

const percentiles = (p95: number): IPercentileValue[] => [
	{ percentile: 50, value: 6 },
	{ percentile: 85, value: 8 },
	{ percentile: 95, value: p95 },
];

const chartIn = (
	items: IWorkItem[],
	p95: number,
	sleValue: number,
	status: WidgetStatus,
) => (
	<WidgetShell
		widgetKey={WIDGET_KEY}
		title="Cycle Time scatterplot"
		status={status}
		hasContentToDim
	>
		<CycleTimeScatterPlotChart
			ownerKind="team"
			percentileValues={percentiles(p95)}
			cycleTimeDataPoints={items}
			serviceLevelExpectation={{ percentile: 85, value: sleValue }}
		/>
	</WidgetShell>
);

// Team Zenith's lines sit below the axis: the 95% line is at 15 days and the SLE at 7.
const showChart = (
	items: IWorkItem[] = teamZenith(),
	p95 = 15,
	sleValue = 7,
	status: WidgetStatus = "ready",
) => render(chartIn(items, p95, sleValue, status));

const outlierChip = () =>
	within(screen.getByTestId(`widget-shell-header-${WIDGET_KEY}`)).getByRole(
		"button",
		{ name: /outlier/i },
	);

const dialog = () => screen.getByRole("dialog");

const listedIds = (): string[] =>
	within(screen.getByRole("list", { name: "Listed Work Items" }))
		.getAllByRole("listitem")
		.map((row) => row.textContent ?? "");

const closeTheList = () =>
	fireEvent.click(screen.getByRole("button", { name: "Close the list" }));

const shownTooltip = async (): Promise<string> => {
	fireEvent.mouseOver(outlierChip());
	return (await screen.findByRole("tooltip")).textContent ?? "";
};

const expectAmberOutlined = (chip: HTMLElement) => {
	expect(
		chip,
		"The outlier chip must be an amber outlined chip (warning colour) so the reader notices it; make OutlierChip a Chip with color warning and variant outlined.",
	).toHaveClass("MuiChip-outlined", "MuiChip-colorWarning");
	expect(chip).toHaveStyle({ color: AMBER });
	expect(chip.querySelector("svg")).toHaveStyle({ color: AMBER });
};

const viewportWidth = (isPhone: boolean) =>
	vi.stubGlobal("matchMedia", (query: string) => ({
		matches: isPhone,
		media: query,
		addEventListener: () => {},
		removeEventListener: () => {},
		addListener: () => {},
		removeListener: () => {},
		onchange: null,
		dispatchEvent: () => false,
	}));

describe("The outlier chip is a plain amber button that lists the outliers", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		viewportWidth(false);
	});

	afterEach(() => {
		vi.unstubAllGlobals();
	});

	it("opens the Work Items list straight from the chip with no step in between and reports the opening once", () => {
		showChart();
		const chip = outlierChip();
		expect(chip).toHaveTextContent("1 outlier");
		expect(chip).not.toHaveAttribute("aria-haspopup");
		expect(chip).not.toHaveAttribute("aria-expanded");

		fireEvent.click(chip);

		expect(
			within(dialog()).getByText(
				"Work Items with a Cycle Time more than 5 times the median",
			),
			"One click on the chip must open the Work Items list; do not put a popover or a second button first.",
		).toBeVisible();
		expect(listedIds()).toEqual(["TZ-301"]);
		expect(screen.getAllByRole("dialog")).toHaveLength(1);
		expect(screen.queryByRole("button", { name: "Open in a list" })).toBeNull();
		expect(screen.queryByText(/Not drawn: longer than/)).toBeNull();
		expect(document.querySelector(".MuiPopover-root")).toBeNull();
		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith({ name: "OutlierListOpened" });
	});

	it("opens the list from the keyboard and hands focus back to the chip when it closes, reporting every opening", async () => {
		showChart();
		outlierChip().focus();

		fireEvent.keyDown(outlierChip(), { key: "Enter" });

		expect(listedIds()).toEqual(["TZ-301"]);
		await waitFor(() =>
			expect(dialog()).toContainElement(document.activeElement as HTMLElement),
		);

		closeTheList();

		await waitFor(() => expect(outlierChip()).toHaveFocus());
		expect(screen.queryByRole("dialog")).toBeNull();
		expect(reportUsage).toHaveBeenCalledTimes(1);

		fireEvent.click(outlierChip());

		expect(listedIds()).toEqual(["TZ-301"]);
		expect(reportUsage).toHaveBeenCalledTimes(2);
	});

	it("is amber outlined when ready, with the same text as its accessible name and its tooltip", async () => {
		showChart();

		expectAmberOutlined(outlierChip());
		expect(outlierChip()).toHaveAccessibleName(
			"1 outlier not drawn on the chart. Show the list.",
		);
		expect(await shownTooltip()).toBe(
			"1 outlier not drawn on the chart. Show the list.",
		);
	});

	it("names the lines above the axis in its accessible name and its tooltip only when there are some", async () => {
		showChart(withSecondOutlier(), 33, 35);

		expectAmberOutlined(outlierChip());
		expect(outlierChip()).toHaveAccessibleName(
			"2 outliers not drawn on the chart. Above the axis: 95% at 33 days, SLE at 35 days. Show the list.",
		);
		expect(await shownTooltip()).toBe(
			"2 outliers not drawn on the chart. Above the axis: 95% at 33 days, SLE at 35 days. Show the list.",
		);
	});

	it("stays amber at phone width with the count beside the icon", () => {
		viewportWidth(true);
		showChart();

		expectAmberOutlined(outlierChip());
		expect(outlierChip()).toHaveTextContent(/^1$/);
		expect(outlierChip()).toHaveAccessibleName(
			"1 outlier not drawn on the chart. Show the list.",
		);
	});

	it("shows a disabled amber Outliers chip with no count while loading, and opens and reports nothing when clicked", () => {
		const view = showChart();
		view.rerender(chartIn(teamZenith(), 15, 7, "loading"));

		const chip = outlierChip();
		expect(chip.matches(":disabled, [aria-disabled='true']")).toBe(true);
		expect(chip).toHaveTextContent(/^Outliers$/);
		expect(chip).not.toHaveAccessibleName(/\d/);
		expectAmberOutlined(chip);

		fireEvent.click(chip);

		expect(screen.queryByRole("dialog")).toBeNull();
		expect(reportUsage).not.toHaveBeenCalled();
	});
});
