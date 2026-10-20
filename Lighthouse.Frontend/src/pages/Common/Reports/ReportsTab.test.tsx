import {
	act,
	fireEvent,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { deferred } from "../../../tests/HeldMetricsService";
import { exportAsPdfLabelOf } from "../../../tests/ReportPrintTestKit";
import {
	APOLLO,
	aListedReport,
	aListedReportOfALaterType,
	aRefusal,
	aReportList,
	aReportService,
	CANNOT_BE_REBUILT,
	CAP_SENTENCE,
	COULD_NOT_LOAD,
	defaultTerms,
	ENGAGEMENT_START,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	LIST_COULD_NOT_LOAD,
	NO_LONGER_EXISTS,
	NOT_SUPPORTED_IN_THIS_VERSION,
	renderWithReports,
	theEngagementStartReport,
} from "../../../tests/ReportsTestKit";
import ReportsTab from "./ReportsTab";
import { reportQueryKeyOf, reportsQueryKeyOf } from "./reportOwner";

/**
 * The Reports tab of a Team or a Portfolio: the table of its reports, who may create, change and delete them, the
 * Community cap, and what the tab shows while its list is on its way or could not be read. Opening the tab
 * itself is reported as a tab opening by the page, not here; deleting a report is reported here.
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

const EMPTY = "No reports yet.";

const theCreateButton = () =>
	screen.findByRole("button", { name: "Create Report" });

const theDeleteButtonOf = async (name: string) => {
	const row = (await screen.findByRole("link", { name })).closest(
		'[role="row"]',
	) as HTMLElement;
	return within(row).getByRole("button", { name: /delete/i });
};

describe("The Reports tab lists a Team's reports", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-01 @slice-01 @driving_port @contract-shape:pure-function
	it("lists each report by name, linking to it, with its Type and when it was created", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
		});

		const link = await screen.findByRole("link", { name: ENGAGEMENT_START });

		expect(link).toHaveAttribute(
			"href",
			`/teams/${LIGHTSPEED.id}/reports/${ENGAGEMENT_START_ID}`,
		);
		expect(
			screen.getAllByRole("columnheader").map((header) => header.textContent),
		).toEqual(expect.arrayContaining(["Name", "Type", "Created"]));
		expect(screen.getByRole("row", { name: /Then & Now/ })).toBeVisible();
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// The code calls it a template; nobody using Lighthouse ever reads that word.
	it("never says Template", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
		});

		await screen.findByRole("link", { name: ENGAGEMENT_START });

		expect(document.body).not.toHaveTextContent(/template/i);
	});

	// @us-01 @slice-01 @contract-shape:pure-function
	it("offers an admin Create Report", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
		});

		expect(await theCreateButton()).toBeEnabled();
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("shows a reader the reports but nothing to create or delete them with", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
			viewer: "reader",
		});

		await screen.findByRole("link", { name: ENGAGEMENT_START });

		expect(screen.queryByRole("button", { name: "Create Report" })).toBeNull();
		expect(screen.queryByRole("button", { name: /delete/i })).toBeNull();
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	// Every Community instance runs without sign-in, where everybody may do everything.
	it("lets everybody create and delete when roles are not enforced", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
			viewer: "rbacOff",
		});

		expect(await theCreateButton()).toBeEnabled();
		expect(await theDeleteButtonOf(ENGAGEMENT_START)).toBeEnabled();
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	it("shows an admin of a Team without reports an empty list and Create Report", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
			}),
		});

		expect(await screen.findByText(EMPTY)).toBeVisible();
		expect(await theCreateButton()).toBeEnabled();
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	it("tells a reader of a Team without reports that a Team Admin can create one", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
			}),
			viewer: "reader",
		});

		expect(
			await screen.findByText("No reports yet. A Team Admin can create one."),
		).toBeVisible();
	});

	// @us-01 @us-06 @slice-06 @boundary @contract-shape:pure-function
	it("tells a reader of a Portfolio without reports that a Portfolio Admin can create one, in the instance's words", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.PORTFOLIO]: "Programme",
		};
		renderWithReports(<ReportsTab owner={APOLLO} />, {
			reportService: aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
			}),
			viewer: "reader",
			owner: APOLLO,
		});

		expect(
			await screen.findByText(
				"No reports yet. A Programme Admin can create one.",
			),
		).toBeVisible();
	});

	// @us-06 @slice-06 @driving_port @contract-shape:pure-function
	it("links a Portfolio's report to its place under the Portfolio", async () => {
		renderWithReports(<ReportsTab owner={APOLLO} />, {
			reportService: aReportService({
				getReports: vi
					.fn()
					.mockResolvedValue(
						aReportList([
							aListedReport({ ownerKind: "portfolio", ownerId: APOLLO.id }),
						]),
					),
			}),
			owner: APOLLO,
		});

		expect(
			await screen.findByRole("link", { name: ENGAGEMENT_START }),
		).toHaveAttribute(
			"href",
			`/portfolios/${APOLLO.id}/reports/${ENGAGEMENT_START_ID}`,
		);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// An instance rolled back from a later version still holds that version's reports.
	it("lists a report of a type this version cannot show as not supported, and still lets an admin delete it", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi
					.fn()
					.mockResolvedValue(
						aReportList([aListedReport(), aListedReportOfALaterType()]),
					),
			}),
		});

		const laterRow = (
			await screen.findByRole("link", { name: "Signals — flow alerts" })
		).closest('[role="row"]') as HTMLElement;

		expect(laterRow).toHaveTextContent(NOT_SUPPORTED_IN_THIS_VERSION);
		expect(await theDeleteButtonOf("Signals — flow alerts")).toBeEnabled();
		expect(screen.getByRole("link", { name: ENGAGEMENT_START })).toBeVisible();
	});
});

describe("The Community cap on the Reports tab", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	const atTheCap = () =>
		aReportService({
			getReports: vi
				.fn()
				.mockResolvedValue(aReportList([aListedReport()], true)),
		});

	// @us-08 @slice-08 @error @kpi-OUT-5878-K4-community-cap-reached @contract-shape:pure-function
	it("disables Create Report at the cap and says why, in the instance's word for a Team", async () => {
		terms.current = { ...defaultTerms, [TERMINOLOGY_KEYS.TEAM]: "Squad" };
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: atTheCap(),
		});

		expect(await theCreateButton()).toBeDisabled();
		expect(
			screen.getByText(CAP_SENTENCE.replace("per Team", "per Squad")),
		).toBeVisible();
	});

	// @us-08 @slice-08 @error @contract-shape:pure-function
	// The same tooltip every other Premium-limited control carries, so the way to Premium looks the same.
	it("explains the disabled Create Report with the Premium licence tooltip", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: atTheCap(),
		});
		const create = await theCreateButton();
		expect(create).toBeDisabled();

		await userEvent.hover(create.parentElement as HTMLElement);

		expect(
			await screen.findByRole("link", { name: "premium license." }),
		).toHaveAttribute(
			"href",
			"https://letpeople.work/lighthouse#lighthouse-license",
		);
	});

	// @us-08 @slice-08 @boundary @contract-shape:pure-function
	it("says nothing about the cap below it", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
			}),
		});

		expect(await theCreateButton()).toBeEnabled();
		expect(screen.queryByText(/Community includes/)).toBeNull();
	});

	// @us-08 @slice-08 @contract-shape:bounded-change
	it("enables Create Report again once the report is deleted", async () => {
		const getReports = vi
			.fn()
			.mockResolvedValueOnce(aReportList([aListedReport()], true))
			.mockResolvedValue(aReportList([]));
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({ getReports }),
		});
		expect(await theCreateButton()).toBeDisabled();

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		await userEvent.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: "Delete",
			}),
		);

		await waitFor(async () => expect(await theCreateButton()).toBeEnabled());
		expect(screen.queryByText(/Community includes/)).toBeNull();
	});
});

describe("Deleting a report from the list", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-08 @slice-08 @driving_port @kpi-OUT-5878-guardrail-quick-deletes @contract-shape:bounded-change
	it("asks first, deletes the report, takes it off the list and reports one report deleted", async () => {
		const reportService = aReportService({
			getReports: vi
				.fn()
				.mockResolvedValueOnce(aReportList([aListedReport()]))
				.mockResolvedValue(aReportList([])),
		});
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, { reportService });

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		const dialog = await screen.findByRole("dialog");
		expect(dialog).toHaveTextContent(
			`Do you really want to delete ${ENGAGEMENT_START}?`,
		);
		await userEvent.click(
			within(dialog).getByRole("button", { name: "Delete" }),
		);

		await waitFor(() =>
			expect(reportService.deleteReport).toHaveBeenCalledWith(
				LIGHTSPEED,
				ENGAGEMENT_START_ID,
			),
		);
		expect(await screen.findByText(EMPTY)).toBeVisible();
		expect(reportUsage).toHaveBeenCalledWith({ name: "ReportDeleted" });
		expect(reportUsage).toHaveBeenCalledTimes(1);
	});

	// @us-08 @slice-08 @error @contract-shape:unbounded-preservation
	it("keeps the report and reports nothing when the delete is cancelled", async () => {
		const reportService = aReportService();
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, { reportService });

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		await userEvent.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: "Cancel",
			}),
		);

		expect(reportService.deleteReport).not.toHaveBeenCalled();
		expect(screen.getByRole("link", { name: ENGAGEMENT_START })).toBeVisible();
		expect(reportUsage).not.toHaveBeenCalled();
	});

	// @us-08 @slice-08 @error @contract-shape:pure-function
	it("warns that a Then reaching past the kept history can no longer be rebuilt", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi
					.fn()
					.mockResolvedValue(
						aReportList([
							aListedReport({ thenAndNow: { thenRebuildable: false } }),
						]),
					),
			}),
		});

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));

		expect(await screen.findByRole("dialog")).toHaveTextContent(
			CANNOT_BE_REBUILT,
		);
	});

	// @us-08 @slice-08 @boundary @contract-shape:pure-function
	it("does not warn about rebuilding a Then that can still be rebuilt", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
		});

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		const dialog = await screen.findByRole("dialog");

		expect(dialog).toHaveTextContent(
			`Do you really want to delete ${ENGAGEMENT_START}?`,
		);
		expect(dialog).not.toHaveTextContent(/can no longer be rebuilt/);
	});

	// @us-08 @slice-08 @error @contract-shape:unbounded-preservation
	it("keeps the report listed and reports nothing when the delete fails", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				deleteReport: vi
					.fn()
					.mockRejectedValue(new Error("the network dropped")),
			}),
		});

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		await userEvent.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: "Delete",
			}),
		);

		expect(await screen.findByTestId("delete-error-alert")).toHaveTextContent(
			"This report couldn't be deleted. Try again.",
		);
		// The open dialog hides the list from role queries, so the row is found by its text.
		expect(screen.getByText(ENGAGEMENT_START)).toBeVisible();
		expect(reportUsage).not.toHaveBeenCalled();
	});

	// @us-08 @slice-08 @error @contract-shape:unbounded-preservation
	it("lets Delete be pressed again after a delete failed", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				deleteReport: vi
					.fn()
					.mockRejectedValue(new Error("the network dropped")),
			}),
		});

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		const dialog = await screen.findByRole("dialog");
		await userEvent.click(
			within(dialog).getByRole("button", { name: "Delete" }),
		);

		await screen.findByTestId("delete-error-alert");
		expect(
			within(dialog).getByRole("button", { name: "Delete" }),
		).toBeEnabled();
	});

	// @us-08 @slice-08 @boundary @contract-shape:bounded-change
	it("sends one delete however quickly Delete is pressed twice", async () => {
		const held = deferred<void>();
		const reportService = aReportService({
			deleteReport: vi.fn().mockReturnValue(held.promise),
		});
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, { reportService });

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		const confirm = within(await screen.findByRole("dialog")).getByRole(
			"button",
			{ name: "Delete" },
		);
		fireEvent.click(confirm);
		fireEvent.click(confirm);
		await act(async () => held.resolve());

		await waitFor(() =>
			expect(reportUsage).toHaveBeenCalledWith({ name: "ReportDeleted" }),
		);
		expect(reportService.deleteReport).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledTimes(1);
	});

	// @us-08 @slice-08 @error @contract-shape:bounded-change
	// A dialog that looked cancelled must not turn out to have deleted the report after all.
	it("holds the question open while the delete is on its way, whatever is pressed", async () => {
		const held = deferred<void>();
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				deleteReport: vi.fn().mockReturnValue(held.promise),
			}),
		});

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		const dialog = await screen.findByRole("dialog");
		await userEvent.click(
			within(dialog).getByRole("button", { name: "Delete" }),
		);
		expect(
			within(dialog).getByRole("button", { name: "Delete" }),
		).toBeDisabled();
		await userEvent.click(
			within(dialog).getByRole("button", { name: "Cancel" }),
		);
		await userEvent.keyboard("{Escape}");

		expect(screen.getByRole("dialog")).toBeVisible();
		expect(reportUsage).not.toHaveBeenCalled();

		await act(async () => held.resolve());

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		expect(reportUsage).toHaveBeenCalledWith({ name: "ReportDeleted" });
	});

	// @us-08 @slice-08 @error @contract-shape:bounded-change
	// Somebody else got there first: the report is gone either way, but nobody here deleted it.
	it("takes a report somebody else already deleted off the list and reports nothing", async () => {
		const reportService = aReportService({
			getReports: vi
				.fn()
				.mockResolvedValueOnce(aReportList([aListedReport()]))
				.mockResolvedValue(aReportList([])),
			deleteReport: vi.fn().mockRejectedValue(aRefusal(404, "not-found")),
		});
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, { reportService });

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		await userEvent.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: "Delete",
			}),
		);

		expect(await screen.findByText(EMPTY)).toBeVisible();
		expect(screen.queryByRole("dialog")).toBeNull();
		expect(reportUsage).not.toHaveBeenCalled();
	});
});

describe("The Reports list on a slow or failing connection", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	// An empty area while the list is on its way would read as "no reports".
	it("shows a spinner, never the empty list, while the list is on its way", async () => {
		const held = deferred<ReturnType<typeof aReportList>>();
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi.fn().mockReturnValue(held.promise),
			}),
		});

		expect(await screen.findByRole("progressbar")).toBeVisible();
		expect(screen.queryByText(EMPTY)).toBeNull();
	});

	// @us-01 @us-08 @slice-01 @boundary @contract-shape:pure-function
	// Until the list arrives nobody knows whether the Community cap is reached.
	it("holds Create Report until the list says whether another report may be created", async () => {
		const held = deferred<ReturnType<typeof aReportList>>();
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi.fn().mockReturnValue(held.promise),
			}),
		});

		expect(await theCreateButton()).toBeDisabled();

		await act(async () => held.resolve(aReportList([aListedReport()])));

		expect(await theCreateButton()).toBeEnabled();
	});

	// @us-01 @slice-01 @error @ac-1.8 @contract-shape:pure-function
	it("says the list could not be loaded and offers Retry, never the empty list", async () => {
		const getReports = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(aReportList([aListedReport()]));
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({ getReports }),
		});

		const retry = await screen.findByRole("button", { name: "Retry" });
		expect(screen.getByText(LIST_COULD_NOT_LOAD)).toBeVisible();
		expect(screen.queryByText(EMPTY)).toBeNull();

		await userEvent.click(retry);

		expect(
			await screen.findByRole("link", { name: ENGAGEMENT_START }),
		).toBeVisible();
	});

	it("says it is busy while the list is on its way, and no longer once it is shown", async () => {
		const held = deferred<ReturnType<typeof aReportList>>();
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi.fn().mockReturnValue(held.promise),
			}),
		});

		await screen.findByRole("progressbar");
		expect(screen.getByTestId("reports-list")).toHaveAttribute(
			"aria-busy",
			"true",
		);

		await act(async () => held.resolve(aReportList([aListedReport()])));
		await screen.findByRole("link", { name: ENGAGEMENT_START });

		expect(screen.getByTestId("reports-list")).not.toHaveAttribute("aria-busy");
	});
});

describe("The columns of the Reports list", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	it("names each report's Type and the day it was created", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi.fn().mockResolvedValue(
					aReportList([
						aListedReport({
							name: "Engagement start",
							createdOn: "2026-08-01",
						}),
					]),
				),
			}),
		});

		const row = (
			await screen.findByRole("link", { name: "Engagement start" })
		).closest('[role="row"]') as HTMLElement;

		expect(row).toHaveTextContent("Then & Now");
		expect(row).toHaveTextContent(new Date(2026, 7, 1).toLocaleDateString());
	});

	it("names a Type this version does not know as not supported", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi
					.fn()
					.mockResolvedValue(aReportList([aListedReportOfALaterType()])),
			}),
		});

		const row = (
			await screen.findByRole("link", {
				name: aListedReportOfALaterType().name,
			})
		).closest('[role="row"]') as HTMLElement;

		expect(row).toHaveTextContent(NOT_SUPPORTED_IN_THIS_VERSION);
	});
});

describe("Editing a report from the list", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	const theEditButtonOf = async (name: string) => {
		const row = (await screen.findByRole("link", { name })).closest(
			'[role="row"]',
		) as HTMLElement;
		return within(row).queryByRole("button", { name: `Edit ${name}` });
	};

	const theNowLength = async () =>
		within(await screen.findByRole("dialog")).findByRole("spinbutton", {
			name: "Now length in days",
		});

	it("offers an admin Edit on a Then & Now report, after Export PDF and before its Delete", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
		});

		const edit = await theEditButtonOf(ENGAGEMENT_START);

		expect(edit).toBeEnabled();
		const row = edit?.closest('[role="row"]') as HTMLElement;
		expect(
			within(row)
				.getAllByRole("button")
				.map((button) => button.getAttribute("aria-label")),
		).toEqual([
			exportAsPdfLabelOf(ENGAGEMENT_START),
			`Edit ${ENGAGEMENT_START}`,
			`Delete ${ENGAGEMENT_START}`,
		]);
	});

	it("offers a reader no Edit", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
			viewer: "reader",
		});

		expect(await theEditButtonOf(ENGAGEMENT_START)).toBeNull();
		expect(screen.queryByRole("button", { name: /^Edit/ })).toBeNull();
	});

	// An instance rolled back from a later version cannot show, and so cannot change, that version's reports.
	it("offers only Delete on a report of a type this version cannot show", async () => {
		const later = aListedReportOfALaterType();
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi
					.fn()
					.mockResolvedValue(aReportList([aListedReport(), later])),
			}),
		});

		expect(await theEditButtonOf(later.name)).toBeNull();
		expect(await theDeleteButtonOf(later.name)).toBeEnabled();
		expect(await theEditButtonOf(ENGAGEMENT_START)).toBeEnabled();
	});

	it("reads the report and opens Edit report on Now, with the report's values", async () => {
		const reportService = aReportService({
			getReport: vi.fn().mockResolvedValue(
				theEngagementStartReport({
					thenAndNow: {
						...theEngagementStartReport().thenAndNow,
						nowWindow: {
							start: "2026-09-03",
							end: "2026-10-02",
							lengthDays: 30,
						},
					},
				}),
			),
		});
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, { reportService });

		await userEvent.click(
			(await theEditButtonOf(ENGAGEMENT_START)) as HTMLElement,
		);

		const wizard = await screen.findByRole("dialog");
		expect(wizard).toHaveTextContent("Edit report");
		expect(await theNowLength()).toHaveValue(30);
		expect(reportService.getReport).toHaveBeenCalledWith(
			LIGHTSPEED,
			ENGAGEMENT_START_ID,
		);
	});

	it("saves once over the report as it was read, and reads the list again", async () => {
		const reportService = aReportService({
			getReport: vi
				.fn()
				.mockResolvedValue(
					theEngagementStartReport({ concurrencyToken: "token-7" }),
				),
		});
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, { reportService });
		await waitFor(() =>
			expect(reportService.getReports).toHaveBeenCalledTimes(1),
		);

		await userEvent.click(
			(await theEditButtonOf(ENGAGEMENT_START)) as HTMLElement,
		);
		await theNowLength();
		await userEvent.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: "Next",
			}),
		);
		await userEvent.click(
			await within(await screen.findByRole("dialog")).findByRole("button", {
				name: "Save",
			}),
		);

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		expect(reportService.editReport).toHaveBeenCalledTimes(1);
		expect(reportService.editReport).toHaveBeenCalledWith(
			LIGHTSPEED,
			ENGAGEMENT_START_ID,
			expect.objectContaining({ concurrencyToken: "token-7" }),
		);
		await waitFor(() =>
			expect(reportService.getReports).toHaveBeenCalledTimes(2),
		);
	});

	it("reads the report once however quickly Edit is pressed twice, and shows it is on its way", async () => {
		const held = deferred<ReturnType<typeof theEngagementStartReport>>();
		const reportService = aReportService({
			getReport: vi.fn().mockReturnValue(held.promise),
		});
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, { reportService });

		const edit = (await theEditButtonOf(ENGAGEMENT_START)) as HTMLElement;
		fireEvent.click(edit);
		fireEvent.click(edit);

		expect(await within(edit).findByRole("progressbar")).toBeVisible();
		expect(edit).toBeDisabled();
		await act(async () => held.resolve(theEngagementStartReport()));

		expect(await screen.findByRole("dialog")).toHaveTextContent("Edit report");
		expect(reportService.getReport).toHaveBeenCalledTimes(1);
	});

	// Somebody else deleted it after the list was read.
	it("says a report that is gone no longer exists, opens nothing and reads the list again", async () => {
		const reportService = aReportService({
			getReports: vi
				.fn()
				.mockResolvedValueOnce(aReportList([aListedReport()]))
				.mockResolvedValue(aReportList([])),
			getReport: vi.fn().mockRejectedValue(aRefusal(404, "not-found")),
		});
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, { reportService });

		await userEvent.click(
			(await theEditButtonOf(ENGAGEMENT_START)) as HTMLElement,
		);

		expect(await screen.findByText(NO_LONGER_EXISTS)).toBeVisible();
		expect(screen.queryByRole("dialog")).toBeNull();
		expect(await screen.findByText(EMPTY)).toBeVisible();
	});

	it("forgets a report that is gone, keeping the others it has read", async () => {
		const another = reportQueryKeyOf(LIGHTSPEED, 7);
		const { queryClient } = renderWithReports(
			<ReportsTab owner={LIGHTSPEED} />,
			{
				reportService: aReportService({
					getReport: vi.fn().mockRejectedValue(aRefusal(404, "not-found")),
				}),
			},
		);
		queryClient.setQueryData(another, theEngagementStartReport({ id: 7 }));

		await userEvent.click(
			(await theEditButtonOf(ENGAGEMENT_START)) as HTMLElement,
		);

		expect(await screen.findByText(NO_LONGER_EXISTS)).toBeVisible();
		expect(
			queryClient.getQueryState(
				reportQueryKeyOf(LIGHTSPEED, ENGAGEMENT_START_ID),
			),
		).toBeUndefined();
		expect(queryClient.getQueryState(another)?.isInvalidated).toBe(false);
	});

	it("lets what went wrong be dismissed, and takes it back when Edit is pressed again", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReport: vi
					.fn()
					.mockRejectedValueOnce(new Error("the network dropped"))
					.mockRejectedValueOnce(new Error("the network dropped"))
					.mockResolvedValue(theEngagementStartReport()),
			}),
		});
		const edit = async () =>
			userEvent.click((await theEditButtonOf(ENGAGEMENT_START)) as HTMLElement);

		await edit();
		await userEvent.click(
			within(
				(await screen.findByText(COULD_NOT_LOAD)).closest(
					'[role="alert"]',
				) as HTMLElement,
			).getByRole("button", { name: "Close" }),
		);
		expect(screen.queryByText(COULD_NOT_LOAD)).toBeNull();

		await edit();
		expect(await screen.findByText(COULD_NOT_LOAD)).toBeVisible();
		await edit();

		await userEvent.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: "Cancel",
			}),
		);

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		expect(screen.queryByText(COULD_NOT_LOAD)).toBeNull();
	});

	it("says a report that could not be read could not be loaded, and opens nothing", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReport: vi.fn().mockRejectedValue(new Error("the network dropped")),
			}),
		});

		await userEvent.click(
			(await theEditButtonOf(ENGAGEMENT_START)) as HTMLElement,
		);

		expect(await screen.findByText(COULD_NOT_LOAD)).toBeVisible();
		expect(screen.queryByRole("dialog")).toBeNull();
		expect(screen.getByRole("link", { name: ENGAGEMENT_START })).toBeVisible();
	});
});

describe("Deleting one report after another", () => {
	const SINCE_AUGUST = "Since August";
	const SINCE_AUGUST_ID = 7;
	const aSecondReport = () =>
		aListedReport({ id: SINCE_AUGUST_ID, name: SINCE_AUGUST });

	const confirmTheDelete = async () =>
		userEvent.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: "Delete",
			}),
		);

	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	it("lets a second report be deleted once the first is gone", async () => {
		const reportService = aReportService({
			getReports: vi
				.fn()
				.mockResolvedValueOnce(aReportList([aListedReport(), aSecondReport()]))
				.mockResolvedValue(aReportList([aSecondReport()])),
		});
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, { reportService });

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		await confirmTheDelete();
		await waitFor(() =>
			expect(screen.queryByRole("link", { name: ENGAGEMENT_START })).toBeNull(),
		);
		await userEvent.click(await theDeleteButtonOf(SINCE_AUGUST));
		const confirm = within(await screen.findByRole("dialog")).getByRole(
			"button",
			{ name: "Delete" },
		);

		expect(confirm).toBeEnabled();
		await userEvent.click(confirm);
		await waitFor(() =>
			expect(reportService.deleteReport).toHaveBeenLastCalledWith(
				LIGHTSPEED,
				SINCE_AUGUST_ID,
			),
		);
	});

	it("keeps the other reports on screen, dimmed, while the list is read again after a delete", async () => {
		const reread = deferred<ReturnType<typeof aReportList>>();
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi
					.fn()
					.mockResolvedValueOnce(
						aReportList([aListedReport(), aSecondReport()]),
					)
					.mockReturnValue(reread.promise),
			}),
		});

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		await confirmTheDelete();
		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());

		expect(screen.getByTestId("reports-list")).toHaveAttribute(
			"aria-busy",
			"true",
		);
		expect(screen.getByRole("link", { name: SINCE_AUGUST })).toBeVisible();
	});

	it("says a delete the server refused couldn't be done, rather than taking the report off the list", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				deleteReport: vi.fn().mockRejectedValue(aRefusal(403, "forbidden")),
			}),
		});

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		await confirmTheDelete();

		expect(await screen.findByTestId("delete-error-alert")).toHaveTextContent(
			"This report couldn't be deleted. Try again.",
		);
		expect(screen.getByRole("dialog")).toBeVisible();
	});

	it("asks before deleting a report of a type this version cannot show, warning of nothing", async () => {
		const later = aListedReportOfALaterType();
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([later])),
			}),
		});

		await userEvent.click(await theDeleteButtonOf(later.name));
		const dialog = await screen.findByRole("dialog");

		expect(dialog).toHaveTextContent(
			`Do you really want to delete ${later.name}?`,
		);
		expect(dialog).not.toHaveTextContent(/can no longer be rebuilt/);
	});

	it("forgets only the deleted report and reads only the list again", async () => {
		const theDeleted = reportQueryKeyOf(LIGHTSPEED, ENGAGEMENT_START_ID);
		const another = reportQueryKeyOf(LIGHTSPEED, SINCE_AUGUST_ID);
		const anotherOwnersList = reportsQueryKeyOf(APOLLO);
		const { queryClient } = renderWithReports(
			<ReportsTab owner={LIGHTSPEED} />,
			{ reportService: aReportService() },
		);
		queryClient.setQueryData(theDeleted, theEngagementStartReport());
		queryClient.setQueryData(
			another,
			theEngagementStartReport({ id: SINCE_AUGUST_ID }),
		);
		queryClient.setQueryData(anotherOwnersList, aReportList([]));

		await userEvent.click(await theDeleteButtonOf(ENGAGEMENT_START));
		await confirmTheDelete();

		await waitFor(() =>
			expect(queryClient.getQueryData(theDeleted)).toBeUndefined(),
		);
		expect(queryClient.getQueryState(another)?.isInvalidated).toBe(false);
		expect(queryClient.getQueryState(anotherOwnersList)?.isInvalidated).toBe(
			false,
		);
	});
});

describe("The layout of the Reports list", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
		localStorage.clear();
	});

	afterEach(() => localStorage.clear());

	it("heads the row actions Actions, a column that cannot be sorted", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
		});
		await screen.findByRole("link", { name: ENGAGEMENT_START });

		const sortButtonOf = (column: string) =>
			within(
				screen.getByRole("columnheader", { name: column }),
			).queryByLabelText("Sort", { selector: "button" });

		expect(sortButtonOf("Actions")).toBeNull();
		expect(sortButtonOf("Created")).not.toBeNull();
	});

	it("sorts the reports by name", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi
					.fn()
					.mockResolvedValue(
						aReportList([
							aListedReport({ id: 1, name: "Zenith" }),
							aListedReport({ id: 2, name: "Apex" }),
						]),
					),
			}),
		});
		await screen.findByRole("link", { name: "Zenith" });

		await userEvent.click(screen.getByRole("columnheader", { name: "Name" }));

		expect(screen.getAllByRole("link").map((link) => link.textContent)).toEqual(
			["Apex", "Zenith"],
		);
	});

	it("follows a Team's saved layout, which may hide Type and Created but never Name or the actions", async () => {
		localStorage.setItem(
			"lighthouse:datagrid:reports-team:state",
			JSON.stringify({
				columnVisibilityModel: {
					name: false,
					templateKey: false,
					createdOn: false,
					actions: false,
				},
			}),
		);
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService(),
		});
		await screen.findByRole("link", { name: ENGAGEMENT_START });

		expect(screen.getByRole("columnheader", { name: "Name" })).toBeVisible();
		expect(screen.getByRole("columnheader", { name: "Actions" })).toBeVisible();
		expect(screen.queryByRole("columnheader", { name: "Type" })).toBeNull();
		expect(screen.queryByRole("columnheader", { name: "Created" })).toBeNull();
	});
});
