import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	fireEvent,
	type RenderResult,
	render,
	screen,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { MemoryRouter } from "react-router";
import { vi } from "vitest";
import SnackbarErrorHandler from "../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type {
	IReport,
	IReportList,
	IReportListItem,
	IReportOwner,
	IReportTemplates,
	IReportValue,
	ReportAbsentReason,
	ReportMetricKey,
} from "../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../models/TerminologyKeys";
import { ApiError } from "../services/Api/ApiError";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IReportService } from "../services/Api/ReportService";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "./MockApiServiceProvider";

/**
 * The Reports of one Team or Portfolio as a browser sees them, on the day every example is set: 2 Oct 2026,
 * with finished Work Items kept back to 2 Oct 2025. The report the examples open is the engagement-start
 * report of Team Lightspeed - Then the 90 days to 31 Jul 2026, Now the last 90 days - with one set of
 * numbers for each of the four metrics, chosen so every change has a sign and a whole percentage.
 *
 * Who counts as an editor comes from useRbac() alone, so a viewer is set up by the authorisation summary
 * the page reads, never by a flag handed to the component.
 */

export const LIGHTSPEED: IReportOwner = { kind: "team", id: 1 };
export const APOLLO: IReportOwner = { kind: "portfolio", id: 3 };

/** The instance's day every example is set on; the browser's own clock never decides it. */
export const TODAY_ON_THE_INSTANCE = "2026-10-02";
export const ENGAGEMENT_START = "Then & Now — engagement start";
export const ENGAGEMENT_START_ID = 42;

export const ALL_FOUR_METRICS: ReportMetricKey[] = [
	"cycle-time",
	"throughput",
	"wip",
	"work-item-age",
];

/** The seeded words, so a test that renames one shows the page uses the instance's word. */
export const defaultTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.TEAM]: "Team",
	[TERMINOLOGY_KEYS.PORTFOLIO]: "Portfolio",
	[TERMINOLOGY_KEYS.WORK_ITEM]: "Work Item",
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	[TERMINOLOGY_KEYS.CYCLE_TIME]: "Cycle Time",
	[TERMINOLOGY_KEYS.THROUGHPUT]: "Throughput",
	[TERMINOLOGY_KEYS.WIP]: "WIP",
	[TERMINOLOGY_KEYS.WORK_ITEM_AGE]: "Work Item Age",
};

export const CAP_SENTENCE =
	"Community includes 1 Then & Now report per Team. Delete it, or use Premium for unlimited reports.";
export const CUTOFF_REFUSAL =
	"The Then window must start on or after 2 Oct 2025 — older finished Work Items are no longer kept.";
export const thenNeedsAtLeast = (days: number) =>
	`Then needs at least ${days} days.`;
export const THEN_TOO_SHORT = thenNeedsAtLeast(14);
export const nowNeedsAtLeast = (days: number) =>
	`Now needs at least ${days} days.`;
export const THEN_TOO_LONG = "Then can't reach back that far.";
export const NO_METRIC = "Choose at least one metric";
export const NOT_CAPTURED = "Not captured for this report";
export const FROZEN = "frozen, can't be changed";
export const STALE_SAVE =
	"This report was changed by someone else. Reload to see their changes.";
export const CANNOT_BE_REBUILT = "This Then window can no longer be rebuilt.";
export const LOADING = "Loading report…";
export const COULD_NOT_LOAD =
	"This report couldn't be loaded. Reload to try again.";
export const NO_LONGER_EXISTS = "This report no longer exists.";
export const MADE_BY_A_NEWER_VERSION =
	"This report was made by a newer version of Lighthouse and can't be shown here.";
export const METRIC_FROM_A_NEWER_VERSION =
	"This metric can't be shown in this version of Lighthouse.";
export const NOT_SUPPORTED_IN_THIS_VERSION = "Not supported in this version";
export const LIST_COULD_NOT_LOAD =
	"Reports couldn't be loaded. Reload to try again.";
export const TYPES_COULD_NOT_LOAD = "Report types couldn't be loaded.";

/** A type of report a later version adds; this version lists it but cannot show it. */
export const A_LATER_TYPE = "signals";

/** A metric a later version adds; this version has no panel for it. */
export const A_LATER_METRIC = "lead-time-distribution" as ReportMetricKey;

