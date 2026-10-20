import { useEffect, useRef } from "react";
import type { PrintPageState } from "./printPageState";

export const THE_REPORT_COULD_NOT_BE_LOADED =
	"This report couldn't be loaded, so no PDF was made.";
export const PART_OF_THE_PAGE_COULD_NOT_BE_LOADED =
	"Part of the page couldn't be loaded, so no PDF was made.";

/** Whether everything the browser was asked to load (every font face, or a picture) arrived. */
export type PrintLoadOutcome = "loaded" | "failed";

/** The print dialog, its closing, the document's fonts, pictures and title: the printed report's whole reach into the browser. */
export interface PrintEffectPort {
	readonly print: () => void;
	/** Calls the listener when the print dialog closes; the returned function stops listening. */
	readonly onAfterPrint: (listener: () => void) => () => void;
	/** Loads the named faces, such as '700 1em "Quicksand Variable"', into the document; asked by the page once it has drawn the report. */
	readonly loadFonts: (faces: readonly string[]) => Promise<PrintLoadOutcome>;
	/** Loads the picture at an address, such as the Lighthouse mark, so a print made at once shows it. */
	readonly loadPicture: (source: string) => Promise<PrintLoadOutcome>;
	/** Names the printed document, so the PDF's file name is the report's; the returned function gives the app its own name back. */
	readonly setTitle: (title: string) => () => void;
}

/** What the print-once rule tells the frame's host. */
export type PrintOutcome =
	| { readonly kind: "printing" }
	| { readonly kind: "closed" }
	| {
			readonly kind: "failed";
			readonly message: string;
			readonly retry: boolean;
	  };

/** Something the PDF needed could not be had, and a fresh attempt may get it. */
export const A_PART_COULD_NOT_BE_LOADED: PrintOutcome = {
	kind: "failed",
	message: PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
	retry: true,
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
		tell(A_PART_COULD_NOT_BE_LOADED);
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
			// Open before asking, since some browsers say the dialog closed before print() has returned.
			dialogOpen.current = true;
			if (!askForTheDialog(port, tell)) dialogOpen.current = false;
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
