export interface IConnectionValidationResult {
	isValid: boolean;
	/** A stable identifier for the kind of refusal, for a caller that needs to branch on it. */
	code?: string;
	/** What went wrong, in a sentence written to be read by the person who has to fix it. */
	message?: string;
	/** What to try next, or whatever the work tracking system itself answered. */
	technicalDetails?: string;
	/** The setting the refusal is about, where the connector could name one. */
	fieldName?: string;
}

/**
 * The answer as it arrives, which is not the same shape as the model above: the connectors leave a
 * hint or a field name they have nothing to say about as null rather than omitting it.
 */
export interface IConnectionValidationPayload {
	isValid?: boolean;
	code?: string | null;
	message?: string | null;
	technicalDetails?: string | null;
	fieldName?: string | null;
}

/**
 * Reads the validation endpoint's answer, which is either a bare boolean (older connectors) or the
 * full result object. An absent answer is not evidence that anything worked.
 *
 * Only the keys a connector actually answers are carried over. A key that is absent, null or empty
 * is left off rather than passed on as a blank, because the screens have their own sentence for a
 * refusal that came without one and a blank would take its place.
 */
export function readConnectionValidation(
	payload: boolean | IConnectionValidationPayload | null | undefined,
): IConnectionValidationResult {
	if (payload === null || payload === undefined) {
		return { isValid: false };
	}

	if (typeof payload === "boolean") {
		return { isValid: payload };
	}

	const verdict: IConnectionValidationResult = {
		isValid: payload.isValid === true,
	};

	const code = sentenceIn(payload.code);
	if (code !== undefined) {
		verdict.code = code;
	}

	const message = sentenceIn(payload.message);
	if (message !== undefined) {
		verdict.message = message;
	}

	const technicalDetails = sentenceIn(payload.technicalDetails);
	if (technicalDetails !== undefined) {
		verdict.technicalDetails = technicalDetails;
	}

	const fieldName = sentenceIn(payload.fieldName);
	if (fieldName !== undefined) {
		verdict.fieldName = fieldName;
	}

	return verdict;
}

function sentenceIn(value: unknown): string | undefined {
	return typeof value === "string" && value !== "" ? value : undefined;
}