export const aTemplateList = (
	overrides: Partial<IReportTemplates> = {},
): IReportTemplates => ({
	today: TODAY_ON_THE_INSTANCE,
	earliestStart: "2025-10-02",
	minimumLengthDays: 14,
	...overrides,
	templates: [
		{
			templateKey: "then-and-now",
			metrics: ALL_FOUR_METRICS.map((key) => ({ key })),
		},
	],
});

export const aListedReport = (
	overrides: Partial<IReportListItem> = {},
): IReportListItem => ({
	id: ENGAGEMENT_START_ID,
	name: ENGAGEMENT_START,
	templateKey: "then-and-now",
	ownerKind: "team",
	ownerId: LIGHTSPEED.id,
	createdOn: "2026-08-01",
	thenAndNow: { thenRebuildable: true },
	...overrides,
});

/** A report a later version created, listed with no Then & Now part. */
export const aListedReportOfALaterType = (): IReportListItem => ({
	id: 44,
	name: "Signals — flow alerts",
	templateKey: A_LATER_TYPE,
	ownerKind: "team",
	ownerId: LIGHTSPEED.id,
	createdOn: "2026-09-20",
});

export const aReportList = (
	reports: IReportListItem[],
	creationBlockedByCap = false,
): IReportList => ({ reports, creationBlockedByCap, communityCap: 1 });

export const shown = (
	value: number,
	sampleSize: number | null = null,
): IReportValue => ({ value, sampleSize, absentReason: null });

export const aDash = (absentReason: ReportAbsentReason): IReportValue => ({
	value: null,
	sampleSize: null,
	absentReason,
});

/**
 * The engagement-start report: {Cycle Time} 95th 34 → 19, 85th 21 → 12, 70th 10 → 7, 50th 5 → 4 days over 38 → 41 finished
 * Work Items; {Throughput} 52 → 65 finished, 0.58 → 0.72 per day; {WIP} Average 8.62 → 7.38 (shown 9 → 7), Median
 * 9 → 7, Min 6 → 5, Max 12 → 9; Total {Work Item Age} Average 610.4 → 880 days (shown 610 → 880), Median 600 → 870,
 * Min 420 → 640, Max 760 → 990. Throughput's totals differ from Cycle Time's counts on purpose: Cycle Time counts
 * only the finished Work Items it can measure, so the two must never be mistaken for each other.
 */
export const theEngagementStartReport = (
	overrides: Partial<IReport> = {},
): IReport => ({
	id: ENGAGEMENT_START_ID,
	name: ENGAGEMENT_START,
	templateKey: "then-and-now",
	ownerKind: "team",
	ownerId: LIGHTSPEED.id,
	createdOn: "2026-08-01",
	concurrencyToken: "token-1",
	shownMetricKeys: [...ALL_FOUR_METRICS],
	thenAndNow: {
		thenWindow: { start: "2026-05-03", end: "2026-07-31", lengthDays: 90 },
		nowWindow: { start: "2026-07-05", end: "2026-10-02", lengthDays: 90 },
		daysSinceThenEnded: 63,
		thenRebuildable: true,
		metrics: {
			"cycle-time": {
				atThen: {
					p95: shown(34, 38),
					p85: shown(21, 38),
					p70: shown(10, 38),
					p50: shown(5, 38),
				},
				atNow: {
					p95: shown(19, 41),
					p85: shown(12, 41),
					p70: shown(7, 41),
					p50: shown(4, 41),
				},
			},
			throughput: {
				atThen: { total: shown(52, 90), perDay: shown(0.58, 90) },
				atNow: { total: shown(65, 90), perDay: shown(0.72, 90) },
			},
			wip: {
				atThen: {
					average: shown(8.62, 90),
					median: shown(9, 90),
					min: shown(6, 90),
					max: shown(12, 90),
				},
				atNow: {
					average: shown(7.38, 90),
					median: shown(7, 90),
					min: shown(5, 90),
					max: shown(9, 90),
				},
			},
			"work-item-age": {
				atThen: {
					average: shown(610.4, 90),
					median: shown(600, 90),
					min: shown(420, 90),
					max: shown(760, 90),
				},
				atNow: {
					average: shown(880, 90),
					median: shown(870, 90),
					min: shown(640, 90),
					max: shown(990, 90),
				},
			},
		},
	},
	...overrides,
});

