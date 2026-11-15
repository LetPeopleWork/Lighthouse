import { describe, expect, it } from "vitest";
import type {
	ISizingLogEntry,
	SizingChannel,
} from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiError } from "../../../../services/Api/ApiError";
import {
	describeLogDay,
	describeLogFailure,
	describeWriter,
	isWritten,
} from "./sizingLogWording";

const anEntry = (entry: Partial<ISizingLogEntry>): ISizingLogEntry => ({
	kind: "Vote",
	answer: "Yes",
	comment: null,
	voterName: "Ana Lima",
	channel: "Web",
	recordedAt: "2026-10-07T09:00:00Z",
	isMine: false,
	isOpenQuestion: false,
	...entry,
});

const CHANNELS: SizingChannel[] = ["Web", "Cli", "Assistant"];

const getRenamedTerm = (key: string): string =>
	({
		[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
		[TERMINOLOGY_KEYS.REFINEMENT]: "Grooming",
	})[key] ?? key;

describe("why a Work Item's log could not be read", () => {
	it("says, in the renamed terms, that the Work Item left refinement when the server finds no log for it", () => {
		expect(
			describeLogFailure(
				new ApiError(404, "Request failed with status code 404"),
				getRenamedTerm,
			),
		).toBe("That ticket is no longer in grooming.");
	});

	it.each([
		[
			"a refusal other than not found",
			new ApiError(500, "The sizing log could not be read"),
			"The sizing log could not be read",
		],
		[
			"a failure that is not a refusal",
			new Error("Network Error"),
			"Network Error",
		],
		[
			"something that is not an error at all",
			"the server went away",
			"the server went away",
		],
	])("returns the message of %s", (_what, error, message) => {
		expect(describeLogFailure(error, getRenamedTerm)).toBe(message);
	});
});

describe("what somebody wrote, as the votes and comments list it", () => {
	it.each([
		["Vote", "Yes", "Ana Lima · Yes"],
		["Vote", "YesBut", "Ana Lima · Yes, if…"],
		["Vote", "No", "Ana Lima · No"],
		["Comment", null, "Ana Lima"],
	] as const)("heads a %s answering %s as '%s'", (kind, answer, wording) => {
		expect(
			describeWriter(anEntry({ kind, answer, comment: "some words" })),
		).toBe(wording);
	});

	it.each(CHANNELS)(
		"heads an entry that came through %s like any other",
		(channel) => {
			expect(describeWriter(anEntry({ channel, comment: "words" }))).toBe(
				"Ana Lima · Yes",
			);
		},
	);

	it.each([
		["a vote with words", anEntry({ comment: "only if…" }), true],
		["a vote without words", anEntry({}), false],
		[
			"a comment",
			anEntry({ kind: "Comment", answer: null, comment: "Why?" }),
			true,
		],
		["a vote taken back", anEntry({ kind: "Revocation", answer: null }), false],
		[
			"a vote taken back that still carries the words it had",
			anEntry({ kind: "Revocation", answer: null, comment: "only if…" }),
			false,
		],
	])("counts %s as written: %s", (_what, entry, written) => {
		expect(isWritten(entry)).toBe(written);
	});

	it("names the day an entry was recorded", () => {
		expect(describeLogDay(anEntry({}))).toBe("Wed 7 Oct");
	});
});
