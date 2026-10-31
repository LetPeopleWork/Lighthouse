import { Box, Typography } from "@mui/material";
import type React from "react";
import LighthouseLogo from "../../../../components/App/LetPeopleWork/LighthouseLogo";
import type { PrintPageLook } from "./ReportPrintContent";

const BRANDING = "Made with Lighthouse by LetPeopleWork · letpeople.work";

/** How tall the credit is on paper, and the gap it keeps to the report above it. */
export const FOOTER_HEIGHT = "16mm";
export const GAP_TO_THE_FOOTER = "4mm";

/**
 * The credit reads the same under every report, so a printed document carries it once, fixed to the page: the browser
 * then prints it at the foot of every sheet. Each report keeps room of this height at the foot of its own sheets, so
 * no panel runs under it. On the screen it simply closes the document.
 */
const CREDIT_FOOTER = {
	display: "flex",
	alignItems: "center",
	gap: 2,
	mt: 2,
	borderTop: 1,
	borderColor: "divider",
	"@media print": {
		position: "fixed",
		left: 0,
		right: 0,
		bottom: 0,
		mt: 0,
		boxSizing: "border-box",
		height: FOOTER_HEIGHT,
		bgcolor: "background.paper",
	},
} as const;

const CreditFooter: React.FC = () => (
	<Box component="footer" sx={CREDIT_FOOTER}>
		<LighthouseLogo linksHome={false} />
		<Typography variant="body2">{BRANDING}</Typography>
	</Box>
);

/** The document with the credit closing it, unless the document could not be made. */
export const withTheCredit = (
	look: PrintPageLook,
	document: React.ReactNode,
): React.ReactNode => (
	<>
		{document}
		{look.state.startsWith("failed") ? null : <CreditFooter />}
	</>
);
