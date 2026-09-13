import { useCallback } from "react";
import { useUsageDataConsentIfKnown } from "../../hooks/useUsageDataConsent";
import type { UsageDataWorkTrackingSystem } from "../../models/UsageData/UsageData";
import type { WorkTrackingSystemType } from "../../models/WorkTracking/WorkTrackingSystemConnection";
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
 * Which kind of system a connection is, said in the words usage data publishes rather than the
 * words the product stores.
 *
 * Written out rather than passed straight through, even though the two lists happen to spell the
 * same names today. They are separate on purpose - what leaves an instance must not change because
 * a storage concern changed - and writing it out is what makes a new kind of system fail to compile
 * here rather than start travelling unannounced.
 */
const asSomethingWeDisclose: Record<
	WorkTrackingSystemType,
	UsageDataWorkTrackingSystem
> = {
	AzureDevOps: "AzureDevOps",
	Jira: "Jira",
	Linear: "Linear",
	Csv: "Csv",
	ServiceNow: "ServiceNow",
};

export const usageDataWorkTrackingSystemFor = (
	system: WorkTrackingSystemType,
): UsageDataWorkTrackingSystem => asSomethingWeDisclose[system];

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
	const consent = useUsageDataConsentIfKnown();

	// What decides is the answer the server gave about this browser, never whether a token is lying
	// around. Refusing mints a token too, so a browser that said no holds one - and an answer that
	// has not arrived yet is not a yes either, nor is a screen mounted where nothing asked.
	const isSending = consent?.indicatorState === "sending";

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
