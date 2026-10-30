import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { act } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { deferred } from "../../../../tests/HeldMetricsService";
import { inSequence } from "../../../../tests/inSequence";
import { browserPrintEffects, openPrintFrame } from "./browserPrintEffects";

/**
 * The report's reach into the real browser, with only what jsdom lacks stood in for: the dialog is asked for through
 * the window's print, the dialog closing is heard on that window, the fonts are loaded into its document and
 * answered as loaded only when every face was found, and a picture is answered as loaded only once it is decoded.
 * The binding is made for one window: the app's own for the print page opened from its address, the hidden frame's
 * for an export. The frame itself is made here too: off-screen, at paper size, unseen and untabbable, carrying the
 * app's own style sheets. jsdom loads no srcdoc document and no style sheet, so these scenarios prove which window,
 * element and sheets are used; that an engine prints them is for the end-to-end check and the hand probe.
 */

const REPORT_PDF_FRAME = "Report PDF";
const A_TITLE = "Then & Now — engagement start — Team Lightspeed";

const theOnlyFrame = (): HTMLIFrameElement => {
	const frames = Array.from(document.querySelectorAll("iframe"));
	expect(frames).toHaveLength(1);
	return frames[0];
};

const forTheAppsWindow = () => browserPrintEffects(globalThis.window);

type FramesWindow = Window & typeof globalThis;

/** A frame of the app's page, as jsdom makes one: its own window and document, born blank. */
const aFrameInThePage = () => {
	const frame = document.createElement("iframe");
	document.body.append(frame);
	return {
		frame,
		frameWindow: frame.contentWindow as FramesWindow,
		frameDocument: frame.contentDocument as Document,
	};
};

/**
 * The style sheets copied into the frames the factory makes, answered as a browser answers once it has fetched each
 * one, or failed to: at once, or only once the test lets them through.
 */
const theFramesSheets = (answer: "load" | "error", held = false) => {
	const waiting: HTMLLinkElement[] = [];
	let letThrough = !held;
	const answerTo = (link: HTMLLinkElement) =>
		setTimeout(() =>
			link.dispatchEvent(
				new (link.ownerDocument.defaultView as FramesWindow).Event(answer),
			),
		);
	const watching: MutationObserver[] = [];
	const watch = (target: Node, onAdded: (node: Node) => void) => {
		const observer = new MutationObserver((changes) => {
			for (const change of changes) {
				for (const node of Array.from(change.addedNodes)) onAdded(node);
			}
		});
		observer.observe(target, { childList: true });
		watching.push(observer);
	};
	watch(document.body, (node) => {
		const head = (node as HTMLIFrameElement).contentDocument?.head;
		if (node.nodeName !== "IFRAME" || !head) return;
		watch(head, (added) => {
			if (added.nodeName !== "LINK") return;
			if (letThrough) answerTo(added as HTMLLinkElement);
			else waiting.push(added as HTMLLinkElement);
		});
	});
	return {
		letThemThrough: () => {
			letThrough = true;
			for (const link of waiting.splice(0)) answerTo(link);
		},
		stop: () => {
			for (const observer of watching) observer.disconnect();
		},
	};
};

/** The app's own head as the built app carries it: a style sheet, a rule of its own, and one Emotion wrote. */
const theAppsHead = () => {
	const sheet = document.createElement("link");
	sheet.rel = "stylesheet";
	sheet.href = "/assets/index-3f9a1c.css";
	const ownRules = document.createElement("style");
	ownRules.textContent = "body { margin: 0; }";
	const emotions = document.createElement("style");
	emotions.dataset.emotion = "css";
	emotions.textContent = ".css-1x2y3z { background-color: #121212; }";
	document.head.append(sheet, ownRules, emotions);
	return {
		sheet: sheet.getAttribute("href"),
		ownRules: ownRules.textContent,
		emotions: emotions.textContent,
		takeAway: () => {
			sheet.remove();
			ownRules.remove();
			emotions.remove();
		},
	};
};

const theBrowsersPrint = () => {
	const print = vi.fn();
	vi.stubGlobal("print", print);
	return print;
};

const theDialogCloses = () =>
	act(() => {
		globalThis.dispatchEvent(new Event("afterprint"));
	});

