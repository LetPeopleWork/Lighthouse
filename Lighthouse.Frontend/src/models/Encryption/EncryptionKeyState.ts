import { z } from "zod";

export const KEY_CUSTODY_VALUES = [
	"NoDurableStore",
	"GeneratedForThisInstance",
	"SuppliedByConfiguration",
	"SuppliedByExternalSecret",
] as const;

export type KeyCustody = (typeof KEY_CUSTODY_VALUES)[number];

export interface EncryptionKeyState {
	custody: KeyCustody;
	canMint: boolean;
	activeKeyId: string;
	keyIds: string[];
	keyStorePath: string;
	legacyDefaultPresent: boolean;
}

export const EncryptionKeyStateSchema = z.object({
	custody: z.enum(KEY_CUSTODY_VALUES),
	canMint: z.boolean(),
	activeKeyId: z.string(),
	keyIds: z.array(z.string()),
	keyStorePath: z.string(),
	legacyDefaultPresent: z.boolean(),
});

// A self-hoster reading this screen is asking whether the key is theirs to keep, not what the
// server calls the case internally. The same four phrasings are what the startup log prints.
export const KEY_CUSTODY_WORDING: Record<KeyCustody, string> = {
	NoDurableStore: "the key published with the product",
	GeneratedForThisInstance: "generated for this instance",
	SuppliedByConfiguration: "supplied by configuration",
	SuppliedByExternalSecret: "supplied by a mounted secret file",
};
