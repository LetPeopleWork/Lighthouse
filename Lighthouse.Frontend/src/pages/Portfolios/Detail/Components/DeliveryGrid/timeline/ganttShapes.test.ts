import { describe, expect, it } from "vitest";
import type { TeamLane } from "./deliveryTeamLanes";
import type { TimelineBar } from "./deliveryTimelineModel";
import {
	barTooltip,
	chartHeight,
	columnHighlight,
	ganttColorOverrides,
	scalesForSpan,
	THEMED_ELEMENT_SELECTOR,
	TIMELINE_SCALES,
	taskContentLookup,
	toGanttLinks,
	toGanttTasks,
} from "./ganttShapes";

// Every decision the chart adapter makes, tested without loading the chart. That is the point of
// the split: these are arithmetic and string-building, and pulling the library in to reach them
// costs a stubbed canvas and an axis that never renders.

const bar = (id: number, name: string): TimelineBar => ({
	featureId: id,
	name,
	start: new Date(2026, 9, 10),
	end: new Date(2026, 9, 20),
	startIsObserved: false,
});

const BRAND_GREEN = "#30574e";

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
		const [selector, ...others] = Object.keys(
			ganttColorOverrides(BRAND_GREEN, "#ffffff"),
		);

		// Asserted on the key the function actually returned. Reading these off the imported
		// constant instead would be checking the test's own input — the exact shape of
		// not-really-asserting that has already cost this slice three defects.
		expect(others).toEqual([]);
		expect(selector).toBe(THEMED_ELEMENT_SELECTOR);
		expect(selector).toContain("wx-willow-theme");
		expect(selector).toContain("wx-willow-dark-theme");
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
describe("fitting the axis to the delivery's length and the space it has", () => {
	// A laptop-ish panel unless a test is about width, in which case it says so.
	const WIDE = 1600;
	const NARROW = 400;

	const spanOf = (days: number, width = WIDE) =>
		scalesForSpan(new Date(2026, 0, 1), new Date(2026, 0, 1 + days), width);

	const finestUnit = (scales: { unit: string }[]) =>
		scales[scales.length - 1].unit;

	it("rules a short delivery by the day", () => {
		expect(finestUnit(spanOf(10))).toBe("day");
	});

	it("is off days well before a delivery runs a month", () => {
		// The first version kept day columns up to two months and the chart scrolled at three
		// weeks, because a column has a floor on how narrow it can get. Pinned because the
		// mistake was an order of magnitude, not a nudge.
		expect(finestUnit(spanOf(30))).not.toBe("day");
	});

	it("rules the same delivery more coarsely on a narrower panel", () => {
		// The whole reason the width is measured rather than assumed: resizing the window has
		// to re-rule the axis, or a chart that fitted a moment ago starts scrolling.
		// Ten days is sixteen columns' worth of room on the wide panel and four on the narrow one.
		const days = 10;

		expect(finestUnit(spanOf(days, WIDE))).toBe("day");
		expect(finestUnit(spanOf(days, NARROW))).not.toBe("day");
	});

	it("never rules a longer delivery more finely than a shorter one", () => {
		// The thresholds are a judgement and will move; this is the property that must not.
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

	it("falls back to a sensible axis before the panel has been measured", () => {
		// A panel reports zero width until it is laid out. Taken at face value that is "no
		// columns fit", and every Delivery would open ruled by months and re-rule a frame later.
		expect(finestUnit(spanOf(10, 0))).toBe("day");
	});

	it("formats every row of every scale with a function, and each one returns a date", () => {
		// Same trap as the default scales: a format given as a string is printed verbatim.
		// Checking only that it IS a function leaves the coarser scales' formatters never once
		// called, so a broken one would go unnoticed until someone opened a long Delivery.
		const day = new Date(2026, 2, 7);

		for (const days of [10, 90, 900]) {
			for (const scale of spanOf(days)) {
				expect(typeof scale.format).toBe("function");

				const rendered = scale.format(day);

				expect(rendered).not.toBe("");
				// Neither a leftover pattern nor a raw Date stringified by accident.
				expect(rendered).not.toMatch(/MMM|yyyy|GMT/);
			}
		}
	});

	it("labels the coarser axes with the month and the year they show", () => {
		const day = new Date(2026, 2, 7);
		const weekly = spanOf(60);
		const monthly = spanOf(900);

		expect(finestUnit(weekly)).toBe("week");
		expect(weekly[0].format(day)).toBe("March 2026");
		expect(weekly[1].format(day)).toBe("3/7");

		expect(monthly[0].format(day)).toBe("2026");
		expect(monthly[1].format(day)).toBe("Mar 2026");
	});

	it("coarsens one column past what fits, and not before", () => {
		// The boundary itself, because `<=` and `<` are one character apart and the difference
		// is a column of overflow. At 96px a column, 960px holds ten.
		expect(finestUnit(spanOf(10, 960))).toBe("day");
		expect(finestUnit(spanOf(11, 960))).toBe("week");
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

	const dayColumn = (day: number) => new Date(2026, 9, day);

	it("marks each of the two days, and marks them differently", () => {
		const onTarget = columnHighlight(dayColumn(15), "day", "day", marks);
		const onToday = columnHighlight(dayColumn(2), "day", "day", marks);

		expect(onTarget).not.toBe("");
		expect(onToday).not.toBe("");
		// Two marks that rendered identically would need a legend to tell apart, which is worse
		// than not marking the second one.
		expect(onTarget).not.toBe(onToday);
	});

	it("says both when the Delivery is due today", () => {
		const dueToday = dayColumn(15);
		const classes = columnHighlight(dueToday, "day", "day", {
			targetDate,
			today: dueToday,
		}).split(" ");

		expect(classes).toHaveLength(2);
		expect(new Set(classes).size).toBe(2);
	});

	it("marks the week that contains each date, when the axis is weekly", () => {
		// The defect this replaces: the first version answered only for day columns, so the
		// moment the axis coarsened — which is the ordinary case for any Delivery past a
		// fortnight — both markers vanished and the legend pointed at nothing.
		expect(columnHighlight(dayColumn(12), "week", "week", marks)).toContain(
			"target",
		);
		// Today is 2 October, so the week that holds it is the one opening 28 September.
		expect(
			columnHighlight(new Date(2026, 8, 28), "week", "week", marks),
		).toContain("today");
	});

	it("marks the month that contains each date, when the axis is monthly", () => {
		const september = new Date(2026, 8, 1);
		const october = new Date(2026, 9, 1);

		expect(columnHighlight(october, "month", "month", marks)).toContain(
			"target",
		);
		expect(columnHighlight(september, "month", "month", marks)).toBe("");
	});

	it("leaves the neighbouring column alone at every resolution", () => {
		// Containment has two edges and only one of them is obvious. A week column starting the
		// day after the target must not claim it.
		expect(columnHighlight(dayColumn(16), "week", "week", marks)).not.toContain(
			"target",
		);
		expect(columnHighlight(dayColumn(8), "week", "week", marks)).not.toContain(
			"target",
		);
		expect(columnHighlight(dayColumn(15), "week", "week", marks)).toContain(
			"target",
		);
	});

	it("marks only the finest row of the axis", () => {
		// The scale has two rows. Marking the coarser one as well would put a band across the
		// whole month or year holding the date.
		expect(columnHighlight(dayColumn(15), "month", "day", marks)).toBe("");
		// The monthly scale's upper row is years, and it must stay unmarked — otherwise a whole
		// year is shaded because one day inside it is the target.
		expect(columnHighlight(dayColumn(15), "year", "month", marks)).toBe("");
	});

	it("marks nothing on an ordinary column, or when neither date is given", () => {
		expect(columnHighlight(dayColumn(8), "day", "day", marks)).toBe("");
		expect(columnHighlight(dayColumn(15), "day", "day", {})).toBe("");
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
describe("translating drawn dependencies into the library's links", () => {
	it("runs the line from the blocker's task to the one waiting on it", () => {
		const [link] = toGanttLinks([{ blockerFeatureId: 7, waitingFeatureId: 9 }]);

		// Asserted on the object handed over, and deliberately not on what gets drawn from it. The
		// two ends are the whole meaning of the line — swapped, it reads as the blocker waiting on
		// the Feature it blocks, which is a plausible-looking picture of the opposite plan.
		expect(link.source).toBe(7);
		expect(link.target).toBe(9);
		expect(link.id).toBeTruthy();
	});

	it("gives each line its own id when two Features wait on the same blocker", () => {
		// Two waiters on one blocker is the ordinary shape here, and ids that collide are resolved by
		// the library drawing a single line — so one of the two waits stops being visible.
		const links = toGanttLinks([
			{ blockerFeatureId: 7, waitingFeatureId: 9 },
			{ blockerFeatureId: 7, waitingFeatureId: 11 },
		]);

		expect(links).toHaveLength(2);
		expect(new Set(links.map((link) => link.id)).size).toBe(2);
	});

	it("keeps the order it was given, and translates nothing extra", () => {
		const links = toGanttLinks([
			{ blockerFeatureId: 1, waitingFeatureId: 2 },
			{ blockerFeatureId: 3, waitingFeatureId: 4 },
		]);

		expect(links.map((link) => link.source)).toEqual([1, 3]);
		expect(toGanttLinks([])).toEqual([]);
	});
});
describe("the date axis", () => {
	const [months, days] = TIMELINE_SCALES;

	// The axis needs a measured width and so draws nothing in this environment, which is why
	// these assert the scales rather than the rendered header. They pin the defect that shipped:
	// the library calls `format` only when it is a function and otherwise prints it as it
	// stands, so the first version headed every column with the characters "MMMM yyyy".
	it.each(TIMELINE_SCALES)("formats the $unit row with a function", (scale) => {
		expect(typeof scale.format).toBe("function");
	});

	it("names the month and numbers the day", () => {
		expect(months.format(new Date(2026, 9, 15))).toBe("October 2026");
		expect(days.format(new Date(2026, 9, 15))).toBe("15");
	});
});
describe("interleaving a Team's lanes with the Features they belong to", () => {
	const lane = (
		featureId: number,
		teamId: number,
		teamName: string,
	): TeamLane => ({
		featureId,
		teamId,
		teamName,
		start: new Date(2026, 9, 12),
		end: new Date(2026, 9, 18),
		color: "#4DA98C",
	});

	const board = [bar(7, "Coral Reef"), bar(3, "Kelp Forest")];

	it("hands over the shipped list when no lanes are supplied", () => {
		expect(toGanttTasks(board)).toEqual(toGanttTasks(board, []));
	});

	it("leaves every Feature's own task exactly as it was without the lanes", () => {
		// The slice's central guarantee, and it needs no drawing surface. Both halves in one
		// assertion: the lanes must actually be there, or this passes against a function that
		// ignores its second argument — which is what the shipped one does.
		const withLanes = toGanttTasks(board, [lane(7, 5, "Zenith")]);
		const withoutLanes = toGanttTasks(board);

		expect(withLanes).toHaveLength(withoutLanes.length + 1);
		expect(withLanes.filter((task) => typeof task.id === "number")).toEqual(
			withoutLanes,
		);
	});

	it("puts each lane immediately after the Feature it belongs to, in the board's order", () => {
		// The board's order is neither alphabetical nor chronological here, so any re-sort fails,
		// and a lane appended at the end rather than interleaved fails too.
		const tasks = toGanttTasks(board, [
			lane(3, 6, "Gravity"),
			lane(7, 5, "Zenith"),
		]);

		expect(tasks.map((task) => task.text)).toEqual([
			"Coral Reef",
			"Zenith",
			"Kelp Forest",
			"Gravity",
		]);
	});

	it("hands over ordinary tasks and not one word of the library's hierarchy", () => {
		// Asserted with the lanes present and typed, so it cannot pass against today's function.
		// `parent`, `open` and `type: "summary"` are vendor vocabulary this slice promised not to
		// widen by one word — and a value the library does not recognise draws nothing, silently.
		const tasks = toGanttTasks(board, [lane(7, 5, "Zenith")]);

		expect(tasks).toHaveLength(3);

		for (const task of tasks) {
			expect(task.type).toBe("task");
			expect(task).not.toHaveProperty("parent");
			expect(task).not.toHaveProperty("open");
		}
	});

	it("gives no two tasks the same id", () => {
		// One Team on two Features and two Teams on one Feature, so a lane id that is its
		// Feature's and a lane id that is the Team's alone both collide here.
		const tasks = toGanttTasks(board, [
			lane(7, 5, "Zenith"),
			lane(7, 6, "Gravity"),
			lane(3, 5, "Zenith"),
		]);

		// The count is asserted too. Unique ids over a list the lanes never reached is a property
		// today's function already has.
		expect(tasks).toHaveLength(5);
		expect(new Set(tasks.map((task) => task.id)).size).toBe(tasks.length);
	});

	it("resolves every id it issues to the content that id was minted for", () => {
		// The adapter used to look a bar up with `barsById.get(Number(data.id))`. A lane id of
		// "7:5" coerces to NaN there, the lookup misses, and the library draws its own untemplated
		// bar — a blank box with no name and no click, and nothing anywhere says so.
		const lanes = [lane(7, 5, "Zenith"), lane(3, 6, "Gravity")];
		const contentFor = taskContentLookup(board, lanes);

		const resolved = toGanttTasks(board, lanes).map((task) => {
			const content = contentFor(task.id);
			return content?.lane?.teamName ?? content?.bar?.name;
		});

		expect(resolved).toEqual([
			"Coral Reef",
			"Zenith",
			"Kelp Forest",
			"Gravity",
		]);
	});
});