const theDocumentsFontsAre = (
	load: (face: string) => Promise<FontFace[]>,
	ready: Promise<unknown> = Promise.resolve(),
) =>
	Object.defineProperty(document, "fonts", {
		configurable: true,
		value: { load, ready },
	});

const A_FACE = { family: "Quicksand" } as unknown as FontFace;
const FACES = ["400 1em Quicksand", "700 1em Quicksand"];
const A_PICTURE = "/icons/icon-512x512.png";

const THE_TYPEFACES_STYLES = resolve(
	dirname(fileURLToPath(import.meta.url)),
	"../../../../../node_modules/@fontsource-variable/quicksand/index.css",
);
const THE_PRINTED_FACES = [
	'400 1em "Quicksand Variable"',
	'700 1em "Quicksand Variable"',
];

interface TypefacePart {
	readonly name: string;
	readonly ranges: readonly (readonly [number, number])[];
}

/** The code points one face of the typeface draws, as its unicode-range declares them. */
const rangesOf = (declarations: string): [number, number][] =>
	(/unicode-range:\s*([^;]+);/.exec(declarations)?.[1] ?? "")
		.split(",")
		.map((range) => {
			const [from, to = from] = range.trim().replace("U+", "").split("-");
			return [Number.parseInt(from, 16), Number.parseInt(to, 16)];
		});

/** The typeface as the app ships it: one file per part of the alphabet, read off the package's own style sheet. */
const theTypefacesParts = (): TypefacePart[] =>
	Array.from(
		readFileSync(THE_TYPEFACES_STYLES, "utf8").matchAll(
			/\/\* quicksand-([\w-]+)-wght-normal \*\/\s*@font-face\s*\{([^}]*)\}/g,
		),
	).map(([, name, declarations]) => ({ name, ranges: rangesOf(declarations) }));

const draws = (part: TypefacePart, text: string) =>
	Array.from(text).some((letter) => {
		const at = letter.codePointAt(0) ?? 0;
		return part.ranges.some(([from, to]) => at >= from && at <= to);
	});

/**
 * The document's fonts as a browser holds the typeface: one face per part, each fetched only when it is asked for.
 * Asked by a face's description, the browser fetches only the parts that draw the sample text, which is a lone
 * space when none is given; asked directly, a face fetches itself. A refused part fails whichever way it is asked.
 */
const theBrowserHoldsTheTypeface = (refused: readonly string[] = []) => {
	const fetched = new Set<string>();
	const faces = theTypefacesParts().map((part) => {
		const face = {
			family: "Quicksand Variable",
			style: "normal",
			weight: "300 700",
			status: "unloaded",
			load: () => {
				if (refused.includes(part.name)) {
					face.status = "error";
					return Promise.reject(
						new Error(`the ${part.name} part could not be fetched`),
					);
				}
				fetched.add(part.name);
				face.status = "loaded";
				return Promise.resolve(face);
			},
		};
		return { part, face };
	});
	const load = async (font: string, text = " ") =>
		font.includes("Quicksand Variable")
			? Promise.all(
					faces
						.filter(({ part }) => draws(part, text))
						.map(({ face }) => face.load()),
				)
			: [];
	const held = faces.map(({ face }) => face);
	Object.defineProperty(document, "fonts", {
		configurable: true,
		value: {
			load,
			ready: Promise.resolve(),
			size: held.length,
			values: () => held.values(),
			forEach: (each: (face: unknown) => void) => {
				held.forEach(each);
			},
			[Symbol.iterator]: () => held.values(),
		},
	});
	return fetched;
};

// jsdom draws no pictures, so it has no decode of its own to stand in for.
const thePicturesDecodeBy = (
	decode: (this: HTMLImageElement) => Promise<void>,
) =>
	Object.defineProperty(HTMLImageElement.prototype, "decode", {
		configurable: true,
		value: decode,
	});

afterEach(() => {
	vi.unstubAllGlobals();
	Reflect.deleteProperty(HTMLImageElement.prototype, "decode");
	Reflect.deleteProperty(document, "fonts");
	for (const frame of Array.from(document.querySelectorAll("iframe"))) {
		frame.remove();
	}
	document.title = "";
});

