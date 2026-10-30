import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IReport } from "../../../models/Reports/Report";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	aRefusal,
	aReportService,
	defaultTerms,
	ENGAGEMENT_START,
	ENGAGEMENT_START_ID,
	enterNumber,
	FROZEN,
	LIGHTSPEED,
	NO_METRIC,
	NOT_CAPTURED,
	next,
	renderWithReports,
	STALE_SAVE,
	theEngagementStartReport,
	theMetricBox,
	theNameField,
	theNowLengthField,
	thePanelTitles,
	theThenEndField,
	theThenLengthField,
	theWizard,
} from "../../../tests/ReportsTestKit";
import { useAllReports } from "../../Reports/useAllReports";
import ReportView from "./ReportView";
import { reportQueryKeyOf } from "./reportOwner";
import { useOwnerReports } from "./useOwnerReports";

/**
 * Edit report reopens the create wizard on the report as it is. The Type and Then are shown but locked -
 * Then stays exactly as it was frozen - while how long Now is, which metrics are shown and the name can be
 * changed. A metric the report never captured cannot be ticked. An edit saved over somebody else's is
 * refused in words, never silently lost or silently winning.
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

const SINCE_AUGUST = "Since August";

const renamed = (): IReport =>
	theEngagementStartReport({ name: SINCE_AUGUST, concurrencyToken: "token-2" });

const openTheEditWizard = async (
	reportService = aReportService({
		getReport: vi
			.fn()
			.mockResolvedValueOnce(theEngagementStartReport())
			.mockResolvedValue(renamed()),
		editReport: vi.fn().mockResolvedValue(renamed()),
	}),
) => {
	renderWithReports(
		<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
		{ reportService, viewer: "admin" },
	);
	await userEvent.click(
		await screen.findByRole("button", { name: "Edit report" }),
	);
	await theWizard();
	return reportService;
};

const back = async () =>
	userEvent.click(
		within(await theWizard()).getByRole("button", { name: "Back" }),
	);

const onToTheLastStep = async () => {
	await theNowLengthField();
	await next();
};

/** The Reports tab's list, mounted beside the report so a refresh of it can be seen. */
const TheReportList = () => {
	useOwnerReports(LIGHTSPEED);
	return null;
};

/** The Reports page's list of every report, mounted beside the report so a refresh of it can be seen. */
const TheReportsPageList = () => {
	useAllReports();
	return null;
};

const theSaveButton = async () =>
	within(await theWizard()).findByRole("button", { name: "Save" });

const renameTo = async (name: string) => {
	await userEvent.clear(await theNameField());
	await userEvent.type(await theNameField(), name);
};

