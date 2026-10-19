import { renderHook } from "@testing-library/react";
import { StrictMode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { PrintPageState } from "./printPageState";
import {
	type PrintEffectPort,
	type PrintOnceInputs,
	type PrintOrigin,
	RETURNED_FROM_PRINT,
	usePrintOnce,
} from "./usePrintOnce";

/**
 * Printing once, driven through the print page's effect port with a fake browser: the dialog is asked for only
 * when the page is ready and was opened by an export, the wish to print is cleared from the page's history entry
 * before the dialog opens, so nothing can print it a second time, and when the dialog closes the reader is taken,
 * without a new history entry, back to where the export started.
 */

const pendingEpic6298 = "Pending: printing once (epic 6298, slice 01)";

const HERE = "/print/teams/1/reports/42";
const THE_REPORT = "/teams/1/reports/42";
const THE_LIST = "/teams/1/reports";

/** The browser as the hook sees it: every call in the order it happened, and a dialog that closes on demand. */
const aFakeBrowser = () => {
	const happened: string[] = [];
	const listeners = new Set<() => void>();
	const port: PrintEffectPort = {
		print: vi.fn(() => {
			happened.push("print");
		}),
		onAfterPrint: vi.fn((listener: () => void) => {
			listeners.add(listener);
			return () => listeners.delete(listener);
		}),
		navigate: vi.fn((to: string) => {
			happened.push(`navigate ${to}`);
		}),
		loadFonts: vi.fn(async () => "loaded" as const),
	};
	return {
		port,
		happened: () => [...happened],
		prints: () => vi.mocked(port.print).mock.calls.length,
		closeTheDialog: () => {
			for (const listener of [...listeners]) listener();
		},
		listening: () => listeners.size,
	};
};

const inputs = (
	port: PrintEffectPort,
	overrides: Partial<PrintOnceInputs> = {},
): PrintOnceInputs => ({
	state: "ready",
	printWhenReady: true,
	from: "report",
	here: HERE,
	reportPath: THE_REPORT,
	reportsPath: THE_LIST,
	port,
	...overrides,
});

describe("printing once", () => {
	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it.skip(`clears the wish to print from the page's entry, keeping where it started, then prints once (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();

		renderHook(() => usePrintOnce(inputs(browser.port)));

		expect(browser.happened()).toEqual([`navigate ${HERE}`, "print"]);
		expect(browser.port.navigate).toHaveBeenCalledWith(HERE, {
			replace: true,
			state: expect.objectContaining({ from: "report" }),
		});
		expect(
			vi.mocked(browser.port.navigate).mock.calls[0][1].state,
		).not.toMatchObject({
			printWhenReady: true,
		});
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it.skip.each<PrintPageState>([
		"preparing",
		"failedRetryable",
		"failedPart",
		"failedFinal",
	])(
		`never prints, nor touches the history, while the page is %s (${pendingEpic6298})`,
		(state) => {
			const browser = aFakeBrowser();

			renderHook(() => usePrintOnce(inputs(browser.port, { state })));

			expect(browser.happened()).toEqual([]);
		},
	);

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// A pasted link, a reload after printing and Forward onto the page all arrive without the wish to print.
	it.skip(`never prints a page that carries no wish to print (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();

		renderHook(() =>
			usePrintOnce(inputs(browser.port, { printWhenReady: false })),
		);

		expect(browser.prints()).toBe(0);
		expect(browser.port.navigate).not.toHaveBeenCalled();
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	it.skip(`prints once under React's strict mode, which runs the effect twice (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();

		renderHook(() => usePrintOnce(inputs(browser.port)), {
			wrapper: StrictMode,
		});

		expect(browser.prints()).toBe(1);
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	it.skip(`prints no more when the page draws again before the cleared entry reaches it (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();
		const { rerender } = renderHook(
			(state: PrintPageState) => usePrintOnce(inputs(browser.port, { state })),
			{ initialProps: "ready" as PrintPageState },
		);

		rerender("preparing");
		rerender("ready");

		expect(browser.prints()).toBe(1);
	});

	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
	// The wish to print is used up only by printing, so a failure followed by a successful Retry still prints.
	it.skip(`prints once when a failed page becomes ready after Retry (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();
		const { rerender } = renderHook(
			(state: PrintPageState) => usePrintOnce(inputs(browser.port, { state })),
			{ initialProps: "preparing" as PrintPageState },
		);

		rerender("failedRetryable");
		expect(browser.prints()).toBe(0);
		rerender("preparing");
		rerender("ready");

		expect(browser.prints()).toBe(1);
	});
});

describe("returning when the dialog closes", () => {
	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it.skip.each<[PrintOrigin | undefined, string]>([
		["report", THE_REPORT],
		["list", THE_LIST],
		[undefined, THE_LIST],
	])(
		`leads an export started from %s back to %s, replacing the print page's entry (${pendingEpic6298})`,
		(from, destination) => {
			const browser = aFakeBrowser();
			renderHook(() => usePrintOnce(inputs(browser.port, { from })));

			browser.closeTheDialog();

			expect(browser.port.navigate).toHaveBeenLastCalledWith(destination, {
				replace: true,
				state: expect.objectContaining(RETURNED_FROM_PRINT),
			});
		},
	);

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	// Clearing the wish re-renders the page; the reader is still taken back when the dialog closes after that.
	it.skip(`still leads back to the report when the dialog closes after the cleared entry reached the page (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();
		const { rerender } = renderHook(
			(printWhenReady: boolean) =>
				usePrintOnce(inputs(browser.port, { printWhenReady })),
			{ initialProps: true },
		);
		expect(browser.prints()).toBe(1);

		rerender(false);
		browser.closeTheDialog();

		expect(browser.port.navigate).toHaveBeenLastCalledWith(THE_REPORT, {
			replace: true,
			state: expect.objectContaining(RETURNED_FROM_PRINT),
		});
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	it.skip(`listens for the dialog once for as long as the page is open, whatever it draws meanwhile (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();
		const { rerender } = renderHook(
			(change: Partial<PrintOnceInputs>) =>
				usePrintOnce(inputs(browser.port, change)),
			{ initialProps: { state: "preparing" } as Partial<PrintOnceInputs> },
		);

		rerender({ state: "ready" });
		rerender({ state: "ready", printWhenReady: false });

		expect(browser.port.onAfterPrint).toHaveBeenCalledTimes(1);
		expect(browser.listening()).toBe(1);
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	// React's strict mode mounts, unmounts and mounts again; one listener must be left, not two and not none.
	it.skip(`is left with one listener for the dialog under React's strict mode (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();

		renderHook(() => usePrintOnce(inputs(browser.port)), {
			wrapper: StrictMode,
		});

		expect(browser.listening()).toBe(1);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// A dialog the reader opened with the browser's own menu, on a page that did not print, leads nowhere.
	it.skip(`stays on the page when a dialog it did not open closes (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();
		renderHook(() =>
			usePrintOnce(inputs(browser.port, { printWhenReady: false })),
		);

		browser.closeTheDialog();

		expect(browser.port.navigate).not.toHaveBeenCalled();
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it.skip(`stops listening for the dialog once the page is gone (${pendingEpic6298})`, () => {
		const browser = aFakeBrowser();
		const { unmount } = renderHook(() => usePrintOnce(inputs(browser.port)));

		unmount();

		expect(browser.listening()).toBe(0);
	});
});
