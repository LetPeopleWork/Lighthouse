import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	type RenderResult,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router";
import { vi } from "vitest";
import {
	UsageDataConsentProvider,
	useUsageDataConsentIfKnown,
} from "../hooks/useUsageDataConsent";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import type {
	IReportAcrossOwners,
	IReportCreationOptions,
	IReportCreationOutcome,
	IReportCreationOwner,
	IReportRemovalOutcome,
	IReportsAcrossOwners,
} from "../models/Reports/ReportsAcrossOwners";
import ReportsPage from "../pages/Reports/ReportsPage";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IReportService } from "../services/Api/ReportService";
import type { IUsageDataService } from "../services/Api/UsageDataService";
import { TerminologyProvider } from "../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockRbacService,
} from "./MockApiServiceProvider";
import {
	aTerminologyServiceAnswering,
	theTermsAnswer,
} from "./ReportPrintTestKit";
import { aReportService, defaultTerms } from "./ReportsTestKit";

/**
 * Every report in one place, created and removed in bulk, from the reader's side: the Reports page with its
 * table, its selection and its toolbar, the wizard that creates one report for several Teams and Portfolios, the
 * result and removal dialogs, and one PDF of several reports. The server is stood in for at the report service;
 * permissions, the licence and the instance's terms come from their own services as they do in the app.
 */

export const pendingEpic6350 = (slice: string) =>
	`pending: every report in one place (epic 6350, slice ${slice})`;

/** The instance's day every example is set on. */
export const TODAY = "2026-10-27";

export interface Owner {
	readonly kind: "team" | "portfolio";
	readonly id: number;
	readonly name: string;
}

export const ZENITH: Owner = { kind: "team", id: 21, name: "Zenith" };
export const GRAVITY: Owner = { kind: "team", id: 22, name: "Gravity" };
export const EQUINOX: Owner = { kind: "team", id: 23, name: "Equinox" };
export const VOYAGER: Owner = { kind: "team", id: 24, name: "Voyager" };
export const PULSAR: Owner = { kind: "team", id: 25, name: "Pulsar" };
export const LIGHTSPEED_TEAM: Owner = {
	kind: "team",
	id: 26,
	name: "Lightspeed",
};
export const OCEAN_EXPLORER: Owner = {
	kind: "portfolio",
	id: 31,
	name: "Ocean Explorer",
};
export const APOLLO_PORTFOLIO: Owner = {
	kind: "portfolio",
	id: 32,
	name: "Apollo",
};
export const NEUROLINK_CITY: Owner = {
	kind: "portfolio",
	id: 33,
	name: "NeuroLink City",
};

export const QUARTER = "Quarter vs qtr";
export const MONTH = "Month vs month";

// The copy the maintainer settled in the sketch walk, pinned word for word.
export const REPORTS = "Reports";
export const CREATE_REPORTS = "Create reports";
export const EMPTY_BEFORE_BULK_CREATION =
	"Reports are created on a Team's or Portfolio's Reports tab.";
export const EMPTY = `Reports are created on a Team's or Portfolio's Reports tab, or here with ${CREATE_REPORTS}.`;
export const EVERY_REPORT_COULD_NOT_LOAD = "The reports couldn't be loaded.";
export const RETRY = "Retry";
export const FILTER = "Filter: name / owner / type";
export const removeButton = (count: number) => `Remove (${count})`;
export const exportButton = (count: number) => `Export PDF (${count})`;
export const selectedCount = (count: number) => `${count} selected`;
export const AT_MOST_50 = "Pick at most 50 reports.";
export const notAdminOfSome = (notAdmin: number, selected: number) =>
	`${notAdmin} of the ${selected} selected reports is on a Team or Portfolio you are not Admin of.`;
export const NOT_ADMIN_OF_ANY = "You are not Admin of any Team or Portfolio.";
export const OWNER_STEP = "Step 5 of 5 — Who gets this report?";
export const TEAMS_GROUP = "Teams";
export const PORTFOLIOS_GROUP = "Portfolios";
export const SELECT_ALL = "Select all";
export const keptHistoryStarts = (keptSince: string, thenStarts: string) =>
	`Kept history starts ${keptSince}; Then starts ${thenStarts}.`;
export const openSettingsOf = (name: string) => `Open ${name} settings`;
export const ONLY_ADMIN_OWNERS =
	"Only Teams and Portfolios you are Admin of are listed.";
