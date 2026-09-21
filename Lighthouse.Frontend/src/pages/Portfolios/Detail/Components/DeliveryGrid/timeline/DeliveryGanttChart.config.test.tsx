import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DeliveryGanttChart, {
	type DeliveryGanttChartProps,
} from "./DeliveryGanttChart";
import type { TeamLane } from "./deliveryTeamLanes";
import type { TimelineBar } from "./deliveryTimelineModel";

/**
 * What the adapter hands the library, asserted by standing the library in.
 *
 * Several of these props are decisions with real consequences that leave no trace anywhere else.
 * `autoScale` is the clearest: it defaults on, and when it is on the library recomputes the axis
 * from the tasks and throws away the range we passed — which silently drops a target date falling
 * beyond the last bar. That shipped once. Nothing but this test would notice it coming back.
 *
 * This is the contract we own. The markup the library then produces is theirs, and no test here
 * reaches into it.
 */
const ganttConfig = vi.hoisted(() => ({
	current: null as Record<string, unknown> | null,
}));

vi.mock("@svar-ui/react-gantt", () => ({
	Gantt: (props: Record<string, unknown>) => {
		ganttConfig.current = props;
		return <div data-testid="stand-in-gantt" />;
	},
	Willow: ({ children }: { children?: React.ReactNode }) => <>{children}</>,
	WillowDark: ({ children }: { children?: React.ReactNode }) => <>{children}</>,
}));

const bar = (id: number, name: string): TimelineBar => ({
	featureId: id,
	name,
	start: new Date(2026, 9, 10),
	end: new Date(2026, 9, 20),
	startIsObserved: false,
	endIsObserved: false,
});

const lane = (featureId: number, teamId: number): TeamLane => ({
	featureId,
	teamId,
	teamName: "Zenith",
	start: new Date(2026, 9, 12),
	end: new Date(2026, 9, 18),
	color: "#4DA98C",
});

const renderChart = (props: Partial<DeliveryGanttChartProps> = {}) =>
	render(
		<ThemeProvider theme={createTheme()}>
			<DeliveryGanttChart
				bars={[bar(1, "Deep Sea Mapping")]}
				today={new Date(2026, 9, 12)}
				{...props}
			/>
		</ThemeProvider>,
	);

beforeEach(() => {
	ganttConfig.current = null;
});

