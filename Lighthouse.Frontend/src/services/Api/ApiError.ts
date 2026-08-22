export class ApiError extends Error {
	public readonly code: string | number;
	public readonly technicalDetails?: string;
	public readonly fieldName?: string;
	/**
	 * The machine-readable reason the server gave, where it gave one. Two refusals can share a
	 * status code and still need different words on screen — a Delivery that is archived and one
	 * that somebody else has just changed are both a conflict — and matching on the prose of the
	 * message to tell them apart breaks the moment that prose is reworded.
	 */
	public readonly problemCode?: string;

	constructor(
		code: string | number,
		message: string,
		technicalDetails?: string,
		fieldName?: string,
		problemCode?: string,
	) {
		super(message);
		this.code = code;
		this.technicalDetails = technicalDetails;
		this.fieldName = fieldName;
		this.problemCode = problemCode;
		this.name = "ApiError";
		// Set the prototype explicitly for older TS targets
		Object.setPrototypeOf(this, ApiError.prototype);
	}

	toString() {
		return `ApiError(${this.code}): ${this.message}`;
	}
}