export const createCount = (count: number) =>
	count === 1 ? "Create 1 report" : `Create ${count} reports`;
export const OWNERS_COULD_NOT_LOAD =
	"Your Teams and Portfolios couldn't be loaded.";
export const creating = (count: number) => `Creating ${count} reports…`;
export const createdHeadline = (created: number, notCreated: number) =>
	`${created} ${created === 1 ? "report" : "reports"} created, ${notCreated} not created`;
export const CREATED = "Created";
export const OPEN = "Open";
export const DONE = "Done";
export const NOT_CONFIRMED_CREATED =
	"It could not be confirmed which reports were created.";
export const GO_TO_REPORTS = "Go to Reports";
export const NOT_CONFIRMED_REMOVED =
	"It could not be confirmed which reports were removed.";
export const LOST_FOR_GOOD_WARNING =
	"1 of them has a Then that can no longer be rebuilt from the kept history; removing it loses it for good.";
export const CANCEL = "Cancel";
export const removeCount = (count: number) => `Remove ${count} reports`;
export const removedHeadline = (count: number) => `${count} reports removed`;
export const NO_PDF_WAS_MADE = "No PDF was made";
export const couldNotLoadTheReportOf = (owner: Owner, reportName: string) =>
	`${kindWordOf(owner)} ${owner.name}'s report "${reportName}" couldn't be loaded.`;
export const LEAVE_IT_OUT = "Leave it out";
export const PART_OF_THE_PAGE_FAILED =
	"Part of the page couldn't be loaded, so no PDF was made.";
export const THEN_PRESETS = ["30 days", "90 days", "180 days", "Custom"];
export const SAME_AS_THEN = "Same as Then";
/** The existing Premium tooltip (`LicenseTooltip`), word for word up to the link's end. */
export const PREMIUM_LICENSE_TOOLTIP =
	/^This feature requires a premium license\./;
export const OWNERS_AT_MOST_50 = "Pick at most 50 Teams and Portfolios.";
export const OPTIONS_COULD_NOT_LOAD = "What a report needs couldn't be loaded.";
/** Provisional wording: the maintainer has not confirmed it yet. */
export const NOTHING_SELECTED_TO_EXPORT = "Select at least one report.";
export const DELETE = "Delete";

/** The reason lines of a bulk result, word for word as the maintainer settled them. */
export const notCreatedBecause = {
	notAdmin: (owner: Owner) =>
		`Not created: you are no longer Admin of ${owner.name}.`,
	ownerGone: (owner: Owner) => `Not created: ${owner.name} no longer exists.`,
	capReached: (owner: Owner) =>
		`Not created: ${owner.name} already has its one Then & Now report; Premium lifts this limit.`,
	historyTooShort: (keptSince: string) =>
		`Not created: its kept history starts ${keptSince}. Keep more history under Closed Items Cutoff in its settings.`,
	notSaved: () => "Not created: it couldn't be saved. Try again.",
};
export const notRemovedBecause = {
	notAdmin: (owner: Owner) =>
		`Not removed: you are no longer Admin of ${owner.name}.`,
};

const kindWordOf = (owner: Owner) =>
	owner.kind === "team" ? defaultTerms.team : defaultTerms.portfolio;

/** What the Owner cell shows: the name alone, as the link to the owner, behind a Team or Portfolio icon. */
export const ownerCellOf = (owner: Owner) => owner.name;

/** How the Owner link is announced: its name, then its kind in the instance's words. */
export const ownerLinkNameOf = (owner: Owner) =>
	`${owner.name}, ${kindWordOf(owner)}`;

/** One line of the removal confirmation: the report, then its owner. */
export const confirmLineOf = (reportName: string, owner: Owner) =>
	`${reportName} — ${owner.name}`;

// --- What the server answers ---

let nextReportId = 900;

export const aReportOn = (
	owner: Owner,
	name: string = QUARTER,
	overrides: Partial<IReportAcrossOwners> = {},
): IReportAcrossOwners => ({
	id: nextReportId++,
	name,
	templateKey: "then-and-now",
	ownerKind: owner.kind,
	ownerId: owner.id,
	ownerName: owner.name,
	createdOn: `${TODAY}T09:00:00Z`,
	thenAndNow: { thenRebuildable: true },
	...overrides,
});

