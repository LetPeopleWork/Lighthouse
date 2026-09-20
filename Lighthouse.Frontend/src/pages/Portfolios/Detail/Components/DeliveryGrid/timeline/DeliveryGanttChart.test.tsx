import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import DeliveryGanttChart, {
	chartHeight,
	TIMELINE_SCALES,
	targetDayHighlight,
	toGanttTasks,
} from "./DeliveryGanttChart";
import type { TimelineBar } from "./deliveryTimelineModel";

// The chart paints its background grid onto a canvas, and this test environment has no 2D canvas —
// `getContext` answers null and the library dereferences it. Nothing in our own wiring is involved,
// so the drawing surface is stubbed rather than the test abandoned: everything up to the paint is
// then genuinely exercised. The painted grid itself is only ever seen in a browser.
beforeAll(() => {
	HTMLCanvasElement.prototype.getContext = vi.fn(
		() =>
			({
				translate: vi.fn(),
				beginPath: vi.fn(),
				moveTo: vi.fn(),
				lineTo: vi.fn(),
				stroke: vi.fn(),
				clearRect: vi.fn(),
				setTransform: vi.fn(),
				scale: vi.fn(),
			}) as unknown as CanvasRenderingContext2D,
	) as unknown as HTMLCanvasElement["getContext"];

	// The grid is then handed to CSS as a data URL. Left alone this prints twelve lines of
	// "not implemented" per run, which is the kind of noise a real warning hides in later.
	HTMLCanvasElement.prototype.toDataURL = vi.fn(() => "data:,");
});

/**
 * The one test that renders the real library rather than standing it in.
 *
 * Everywhere else the chart is mocked, deliberately — its markup is theirs and asserting on it
 * would go red on their release. What that cannot cover is whether our wiring mounts at all, and
 * that is a real failure mode here: one of the themes this library ships crashes its free build
 * outright, and a wrong stylesheet import leaves it reporting success while rendering nothing.
 * So this asserts mounting and theme selection, and stops short of their DOM.
 */
const bar = (id: number, name: string): TimelineBar => ({
	featureId: id,
	name,
	start: new Date(2026, 9, 10),
	end: new Date(2026, 9, 20),
	startIsObserved: false,
});

const BRAND_GREEN = "#30574e";

const renderInTheme = (mode: "light" | "dark") =>
	render(
		<ThemeProvider
			theme={createTheme({
				palette: { mode, primary: { main: BRAND_GREEN } },
			})}
		>
			<DeliveryGanttChart
				bars={[bar(1, "Deep Sea Mapping"), bar(2, "Sonar Refit")]}
				targetDate={new Date(2026, 9, 25)}
			/>
		</ThemeProvider>,
	);

describe("DeliveryGanttChart", () => {
	it.each(["light", "dark"] as const)("mounts in %s mode", (mode) => {
		renderInTheme(mode);

		expect(screen.getByTestId("delivery-gantt")).toHaveAttribute(
			"data-theme-mode",
			mode,
		);
	});

	it("paints the bars in the product's own colour, not the library's", () => {
		renderInTheme("light");

		// The library takes its colours from its own variables rather than from the MUI theme, so
		// left alone it draws a blue that appears nowhere else in this product.
		expect(screen.getByTestId("delivery-gantt")).toHaveStyle({
			"--wx-gantt-task-color": BRAND_GREEN,
		});
	});

	describe("the date axis", () => {
		const [months, days] = TIMELINE_SCALES;

		// The axis needs a measured width and so draws nothing in this environment, which is why
		// these assert the scales rather than the rendered header. They pin the defect that shipped:
		// the library calls `format` only when it is a function and otherwise prints it as it
		// stands, so the first version headed every column with the characters "MMMM yyyy".
		it.each(TIMELINE_SCALES)(
			"formats the $unit row with a function",
			(scale) => {
				expect(typeof scale.format).toBe("function");
			},
		);

		it("names the month and numbers the day", () => {
			expect(months.format(new Date(2026, 9, 15))).toBe("October 2026");
			expect(days.format(new Date(2026, 9, 15))).toBe("15");
		});
	});

	describe("translating bars into the library's tasks", () => {
		it("carries each bar's identity, name and both ends across", () => {
			const [task] = toGanttTasks([bar(7, "Sonar Refit")]);

			expect(task).toEqual({
				id: 7,
				text: "Sonar Refit",
				start: new Date(2026, 9, 10),
				end: new Date(2026, 9, 20),
				type: "task",
			});
		});

		it("keeps the order it was given, and translates nothing extra", () => {
			const tasks = toGanttTasks([bar(1, "First"), bar(2, "Second")]);

			expect(tasks.map((task) => task.text)).toEqual(["First", "Second"]);
			expect(toGanttTasks([])).toEqual([]);
		});
	});

	describe("the chart's height", () => {
		it("grows by a row per bar", () => {
			expect(chartHeight(3) - chartHeight(2)).toBe(
				chartHeight(2) - chartHeight(1),
			);
			expect(chartHeight(3)).toBeGreaterThan(chartHeight(2));
		});

		it("keeps a row's worth of height with nothing to draw", () => {
			// Otherwise an empty chart collapses onto its own axis and the tab looks broken rather
			// than empty.
			expect(chartHeight(0)).toBe(chartHeight(1));
			expect(chartHeight(0)).toBeGreaterThan(0);
		});
	});

	describe("the target tint", () => {
		const target = new Date("2026-10-15T00:00:00Z");
		const theDay = new Date(2026, 9, 15);

		it("tints the target day on the day row", () => {
			expect(targetDayHighlight(theDay, "day", target)).not.toBe("");
		});

		it("tints nothing on any other row", () => {
			// The scale calls this for the month row too, with the first day of the month. Without
			// the unit guard the whole month holding the target would be shaded instead of one day.
			expect(targetDayHighlight(theDay, "month", target)).toBe("");
		});

		it("tints nothing on another day, or with no target at all", () => {
			expect(targetDayHighlight(new Date(2026, 9, 16), "day", target)).toBe("");
			expect(targetDayHighlight(theDay, "day", undefined)).toBe("");
		});
	});

	it("mounts with nothing to draw", () => {
		render(
			<ThemeProvider theme={createTheme()}>
				<DeliveryGanttChart bars={[]} />
			</ThemeProvider>,
		);

		expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
	});
});
