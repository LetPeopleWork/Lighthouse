export const SecretStates = {
	Envelope: "Envelope",
	LegacyCbc: "LegacyCbc",
	LegacyPlaintext: "LegacyPlaintext",
	Unreadable: "Unreadable",
} as const;

export type SecretState = (typeof SecretStates)[keyof typeof SecretStates];

export interface IWorkTrackingSystemOption {
	key: string;
	value: string;
	isSecret: boolean;
	isOptional: boolean;
	secretState?: SecretState | null;
}
