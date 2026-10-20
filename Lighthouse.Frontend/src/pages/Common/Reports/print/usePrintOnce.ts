import { useEffect, useRef } from "react";
import type { PrintPageState } from "./printPageState";

/** Where an export started, so the way back and the return after printing lead there. */
export type PrintOrigin = "report" | "list";

/** What a return from the print page to the Reports list carries in the list's history entry. */
export const RETURNED_FROM_PRINT = { returnedFromPrint: true } as const;

/** Whether every font face the page asked for was found and loaded. */
export type PrintFontsOutcome = "loaded" | "failed";

/** The print dialog, its closing, the router and the document's fonts: the print page's whole reach into the browser. */
export interface PrintEffectPort {
	readonly print: () => void;
	/** Calls the listener when the print dialog closes; the returned function stops listening. */
	readonly onAfterPrint: (listener: () => void) => () => void;
	/** Leads to an address, or a number of steps through the history, as -1 steps back to the entry before. */
	navigate(
		to: string | number,
		options: { readonly replace: boolean; readonly state?: unknown },
	): void;
	/** Loads the named faces, such as "700 1em Quicksand", into the document; asked by the page once it has drawn the report. */
	readonly loadFonts: (faces: readonly string[]) => Promise<PrintFontsOutcome>;
}

/** Steps back to the history entry before the print page, which is where an export started. */
export const stepBackFromThePrintPage = (port: PrintEffectPort): void =>
	port.navigate(-1, { replace: false });

export interface PrintOnceInputs {
	readonly state: PrintPageState;
	/** The one-shot wish to print the export left in the current history entry. */
	readonly printWhenReady: boolean;
	readonly from: PrintOrigin | undefined;
	/** The print page's own address, whose history entry is replaced without the wish to print. */
	readonly here: string;
	readonly reportPath: string;
	readonly reportsPath: string;
	readonly port: PrintEffectPort;
}

/**
 * Prints once when the page is ready and was opened by an export. The wish to print is used up only by printing,
 * so a page that is still preparing or failed keeps it, and prints once when it becomes ready after all. Once the
 * dialog it opened closes, it steps back to the entry the export started from, so that entry is in the history once;
 * a page that knows no origin has no such entry, and the Reports list takes its place.
 */
export const usePrintOnce = ({
	state,
	printWhenReady,
	from,
	here,
	reportsPath,
	port,
}: PrintOnceInputs): void => {
	// Set by printing, so React running the effect again before the cleared entry reaches the page cannot open the
	// dialog twice.
	const printed = useRef(false);
	// Set only by printing, so a dialog the reader opened from the browser's own menu leads nowhere; used up by
	// leaving, so a dialog heard closing twice cannot step back past the entry the export started from.
	const leave = useRef<(() => void) | undefined>(undefined);

	useEffect(() => {
		if (state !== "ready" || !printWhenReady || printed.current) {
			return;
		}
		printed.current = true;
		leave.current =
			from === undefined
				? () =>
						port.navigate(reportsPath, {
							replace: true,
							state: RETURNED_FROM_PRINT,
						})
				: () => stepBackFromThePrintPage(port);
		// The wish to print is used up before the dialog opens, so a reload or Forward onto this entry never prints.
		port.navigate(here, { replace: true, state: { from } });
		port.print();
	}, [state, printWhenReady, from, here, reportsPath, port]);

	// Subscribed once for the page's lifetime: clearing the wish re-renders the page before the dialog closes.
	useEffect(
		() =>
			port.onAfterPrint(() => {
				const leaving = leave.current;
				leave.current = undefined;
				leaving?.();
			}),
		[port],
	);
};
