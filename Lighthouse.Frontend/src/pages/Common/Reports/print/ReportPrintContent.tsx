import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useEffect, useRef, useState } from "react";
import { LIGHTHOUSE_MARK } from "../../../../components/App/LetPeopleWork/LighthouseLogo";
import type { IReport, IReportOwner } from "../../../../models/Reports/Report";
import { WidgetBody } from "../../MetricsView/WidgetShell";
import type { WidgetStatus } from "../../MetricsView/widgetStatus";
import { useReportRead } from "../useReportRead";
import { finalFailureOf } from "../wizard/reportRefusalCopy";
import {
	isAFreshAnswer,
	type PrintLoadState,
	type PrintPageState,
	printPageStateOf,
} from "./printPageState";
import type { PrintReadPort } from "./printReadPort";
import ReportPrintDocument, {
	ownerLineOf,
	printTitleOf,
} from "./ReportPrintDocument";
import {
	PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
	type PrintEffectPort,
	type PrintFailureLook,
	THE_REPORT_COULD_NOT_BE_LOADED,
} from "./usePrintOnce";

const PREPARING = "Preparing the PDF…";

/** The weights the printed report is set in: its text and its headings. */
const THE_PRINTED_FACES = [
	'400 1em "Quicksand Variable"',
	'700 1em "Quicksand Variable"',
];

/** What the printed report needs of the instance's terms: reading them, and asking for them again, but never changing them. */
export interface PrintTerms {
	readonly getTerm: (key: string) => string;
	readonly isLoading: boolean;
	readonly error: string | null;
	readonly refetch: () => void;
}

/** What the printed report asks of the browser of the window it is drawn in: its fonts, its pictures and its title. */
export type PrintContentEffects = Pick<
	PrintEffectPort,
	"loadFonts" | "loadPicture" | "setTitle"
>;

/** Where the printed report stands, for whatever it is drawn on. */
export interface PrintPageLook {
	readonly state: PrintPageState;
	readonly failure: PrintFailureLook;
}

interface ReportPrintContentProps {
	readonly owner: IReportOwner;
	readonly reportId: number;
	readonly reads: PrintReadPort;
	readonly terms: PrintTerms;
	readonly effects: PrintContentEffects;
	/** Whether the time the caller allows for printing has run out; only the hidden frame an export prints from has a limit. */
	readonly timedOut?: boolean;
	/** The sheet the report is drawn on, told where the report stands. */
	readonly renderSheet: (
		look: PrintPageLook,
		report: React.ReactNode,
	) => React.ReactNode;
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
		load(what).then(
			(outcome) => {
				if (!gone) setState(outcome);
			},
			() => {
				if (!gone) setState("failed");
			},
		);
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

/** Names the printed document while the report is drawn, and gives the old name back when it goes. */
const useTitle = (
	title: string | undefined,
	setTitle: PrintContentEffects["setTitle"],
) => {
	useEffect(() => {
		if (title === undefined) return;
		return setTitle(title);
	}, [title, setTitle]);
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

/**
 * The report as it goes on paper, read afresh, with everything it shows loaded into the window it is drawn in. What it
 * is drawn on is the caller's: the print page opened by its address, or the hidden frame an export prints from.
 */
const ReportPrintContent: React.FC<ReportPrintContentProps> = ({
	owner,
	reportId,
	reads,
	terms,
	effects,
	timedOut = false,
	renderSheet,
}) => {
	const mountedAt = useRef(Date.now()).current;

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
	useTitle(
		printable && printTitleOf(printable.report.name, printable.ownerLine),
		effects.setTitle,
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
		timedOut,
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

	return renderSheet(
		{
			state,
			failure: { message: failure.message, retry: failure.retry !== undefined },
		},
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
		</WidgetBody>,
	);
};

export default ReportPrintContent;
