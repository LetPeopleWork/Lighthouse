import createCache, { type EmotionCache } from "@emotion/cache";
import { CacheProvider } from "@emotion/react";
import { CssBaseline, ThemeProvider } from "@mui/material";
import React, { useCallback, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { lightTheme } from "../../../../utils/theme/appThemes";
import {
	browserPrintEffects,
	openPrintFrame,
	type PrintFrame,
} from "./browserPrintEffects";
import PrintSheet from "./PrintSheet";
import ReportPrintBundle, { type PrintedReport } from "./ReportPrintBundle";
import type { PrintPageLook, PrintTerms } from "./ReportPrintContent";
import { usePrintPagePorts } from "./reportPrintRoutes";
import {
	A_PART_COULD_NOT_BE_LOADED,
	type PrintEffectPort,
	type PrintOutcome,
	usePrintOnce,
} from "./usePrintOnce";

interface PrintFrameHostProps {
	/** The reports to print as one PDF, in the order they print. */
	readonly reports: readonly PrintedReport[];
	/** Hears that the dialog is asked for, that it closed, or why no PDF could be made. */
	readonly tell: (outcome: PrintOutcome) => void;
}

/** The frame once it has loaded, with the browser of its own window and a style cache that writes into its head. */
interface DrawnInto {
	readonly frame: PrintFrame;
	readonly effects: PrintEffectPort;
	readonly cache: EmotionCache;
}

const drawnInto = (frame: PrintFrame): DrawnInto => ({
	frame,
	effects: browserPrintEffects(frame.window),
	cache: createCache({ key: "report-pdf", container: frame.head }),
});

/**
 * Opens the frame once per mount, and takes it away again when the host goes. A frame still not open when time runs
 * out, such as one whose copied style sheet the browser never answers, ends the export as a part that could not load.
 */
const useOpenedFrame = (
	tell: PrintFrameHostProps["tell"],
	timedOut: boolean,
): DrawnInto | undefined => {
	const [opened, setOpened] = useState<DrawnInto | undefined>();
	// The frame is opened once per mount, so it reaches the host through whatever it was last handed.
	const latestTell = useRef(tell);

	useEffect(() => {
		latestTell.current = tell;
	}, [tell]);

	useEffect(() => {
		let gone = false;
		let made: PrintFrame | undefined;
		const givingUp = new AbortController();
		openPrintFrame(givingUp.signal).then(
			(frame) => {
				if (gone) {
					frame.close();
					return;
				}
				made = frame;
				setOpened(drawnInto(frame));
			},
			() => {
				if (!gone) {
					latestTell.current(A_PART_COULD_NOT_BE_LOADED);
				}
			},
		);
		return () => {
			gone = true;
			givingUp.abort();
			made?.close();
		};
	}, []);

	useEffect(() => {
		if (timedOut && opened === undefined) {
			latestTell.current(A_PART_COULD_NOT_BE_LOADED);
		}
	}, [timedOut, opened]);

	return opened;
};

/**
 * The app reads its terms once and never again on its own, so an export that finds them failed asks once more, and
 * says it is still asking until an answer to that ask has begun, so the report is not drawn against the old failure.
 * Terms that never change are given up on with everything else when time runs out.
 */
const useTermsAskedForAgain = (
	terms: PrintTerms,
	timedOut: boolean,
): boolean => {
	const termsAtMount = useRef(terms);

	useEffect(() => {
		const { error, refetch } = termsAtMount.current;
		if (error !== null) refetch();
	}, []);

	const failedAtMount = termsAtMount.current.error !== null;
	return failedAtMount && terms === termsAtMount.current && !timedOut;
};

// The frame cannot be seen, so a read, a font or a picture that never arrives would leave the spinner turning with no
// word; this leaves room for a slow connection's retried reads while still ending. Each further report adds its own
// reads, fonts and charts to draw.
const THE_TIME_A_FRAME_IS_GIVEN_MS = 30_000;
const THE_TIME_EACH_FURTHER_REPORT_ADDS_MS = 2_000;

/** How long a frame printing this many reports is given, from its mount to asking for the dialog. */
export const timeAFrameIsGivenMs = (reportCount: number): number =>
	THE_TIME_A_FRAME_IS_GIVEN_MS +
	THE_TIME_EACH_FURTHER_REPORT_ADDS_MS * Math.max(0, reportCount - 1);

interface Budgeted {
	readonly timedOut: boolean;
	/** Tells the host, and stops the clock once the dialog is asked for, since some browsers return only when it closes. */
	readonly tell: PrintFrameHostProps["tell"];
}

/** Gives the export its time from the host's mount to asking for the dialog, and says when it has run out. */
const useTimeToPrint = (
	tell: PrintFrameHostProps["tell"],
	budgetMs: number,
): Budgeted => {
	const [timedOut, setTimedOut] = useState(false);
	const clock = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

	useEffect(() => {
		clock.current = setTimeout(() => setTimedOut(true), budgetMs);
		return () => clearTimeout(clock.current);
	}, [budgetMs]);

	const tellAndStopTheClock = useCallback(
		(outcome: PrintOutcome) => {
			if (outcome.kind === "printing") clearTimeout(clock.current);
			tell(outcome);
		},
		[tell],
	);

	return { timedOut, tell: tellAndStopTheClock };
};

interface DrawingGuardProps {
	readonly onBroken: () => void;
	readonly children: React.ReactNode;
}

interface DrawingGuardState {
	readonly broken: boolean;
}

/**
 * Catches a report that breaks while it is drawn for paper, so the failure stays the export's: the surface's own
 * boundary would otherwise replace what the reader is looking at.
 */
class DrawingGuard extends React.Component<
	DrawingGuardProps,
	DrawingGuardState
> {
	constructor(props: DrawingGuardProps) {
		super(props);
		this.state = { broken: false };
	}

	static getDerivedStateFromError(): DrawingGuardState {
		return { broken: true };
	}

	componentDidCatch() {
		this.props.onBroken();
	}

	render() {
		return this.state.broken ? null : this.props.children;
	}
}

interface InTheFrameProps {
	readonly look: PrintPageLook;
	readonly effects: PrintEffectPort;
	readonly tell: PrintFrameHostProps["tell"];
	readonly children: React.ReactNode;
}

const InTheFrame: React.FC<InTheFrameProps> = ({
	look,
	effects,
	tell,
	children,
}) => {
	usePrintOnce({
		state: look.state,
		failure: look.failure,
		port: effects,
		tell,
	});
	return <PrintSheet ready={look.state === "ready"}>{children}</PrintSheet>;
};

/**
 * Prints a report from a hidden frame on the page the reader is on. The report is drawn into the frame by the app
 * itself, so it shares the app's answers, and set in its own light page whatever the reader's theme, since only the
 * frame's document goes to the print dialog.
 */
const PrintFrameHost: React.FC<PrintFrameHostProps> = ({ reports, tell }) => {
	const { reads, terms } = usePrintPagePorts();
	const { timedOut, tell: tellTheHost } = useTimeToPrint(
		tell,
		timeAFrameIsGivenMs(reports.length),
	);
	const stillAskingForTheTerms = useTermsAskedForAgain(terms, timedOut);
	const opened = useOpenedFrame(tellTheHost, timedOut);

	if (opened === undefined || stillAskingForTheTerms) return null;
	const { frame, effects, cache } = opened;

	return (
		<DrawingGuard onBroken={() => tellTheHost(A_PART_COULD_NOT_BE_LOADED)}>
			{createPortal(
				<CacheProvider value={cache}>
					<ThemeProvider theme={lightTheme}>
						<CssBaseline />
						<ReportPrintBundle
							reports={reports}
							reads={reads}
							terms={terms}
							effects={effects}
							timedOut={timedOut}
							renderSheet={(look, drawn) => (
								<InTheFrame look={look} effects={effects} tell={tellTheHost}>
									{drawn}
								</InTheFrame>
							)}
						/>
					</ThemeProvider>
				</CacheProvider>,
				frame.body,
			)}
		</DrawingGuard>
	);
};

export default PrintFrameHost;
