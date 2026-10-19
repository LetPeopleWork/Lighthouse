import type { PrintEffectPort } from "./usePrintOnce";

export const __SCAFFOLD__ = true;

/**
 * The print page's reach into the browser: its print dialog, the dialog closing, the document's fonts, and the
 * router's navigation it is handed. The one place that names the browser's own print and fonts, so everything else
 * can be given a stand-in.
 */
export const browserPrintEffects = (
	_navigate: PrintEffectPort["navigate"],
): PrintEffectPort => {
	throw new Error("SCAFFOLD: the browser's print effects are not bound yet");
};
