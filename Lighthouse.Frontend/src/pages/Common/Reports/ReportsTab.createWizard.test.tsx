import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IReportOwner } from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	ALL_FOUR_METRICS,
	APOLLO,
	aRefusal,
	aReportList,
	aReportService,
	aTemplateList,
	CAP_SENTENCE,
	CUTOFF_REFUSAL,
	chooseThenEnd,
	defaultTerms,
	ENGAGEMENT_START_ID,
	enterNumber,
	LIGHTSPEED,
	NO_METRIC,
	next,
	renderWithReports,
	THEN_TOO_SHORT,
	TODAY_ON_THE_INSTANCE,
	TYPES_COULD_NOT_LOAD,
	theMetricBox,
	theNameField,
	theNowLengthField,
	thenNeedsAtLeast,
	theThenEndField,
	theThenLengthField,
	theWizard,
} from "../../../tests/ReportsTestKit";
import ReportsTab from "./ReportsTab";

/**
 * Creating a report is a wizard: the Type, then Then, then Now, then the metrics and the name. Each step
 * says in words what the numbers entered will mean, refuses what the server would refuse before anything
 * is sent, and the last step creates the report and opens it. The examples are set on 2 Oct 2026, with
 * finished Work Items kept back to 2 Oct 2025.
 *
 * The date field is driven as a plain date input; if the wizard uses a date picker instead, only the step
 * in the test kit that picks the date changes.
 */

