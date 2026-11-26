import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IReportNote } from "../../../models/Reports/ReportNote";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	aRefusal,
	aReportService,
	defaultTerms,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	renderWithReports,
	thePanel,
} from "../../../tests/ReportsTestKit";
import ReportNotes from "./ReportNotes";
import ReportView from "./ReportView";

/**
 * A writer opens the notes of a report: they are listed newest first, each with the day it was written and
 * who wrote it, the writer adds one and sees it at the top, a note without text is not saved and says why, and
 * the area shows a loading look and a failure look of its own while the report numbers stay in view.
 */

const { reportUsage, terms } = vi.hoisted(() => ({
	reportUsage: vi.fn(),
	terms: { current: {} as Record<string, string> },
}));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const A_REPORT = ENGAGEMENT_START_ID;
const NEEDS_TEXT = "A note needs some text.";
const AT_MOST_2000 = "A note can have at most 2000 characters.";
const LOADING_NOTES = "Loading notes";
const COULD_NOT_LOAD_NOTES = "Could not load the notes";

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

const NEWER = aNote({
	id: 2,
	text: "Team grew to 9",
	createdAt: "2026-10-02T23:30:00Z",
	createdOn: "2026-10-03",
	authorDisplayName: "Ann",
});
const OLDER = aNote({ id: 1, text: "Training took place" });

/** The notes the server holds, newest first, which writing a note changes the way the real server does. */
const aServerHolding = (held: IReportNote[]) => {
	const notes = [...held];
	return aReportService({
		getNotes: vi.fn(() => Promise.resolve([...notes])),
		addNote: vi.fn((_owner, _report, text: string) => {
			const saved = aNote({
				id: 10,
				text: text.trim(),
				createdOn: "2026-10-02",
				authorDisplayName: "Ann",
			});
			notes.unshift(saved);
			return Promise.resolve(saved);
		}),
	});
};

const openTheNotes = (
	reportService = aServerHolding([NEWER, OLDER]),
	mayWrite = true,
) => {
	renderWithReports(
		<ReportNotes owner={LIGHTSPEED} reportId={A_REPORT} mayWrite={mayWrite} />,
		{ reportService },
	);
	return reportService;
};

const theNotesArea = async () => screen.findByTestId("report-notes");
const theNotes = () => screen.queryAllByTestId("report-note");
const theField = () => screen.getByTestId("report-note-input");
const theSaveButton = () => screen.getByTestId("save-report-note-button");

const typeIntoTheField = async (text: string) => {
	if (text !== "") {
		await userEvent.type(theField(), text);
	}
};

const saveWhatWasTyped = async () => userEvent.click(theSaveButton());

const theUsageEventsNamed = (name: string) =>
	reportUsage.mock.calls.filter(([use]) => use.name === name);

describe("The notes of a report", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	it("lists the notes newest first, each with the day it was written and its author, and shows a note nobody signed with the day alone", async () => {
		openTheNotes();

		const area = await theNotesArea();
		await waitFor(() => expect(theNotes()).toHaveLength(2));
		const [first, second] = theNotes();
		expect(
			within(area).getByRole("heading", { name: /^Notes$/ }),
		).toBeInTheDocument();
		expect(first).toHaveTextContent("Team grew to 9");
		expect(first).toHaveTextContent("2026-10-03 · Ann");
		expect(second).toHaveTextContent("Training took place");
		expect(second).toHaveTextContent("2026-10-02");
		expect(second.textContent).not.toContain("·");
	});

	it("says there are no notes yet when the report has none, and still offers the writer a field", async () => {
		openTheNotes(aServerHolding([]));

		await screen.findByText("No notes yet.");

		expect(theNotes()).toHaveLength(0);
		expect(theField()).toBeInTheDocument();
	});

	it("saves what the writer typed as typed, shows the new note first and empties the field", async () => {
		const reportService = openTheNotes();
		await waitFor(() => expect(theNotes()).toHaveLength(2));

		await typeIntoTheField("  Team grew to 12  ");
		await saveWhatWasTyped();

		await waitFor(() => expect(theNotes()).toHaveLength(3));
		expect(reportService.addNote).toHaveBeenCalledWith(
			LIGHTSPEED,
			A_REPORT,
			"  Team grew to 12  ",
		);
		expect(theNotes()[0]).toHaveTextContent("Team grew to 12");
		expect(theNotes()[0]).toHaveTextContent("2026-10-02 · Ann");
		expect(theNotes()[1]).toHaveTextContent("Team grew to 9");
		expect(theField()).toHaveValue("");
		expect(theUsageEventsNamed("ReportNoteAdded")).toEqual([
			[{ name: "ReportNoteAdded" }],
		]);
	});

	it.each([
		["nothing", ""],
		["only blanks", "   "],
	])(
		"does not save a note with %s in it and tells the writer it needs some text",
		async (_what, typed) => {
			const reportService = openTheNotes();
			vi.mocked(reportService.addNote).mockRejectedValue(
				aRefusal(400, "note-text-missing"),
			);
			await waitFor(() => expect(theNotes()).toHaveLength(2));

			await typeIntoTheField(typed);
			await saveWhatWasTyped();

			expect(await screen.findByText(NEEDS_TEXT)).toBeInTheDocument();
			expect(theNotes()).toHaveLength(2);
			expect(theUsageEventsNamed("ReportNoteAdded")).toHaveLength(0);
		},
	);

	it("does not save a note over 2000 characters and tells the writer the limit", async () => {
		const reportService = openTheNotes();
		vi.mocked(reportService.addNote).mockRejectedValue(
			aRefusal(400, "note-text-too-long"),
		);
		await waitFor(() => expect(theNotes()).toHaveLength(2));

		fireEvent.change(theField(), { target: { value: "x".repeat(2001) } });
		await saveWhatWasTyped();

		expect(await screen.findByText(AT_MOST_2000)).toBeInTheDocument();
		expect(screen.queryByText(NEEDS_TEXT)).toBeNull();
		expect(theNotes()).toHaveLength(2);
		expect(theUsageEventsNamed("ReportNoteAdded")).toHaveLength(0);
	});
});

