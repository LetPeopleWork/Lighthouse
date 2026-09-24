import { UsageDataOptionalFeature } from "../../models/UsageData/UsageData";

/**
 * The behaviour settings usage data has a name for, keyed by the name the product stores them under.
 *
 * Only settings somebody decided to report are here. Everything else - the setting that stops usage
 * data, and any setting added later - has no entry and is never reported, because the server refuses
 * a whole batch over one name it does not know.
 *
 * Built without a prototype so that a stored key such as "constructor" or "toString" finds nothing,
 * rather than something every object inherits.
 */
const namedForUsageData: Readonly<Record<string, UsageDataOptionalFeature>> =
	Object.assign(Object.create(null), {
		FeatureOrdering: UsageDataOptionalFeature.FeatureOrder,
	});

export const usageDataOptionalFeatureFor = (
	key: string,
): UsageDataOptionalFeature | undefined => namedForUsageData[key];
