import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IReportNote } from "../../../models/Reports/ReportNote";
import {
	aRefusal,
	aReportService,
	defaultTerms,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	renderWithReports,
} from "../../../tests/ReportsTestKit";
import ReportNotes from "./ReportNotes";

/**
 * Each note of a report offers Edit and Delete exactly when the server says the user may change it and the user
 * may write on the report; an edit saves the new text and shows the note marked as edited, a refused edit says
 * why and keeps the field, and a delete takes the note out of the list.
 */

vi.mock("../../../services/TerminologyContext", async (importOriginal) => ({
	...(await importOriginal<
		typeof import("../../../services/TerminologyContext")
	>()),
	useTerminology: () => ({
		getTerm: (key: string) => defaultTerms[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const A_REPORT = ENGAGEMENT_START_ID;
const NEEDS_TEXT = "A note needs some text.";
const COULD_NOT_DELETE = "This note couldn't be deleted. Try again.";

const aNote = (overrides: Partial<IReportNote> = {}): IReportNote => ({
	id: 1,
	text: "Training took place",
	createdAt: "2026-10-02T07:00:00Z",
	createdOn: "2026-10-02",
	lastEditedAt: null,
	lastEditedOn: null,
	authorDisplayName: "Ann",
	canModify: true,
	...overrides,
});

const MINE = aNote({ id: 2, text: "Team grew to 9", createdOn: "2026-10-03" });
const SOMEBODY_ELSES = aNote({
	id: 1,
	text: "Training took place",
	authorDisplayName: "Bob",
	canModify: false,
});

/** The notes the server holds, which changing and deleting a note alter the way the real server does. */
const aServerHolding = (held: IReportNote[]) => {
	let notes = [...held];
	return aReportService({
		getNotes: vi.fn(() => Promise.resolve([...notes])),
		updateNote: vi.fn((_owner, _report, id: number, text: string) => {
			const changed = {
				...(notes.find((note) => note.id === id) ?? MINE),
				text: text.trim(),
				lastEditedOn: "2026-10-04",
			};
			notes = notes.map((note) => (note.id === id ? changed : note));
			return Promise.resolve(changed);
		}),
		deleteNote: vi.fn((_owner, _report, id: number) => {
			notes = notes.filter((note) => note.id !== id);
			return Promise.resolve();
		}),
	});
};

const openTheNotes = (
	reportService = aServerHolding([MINE, SOMEBODY_ELSES]),
	mayWrite = true,
) => {
	renderWithReports(
		<ReportNotes owner={LIGHTSPEED} reportId={A_REPORT} mayWrite={mayWrite} />,
		{ reportService },
	);
	return reportService;
};

const theNotes = () => screen.queryAllByTestId("report-note");
const theNoteWith = (text: string): HTMLElement => {
	const found = theNotes().find(
		(note) => within(note).queryByText(text) !== null,
	);
	if (found === undefined) {
		throw new Error(`No note shows "${text}".`);
	}
	return found;
};
const editOf = (note: HTMLElement) =>
	within(note).queryByRole("button", { name: /^Edit$/ });
const deleteOf = (note: HTMLElement) =>
	within(note).queryByRole("button", { name: /^Delete$/ });
const deleteTheNoteShowing = async (text: string) =>
	userEvent.click(
		within(theNoteWith(text)).getByRole("button", { name: /^Delete$/ }),
	);

const startEditing = async (text: string) => {
	await userEvent.click(
		within(theNoteWith(text)).getByRole("button", { name: /^Edit$/ }),
	);
	return within(theNoteWith(text)).getByRole("textbox");
};

const replaceTheTextWith = async (field: HTMLElement, text: string) => {
	await userEvent.clear(field);
	if (text !== "") {
		await userEvent.type(field, text);
	}
};

const saveTheEdit = async (note: HTMLElement) =>
	userEvent.click(within(note).getByRole("button", { name: /^Save$/ }));

describe("Changing and deleting the notes of a report", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("offers Edit and Delete on the notes the user may change and on no other", async () => {
		openTheNotes();

		await waitFor(() => expect(theNotes()).toHaveLength(2));

		expect(editOf(theNoteWith("Team grew to 9"))).toBeInTheDocument();
		expect(deleteOf(theNoteWith("Team grew to 9"))).toBeInTheDocument();
		expect(editOf(theNoteWith("Training took place"))).toBeNull();
		expect(deleteOf(theNoteWith("Training took place"))).toBeNull();
	});

	it("offers no Edit or Delete to a user who may not write on the report, even on a note marked as changeable", async () => {
		openTheNotes(aServerHolding([MINE, SOMEBODY_ELSES]), false);

		await waitFor(() => expect(theNotes()).toHaveLength(2));

		expect(screen.queryByRole("button", { name: /^Edit$/ })).toBeNull();
		expect(screen.queryByRole("button", { name: /^Delete$/ })).toBeNull();
	});

	it("saves the new text, shows the note marked as edited with the day and keeps it in its place", async () => {
		const reportService = openTheNotes();
		await waitFor(() => expect(theNotes()).toHaveLength(2));

		const field = await startEditing("Team grew to 9");
		expect(field).toHaveValue("Team grew to 9");
		await replaceTheTextWith(field, "  Team grew to 12  ");
		await saveTheEdit(theNotes()[0]);

		await waitFor(() =>
			expect(theNotes()[0]).toHaveTextContent("Team grew to 12"),
		);
		expect(reportService.updateNote).toHaveBeenCalledWith(
			LIGHTSPEED,
			A_REPORT,
			2,
			"  Team grew to 12  ",
		);
		expect(theNotes()[0]).toHaveTextContent(
			"2026-10-03 · Ann · edited 2026-10-04",
		);
		expect(within(theNotes()[0]).queryByRole("textbox")).toBeNull();
		expect(theNotes()[1]).toHaveTextContent("Training took place");
		expect(theNotes()[1].textContent).not.toContain("edited");
	});

	it("leaves the note as it was when the user cancels an edit", async () => {
		const reportService = openTheNotes();
		await waitFor(() => expect(theNotes()).toHaveLength(2));

		const field = await startEditing("Team grew to 9");
		await replaceTheTextWith(field, "Something else");
		await userEvent.click(
			within(theNotes()[0]).getByRole("button", { name: /^Cancel$/ }),
		);

		expect(reportService.updateNote).not.toHaveBeenCalled();
		expect(theNotes()[0]).toHaveTextContent("Team grew to 9");
		expect(within(theNotes()[0]).queryByRole("textbox")).toBeNull();
	});

	it("says why a note with no text is not saved, keeps the field with what was typed and keeps the note as it was", async () => {
		const reportService = openTheNotes();
		vi.mocked(reportService.updateNote).mockRejectedValue(
			aRefusal(400, "note-text-missing"),
		);
		await waitFor(() => expect(theNotes()).toHaveLength(2));

		const field = await startEditing("Team grew to 9");
		await replaceTheTextWith(field, "");
		await saveTheEdit(theNotes()[0]);

		expect(await screen.findByText(NEEDS_TEXT)).toBeInTheDocument();
		expect(within(theNotes()[0]).getByRole("textbox")).toHaveValue("");
		expect(theNotes()).toHaveLength(2);
	});

	it("deletes the note the user chose and lists the others", async () => {
		const reportService = openTheNotes();
		await waitFor(() => expect(theNotes()).toHaveLength(2));

		await deleteTheNoteShowing("Team grew to 9");

		await waitFor(() => expect(theNotes()).toHaveLength(1));
		expect(reportService.deleteNote).toHaveBeenCalledWith(
			LIGHTSPEED,
			A_REPORT,
			2,
		);
		expect(theNotes()[0]).toHaveTextContent("Training took place");
	});

	it("keeps a note that could not be deleted in the list with its controls", async () => {
		const reportService = openTheNotes();
		vi.mocked(reportService.deleteNote).mockRejectedValue(
			aRefusal(403, "note-not-yours"),
		);
		await waitFor(() => expect(theNotes()).toHaveLength(2));

		await deleteTheNoteShowing("Team grew to 9");

		await waitFor(() => expect(reportService.deleteNote).toHaveBeenCalled());
		expect(await screen.findByRole("alert")).toHaveTextContent(
			COULD_NOT_DELETE,
		);
		expect(theNotes()).toHaveLength(2);
		expect(deleteOf(theNoteWith("Team grew to 9"))).toBeInTheDocument();
	});
});
