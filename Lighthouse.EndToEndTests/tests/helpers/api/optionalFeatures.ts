import type { APIRequestContext } from "@playwright/test";

const OVER_TIME_HISTORY_FILL_KEY = "OverTimeHistoryFill";
const FEATURE_ORDERING_KEY = "FeatureOrdering";

type OptionalFeature = {
	id: number;
	key: string;
	enabled: boolean;
};

async function switchOptionalFeature(
	request: APIRequestContext,
	key: string,
	enabled: boolean,
): Promise<void> {
	const current = await request.get(`/api/latest/optionalfeatures/${key}`);
	if (!current.ok()) {
		throw new Error(
			`Failed to read optional feature ${key}: ${current.status()}`,
		);
	}

	// The update binds a whole feature, so the row goes back as it came with only the switch changed.
	const feature = (await current.json()) as OptionalFeature;
	const response = await request.post(`/api/latest/optionalfeatures/${key}`, {
		data: { ...feature, enabled },
	});
	if (!response.ok()) {
		throw new Error(
			`Failed to switch optional feature ${key} to ${enabled}: ${response.status()}`,
		);
	}
}

/**
 * Filling in past over-time days is on by default, but a spec that expects a populated
 * over-time chart switches it on explicitly so it does not depend on the instance's state,
 * and switches it off afterwards so later specs never meet a background fill they did not
 * ask for.
 */
export async function switchHistoryFill(
	request: APIRequestContext,
	enabled: boolean,
): Promise<void> {
	await switchOptionalFeature(request, OVER_TIME_HISTORY_FILL_KEY, enabled);
}

/**
 * Letting this instance own the order of Features ships switched off, and the switch outlives the
 * spec that flips it. A spec that hands the order over should start from off, so it really clicks
 * the switch, and switch it back off afterwards so the next spec meets the shipped default.
 */
export async function switchFeatureOrdering(
	request: APIRequestContext,
	enabled: boolean,
): Promise<void> {
	await switchOptionalFeature(request, FEATURE_ORDERING_KEY, enabled);
}
