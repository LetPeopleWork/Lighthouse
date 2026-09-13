import type { UsageDataWorkTrackingSystem } from "../../models/UsageData/UsageData";
import type { UsageDataEventName } from "../Api/UsageDataService";

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
	throw new Error("useUsageDataReporter is not implemented");
};
