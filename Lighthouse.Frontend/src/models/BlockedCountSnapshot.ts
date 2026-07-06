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
 * Schema for the full blockedCountHistory response array.
 * Validates at the trust boundary per rolling-adoption gate
 * from docs/ci-learnings.md (2026-06-07).
 */
export const BlockedCountHistoryResponseSchema = z.array(
	BlockedCountSnapshotSchema,
);

export type BlockedCountHistoryResponse = z.infer<
	typeof BlockedCountHistoryResponseSchema
>;
