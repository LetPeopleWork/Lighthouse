import { describe, expect, it } from "vitest";
import type { IReportNote } from "../../../models/Reports/ReportNote";
import { bylineOf } from "./reportNoteByline";

const aNote = (overrides: Partial<IReportNote> = {}): IReportNote => ({
	id: 1,
	text: "Training took place",
	createdAt: "2026-10-02T07:00:00Z",
	createdOn: "2026-10-02",
	lastEditedAt: null,
	lastEditedOn: null,
	authorDisplayName: null,
	...overrides,
});

describe("The line under a note", () => {
	it("names the day and the author of a signed note", () => {
		expect(bylineOf(aNote({ authorDisplayName: "Ann" }))).toBe(
			"2026-10-02 · Ann",
		);
	});

	it("names the day alone when nobody signed the note", () => {
		expect(bylineOf(aNote())).toBe("2026-10-02");
	});

	it("adds the day of the last edit to an edited note", () => {
		expect(
			bylineOf(aNote({ authorDisplayName: "Ann", lastEditedOn: "2026-10-04" })),
		).toBe("2026-10-02 · Ann · edited 2026-10-04");
		expect(bylineOf(aNote({ lastEditedOn: "2026-10-04" }))).toBe(
			"2026-10-02 · edited 2026-10-04",
		);
	});
});
