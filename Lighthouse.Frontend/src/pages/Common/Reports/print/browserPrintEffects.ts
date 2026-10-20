import type { PrintEffectPort, PrintLoadOutcome } from "./usePrintOnce";

const loadIntoTheDocument = async (
	faces: readonly string[],
): Promise<PrintLoadOutcome> => {
	try {
		const found = await Promise.all(
			faces.map((face) => document.fonts.load(face)),
		);
		await document.fonts.ready;
		return found.every((matches) => matches.length > 0) ? "loaded" : "failed";
	} catch {
		return "failed";
	}
};

// Decoded, not merely fetched, so the picture is ready to paint the moment the print dialog draws the page.
const loadThePicture = async (source: string): Promise<PrintLoadOutcome> => {
	const picture = new Image();
	picture.src = source;
	try {
		await picture.decode();
		return "loaded";
	} catch {
		return "failed";
	}
};

/**
 * The print page's reach into the browser: its print dialog, the dialog closing, the document's fonts and pictures,
 * and the router's navigation it is handed. The one place that names the browser's own print and fonts, so
 * everything else can be given a stand-in.
 */
export const browserPrintEffects = (
	navigate: PrintEffectPort["navigate"],
): PrintEffectPort => ({
	print: () => globalThis.print(),
	onAfterPrint: (listener) => {
		globalThis.addEventListener("afterprint", listener);
		return () => globalThis.removeEventListener("afterprint", listener);
	},
	navigate,
	loadFonts: loadIntoTheDocument,
	loadPicture: loadThePicture,
});
