import { describe, expect, it } from "vitest";
import type { RowReadiness } from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiError } from "../../../../services/Api/ApiError";
import {
	describeReadiness,
	describeSplit,
	describeVoteCount,
	describeVoteRefusal,
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

describe("why a vote was refused", () => {
	const terms: Record<string, string> = {
		[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
		[TERMINOLOGY_KEYS.REFINEMENT]: "Grooming",
	};
	const getTerm = (key: string) => terms[key] ?? key;
	const ASK_FOR_THE_NAME = "Please give your name (at most 100 characters).";

	it.each([
		[
			"too many votes",
			new ApiError(429, "Request failed with status code 429"),
			"Too many votes from this browser. Try again in a minute.",
		],
		[
			"the work left refinement",
			new ApiError(
				409,
				"Conflict",
				undefined,
				undefined,
				"work-item-not-in-refinement",
			),
			"This ticket is no longer in grooming.",
		],
		[
			"a refused name, by its code",
			new ApiError(
				400,
				"Bad Request",
				undefined,
				undefined,
				"voter-name-too-long",
			),
			ASK_FOR_THE_NAME,
		],
		[
			"a refused name, by the field the body check named",
			new ApiError(
				400,
				"The field VoterName is invalid.",
				undefined,
				"VoterName",
			),
			ASK_FOR_THE_NAME,
		],
		[
			"another code the server named",
			new ApiError(
				400,
				"A vote needs the key the voter's browser keeps.",
				undefined,
				undefined,
				"voter-key-required",
			),
			"A vote needs the key the voter's browser keeps.",
		],
		[
			"another field the body check named",
			new ApiError(400, "The field Comment is too long.", undefined, "Comment"),
			"The field Comment is too long.",
		],
		[
			"the name field on something other than a bad request",
			new ApiError(
				500,
				"The sizing log could not be written",
				undefined,
				"voterName",
			),
			"The sizing log could not be written",
		],
		[
			"an error that did not come from the server",
			new Error("Network Error"),
			"Network Error",
		],
		["something that is not an error at all", "offline", "offline"],
	])("says so for %s", (_why, error, said) => {
		expect(describeVoteRefusal(error, getTerm)).toBe(said);
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
