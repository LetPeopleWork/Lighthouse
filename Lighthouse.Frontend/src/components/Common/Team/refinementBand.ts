import type { IRefinementBandSetting } from "../../../models/Refinement/Refinement";

const LOWEST_LIKELIHOOD = 50;
const HIGHEST_LIKELIHOOD = 95;

export const OUT_OF_RANGE_ERROR = `Between ${LOWEST_LIKELIHOOD}% and ${HIGHEST_LIKELIHOOD}%.`;

type BandErrors = Record<keyof IRefinementBandSetting, string | null>;

const isInRange = (likelihood: number): boolean =>
	Number.isInteger(likelihood) &&
	likelihood >= LOWEST_LIKELIHOOD &&
	likelihood <= HIGHEST_LIKELIHOOD;

// The same rule the server enforces, so a save it would refuse is never sent.
export const bandErrors = ({
	lowPercentile,
	highPercentile,
}: IRefinementBandSetting): BandErrors => {
	const lowInRange = isInRange(lowPercentile);
	const highInRange = isInRange(highPercentile);
	const inverted = lowInRange && highInRange && lowPercentile >= highPercentile;
	let lowError: string | null = null;
	if (!lowInRange) {
		lowError = OUT_OF_RANGE_ERROR;
	} else if (inverted) {
		lowError = `The low end (${lowPercentile}%) must be below the high end (${highPercentile}%).`;
	}
	return {
		lowPercentile: lowError,
		highPercentile: highInRange ? null : OUT_OF_RANGE_ERROR,
	};
};

export const hasBandErrors = (band: IRefinementBandSetting): boolean =>
	Object.values(bandErrors(band)).some((error) => error !== null);
