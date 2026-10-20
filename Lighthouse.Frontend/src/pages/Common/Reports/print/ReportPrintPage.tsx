import type React from "react";
import { useLocation, useParams } from "react-router";
import type { IReportOwner } from "../../../../models/Reports/Report";
import { reportsPathOf } from "../reportOwner";
import PrintSheet, { type WayBack } from "./PrintSheet";
import type { PrintPageState } from "./printPageState";
import type { PrintReadPort } from "./printReadPort";
import { printRequestOf } from "./printRequest";
import ReportPrintContent, {
	type PrintPageLook,
	type PrintTerms,
} from "./ReportPrintContent";
import {
	type PrintEffectPort,
	type PrintOrigin,
	stepBackFromThePrintPage,
	usePrintOnceAfterNavigating,
} from "./usePrintOnce";

const BACK_TO_REPORT = "‹ Back to report";
const BACK_TO_REPORTS = "‹ Back to Reports";

interface ReportPrintPageProps {
	readonly kind: IReportOwner["kind"];
	readonly reads: PrintReadPort;
	readonly terms: PrintTerms;
	readonly effects: PrintEffectPort;
}

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

interface OnThePrintPageProps {
	readonly look: PrintPageLook;
	readonly reportsPath: string;
	readonly effects: PrintEffectPort;
	readonly children: React.ReactNode;
}

const OnThePrintPage: React.FC<OnThePrintPageProps> = ({
	look,
	reportsPath,
	effects,
	children,
}) => {
	const location = useLocation();
	const { printWhenReady, from } = printRequestOf(location.state);

	usePrintOnceAfterNavigating({
		state: look.state,
		printWhenReady,
		from,
		here: location.pathname,
		reportsPath,
		port: effects,
	});

	return (
		<PrintSheet
			ready={look.state === "ready"}
			back={wayBackOf(look.state, from, reportsPath, effects)}
		>
			{children}
		</PrintSheet>
	);
};

/** The print page of a report, opened by its address: the report on its sheet, and the way back. */
const ReportPrintPage: React.FC<ReportPrintPageProps> = ({
	kind,
	reads,
	terms,
	effects,
}) => {
	const params = useParams();
	const owner: IReportOwner = { kind, id: Number(params.id) };
	const reportsPath = reportsPathOf(owner);

	return (
		<ReportPrintContent
			owner={owner}
			reportId={Number(params.reportId)}
			reads={reads}
			terms={terms}
			effects={effects}
			renderSheet={(look, report) => (
				<OnThePrintPage look={look} reportsPath={reportsPath} effects={effects}>
					{report}
				</OnThePrintPage>
			)}
		/>
	);
};

export default ReportPrintPage;
