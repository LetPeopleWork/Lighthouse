import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IWorkItem } from "../../../models/WorkItem";
import WidgetShell from "../../../pages/Common/MetricsView/WidgetShell";
import { getWidgetInfo } from "../../../pages/Common/MetricsView/widgetInfoMetadata";
import { testTheme } from "../../../tests/testTheme";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";

const { terms } = vi.hoisted(() => ({
	terms: {
		workItems: "Work Items",
		cycleTime: "Cycle Time",
		sle: "SLE",
	} as Record<string, string>,
}));

vi.mock("../../../services/TerminologyContext", async (original) => ({
	...(await original<typeof import("../../../services/TerminologyContext")>()),
	useTerminology: () => ({
		getTerm: (key: string) => terms[key] ?? key,
	}),
}));

vi.mock("../../../services/UsageData/usageDataReporter", async (original) => ({
	...(await original<
		typeof import("../../../services/UsageData/usageDataReporter")
	>()),
	useUsageDataReporter: () => vi.fn(),
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

const closedItem = (
	referenceId: string,
	cycleTime: number,
	day: number,
	type = "Task",
): IWorkItem => ({
	id: Number(referenceId.replace(/\D/g, "")),
	referenceId,
	name: referenceId,
	url: null,
	cycleTime,
	startedDate: new Date(2026, 1, 1),
	closedDate: new Date(2026, 3, day),
	workItemAge: cycleTime,
	state: "Done",
	stateCategory: "Done",
	type,
	parentWorkItemReference: "",
	isBlocked: false,
});

// 25 typical items put the median at 6 days, so the cut is 30 days.
const typicalItems = (): IWorkItem[] =>
	Array.from({ length: 25 }, (_, index) =>
		closedItem(`TZ-${200 + index}`, 6, 1 + (index % 28)),
	);

// 31 to 41 days: TZ-300 (31 days) to TZ-310 (41 days).
const elevenOutliers = (type = "Task"): IWorkItem[] =>
	Array.from({ length: 11 }, (_, index) =>
		closedItem(`TZ-${300 + index}`, 31 + index, 12, type),
	);

const showChart = (items: IWorkItem[]) =>
	render(
		<WidgetShell
			widgetKey={WIDGET_KEY}
			title="Cycle Time scatterplot"
			status="ready"
			hasContentToDim
		>
			<CycleTimeScatterPlotChart
				ownerKind="team"
				percentileValues={[]}
				cycleTimeDataPoints={items}
				serviceLevelExpectation={null}
			/>
		</WidgetShell>,
	);

const outlierChip = () =>
	within(screen.getByTestId(`widget-shell-header-${WIDGET_KEY}`)).getByRole(
		"button",
		{ name: /outlier/i },
	);

const openTheList = () => fireEvent.click(outlierChip());

const listedIds = (): string[] =>
	within(screen.getByRole("list", { name: "Listed Work Items" }))
		.getAllByRole("listitem")
		.map((row) => row.textContent ?? "")
		.sort();

const dialog = () => screen.getByRole("dialog");

describe("Cycle Time scatterplot opens the outliers in the Work Items list", () => {
	beforeEach(() => {
		terms.workItems = "Work Items";
		terms.cycleTime = "Cycle Time";
		terms.sle = "SLE";
	});

	it("lists every outlier straight from the chip and hands focus back to the chip afterwards", async () => {
		showChart([...typicalItems(), ...elevenOutliers()]);

		openTheList();

		expect(screen.queryByText(/Open in a list|and 1 more/)).toBeNull();
		expect(screen.queryByText(/Not drawn: longer than/)).toBeNull();
		expect(
			within(dialog()).getByText(
				"Work Items with a Cycle Time more than 5 times the median",
			),
		).toBeVisible();
		expect(listedIds()).toEqual(
			Array.from({ length: 11 }, (_, index) => `TZ-${300 + index}`),
		);
		await waitFor(() =>
			expect(dialog()).toContainElement(document.activeElement as HTMLElement),
		);

		fireEvent.click(screen.getByRole("button", { name: "Close the list" }));

		await waitFor(() => expect(outlierChip()).toHaveFocus());
		expect(screen.queryByRole("dialog")).toBeNull();
	});

	it("titles the list in the reader's own words for Work Items and Cycle Time", () => {
		terms.workItems = "Tasks";
		terms.cycleTime = "Lead Time";
		showChart([...typicalItems(), ...elevenOutliers().slice(0, 2)]);

		openTheList();

		expect(
			within(dialog()).getByText(
				"Tasks with a Lead Time more than 5 times the median",
			),
		).toBeVisible();
	});

	it("lists only the outliers of the Work Item types the reader still shows", () => {
		const bug = closedItem("TZ-301", 58, 12, "Bug");
		const story = closedItem("TZ-302", 40, 11, "Story");
		showChart([
			...typicalItems().map((item) => ({ ...item, type: "Task" })),
			bug,
			story,
		]);
		fireEvent.click(
			screen.getByRole("button", { name: "Story visibility toggle" }),
		);

		openTheList();

		expect(listedIds()).toEqual(["TZ-301"]);
	});
});

describe("Cycle Time scatterplot help names the outlier rule", () => {
	const renamed = {
		feature: "Bet",
		features: "Bets",
		team: "Squad",
		workItems: "Tasks",
		cycleTime: "Lead Time",
	};

	it.each(["team", "portfolio"] as const)(
		"on a %s the help states the rule in the reader's terms and keeps 'outlier' fixed",
		(kind) => {
			const help = getWidgetInfo(WIDGET_KEY, { kind, terms: renamed });

			expect(help?.description).toMatch(
				/An outlier is a closed .+ whose Lead Time is more than 5 times the median Lead Time of all closed Tasks in this date range\./,
			);
			expect(help?.description).not.toMatch(/Work Items?|Cycle Time/);
		},
	);
});
