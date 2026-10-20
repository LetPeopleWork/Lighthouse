import type { PrintOrigin } from "./usePrintOnce";

/** What an export leaves in the print page's history entry: the one-shot wish to print, and where it started. */
export interface PrintRequest {
	readonly printWhenReady: boolean;
	readonly from: PrintOrigin | undefined;
}

/** The request an export started from the report view or the list carries; never part of the address, so a pasted link never prints. */
export const printRequestFor = (from: PrintOrigin): PrintRequest => ({
	printWhenReady: true,
	from,
});

const isAnOrigin = (from: unknown): from is PrintOrigin =>
	from === "report" || from === "list";

/** What the export left in the history entry; a page opened from its address finds nothing there. */
export const printRequestOf = (state: unknown): PrintRequest => {
	const carried = (state ?? {}) as {
		printWhenReady?: unknown;
		from?: unknown;
	};
	return {
		printWhenReady: carried.printWhenReady === true,
		from: isAnOrigin(carried.from) ? carried.from : undefined,
	};
};
