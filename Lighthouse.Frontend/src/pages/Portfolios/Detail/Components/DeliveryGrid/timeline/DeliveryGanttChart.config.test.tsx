import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DeliveryGanttChart from "./DeliveryGanttChart";
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
});

const renderChart = (props: Partial<{ targetDate: Date }> = {}) =>
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

	it("hands over one task per bar, and no dependency links", () => {
		renderChart();

		expect(ganttConfig.current?.tasks).toHaveLength(1);
		// Dependency arrows are a later slice. An accidental non-empty list here would draw
		// something nobody asked for.
		expect(ganttConfig.current?.links).toEqual([]);
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
});
