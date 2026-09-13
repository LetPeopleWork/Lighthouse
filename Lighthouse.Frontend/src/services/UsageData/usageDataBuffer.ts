import type { UsageDataRouteKey } from "../../models/UsageData/UsageData";

export interface NoticedPage {
	route: UsageDataRouteKey;
	noticedAt: number;
}

/**
 * Everything this browser remembers about pages somebody opened. It is an array and nothing else -
 * no storage key, no database, no worker. A queue kept anywhere it would survive the tab would also
 * survive the withdrawal it is meant to obey, and would send afterwards. Losing what is in here when
 * a tab closes costs nobody anything, which is why it is the cheap side of that trade.
 *
 * It sits in its own file, rather than inside the detector that fills it, because withdrawing
 * consent has to throw it away as well - and if the detector owned it, the withdrawal code and the
 * detector would have to import one another in a circle.
 */
let noticed: NoticedPage[] = [];

export const notice = (page: NoticedPage): void => {
	noticed.push(page);
};

export const takeWhatWasNoticed = (): NoticedPage[] => {
	const taken = noticed;
	noticed = [];
	return taken;
};

export const forgetWhatWasNoticed = (): void => {
	noticed = [];
};
