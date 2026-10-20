import createCache, { type EmotionCache } from "@emotion/cache";
import { CacheProvider } from "@emotion/react";
import { CssBaseline, ThemeProvider } from "@mui/material";
import React, { useCallback, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import type { IReportOwner } from "../../../../models/Reports/Report";
import { lightTheme } from "../../../../utils/theme/appThemes";
import {
	browserPrintEffects,
	openPrintFrame,
	type PrintFrame,
} from "./browserPrintEffects";
import PrintSheet from "./PrintSheet";
import ReportPrintContent, { type PrintPageLook } from "./ReportPrintContent";
import { usePrintPagePorts } from "./reportPrintRoutes";
import {
	A_PART_COULD_NOT_BE_LOADED,
	type PrintEffectPort,
	type PrintOutcome,
	usePrintOnce,
} from "./usePrintOnce";

interface PrintFrameHostProps {
	readonly owner: IReportOwner;
	readonly reportId: number;
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

/** Opens the frame once per mount, and takes it away again when the host goes. */
const useOpenedFrame = (
	tell: PrintFrameHostProps["tell"],
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
		openPrintFrame().then(
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
			made?.close();
		};
	}, []);

	return opened;
};

// The frame cannot be seen, so a read, a font or a picture that never arrives would leave the spinner turning with no
// word; this leaves room for a slow connection's retried reads while still ending.
const THE_TIME_A_FRAME_IS_GIVEN_MS = 30_000;

interface Budgeted {
	readonly timedOut: boolean;
	/** Tells the host, and stops the clock once the dialog is asked for, since some browsers return only when it closes. */
	readonly tell: PrintFrameHostProps["tell"];
}

/** Gives the export a fixed time from the host's mount to asking for the dialog, and says when it has run out. */
const useTimeToPrint = (tell: PrintFrameHostProps["tell"]): Budgeted => {
	const [timedOut, setTimedOut] = useState(false);
	const clock = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

	useEffect(() => {
		clock.current = setTimeout(
			() => setTimedOut(true),
			THE_TIME_A_FRAME_IS_GIVEN_MS,
		);
		return () => clearTimeout(clock.current);
	}, []);

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
const PrintFrameHost: React.FC<PrintFrameHostProps> = ({
	owner,
	reportId,
	tell,
}) => {
	const { reads, terms } = usePrintPagePorts();
	const { timedOut, tell: tellTheHost } = useTimeToPrint(tell);
	const opened = useOpenedFrame(tellTheHost);

	if (opened === undefined) return null;
	const { frame, effects, cache } = opened;

	return (
		<DrawingGuard onBroken={() => tellTheHost(A_PART_COULD_NOT_BE_LOADED)}>
			{createPortal(
				<CacheProvider value={cache}>
					<ThemeProvider theme={lightTheme}>
						<CssBaseline />
						<ReportPrintContent
							owner={owner}
							reportId={reportId}
							reads={reads}
							terms={terms}
							effects={effects}
							timedOut={timedOut}
							renderSheet={(look, report) => (
								<InTheFrame look={look} effects={effects} tell={tellTheHost}>
									{report}
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
