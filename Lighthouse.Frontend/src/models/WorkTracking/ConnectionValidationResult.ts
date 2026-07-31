export interface IConnectionValidationResult {
	isValid: boolean;
	/** Present only when a working connection still has something worth saying (ADR-118 D5). */
	advisory?: string;
	/** Machine-readable half of {@link advisory}, free-form per connector. */
	advisoryCode?: string;
}

/**
 * Reads the validation endpoint's answer, which is either a bare boolean (older connectors) or the
 * full result object. An absent answer is not evidence that anything worked.
 */
export function readConnectionValidation(
	payload: boolean | Partial<IConnectionValidationResult> | null | undefined,
): IConnectionValidationResult {
	if (payload === null || payload === undefined) {
		return { isValid: false };
	}

	if (typeof payload === "boolean") {
		return { isValid: payload };
	}

	return {
		isValid: payload.isValid === true,
		advisory: payload.advisory,
		advisoryCode: payload.advisoryCode,
	};
}
