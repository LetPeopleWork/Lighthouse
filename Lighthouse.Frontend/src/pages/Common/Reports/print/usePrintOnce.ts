import { useEffect, useRef } from "react";
import type { PrintPageState } from "./printPageState";

export const THE_REPORT_COULD_NOT_BE_LOADED =
	"This report couldn't be loaded, so no PDF was made.";
export const PART_OF_THE_PAGE_COULD_NOT_BE_LOADED =
	"Part of the page couldn't be loaded, so no PDF was made.";

/** Where an export started, so the way back and the return after printing lead there. */
export type PrintOrigin = "report" | "list";

/** What a return from the print page to the Reports list carries in the list's history entry. */
export const RETURNED_FROM_PRINT = { returnedFromPrint: true } as const;

/** Whether everything the browser was asked to load (every font face, or a picture) arrived. */
export type PrintLoadOutcome = "loaded" | "failed";

/** The print dialog, its closing, the router, the document's fonts, pictures and title: the print page's whole reach into the browser. */
export interface PrintEffectPort {
	readonly print: () => void;
	/** Calls the listener when the print dialog closes; the returned function stops listening. */
	readonly onAfterPrint: (listener: () => void) => () => void;
	/** Leads to an address, or a number of steps through the history, as -1 steps back to the entry before. */
	navigate(
		to: string | number,
		options: { readonly replace: boolean; readonly state?: unknown },
	): void;
	/** Loads the named faces, such as '700 1em "Quicksand Variable"', into the document; asked by the page once it has drawn the report. */
	readonly loadFonts: (faces: readonly string[]) => Promise<PrintLoadOutcome>;
	/** Loads the picture at an address, such as the Lighthouse mark, so a print made at once shows it. */
	readonly loadPicture: (source: string) => Promise<PrintLoadOutcome>;
	/** Names the printed document, so the PDF's file name is the report's; the returned function gives the app its own name back. */
	readonly setTitle: (title: string) => () => void;
}

/** Steps back to the history entry before the print page, which is where an export started. */
export const stepBackFromThePrintPage = (port: PrintEffectPort): void =>
	port.navigate(-1, { replace: false });

/** What the print-once rule tells the frame's host. */
export type PrintOutcome =
	| { readonly kind: "printing" }
	| { readonly kind: "closed" }
	| {
			readonly kind: "failed";
			readonly message: string;
			readonly retry: boolean;
	  };

/** What a failed page says, and whether Retry is offered. */
export interface PrintFailureLook {
	readonly message?: string;
	readonly retry: boolean;
}

/** The frame window's print dialog and its closing: all the print-once rule asks of the browser. */
export type PrintDialogPort = Pick<PrintEffectPort, "print" | "onAfterPrint">;

export interface PrintOnceInputs {
	readonly state: PrintPageState;
	readonly failure: PrintFailureLook;
	readonly port: PrintDialogPort;
	readonly tell: (outcome: PrintOutcome) => void;
}

/**
 * Tells the host before asking for the dialog, because some browsers return from print() only once the dialog closes.
 * A dialog that cannot be opened made no PDF, and a fresh attempt may open it. Answers whether the dialog opened.
 */
const askForTheDialog = (
	port: PrintDialogPort,
	tell: PrintOnceInputs["tell"],
): boolean => {
	tell({ kind: "printing" });
	try {
		port.print();
		return true;
	} catch {
		tell({
			kind: "failed",
			message: PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
			retry: true,
		});
		return false;
	}
};

/**
 * Prints the frame once, as soon as the page drawn into it is ready, or tells its host why it cannot. Either happens
 * at most once for as long as the frame is mounted, and the host hears of the dialog closing only for the dialog
 * this frame opened.
 */
export const usePrintOnce = ({
	state,
	failure,
	port,
	tell,
}: PrintOnceInputs): void => {
	// Set once the page printed or failed, so React running the effect again, or the page drawing again, adds nothing.
	const settled = useRef(false);
	// Set only by a dialog this frame opened, and used up when it closes, so each such closing is told once.
	const dialogOpen = useRef(false);
	// The listener is subscribed once per mount, so it reaches the host through whatever it was last handed.
	const latestTell = useRef(tell);

	useEffect(() => {
		latestTell.current = tell;
	}, [tell]);

	useEffect(() => {
		if (settled.current || state === "preparing") {
			return;
		}
		settled.current = true;
		if (state === "ready") {
			dialogOpen.current = askForTheDialog(port, tell);
			return;
		}
		tell({
			kind: "failed",
			message: failure.message ?? THE_REPORT_COULD_NOT_BE_LOADED,
			retry: failure.retry,
		});
	}, [state, failure.message, failure.retry, port, tell]);

	useEffect(
		() =>
			port.onAfterPrint(() => {
				if (!dialogOpen.current) return;
				dialogOpen.current = false;
				latestTell.current({ kind: "closed" });
			}),
		[port],
	);
};

export interface PrintOnceAfterNavigatingInputs {
	readonly state: PrintPageState;
	/** The one-shot wish to print the export left in the current history entry. */
	readonly printWhenReady: boolean;
	readonly from: PrintOrigin | undefined;
	/** The print page's own address, whose history entry is replaced without the wish to print. */
	readonly here: string;
	readonly reportsPath: string;
	readonly port: PrintEffectPort;
}

/**
 * Prints once when the print page is ready and was opened by an export. The wish to print is used up only by
 * printing, so a page that is still preparing or failed keeps it, and prints once when it becomes ready after all.
 * Once the dialog it opened closes, it steps back to the entry the export started from, so that entry is in the
 * history once; a page that knows no origin has no such entry, and the Reports list takes its place.
 */
export const usePrintOnceAfterNavigating = ({
	state,
	printWhenReady,
	from,
	here,
	reportsPath,
	port,
}: PrintOnceAfterNavigatingInputs): void => {
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
