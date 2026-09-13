import type {
	UsageDataRouteKey,
	UsageDataWorkTrackingSystem,
} from "../../models/UsageData/UsageData";
import type { UsageDataEventName } from "../Api/UsageDataService";

/**
 * One thing worth reporting, as this browser holds it until the next hand-in.
 *
 * Two of these say which page somebody opened and carry a page; the rest say somebody did
 * something, which happens on no particular page, and carry none. The server refuses one that
 * carries something its event has no business carrying, so this is the shape rather than a
 * convenience.
 */
export interface NoticedEvent {
	name: UsageDataEventName;
	route?: UsageDataRouteKey;
	workTrackingSystem?: UsageDataWorkTrackingSystem;
	noticedAt: number;
}

/**
 * Everything this browser remembers about what it has seen. It is an array and nothing else -
 * no storage key, no database, no worker. A queue kept anywhere it would survive the tab would also
 * survive the withdrawal it is meant to obey, and would send afterwards. Losing what is in here when
 * a tab closes costs nobody anything, which is why it is the cheap side of that trade.
 *
 * It sits in its own file, rather than inside the detector that fills it, because withdrawing
 * consent has to throw it away as well - and if the detector owned it, the withdrawal code and the
 * detector would have to import one another in a circle.
 */
let noticed: NoticedEvent[] = [];

export const notice = (seen: NoticedEvent): void => {
	noticed.push(seen);
};

export const takeWhatWasNoticed = (): NoticedEvent[] => {
	const taken = noticed;
	noticed = [];
	return taken;
};

export const forgetWhatWasNoticed = (): void => {
	noticed = [];
};
