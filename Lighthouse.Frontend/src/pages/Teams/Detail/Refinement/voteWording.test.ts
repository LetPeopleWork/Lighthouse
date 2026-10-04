import { describe, expect, it } from "vitest";
import type { RowReadiness } from "../../../../models/Refinement/Refinement";
import {
	describeReadiness,
	describeSplit,
	describeVoteCount,
} from "./voteWording";

describe("how a row's votes read", () => {
	it.each([
		[0, "No votes"],
		[1, "1 vote"],
		[2, "2 votes"],
		[300, "300 votes"],
	])("%i votes read as '%s'", (voteCount, expected) => {
		expect(describeVoteCount(voteCount)).toBe(expected);
	});

	it.each([
		[{ yes: 0, yesBut: 0, no: 0 }, "0 Yes · 0 Yes, if… · 0 No"],
		[{ yes: 3, yesBut: 0, no: 1 }, "3 Yes · 0 Yes, if… · 1 No"],
		[{ yes: 1, yesBut: 2, no: 3 }, "1 Yes · 2 Yes, if… · 3 No"],
	])("the split %o reads as '%s'", (split, expected) => {
		expect(describeSplit(split)).toBe(expected);
	});
});

describe("what a row's votes make of it, in words", () => {
	it.each<[RowReadiness, number | null, string]>([
		["Ready", null, "Ready"],
		["MoreYesNeeded", 1, "1 more Yes needed"],
		["MoreYesNeeded", 2, "2 more Yes needed"],
		["MoreVotersNeeded", 1, "1 more voter needed"],
		["MoreVotersNeeded", 2, "2 more voters needed"],
		["NeedsDiscussion", null, "Needs discussion"],
	])("%s missing %s reads as '%s'", (readiness, missingVotes, expected) => {
		expect(describeReadiness(readiness, missingVotes)).toBe(expected);
	});
});
