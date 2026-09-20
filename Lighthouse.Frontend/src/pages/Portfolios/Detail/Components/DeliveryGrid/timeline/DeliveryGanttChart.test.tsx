import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import DeliveryGanttChart, {
	barTooltip,
	chartHeight,
	dayHighlight,
	ganttColorOverrides,
	scalesForSpan,
	THEMED_ELEMENT_SELECTOR,
	TIMELINE_SCALES,
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
				today={new Date(2026, 9, 12)}
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

	describe("the bar colour", () => {
		// This is as far as a unit test can honestly go, and the reason is worth knowing before
		// anyone strengthens it. The library declares these variables on its own theme element, so
		// ours must be aimed at that element rather than set on the wrapper around it. The first
		// version set them on the wrapper: it drew the library's default blue, and an assertion on
		// the rendered value passed anyway — this environment mocks the library's stylesheet away,
		// so the declaration that ought to have won was not there to win. Both arrangements look
		// identical to jsdom. What is pinned here is the shape; the colour on screen is the
		// screenshot test's job, and only its job.
		it("is aimed at the element that declares the variables, not the wrapper", () => {
			const overrides = ganttColorOverrides(BRAND_GREEN, "#ffffff");

			expect(Object.keys(overrides)).toEqual([THEMED_ELEMENT_SELECTOR]);
			expect(THEMED_ELEMENT_SELECTOR).toContain("wx-willow-theme");
			expect(THEMED_ELEMENT_SELECTOR).toContain("wx-willow-dark-theme");
		});

		it("carries the product's colour, and a legible label over it", () => {
			const overrides = ganttColorOverrides(BRAND_GREEN, "#ffffff")[
				THEMED_ELEMENT_SELECTOR
			];

			expect(overrides["--wx-gantt-task-color"]).toBe(BRAND_GREEN);
			expect(overrides["--wx-gantt-task-fill-color"]).toBe(BRAND_GREEN);
			expect(overrides["--wx-gantt-task-border-color"]).toBe(BRAND_GREEN);
			expect(overrides["--wx-gantt-task-font-color"]).toBe("#ffffff");
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

	describe("fitting the axis to the delivery's length", () => {
		const spanOf = (days: number) =>
			scalesForSpan(new Date(2026, 0, 1), new Date(2026, 0, 1 + days));

		const finestUnit = (scales: { unit: string }[]) =>
			scales[scales.length - 1].unit;

		it("rules a short delivery by the day", () => {
			expect(finestUnit(spanOf(10))).toBe("day");
		});

		it("is already off days by the time a delivery runs a month", () => {
			// The first version of this kept day columns up to two months and the chart scrolled
			// at three weeks — each column is a fixed width, so a fortnight of them is about all
			// a panel holds. Pinned because the mistake was an order of magnitude, not a nudge.
			expect(finestUnit(spanOf(30))).not.toBe("day");
			expect(finestUnit(spanOf(30))).toBe("week");
		});

		it("coarsens as the delivery gets longer, never the other way", () => {
			// The point is not the exact thresholds, which are a judgement and will move. It is
			// that a longer delivery never gets a finer axis than a shorter one — that is what
			// keeps it on screen instead of behind a scrollbar.
			const order = ["day", "week", "month"];
			const spans = [10, 45, 90, 200, 400, 900];

			const coarseness = spans.map((days) =>
				order.indexOf(finestUnit(spanOf(days))),
			);

			expect(coarseness).toEqual([...coarseness].sort((a, b) => a - b));
			expect(new Set(coarseness).size).toBeGreaterThan(1);
		});

		it("rules a multi-year delivery by the month", () => {
			expect(finestUnit(spanOf(900))).toBe("month");
		});

		it("formats every row of every scale with a function", () => {
			// Same trap as the default scales: a format given as a string is printed verbatim.
			for (const days of [10, 90, 900]) {
				for (const scale of spanOf(days)) {
					expect(typeof scale.format).toBe("function");
				}
			}
		});
	});

	describe("what a bar says on hover", () => {
		it("gives the span, and offers the detail behind it", () => {
			expect(barTooltip(bar(1, "Sonar Refit"), true)).toBe(
				"10/10/2026 – 10/20/2026 (click for more details)",
			);
		});

		it("does not offer detail there is no way to reach", () => {
			// Nothing is listening for a click, so promising one would be a lie the reader only
			// discovers by trying it.
			expect(barTooltip(bar(1, "Sonar Refit"), false)).toBe(
				"10/10/2026 – 10/20/2026",
			);
		});

		it("leaves the name to the bar it is already written on", () => {
			expect(barTooltip(bar(1, "Sonar Refit"), true)).not.toContain(
				"Sonar Refit",
			);
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

	describe("marking the target day and today", () => {
		const targetDate = new Date("2026-10-15T00:00:00Z");
		const today = new Date(2026, 9, 2);
		const marks = { targetDate, today };

		it("marks each of the two days, and marks them differently", () => {
			const onTarget = dayHighlight(new Date(2026, 9, 15), "day", marks);
			const onToday = dayHighlight(new Date(2026, 9, 2), "day", marks);

			expect(onTarget).not.toBe("");
			expect(onToday).not.toBe("");
			// Two marks that rendered identically would need a legend to tell apart, which is worse
			// than not marking the second one.
			expect(onTarget).not.toBe(onToday);
		});

		it("says both when the Delivery is due today", () => {
			const dueToday = new Date(2026, 9, 15);
			const classes = dayHighlight(dueToday, "day", {
				targetDate,
				today: dueToday,
			}).split(" ");

			expect(classes).toHaveLength(2);
			expect(new Set(classes).size).toBe(2);
		});

		it("marks nothing on any row but the day row", () => {
			// The scale calls this for the month row too, with the first day of the month. Without
			// the unit guard the whole month holding a marked day would be shaded instead of it.
			expect(dayHighlight(new Date(2026, 9, 15), "month", marks)).toBe("");
			expect(dayHighlight(new Date(2026, 9, 2), "month", marks)).toBe("");
		});

		it("marks nothing on an ordinary day, or when neither date is given", () => {
			expect(dayHighlight(new Date(2026, 9, 8), "day", marks)).toBe("");
			expect(dayHighlight(new Date(2026, 9, 15), "day", {})).toBe("");
		});
	});

	it("mounts with nothing to draw", () => {
		render(
			<ThemeProvider theme={createTheme()}>
				<DeliveryGanttChart bars={[]} today={new Date(2026, 9, 12)} />
			</ThemeProvider>,
		);

		expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
	});
});
