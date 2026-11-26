import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IReportOwner } from "../../../models/Reports/Report";
import type { IReportNote } from "../../../models/Reports/ReportNote";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "../../../tests/MockApiServiceProvider";
import {
	forgetTheBrowser,
	openingThePrintPageDirectly,
	theFontsHaveLoaded,
	thePrintPage,
	thePrintPageIsReady,
} from "../../../tests/ReportPrintTestKit";
import {
	APOLLO,
	aRefusal,
	aReportService,
	defaultTerms,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	renderWithReports,
	summaryFor,
	thePanel,
	type Viewer,
} from "../../../tests/ReportsTestKit";
import ReportView from "./ReportView";

/**
 * A reader of a report who may not change it still gets the context: the notes are listed above the numbers,
 * for a Team and for a Portfolio, with no field to write in and no control to change or delete a note. The
 * same holds while the reader's roles are still on their way, and a printed report carries no note.
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

const aReportHoldingNotes = (notes: IReportNote[]) =>
	aReportService({ getNotes: vi.fn().mockResolvedValue(notes) });

const openTheReportAs = (
	viewer: Viewer,
	owner: IReportOwner,
	reportService = aReportHoldingNotes([NEWER, OLDER]),
) => {
	renderWithReports(<ReportView owner={owner} reportId={A_REPORT} />, {
		reportService,
		viewer,
		owner,
	});
	return reportService;
};

/** The report opened by a reader whose roles have not arrived yet; they arrive when the test says. */
const openTheReportBeforeTheRolesArrive = (owner: IReportOwner) => {
	const roles = deferred<ReturnType<typeof summaryFor>>();
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockReturnValue(roles.promise);
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
	});
	render(
		<QueryClientProvider client={queryClient}>
			<MemoryRouter>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({
						reportService: aReportHoldingNotes([NEWER, OLDER]),
						rbacService,
					})}
				>
					<SnackbarErrorHandler>
						<ReportView owner={owner} reportId={A_REPORT} />
					</SnackbarErrorHandler>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);
	return { rolesArrive: () => roles.resolve(summaryFor("reader", owner)) };
};

const theNotesArea = async () => screen.findByTestId("report-notes");
const theNotes = () => screen.queryAllByTestId("report-note");
const theNoteTexts = () => theNotes().map((note) => note.textContent);
const theControlsIn = (area: HTMLElement) =>
	within(area)
		.queryAllByRole("button")
		.concat(within(area).queryAllByRole("textbox"))
		.map(
			(control) => control.getAttribute("aria-label") ?? control.textContent,
		);
const noWritingControlIsShown = () => {
	expect(screen.queryByTestId("report-note-input")).toBeNull();
	expect(screen.queryByRole("button", { name: /^Add note$/ })).toBeNull();
};
const noChangeOrDeleteControlIsShownOn = (area: HTMLElement) => {
	expect(within(area).queryByRole("button", { name: /edit/i })).toBeNull();
	expect(within(area).queryByRole("button", { name: /delete/i })).toBeNull();
};

describe("The notes of a report for someone who may only read it", () => {
	it.each([
		["Team", LIGHTSPEED],
		["Portfolio", APOLLO],
	])(
		"lists the notes of a %s report above the numbers, newest first, with no way to add, change or delete one",
		async (_kind, owner) => {
			openTheReportAs("reader", owner);

			const area = await theNotesArea();
			const firstPanel = await thePanel("Cycle Time");

			await waitFor(() => expect(theNotes()).toHaveLength(2));
			const [first, second] = theNotes();
			expect(first).toHaveTextContent("Team grew to 9");
			expect(first).toHaveTextContent("2026-10-03 · Ann");
			expect(second).toHaveTextContent("Training took place");
			expect(second).toHaveTextContent("2026-10-02");
			expect(
				area.compareDocumentPosition(firstPanel) &
					Node.DOCUMENT_POSITION_FOLLOWING,
			).toBeTruthy();
			noWritingControlIsShown();
			noChangeOrDeleteControlIsShownOn(area);
		},
	);

	it("says there are no notes yet to a reader of a report without notes, and still offers no field", async () => {
		openTheReportAs("reader", LIGHTSPEED, aReportHoldingNotes([]));

		const area = await theNotesArea();

		expect(await within(area).findByText("No notes yet.")).toBeVisible();
		noWritingControlIsShown();
	});

	it("says the notes could not be loaded to a reader, with a Retry, and shows them once Retry works", async () => {
		const getNotes = vi
			.fn()
			.mockRejectedValueOnce(aRefusal(500, "failed"))
			.mockResolvedValue([NEWER]);
		openTheReportAs("reader", LIGHTSPEED, aReportService({ getNotes }));

		const area = await theNotesArea();
		expect(await within(area).findByText(COULD_NOT_LOAD_NOTES)).toBeVisible();
		expect(await thePanel("Cycle Time")).toBeVisible();
		noWritingControlIsShown();

		await userEvent.click(within(area).getByRole("button", { name: "Retry" }));

		await waitFor(() => expect(theNotes()).toHaveLength(1));
		expect(screen.queryByText(COULD_NOT_LOAD_NOTES)).toBeNull();
		noWritingControlIsShown();
	});

	it("shows the same notes in the same order with the same controls while the reader's roles are on their way and after they arrive as a reader's", async () => {
		const { rolesArrive } = openTheReportBeforeTheRolesArrive(LIGHTSPEED);
		await waitFor(() => expect(theNotes()).toHaveLength(2));
		const area = await theNotesArea();
		const before = { texts: theNoteTexts(), controls: theControlsIn(area) };

		await act(async () => rolesArrive());

		expect(theNoteTexts()).toEqual(before.texts);
		expect(theControlsIn(area)).toEqual(before.controls);
		expect(before.texts).toEqual([
			expect.stringContaining("Team grew to 9"),
			expect.stringContaining("Training took place"),
		]);
		noWritingControlIsShown();
	});
});

describe("A printed report", () => {
	beforeEach(() => {
		theFontsHaveLoaded();
	});

	afterEach(forgetTheBrowser);

	it("carries none of the notes the report has, and never asks for them", async () => {
		const reportService = aReportHoldingNotes([NEWER, OLDER]);

		openingThePrintPageDirectly({ reportService });
		await thePrintPageIsReady();

		expect(thePrintPage()).not.toHaveTextContent("Team grew to 9");
		expect(thePrintPage()).not.toHaveTextContent("Training took place");
		expect(reportService.getNotes).not.toHaveBeenCalled();
	});
});
