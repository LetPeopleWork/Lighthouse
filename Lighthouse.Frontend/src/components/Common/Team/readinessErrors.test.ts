import { describe, expect, it } from "vitest";
import {
	DISCUSSION_RULE_ERROR,
	MIN_VOTERS_ERROR,
	MIN_YES_ERROR,
	readinessErrors,
} from "./ReadinessSettings";

// The server binds each number as a 32-bit int and refuses anything larger with a bare 400.
const LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES = 2147483647;

describe("the readiness rule the form shares with the server", () => {
	it.each([
		{ minYes: 3, minVoters: 3, minYesError: null, minVotersError: null },
		{ minYes: 1, minVoters: 1, minYesError: null, minVotersError: null },
		{ minYes: 2, minVoters: 5, minYesError: null, minVotersError: null },
		{
			minYes: 0,
			minVoters: 3,
			minYesError: MIN_YES_ERROR,
			minVotersError: null,
		},
		{
			minYes: -1,
			minVoters: 0,
			minYesError: MIN_YES_ERROR,
			minVotersError: null,
		},
		{
			minYes: Number.NaN,
			minVoters: 3,
			minYesError: MIN_YES_ERROR,
			minVotersError: null,
		},
		{
			minYes: 3,
			minVoters: 2,
			minYesError: null,
			minVotersError: MIN_VOTERS_ERROR,
		},
		{
			minYes: 1,
			minVoters: 0,
			minYesError: null,
			minVotersError: MIN_VOTERS_ERROR,
		},
		{
			minYes: 3,
			minVoters: Number.NaN,
			minYesError: null,
			minVotersError: MIN_VOTERS_ERROR,
		},
		{
			minYes: 1e11,
			minVoters: 1e11,
			minYesError: MIN_YES_ERROR,
			minVotersError: null,
		},
		{
			minYes: 3,
			minVoters: 1e11,
			minYesError: null,
			minVotersError: MIN_VOTERS_ERROR,
		},
		{
			minYes: LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES,
			minVoters: LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES,
			minYesError: null,
			minVotersError: null,
		},
	])(
		"$minYes Yes from $minVoters voters → $minYesError / $minVotersError",
		({ minYes, minVoters, minYesError, minVotersError }) => {
			expect(
				readinessErrors({
					minYes,
					minVoters,
					discussWhen: { no: 1, yesIf: 2 },
				}),
			).toEqual({
				minYes: minYesError,
				minVoters: minVotersError,
				discussWhenNo: null,
				discussWhenYesIf: null,
			});
		},
	);
});

describe("the discussion rules the form shares with the server", () => {
	it.each([
		{ no: 1, yesIf: 2, noError: null, yesIfError: null },
		{ no: 5, yesIf: 1, noError: null, yesIfError: null },
		{ no: null, yesIf: null, noError: null, yesIfError: null },
		{ no: 0, yesIf: 2, noError: DISCUSSION_RULE_ERROR, yesIfError: null },
		{ no: 1, yesIf: -1, noError: null, yesIfError: DISCUSSION_RULE_ERROR },
		{
			no: Number.NaN,
			yesIf: Number.NaN,
			noError: DISCUSSION_RULE_ERROR,
			yesIfError: DISCUSSION_RULE_ERROR,
		},
		{ no: null, yesIf: 0, noError: null, yesIfError: DISCUSSION_RULE_ERROR },
		{ no: 1e11, yesIf: 2, noError: DISCUSSION_RULE_ERROR, yesIfError: null },
		{ no: 1, yesIf: 1e11, noError: null, yesIfError: DISCUSSION_RULE_ERROR },
		{
			no: LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES,
			yesIf: LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES,
			noError: null,
			yesIfError: null,
		},
	])(
		"No at $no, “Yes, if…” at $yesIf → $noError / $yesIfError",
		({ no, yesIf, noError, yesIfError }) => {
			expect(
				readinessErrors({
					minYes: 3,
					minVoters: 3,
					discussWhen: { no, yesIf },
				}),
			).toEqual({
				minYes: null,
				minVoters: null,
				discussWhenNo: noError,
				discussWhenYesIf: yesIfError,
			});
		},
	);

	it("checks a readiness stored without discussion rules as if it had the defaults", () => {
		expect(readinessErrors({ minYes: 3, minVoters: 3 })).toEqual({
			minYes: null,
			minVoters: null,
			discussWhenNo: null,
			discussWhenYesIf: null,
		});
	});
});
