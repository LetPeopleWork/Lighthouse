import { useEffect, useRef } from "react";
import type { PrintPageState } from "./printPageState";

/** Where an export started, so the way back and the return after printing lead there. */
export type PrintOrigin = "report" | "list";

/**
 * What a return from the print page carries in its history entry, so the report view it lands on knows the reader
 * is coming back from printing rather than opening the report again.
 */
export const RETURNED_FROM_PRINT = { returnedFromPrint: true } as const;

/** Whether every font face the page asked for was found and loaded. */
export type PrintFontsOutcome = "loaded" | "failed";

/** The print dialog, its closing, the router and the document's fonts: the print page's whole reach into the browser. */
export interface PrintEffectPort {
	readonly print: () => void;
	/** Calls the listener when the print dialog closes; the returned function stops listening. */
	readonly onAfterPrint: (listener: () => void) => () => void;
	readonly navigate: (
		to: string,
		options: { readonly replace: boolean; readonly state?: unknown },
	) => void;
	/** Loads the named faces, such as "700 1em Quicksand", into the document; asked by the page once it has drawn the report. */
	readonly loadFonts: (faces: readonly string[]) => Promise<PrintFontsOutcome>;
}

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
 * so a page that is still preparing or failed keeps it, and prints once when it becomes ready after all.
 */
export const usePrintOnce = ({
	state,
	printWhenReady,
	from,
	here,
	port,
}: PrintOnceInputs): void => {
	// React may run the effect again before the cleared entry reaches the page; the dialog must still open once.
	const printed = useRef(false);

	useEffect(() => {
		if (state !== "ready" || !printWhenReady || printed.current) {
			return;
		}
		printed.current = true;
		// The wish to print is used up before the dialog opens, so a reload or Forward onto this entry never prints.
		port.navigate(here, { replace: true, state: { from } });
		port.print();
	}, [state, printWhenReady, from, here, port]);
};