const { reportUsage, terms, mockNavigate } = vi.hoisted(() => ({
	reportUsage: vi.fn(),
	terms: { current: {} as Record<string, string> },
	mockNavigate: vi.fn(),
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

vi.mock("react-router", async () => ({
	...(await vi.importActual("react-router")),
	useNavigate: () => mockNavigate,
}));

const THEN_AND_NOW_CARD = /Then & Now/;
const CARD_LINE = "Set today's flow against a window that stays as it was.";
const CREATE = "Create";

const openTheWizard = async (
	reportService = aReportService({
		getReports: vi.fn().mockResolvedValue(aReportList([])),
	}),
	owner: IReportOwner = LIGHTSPEED,
) => {
	renderWithReports(<ReportsTab owner={owner} />, { reportService, owner });
	await userEvent.click(
		await screen.findByRole("button", { name: "Create Report" }),
	);
	await theWizard();
	return reportService;
};

const onToTheLastStep = async () => {
	await next();
	await next();
	await next();
};

const theCreateButton = async () =>
	within(await theWizard()).findByRole("button", { name: CREATE });

describe("Step one: the Type of report", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		mockNavigate.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-01 @slice-01 @driving_port @contract-shape:pure-function
	it("offers Then & Now as the one Type, already chosen, saying what it is for", async () => {
		await openTheWizard();

		const card = within(await theWizard()).getByRole("radio", {
			name: THEN_AND_NOW_CARD,
		});

		expect(card).toBeChecked();
		expect(await theWizard()).toHaveTextContent(CARD_LINE);
		expect(await theWizard()).not.toHaveTextContent(/template/i);
	});
});

describe("Step two: Then", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		mockNavigate.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-01 @slice-01 @contract-shape:pure-function
	it("starts as the 90 days ending today and says which days that is", async () => {
		await openTheWizard();
		await next();

		expect(await theThenEndField()).toHaveValue("2026-10-02");
		expect(await theThenLengthField()).toHaveValue(90);
		expect(await theWizard()).toHaveTextContent("= 5 Jul – 2 Oct 2026");
		expect(await theWizard()).not.toHaveTextContent(/frozen/);
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it.skip("says the days of a Then that ended in the past", async () => {
		await openTheWizard();
		await next();

		await chooseThenEnd("2026-07-31");

		expect(await theWizard()).toHaveTextContent("= 3 May – 31 Jul 2026");
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it.skip("says the days of a Then of a custom length", async () => {
		await openTheWizard();
		await next();

		await chooseThenEnd("2026-09-13");
		await enterNumber(await theThenLengthField(), 45);

		expect(await theWizard()).toHaveTextContent("= 31 Jul – 13 Sep 2026");
	});

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it.skip("cannot end in the future", async () => {
		await openTheWizard();
		await next();

		expect(await theThenEndField()).toHaveAttribute("max", "2026-10-02");
		await chooseThenEnd("2026-10-03");

		expect(await theWizard()).toHaveTextContent("Then cannot end after today.");
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeDisabled();
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it.skip("offers a Team's Then as 30 or 90 days with one click", async () => {
		await openTheWizard();
		await next();

		await userEvent.click(
			within(await theWizard()).getByRole("button", { name: "30 days" }),
		);

		expect(await theThenLengthField()).toHaveValue(30);
		expect(
			within(await theWizard()).getByRole("button", { name: "90 days" }),
		).toBeVisible();
		expect(
			within(await theWizard()).queryByRole("button", { name: "180 days" }),
		).toBeNull();
	});

	// @us-06 @slice-06 @contract-shape:pure-function
	it.skip("offers a Portfolio's Then as 90 or 180 days with one click", async () => {
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
			}),
			APOLLO,
		);
		await next();

		await userEvent.click(
			within(await theWizard()).getByRole("button", { name: "180 days" }),
		);

		expect(await theThenLengthField()).toHaveValue(180);
		expect(
			within(await theWizard()).getByRole("button", { name: "90 days" }),
		).toBeVisible();
		expect(
			within(await theWizard()).queryByRole("button", { name: "30 days" }),
		).toBeNull();
	});

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it.skip("refuses a Then starting before the oldest kept day, naming that day, and goes no further", async () => {
		await openTheWizard();
		await next();

		await chooseThenEnd("2025-11-15");

		expect(await theWizard()).toHaveTextContent(CUTOFF_REFUSAL);
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeDisabled();
	});

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it.skip("refuses a Then shorter than 14 days and goes no further", async () => {
		await openTheWizard();
		await next();

		await enterNumber(await theThenLengthField(), 10);

		expect(await theWizard()).toHaveTextContent(THEN_TOO_SHORT);
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeDisabled();
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it.skip("accepts a Then of exactly 14 days", async () => {
		await openTheWizard();
		await next();

		await enterNumber(await theThenLengthField(), 14);

		expect(await theWizard()).not.toHaveTextContent(THEN_TOO_SHORT);
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeEnabled();
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	// The shortest window is the server's to say; the wizard holds no number of its own.
	it.skip("takes the shortest Then it allows from the server", async () => {
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				getTemplates: vi
					.fn()
					.mockResolvedValue(aTemplateList({ minimumLengthDays: 21 })),
			}),
		);
		await next();

		await enterNumber(await theThenLengthField(), 20);
		expect(await theWizard()).toHaveTextContent(thenNeedsAtLeast(21));

		await enterNumber(await theThenLengthField(), 21);
		expect(await theWizard()).not.toHaveTextContent(thenNeedsAtLeast(21));
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeEnabled();
	});

	// @us-02 @slice-02 @error @contract-shape:pure-function
	// A Team keeping 180 days has a later oldest kept day; the refusal names the day the server answered.
	it.skip("names the oldest kept day the server answers when Then starts before it", async () => {
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				getTemplates: vi
					.fn()
					.mockResolvedValue(aTemplateList({ earliestStart: "2026-04-05" })),
			}),
		);
		await next();

		await chooseThenEnd("2026-05-31");

		expect(await theWizard()).toHaveTextContent(
			"The Then window must start on or after 5 Apr 2026 — older finished Work Items are no longer kept.",
		);
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeDisabled();
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it.skip("lets Then start on any day when the Team keeps every finished Work Item", async () => {
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				getTemplates: vi
					.fn()
					.mockResolvedValue(aTemplateList({ earliestStart: null })),
			}),
		);
		await next();

		await chooseThenEnd("2025-01-31");

		expect(await theWizard()).toHaveTextContent("= 3 Nov 2024 – 31 Jan 2025");
		expect(await theWizard()).not.toHaveTextContent(/no longer kept/);
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeEnabled();
	});
});

describe("The wizard on a slow or failing connection", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		mockNavigate.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	// Today is the instance's day; until it arrives the wizard has no day to start Then from.
	it("draws no dates and goes no further while the report types are on their way", async () => {
		const held = deferred<ReturnType<typeof aTemplateList>>();
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				getTemplates: vi.fn().mockReturnValue(held.promise),
			}),
		);

		const wizard = await theWizard();
		expect(within(wizard).getByRole("progressbar")).toBeVisible();
		expect(within(wizard).getByRole("button", { name: "Next" })).toBeDisabled();
		expect(wizard).not.toHaveTextContent(/\d{4}/);

		await act(async () => held.resolve(aTemplateList()));
		await next();

		expect(await theThenEndField()).toHaveValue(TODAY_ON_THE_INSTANCE);
	});

	// @us-01 @slice-01 @error @contract-shape:pure-function
	it("says the report types could not be loaded and offers Retry, going no further", async () => {
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				getTemplates: vi
					.fn()
					.mockRejectedValueOnce(new Error("the network dropped"))
					.mockResolvedValue(aTemplateList()),
			}),
		);

		const alert = await within(await theWizard()).findByRole("alert");
		expect(alert).toHaveTextContent(TYPES_COULD_NOT_LOAD);
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeDisabled();

		await userEvent.click(within(alert).getByRole("button", { name: "Retry" }));

		await waitFor(async () =>
			expect(
				within(await theWizard()).getByRole("button", { name: "Next" }),
			).toBeEnabled(),
		);
		expect(
			within(await theWizard()).queryByText(TYPES_COULD_NOT_LOAD),
		).toBeNull();
	});
});

