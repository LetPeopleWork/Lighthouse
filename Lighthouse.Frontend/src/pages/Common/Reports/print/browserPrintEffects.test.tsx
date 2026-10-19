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
 * a return replaces the print page's entry in the history, and the fonts are loaded into the document and
 * answered as loaded only when every face was found.
 */

const pendingEpic6298 =
	"Pending: the browser's print effects (epic 6298, slice 01)";

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
	Reflect.deleteProperty(document, "fonts");
});

describe("the browser's print effects", () => {
	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it.skip(`asks the browser for its print dialog once per print (${pendingEpic6298})`, () => {
		const print = theBrowsersPrint();

		browserPrintEffects(vi.fn()).print();

		expect(print).toHaveBeenCalledTimes(1);
	});

	// @us-01 @slice-01 @contract-shape:bounded-change
	it.skip(`hears the dialog close until it is told to stop listening (${pendingEpic6298})`, () => {
		const listener = vi.fn();
		const stopListening = browserPrintEffects(vi.fn()).onAfterPrint(listener);

		theDialogCloses();
		stopListening();
		theDialogCloses();

		expect(listener).toHaveBeenCalledTimes(1);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it.skip(`replaces the print page's entry when it leads the reader back (${pendingEpic6298})`, () => {
		const router = onThePrintPage();
		const port = browserPrintEffects((to, options) =>
			router.result.current.navigate(to, options),
		);

		act(() => {
			port.navigate(THE_LIST, { replace: true, state: RETURNED_FROM_PRINT });
		});

		expect(router.result.current.location.pathname).toBe(THE_LIST);
		expect(router.result.current.location.state).toEqual(RETURNED_FROM_PRINT);
		expect(router.result.current.navigationType).toBe("REPLACE");
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// Wired to the print-once rule: the page hears the dialog close while it is open, and not once it is gone.
	it.skip(`leads back after the dialog closes while the page is open, and hears nothing once it is gone (${pendingEpic6298})`, () => {
		theBrowsersPrint();
		const navigate = vi.fn();
		const { unmount } = renderHook(() =>
			usePrintOnce({
				state: "ready",
				printWhenReady: true,
				from: "report",
				here: HERE,
				reportPath: THE_REPORT,
				reportsPath: THE_LIST,
				port: browserPrintEffects(navigate),
			}),
		);

		theDialogCloses();
		const returnsWhileOpen = navigate.mock.calls.filter(
			([to]) => to === THE_REPORT,
		).length;
		unmount();
		theDialogCloses();

		expect(returnsWhileOpen).toBe(1);
		expect(
			navigate.mock.calls.filter(([to]) => to === THE_REPORT),
		).toHaveLength(1);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it.skip(`loads every face it is asked for and answers loaded once the document's fonts are ready (${pendingEpic6298})`, async () => {
		const ready = deferred<void>();
		const load = vi.fn(async (_face: string) => [A_FACE]);
		theDocumentsFontsAre(load, ready.promise);
		let answer: string | undefined;

		const asked = browserPrintEffects(vi.fn())
			.loadFonts(FACES)
			.then((outcome) => {
				answer = outcome;
			});
		await Promise.resolve();
		await Promise.resolve();
		expect(answer).toBeUndefined();
		ready.resolve();
		await asked;

		expect(load.mock.calls.map(([face]) => face)).toEqual(FACES);
		expect(answer).toBe("loaded");
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it.skip.each<[string, (face: string) => Promise<FontFace[]>]>([
		[
			"refused",
			async () => {
				throw new Error("the font could not be fetched");
			},
		],
		["finds no face", async () => []],
	])(
		`answers failed when a face's load is %s (${pendingEpic6298})`,
		async (_how, load) => {
			theDocumentsFontsAre(load);

			await expect(browserPrintEffects(vi.fn()).loadFonts(FACES)).resolves.toBe(
				"failed",
			);
		},
	);
});
