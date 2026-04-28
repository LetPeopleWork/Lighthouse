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

export interface ICreateApiKeyRequest {
	name: string;
	description?: string;
}
