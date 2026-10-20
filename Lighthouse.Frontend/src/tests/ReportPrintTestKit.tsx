import { Box } from "@mui/material";
import { createTheme, ThemeProvider } from "@mui/material/styles";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	act,
	type RenderResult,
	render,
	screen,
	waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { StrictMode } from "react";
import {
	MemoryRouter,
	Route,
	Routes,
	useLocation,
	useNavigate,
	useParams,
} from "react-router";
import { expect, vi } from "vitest";
import {
	UsageDataConsentProvider,
	useUsageDataConsentIfKnown,
} from "../hooks/useUsageDataConsent";
import { Portfolio, PortfolioSchema } from "../models/Portfolio/Portfolio";
import type { IReportOwner } from "../models/Reports/Report";
import { Team, TeamSchema } from "../models/Team/Team";
import type { ITerminology } from "../models/Terminology";
import {
	type PrintPageStandIns,
	reportPrintRoutes,
} from "../pages/Common/Reports/print/reportPrintRoutes";
import ReportsTab from "../pages/Common/Reports/ReportsTab";
import ReportView from "../pages/Common/Reports/ReportView";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IPortfolioService } from "../services/Api/PortfolioService";
import type { IReportService } from "../services/Api/ReportService";
import type { ITeamService } from "../services/Api/TeamService";
import type { ITerminologyService } from "../services/Api/TerminologyService";
import type { IUsageDataService } from "../services/Api/UsageDataService";
import { TerminologyProvider } from "../services/TerminologyContext";
import { deferred } from "./HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockRbacService,
	createMockTeamService,
	createMockTerminologyService,
} from "./MockApiServiceProvider";
import {
	APOLLO,
	defaultTerms,
	LIGHTSPEED,
	readingOf,
	summaryFor,
	type Viewer,
} from "./ReportsTestKit";

/**
 * A report exported as a PDF, from the reader's side: the Export PDF action on the report view and on the
 * Reports list, the print page it leads to, and the browser's print dialog in front of it. The app is the
 * one a reader runs - real routes, real Reports pages, the instance's terms read through the real provider -
 * and only what lies outside it is stood in for: the backend's answers, the print dialog, and the fonts and
 * pictures the browser loads.
 *
 * Every example is set on the instance's day of the Reports kit, 2 Oct 2026, at 09:15 on the reader's clock.
 */

export const EXPORT_PDF = "Export PDF";

/** The report view shows Export PDF while the report is on its way, but it can be pressed only once the report is in. */
export const theExportButtonOnceItCanBePressed =
	async (): Promise<HTMLElement> => {
		const button = await screen.findByRole("button", { name: EXPORT_PDF });
		await waitFor(() => expect(button).toBeEnabled());
		return button;
	};
export const exportAsPdfLabelOf = (reportName: string) =>
	`Export ${reportName} as PDF`;
export const PREPARING = "Preparing the PDF…";
/** The report itself could not be read. */
export const THE_REPORT_COULD_NOT_BE_LOADED =
	"This report couldn't be loaded, so no PDF was made.";
/** The report was read, but its owner's name, the instance's terms, the fonts or the Lighthouse mark were not. */
export const PART_OF_THE_PAGE_COULD_NOT_BE_LOADED =
	"Part of the page couldn't be loaded, so no PDF was made.";
export const BACK_TO_REPORT = "‹ Back to report";
export const BACK_TO_REPORTS = "‹ Back to Reports";
export const BRANDING =
	"Made with Lighthouse by LetPeopleWork · letpeople.work";
export const PDF_EXPORT_STARTED = "ReportPdfExportStarted";
export const THEN_AND_NOW = "ThenAndNow";

/** The reader's clock when every example exports: 2 Oct 2026, 09:15. */
export const EXPORTED_AT = new Date(2026, 9, 2, 9, 15);
export const PRODUCED_LINE = "Produced 2 Oct 2026, 09:15";

/** The engagement-start report's two windows, as the header writes them on one line. */
export const WINDOWS_LINE = "Then 3 May – 31 Jul 2026 · Now 5 Jul – 2 Oct 2026";

export const LIGHTSPEEDS_NAME = "Lightspeed";
export const APOLLOS_NAME = "Apollo";

const STAND_IN_FOR_THE_PRINT_PAGE = "The print page";

export const reportPathFor = (owner: IReportOwner, reportId: number) =>
	`/${owner.kind}s/${owner.id}/reports/${reportId}`;

export const reportsPathFor = (owner: IReportOwner) =>
	`/${owner.kind}s/${owner.id}/reports`;

