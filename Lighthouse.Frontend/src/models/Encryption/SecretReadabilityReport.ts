import { z } from "zod";

export const SECRET_STATES = [
	"Envelope",
	"LegacyCbc",
	"LegacyPlaintext",
	"Unreadable",
] as const;

export const SECRET_MOVE_OUTCOMES = [
	"Unmoved",
	"Moved",
	"MovedByAnotherWriter",
	"CouldNotBeRead",
	"NotEncrypted",
] as const;

export type SecretState = (typeof SECRET_STATES)[number];

export type SecretMoveOutcome = (typeof SECRET_MOVE_OUTCOMES)[number];

export interface StoredSecret {
	connectionId: number;
	connectionName: string;
	field: string;
	keyId: string | null;
	state: SecretState;
	outcome: SecretMoveOutcome;
}

export interface ConnectionSecretSummary {
	connectionId: number;
	connectionName: string;
	movedCount: number;
	unreadableCount: number;
}

export interface SecretReadabilityReport {
	activeKeyId: string;
	movedCount: number;
	unreadableCount: number;
	secrets: StoredSecret[];
	byConnection: ConnectionSecretSummary[];
}

export const StoredSecretSchema = z.object({
	connectionId: z.number(),
	connectionName: z.string(),
	field: z.string(),
	keyId: z.string().nullable(),
	state: z.enum(SECRET_STATES),
	outcome: z.enum(SECRET_MOVE_OUTCOMES),
});

export const ConnectionSecretSummarySchema = z.object({
	connectionId: z.number(),
	connectionName: z.string(),
	movedCount: z.number(),
	unreadableCount: z.number(),
});

export const SecretReadabilityReportSchema = z.object({
	activeKeyId: z.string(),
	movedCount: z.number(),
	unreadableCount: z.number(),
	secrets: z.array(StoredSecretSchema),
	byConnection: z.array(ConnectionSecretSummarySchema),
});

// What an operator is told happened to one stored credential. "Moved by another writer" is not a
// failure: something else wrote that row under the key in force while the pass was running, which is
// where the pass was taking it anyway.
export const SECRET_OUTCOME_WORDING: Record<SecretMoveOutcome, string> = {
	Unmoved: "left where it was",
	Moved: "moved onto the active key",
	MovedByAnotherWriter: "already on the active key",
	CouldNotBeRead: "could not be read, and was left untouched",
	NotEncrypted: "was not encrypted, and was left untouched",
};
