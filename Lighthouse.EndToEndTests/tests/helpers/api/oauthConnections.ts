import type { APIRequestContext } from "@playwright/test";

type WorkTrackingSystemOption = {
	key: string;
	value: string;
	isSecret: boolean;
	isOptional?: boolean;
};

export async function createOAuthJiraConnection(
	api: APIRequestContext,
	connectionName: string,
	options: { clientId?: string; clientSecret?: string } = {},
): Promise<{ id: number; name: string }> {
	const clientId = options.clientId ?? "test-jira-client";
	const clientSecret = options.clientSecret ?? "test-jira-secret";

	const connectionOptions: WorkTrackingSystemOption[] = [
		{ key: "ClientId", value: clientId, isSecret: false },
		{ key: "ClientSecret", value: clientSecret, isSecret: true },
	];

	const response = await api.post("/api/latest/worktrackingsystemconnections", {
		data: {
			id: 0,
			name: connectionName,
			workTrackingSystem: "Jira",
			authenticationMethodKey: "jira.oauth",
			options: connectionOptions,
			additionalFieldDefinitions: [],
		},
	});

	return response.json();
}

export async function initiateOAuthConnect(
	api: APIRequestContext,
	providerKey: string,
	connectionId: number,
): Promise<{ status: number; authorizationUrl?: string; body: string }> {
	const response = await api.post(`/api/oauth/${providerKey}/connect`, {
		data: { connectionId },
	});

	const body = await response.text();
	let authorizationUrl: string | undefined;
	if (response.ok()) {
		try {
			const parsed = JSON.parse(body);
			authorizationUrl = parsed.authorizationUrl;
		} catch {
			authorizationUrl = undefined;
		}
	}

	return { status: response.status(), authorizationUrl, body };
}

export async function callOAuthCallback(
	api: APIRequestContext,
	authorizationUrl: string,
): Promise<{ status: number; location: string | null }> {
	const url = new URL(authorizationUrl);
	const callbackPath = `${url.pathname}${url.search}`;

	const response = await api.get(callbackPath, { maxRedirects: 0 });
	const location = response.headers().location ?? null;
	return { status: response.status(), location };
}