export const printPathFor = (owner: IReportOwner, reportId: number) =>
	`/print/${owner.kind}s/${owner.id}/reports/${reportId}`;

/** The instance's terms as the backend answers its Terminology read. */
export const theTermsAnswer = (
	terms: Record<string, string> = defaultTerms,
): ITerminology[] =>
	Object.entries(terms).map(([key, value], index) => ({
		id: index + 1,
		key,
		defaultValue: defaultTerms[key] ?? value,
		description: "",
		value,
	}));

export const aTerminologyServiceAnswering = (
	answer: Promise<ITerminology[]>,
): ITerminologyService => ({
	...createMockTerminologyService(),
	getAllTerminology: vi.fn().mockReturnValue(answer),
});

export const lightspeed = (): Team =>
	Team.fromParsed(
		TeamSchema.parse({ id: LIGHTSPEED.id, name: LIGHTSPEEDS_NAME }),
	);

export const apollo = (): Portfolio =>
	Portfolio.fromParsed(
		PortfolioSchema.parse({ id: APOLLO.id, name: APOLLOS_NAME }),
	);

export const aTeamServiceAnswering = (
	answer: Promise<Team | null>,
): ITeamService => ({
	...createMockTeamService(),
	getTeam: vi.fn().mockReturnValue(answer),
});

/** The owner's name cannot be read, every time it is asked for; each failure is made only when it is asked for. */
export const aTeamServiceThatCannotReadTheName = (): ITeamService => ({
	...createMockTeamService(),
	getTeam: vi.fn(() => Promise.reject(new Error("the network dropped"))),
});

/** The instance's terms cannot be read until the given read, which answers them; each failure is made when asked. */
export const aTerminologyServiceThatCannotReadTheTerms = ({
	untilTheRead,
}: {
	readonly untilTheRead: number;
}): ITerminologyService => {
	let reads = 0;
	return {
		...createMockTerminologyService(),
		getAllTerminology: vi.fn(() => {
			reads += 1;
			return reads >= untilTheRead
				? Promise.resolve(theTermsAnswer())
				: Promise.reject(new Error("the network dropped"));
		}),
	};
};

export const aPortfolioServiceAnswering = (
	answer: Promise<Portfolio | null>,
): IPortfolioService => ({
	...createMockPortfolioService(),
	getPortfolio: vi.fn().mockReturnValue(answer),
});

/** Where the reader is: the address bar, which must never carry the wish to print. */
const WhereTheReaderIs: React.FC = () => {
	const location = useLocation();
	return (
		<output
			aria-label="where the reader is"
			data-path={location.pathname}
			data-search={location.search}
			data-state={JSON.stringify(location.state ?? null)}
		/>
	);
};

export const whereTheReaderIs = (): { path: string; search: string } => {
	const shown = screen.getByLabelText("where the reader is");
	return {
		path: shown.dataset.path ?? "",
		search: shown.dataset.search ?? "",
	};
};

/** The browser's own Back button, which the app does not draw; the scene being rendered hands it the router. */
let theBrowsersHistory: ReturnType<typeof useNavigate> | undefined;

const TheBrowsersBackButton: React.FC = () => {
	theBrowsersHistory = useNavigate();
	return null;
};

/** Presses the browser's Back button: one step back through the tab's history, whatever the page draws. */
export const pressTheBrowsersBack = () =>
	act(async () => {
		await theBrowsersHistory?.(-1);
	});

/** What the current history entry carries beside its address: the wish to print and where the export started. */
export const whatThisEntryCarries = (): unknown =>
	JSON.parse(
		screen.getByLabelText("where the reader is").dataset.state ?? "null",
	);

/** What this browser was told about sending usage data, once the answer has arrived. */
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

export const LEAVE_THE_PRINT_PAGE = "Leave the print page";

/** Where the print page goes in scenarios about how Export leaves a report; it only knows the way back. */
const StandInForThePrintPage: React.FC = () => {
	const navigate = useNavigate();
	return (
		<>
			<p>{STAND_IN_FOR_THE_PRINT_PAGE}</p>
			<button type="button" onClick={() => navigate(-1)}>
				{LEAVE_THE_PRINT_PAGE}
			</button>
		</>
	);
};

const OwnersReports: React.FC<{ readonly kind: IReportOwner["kind"] }> = ({
	kind,
}) => {
	const { id } = useParams();
	return <ReportsTab owner={{ kind, id: Number(id) }} />;
};

const OwnersReport: React.FC<{ readonly kind: IReportOwner["kind"] }> = ({
	kind,
}) => {
	const { id, reportId } = useParams();
	return (
		<ReportView owner={{ kind, id: Number(id) }} reportId={Number(reportId)} />
	);
};

