import { Box } from "@mui/material";
import { createTheme, ThemeProvider } from "@mui/material/styles";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	act,
	type RenderResult,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { StrictMode, useRef } from "react";
import {
	MemoryRouter,
	Route,
	Routes,
	useLocation,
	useParams,
} from "react-router";
import { expect, vi } from "vitest";
import SnackbarErrorHandler from "../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
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
	aReportService,
	defaultTerms,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	readingOf,
	summaryFor,
	type Viewer,
} from "./ReportsTestKit";

/**
 * A report exported as a PDF, from the reader's side: the Export PDF action on the report view and on the
 * Reports list, the hidden frame the report is printed from, the print page a pasted link opens, and the
 * browser's print dialog. The app is the one a reader runs - real routes, real Reports pages, the instance's
 * terms read through the real provider, the error snackbar the Team and Portfolio pages hold around their
 * Reports - and only what lies outside it is stood in for: the backend's answers, the print dialog, and what
 * jsdom does not do itself, which is loading fonts, pictures and style sheets.
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

const LIGHTSPEEDS_NAME = "Lightspeed";
const APOLLOS_NAME = "Apollo";

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

/**
 * Where the reader is: the address bar, what the current history entry carries, and how many history entries the
 * reader has been through in this scene. Every move through the history, a replace included, is an entry of its
 * own, so an export that moves the reader anywhere at all is seen.
 */
const WhereTheReaderIs: React.FC = () => {
	const location = useLocation();
	const entries = useRef(new Set<string>());
	entries.current.add(location.key);
	return (
		<output
			aria-label="where the reader is"
			data-path={location.pathname}
			data-search={location.search}
			data-state={JSON.stringify(location.state ?? null)}
			data-entries={entries.current.size}
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

/** The reader's address and how many history entries they have been through since the scene began. */
export const theReadersHistory = (): { path: string; entries: number } => {
	const shown = screen.getByLabelText("where the reader is");
	return {
		path: shown.dataset.path ?? "",
		entries: Number(shown.dataset.entries ?? "0"),
	};
};

/** What the current history entry carries beside its address. */
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
	/** False leaves the print pages out, for scenarios about the Reports pages alone. */
	readonly withPrintPages?: boolean;
	/** The reader's own theme; the print page must not follow a dark one. */
	readonly readersTheme?: "light" | "dark";
	/** A cache that already holds answers from earlier in the reader's session. */
	readonly queryClient?: QueryClient;
	/** True when the test runs on fake timers, so a click does not wait on a clock nobody moves. */
	readonly onFakeTimers?: boolean;
}

/** A cache set up as the app sets up its own: an answer counts as fresh for five minutes, and nothing retries. */
const FIVE_MINUTES = 5 * 60 * 1000;
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
			{withPrintPages && reportPrintRoutes(theBrowsersStandIns())}
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
				<SnackbarErrorHandler>{routes}</SnackbarErrorHandler>
				<WhereTheReaderIs />
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

/** The engagement-start report's print page opened straight from its address, the way a pasted link opens it. */
export const openingThePrintPageDirectly = (
	scene: Partial<ExportScene> = {},
): ReturnType<typeof renderTheExportScene> =>
	renderTheExportScene({
		reportService: aReportService(),
		startAt: printPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
		...scene,
	});

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
	forgetThePrintFrames();
};

// --- The hidden frame an export prints from ---

/** The title the print frame carries, so assistive technology that still meets it can name it. */
export const REPORT_PDF_FRAME = "Report PDF";

/** How the frame's print dialog behaves: as Firefox's, as Chrome's, or not at all. */
export type FramePrintDialog =
	/** Opens, and says it closed only when the test closes it. */
	| "opens"
	/** Says it closed before print() even returns, as Chrome does. */
	| "closes before print returns"
	/** print() throws. */
	| "throws"
	/** The frame's window has no print at all. */
	| "is missing";

export interface PrintFrameBrowserOptions {
	readonly dialog?: FramePrintDialog;
	/** Whether the app's style sheets, copied into the frame, load there, fail, or are never answered. */
	readonly styleSheets?: "load" | "fail" | "never settle";
}

