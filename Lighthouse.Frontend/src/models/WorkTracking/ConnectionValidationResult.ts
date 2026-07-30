// SCAFFOLD (DISTILL slice 04, Story #5577)
//
// ADR-118 decision 5. Validation has always been able to say "this failed and here is why". It
// could not say "this works, and here is a capability your instance cannot offer" — the frontend
// collapsed the whole response to a boolean. That is the channel the ServiceNow history advisory
// needs, and the reason it is here rather than annotating a chart: the person who can act on it is
// the administrator configuring the connection, and re-validating is what clears it.

export interface IConnectionValidationResult {
	isValid: boolean;
	/** Present only when a working connection still has something worth saying. */
	advisory?: string;
	/** Machine-readable half of {@link advisory}, free-form per connector. */
	advisoryCode?: string;
}

const SCAFFOLD_SENTINEL = "__scaffold__";

/**
 * Reads the validation endpoint's answer, which is either a bare boolean (older connectors) or the
 * full result object.
 */
export function readConnectionValidation(
	_payload: boolean | Partial<IConnectionValidationResult> | null | undefined,
): IConnectionValidationResult {
	// Deliberately wrong: a scaffold that echoed the payload back would satisfy the validity cases
	// before any of this existed, and validity is the half that already worked.
	return {
		isValid: false,
		advisory: SCAFFOLD_SENTINEL,
		advisoryCode: SCAFFOLD_SENTINEL,
	};
}
