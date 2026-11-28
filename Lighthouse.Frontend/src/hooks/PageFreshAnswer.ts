/** An answer together with the page it was asked on, so a component can tell whether it is its own. */
export interface PageFreshAnswer<T> {
	answer: T;
	askedOnPage: number;
}
