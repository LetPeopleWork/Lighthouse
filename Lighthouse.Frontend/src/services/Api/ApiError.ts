export class ApiError extends Error {
	public readonly code: string | number;
	public readonly technicalDetails?: string;
	public readonly fieldName?: string;

	constructor(
		code: string | number,
		message: string,
		technicalDetails?: string,
		fieldName?: string,
	) {
		super(message);
		this.code = code;
		this.technicalDetails = technicalDetails;
		this.fieldName = fieldName;
		this.name = "ApiError";
		// Set the prototype explicitly for older TS targets
		Object.setPrototypeOf(this, ApiError.prototype);
	}

	toString() {
		return `ApiError(${this.code}): ${this.message}`;
	}
}
