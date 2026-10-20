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
	bgcolor: "background.paper",
	color: "text.primary",
	colorScheme: "light",
};
const WHITE_PAPER = {
	"@media print": { "html, body": { backgroundColor: "#fff" } },
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
	readonly back: WayBack;
	readonly children: React.ReactNode;
}

/** The sheet the report is printed on: white in any theme, with a way back that stays on the screen and off the paper. */
const PrintSheet: React.FC<PrintSheetProps> = ({ ready, back, children }) => (
	<ThemeProvider theme={lightTheme}>
		<GlobalStyles styles={WHITE_PAPER} />
		<Box data-render-ready={ready ? "true" : "false"} sx={LIGHT_GROUND}>
			<BackControl back={back} />
			<Box sx={BODY}>{children}</Box>
		</Box>
	</ThemeProvider>
);

export default PrintSheet;