export const everyReport = (
	...reports: IReportAcrossOwners[]
): IReportsAcrossOwners => ({ reports });

/** As many reports as the ceiling allows plus `beyond`, all on one owner, so select-all crosses it. */
export const moreReportsThanTheCeiling = (owner: Owner, beyond = 1) =>
	everyReport(
		...Array.from({ length: 50 + beyond }, (_, index) =>
			aReportOn(owner, `Report ${index + 1}`),
		),
	);

export const anOwnerToCreateOn = (
	owner: Owner,
	earliestStart: string | null = null,
): IReportCreationOwner => ({
	ownerKind: owner.kind,
	ownerId: owner.id,
	ownerName: owner.name,
	earliestStart,
});

export const creationOptionsFor = (
	...owners: IReportCreationOwner[]
): IReportCreationOptions => ({
	today: TODAY,
	minimumLengthDays: 14,
	templates: [
		{
			templateKey: "then-and-now",
			metricKeysByOwnerKind: {
				team: ["cycle-time", "throughput", "wip", "work-item-age"],
				portfolio: ["cycle-time", "throughput", "wip", "work-item-age"],
			},
		},
	],
	owners,
});

export const created = (
	owner: Owner,
	reportId: number,
): IReportCreationOutcome => ({
	ownerKind: owner.kind,
	ownerId: owner.id,
	ownerName: owner.name,
	status: "created",
	reportId,
});

export const notCreated = (
	owner: Owner,
	code: string,
	earliestStart?: string,
): IReportCreationOutcome => ({
	ownerKind: owner.kind,
	ownerId: owner.id,
	ownerName: owner.name,
	status: "notCreated",
	code,
	...(earliestStart === undefined ? {} : { earliestStart }),
});

export const removed = (
	reportId: number,
	code?: string,
): IReportRemovalOutcome => ({
	reportId,
	status: "removed",
	...(code === undefined ? {} : { code }),
});

export const notRemoved = (
	reportId: number,
	code: string,
): IReportRemovalOutcome => ({ reportId, status: "notRemoved", code });

/** An answer that never arrives, so the page can be seen while it waits. */
export const neverAnswered = (): Promise<never> =>
	new Promise<never>(() => undefined);

// --- Who is looking, and on what licence ---

export type Permissions =
	| { readonly adminOf: readonly Owner[] }
	| "rolesNotEnforced"
	| "loading"
	| "failing";

export type Licence = "premium" | "community";

const summaryOf = (permissions: Exclude<Permissions, "loading" | "failing">) =>
	permissions === "rolesNotEnforced"
		? {
				isRbacEnabled: false,
				isSystemAdmin: false,
				canCreateTeam: true,
				canCreatePortfolio: true,
				adminTeamIds: [],
				adminPortfolioIds: [],
			}
		: {
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: false,
				canCreatePortfolio: false,
				adminTeamIds: permissions.adminOf
					.filter((owner) => owner.kind === "team")
					.map((owner) => owner.id),
				adminPortfolioIds: permissions.adminOf
					.filter((owner) => owner.kind === "portfolio")
					.map((owner) => owner.id),
			};

const licenceStatusOf = (licence: Licence): ILicenseStatus => ({
	hasLicense: licence === "premium",
	isValid: licence === "premium",
	canUsePremiumFeatures: licence === "premium",
});

// --- The page ---

export interface ReportsPageScene {
	readonly reportService?: IReportService;
	readonly permissions?: Permissions;
	readonly licence?: Licence;
	readonly usageDataService?: IUsageDataService;
	readonly startAt?: string;
	/** The instance's words, when a scenario renames one. */
	readonly terms?: Record<string, string>;
}

const WhereTheReaderIs = () => (
	<output aria-label="where the reader is" data-path={useLocation().pathname} />
);

export const whereTheReaderIs = (): string =>
	screen.getByLabelText("where the reader is").dataset.path ?? "";

const ThisBrowsersUsageDataAnswer: React.FC = () => {
	const consent = useUsageDataConsentIfKnown();
	return (
		<output
			aria-label="this browser's usage data answer"
			data-state={consent?.indicatorState ?? "none"}
		/>
	);
};

export const thisBrowsersUsageDataAnswer = (): string =>
	screen.getByLabelText("this browser's usage data answer").dataset.state ??
	"none";

