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

/** The reason the server sends when asked to bring back something that is not away. */
export const DELIVERY_NOT_ARCHIVED_CODE = "delivery-not-archived";

/**
 * The exact opposite instruction to the one above, and it arrives as the same conflict — so a screen
 * reading only the status would tell somebody to bring back a Delivery that is already back.
 */
export function isNotArchivedRefusal(error: unknown): boolean {
	return (
		error instanceof ApiError &&
		error.problemCode === DELIVERY_NOT_ARCHIVED_CODE
	);
}

export function notArchivedRefusalMessage(deliveryTerm: string): string {
	return `This ${deliveryTerm} is already back in the active list.`;
}
