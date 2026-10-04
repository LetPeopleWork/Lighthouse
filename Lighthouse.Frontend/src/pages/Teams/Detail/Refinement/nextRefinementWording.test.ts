import { describe, expect, it } from "vitest";
import { describeNextRefinement } from "./nextRefinementWording";

describe("describeNextRefinement", () => {
	it.each([
		["2026-10-08", 4, "Thu 8 Oct · in 4 days"],
		["2026-10-08", 1, "Thu 8 Oct · tomorrow"],
		["2026-10-15", 7, "Thu 15 Oct · in 7 days"],
		["2027-01-01", 1, "Fri 1 Jan · tomorrow"],
		["2027-03-30", 3, "Tue 30 Mar · in 3 days"],
	])("names %s, %s days away, as %s", (nextRefinementDate, days, expected) => {
		expect(describeNextRefinement(nextRefinementDate, days, "Grooming")).toBe(
			`Next Grooming: ${expected}`,
		);
	});

	it.each([[null], [undefined]])(
		"names the day alone when the server sent no count (%s)",
		(days) => {
			expect(describeNextRefinement("2026-10-08", days, "Refinement")).toBe(
				"Next Refinement: Thu 8 Oct",
			);
		},
	);

	it.each([[null], [undefined], ["not-a-day"]])(
		"names nothing for %s",
		(nextRefinementDate) => {
			expect(
				describeNextRefinement(nextRefinementDate, 4, "Refinement"),
			).toBeNull();
		},
	);
});
