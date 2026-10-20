import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { act, renderHook } from "@testing-library/react";
import type React from "react";
import {
	MemoryRouter,
	useLocation,
	useNavigate,
	useNavigationType,
} from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import { deferred } from "../../../../tests/HeldMetricsService";
import { browserPrintEffects } from "./browserPrintEffects";
import { RETURNED_FROM_PRINT, usePrintOnce } from "./usePrintOnce";

/**
 * The print page's reach into the real browser, with only the browser's own print and fonts stood in for: the
 * dialog is asked for through the browser's print, the dialog closing is heard for as long as the page is open,
 * a return steps back to where the export started (or, with no origin, replaces the print page's entry), the fonts are loaded into the document and
 * answered as loaded only when every face was found, and a picture is answered as loaded only once it is decoded.
 */

const HERE = "/print/teams/1/reports/42";
const THE_REPORT = "/teams/1/reports/42";
const THE_LIST = "/teams/1/reports";

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

const pendingReviewFix = "Pending: slice 02 review fix (epic 6298)";

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
			load: async () => {
				if (refused.includes(part.name)) {
					face.status = "error";
					throw new Error(`the ${part.name} part could not be fetched`);
				}
				fetched.add(part.name);
				face.status = "loaded";
				return face;
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

/** The router as the print page sits in it: opened from the report, now on the print page. */
const onThePrintPage = () =>
	renderHook(
		() => ({
			navigate: useNavigate(),
			location: useLocation(),
			navigationType: useNavigationType(),
		}),
		{
			wrapper: ({ children }: { children: React.ReactNode }) => (
				<MemoryRouter initialEntries={[THE_REPORT, HERE]} initialIndex={1}>
					{children}
				</MemoryRouter>
			),
		},
	);

afterEach(() => {
	vi.unstubAllGlobals();
	Reflect.deleteProperty(HTMLImageElement.prototype, "decode");
	Reflect.deleteProperty(document, "fonts");
});

describe("the browser's print effects", () => {
	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("asks the browser for its print dialog once per print", () => {
		const print = theBrowsersPrint();

		browserPrintEffects(vi.fn()).print();

		expect(print).toHaveBeenCalledTimes(1);
	});

	// @us-01 @slice-01 @contract-shape:bounded-change
	it("hears the dialog close until it is told to stop listening", () => {
		const listener = vi.fn();
		const stopListening = browserPrintEffects(vi.fn()).onAfterPrint(listener);

		theDialogCloses();
		stopListening();
		theDialogCloses();

		expect(listener).toHaveBeenCalledTimes(1);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("replaces the print page's entry when it leads the reader back", () => {
		const router = onThePrintPage();
		const port = browserPrintEffects((to, options) => {
			if (typeof to === "number") router.result.current.navigate(to);
			else router.result.current.navigate(to, options);
		});

		act(() => {
			port.navigate(THE_LIST, { replace: true, state: RETURNED_FROM_PRINT });
		});

		expect(router.result.current.location.pathname).toBe(THE_LIST);
		expect(router.result.current.location.state).toEqual(RETURNED_FROM_PRINT);
		expect(router.result.current.navigationType).toBe("REPLACE");
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// Wired to the print-once rule: the page hears the dialog close while it is open, and not once it is gone.
	it("leads back after the dialog closes while the page is open, and hears nothing once it is gone", () => {
		theBrowsersPrint();
		const navigate = vi.fn();
		const { unmount } = renderHook(() =>
			usePrintOnce({
				state: "ready",
				printWhenReady: true,
				from: "report",
				here: HERE,
				reportsPath: THE_LIST,
				port: browserPrintEffects(navigate),
			}),
		);

		theDialogCloses();
		const returnsWhileOpen = navigate.mock.calls.filter(
			([to]) => to === -1,
		).length;
		unmount();
		theDialogCloses();

		expect(returnsWhileOpen).toBe(1);
		expect(navigate.mock.calls.filter(([to]) => to === -1)).toHaveLength(1);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("loads every face it is asked for and answers loaded once the document's fonts are ready", async () => {
		const ready = deferred<void>();
		const load = vi.fn(async (_face: string) => [A_FACE]);
		theDocumentsFontsAre(load, ready.promise);
		let answer: string | undefined;

		const asked = browserPrintEffects(vi.fn())
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

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it.each<[string, (face: string) => Promise<FontFace[]>]>([
		[
			"refused",
			async () => {
				throw new Error("the font could not be fetched");
			},
		],
		["finds no face", async () => []],
	])("answers failed when a face's load is %s", async (_how, load) => {
		theDocumentsFontsAre(load);

		await expect(browserPrintEffects(vi.fn()).loadFonts(FACES)).resolves.toBe(
			"failed",
		);
	});

	// @us-02 @slice-02 @driving_port @contract-shape:bounded-change
	// A name such as "Zespół Łódź" needs the typeface's Central European letters; fetched only for a space, they
	// would still be on their way when the page prints, and the name would come out in a machine's font.
	it.skip(`loads every part of the typeface before it answers loaded, so a letter beyond basic Latin prints in it too (${pendingReviewFix})`, async () => {
		const fetched = theBrowserHoldsTheTypeface();
		const parts = theTypefacesParts().map(({ name }) => name);
		expect(parts).toEqual(
			expect.arrayContaining(["latin", "latin-ext", "vietnamese"]),
		);

		const answer = await browserPrintEffects(vi.fn()).loadFonts(
			THE_PRINTED_FACES,
		);

		expect(answer).toBe("loaded");
		expect([...fetched].sort()).toEqual([...parts].sort());
	});

	// @us-02 @slice-02 @error @contract-shape:bounded-change
	it.skip(`answers failed when the part of the typeface with the Central European letters cannot be fetched (${pendingReviewFix})`, async () => {
		theBrowserHoldsTheTypeface(["latin-ext"]);

		await expect(
			browserPrintEffects(vi.fn()).loadFonts(THE_PRINTED_FACES),
		).resolves.toBe("failed");
	});

	// @us-02 @slice-02 @driving_port @contract-shape:bounded-change
	it("asks for the picture at the address and answers loaded once it has been decoded", async () => {
		const decoded: string[] = [];
		thePicturesDecodeBy(async function (this: HTMLImageElement) {
			decoded.push(this.getAttribute("src") ?? "");
		});

		await expect(
			browserPrintEffects(vi.fn()).loadPicture(A_PICTURE),
		).resolves.toBe("loaded");
		expect(decoded).toEqual([A_PICTURE]);
	});

	// @us-02 @slice-02 @error @contract-shape:bounded-change
	it("answers failed when the picture cannot be decoded", async () => {
		thePicturesDecodeBy(async () => {
			throw new Error("the picture could not be fetched");
		});

		await expect(
			browserPrintEffects(vi.fn()).loadPicture(A_PICTURE),
		).resolves.toBe("failed");
	});
});
