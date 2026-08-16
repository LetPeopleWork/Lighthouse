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
	"CouldNotBeWritten",
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

// The outcomes an operator is ever shown a row for. A secret that moved needs no wording because it
// needs no row - the counts already say how many - so only the three that ask somebody to do something
// are spelled out here.
export type SecretOutcomeNeedingAttention = Extract<
	SecretMoveOutcome,
	"CouldNotBeRead" | "CouldNotBeWritten" | "NotEncrypted"
>;

export const SECRET_OUTCOME_WORDING: Record<
	SecretOutcomeNeedingAttention,
	string
> = {
	CouldNotBeRead: "could not be read, and was left untouched",
	CouldNotBeWritten: "the database would not take the change — run this again",
	NotEncrypted: "was not encrypted, and was left untouched",
};
