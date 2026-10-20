import type { PrintEffectPort, PrintLoadOutcome } from "./usePrintOnce";

/** A browser window with its own constructors, such as the app's or the print frame's. */
export type PrintWindow = Window & typeof globalThis;

// The typeface ships as one file per part of the alphabet, and the browser fetches only the parts that draw the text
// it is asked about. A basic Latin, a Central European and a Vietnamese letter ask for every part, so a name in any of
// them prints in the typeface rather than in a fallback font.
const A_LETTER_FROM_EVERY_PART = "AĀạ";

const PRINT_FRAME_TITLE = "Report PDF";

const loadTheFaces = async (
	target: PrintWindow,
	faces: readonly string[],
): Promise<PrintLoadOutcome> => {
	try {
		const found = await Promise.all(
			faces.map((face) =>
				target.document.fonts.load(face, A_LETTER_FROM_EVERY_PART),
			),
		);
		await target.document.fonts.ready;
		return found.every((matches) => matches.length > 0) ? "loaded" : "failed";
	} catch {
		return "failed";
	}
};

// Decoded, not merely fetched, so the picture is ready to paint the moment the print dialog draws the page.
const loadThePicture = async (
	target: PrintWindow,
	source: string,
): Promise<PrintLoadOutcome> => {
	const picture = new target.Image();
	picture.src = source;
	try {
		await picture.decode();
		return "loaded";
	} catch {
		return "failed";
	}
};

// Engines differ on which document's title they offer as the PDF's file name, so both carry the report's.
const nameAfterTheReport = (target: PrintWindow, title: string) => {
	const theAppsOwn = document.title;
	target.document.title = title;
	document.title = title;
	return () => {
		document.title = theAppsOwn;
	};
};

/**
 * The report's reach into the browser for one window: its print dialog, the dialog closing, its document's fonts,
 * pictures and title. The one place that names the browser's own print and fonts, so everything else can be given a
 * stand-in.
 */
export const browserPrintEffects = (target: PrintWindow): PrintEffectPort => ({
	print: () => target.print(),
	onAfterPrint: (listener) => {
		target.addEventListener("afterprint", listener);
		return () => target.removeEventListener("afterprint", listener);
	},
	loadFonts: (faces) => loadTheFaces(target, faces),
	loadPicture: (source) => loadThePicture(target, source),
	setTitle: (title) => nameAfterTheReport(target, title),
});

/** The hidden frame an export draws the report into, and the way to take it away again. */
export interface PrintFrame {
	readonly window: PrintWindow;
	readonly head: HTMLElement;
	readonly body: HTMLElement;
	readonly close: () => void;
}

const asAnAttribute = (value: string): string =>
	value
		.replaceAll("&", "&amp;")
		.replaceAll('"', "&quot;")
		.replaceAll("<", "&lt;");

const aBlankPage = (lang: string): string =>
	`<!doctype html><html lang="${asAnAttribute(lang)}"><head><meta charset="utf-8"></head><body></body></html>`;

// Laid out at paper size but a whole sheet above the screen: a frame that is not displayed, or has no size, prints
// blank in some engines, and one on the screen would cover what the reader is looking at.
const placeOffTheScreen = (frame: HTMLIFrameElement): void => {
	Object.assign(frame.style, {
		position: "fixed",
		top: "-297mm",
		left: "0",
		width: "210mm",
		height: "297mm",
		border: "0",
		pointerEvents: "none",
	});
};

const hasLoaded = (element: HTMLElement, what: string): Promise<void> =>
	new Promise((resolve, reject) => {
		element.addEventListener("load", () => resolve(), { once: true });
		element.addEventListener(
			"error",
			() => reject(new Error(`${what} could not be loaded`)),
			{ once: true },
		);
	});

// Emotion's styles are left behind: they are the app's screen theme, and the frame writes its own.
const copyTheAppsStyleSheets = (into: Document): Promise<void>[] =>
	Array.from(
		document.querySelectorAll<HTMLLinkElement | HTMLStyleElement>(
			'link[rel="stylesheet"], style:not([data-emotion])',
		),
	).map((sheet) => {
		const copy = into.importNode(sheet, true);
		const loading =
			sheet instanceof HTMLLinkElement
				? hasLoaded(copy, `The style sheet ${sheet.href}`)
				: Promise.resolve();
		into.head.append(copy);
		return loading;
	});

/** Fails once the caller gives up, so a load the browser never answers cannot keep the frame in the page. */
const whenGivenUp = (signal: AbortSignal | undefined): Promise<never> => {
	const givenUp = new Promise<never>((_, reject) => {
		signal?.addEventListener(
			"abort",
			() => reject(new Error("The print frame was given up")),
			{ once: true },
		);
	});
	// Giving up after the frame has opened changes nothing, so that failure is nobody's to hear.
	givenUp.catch(() => undefined);
	return givenUp;
};

/**
 * Makes the hidden frame an export prints from, under the app's page, carrying the app's own style sheets, and answers
 * once the frame and every copied sheet have loaded; a sheet that cannot be loaded, or a caller who gives up first,
 * takes the frame away again.
 */
export const openPrintFrame = async (
	signal?: AbortSignal,
): Promise<PrintFrame> => {
	const theAppsTitle = document.title;
	const frame = document.createElement("iframe");
	frame.title = PRINT_FRAME_TITLE;
	frame.setAttribute("aria-hidden", "true");
	frame.tabIndex = -1;
	placeOffTheScreen(frame);
	frame.srcdoc = aBlankPage(document.documentElement.lang);
	const close = () => {
		frame.remove();
		document.title = theAppsTitle;
	};
	const givenUp = whenGivenUp(signal);
	try {
		const loading = hasLoaded(frame, "The print frame");
		document.body.append(frame);
		await Promise.race([loading, givenUp]);
		// Read only after the load: an engine may swap the frame's first blank document for the one it was given.
		const frameWindow = frame.contentWindow as PrintWindow | null;
		const frameDocument = frame.contentDocument;
		if (frameWindow === null || frameDocument === null) {
			throw new Error("The print frame has no document");
		}
		await Promise.race([
			Promise.all(copyTheAppsStyleSheets(frameDocument)),
			givenUp,
		]);
		return {
			window: frameWindow,
			head: frameDocument.head,
			body: frameDocument.body,
			close,
		};
	} catch (error_) {
		close();
		throw error_;
	}
};
