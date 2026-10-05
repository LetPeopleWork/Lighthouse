import { describe, expect, it } from "vitest";
import type {
	ISizingLogEntry,
	SizingChannel,
} from "../../../../models/Refinement/Refinement";
import {
	describeLogDay,
	describeLogEntry,
	findOpenQuestions,
} from "./sizingLogWording";

const anEntry = (entry: Partial<ISizingLogEntry>): ISizingLogEntry => ({
	kind: "Vote",
	answer: "Yes",
	comment: null,
	voterName: "Ana Lima",
	channel: "Web",
	recordedAt: "2026-10-07T09:00:00Z",
	isMine: false,
	...entry,
});

const CHANNELS: SizingChannel[] = ["Web", "Cli", "Assistant"];

describe("the wording of one log entry", () => {
	it.each([
		["Vote", "Yes", "Ana Lima voted Yes"],
		["Vote", "YesBut", "Ana Lima voted Yes, if…"],
		["Vote", "No", "Ana Lima voted No"],
		["Comment", null, "Ana Lima"],
		["Revocation", null, "Ana Lima took back their vote"],
	] as const)("words a %s answering %s as '%s'", (kind, answer, wording) => {
		expect(describeLogEntry(anEntry({ kind, answer }))).toBe(wording);
	});

	it.each(CHANNELS)(
		"words an entry that came through %s like any other",
		(channel) => {
			expect(describeLogEntry(anEntry({ channel }))).toBe("Ana Lima voted Yes");
		},
	);

	it("names the day an entry was recorded", () => {
		expect(describeLogDay(anEntry({}))).toBe("Wed 7 Oct");
	});
});

describe("which entries are open questions", () => {
	it.each([
		{
			case: "a comment from somebody who never voted",
			entries: [anEntry({ kind: "Comment", answer: null })],
			open: [0],
		},
		{
			case: "only the latest of their comments",
			entries: [
				anEntry({ kind: "Comment", answer: null }),
				anEntry({ kind: "Comment", answer: null }),
			],
			open: [1],
		},
		{
			case: "nothing once the commenter votes",
			entries: [anEntry({ kind: "Comment", answer: null }), anEntry({})],
			open: [],
		},
		{
			case: "nothing when the commenter already holds a vote",
			entries: [anEntry({}), anEntry({ kind: "Comment", answer: null })],
			open: [],
		},
		{
			case: "the comment again once its author takes their vote back",
			entries: [
				anEntry({ kind: "Comment", answer: null }),
				anEntry({}),
				anEntry({ kind: "Revocation", answer: null }),
			],
			open: [0],
		},
		{
			case: "each commenter on their own",
			entries: [
				anEntry({ voterName: "Jonas Weber" }),
				anEntry({ voterName: "Jonas Weber", kind: "Comment", answer: null }),
				anEntry({ voterName: "Mo Okafor", kind: "Comment", answer: null }),
			],
			open: [2],
		},
		{ case: "nothing in an empty log", entries: [], open: [] },
	])("marks $case", ({ entries, open }) => {
		expect([...findOpenQuestions(entries)]).toEqual(open);
	});
});