export const renderTheReportsPage = (
	scene: ReportsPageScene = {},
): RenderResult & {
	user: ReturnType<typeof userEvent.setup>;
	reportService: IReportService;
} => {
	const reportService = scene.reportService ?? aReportService();
	const permissions = scene.permissions ?? {
		adminOf: [ZENITH, GRAVITY, OCEAN_EXPLORER],
	};
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn(() => {
		if (permissions === "loading") return neverAnswered();
		if (permissions === "failing")
			return Promise.reject(new Error("the network dropped"));
		return Promise.resolve(summaryOf(permissions));
	});
	const licensingService = createMockLicensingService();
	licensingService.getLicenseStatus = vi
		.fn()
		.mockResolvedValue(licenceStatusOf(scene.licence ?? "premium"));
	const services = createMockApiServiceContext({
		reportService,
		rbacService,
		licensingService,
		terminologyService: aTerminologyServiceAnswering(
			Promise.resolve(theTermsAnswer(scene.terms ?? defaultTerms)),
		),
		...(scene.usageDataService === undefined
			? {}
			: { usageDataService: scene.usageDataService }),
	});
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});

	// Mounted bare, as App.tsx mounts the route: the page must bring its own error snackbar.
	const app = (
		<>
			<Routes>
				<Route path="/reports" element={<ReportsPage />} />
				<Route path="*" element={<p>somewhere else</p>} />
			</Routes>
			<WhereTheReaderIs />
		</>
	);

	const rendered = render(
		<QueryClientProvider client={queryClient}>
			<MemoryRouter initialEntries={[scene.startAt ?? "/reports"]}>
				<ApiServiceContext.Provider value={services}>
					<TerminologyProvider>
						{scene.usageDataService === undefined ? (
							app
						) : (
							<UsageDataConsentProvider>
								{app}
								<ThisBrowsersUsageDataAnswer />
							</UsageDataConsentProvider>
						)}
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);

	return { ...rendered, user: userEvent.setup(), reportService };
};

// --- Reading the page ---

/** The table once its rows have arrived; the page marks it ready only then. */
export const theReportsTable = async (): Promise<HTMLElement> => {
	const table = await screen.findByTestId("all-reports");
	await waitFor(() => {
		if (table.dataset.widgetStatus !== "ready") {
			throw new Error("the reports are still on their way");
		}
	});
	return table;
};

export const rowOf = (reportName: string, owner: Owner): HTMLElement => {
	const row = screen.getAllByRole("row").find(
		(candidate) =>
			within(candidate).queryByRole("link", { name: reportName }) !== null &&
			within(candidate).queryByRole("link", {
				name: ownerLinkNameOf(owner),
			}) !== null,
	);
	if (row === undefined) {
		throw new Error(`no row for "${reportName}" on ${ownerLinkNameOf(owner)}`);
	}
	return row;
};

export const selectTheRowOf = async (
	user: ReturnType<typeof userEvent.setup>,
	reportName: string,
	owner: Owner,
) => user.click(within(rowOf(reportName, owner)).getByRole("checkbox"));

export const theReportNamesListed = (): string[] =>
	screen
		.getAllByRole("row")
		.slice(1)
		.map(
			(row) => within(row).queryAllByRole("link")[0]?.textContent?.trim() ?? "",
		);

/** Opens the wizard and walks it to the owner step with the given Then window chosen. */
export const toTheOwnerStep = async (
	user: ReturnType<typeof userEvent.setup>,
	{ thenPreset = "90 days" }: { thenPreset?: string } = {},
) => {
	await user.click(await screen.findByRole("button", { name: CREATE_REPORTS }));
	await user.click(await screen.findByRole("button", { name: thenPreset }));
	await nextUntil(user, () => screen.queryByText(OWNER_STEP) !== null);
};

/** Presses Next, one step at a time, until the wizard shows what the test is waiting for. */
export const nextUntil = async (
	user: ReturnType<typeof userEvent.setup>,
	reached: () => boolean,
): Promise<void> => {
	if (reached()) return;
	await user.click(screen.getByRole("button", { name: "Next" }));
	return nextUntil(user, reached);
};

export const pickOwner = async (
	user: ReturnType<typeof userEvent.setup>,
	owner: Owner,
) => user.click(screen.getByRole("checkbox", { name: owner.name }));
