export interface IApiKeyInfo {
	id: number;
	name: string;
	description: string;
	createdByUser: string;
	createdAt: string;
	lastUsedAt: string | null;
}

export interface IApiKeyCreationResult {
	id: number;
	name: string;
	description: string;
	createdByUser: string;
	createdAt: string;
	plainTextKey: string;
}

export type ApiKeyScopeRole =
	| "SystemAdmin"
	| "TeamAdmin"
	| "PortfolioAdmin"
	| "Viewer";

export type ApiKeyScopeType = "System" | "Team" | "Portfolio";

export interface IApiKeyScope {
	role: ApiKeyScopeRole;
	scopeType: ApiKeyScopeType;
	scopeId: number | null;
}

export interface ICreateApiKeyRequest {
	name: string;
	description?: string;
	scope?: IApiKeyScope[];
}