describe("Step three: Now", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		mockNavigate.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-07 @slice-07 @contract-shape:pure-function
	it.skip("is as long as Then until changed, always ending today", async () => {
		await openTheWizard();
		await next();
		await enterNumber(await theThenLengthField(), 45);
		await next();

		expect(await theNowLengthField()).toHaveValue(45);
		expect(await theWizard()).toHaveTextContent(
			"= the last 45 days, always ending today",
		);
	});

	// @us-07 @slice-07 @error @contract-shape:pure-function
	it.skip("refuses a Now shorter than 14 days and goes no further", async () => {
		await openTheWizard();
		await next();
		await next();

		await enterNumber(await theNowLengthField(), 13);

		expect(await theWizard()).toHaveTextContent("Now needs at least 14 days.");
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeDisabled();
	});

	// @us-07 @slice-07 @error @contract-shape:pure-function
	it.skip("refuses a Now reaching back past the oldest kept day and goes no further", async () => {
		await openTheWizard();
		await next();
		await next();

		await enterNumber(await theNowLengthField(), 367);

		expect(await theWizard()).toHaveTextContent(
			"The Now window must start on or after 2 Oct 2025 — older finished Work Items are no longer kept.",
		);
		expect(
			within(await theWizard()).getByRole("button", { name: "Next" }),
		).toBeDisabled();
	});
});

