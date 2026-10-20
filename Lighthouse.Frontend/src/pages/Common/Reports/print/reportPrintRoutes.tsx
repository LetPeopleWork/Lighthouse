import { Box, CircularProgress } from "@mui/material";
import type React from "react";
import { lazy, Suspense, useContext, useMemo } from "react";
import { Route, useNavigate } from "react-router";
import type { IReportOwner } from "../../../../models/Reports/Report";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../services/TerminologyContext";
import { browserPrintEffects } from "./browserPrintEffects";
import type { PrintReadPort } from "./printReadPort";
import type { PrintEffectPort } from "./usePrintOnce";

const ReportPrintPage = lazy(() => import("./ReportPrintPage"));

/**
 * What a test may stand in for at the print page's edge. The app passes nothing, and the page loads its fonts
 * through the browser; jsdom has no fonts of its own to load.
 */
export interface PrintPageStandIns {
	readonly loadFonts?: PrintEffectPort["loadFonts"];
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

// The page is handed new objects holding only the reads it needs, because a type that hides the writes is erased
// at runtime and would still hand over every service method.
const PrintRoute: React.FC<PrintRouteProps> = ({ kind, standIns }) => {
	const { reportService, teamService, portfolioService } =
		useContext(ApiServiceContext);
	const { getTerm, isLoading, error } = useTerminology();
	const navigate = useNavigate();
	const { loadFonts } = standIns;

	const reads: PrintReadPort = useMemo(
		() => ({
			getReport: reportService.getReport.bind(reportService),
			getTeam: teamService.getTeam.bind(teamService),
			getPortfolio: portfolioService.getPortfolio.bind(portfolioService),
		}),
		[reportService, teamService, portfolioService],
	);
	const terms = useMemo(
		() => ({ getTerm, isLoading, error }),
		[getTerm, isLoading, error],
	);
	const effects: PrintEffectPort = useMemo(() => {
		const browser = browserPrintEffects((to, options) => {
			navigate(to, options);
		});
		return loadFonts === undefined ? browser : { ...browser, loadFonts };
	}, [navigate, loadFonts]);

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