interface FramePrint {
	readonly frame: HTMLIFrameElement;
	/** Whether the frame was still in the page when its print() returned. */
	stillThereWhenPrintReturned: boolean | undefined;
}

let theFrameWatch: MutationObserver | undefined;
let framePrints: FramePrint[] = [];

type FramesWindow = Window & typeof globalThis;

const A_FACE_FOUND = { family: "Quicksand Variable" } as unknown as FontFace;

/** Answers a load of a style sheet copied into the frame, as a browser would once it has fetched the sheet. */
const answerTheSheetsCopiedInto = (
	head: HTMLElement,
	styleSheets: NonNullable<PrintFrameBrowserOptions["styleSheets"]>,
) => {
	if (styleSheets === "never settle") return;
	const answer = (link: HTMLLinkElement) =>
		setTimeout(() => {
			link.dispatchEvent(
				new (link.ownerDocument.defaultView as FramesWindow).Event(
					styleSheets === "load" ? "load" : "error",
				),
			);
		});
	new MutationObserver((changes) => {
		for (const change of changes) {
			for (const added of Array.from(change.addedNodes)) {
				if (added.nodeName === "LINK") answer(added as HTMLLinkElement);
			}
		}
	}).observe(head, { childList: true });
};

const standInForTheFramesPrint = (
	frame: HTMLIFrameElement,
	frameWindow: FramesWindow,
	dialog: FramePrintDialog,
) => {
	if (dialog === "is missing") {
		Object.defineProperty(frameWindow, "print", {
			configurable: true,
			value: undefined,
		});
		return;
	}
	frameWindow.print = () => {
		if (dialog === "throws") {
			throw new Error("the print dialog could not be opened");
		}
		const print: FramePrint = { frame, stillThereWhenPrintReturned: undefined };
		framePrints.push(print);
		if (dialog === "closes before print returns") {
			frameWindow.dispatchEvent(new frameWindow.Event("afterprint"));
		}
		print.stillThereWhenPrintReturned = frame.isConnected;
	};
};

/**
 * What jsdom's own frame lacks, stood in for on every frame the app makes: the print dialog, the document's fonts
 * (answered by the same stand-in as the print page's), the decoding of a picture such as the Lighthouse mark, and
 * the loading of a copied style sheet. jsdom ignores a frame's srcdoc, so the frame holds the blank document it is
 * born with, which is the one the app draws into.
 */
const standInForTheFramesBrowser = (
	frame: HTMLIFrameElement,
	{ dialog = "opens", styleSheets = "load" }: PrintFrameBrowserOptions,
) => {
	const frameWindow = frame.contentWindow as FramesWindow | null;
	const frameDocument = frame.contentDocument;
	if (frameWindow === null || frameDocument === null) return;
	standInForTheFramesPrint(frame, frameWindow, dialog);
	Object.defineProperty(frameDocument, "fonts", {
		configurable: true,
		value: {
			load: async (face: string) =>
				(await theBrowsersStandIns().loadFonts([face])) === "loaded"
					? [A_FACE_FOUND]
					: [],
			ready: Promise.resolve(),
		},
	});
	Object.defineProperty(frameWindow.HTMLImageElement.prototype, "decode", {
		configurable: true,
		value: async function (this: HTMLImageElement) {
			const outcome = await thePictures(this.getAttribute("src") ?? "");
			if (outcome !== "loaded") {
				throw new Error("the picture could not be fetched");
			}
		},
	});
	answerTheSheetsCopiedInto(frameDocument.head, styleSheets);
};

/**
 * The browser around every frame an export makes from now on, until forgetTheBrowser: each frame's print dialog
 * is a recorder, and its fonts, pictures and style sheets answer as the test sets them.
 */
