import { ApiError } from "../../services/Api/ApiError";

/** The reason the server sends when it refuses a change because the Delivery has been archived. */
export const DELIVERY_ARCHIVED_CODE = "delivery-archived";

/**
 * Tells "this is closed" apart from "somebody else changed it" — both arrive as a conflict, and
 * telling a reader they are out of date when the truth is that the Delivery is retired sends them
 * to refresh a page that will say exactly the same thing again.
 */
export function isArchivedRefusal(error: unknown): boolean {
	return (
		error instanceof ApiError && error.problemCode === DELIVERY_ARCHIVED_CODE
	);
}

export function archivedRefusalMessage(deliveryTerm: string): string {
	return `This ${deliveryTerm} is archived, so it can no longer be changed. Bring it back first.`;
}
