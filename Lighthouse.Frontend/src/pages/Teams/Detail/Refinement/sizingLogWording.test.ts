import { describe, expect, it } from "vitest";
import type {
	ISizingLogEntry,
	SizingChannel,
} from "../../../../models/Refinement/Refinement";
import { describeLogDay, describeLogEntry } from "./sizingLogWording";

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