describe("the browser's print effects, made for the app's own window", () => {
	it("asks the browser for its print dialog once per print", () => {
		const print = theBrowsersPrint();

		forTheAppsWindow().print();

		expect(print).toHaveBeenCalledTimes(1);
	});

	it("hears the dialog close until it is told to stop listening", () => {
		const listener = vi.fn();
		const stopListening = forTheAppsWindow().onAfterPrint(listener);

		theDialogCloses();
		stopListening();
		theDialogCloses();

		expect(listener).toHaveBeenCalledTimes(1);
	});

	it("loads every face it is asked for and answers loaded once the document's fonts are ready", async () => {
		const ready = deferred<void>();
		const load = vi.fn(async (_face: string) => [A_FACE]);
		theDocumentsFontsAre(load, ready.promise);
		let answer: string | undefined;

		const asked = forTheAppsWindow()
			.loadFonts(FACES)
			.then((outcome) => {
				answer = outcome;
			});
		// A whole turn of the event loop, so an answer that does not wait for the fonts has surely come by now.
		await new Promise((settle) => setTimeout(settle));
		expect(answer).toBeUndefined();
		ready.resolve();
		await asked;

		expect(load.mock.calls.map(([face]) => face)).toEqual(FACES);
		expect(answer).toBe("loaded");
	});

	it.each<[string, (face: string) => Promise<FontFace[]>]>([
		[
			"refused",
			() => Promise.reject(new Error("the font could not be fetched")),
		],
		["finds no face", async () => []],
	])("answers failed when a face's load is %s", async (_how, load) => {
		theDocumentsFontsAre(load);

		await expect(forTheAppsWindow().loadFonts(FACES)).resolves.toBe("failed");
	});

	it("answers failed when one face is found and another is not", async () => {
		theDocumentsFontsAre(async (face) => (face === FACES[0] ? [A_FACE] : []));

		await expect(forTheAppsWindow().loadFonts(FACES)).resolves.toBe("failed");
	});

	// A name such as "Zespół Łódź" needs the typeface's Central European letters; fetched only for a space, they
	// would still be on their way when the page prints, and the name would come out in a machine's font.
	it(`loads every part of the typeface before it answers loaded, so a letter beyond basic Latin prints in it too`, async () => {
		const fetched = theBrowserHoldsTheTypeface();
		const parts = theTypefacesParts().map(({ name }) => name);
		expect(parts).toEqual(
			expect.arrayContaining(["latin", "latin-ext", "vietnamese"]),
		);

		const answer = await forTheAppsWindow().loadFonts(THE_PRINTED_FACES);

		expect(answer).toBe("loaded");
		expect([...fetched].sort((a, b) => a.localeCompare(b))).toEqual(
			[...parts].sort((a, b) => a.localeCompare(b)),
		);
	});

	it(`answers failed when the part of the typeface with the Central European letters cannot be fetched`, async () => {
		theBrowserHoldsTheTypeface(["latin-ext"]);

		await expect(forTheAppsWindow().loadFonts(THE_PRINTED_FACES)).resolves.toBe(
			"failed",
		);
	});

	it("asks for the picture at the address and answers loaded once it has been decoded", async () => {
		const decoded: string[] = [];
		thePicturesDecodeBy(function (this: HTMLImageElement) {
			decoded.push(this.getAttribute("src") ?? "");
			return Promise.resolve();
		});

		await expect(forTheAppsWindow().loadPicture(A_PICTURE)).resolves.toBe(
			"loaded",
		);
		expect(decoded).toEqual([A_PICTURE]);
	});

	it("answers failed when the picture cannot be decoded", async () => {
		thePicturesDecodeBy(() =>
			Promise.reject(new Error("the picture could not be fetched")),
		);

		await expect(forTheAppsWindow().loadPicture(A_PICTURE)).resolves.toBe(
			"failed",
		);
	});
});

