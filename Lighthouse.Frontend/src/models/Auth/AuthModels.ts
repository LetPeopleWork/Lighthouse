export enum AuthMode {
	Disabled = "Disabled",
	Enabled = "Enabled",
	Misconfigured = "Misconfigured",
	Blocked = "Blocked",
}

export interface RuntimeAuthStatus {
	mode: AuthMode;
	misconfigurationMessage?: string;
}

export interface AuthSessionStatus {
	isAuthenticated: boolean;
	displayName?: string;
	email?: string;
}
