import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ICycleTimeOutliers } from "../../../models/Metrics/CycleTimeOutliers";
import type { IWorkItem } from "../../../models/WorkItem";
import WidgetShell from "../../../pages/Common/MetricsView/WidgetShell";
import { testTheme } from "../../../tests/testTheme";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";

// The reader has renamed Cycle Time to Lead Time under Settings, Terminology.
vi.mock("../../../services/TerminologyContext", async (original) => ({
	...(await original<typeof import("../../../services/TerminologyContext")>()),
	useTerminology: () => ({
		getTerm: (key: string) =>
			({ workItems: "Work Items", cycleTime: "Lead Time", sle: "SLE" })[key] ??
			key,
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

// The list shows the Work Items it was handed, in the order it was handed them.
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: ({ items, open }: { items: IWorkItem[]; open: boolean }) =>
		open ? (
			<ul aria-label="Listed Work Items">
				{items.map((item) => (
					<li key={item.id}>{item.referenceId}</li>
				))}
			</ul>
		) : null,
}));

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

const closedItem = (id: number, cycleTime: number): IWorkItem => ({
	id,
	referenceId: `TZ-${id}`,
	name: `TZ-${id}`,
	url: null,
	cycleTime,
	startedDate: new Date(2026, 1, 1),
	closedDate: new Date(2026, 3, id),
	workItemAge: cycleTime,
	state: "Done",
	stateCategory: "Done",
	type: "Task",
	parentWorkItemReference: "",
	isBlocked: false,
});

type Judge = (definitionId: number | null) => Promise<ICycleTimeOutliers>;

const backendWithoutLimits: Judge = () =>
	Promise.resolve({
		status: "NoLimits",
		upperNaturalProcessLimit: null,
		outlierWorkItemIds: [],
	});

const backendNaming =
	(...ids: number[]): Judge =>
	() =>
		Promise.resolve({
			status: "Ready",
			upperNaturalProcessLimit: 30,
			outlierWorkItemIds: ids,
		});

interface Question {
	judge?: Judge;
	ownerId?: number;
	startDate?: Date;
	endDate?: Date;
	items?: IWorkItem[];
}

const freshClient = () =>
	new QueryClient({ defaultOptions: { queries: { retry: false } } });

const chartFor = (client: QueryClient, question: Question) => (
	<QueryClientProvider client={client}>
		<WidgetShell
			widgetKey={WIDGET_KEY}
			title="Lead Time scatterplot"
			status="ready"
			hasContentToDim
		>
			<CycleTimeScatterPlotChart
				ownerKind="team"
				percentileValues={[]}
				cycleTimeDataPoints={
					question.items ?? [closedItem(1, 3), closedItem(2, 40)]
				}
				serviceLevelExpectation={null}
				namedCycleTimeDefinitions={[
					{ id: 11, name: "Review time", endState: "Done" },
					{ id: 12, name: "Test time", endState: "Done" },
				]}
				onFetchNamedCycleTimePercentiles={() => Promise.resolve([])}
				onFetchCycleTimeOutliers={question.judge}
				ownerId={question.ownerId}
				startDate={question.startDate}
				endDate={question.endDate}
			/>
		</WidgetShell>
	</QueryClientProvider>
);

const header = () => screen.getByTestId(`widget-shell-header-${WIDGET_KEY}`);
const settled = () => new Promise((resolve) => setTimeout(resolve, 0));

describe("CycleTimeScatterPlotChart asks the backend the question the reader sees", () => {
	it("names the reader's Cycle Time term in the no-limits text", async () => {
		render(chartFor(freshClient(), { judge: backendWithoutLimits }));

		expect(
			await within(header()).findByText(
				"No outliers: the Lead Time process behaviour chart has no limits for this date range",
			),
		).toBeInTheDocument();
		expect(within(header()).queryByText(/Cycle Time/)).not.toBeInTheDocument();
	});

	it("is ready at once, with no outlier chip, when the view hands over no way to ask", () => {
		render(chartFor(freshClient(), {}));

		expect(screen.getByTestId(`widget-shell-${WIDGET_KEY}`)).toHaveAttribute(
			"data-widget-status",
			"ready",
		);
		expect(
			within(header()).queryByRole("button", { name: /outlier/i }),
		).not.toBeInTheDocument();
	});

	it("asks again when the owner, the end or the start of the range changes", async () => {
		const client = freshClient();
		const judge = vi.fn(backendNaming());
		const first: Question = {
			judge,
			ownerId: 7,
			startDate: new Date(2026, 0, 1),
			endDate: new Date(2026, 2, 31),
		};
		const { rerender } = render(chartFor(client, first));
		await settled();
		expect(judge).toHaveBeenCalledTimes(1);

		rerender(chartFor(client, { ...first, ownerId: 8 }));
		await settled();
		expect(judge).toHaveBeenCalledTimes(2);

		rerender(
			chartFor(client, {
				...first,
				ownerId: 8,
				endDate: new Date(2026, 3, 30),
			}),
		);
		await settled();
		expect(judge).toHaveBeenCalledTimes(3);

		rerender(
			chartFor(client, {
				...first,
				ownerId: 8,
				endDate: new Date(2026, 3, 30),
				startDate: new Date(2026, 1, 1),
			}),
		);
		await settled();
		expect(judge).toHaveBeenCalledTimes(4);
	});

	it("asks for each named Cycle Time the reader picks", async () => {
		const judge = vi.fn(backendNaming());
		render(chartFor(freshClient(), { judge }));

		const pick = async (name: string) => {
			fireEvent.mouseDown(screen.getByRole("combobox", { name: "Lead Time" }));
			fireEvent.click(await screen.findByRole("option", { name }));
			await settled();
		};
		await pick("Review time");
		await pick("Test time");

		expect(judge.mock.calls.map(([definitionId]) => definitionId)).toEqual([
			null,
			11,
			12,
		]);
	});

	it("lists the outliers longest first, whatever order they were plotted in", async () => {
		const items = [
			closedItem(1, 3),
			closedItem(2, 35),
			closedItem(3, 50),
			closedItem(4, 41),
		];
		render(chartFor(freshClient(), { judge: backendNaming(2, 3, 4), items }));

		fireEvent.click(
			await within(header()).findByRole("button", {
				name: /^3 outliers not drawn/,
			}),
		);

		expect(
			within(screen.getByRole("list", { name: "Listed Work Items" }))
				.getAllByRole("listitem")
				.map((row) => row.textContent),
		).toEqual(["TZ-3", "TZ-4", "TZ-2"]);
	});
});