export interface ExportScene {
	readonly reportService: IReportService;
	/** Where the reader starts: a report, a Reports list or a print page, by address. */
	readonly startAt: string;
	/** What the first history entry carries, as a reload of a page the reader had reached keeps it. */
	readonly startState?: unknown;
	/** Renders the app in React's strict mode, which runs every effect twice on mount. */
	readonly strict?: boolean;
	readonly viewer?: Viewer;
	readonly teamService?: ITeamService;
	readonly portfolioService?: IPortfolioService;
	readonly terminologyService?: ITerminologyService;
	/** With a usage data service the real consent is in place; without one the page sees no answer about this browser. */
	readonly usageDataService?: IUsageDataService;
	/** False puts a stand-in where the print page goes, for scenarios about how Export leaves the report. */
	readonly withPrintPages?: boolean;
	/** The reader's own theme; the print page must not follow a dark one. */
	readonly readersTheme?: "light" | "dark";
	/** A cache that already holds answers from earlier in the reader's session. */
	readonly queryClient?: QueryClient;
	/** True when the test runs on fake timers, so a click does not wait on a clock nobody moves. */
	readonly onFakeTimers?: boolean;
}

/** A cache set up as the app sets up its own: an answer counts as fresh for five minutes, and nothing retries. */
export const FIVE_MINUTES = 5 * 60 * 1000;
export const aQueryClient = () =>
	new QueryClient({
		defaultOptions: {
			queries: { retry: false, staleTime: FIVE_MINUTES, refetchOnMount: true },
			mutations: { retry: false },
		},
	});

const readersThemeOf = (mode: "light" | "dark") =>
	createTheme({ palette: { mode } });

export const renderTheExportScene = (
	scene: ExportScene,
): RenderResult & {
	queryClient: QueryClient;
	user: ReturnType<typeof userEvent.setup>;
} => {
	const owner = scene.startAt.includes("/portfolios/") ? APOLLO : LIGHTSPEED;
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi
		.fn()
		.mockResolvedValue(summaryFor(scene.viewer ?? "admin", owner));
	const queryClient = scene.queryClient ?? aQueryClient();
	const services = createMockApiServiceContext({
		reportService: scene.reportService,
		rbacService,
		teamService:
			scene.teamService ?? aTeamServiceAnswering(Promise.resolve(lightspeed())),
		portfolioService:
			scene.portfolioService ??
			aPortfolioServiceAnswering(Promise.resolve(apollo())),
		terminologyService:
			scene.terminologyService ??
			aTerminologyServiceAnswering(Promise.resolve(theTermsAnswer())),
		...(scene.usageDataService === undefined
			? {}
			: { usageDataService: scene.usageDataService }),
	});
	const withPrintPages = scene.withPrintPages ?? true;
	const readersMode = scene.readersTheme ?? "light";

	const routes = (
		<Routes>
			<Route
				path="/teams/:id/reports"
				element={<OwnersReports kind="team" />}
			/>
			<Route
				path="/teams/:id/reports/:reportId"
				element={<OwnersReport kind="team" />}
			/>
			<Route
				path="/portfolios/:id/reports"
				element={<OwnersReports kind="portfolio" />}
			/>
			<Route
				path="/portfolios/:id/reports/:reportId"
				element={<OwnersReport kind="portfolio" />}
			/>
			{withPrintPages ? (
				reportPrintRoutes(theBrowsersStandIns())
			) : (
				<Route path="/print/*" element={<StandInForThePrintPage />} />
			)}
		</Routes>
	);

	const app = (
		<ThemeProvider theme={readersThemeOf(readersMode)}>
			<Box
				sx={{
					bgcolor: "background.default",
					color: "text.primary",
				}}
			>
				{routes}
				<WhereTheReaderIs />
				<TheBrowsersBackButton />
			</Box>
		</ThemeProvider>
	);

	const tree = (
		<QueryClientProvider client={queryClient}>
			<MemoryRouter
				initialEntries={[
					{ pathname: scene.startAt, state: scene.startState ?? null },
				]}
			>
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
		</QueryClientProvider>
	);
	const rendered = render(
		scene.strict === true ? <StrictMode>{tree}</StrictMode> : tree,
	);

	const user =
		scene.onFakeTimers === true
			? userEvent.setup({ advanceTimers: vi.advanceTimersByTime })
			: userEvent.setup();

	return { ...rendered, queryClient, user };
};

export const theStandInForThePrintPage = () =>
	screen.findByText(STAND_IN_FOR_THE_PRINT_PAGE);

