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
	/**
	 * The oldest day the server still keeps finished items for, when a refusal names one: a day, null when
	 * every finished item is kept, or undefined when the refusal says nothing about it.
	 */
	public readonly earliestStart?: string | null;

	constructor(
		code: string | number,
		message: string,
		technicalDetails?: string,
		fieldName?: string,
		problemCode?: string,
		earliestStart?: string | null,
	) {
		super(message);
		this.code = code;
		this.technicalDetails = technicalDetails;
		this.fieldName = fieldName;
		this.problemCode = problemCode;
		this.earliestStart = earliestStart;
		this.name = "ApiError";
		// Set the prototype explicitly for older TS targets
		Object.setPrototypeOf(this, ApiError.prototype);
	}

	toString() {
		return `ApiError(${this.code}): ${this.message}`;
	}
}