describe("Step four: metrics and name", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		mockNavigate.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-04 @slice-04 @contract-shape:pure-function
	it.skip("ticks all four metrics, named in the instance's words", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.CYCLE_TIME]: "Lead Time",
		};
		await openTheWizard();
		await onToTheLastStep();

		for (const name of ["Lead Time", "Throughput", "WIP", "Work Item Age"]) {
			expect(await theMetricBox(name)).toBeChecked();
		}
		expect(await theWizard()).not.toHaveTextContent("Cycle Time");
	});

	// @us-04 @slice-04 @error @contract-shape:pure-function
	it.skip("cannot create a report showing no metric", async () => {
		await openTheWizard();
		await onToTheLastStep();

		for (const name of ["Cycle Time", "Throughput", "WIP", "Work Item Age"]) {
			await userEvent.click(await theMetricBox(name));
		}

		expect(await theCreateButton()).toBeDisabled();
		expect(await theWizard()).toHaveTextContent(NO_METRIC);
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it.skip("names the report after its Then until the name is edited", async () => {
		await openTheWizard();
		await next();
		await chooseThenEnd("2026-07-31");
		await next();
		await next();

		expect(await theNameField()).toHaveValue(
			"Then & Now — 90 days to 31 Jul 2026",
		);
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it.skip("keeps an edited name when Then changes afterwards", async () => {
		await openTheWizard();
		await onToTheLastStep();
		await userEvent.clear(await theNameField());
		await userEvent.type(await theNameField(), "Engagement start");

		await userEvent.click(
			within(await theWizard()).getByRole("button", { name: "Back" }),
		);
		await userEvent.click(
			within(await theWizard()).getByRole("button", { name: "Back" }),
		);
		await chooseThenEnd("2026-07-31");
		await onToTheLastStep();

		expect(await theNameField()).toHaveValue("Engagement start");
	});

	// @us-01 @slice-01 @error @contract-shape:pure-function
	it("cannot create a report without a name", async () => {
		await openTheWizard();
		await onToTheLastStep();

		await userEvent.clear(await theNameField());

		expect(await theCreateButton()).toBeDisabled();
	});
});

describe("Creating the report", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		mockNavigate.mockClear();
		terms.current = { ...defaultTerms };
	});

	// @us-01 @slice-01 @driving_port @kpi-OUT-5878-K1-report-created @contract-shape:bounded-change
	it("creates the report as chosen, opens it, and reports one report created with its Type", async () => {
		const reportService = await openTheWizard();
		await next();
		await chooseThenEnd("2026-07-31");
		await next();
		await enterNumber(await theNowLengthField(), 30);
		await next();

		await userEvent.click(await theCreateButton());

		await waitFor(() =>
			expect(mockNavigate).toHaveBeenCalledWith(
				`/teams/${LIGHTSPEED.id}/reports/${ENGAGEMENT_START_ID}`,
			),
		);
		expect(reportService.createReport).toHaveBeenCalledWith(LIGHTSPEED, {
			templateKey: "then-and-now",
			name: "Then & Now — 90 days to 31 Jul 2026",
			shownMetricKeys: ALL_FOUR_METRICS,
			thenAndNow: {
				thenEndDate: "2026-07-31",
				thenLengthDays: 90,
				nowLengthDays: 30,
			},
		});
		expect(reportUsage).toHaveBeenCalledWith({
			name: "ReportCreated",
			reportTemplate: "ThenAndNow",
		});
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	it("shows that the report is being created and holds every input still meanwhile", async () => {
		const held = deferred<number>();
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				createReport: vi.fn().mockReturnValue(held.promise),
			}),
		);
		await onToTheLastStep();

		await userEvent.click(await theCreateButton());

		expect(within(await theWizard()).getByRole("progressbar")).toBeVisible();
		expect(await theNameField()).toBeDisabled();
		expect(await theCreateButton()).toBeDisabled();
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("keeps every input and reports nothing when creating fails", async () => {
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				createReport: vi
					.fn()
					.mockRejectedValue(new Error("the network dropped")),
			}),
		);
		await onToTheLastStep();
		await userEvent.clear(await theNameField());
		await userEvent.type(await theNameField(), "Engagement start");

		await userEvent.click(await theCreateButton());

		expect(await within(await theWizard()).findByRole("alert")).toBeVisible();
		expect(await theNameField()).toHaveValue("Engagement start");
		expect(await theNameField()).toBeEnabled();
		expect(mockNavigate).not.toHaveBeenCalled();
		expect(reportUsage).not.toHaveBeenCalled();
	});

	// @us-08 @slice-08 @error @kpi-OUT-5878-K4-community-cap-reached @contract-shape:unbounded-preservation
	// Somebody else may have created the second report while this wizard was open.
	it.skip("says the cap was reached when the server refuses for it", async () => {
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				createReport: vi
					.fn()
					.mockRejectedValue(aRefusal(403, "report-cap-reached")),
			}),
		);
		await onToTheLastStep();

		await userEvent.click(await theCreateButton());

		expect(
			await within(await theWizard()).findByRole("alert"),
		).toHaveTextContent(CAP_SENTENCE);
		expect(reportUsage).not.toHaveBeenCalled();
	});

	// @us-02 @slice-02 @error @contract-shape:unbounded-preservation
	// The oldest kept day moves on at midnight; a wizard left open overnight can still be refused.
	it.skip("names the oldest kept day when the server refuses Then for starting before it", async () => {
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				createReport: vi
					.fn()
					.mockRejectedValue(aRefusal(400, "then-before-cutoff")),
			}),
		);
		await onToTheLastStep();

		await userEvent.click(await theCreateButton());

		expect(
			await within(await theWizard()).findByRole("alert"),
		).toHaveTextContent(CUTOFF_REFUSAL);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("creates nothing and reports nothing when the wizard is closed", async () => {
		const reportService = await openTheWizard();
		await onToTheLastStep();

		await userEvent.click(
			within(await theWizard()).getByRole("button", { name: "Cancel" }),
		);

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		expect(reportService.createReport).not.toHaveBeenCalled();
		expect(reportUsage).not.toHaveBeenCalled();
	});

	it("holds the wizard open, with Cancel and Back disabled, while the report is being created", async () => {
		const held = deferred<number>();
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				createReport: vi.fn().mockReturnValue(held.promise),
			}),
		);
		await onToTheLastStep();

		await userEvent.click(await theCreateButton());
		const wizard = await theWizard();
		await userEvent.keyboard("{Escape}");

		expect(
			within(wizard).getByRole("button", { name: "Cancel" }),
		).toBeDisabled();
		expect(within(wizard).getByRole("button", { name: "Back" })).toBeDisabled();
		expect(screen.getByRole("dialog")).toBeVisible();
	});

	it("says a name can be at most 200 characters when the server refuses it for its length", async () => {
		await openTheWizard(
			aReportService({
				getReports: vi.fn().mockResolvedValue(aReportList([])),
				createReport: vi.fn().mockRejectedValue(aRefusal(400, "name-too-long")),
			}),
		);
		await onToTheLastStep();

		await userEvent.click(await theCreateButton());

		expect(
			await within(await theWizard()).findByRole("alert"),
		).toHaveTextContent("A report name can be at most 200 characters.");
	});

	it("takes no more than 200 characters for the name", async () => {
		await openTheWizard();
		await onToTheLastStep();

		expect(await theNameField()).toHaveAttribute("maxlength", "200");
	});
});