// --- The browser around the page ---

/** The print dialog as the page asks the browser for it; it opens nothing, and closes when the test says. */
export const aPrintDialog = () => {
	const print = vi.fn();
	vi.stubGlobal("print", print);
	return {
		timesOpened: () => print.mock.calls.length,
		close: () =>
			act(() => {
				globalThis.dispatchEvent(new Event("afterprint"));
			}),
	};
};

type LoadFonts = NonNullable<PrintPageStandIns["loadFonts"]>;

/** The browser fetching and decoding a picture the page shows, as the page asks it to before it prints. */
type LoadPicture = NonNullable<PrintPageStandIns["loadPicture"]>;

/**
 * What the page asked its fonts stand-in for, face by face, and whether the report's title was already on the page
 * when it asked: the fonts are asked for after the document is drawn, for the faces it shows.
 */
export interface FontsAsked {
	readonly fonts: () => string[];
	readonly askedBeforeTheReportWasDrawn: () => boolean;
}

const recordingFontLoads = (
	answer: () => Promise<"loaded" | "failed">,
): FontsAsked & { load: LoadFonts } => {
	const asked: { font: string; drawn: boolean }[] = [];
	return {
		load: (faces) => {
			const drawn = document.querySelector("[data-render-ready] h1") !== null;
			for (const font of faces) asked.push({ font, drawn });
			return answer();
		},
		fonts: () => asked.map(({ font }) => font),
		askedBeforeTheReportWasDrawn: () => asked.some(({ drawn }) => !drawn),
	};
};

/** The stand-in for the browser's fonts that the next scene hands its print page. */
let theFonts: LoadFonts | undefined;

export const theFontsHaveLoaded = (): FontsAsked => {
	const fonts = recordingFontLoads(async () => "loaded");
	theFonts = fonts.load;
	return fonts;
};

/** Fonts still on their way, until the test lets them arrive. */
export const theFontsAreStillLoading = () => {
	const arrived = deferred<void>();
	const fonts = recordingFontLoads(async () => {
		await arrived.promise;
		return "loaded";
	});
	theFonts = fonts.load;
	return {
		...fonts,
		arrive: () =>
			act(async () => {
				arrived.resolve();
				await arrived.promise;
			}),
	};
};

/**
 * The fonts cannot be loaded: the browser did not find a face the page asked for, or asking for the fonts failed
 * outright.
 */
export const theFontsCannotLoad = (
	how: "are not found" | "fail outright",
): FontsAsked => {
	const fonts = recordingFontLoads(async () => {
		if (how === "fail outright") {
			throw new Error("the fonts could not be asked for");
		}
		return "failed";
	});
	theFonts = fonts.load;
	return fonts;
};

/** What the page asked its pictures stand-in for, by address. */
export interface PicturesAsked {
	readonly pictures: () => string[];
}

const recordingPictureLoads = (
	answer: () => Promise<"loaded" | "failed">,
): PicturesAsked & { load: LoadPicture } => {
	const asked: string[] = [];
	return {
		load: (source) => {
			asked.push(source);
			return answer();
		},
		pictures: () => [...asked],
	};
};

/**
 * The stand-in for the browser's pictures that the next scene hands its print page. jsdom never loads a picture,
 * so unless a test says otherwise every picture the page asks for arrives.
 */
let thePictures: LoadPicture = recordingPictureLoads(async () => "loaded").load;

/** The Lighthouse mark still on its way, until the test lets it arrive. */
export const theLighthouseMarkIsStillLoading = () => {
	const arrived = deferred<void>();
	const pictures = recordingPictureLoads(async () => {
		await arrived.promise;
		return "loaded";
	});
	thePictures = pictures.load;
	return {
		...pictures,
		arrive: () =>
			act(async () => {
				arrived.resolve();
				await arrived.promise;
			}),
	};
};

/** The Lighthouse mark cannot be loaded, until the test says it can be reached again. */
export const theLighthouseMarkCannotLoad = () => {
	let reachable = false;
	const pictures = recordingPictureLoads(async () =>
		reachable ? "loaded" : "failed",
	);
	thePictures = pictures.load;
	return {
		...pictures,
		canBeReachedAgain: () => {
			reachable = true;
		},
	};
};

/** Everything the print page reaches in the browser that jsdom does not have: its fonts and its pictures. */
const theBrowsersStandIns = () => ({
	loadFonts: (faces: readonly string[]) =>
		theFonts === undefined
			? Promise.reject(new Error("the test set up no fonts"))
			: theFonts(faces),
	loadPicture: (source: string) => thePictures(source),
});

