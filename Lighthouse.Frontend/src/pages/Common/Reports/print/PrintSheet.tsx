import {
	Box,
	Button,
	GlobalStyles,
	type SxProps,
	type Theme,
	ThemeProvider,
} from "@mui/material";
import type React from "react";
import { Link as RouterLink } from "react-router";
import { lightTheme } from "../../../../utils/theme/appThemes";
import { RETURNED_FROM_PRINT } from "./usePrintOnce";

const SCREEN_ONLY = { "@media print": { display: "none" } };
const BODY: SxProps<Theme> = {
	display: "flex",
	flexDirection: "column",
	minHeight: 240,
};

/**
 * Paper is white whatever the reader's screen is. The app's baseline paints the body from the reader's theme, and a
 * nested light theme does not repaint it, so the page sets its own ground and tells the browser it is a light page.
 */
const LIGHT_GROUND = {
	p: 4,
	minHeight: "100vh",
	"@media print": { minHeight: "auto", p: 0 },
	bgcolor: "background.paper",
	color: "text.primary",
	colorScheme: "light",
};
/**
 * The paper size stays the reader's choice in the print dialog, so A4 and Letter both fit. Colours print as they
 * show, and nothing is caught halfway through an animation when the page is taken.
 */
const WHITE_PAPER = {
	"@page": { margin: "12mm" },
	"@media print": {
		"html, body": { backgroundColor: "#fff", printColorAdjust: "exact" },
		"*, *::before, *::after": { animation: "none", transition: "none" },
	},
};

/** The way back: a step back through the history to where the export started, or else to an address in its place. */
export type WayBack =
	| { readonly label: string; readonly stepBack: () => void }
	| { readonly label: string; readonly to: string };

const BackControl: React.FC<{ readonly back: WayBack }> = ({ back }) =>
	"stepBack" in back ? (
		<Button onClick={back.stepBack} sx={SCREEN_ONLY}>
			{back.label}
		</Button>
	) : (
		<Button
			component={RouterLink}
			to={back.to}
			replace
			state={RETURNED_FROM_PRINT}
			sx={SCREEN_ONLY}
		>
			{back.label}
		</Button>
	);

interface PrintSheetProps {
	readonly ready: boolean;
	/** The way back, on a page the reader sees; the hidden frame an export prints from holds no control. */
	readonly back?: WayBack;
	readonly children: React.ReactNode;
}

/** The sheet the report is printed on: white in any theme, with a way back that stays on the screen and off the paper. */
const PrintSheet: React.FC<PrintSheetProps> = ({ ready, back, children }) => (
	<ThemeProvider theme={lightTheme}>
		<GlobalStyles styles={WHITE_PAPER} />
		<Box data-render-ready={ready ? "true" : "false"} sx={LIGHT_GROUND}>
			{back !== undefined && <BackControl back={back} />}
			<Box sx={BODY}>{children}</Box>
		</Box>
	</ThemeProvider>
);

export default PrintSheet;
