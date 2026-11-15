import type { APIRequestContext } from "@playwright/test";

const EVERYONE_SIGNED_IN_URL = "/api/latest/authorization/everyone-signed-in";
const GROUP_MAPPINGS_URL = "/api/latest/authorization/group-mappings";

type EveryoneSignedIn = {
	everyoneCanView: boolean;
	concurrencyToken: string;
};

type GroupMapping = {
	id: number;
	groupValue: string;
	scopeType: string;
};

/** Switches "Everyone signed in can view all Teams and Portfolios". Needs a System Admin session. */
export async function letEveryoneSignedInView(
	request: APIRequestContext,
	everyoneCanView: boolean,
): Promise<void> {
	const current = await request.get(EVERYONE_SIGNED_IN_URL);
	if (!current.ok()) {
		throw new Error(
			`Failed to read the everyone-signed-in switch: ${current.status()}`,
		);
	}

	const setting = (await current.json()) as EveryoneSignedIn;
	const response = await request.put(EVERYONE_SIGNED_IN_URL, {
		data: { ...setting, everyoneCanView },
	});
	if (!response.ok()) {
		throw new Error(
			`Failed to switch everyone-signed-in to ${everyoneCanView}: ${response.status()}`,
		);
	}
}

/** Adds an SSO group rule that reaches every Team and Portfolio. Needs a System Admin session. */
export async function addRuleForAllUnits(
	request: APIRequestContext,
	groupValue: string,
	role: "Viewer" | "UnitAdmin",
): Promise<void> {
	const response = await request.post(GROUP_MAPPINGS_URL, {
		data: { groupValue, role, scopeType: "AllUnits" },
	});
	if (!response.ok()) {
		throw new Error(
			`Failed to add the rule for all units for ${groupValue}: ${response.status()}`,
		);
	}
}

/** Removes the group's rules for all Teams and Portfolios, so later specs meet none. */
export async function removeRulesForAllUnits(
	request: APIRequestContext,
	groupValue: string,
): Promise<void> {
	const current = await request.get(GROUP_MAPPINGS_URL);
	if (!current.ok()) {
		throw new Error(`Failed to read the group mappings: ${current.status()}`);
	}

	const mappings = (await current.json()) as GroupMapping[];
	for (const mapping of mappings) {
		if (mapping.groupValue === groupValue && mapping.scopeType === "AllUnits") {
			const response = await request.delete(
				`${GROUP_MAPPINGS_URL}/${mapping.id}`,
			);
			if (!response.ok()) {
				throw new Error(
					`Failed to remove group mapping ${mapping.id}: ${response.status()}`,
				);
			}
		}
	}
}
