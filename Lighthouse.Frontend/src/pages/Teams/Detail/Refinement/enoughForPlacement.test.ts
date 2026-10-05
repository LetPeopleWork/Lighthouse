import { describe, expect, it } from "vitest";
import {
	describeEnoughFor,
	type EnoughForSentence,
	isNumbered,
	type LineSide,
	lineBeside,
} from "./enoughForPlacement";

/** Every shown row asked where the line is; the line should be beside exactly one of them. */
const whereTheLineGoes = (high: number, shown: number) =>
	Array.from({ length: shown }, (_, index) => ({
		index,
		line: lineBeside(high, { index, isLastShown: index === shown - 1 }),
	})).flatMap(({ index, line }) => (line === null ? [] : [{ index, ...line }]));

describe("lineBeside", () => {
	it.each<[number, number, number, LineSide, EnoughForSentence]>([
		[3, 6, 2, "below", "EnoughFor"],
		[1, 6, 0, "below", "EnoughFor"],
		[6, 6, 5, "below", "EnoughFor"],
		[8, 6, 5, "below", "AllNeeded"],
		[7, 6, 5, "below", "AllNeeded"],
		[0, 6, 0, "above", "EnoughFor"],
		[2, 1, 0, "below", "AllNeeded"],
		[5, 3, 2, "below", "AllNeeded"],
	])(
		"high %i of %i shown: beside row %i, %s, saying %s",
		(high, shown, index, side, says) => {
			expect(whereTheLineGoes(high, shown)).toEqual([{ index, side, says }]);
		},
	);

	// The grid keeps a focused row rendered, out of sight, after it scrolls away; a line drawn with it
	// would show up where that row is not.
	it.each([
		[3, 2, false],
		[8, 5, true],
		[0, 0, false],
	])(
		"draws no line beside a row kept out of sight (high %i, row %i)",
		(high, index, isLastShown) => {
			expect(
				lineBeside(high, { index, isLastShown, isOutOfSight: true }),
			).toBeNull();
		},
	);
});

describe("isNumbered", () => {
	it.each([
		[3, 0, true],
		[3, 2, true],
		[3, 3, false],
		[0, 0, false],
		[3, -1, false],
	])("high %i, row %i: %s", (high, index, numbered) => {
		expect(isNumbered(high, index)).toBe(numbered);
	});
});

describe("describeEnoughFor", () => {
	const terms = {
		workItem: "Work Item",
		workItems: "Work Items",
		refinement: "Refinement",
	};

	it("says what the numbered ones are enough for", () => {
		expect(
			describeEnoughFor("EnoughFor", { shown: 6, highPercentile: 85, terms }),
		).toBe("enough for the next Refinement (85%) · not needed before then");
	});

	it("says all of them are needed", () => {
		expect(
			describeEnoughFor("AllNeeded", { shown: 6, highPercentile: 85, terms }),
		).toBe(
			"All 6 Work Items in Refinement are needed before the next Refinement.",
		);
	});

	it("says the only one is needed", () => {
		expect(
			describeEnoughFor("AllNeeded", { shown: 1, highPercentile: 85, terms }),
		).toBe(
			"The only Work Item in Refinement is needed before the next Refinement.",
		);
	});
});