describe("the browser's print effects, made for the print frame's window", () => {
	// Only the frame's document goes to a dialog asked for on the frame's window, so the app around it never prints.
	it(`asks the frame's window for the print dialog, never the app's, and hears that window's dialog close`, () => {
		const theAppsPrint = theBrowsersPrint();
		const { frameWindow } = aFrameInThePage();
		const theFramesPrint = vi.fn();
		frameWindow.print = theFramesPrint;
		const effects = browserPrintEffects(frameWindow);
		const listener = vi.fn();
		effects.onAfterPrint(listener);

		effects.print();
		theDialogCloses();
		act(() => {
			frameWindow.dispatchEvent(new frameWindow.Event("afterprint"));
		});

		expect(theFramesPrint).toHaveBeenCalledTimes(1);
		expect(theAppsPrint).not.toHaveBeenCalled();
		expect(listener).toHaveBeenCalledTimes(1);
	});

	// A face or a picture loaded into the app's page would leave the frame's printout in a fallback font, unmarked.
	it(`loads the faces into the frame document's fonts and the Lighthouse mark as a picture of the frame, not the app's`, async () => {
		theDocumentsFontsAre(() =>
			Promise.reject(new Error("the app's own fonts were asked")),
		);
		const { frameWindow, frameDocument } = aFrameInThePage();
		const framesFaces: string[] = [];
		Object.defineProperty(frameDocument, "fonts", {
			configurable: true,
			value: {
				load: (face: string) => {
					framesFaces.push(face);
					return Promise.resolve([A_FACE]);
				},
				ready: Promise.resolve(),
			},
		});
		const decodedInTheFrame: string[] = [];
		Object.defineProperty(frameWindow.HTMLImageElement.prototype, "decode", {
			configurable: true,
			value: function (this: HTMLImageElement) {
				decodedInTheFrame.push(this.getAttribute("src") ?? "");
				return Promise.resolve();
			},
		});
		const effects = browserPrintEffects(frameWindow);

		await expect(effects.loadFonts(FACES)).resolves.toBe("loaded");
		await expect(effects.loadPicture(A_PICTURE)).resolves.toBe("loaded");

		expect(framesFaces).toEqual(FACES);
		expect(decodedInTheFrame).toEqual([A_PICTURE]);
	});

	// Engines differ on which title they offer as the PDF's file name, so both carry the report's, until the frame goes.
	it(`names the frame's document and the app's after the report, and gives the app its own name back when asked`, () => {
		document.title = "Lighthouse";
		const { frameWindow, frameDocument } = aFrameInThePage();
		const effects = browserPrintEffects(frameWindow);
		expect(typeof effects.setTitle, "the binding sets no title").toBe(
			"function",
		);

		const giveTheNameBack = effects.setTitle(A_TITLE);
		expect(frameDocument.title).toBe(A_TITLE);
		expect(document.title).toBe(A_TITLE);
		giveTheNameBack();

		expect(document.title).toBe("Lighthouse");
	});
});

/**
 * An offset that carries a whole A4 sheet past the screen's top or left edge, so the frame is laid out but never
 * seen; a frame fixed at the page's corner would cover what the reader is looking at.
 */
const liesBeyondTheScreen = (offset: string): boolean => {
	const match = /^(-\d+(?:\.\d+)?)(px|mm|vw|vh|%)$/.exec(offset.trim());
	if (match === null) return false;
	const distance = -Number(match[1]);
	const aWholeSheet = { px: 1123, mm: 297, vw: 100, vh: 100, "%": 100 };
	return distance >= aWholeSheet[match[2] as keyof typeof aWholeSheet];
};

