import type React from "react";
import type { PrintEffectPort } from "./usePrintOnce";

export const __SCAFFOLD__ = true;

/**
 * What a test may stand in for at the print page's edge. The app passes nothing, and the page loads its fonts
 * through the browser; jsdom has no fonts of its own to load.
 */
export interface PrintPageStandIns {
	readonly loadFonts?: PrintEffectPort["loadFonts"];
}

/**
 * The print pages of a Team's and a Portfolio's reports, `/print/teams/:id/reports/:reportId` and its Portfolio
 * twin. The app mounts them beside its shell rather than inside it, so a printed report carries none of the
 * navigation, footer or prompts around every other page.
 */
export const reportPrintRoutes = (
	_standIns: PrintPageStandIns = {},
): React.ReactElement => {
	throw new Error("SCAFFOLD: the report print pages are not built yet");
};