describe("Editing a report", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
	});

	it("opens on Now, the first step an edit can change", async () => {
		await openTheEditWizard();

		expect(await theNowLengthField()).toBeEnabled();
		expect(
			within(await theWizard()).queryByLabelText("Then ends on"),
		).toBeNull();
		expect(
			within(await theWizard()).queryByRole("radio", { name: /Then & Now/ }),
		).toBeNull();
	});

	it("shows the Type locked", async () => {
		await openTheEditWizard();
		await theNowLengthField();
		await back();
		await theThenEndField();
		await back();

		const wizard = await theWizard();

		expect(wizard).toHaveTextContent(FROZEN);
		expect(wizard).not.toHaveTextContent(/template/i);
		expect(
			within(wizard).getByRole("radio", { name: /Then & Now/ }),
		).toBeDisabled();
	});

	it("shows Then as it was frozen, locked", async () => {
		await openTheEditWizard();
		await theNowLengthField();
		await back();

		expect(await theThenEndField()).toHaveValue("2026-07-31");
		expect(await theThenEndField()).toBeDisabled();
		expect(await theThenLengthField()).toBeDisabled();
		expect(await theWizard()).toHaveTextContent(FROZEN);
	});

	it("saves a new name, a new Now and the shown metrics, never Then, over the report as it was read", async () => {
		const reportService = await openTheEditWizard();
		await enterNumber(await theNowLengthField(), 30);
		await next();
		await userEvent.click(await theMetricBox("Work Item Age"));
		await renameTo(SINCE_AUGUST);

		await userEvent.click(await theSaveButton());

		await waitFor(() =>
			expect(reportService.editReport).toHaveBeenCalledWith(
				LIGHTSPEED,
				ENGAGEMENT_START_ID,
				{
					name: SINCE_AUGUST,
					shownMetricKeys: ["cycle-time", "throughput", "wip"],
					concurrencyToken: "token-1",
					thenAndNow: { nowLengthDays: 30 },
				},
			),
		);
		expect(await screen.findByText(SINCE_AUGUST)).toBeVisible();
	});

	it("cannot tick a metric the report never captured, and says why", async () => {
		const report = theEngagementStartReport({
			shownMetricKeys: ["cycle-time"],
		});
		const { "work-item-age": _neverCaptured, ...captured } =
			report.thenAndNow.metrics;
		report.thenAndNow.metrics = captured;
		await openTheEditWizard(
			aReportService({ getReport: vi.fn().mockResolvedValue(report) }),
		);
		await onToTheLastStep();

		expect(await theMetricBox("Work Item Age")).toBeDisabled();
		expect(await theWizard()).toHaveTextContent(NOT_CAPTURED);
		expect(await theMetricBox("Throughput")).toBeEnabled();
	});

	it("cannot save a report showing no metric", async () => {
		const report = theEngagementStartReport({
			shownMetricKeys: ["cycle-time"],
		});
		await openTheEditWizard(
			aReportService({ getReport: vi.fn().mockResolvedValue(report) }),
		);
		await onToTheLastStep();

		await userEvent.click(await theMetricBox("Cycle Time"));

		expect(await theSaveButton()).toBeDisabled();
		expect(await theWizard()).toHaveTextContent(NO_METRIC);
	});

	// Another Team Admin saved a change after this wizard was opened.
	it("says the report was changed by someone else when the save comes too late, keeping the edit", async () => {
		await openTheEditWizard(
			aReportService({
				editReport: vi.fn().mockRejectedValue(aRefusal(409, "conflict")),
			}),
		);
		await onToTheLastStep();
		await renameTo(SINCE_AUGUST);

		await userEvent.click(await theSaveButton());

		expect(
			await within(await theWizard()).findByRole("alert"),
		).toHaveTextContent(STALE_SAVE);
		expect(await theNameField()).toHaveValue(SINCE_AUGUST);
	});

	it("says the report couldn't be saved when the save fails for no reason it names", async () => {
		await openTheEditWizard(
			aReportService({
				editReport: vi.fn().mockRejectedValue(new Error("network down")),
			}),
		);
		await onToTheLastStep();

		await userEvent.click(await theSaveButton());

		expect(
			await within(await theWizard()).findByRole("alert"),
		).toHaveTextContent("The report couldn't be saved. Try again.");
	});

	// Another Team Admin deleted the report after this wizard was opened.
	it("says the report no longer exists when it was deleted before the save", async () => {
		await openTheEditWizard(
			aReportService({
				editReport: vi.fn().mockRejectedValue(aRefusal(404, "not-found")),
			}),
		);
		await onToTheLastStep();

		await userEvent.click(await theSaveButton());

		expect(
			await within(await theWizard()).findByRole("alert"),
		).toHaveTextContent("This report no longer exists.");
	});

	it("says the report couldn't be saved when the edit is not allowed", async () => {
		await openTheEditWizard(
			aReportService({
				editReport: vi.fn().mockRejectedValue(aRefusal(403, "forbidden")),
			}),
		);
		await onToTheLastStep();

		await userEvent.click(await theSaveButton());

		expect(
			await within(await theWizard()).findByRole("alert"),
		).toHaveTextContent("The report couldn't be saved. Try again.");
	});

	// The page shows the report the save answered with, dimmed until it has been read again.
	it("dims the saved report under a spinner while it is read again after the save", async () => {
		const reread = deferred<IReport>();
		await openTheEditWizard(
			aReportService({
				getReport: vi
					.fn()
					.mockResolvedValueOnce(theEngagementStartReport())
					.mockReturnValue(reread.promise),
				editReport: vi.fn().mockResolvedValue(renamed()),
			}),
		);
		await onToTheLastStep();
		await renameTo(SINCE_AUGUST);

		await userEvent.click(await theSaveButton());

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		expect(screen.getByRole("progressbar")).toBeVisible();
		expect(document.querySelector('[data-report-status="ready"]')).toBeNull();
		expect(screen.getByText(SINCE_AUGUST)).toBeInTheDocument();
		expect(thePanelTitles()).not.toHaveLength(0);
		expect(screen.queryByText(ENGAGEMENT_START)).toBeNull();
	});

	// A second edit made before the re-read lands must not be refused as somebody else's change.
	it("edits the saved report, not the one before, when Edit is chosen again", async () => {
		const reportService = await openTheEditWizard(
			aReportService({
				getReport: vi
					.fn()
					.mockResolvedValueOnce(theEngagementStartReport())
					.mockReturnValue(deferred<IReport>().promise),
				editReport: vi.fn().mockResolvedValue(renamed()),
			}),
		);
		await onToTheLastStep();
		await renameTo(SINCE_AUGUST);
		await userEvent.click(await theSaveButton());
		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());

		await userEvent.click(screen.getByRole("button", { name: "Edit report" }));
		await onToTheLastStep();

		expect(await theNameField()).toHaveValue(SINCE_AUGUST);
		await userEvent.click(await theSaveButton());
		await waitFor(() =>
			expect(reportService.editReport).toHaveBeenLastCalledWith(
				LIGHTSPEED,
				ENGAGEMENT_START_ID,
				expect.objectContaining({ concurrencyToken: "token-2" }),
			),
		);
	});

	it("holds the wizard open, with Cancel and Back disabled, while the save is on its way", async () => {
		await openTheEditWizard(
			aReportService({
				getReport: vi.fn().mockResolvedValue(theEngagementStartReport()),
				editReport: vi.fn().mockReturnValue(deferred<IReport>().promise),
			}),
		);
		await onToTheLastStep();

		await userEvent.click(await theSaveButton());
		const wizard = await theWizard();

		expect(
			within(wizard).getByRole("button", { name: "Cancel" }),
		).toBeDisabled();
		expect(within(wizard).getByRole("button", { name: "Back" })).toBeDisabled();
	});

	it("reads again only the saved report and its owner's list", async () => {
		const anotherReport = reportQueryKeyOf(LIGHTSPEED, 7);
		const { queryClient } = renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{
				reportService: aReportService({
					getReport: vi.fn().mockResolvedValue(theEngagementStartReport()),
					editReport: vi.fn().mockResolvedValue(renamed()),
				}),
				viewer: "admin",
			},
		);
		queryClient.setQueryData(
			anotherReport,
			theEngagementStartReport({ id: 7 }),
		);
		await userEvent.click(
			await screen.findByRole("button", { name: "Edit report" }),
		);
		await onToTheLastStep();

		await userEvent.click(await theSaveButton());

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		expect(queryClient.getQueryState(anotherReport)?.isInvalidated).toBe(false);
	});

	it("has the report list read again after the save", async () => {
		const reportService = aReportService({
			getReport: vi.fn().mockResolvedValue(theEngagementStartReport()),
			editReport: vi.fn().mockResolvedValue(renamed()),
		});
		renderWithReports(
			<>
				<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />
				<TheReportList />
			</>,
			{ reportService, viewer: "admin" },
		);
		await waitFor(() =>
			expect(reportService.getReports).toHaveBeenCalledTimes(1),
		);
		await userEvent.click(
			await screen.findByRole("button", { name: "Edit report" }),
		);
		await onToTheLastStep();

		await userEvent.click(await theSaveButton());

		await waitFor(() =>
			expect(reportService.getReports).toHaveBeenCalledTimes(2),
		);
	});

	it("has the Reports page's list of every report read again after the save", async () => {
		const reportService = aReportService({
			getReport: vi.fn().mockResolvedValue(theEngagementStartReport()),
			editReport: vi.fn().mockResolvedValue(renamed()),
		});
		renderWithReports(
			<>
				<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />
				<TheReportsPageList />
			</>,
			{ reportService, viewer: "admin" },
		);
		await waitFor(() =>
			expect(reportService.getAllReports).toHaveBeenCalledTimes(1),
		);
		await userEvent.click(
			await screen.findByRole("button", { name: "Edit report" }),
		);
		await onToTheLastStep();

		await userEvent.click(await theSaveButton());

		await waitFor(() =>
			expect(reportService.getAllReports).toHaveBeenCalledTimes(2),
		);
	});
});

describe("Editing does not count as opening the report again", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		terms.current = { ...defaultTerms };
		vi.useFakeTimers({ shouldAdvanceTime: true });
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("reports the report opened once, however often it is read again after edits", async () => {
		await openTheEditWizard();
		await act(() => vi.advanceTimersByTimeAsync(5_100));
		await onToTheLastStep();
		await renameTo(SINCE_AUGUST);

		await userEvent.click(await theSaveButton());
		await screen.findByText(SINCE_AUGUST);
		await act(() => vi.advanceTimersByTimeAsync(10_000));

		expect(
			reportUsage.mock.calls.filter(([use]) => use.name === "ReportOpened"),
		).toHaveLength(1);
	});
});
