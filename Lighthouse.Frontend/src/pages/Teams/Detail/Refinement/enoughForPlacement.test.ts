import { describe, expect, it } from "vitest";
import {
	describeEnoughFor,
	type EnoughForPlacement,
	placeEnoughForLine,
} from "./enoughForPlacement";

describe("placeEnoughForLine", () => {
	it.each<[number, number, EnoughForPlacement]>([
		[3, 6, { numbered: 3, lineAfterRow: 2, says: "EnoughFor" }],
		[1, 6, { numbered: 1, lineAfterRow: 0, says: "EnoughFor" }],
		[6, 6, { numbered: 6, lineAfterRow: 5, says: "EnoughFor" }],
		[8, 6, { numbered: 6, lineAfterRow: 5, says: "AllNeeded" }],
		[7, 6, { numbered: 6, lineAfterRow: 5, says: "AllNeeded" }],
		[0, 6, { numbered: 0, lineAfterRow: -1, says: "EnoughFor" }],
		[2, 1, { numbered: 1, lineAfterRow: 0, says: "AllNeeded" }],
	])("high %i of %i listed", (high, listed, expected) => {
		expect(placeEnoughForLine(high, listed)).toEqual(expected);
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
			describeEnoughFor("EnoughFor", { listed: 6, highPercentile: 85, terms }),
		).toBe("enough for the next Refinement (85%) · not needed before then");
	});

	it("says all of them are needed", () => {
		expect(
			describeEnoughFor("AllNeeded", { listed: 6, highPercentile: 85, terms }),
		).toBe(
			"All 6 Work Items in Refinement are needed before the next Refinement.",
		);
	});

	it("says the only one is needed", () => {
		expect(
			describeEnoughFor("AllNeeded", { listed: 1, highPercentile: 85, terms }),
		).toBe(
			"The only Work Item in Refinement is needed before the next Refinement.",
		);
	});
});
