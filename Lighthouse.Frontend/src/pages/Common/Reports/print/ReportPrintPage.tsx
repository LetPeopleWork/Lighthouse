import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useEffect, useState } from "react";
import { useLocation, useParams } from "react-router";
import { LIGHTHOUSE_MARK } from "../../../../components/App/LetPeopleWork/LighthouseLogo";
import type { IReport, IReportOwner } from "../../../../models/Reports/Report";
import type { useTerminology } from "../../../../services/TerminologyContext";
import { WidgetBody } from "../../MetricsView/WidgetShell";
import type { WidgetStatus } from "../../MetricsView/widgetStatus";
import { reportsPathOf } from "../reportOwner";
import { useReportRead } from "../useReportRead";
import { finalFailureOf } from "../wizard/reportRefusalCopy";
import PrintSheet, { type WayBack } from "./PrintSheet";
import {
	isAFreshAnswer,
	type PrintLoadState,
	type PrintPageState,
	printPageStateOf,
} from "./printPageState";
import type { PrintReadPort } from "./printReadPort";
import { printRequestOf } from "./printRequest";
import ReportPrintDocument, {
	ownerLineOf,
	printTitleOf,
} from "./ReportPrintDocument";
import {
	type PrintEffectPort,
	type PrintOrigin,
	stepBackFromThePrintPage,
	usePrintOnce,
} from "./usePrintOnce";

const PREPARING = "Preparing the PDF…";
const THE_REPORT_COULD_NOT_BE_LOADED =
	"This report couldn't be loaded, so no PDF was made.";
const PART_OF_THE_PAGE_COULD_NOT_BE_LOADED =
	"Part of the page couldn't be loaded, so no PDF was made.";
const BACK_TO_REPORT = "‹ Back to report";
const BACK_TO_REPORTS = "‹ Back to Reports";

/** The weights the printed report is set in: its text and its headings. */
const THE_PRINTED_FACES = [
	'400 1em "Quicksand Variable"',
	'700 1em "Quicksand Variable"',
];

/** What the print page needs of the instance's terms: reading them, and asking for them again, but never changing them. */
export type PrintTerms = Pick<
	ReturnType<typeof useTerminology>,
	"getTerm" | "isLoading" | "error"
> & { readonly refetch: () => void };

interface ReportPrintPageProps {
	readonly kind: IReportOwner["kind"];
	readonly reads: PrintReadPort;
	readonly terms: PrintTerms;
	readonly effects: PrintEffectPort;
}

const useOwnerName = (owner: IReportOwner, reads: PrintReadPort) =>
	useQuery({
		queryKey: ["reportPrintOwner", owner.kind, owner.id],
		queryFn: async () => {
			const found =
				owner.kind === "team"
					? await reads.getTeam(owner.id)
					: await reads.getPortfolio(owner.id);
			// An owner deleted since the report was opened answers with no one, and its name would never arrive.
			if (found === null || found === undefined) {
				throw new Error(`The ${owner.kind} ${owner.id} was not found`);
			}
			return found.name;
		},
	});

interface AskedOfTheBrowser {
	readonly state: PrintLoadState;
	readonly askAgainIfFailed: () => void;
}

/** Asks the browser for something the page shows once the report is drawn, so it loads what the page actually shows. */
const useLoadedOnceDrawn = <T,>(
	drawn: boolean,
	load: (what: T) => Promise<PrintLoadState>,
	what: T,
): AskedOfTheBrowser => {
	const [state, setState] = useState<PrintLoadState>("pending");

	useEffect(() => {
		if (!drawn || state !== "pending") {
			return;
		}
		let gone = false;
		load(what)
			.catch((): PrintLoadState => "failed")
			.then((outcome) => {
				if (!gone) setState(outcome);
			});
		return () => {
			gone = true;
		};
	}, [drawn, state, load, what]);

	return {
		state,
		askAgainIfFailed: () => {
			if (state === "failed") setState("pending");
		},
	};
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

/**
 * A reader who came by Export steps back to where they started, so it stays in their history once, while the report
 * can still be shown. A pasted link, or a report that is gone or made by a newer version, leads to the Reports list.
 */
const wayBackOf = (
	state: PrintPageState,
	from: PrintOrigin | undefined,
	reportsPath: string,
	port: PrintEffectPort,
): WayBack => {
	if (from === undefined || state === "failedFinal") {
		return { to: reportsPath, label: BACK_TO_REPORTS };
	}
	return {
		stepBack: () => stepBackFromThePrintPage(port),
		label: from === "report" ? BACK_TO_REPORT : BACK_TO_REPORTS,
	};
};

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
			return {
				message: finalFailureOf(reportError) ?? THE_REPORT_COULD_NOT_BE_LOADED,
			};
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
	const [mountedAt] = useState(() => Date.now());

	const report = useReportRead(
		owner,
		reportId,
		() => reads.getReport(owner, reportId),
		{ refetchOnMount: "always" },
	);
	const ownerName = useOwnerName(owner, reads);

	const fetchedAt = report.data === undefined ? null : report.dataUpdatedAt;
	const fresh = isAFreshAnswer(fetchedAt, mountedAt) ? report.data : undefined;
	const printable = printableOf(fresh, owner, ownerName.data, terms.getTerm);
	const drawn = printable !== undefined;
	const fonts = useLoadedOnceDrawn(drawn, effects.loadFonts, THE_PRINTED_FACES);
	const mark = useLoadedOnceDrawn(drawn, effects.loadPicture, LIGHTHOUSE_MARK);
	// Kept out of sight until its typeface is in, so the reader never sees the report flash in a stand-in font.
	const drawnInItsOwnFaces = drawn && fonts.state === "loaded";
	useDocumentTitle(
		printable && printTitleOf(printable.report.name, printable.ownerLine),
	);

	const state = printPageStateOf({
		fetchedAt,
		mountedAt,
		reportError: report.error,
		reportFetching: report.isFetching,
		ownerName: ownerName.data ?? null,
		ownerError: ownerName.error,
		termsLoading: terms.isLoading,
		termsError: terms.error,
		fonts: fonts.state,
		mark: mark.state,
	});
	const failure = failureLookOf(state, report.error, {
		forTheReport: () => askAgainFor(report),
		forTheFailedPart: () => {
			if (ownerName.isError) askAgainFor(ownerName);
			if (terms.error !== null) terms.refetch();
			fonts.askAgainIfFailed();
			mark.askAgainIfFailed();
		},
	});
	const { printWhenReady, from } = printRequestOf(location.state);
	const reportsPath = reportsPathOf(owner);

	usePrintOnce({
		state,
		printWhenReady,
		from,
		here: location.pathname,
		reportsPath,
		port: effects,
	});

	return (
		<PrintSheet
			ready={state === "ready"}
			back={wayBackOf(state, from, reportsPath, effects)}
		>
			<WidgetBody
				widgetKey="report-print"
				status={widgetStatusOf(state)}
				hasContentToDim={drawnInItsOwnFaces}
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
		</PrintSheet>
	);
};

export default ReportPrintPage;
