import { describe, expect, it } from "vitest";
import { PAGE_SIZE } from "../../../../components/Common/DataGrid/RowRangeFooter";
import { describeEnoughFor, lineBeside } from "./enoughForPlacement";
import {
	LINE_HEIGHT_PX,
	neededNumberAt,
	shownCountOf,
	shownRowOf,
	spaceKeptFor,
} from "./refinementPaging";

const TERMS = {
	workItem: "Work Item",
	workItems: "Work Items",
	refinement: "Refinement",
};

const indexesOf = (count: number) =>
	Array.from({ length: count }, (_, index) => index);

const pageOf = (index: number) => (index < PAGE_SIZE ? 1 : 2);

/** Every row of the whole list asked where the line goes, with the page the row is drawn on. */
const linesInTheWholeList = (high: number, shownCount: number) =>
	indexesOf(shownCount).flatMap((index) => {
		const beside = lineBeside(high, shownRowOf(index, shownCount));
		return beside === null ? [] : [{ index, page: pageOf(index), ...beside }];
	});

const spaceInTheWholeList = (high: number, shownCount: number) =>
	indexesOf(shownCount).flatMap((index) => {
		const space = spaceKeptFor(high, shownRowOf(index, shownCount));
		return Object.keys(space).length === 0 ? [] : [{ index, space }];
	});

describe("shownRowOf", () => {
	it.each([
		[0, 143, false],
		[98, 143, false],
		[99, 143, false],
		[100, 143, false],
		[141, 143, false],
		[142, 143, true],
		[63, 64, true],
		[0, 1, true],
	])("row %i of %i shown: last shown is %s", (index, shownCount, isLast) => {
		expect(shownRowOf(index, shownCount).isLastShown).toBe(isLast);
	});

	it("keeps the place of the row and whether it is out of sight", () => {
		expect(shownRowOf(100, 143, true)).toEqual({
			index: 100,
			isLastShown: false,
			isOutOfSight: true,
		});
	});
});

describe("shownCountOf", () => {
	it.each([
		[0, 1],
		[99, 100],
		[100, 101],
		[142, 143],
		[63, 64],
	])("the row at place %i counts %i shown", (index, count) => {
		expect(shownCountOf(index)).toBe(count);
	});
});

describe("the line in a list of 143 shown rows", () => {
	it.each([
		[0, 1, "above", "EnoughFor", 0, { top: LINE_HEIGHT_PX }],
		[20, 1, "below", "EnoughFor", 19, { bottom: LINE_HEIGHT_PX }],
		[100, 1, "below", "EnoughFor", 99, { bottom: LINE_HEIGHT_PX }],
		[101, 2, "below", "EnoughFor", 100, { bottom: LINE_HEIGHT_PX }],
		[120, 2, "below", "EnoughFor", 119, { bottom: LINE_HEIGHT_PX }],
		[143, 2, "below", "EnoughFor", 142, { bottom: LINE_HEIGHT_PX }],
		[150, 2, "below", "AllNeeded", 142, { bottom: LINE_HEIGHT_PX }],
	])(
		"with %i needed, the line is beside one row only, on page %i, and space is kept for it there and nowhere else",
		(high, page, side, says, index, space) => {
			expect(linesInTheWholeList(high, 143)).toEqual([
				{ index, page, side, says },
			]);
			expect(spaceInTheWholeList(high, 143)).toEqual([{ index, space }]);
		},
	);

	it("says all 143 are needed under the last row when more are needed than are shown", () => {
		const row = shownRowOf(142, 143);
		const beside = lineBeside(150, row);

		expect(beside?.says).toBe("AllNeeded");
		expect(
			describeEnoughFor("AllNeeded", {
				shown: shownCountOf(row.index),
				highPercentile: 85,
				terms: TERMS,
			}),
		).toBe(
			"All 143 Work Items in Refinement are needed before the next Refinement.",
		);
	});
});

describe("the line in a list that a filter cut down to 64 rows", () => {
	it("puts it under the last shown row, saying all 64 are needed, with 120 needed", () => {
		expect(linesInTheWholeList(120, 64)).toEqual([
			{ index: 63, page: 1, side: "below", says: "AllNeeded" },
		]);
		expect(spaceInTheWholeList(120, 64)).toEqual([
			{ index: 63, space: { bottom: LINE_HEIGHT_PX } },
		]);
		expect(
			describeEnoughFor("AllNeeded", {
				shown: shownCountOf(63),
				highPercentile: 85,
				terms: TERMS,
			}),
		).toBe(
			"All 64 Work Items in Refinement are needed before the next Refinement.",
		);
	});
});

describe("neededNumberAt", () => {
	const numbersOnTheWholeList = (
		high: number | undefined,
		shownCount: number,
	) => indexesOf(shownCount).map((index) => neededNumberAt(high, index));

	it("numbers the first page 1 to 100 and continues at 101 on the second, up to 120", () => {
		const numbers = numbersOnTheWholeList(120, 143);

		expect(numbers.slice(0, PAGE_SIZE)).toEqual(
			indexesOf(PAGE_SIZE).map((index) => index + 1),
		);
		expect(numbers.slice(PAGE_SIZE)).toEqual([
			...indexesOf(20).map((offset) => PAGE_SIZE + offset + 1),
			...indexesOf(23).map(() => null),
		]);
	});

	it.each([
		[0, 0],
		[120, 120],
		[120, 142],
		[120, -1],
	])("with %i needed, the row at place %i has no number", (high, index) => {
		expect(neededNumberAt(high, index)).toBeNull();
	});

	it("gives no row a number when no line is drawn", () => {
		expect(numbersOnTheWholeList(undefined, 143)).toEqual(
			indexesOf(143).map(() => null),
		);
	});

	it("gives the last needed row its number", () => {
		expect(neededNumberAt(120, 119)).toBe(120);
	});
});