export const forgetTheBrowser = () => {
	theFonts = undefined;
	thePictures = recordingPictureLoads(async () => "loaded").load;
	vi.unstubAllGlobals();
};

// --- What the print page says about itself ---

const thePrintPage = (): HTMLElement | null =>
	document.querySelector<HTMLElement>("[data-render-ready]");

/** What the page tells a renderer about itself: "true" only once everything it prints is in. */
export const renderReadiness = (): string | undefined =>
	thePrintPage()?.dataset.renderReady;

export const thePrintPageIsReady = () =>
	waitFor(() => expect(renderReadiness()).toBe("true"));

/** The way back to where Export started; a link or a button, whichever the page draws. */
export const theBackControl = (): HTMLElement | undefined =>
	[...screen.queryAllByRole("link"), ...screen.queryAllByRole("button")].find(
		(control) => /Back to (report|Reports)$/.test(readingOf(control)),
	);

/** Lets a few turns of the event loop pass, so "nothing happened" is not merely "not yet". */
export const aMomentLater = () =>
	act(async () => {
		for (let turn = 0; turn < 5; turn++) {
			await Promise.resolve();
		}
	});

/** The print page's footer, found by the credit it carries. */
export const theFooter = (): HTMLElement => {
	const footer = screen.getByText(BRANDING).closest("footer");
	expect(footer, "the credit is not in a footer").not.toBeNull();
	return footer as HTMLElement;
};

// --- The page on paper ---

/**
 * The style rules the browser applies only when it prints. jsdom applies none of them when it computes a style,
 * so they are read off the page's style sheets.
 */
const printStyleRules = (): CSSStyleRule[] =>
	Array.from(document.styleSheets)
		.flatMap((sheet) => Array.from(sheet.cssRules))
		.flatMap((rule) =>
			rule instanceof CSSMediaRule && /\bprint\b/.test(rule.media.mediaText)
				? Array.from(rule.cssRules).filter(
						(inner): inner is CSSStyleRule => inner instanceof CSSStyleRule,
					)
				: [],
		);

/** The last of the print rules for the element that names the property, if any does. */
const askedInPrint = (
	rules: readonly CSSStyleRule[],
	element: Element,
	property: string,
): string | undefined => {
	const asked = rules
		.filter((rule) => element.matches(rule.selectorText))
		.map((rule) => rule.style.getPropertyValue(property).trim())
		.filter((value) => value !== "");
	return asked.length === 0 ? undefined : asked[asked.length - 1];
};

/**
 * What the page asks of one property of the element when it is printed: the last print rule for the element that
 * names the property, or else what the element asks on the screen.
 */
export const whatPrintAsksOf = (element: Element, property: string): string =>
	askedInPrint(printStyleRules(), element, property) ??
	getComputedStyle(element).getPropertyValue(property).trim();

/** The groups a table repeats on every printed page by their own kind, unless print asks otherwise. */
const REPEATED_BY_KIND: Readonly<Record<string, string>> = {
	THEAD: "table-header-group",
	TFOOT: "table-footer-group",
};

/** The elements of the print page laid out on paper as the given display: asked for in print, or by their kind. */
export const elementsPrintedAs = (display: string): HTMLElement[] => {
	const rules = printStyleRules();
	return Array.from(
		document.querySelectorAll<HTMLElement>("[data-render-ready] *"),
	).filter(
		(element) =>
			(askedInPrint(rules, element, "display") ??
				REPEATED_BY_KIND[element.tagName]) === display,
	);
};

/** Every declaration the page makes for print only, as one text. */
export const thePrintOnlyDeclarations = (): string =>
	printStyleRules()
		.map((rule) => rule.style.cssText)
		.join("\n");

/** Whether the element, or what holds it on the page, is hidden when the page is printed. */
export const isHiddenInPrint = (element: HTMLElement): boolean => {
	const printRules = Array.from(document.querySelectorAll("style"))
		.map((style) => style.textContent ?? "")
		.join("\n")
		.match(/@media print\s*\{(?:[^{}]*\{[^{}]*\})*[^{}]*\}/g);
	const hiddenInPrint = (printRules ?? []).join("\n");
	for (
		let current: HTMLElement | null = element;
		current !== null;
		current = current.parentElement
	) {
		for (const name of Array.from(current.classList)) {
			// The boundary keeps ".back" from matching a rule written for ".back-link".
			const rule = new RegExp(
				`\\.${name}(?![\\w-])[^{]*\\{[^}]*display:\\s*none`,
			);
			if (rule.test(hiddenInPrint)) return true;
		}
	}
	return false;
};
