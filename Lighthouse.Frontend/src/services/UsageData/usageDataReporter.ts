import { useCallback } from "react";
import { useUsageDataConsent } from "../../hooks/useUsageDataConsent";
import type { UsageDataWorkTrackingSystem } from "../../models/UsageData/UsageData";
import type { UsageDataEventName } from "../Api/UsageDataService";
import { notice } from "./usageDataBuffer";

/**
 * One thing somebody did, as a call site hands it in.
 *
 * There is no field here for a page address. The two events that say which page somebody was on
 * are worked out from the router by the detector, which is the only place allowed to read an
 * address at all - so a call site cannot supply one even by mistake.
 */
export interface UsageDataCapabilityUse {
	name: UsageDataEventName;
	workTrackingSystem?: UsageDataWorkTrackingSystem;
}

/**
 * How a screen says that somebody used something, rather than that somebody looked at something.
 *
 * It is a hook rather than a plain function because what decides whether anything is recorded is
 * the answer this instance gave about this browser, and that answer lives in React state that is
 * refreshed on a clock. A plain function would have to read a token lying around instead - and a
 * browser that refused holds one of those too, so it would collect on behalf of exactly the person
 * who asked us not to.
 *
 * Handing a call site a function that quietly does nothing is the point: every screen calls it the
 * same way, and none of them has to carry a branch about consent that could be got wrong.
 */
export const useUsageDataReporter = (): ((
	use: UsageDataCapabilityUse,
) => void) => {
	const { indicatorState } = useUsageDataConsent();

	// What decides is the answer the server gave about this browser, never whether a token is lying
	// around. Refusing mints a token too, so a browser that said no holds one - and an answer that
	// has not arrived yet is not a yes either.
	const isSending = indicatorState === "sending";

	return useCallback(
		(use: UsageDataCapabilityUse): void => {
			if (!isSending) {
				return;
			}

			notice({ ...use, noticedAt: Date.now() });
		},
		[isSending],
	);
};
