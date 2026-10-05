import { describe, expect, it } from "vitest";
import type {
	ISizingLogEntry,
	SizingChannel,
} from "../../../../models/Refinement/Refinement";
import { describeLogDay, describeWriter, isWritten } from "./sizingLogWording";

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
