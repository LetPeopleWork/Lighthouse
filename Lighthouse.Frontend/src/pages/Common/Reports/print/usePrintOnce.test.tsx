import { renderHook } from "@testing-library/react";
import { StrictMode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { WindowPrintEffects } from "./browserPrintEffects";
import type { PrintPageState } from "./printPageState";
import {
	type PrintFailureLook,
	type PrintOnceInputs,
	type PrintOutcome,
	usePrintOnce,
} from "./usePrintOnce";

/**
 * Printing once, from the hidden frame an export draws the report into, driven through the frame's effect port with
 * a fake browser. The dialog is asked for once per frame, and only when the report drawn for paper is ready; the
 * frame's host is told when the dialog is being asked for, when the frame's dialog closed, and when the page failed,
 * with the sentence to show and whether Retry may mend it. Nothing here moves the reader anywhere.
 */

const THE_REPORT_COULD_NOT_BE_LOADED =
	"This report couldn't be loaded, so no PDF was made.";
const PART_OF_THE_PAGE_COULD_NOT_BE_LOADED =
	"Part of the page couldn't be loaded, so no PDF was made.";
const NO_LONGER_EXISTS = "This report no longer exists.";

/** The browser as the hook sees it: every call and every word to the host, in order, and a dialog closing on demand. */
const aFakeFrame = (print: () => void = () => undefined) => {
	const happened: string[] = [];
	const listeners = new Set<() => void>();
	const outcomes: PrintOutcome[] = [];
	const port: WindowPrintEffects = {
		print: vi.fn(() => {
			happened.push("print");
			print();
		}),
		onAfterPrint: vi.fn((listener: () => void) => {
			listeners.add(listener);
			return () => listeners.delete(listener);
		}),
		loadFonts: vi.fn(async () => "loaded" as const),
		loadPicture: vi.fn(async () => "loaded" as const),
		setTitle: vi.fn(() => () => undefined),
	};
	return {
		port,
		tell: (outcome: PrintOutcome) => {
			happened.push(outcome.kind);
			outcomes.push(outcome);
		},
		happened: () => [...happened],
		outcomes: () => [...outcomes],
		prints: () => vi.mocked(port.print).mock.calls.length,
		closeTheDialog: () => {
			for (const listener of [...listeners]) listener();
		},
		listening: () => listeners.size,
	};
};

const NO_FAILURE: PrintFailureLook = { retry: false };

const inputs = (
	frame: ReturnType<typeof aFakeFrame>,
	overrides: Partial<PrintOnceInputs> = {},
): PrintOnceInputs => ({
	state: "ready",
	failure: NO_FAILURE,
	port: frame.port,
	tell: frame.tell,
	...overrides,
});

describe("printing once in the frame", () => {
	// @us-01 @rev-3 @driving_port @contract-shape:bounded-change
	// The control stops waiting when the dialog is asked for, since in Chrome print() returns only once it closes.
	it(`tells the host it is printing, then asks the frame's window for the dialog once, when the page is ready`, () => {
		const frame = aFakeFrame();

		renderHook(() => usePrintOnce(inputs(frame)));

		expect(frame.happened()).toEqual(["printing", "print"]);
	});

	// @us-01 @rev-3 @error @contract-shape:unbounded-preservation
	it.each<PrintPageState>([
		"preparing",
		"failedRetryable",
		"failedPart",
		"failedFinal",
	])(`never prints while the page is %s`, (state) => {
		const frame = aFakeFrame();

		renderHook(() => usePrintOnce(inputs(frame, { state })));

		expect(frame.prints()).toBe(0);
	});

	// @us-01 @rev-3 @edge @contract-shape:bounded-change
	// React's strict mode runs every effect twice on mount; one frame still asks for one dialog.
	it(`prints once under React's strict mode, and no more when the page draws again`, () => {
		const frame = aFakeFrame();
		const { rerender } = renderHook(
			(state: PrintPageState) => usePrintOnce(inputs(frame, { state })),
			{ initialProps: "ready" as PrintPageState, wrapper: StrictMode },
		);

		rerender("preparing");
		rerender("ready");

		expect(frame.prints()).toBe(1);
	});

	// @us-01 @rev-3 @error @contract-shape:bounded-change
	it.each<[PrintPageState, PrintFailureLook, PrintOutcome]>([
		[
			"failedRetryable",
			{ message: THE_REPORT_COULD_NOT_BE_LOADED, retry: true },
			{ kind: "failed", message: THE_REPORT_COULD_NOT_BE_LOADED, retry: true },
		],
		[
			"failedPart",
			{ message: PART_OF_THE_PAGE_COULD_NOT_BE_LOADED, retry: true },
			{
				kind: "failed",
				message: PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
				retry: true,
			},
		],
		[
			"failedFinal",
			{ message: NO_LONGER_EXISTS, retry: false },
			{ kind: "failed", message: NO_LONGER_EXISTS, retry: false },
		],
	])(
		`tells the host the page is %s, with its sentence and whether Retry is offered, and prints nothing`,
		(state, failure, told) => {
			const frame = aFakeFrame();

			renderHook(() => usePrintOnce(inputs(frame, { state, failure })));

			expect(frame.outcomes()).toEqual([told]);
			expect(frame.prints()).toBe(0);
		},
	);

	// @us-01 @rev-3 @error @contract-shape:bounded-change
	// A print dialog that cannot be opened made no PDF, and asking again in a fresh frame may help.
	it(`tells the host part of the page couldn't be loaded, with Retry, when the frame's print dialog cannot be opened`, () => {
		const frame = aFakeFrame(() => {
			throw new Error("the print dialog could not be opened");
		});

		renderHook(() => usePrintOnce(inputs(frame)));

		expect(frame.outcomes()).toEqual([
			{ kind: "printing" },
			{
				kind: "failed",
				message: PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
				retry: true,
			},
		]);
	});
});

describe("hearing the frame's dialog close", () => {
	// @us-01 @rev-3 @driving_port @contract-shape:bounded-change
	it(`tells the host once that the dialog closed, after it printed`, () => {
		const frame = aFakeFrame();
		renderHook(() => usePrintOnce(inputs(frame)));

		frame.closeTheDialog();
		frame.closeTheDialog();

		expect(frame.happened()).toEqual(["printing", "print", "closed"]);
	});

	// @us-01 @rev-3 @error @contract-shape:unbounded-preservation
	// A dialog the frame did not open is none of the host's business, and a frame gone hears nothing.
	it(`listens once for as long as it is mounted, tells nothing of a dialog it did not open, and stops listening once gone`, () => {
		const frame = aFakeFrame();
		const { rerender, unmount } = renderHook(
			(state: PrintPageState) => usePrintOnce(inputs(frame, { state })),
			{ initialProps: "preparing" as PrintPageState, wrapper: StrictMode },
		);
		rerender("preparing");
		expect(frame.listening()).toBe(1);

		frame.closeTheDialog();
		expect(frame.outcomes()).toEqual([]);

		unmount();
		expect(frame.listening()).toBe(0);
	});
});
