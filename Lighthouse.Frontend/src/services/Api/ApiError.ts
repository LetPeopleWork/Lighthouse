export class ApiError extends Error {
	public readonly code: string | number;

	constructor(code: string | number, message: string) {
		super(message);
		this.code = code;
		this.name = "ApiError";
		// Set the prototype explicitly for older TS targets
		Object.setPrototypeOf(this, ApiError.prototype);
	}

	toString() {
		return `ApiError(${this.code}): ${this.message}`;
	}
}