export const aPrintFrameBrowser = (options: PrintFrameBrowserOptions = {}) => {
	theFrameWatch?.disconnect();
	framePrints = [];
	theFrameWatch = new MutationObserver((changes) => {
		for (const change of changes) {
			for (const added of Array.from(change.addedNodes)) {
				if (added.nodeName === "IFRAME") {
					standInForTheFramesBrowser(added as HTMLIFrameElement, options);
				}
			}
		}
	});
	theFrameWatch.observe(document.body, { childList: true, subtree: true });
	return {
		/** How often a frame's own window was asked for the print dialog. */
		timesAFramePrinted: () => framePrints.length,
		/** The frames whose window was asked for the print dialog, in order. */
		framesThatPrinted: () => framePrints.map(({ frame }) => frame),
		/** Whether each frame was still in the page when its print() returned, in order. */
		stillThereWhenPrintReturned: () =>
			framePrints.map(({ stillThereWhenPrintReturned }) =>
				Boolean(stillThereWhenPrintReturned),
			),
		/** Closes the dialog of the frame that printed last, the way the browser tells that frame's window. */
		closeTheDialog: () =>
			act(() => {
				const last = framePrints[framePrints.length - 1]?.frame;
				const frameWindow = last?.contentWindow as FramesWindow | undefined;
				frameWindow?.dispatchEvent(new frameWindow.Event("afterprint"));
			}),
	};
};

const forgetThePrintFrames = () => {
	theFrameWatch?.disconnect();
	theFrameWatch = undefined;
	framePrints = [];
	for (const frame of thePrintFrames()) frame.remove();
};

/** Every frame in the app's page; an export makes one, and only while it prints. */
export const thePrintFrames = (): HTMLIFrameElement[] =>
	Array.from(document.querySelectorAll("iframe"));

/** The one print frame there is now; there must be exactly one. */
export const thePrintFrame = (): HTMLIFrameElement => {
	const frames = thePrintFrames();
	expect(frames, "there is not exactly one print frame").toHaveLength(1);
	return frames[0];
};

/** The page drawn inside the print frame: the body of the frame's own document. */
export const theFramesPage = (): HTMLElement => {
	const page = thePrintFrame().contentDocument?.body;
	expect(page, "the print frame has no document").toBeTruthy();
	return page as HTMLElement;
};

/** Waits until the export's frame has asked for its print dialog as often as given. */
export const theFramesHavePrinted = (
	frames: ReturnType<typeof aPrintFrameBrowser>,
	times = 1,
) => waitFor(() => expect(frames.timesAFramePrinted()).toBe(times));

/** Waits until no print frame is left in the page. */
export const everyPrintFrameIsGone = () =>
	waitFor(() => expect(thePrintFrames()).toHaveLength(0));

// --- The app's error snackbar ---

// MUI marks its snackbar by class alone; a page's own alerts, such as a section that could not be shown, are not it.
const theSnackbarsMessage = (): HTMLElement | null =>
	document.querySelector<HTMLElement>(".MuiSnackbar-root [role='alert']");

/** The error snackbar's message, once it shows one. */
export const theErrorSnackbar = async (): Promise<HTMLElement> => {
	await waitFor(() =>
		expect(
			theSnackbarsMessage(),
			"the error snackbar says nothing",
		).not.toBeNull(),
	);
	return theSnackbarsMessage() as HTMLElement;
};

/** Whether the error snackbar shows a message now. */
export const theErrorSnackbarIsShown = (): boolean =>
	theSnackbarsMessage() !== null;

/** The snackbar's Retry, which only a failure that asking again may mend offers. */
export const theSnackbarsRetry = (snackbar: HTMLElement): HTMLElement | null =>
	within(snackbar).queryByRole("button", { name: "Retry" });

// --- What the print page says about itself ---

const thePrintPageIfDrawn = (): HTMLElement | null =>
	document.querySelector<HTMLElement>("[data-render-ready]");

export const thePrintPage = (): HTMLElement => {
	const page = thePrintPageIfDrawn();
	expect(page, "there is no print page").not.toBeNull();
	return page as HTMLElement;
};

/** Every style the page has written, as one text. */
export const theStylesOfThePage = () =>
	Array.from(document.querySelectorAll("style"))
		.map((style) => style.textContent ?? "")
		.join("\n");

/** What the page tells a renderer about itself: "true" only once everything it prints is in. */
export const renderReadiness = (): string | undefined =>
	thePrintPageIfDrawn()?.dataset.renderReady;

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
