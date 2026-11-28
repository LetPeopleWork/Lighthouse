export interface PageFreshRead<T> {
	/** The latest good answer asked on or after the page the component appeared on; undefined while there is none. */
	answer: T | undefined;
	/** True while the component has no answer of its own and an ask is on its way. */
	isLoading: boolean;
	/** True when the component has no answer of its own, nothing is on its way and the latest ask failed. */
	isFailed: boolean;
	/** Asks again once, for every component that shares the read. */
	askAgain: () => void;
}