/** A report service answering every read with the engagement-start report of Team Lightspeed. */
export const aReportService = (
	overrides: Partial<IReportService> = {},
): IReportService => ({
	getTemplates: vi.fn().mockResolvedValue(aTemplateList()),
	getReports: vi.fn().mockResolvedValue(aReportList([aListedReport()])),
	getReport: vi.fn().mockResolvedValue(theEngagementStartReport()),
	createReport: vi.fn().mockResolvedValue(ENGAGEMENT_START_ID),
	editReport: vi.fn().mockResolvedValue(theEngagementStartReport()),
	deleteReport: vi.fn().mockResolvedValue(undefined),
	getAllReports: vi.fn().mockResolvedValue({ reports: [] }),
	getCreationOptions: vi.fn().mockResolvedValue({
		today: TODAY_ON_THE_INSTANCE,
		minimumLengthDays: 14,
		templates: [],
		owners: [],
	}),
	createReportsInBulk: vi.fn().mockResolvedValue([]),
	removeReportsInBulk: vi.fn().mockResolvedValue([]),
	...overrides,
});

/** A refusal the way the API service hands it on: the status, the reason's code, and the oldest kept day when the server names one. */
export const aRefusal = (
	status: number,
	reason: string,
	earliestStart?: string | null,
): ApiError =>
	new ApiError(
		status,
		`refused: ${reason}`,
		undefined,
		undefined,
		reason,
		earliestStart,
	);

export type Viewer = "admin" | "reader" | "rbacOff";

/** What the page reads to decide who may change the reports, for one kind of reader of one owner. */
export const summaryFor = (viewer: Viewer, owner: IReportOwner) => ({
	isRbacEnabled: viewer !== "rbacOff",
	isSystemAdmin: false,
	canCreateTeam: false,
	canCreatePortfolio: false,
	adminTeamIds: viewer === "admin" && owner.kind === "team" ? [owner.id] : [],
	adminPortfolioIds:
		viewer === "admin" && owner.kind === "portfolio" ? [owner.id] : [],
});

export const renderWithReports = (
	ui: React.ReactElement,
	{
		reportService,
		viewer = "admin",
		owner = LIGHTSPEED,
		retry = false,
	}: {
		reportService: IReportService;
		viewer?: Viewer;
		owner?: IReportOwner;
		retry?: number | false;
	},
): RenderResult & {
	rerenderWith: (element: React.ReactElement) => void;
	queryClient: QueryClient;
} => {
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi
		.fn()
		.mockResolvedValue(summaryFor(viewer, owner));

	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry }, mutations: { retry: false } },
	});

	const wrap = (element: React.ReactElement) => (
		<QueryClientProvider client={queryClient}>
			<MemoryRouter>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({ reportService, rbacService })}
				>
					<SnackbarErrorHandler>{element}</SnackbarErrorHandler>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>
	);

	const rendered = render(wrap(ui));
	return {
		...rendered,
		queryClient,
		rerenderWith: (element: React.ReactElement) =>
			rendered.rerender(wrap(element)),
	};
};

/** Text as a reader sees it: every run of white space read as one space. */
export const readingOf = (element: HTMLElement): string =>
	(element.textContent ?? "").replaceAll(/\s+/g, " ").trim();

export const thePanel = async (title: string) =>
	screen.findByRole("region", { name: title });

export const thePanelTitles = () =>
	screen
		.queryAllByRole("region")
		.map((panel) => readingOf(within(panel).getAllByRole("heading")[0]));

// --- The create wizard, step by step ---

export const theWizard = async () => screen.findByRole("dialog");

export const next = async () =>
	userEvent.click(
		within(await theWizard()).getByRole("button", { name: "Next" }),
	);

export const theThenEndField = async () =>
	within(await theWizard()).findByLabelText("Then ends on");

export const theThenLengthField = async () =>
	within(await theWizard()).findByRole("spinbutton", {
		name: "Then length in days",
	});

export const theNowLengthField = async () =>
	within(await theWizard()).findByRole("spinbutton", {
		name: "Now length in days",
	});

export const theNameField = async () =>
	within(await theWizard()).findByRole("textbox", { name: "Name" });

export const chooseThenEnd = async (date: string) =>
	fireEvent.change(await theThenEndField(), { target: { value: date } });

export const enterNumber = async (field: HTMLElement, days: number) => {
	await userEvent.clear(field);
	await userEvent.type(field, String(days));
};

export const theMetricBox = async (name: string) =>
	within(await theWizard()).findByRole("checkbox", { name });
