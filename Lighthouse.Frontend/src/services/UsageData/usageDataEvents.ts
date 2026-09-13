import { useCallback, useContext, useEffect } from "react";
import { useLocation } from "react-router";
import {
	readUsageDataConsentToken,
	useUsageDataConsent,
} from "../../hooks/useUsageDataConsent";
import type { UsageDataRouteKey } from "../../models/UsageData/UsageData";
import { ApiServiceContext } from "../Api/ApiServiceContext";
import {
	type IUsageDataEvent,
	UsageDataEventName,
} from "../Api/UsageDataService";
import {
	forgetWhatWasNoticed,
	type NoticedPage,
	notice,
	takeWhatWasNoticed,
} from "./usageDataBuffer";
import { usageDataRouteKeyFor } from "./usageDataRouteKeys";

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
 * Which of the two openings this is, decided from the page itself.
 *
 * The name and the page each say which kind it is, so they can disagree - and a message read
 * straight would be counted as an opening that never happened. The server refuses that
 * disagreement; choosing the name from the page here is why it never has to.
 */
const asOpeningOf = (route: UsageDataRouteKey): UsageDataEventName =>
	route.startsWith("TeamDetail_")
		? UsageDataEventName.TeamTabOpened
		: UsageDataEventName.PortfolioTabOpened;

const asHandedIn = (
	pages: NoticedPage[],
	handedInAt: number,
): IUsageDataEvent[] =>
	pages.map((page, position) => ({
		name: asOpeningOf(page.route),
		route: page.route,
		offsetMs: Math.max(0, handedInAt - page.noticedAt),
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

		const route = usageDataRouteKeyFor(pathname);

		if (route !== undefined) {
			notice({ route, noticedAt: Date.now() });
		}
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
