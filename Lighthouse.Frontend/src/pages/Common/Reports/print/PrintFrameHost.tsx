import createCache, { type EmotionCache } from "@emotion/cache";
import { CacheProvider } from "@emotion/react";
import { CssBaseline, ThemeProvider } from "@mui/material";
import type React from "react";
import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import type { IReportOwner } from "../../../../models/Reports/Report";
import { lightTheme } from "../../../../utils/theme/appThemes";
import {
	browserPrintEffects,
	openPrintFrame,
	type PrintFrame,
	type WindowPrintEffects,
} from "./browserPrintEffects";
import PrintSheet from "./PrintSheet";
import ReportPrintContent, { type PrintPageLook } from "./ReportPrintContent";
import { usePrintPagePorts } from "./reportPrintRoutes";
import {
	PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
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
	readonly effects: WindowPrintEffects;
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
					latestTell.current({
						kind: "failed",
						message: PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
						retry: true,
					});
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

interface InTheFrameProps {
	readonly look: PrintPageLook;
	readonly effects: WindowPrintEffects;
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
	const opened = useOpenedFrame(tell);

	if (opened === undefined) return null;
	const { frame, effects, cache } = opened;

	return createPortal(
		<CacheProvider value={cache}>
			<ThemeProvider theme={lightTheme}>
				<CssBaseline />
				<ReportPrintContent
					owner={owner}
					reportId={reportId}
					reads={reads}
					terms={terms}
					effects={effects}
					renderSheet={(look, report) => (
						<InTheFrame look={look} effects={effects} tell={tell}>
							{report}
						</InTheFrame>
					)}
				/>
			</ThemeProvider>
		</CacheProvider>,
		frame.body,
	);
};

export default PrintFrameHost;
