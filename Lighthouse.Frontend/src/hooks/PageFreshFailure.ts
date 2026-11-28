/** A failed ask, together with the page it was asked on, so a later page knows to ask again. */
export class PageFreshFailure extends Error {
	constructor(
		readonly askedOnPage: number,
		readonly reason: unknown,
	) {
		super(`The shared read asked on page ${askedOnPage} failed.`);
		this.name = "PageFreshFailure";
	}
}