describe("what the adapter configures the library with", () => {
	it("keeps the axis range it was given instead of letting the library recompute it", () => {
		// The whole reason the target date is visible at all. With autoScale on, the range is
		// derived from the tasks and a target beyond the last bar falls off the axis.
		renderChart({ targetDate: new Date(2026, 10, 20) });

		expect(ganttConfig.current?.autoScale).toBe(false);

		const start = ganttConfig.current?.start as Date;
		const end = ganttConfig.current?.end as Date;

		expect(start.getTime()).toBeLessThan(new Date(2026, 9, 10).getTime());
		expect(end.getTime()).toBeGreaterThan(new Date(2026, 10, 20).getTime());
	});

	it("is read-only, and carries no task-name pane", () => {
		renderChart();

		// The library is an editing-first Gantt. This view is a read, so its editing is switched
		// off at the boundary rather than relied upon to go unused.
		expect(ganttConfig.current?.readonly).toBe(true);
		expect(ganttConfig.current?.columns).toBe(false);
	});

	it("hands over one task per bar, and draws nothing between them unasked", () => {
		renderChart();

		expect(ganttConfig.current?.tasks).toHaveLength(1);
		// A Delivery whose Features wait on nothing has nothing to join. An invented line here would
		// assert a wait that the forecast never made.
		expect(ganttConfig.current?.links).toEqual([]);
	});

	it("passes on the dependencies it was given rather than an empty list", () => {
		// The prop existing is not the same as the prop arriving: the chart passed a hardcoded empty
		// array to the library for two slices, and every assertion above it stayed green throughout.
		renderChart({
			bars: [bar(1, "Deep Sea Mapping"), bar(2, "Sonar Refit")],
			links: [{ blockerFeatureId: 1, waitingFeatureId: 2 }],
		});

		const links = ganttConfig.current?.links as {
			source: number;
			target: number;
		}[];

		expect(links).toHaveLength(1);
		expect(links[0].source).toBe(1);
		expect(links[0].target).toBe(2);
	});

	it("reports the axis resolution it settled on", () => {
		renderChart();

		// Carried on our own wrapper, and the same value keys the chart so a change of
		// resolution remounts it. The library reads `scales` once at init and ignores later
		// changes, so without that the axis silently keeps the resolution it opened with.
		// Whether the remount actually redraws is a browser's answer, not this environment's.
		const scales = ganttConfig.current?.scales as { unit: string }[];
		const finest = scales[scales.length - 1].unit;

		// Tied to the scales actually handed over rather than to a literal, so the two cannot
		// drift: the attribute is what keys the remount, and a value that disagreed with the
		// scales would remount on the wrong changes and not on the right ones.
		expect(screen.getByTestId("delivery-gantt")).toHaveAttribute(
			"data-axis-unit",
			finest,
		);
	});

	it("supplies its own axis formatting and its own bar content", () => {
		renderChart();

		expect(typeof ganttConfig.current?.taskTemplate).toBe("function");
		expect(typeof ganttConfig.current?.highlightTime).toBe("function");
		expect(ganttConfig.current?.scales).toBeInstanceOf(Array);
	});

	describe("the content it draws inside each row the library asks about", () => {
		// The library calls this back for every row, handing it the row's id and nothing else, so
		// everything the adapter decides about a row is decided in here. It is exercised by calling
		// it the way the library does rather than through a stub that re-does its work: a stub that
		// looked the Team up itself would pass whatever this callback did or did not do.
		const drawRow = (id: string | number) => {
			const Template = ganttConfig.current?.taskTemplate as React.FC<{
				data: { id?: string | number };
			}>;

			return render(
				<ThemeProvider theme={createTheme()}>
					<Template data={{ id }} />
				</ThemeProvider>,
			);
		};

		it("writes a Team's name along that Team's own row", () => {
			renderChart({
				bars: [bar(1, "Coral Reef")],
				lanes: [lane(1, 5)],
			});

			// The id is the one the translation mints for that row, so this fails both on a row
			// whose content never reaches the template and on an id the lookup cannot resolve.
			const { container } = drawRow("1:5");

			expect(container).toHaveTextContent("Zenith");
			expect(container).not.toHaveTextContent("Coral Reef");
		});

		it("writes the one Team a Feature has to itself along the Feature's own bar", () => {
			renderChart({
				bars: [bar(1, "Coral Reef")],
				barTeams: new Map([
					[1, { teamId: 5, teamName: "Zenith", color: "#4DA98C" }],
				]),
			});

			const { container } = drawRow(1);

			expect(container).toHaveTextContent("Coral Reef");
			expect(container).toHaveTextContent("Zenith");
		});

		it("leaves a Feature with no Team of its own carrying only its own name", () => {
			// Paired with the case above, so neither can pass against a template that draws the
			// Feature's name and nothing else whatever it is handed.
			renderChart({ bars: [bar(1, "Coral Reef")] });

			const { container } = drawRow(1);

			expect(container).toHaveTextContent("Coral Reef");
			expect(container).not.toHaveTextContent("Zenith");
		});
	});

	it("hands over a task per lane as well as per bar", () => {
		renderChart({ lanes: [lane(1, 5)] });

		expect(ganttConfig.current?.tasks).toHaveLength(2);
	});

	it("sizes itself for every row it draws, not for the Features alone", () => {
		// A height taken from the bars leaves the last Feature's Teams drawn outside the box. The
		// height itself is a style nothing here resolves, so the row count it was computed from is
		// carried on the element instead - and asserted against the tasks actually handed over, so
		// the two cannot drift apart.
		renderChart({
			bars: [bar(1, "Coral Reef"), bar(2, "Kelp Forest")],
			lanes: [lane(1, 5), lane(1, 6)],
		});

		const tasks = ganttConfig.current?.tasks as unknown[];

		expect(screen.getByTestId("delivery-gantt")).toHaveAttribute(
			"data-row-count",
			String(tasks.length),
		);
		expect(tasks).toHaveLength(4);
	});

	it("draws the axis around the lanes as well, not only around the bars", () => {
		// The window function widening its parameter is the cheap half and it fixes nothing on its
		// own: the type widens for free and the behaviour does not. Until this call site is handed
		// the lanes, a lane reaching past every bar is clipped off the axis in complete silence —
		// no error, no gap, and nothing drawn in this environment to make the absence visible. So
		// the check has to be made where the window and the drawing can disagree, which is here.
		const reachesPastEveryBar = {
			...lane(1, 5),
			end: new Date(2026, 10, 20),
		};

		renderChart({ lanes: [reachesPastEveryBar] });

		const axisEnd = ganttConfig.current?.end as Date;

		expect(axisEnd.getTime()).toBeGreaterThan(
			reachesPastEveryBar.end.getTime(),
		);
	});
});
