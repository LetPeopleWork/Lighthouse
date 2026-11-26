import { describe, expect, it } from "vitest";
import { aRefusal } from "../../../tests/ReportsTestKit";
import { refusalCopyOf } from "./reportNoteRefusalCopy";

/** What a user is told when the server did not keep the text of a note. */
describe("The words for a note the server did not keep", () => {
	it.each([
		["no text", "note-text-missing", 400, "A note needs some text."],
		[
			"more than 2000 characters",
			"note-text-too-long",
			400,
			"A note can have at most 2000 characters.",
		],
		[
			"another reason",
			"not-saved",
			500,
			"This note couldn't be saved. Try again.",
		],
	])("tells the user about %s", (_what, code, status, words) => {
		expect(refusalCopyOf(aRefusal(status, code))).toBe(words);
	});

	it("falls back to the general words for something that is not a refusal", () => {
		expect(refusalCopyOf(new Error("offline"))).toBe(
			"This note couldn't be saved. Try again.",
		);
	});
});
