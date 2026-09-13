import { useCallback, useContext, useEffect } from "react";
import { useLocation } from "react-router";
import {
	readUsageDataConsentToken,
	useUsageDataConsent,
} from "../../hooks/useUsageDataConsent";
import { ApiServiceContext } from "../Api/ApiServiceContext";
import type { IUsageDataEvent } from "../Api/UsageDataService";
import {
	forgetWhatWasNoticed,
	type NoticedEvent,
	notice,
	takeWhatWasNoticed,
} from "./usageDataBuffer";
import { usageDataPageOpeningFor } from "./usageDataRouteKeys";

/**
 * How long a page opening may sit here before it is handed in.
 *
 * Nothing depends on the exact figure: a flush only happens when there is something to flush, so a
 * tab nobody is touching costs nothing at all. A tab somebody is working in is the case to be
 * honest about - at this interval it asks the server about consent up to a hundred and twenty
 * times an hour, which is two orders of magnitude more often than the hourly check the footer
 * already makes. Shortening this makes that worse, and buys nothing back.
 */
export const FLUSH_INTERVAL_MS = 30 * 1000;

/**
 * How long somebody has to still be on a page before it counts as having been opened.
 *
 * Nothing is measured and nothing about duration is ever sent - this decides which openings are
 * recorded at all, not what any of them carries. Without it, clicking through three tabs to find
 * something records three openings, two of which nobody looked at, and the same is true of every
 * press of the browser's back button.
 *
 * It also silences the openings this application causes rather than a person: a Team with no
 * features is redirected off that tab within milliseconds, so what would otherwise be recorded is a
 * tab the reader never saw and never chose.
 *
 * The cost, stated plainly: a deliberate quick look - open the metrics, read the one number, leave -
 * is discarded along with the accidents. Five seconds is where that trade was set; nothing depends
 * on the exact figure.
 */
export const DWELL_BEFORE_A_PAGE_COUNTS_MS = 5 * 1000;

const asHandedIn = (
	seen: NoticedEvent[],
	handedInAt: number,
): IUsageDataEvent[] =>
	seen.map(({ noticedAt, ...event }, position) => ({
		...event,
		offsetMs: Math.max(0, handedInAt - noticedAt),
		sequence: position,
	}));

/**
 * Notices which of the named pages somebody is on, and hands the list in every so often.
 *
 * Mounted once, at the top of the application. Mounting it on the pages it watches would give each
 * of them its own list and its own clock, and the list a page was holding would be thrown away the
 * moment somebody navigated off it.
 */
export const useUsageDataEventDetector = (): void => {
	const { usageDataService } = useContext(ApiServiceContext);
	const { indicatorState } = useUsageDataConsent();
	const { pathname } = useLocation();

	// What decides is the answer the server gave about this browser, never whether a token is
	// lying around. Refusing mints a token too, so a browser that said no holds one - and asking
	// the wrong question here would collect on behalf of exactly the person who asked us not to.
	const isSending = indicatorState === "sending";

	useEffect(() => {
		if (!isSending) {
			return;
		}

		const opening = usageDataPageOpeningFor(pathname);

		if (opening === undefined) {
			return;
		}

		const counted = setTimeout(() => {
			notice({ ...opening, noticedAt: Date.now() });
		}, DWELL_BEFORE_A_PAGE_COUNTS_MS);

		return () => clearTimeout(counted);
	}, [isSending, pathname]);

	// Stryker disable ArrayDeclaration: the dependency lists below only show themselves when a dependency changes, and a test contrived to change one would be watching React re-run an effect rather than anything this feature promises.
	const handIn = useCallback((): void => {
		const token = readUsageDataConsentToken();
		const pages = takeWhatWasNoticed();

		if (token === null || pages.length === 0) {
			return;
		}

		usageDataService
			.postEvents(token, asHandedIn(pages, Date.now()))
			.catch(() => {
				// A batch that does not arrive is gone, and that is the whole handling. Trying again
				// later would be a second chance to send something the person may have withdrawn in
				// the meantime, and a page opening nobody hears about costs nothing.
			});
	}, [usageDataService]);

	useEffect(() => {
		const timer = setInterval(handIn, FLUSH_INTERVAL_MS);
		return () => clearInterval(timer);
	}, [handIn]);

	useEffect(() => {
		const onVisibilityChange = () => {
			// Somebody switching away or closing the lid is the ordinary ending, not the exception.
			// Waiting for the next tick of the clock would lose everything since the last one.
			if (document.visibilityState === "hidden") {
				handIn();
			}
		};

		document.addEventListener("visibilitychange", onVisibilityChange);
		return () =>
			document.removeEventListener("visibilitychange", onVisibilityChange);
	}, [handIn]);

	useEffect(() => forgetWhatWasNoticed, []);
	// Stryker restore ArrayDeclaration
};
