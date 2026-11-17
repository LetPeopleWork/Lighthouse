import fc from "fast-check";
import { describe, expect, it } from "vitest";
import { axisTop, findOutliers } from "./outliers";
import { outlierRuleSentence, outlierWording } from "./outlierWording";

interface PlottedItem {
	readonly id: number;
	readonly days: number;
}

const daysOf = (item: PlottedItem): number => item.days;
const idOf = (item: PlottedItem): number => item.id;

const plotted = (...days: number[]): PlottedItem[] =>
	days.map((value, index) => ({ id: 100 + index, days: value }));

// Plotted items with unique ids and Cycle Times from 0 to 400 days, a verdict that names some of them and
// possibly Work Items that are not plotted, and a limit from 0 to 400 days.
const aPlotAndItsVerdict = fc
	.uniqueArray(
		fc.record({
			id: fc.integer({ min: 1, max: 10_000 }),
			days: fc.integer({ min: 0, max: 400 }),
		}),
		{ selector: (item) => item.id, maxLength: 50 },
	)
	.chain((items) =>
		fc.record({
			items: fc.constant(items as PlottedItem[]),
			verdictIds: fc.uniqueArray(
				fc.oneof(
					fc.constantFrom(...items.map(idOf), 20_001),
					fc.integer({ min: 10_001, max: 20_000 }),
				),
				{ maxLength: 50 },
			),
			limit: fc.integer({ min: 0, max: 400 }),
		}),
	);

const ascending = (values: readonly number[]): number[] =>
	[...values].sort((a, b) => a - b);

describe("the outliers are the plotted Work Items the verdict names", () => {
	it("splits the plotted items into the outliers the verdict names, longest first, and every other item in its order (L3)", () => {
		fc.assert(
			fc.property(aPlotAndItsVerdict, ({ items, verdictIds }) => {
				const named = new Set(verdictIds);
				const expectedOutliers = items
					.filter((item) => named.has(item.id))
					.map(idOf);
				const expectedTypical = items
					.filter((item) => !named.has(item.id))
					.map(idOf);

				const { outliers, typical } = findOutliers(
					items,
					daysOf,
					idOf,
					verdictIds,
				);

				expect(
					ascending([...outliers.map(idOf), ...typical.map(idOf)]),
					"WHAT: an item was dropped or counted twice. WHY: outliers and drawn items together are every plotted item. HOW: put each plotted item in exactly one of the two lists.",
				).toEqual(ascending(items.map(idOf)));
				expect(
					ascending(outliers.map(idOf)),
					"WHAT: the outliers are not the plotted items the verdict names. WHY: the backend owns the rule; the chart only follows its verdict. HOW: set an item apart when its id is in the verdict, never by its days.",
				).toEqual(ascending(expectedOutliers));
				expect(
					outliers.map(daysOf),
					"WHAT: the outliers are not listed longest first. WHY: the list shows the longest Work Items at the top. HOW: sort the outliers by Cycle Time, longest first.",
				).toEqual([...outliers.map(daysOf)].sort((a, b) => b - a));
				expect(
					typical.map(idOf),
					"WHAT: the drawn items changed order. WHY: the chart draws the items as it got them. HOW: keep the drawn items in input order.",
				).toEqual(expectedTypical);
			}),
		);
	});

	it("never cuts the axis below a drawn item and, with outliers set apart, stops it at the limit unless a drawn item is longer (L3)", () => {
		fc.assert(
			fc.property(aPlotAndItsVerdict, ({ items, verdictIds, limit }) => {
				const { outliers, typical } = findOutliers(
					items,
					daysOf,
					idOf,
					verdictIds,
				);
				const drawn = typical.map(daysOf);
				const tallestDrawn = Math.max(0, ...drawn);

				const top = axisTop({
					typicalValues: drawn,
					lineValues: [],
					cut: limit,
					hasOutliers: outliers.length > 0,
				});

				expect(
					top,
					"WHAT: the axis stops below a drawn Work Item. WHY: a drawn dot above the axis is lost. HOW: never cap the axis below the tallest drawn item.",
				).toBeGreaterThanOrEqual(tallestDrawn);
				if (outliers.length > 0) {
					expect(
						top,
						"WHAT: the axis rises above the limit with nothing drawn there. WHY: the outliers live in the chip, above the axis. HOW: stop the axis at the limit unless a drawn item is longer.",
					).toBeLessThanOrEqual(Math.max(limit, tallestDrawn));
				}
			}),
		);
	});

	it("sets nothing apart when nothing is plotted", () => {
		const { outliers, typical } = findOutliers([], daysOf, idOf, [301]);

		expect(outliers).toEqual([]);
		expect(typical).toEqual([]);
	});

	it("ignores a verdict id that is not among the plotted items", () => {
		const { outliers, typical } = findOutliers(
			plotted(12, 30, 31, 90),
			daysOf,
			idOf,
			[102, 103, 999],
		);

		expect(outliers.map(daysOf)).toEqual([90, 31]);
		expect(typical.map(daysOf)).toEqual([12, 30]);
	});

	it("keeps the axis above a drawn item that is longer than the limit", () => {
		const { outliers, typical } = findOutliers(
			plotted(12, 45, 90),
			daysOf,
			idOf,
			[102],
		);

		const top = axisTop({
			typicalValues: typical.map(daysOf),
			lineValues: [],
			cut: 30,
			hasOutliers: outliers.length > 0,
		});

		expect(top).toBeGreaterThanOrEqual(45);
	});
});

describe("the outlier wording names the upper natural process limit", () => {
	it("states the rule in the configured words", () => {
		expect(
			outlierRuleSentence({
				workItem: "Ticket",
				workItems: "Tickets",
				cycleTime: "Lead Time",
			}),
		).toBe(
			"An outlier is a closed Ticket whose Lead Time is above the upper natural process limit of the Lead Time process behaviour chart for this date range.",
		);
	});

	it.each([
		[
			30.6,
			"Tickets with a Lead Time above the upper natural process limit (30.6 days)",
		],
		[
			30,
			"Tickets with a Lead Time above the upper natural process limit (30 days)",
		],
		[
			30.64,
			"Tickets with a Lead Time above the upper natural process limit (30.6 days)",
		],
	])(
		"titles the list with a limit of %s days, shown with at most one decimal",
		(limit, title) => {
			const { listTitle } = outlierWording({
				workItems: "Tickets",
				sle: "SLE",
				cycleTime: "Lead Time",
			});

			expect(
				typeof listTitle,
				"WHAT: the list title does not take the limit. WHY: the title states the limit the listed Work Items are above. HOW: make the list title a function of the unrounded limit.",
			).toBe("function");
			expect(listTitle(limit)).toBe(title);
		},
	);
});
