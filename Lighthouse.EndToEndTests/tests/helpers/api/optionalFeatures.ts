import type { APIRequestContext } from "@playwright/test";

const FEATURE_ORDERING_KEY = "FeatureOrdering";

type OptionalFeature = {
	id: number;
	key: string;
	enabled: boolean;
};

/**
 * Letting this instance own the order of Features ships switched off, and the switch outlives the
 * spec that flips it. A spec that hands the order over should start from off, so it really clicks
 * the switch, and switch it back off afterwards so the next spec meets the shipped default.
 */
export async function switchFeatureOrdering(
	request: APIRequestContext,
	enabled: boolean,
): Promise<void> {
	const url = `/api/latest/optionalfeatures/${FEATURE_ORDERING_KEY}`;

	const current = await request.get(url);
	if (!current.ok()) {
		throw new Error(
			`Failed to read optional feature ${FEATURE_ORDERING_KEY}: ${current.status()}`,
		);
	}

	// The update binds a whole feature, so the row goes back as it came with only the switch changed.
	const feature = (await current.json()) as OptionalFeature;
	const response = await request.post(url, {
		data: { ...feature, enabled },
	});
	if (!response.ok()) {
		throw new Error(
			`Failed to switch optional feature ${FEATURE_ORDERING_KEY} to ${enabled}: ${response.status()}`,
		);
	}
}