describe("the print frame an export draws the report into", () => {
	// Never display: none or zero-sized: the frame must lay out at paper size and stay printable in every engine.
	it(`makes one off-screen frame under the app's page, at A4 size, unseen, untabbable and titled, and answers once it has loaded`, async () => {
		document.documentElement.lang = "en";

		const opened = await openPrintFrame();

		const frames = Array.from(document.body.querySelectorAll("iframe"));
		expect(frames).toHaveLength(1);
		const [frame] = frames;
		expect(frame).toHaveAttribute("aria-hidden", "true");
		expect(frame).toHaveAttribute("tabindex", "-1");
		expect(frame).toHaveAttribute("title", REPORT_PDF_FRAME);
		expect(frame.style.position).toBe("fixed");
		expect(
			[frame.style.left, frame.style.top].some(liesBeyondTheScreen),
			`the frame sits on the page at left ${frame.style.left || "auto"}, top ${frame.style.top || "auto"}`,
		).toBe(true);
		expect(frame.style.width).toBe("210mm");
		expect(frame.style.height).toBe("297mm");
		expect(frame.style.display).not.toBe("none");
		expect(frame.srcdoc).toMatch(/^<!doctype html>/i);
		expect(frame.srcdoc).toContain('lang="en"');
		expect(opened.window).toBe(frame.contentWindow);
		expect(opened.body).toBe(frame.contentDocument?.body);
		expect(opened.head).toBe(frame.contentDocument?.head);
	});

	// The bundled typeface is declared in the app's own sheets; Emotion's styles are the app's dark screen, not paper.
	it(`copies the app's own style sheets into the frame, but none that Emotion wrote, and answers only once each copied sheet has loaded`, async () => {
		const head = theAppsHead();
		const answered = vi.fn();
		const sheets = theFramesSheets("load", true);
		try {
			const opening = openPrintFrame().then((frame) => {
				answered();
				return frame;
			});
			// A few turns of the event loop: the frame loads and its sheets are copied, but none has loaded yet.
			await inSequence(
				Array.from({ length: 5 }),
				() => new Promise((settle) => setTimeout(settle)),
			);
			expect(
				answered,
				"it answered before the sheets had loaded",
			).not.toHaveBeenCalled();
			sheets.letThemThrough();
			const frame = await opening;

			expect(answered).toHaveBeenCalledTimes(1);
			const framesHead = frame.head;
			expect(
				Array.from(
					framesHead.querySelectorAll<HTMLLinkElement>(
						'link[rel="stylesheet"]',
					),
				).map((link) => link.getAttribute("href")),
			).toEqual([head.sheet]);
			const copiedRules = Array.from(framesHead.querySelectorAll("style")).map(
				(style) => style.textContent,
			);
			expect(copiedRules).toContain(head.ownRules);
			expect(copiedRules).not.toContain(head.emotions);
		} finally {
			sheets.stop();
			head.takeAway();
		}
	});

	// A sheet that never reached the frame would print the report in a fallback, so no PDF is made from it.
	it(`refuses to answer with a frame when one of the app's style sheets cannot be loaded into it`, async () => {
		const head = theAppsHead();
		const sheets = theFramesSheets("error");
		try {
			await expect(openPrintFrame()).rejects.toBeDefined();
		} finally {
			sheets.stop();
			head.takeAway();
		}
	});

	it(`carries the page's language into the frame's document intact, even one holding characters markup treats specially`, async () => {
		const anOddLanguage = 'x-"a&b<c"';
		document.documentElement.lang = anOddLanguage;
		try {
			await openPrintFrame();

			const framesMarkup = new DOMParser().parseFromString(
				theOnlyFrame().srcdoc,
				"text/html",
			);
			expect(framesMarkup.documentElement.lang).toBe(anOddLanguage);
		} finally {
			document.documentElement.lang = "";
		}
	});

	it(`says which of the app's style sheets could not be loaded into the frame`, async () => {
		const head = theAppsHead();
		const sheets = theFramesSheets("error");
		try {
			await expect(openPrintFrame()).rejects.toThrow(
				/^The style sheet \S*\/assets\/index-3f9a1c\.css could not be loaded$/,
			);
		} finally {
			sheets.stop();
			head.takeAway();
		}
	});

	it(`says the frame itself could not be loaded, and takes it away, when the browser cannot load it`, async () => {
		const failing = new MutationObserver((changes) => {
			for (const change of changes) {
				for (const node of Array.from(change.addedNodes)) {
					if (node.nodeName === "IFRAME")
						node.dispatchEvent(new Event("error"));
				}
			}
		});
		failing.observe(document.body, { childList: true });
		try {
			await expect(openPrintFrame()).rejects.toThrow(
				/^The print frame could not be loaded$/,
			);
			expect(document.querySelectorAll("iframe")).toHaveLength(0);
		} finally {
			failing.disconnect();
		}
	});

	it(`stops waiting, and takes the frame away, once the caller gives up before it has loaded`, async () => {
		const givingUp = new AbortController();

		const opening = openPrintFrame(givingUp.signal);
		givingUp.abort();

		await expect(opening).rejects.toThrow(/^The print frame was given up$/);
		expect(document.querySelectorAll("iframe")).toHaveLength(0);
	});

	it(`takes away exactly the frame it made when closed, and leaves the rest of the page as it was`, async () => {
		const { frame: another } = aFrameInThePage();

		const opened = await openPrintFrame();
		opened.close();

		expect(Array.from(document.body.querySelectorAll("iframe"))).toEqual([
			another,
		]);
	});
});
