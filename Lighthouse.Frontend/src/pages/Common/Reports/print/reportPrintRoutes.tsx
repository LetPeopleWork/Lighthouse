import { Box, CircularProgress } from "@mui/material";
import type React from "react";
import { lazy, Suspense, useContext, useMemo } from "react";
import { Route, useNavigate } from "react-router";
import type { IReportOwner } from "../../../../models/Reports/Report";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../services/TerminologyContext";
import { browserPrintEffects } from "./browserPrintEffects";
import type { PrintReadPort } from "./printReadPort";
import type { PrintTerms } from "./ReportPrintContent";
import type { PrintEffectPort } from "./usePrintOnce";

const loadReportPrintPage = () => import("./ReportPrintPage");
const ReportPrintPage = lazy(loadReportPrintPage);

/**
 * Starts fetching the print page's code before anyone asks for a PDF, so the print dialog is not kept waiting on
 * a download. Nothing waits on this fetch, so a failure is left to the page itself when it is opened.
 */
export const prefetchReportPrintPage = (): void => {
	loadReportPrintPage().catch(() => undefined);
};

/**
 * What a test may stand in for at the print page's edge. The app passes nothing, and the page loads its fonts and
 * pictures through the browser; jsdom has no fonts of its own to load and never loads a picture.
 */
export interface PrintPageStandIns {
	readonly loadFonts?: PrintEffectPort["loadFonts"];
	readonly loadPicture?: PrintEffectPort["loadPicture"];
}

interface PrintRouteProps {
	readonly kind: IReportOwner["kind"];
	readonly standIns: PrintPageStandIns;
}

const WhileThePageLoads: React.FC = () => (
	<Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
		<CircularProgress />
	</Box>
);

export interface PrintPagePorts {
	readonly reads: PrintReadPort;
	readonly terms: PrintTerms;
}

/**
 * What the printed report may ask of the app, for the print page and the export's hidden frame alike. They are new
 * objects holding only the reads needed, because a type that hides the writes is erased at runtime and would still
 * hand over every service method.
 */
export const usePrintPagePorts = (): PrintPagePorts => {
	const { reportService, teamService, portfolioService } =
		useContext(ApiServiceContext);
	const { getTerm, isLoading, error, refetchTerminology } = useTerminology();

	const reads: PrintReadPort = useMemo(
		() => ({
			getReport: (owner, reportId) => reportService.getReport(owner, reportId),
			getTeam: (id) => teamService.getTeam(id),
			getPortfolio: (id) => portfolioService.getPortfolio(id),
		}),
		[reportService, teamService, portfolioService],
	);
	const terms = useMemo(
		() => ({ getTerm, isLoading, error, refetch: refetchTerminology }),
		[getTerm, isLoading, error, refetchTerminology],
	);
	return useMemo(() => ({ reads, terms }), [reads, terms]);
};

const PrintRoute: React.FC<PrintRouteProps> = ({ kind, standIns }) => {
	const { reads, terms } = usePrintPagePorts();
	const navigate = useNavigate();
	const { loadFonts, loadPicture } = standIns;

	const effects: PrintEffectPort = useMemo(() => {
		const browser = browserPrintEffects(globalThis.window);
		return {
			...browser,
			navigate: (to, options) => {
				if (typeof to === "number") navigate(to);
				else navigate(to, options);
			},
			loadFonts: loadFonts ?? browser.loadFonts,
			loadPicture: loadPicture ?? browser.loadPicture,
		};
	}, [navigate, loadFonts, loadPicture]);

	return (
		<Suspense fallback={<WhileThePageLoads />}>
			<ReportPrintPage
				kind={kind}
				reads={reads}
				terms={terms}
				effects={effects}
			/>
		</Suspense>
	);
};

/**
 * The print pages of a Team's and a Portfolio's reports, `/print/teams/:id/reports/:reportId` and its Portfolio
 * twin. The app mounts them beside its shell rather than inside it, so a printed report carries none of the
 * navigation, footer or prompts around every other page.
 */
export const reportPrintRoutes = (
	standIns: PrintPageStandIns = {},
): React.ReactElement => (
	<>
		<Route
			path="/print/teams/:id/reports/:reportId"
			element={<PrintRoute kind="team" standIns={standIns} />}
		/>
		<Route
			path="/print/portfolios/:id/reports/:reportId"
			element={<PrintRoute kind="portfolio" standIns={standIns} />}
		/>
	</>
);
