import { Box, Button, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useEffect, useMemo, useState } from "react";
import { Link as RouterLink, useLocation, useParams } from "react-router";
import type { IReportOwner } from "../../../../models/Reports/Report";
import type { useTerminology } from "../../../../services/TerminologyContext";
import { WidgetBody } from "../../MetricsView/WidgetShell";
import type { WidgetStatus } from "../../MetricsView/widgetStatus";
import { reportPathOf, reportQueryKeyOf, reportsPathOf } from "../reportOwner";
import {
	isAFreshAnswer,
	type PrintFontsState,
	type PrintPageState,
	printPageStateOf,
} from "./printPageState";
import type { PrintReadPort } from "./printReadPort";
import {
	type PrintEffectPort,
	type PrintOrigin,
	usePrintOnce,
} from "./usePrintOnce";

const PREPARING = "Preparing the PDF…";
const PART_OF_THE_PAGE_COULD_NOT_BE_LOADED =
	"Part of the page couldn't be loaded, so no PDF was made.";
const BACK_TO_REPORT = "‹ Back to report";
const BODY_MIN_HEIGHT = 240;

/** The weights the printed report is set in: its text and its headings. */
const THE_PRINTED_FACES = ["400 1em Quicksand", "700 1em Quicksand"];

/** What the print page needs of the instance's terms, and nothing that changes them. */
export type PrintTerms = Pick<
	ReturnType<typeof useTerminology>,
	"getTerm" | "isLoading" | "error"
>;

interface ReportPrintPageProps {
	readonly kind: IReportOwner["kind"];
	readonly reads: PrintReadPort;
	readonly terms: PrintTerms;
	readonly effects: PrintEffectPort;
}

interface PrintRequest {
	readonly printWhenReady: boolean;
	readonly from: PrintOrigin | undefined;
}

const isAnOrigin = (from: unknown): from is PrintOrigin =>
	from === "report" || from === "list";

/** What the export left in the history entry; a page opened from its address finds nothing there. */
const printRequestOf = (state: unknown): PrintRequest => {
	const carried = (state ?? {}) as {
		printWhenReady?: unknown;
		from?: unknown;
	};
	return {
		printWhenReady: carried.printWhenReady === true,
		from: isAnOrigin(carried.from) ? carried.from : undefined,
	};
};

const useOwnerName = (owner: IReportOwner, reads: PrintReadPort) =>
	useQuery({
		queryKey: ["reportPrintOwner", owner.kind, owner.id],
		queryFn: async () => {
			const found =
				owner.kind === "team"
					? await reads.getTeam(owner.id)
					: await reads.getPortfolio(owner.id);
			return found?.name ?? null;
		},
	});

interface PrintFonts {
	readonly fonts: PrintFontsState;
	readonly askAgain: () => void;
}

/** Asks for the fonts once the report is drawn, so the browser loads the faces the page actually shows. */
const useFontsOnceDrawn = (
	drawn: boolean,
	loadFonts: PrintEffectPort["loadFonts"],
): PrintFonts => {
	const [fonts, setFonts] = useState<PrintFontsState>("pending");

	useEffect(() => {
		if (!drawn || fonts !== "pending") {
			return;
		}
		let gone = false;
		loadFonts(THE_PRINTED_FACES)
			.catch((): PrintFontsState => "failed")
			.then((outcome) => {
				if (!gone) setFonts(outcome);
			});
		return () => {
			gone = true;
		};
	}, [drawn, fonts, loadFonts]);

	return { fonts, askAgain: () => setFonts("pending") };
};

const widgetStatusOf = (state: PrintPageState): WidgetStatus => {
	if (state === "ready") return "ready";
	return state === "preparing" ? "loading" : "error";
};

const ReportPrintPage: React.FC<ReportPrintPageProps> = ({
	kind,
	reads,
	terms,
	effects,
}) => {
	const params = useParams();
	const location = useLocation();
	const owner: IReportOwner = { kind, id: Number(params.id) };
	const reportId = Number(params.reportId);
	const mountedAt = useMemo(() => Date.now(), []);

	const report = useQuery({
		queryKey: reportQueryKeyOf(owner, reportId),
		queryFn: () => reads.getReport(owner, reportId),
		refetchOnMount: "always",
	});
	const ownerName = useOwnerName(owner, reads);

	const fetchedAt = report.data === undefined ? null : report.dataUpdatedAt;
	const fresh = isAFreshAnswer(fetchedAt, mountedAt) ? report.data : undefined;
	const { fonts, askAgain } = useFontsOnceDrawn(
		fresh !== undefined,
		effects.loadFonts,
	);

	const state = printPageStateOf({
		fetchedAt,
		mountedAt,
		reportError: report.error,
		ownerName: ownerName.data ?? null,
		ownerError: ownerName.error,
		termsLoading: terms.isLoading,
		termsError: terms.error,
		fonts,
	});
	const partFailed = state === "failedPart";
	const { printWhenReady, from } = printRequestOf(location.state);
	const reportPath = reportPathOf(owner, reportId);

	usePrintOnce({
		state,
		printWhenReady,
		from,
		here: location.pathname,
		reportPath,
		reportsPath: reportsPathOf(owner),
		port: effects,
	});

	return (
		<Box data-render-ready={state === "ready" ? "true" : "false"} sx={{ p: 4 }}>
			<Button component={RouterLink} to={reportPath}>
				{BACK_TO_REPORT}
			</Button>
			<Box
				sx={{
					display: "flex",
					flexDirection: "column",
					minHeight: BODY_MIN_HEIGHT,
				}}
			>
				<WidgetBody
					widgetKey="report-print"
					status={widgetStatusOf(state)}
					hasContentToDim={false}
					loadingMessage={PREPARING}
					failureMessage={
						partFailed ? PART_OF_THE_PAGE_COULD_NOT_BE_LOADED : undefined
					}
					onRetry={partFailed ? askAgain : undefined}
				>
					{fresh !== undefined && (
						<Typography variant="h4" component="h1">
							{fresh.name}
						</Typography>
					)}
				</WidgetBody>
			</Box>
		</Box>
	);
};

export default ReportPrintPage;
