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

const BACK_TO_REPORTS = "‹ Back to Reports";

interface PrintSheetProps {
	readonly ready: boolean;
	/** The owner's Reports list, on a page the reader sees; the hidden frame an export prints from holds no way back. */
	readonly reportsPath?: string;
	readonly children: React.ReactNode;
}

/** The sheet the report is printed on: white in any theme, with a way back that stays on the screen and off the paper. */
const PrintSheet: React.FC<PrintSheetProps> = ({
	ready,
	reportsPath,
	children,
}) => (
	<ThemeProvider theme={lightTheme}>
		<GlobalStyles styles={WHITE_PAPER} />
		<Box data-render-ready={ready ? "true" : "false"} sx={LIGHT_GROUND}>
			{reportsPath === undefined ? null : (
				<Button component={RouterLink} to={reportsPath} sx={SCREEN_ONLY}>
					{BACK_TO_REPORTS}
				</Button>
			)}
			<Box sx={BODY}>{children}</Box>
		</Box>
	</ThemeProvider>
);

export default PrintSheet;
