import { z } from "zod";

/**
 * Schema for a single blocked count snapshot from the backend.
 * recordedAt: ISO date string (DateOnly on the backend).
 * blockedCount: number of blocked work items at that point in time.
 */
export const BlockedCountSnapshotSchema = z.object({
	recordedAt: z.string(),
	blockedCount: z.number().int().min(0),
});

export type BlockedCountSnapshot = z.infer<typeof BlockedCountSnapshotSchema>;

/**
 * Schema for the full blockedCountHistory response array. The response is checked where it enters
 * the app, so a row with a missing or wrongly typed field is refused here instead of reaching a
 * chart. It stays a string for the day, because coercing a missing date to a Date would turn it
 * into 1970.
 */
export const BlockedCountHistoryResponseSchema = z.array(
	BlockedCountSnapshotSchema,
);

export type BlockedCountHistoryResponse = z.infer<
	typeof BlockedCountHistoryResponseSchema
>;
