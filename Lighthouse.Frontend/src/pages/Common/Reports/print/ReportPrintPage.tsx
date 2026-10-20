import { Box, Button } from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useEffect, useMemo, useState } from "react";
import { Link as RouterLink, useLocation, useParams } from "react-router";
import type { IReport, IReportOwner } from "../../../../models/Reports/Report";
import type { useTerminology } from "../../../../services/TerminologyContext";
import { WidgetBody } from "../../MetricsView/WidgetShell";
import type { WidgetStatus } from "../../MetricsView/widgetStatus";
import { reportPathOf, reportQueryKeyOf, reportsPathOf } from "../reportOwner";
import {
	finalFailureOf,
	retriesUnlessFinal,
} from "../wizard/reportRefusalCopy";
import {
	isAFreshAnswer,
	type PrintFontsState,
	type PrintPageState,
	printPageStateOf,
} from "./printPageState";
import type { PrintReadPort } from "./printReadPort";
import ReportPrintDocument, {
	ownerLineOf,
	printTitleOf,
} from "./ReportPrintDocument";
import {
	type PrintEffectPort,
	type PrintOrigin,
	RETURNED_FROM_PRINT,
	usePrintOnce,
} from "./usePrintOnce";

const PREPARING = "Preparing the PDF…";
const THE_REPORT_COULD_NOT_BE_LOADED =
	"This report couldn't be loaded, so no PDF was made.";
const PART_OF_THE_PAGE_COULD_NOT_BE_LOADED =
	"Part of the page couldn't be loaded, so no PDF was made.";
const BACK_TO_REPORT = "‹ Back to report";
const BACK_TO_REPORTS = "‹ Back to Reports";
const SCREEN_ONLY = { "@media print": { display: "none" } };
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

interface Printable {
	readonly report: IReport;
	readonly ownerLine: string;
}

/** The report can go on paper once it has been read afresh and its owner's name is known. */
const printableOf = (
	report: IReport | undefined,
	owner: IReportOwner,
	ownerName: string | null | undefined,
	getTerm: PrintTerms["getTerm"],
): Printable | undefined => {
	if (report === undefined || typeof ownerName !== "string") return undefined;
	return { report, ownerLine: ownerLineOf(owner, ownerName, getTerm) };
};

/** Names the page while it is open, and gives the tab its old name back when it closes. */
const useDocumentTitle = (title: string | undefined) => {
	useEffect(() => {
		if (title === undefined) return;
		const previous = document.title;
		document.title = title;
		return () => {
			document.title = previous;
		};
	}, [title]);
};

interface WayBack {
	readonly to: string;
	readonly label: string;
}

/**
 * Only a reader who came from the report goes back to it, and only while it can be shown; from the list, a pasted
 * link, or a report that is gone or made by a newer version, the Reports list is where they belong.
 */
const wayBackOf = (
	state: PrintPageState,
	from: PrintOrigin | undefined,
	reportPath: string,
	reportsPath: string,
): WayBack =>
	from === "report" && state !== "failedFinal"
		? { to: reportPath, label: BACK_TO_REPORT }
		: { to: reportsPath, label: BACK_TO_REPORTS };

const widgetStatusOf = (state: PrintPageState): WidgetStatus => {
	if (state === "ready") return "ready";
	return state === "preparing" ? "loading" : "error";
};

/** Reads again what could not be read; a failed read shows on the page through the query, not the returned promise. */
const askAgainFor = (query: { readonly refetch: () => Promise<unknown> }) => {
	query.refetch().catch(() => undefined);
};

interface FailureLook {
	readonly message?: string;
	readonly retry?: () => void;
}

interface AsksAgain {
	readonly forTheReport: () => void;
	readonly forTheFailedPart: () => void;
}

/** What a failed page says, and whether Retry is offered: never for a report that asking again will not change. */
const failureLookOf = (
	state: PrintPageState,
	reportError: unknown,
	askAgain: AsksAgain,
): FailureLook => {
	switch (state) {
		case "failedRetryable":
			return {
				message: THE_REPORT_COULD_NOT_BE_LOADED,
				retry: askAgain.forTheReport,
			};
		case "failedPart":
			return {
				message: PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
				retry: askAgain.forTheFailedPart,
			};
		case "failedFinal":
			return { message: finalFailureOf(reportError) };
		default:
			return {};
	}
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

	const usualRetry = useQueryClient().getDefaultOptions().queries?.retry;
	const report = useQuery({
		queryKey: reportQueryKeyOf(owner, reportId),
		queryFn: () => reads.getReport(owner, reportId),
		refetchOnMount: "always",
		retry: retriesUnlessFinal(usualRetry),
	});
	const ownerName = useOwnerName(owner, reads);

	const fetchedAt = report.data === undefined ? null : report.dataUpdatedAt;
	const fresh = isAFreshAnswer(fetchedAt, mountedAt) ? report.data : undefined;
	const printable = printableOf(fresh, owner, ownerName.data, terms.getTerm);
	const { fonts, askAgain: askForTheFontsAgain } = useFontsOnceDrawn(
		printable !== undefined,
		effects.loadFonts,
	);
	useDocumentTitle(
		printable && printTitleOf(printable.report.name, printable.ownerLine),
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
	const failure = failureLookOf(state, report.error, {
		forTheReport: () => askAgainFor(report),
		forTheFailedPart: () => {
			if (ownerName.isError) askAgainFor(ownerName);
			if (fonts === "failed") askForTheFontsAgain();
		},
	});
	const { printWhenReady, from } = printRequestOf(location.state);
	const reportPath = reportPathOf(owner, reportId);
	const reportsPath = reportsPathOf(owner);
	const back = wayBackOf(state, from, reportPath, reportsPath);

	usePrintOnce({
		state,
		printWhenReady,
		from,
		here: location.pathname,
		reportPath,
		reportsPath,
		port: effects,
	});

	return (
		<Box data-render-ready={state === "ready" ? "true" : "false"} sx={{ p: 4 }}>
			<Button
				component={RouterLink}
				to={back.to}
				replace
				state={RETURNED_FROM_PRINT}
				sx={SCREEN_ONLY}
			>
				{back.label}
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
					failureMessage={failure.message}
					onRetry={failure.retry}
				>
					{printable !== undefined && (
						<ReportPrintDocument
							report={printable.report}
							ownerLine={printable.ownerLine}
							producedAt={report.dataUpdatedAt}
						/>
					)}
				</WidgetBody>
			</Box>
		</Box>
	);
};

export default ReportPrintPage;
