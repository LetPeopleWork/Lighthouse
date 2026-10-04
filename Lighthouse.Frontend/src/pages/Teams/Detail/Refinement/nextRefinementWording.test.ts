import { describe, expect, it } from "vitest";
import { describeNextRefinement } from "./nextRefinementWording";

describe("describeNextRefinement", () => {
	it.each([
		["2026-10-08", new Date(2026, 9, 4, 9, 0), "Thu 8 Oct · in 4 days"],
		["2026-10-08", new Date(2026, 9, 7, 23, 59), "Thu 8 Oct · tomorrow"],
		["2026-10-08", new Date(2026, 9, 7, 0, 0), "Thu 8 Oct · tomorrow"],
		["2026-10-15", new Date(2026, 9, 8, 9, 0), "Thu 15 Oct · in 7 days"],
		["2026-10-27", new Date(2026, 9, 24, 12, 0), "Tue 27 Oct · in 3 days"],
		["2027-01-01", new Date(2026, 11, 31, 23, 30), "Fri 1 Jan · tomorrow"],
		["2027-03-30", new Date(2027, 2, 27, 1, 0), "Tue 30 Mar · in 3 days"],
	])("names %s seen on %s as %s", (nextRefinementDate, today, expected) => {
		expect(describeNextRefinement(nextRefinementDate, today, "Grooming")).toBe(
			`Next Grooming: ${expected}`,
		);
	});

	it.each([[null], [undefined], ["not-a-day"]])(
		"names nothing for %s",
		(nextRefinementDate) => {
			expect(
				describeNextRefinement(nextRefinementDate, new Date(), "Refinement"),
			).toBeNull();
		},
	);
});
