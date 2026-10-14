import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	APOLLO,
	aListedReport,
	aListedReportOfALaterType,
	aReportList,
	aReportService,
	CANNOT_BE_REBUILT,
	CAP_SENTENCE,
	defaultTerms,
	ENGAGEMENT_START,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	LIST_COULD_NOT_LOAD,
	NOT_SUPPORTED_IN_THIS_VERSION,
	renderWithReports,
} from "../../../tests/ReportsTestKit";
import ReportsTab from "./ReportsTab";

/**
 * The Reports tab of a Team or a Portfolio: the table of its reports, who may create and delete them, the
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
	it.skip("lets everybody create and delete when roles are not enforced", async () => {
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
	it.skip("lists a report of a type this version cannot show as not supported, and still lets an admin delete it", async () => {
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
	it.skip("disables Create Report at the cap and says why, in the instance's word for a Team", async () => {
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
	it.skip("explains the disabled Create Report with the Premium licence tooltip", async () => {
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
	it.skip("says nothing about the cap below it", async () => {
		renderWithReports(<ReportsTab owner={LIGHTSPEED} />, {
			reportService: aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
			}),
		});

		expect(await theCreateButton()).toBeEnabled();
		expect(screen.queryByText(/Community includes/)).toBeNull();
	});

	// @us-08 @slice-08 @contract-shape:bounded-change
	it.skip("enables Create Report again once the report is deleted", async () => {
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
	it.skip("asks first, deletes the report, takes it off the list and reports one report deleted", async () => {
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
	});

	// @us-08 @slice-08 @error @contract-shape:unbounded-preservation
	it.skip("keeps the report and reports nothing when the delete is cancelled", async () => {
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
	it.skip("warns that a Then reaching past the kept history can no longer be rebuilt", async () => {
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
	it.skip("does not warn about rebuilding a Then that can still be rebuilt", async () => {
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
	it.skip("keeps the report listed and reports nothing when the delete fails", async () => {
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

		expect(await screen.findByTestId("delete-error-alert")).toBeVisible();
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