describe("The notes area while its notes are on the way or cannot be read", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	it("says it is loading the notes under a spinner, and shows no 'No notes yet.' that could pass for an empty report", async () => {
		const answer = deferred<IReportNote[]>();
		const reportService = aReportService({
			getNotes: vi.fn().mockReturnValue(answer.promise),
		});

		openTheNotes(reportService);

		const area = await theNotesArea();
		expect(within(area).getByText(LOADING_NOTES)).toBeInTheDocument();
		expect(within(area).getByRole("progressbar")).toBeInTheDocument();
		expect(screen.queryByText("No notes yet.")).toBeNull();

		answer.resolve([NEWER]);

		await waitFor(() => expect(theNotes()).toHaveLength(1));
		expect(screen.queryByText(LOADING_NOTES)).toBeNull();
	});

	it("says the notes could not be loaded with a Retry and no field to write in, and shows them once Retry works", async () => {
		const getNotes = vi
			.fn()
			.mockRejectedValueOnce(aRefusal(500, "failed"))
			.mockResolvedValue([NEWER]);

		openTheNotes(aReportService({ getNotes }));

		const area = await theNotesArea();
		expect(await within(area).findByText(COULD_NOT_LOAD_NOTES)).toBeVisible();
		expect(screen.queryByTestId("report-note-input")).toBeNull();
		expect(screen.queryByText("No notes yet.")).toBeNull();

		await userEvent.click(within(area).getByRole("button", { name: "Retry" }));

		await waitFor(() => expect(theNotes()).toHaveLength(1));
		expect(screen.queryByText(COULD_NOT_LOAD_NOTES)).toBeNull();
		expect(theField()).toBeInTheDocument();
	});
});

describe("The notes in the live report", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	it("shows an editor of the report its notes above the numbers, and keeps the numbers in view when the notes cannot be loaded", async () => {
		const reportService = aReportService({
			getNotes: vi.fn().mockRejectedValue(aRefusal(500, "failed")),
		});
		renderWithReports(<ReportView owner={LIGHTSPEED} reportId={A_REPORT} />, {
			reportService,
			viewer: "admin",
		});

		const area = await theNotesArea();
		const firstPanel = await thePanel("Cycle Time");

		expect(
			area.compareDocumentPosition(firstPanel) &
				Node.DOCUMENT_POSITION_FOLLOWING,
		).toBeTruthy();
		expect(await within(area).findByText(COULD_NOT_LOAD_NOTES)).toBeVisible();
		expect(firstPanel).toBeVisible();
	});

	it("shows an editor of the report a written note above the first panel of the numbers", async () => {
		const reportService = aReportService({
			getNotes: vi.fn().mockResolvedValue([NEWER]),
		});
		renderWithReports(<ReportView owner={LIGHTSPEED} reportId={A_REPORT} />, {
			reportService,
			viewer: "admin",
		});

		const area = await theNotesArea();
		const firstPanel = await thePanel("Cycle Time");

		expect(await within(area).findByText("Team grew to 9")).toBeVisible();
		expect(
			area.compareDocumentPosition(firstPanel) &
				Node.DOCUMENT_POSITION_FOLLOWING,
		).toBeTruthy();
	});
});
