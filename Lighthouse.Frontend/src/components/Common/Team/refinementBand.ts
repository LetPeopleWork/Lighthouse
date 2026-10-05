import type { IRefinementBandSetting } from "../../../models/Refinement/Refinement";

export const LOWEST_LIKELIHOOD = 50;
export const HIGHEST_LIKELIHOOD = 95;

// What the server reads the Team's forecast at until the admin chooses otherwise.
export const DEFAULT_BAND: IRefinementBandSetting = {
	lowPercentile: 50,
	highPercentile: 85,
};

const OUT_OF_RANGE_ERROR = `Between ${LOWEST_LIKELIHOOD}% and ${HIGHEST_LIKELIHOOD}%.`;

type BandErrors = Record<keyof IRefinementBandSetting, string | null>;

const isInRange = (likelihood: number): boolean =>
	Number.isInteger(likelihood) &&
	likelihood >= LOWEST_LIKELIHOOD &&
	likelihood <= HIGHEST_LIKELIHOOD;

/** Both ends are valid likelihoods, but the low end is not below the high end. */
export const isBandInverted = ({
	lowPercentile,
	highPercentile,
}: IRefinementBandSetting): boolean =>
	isInRange(lowPercentile) &&
	isInRange(highPercentile) &&
	lowPercentile >= highPercentile;

// The same rule the server enforces, so a save it would refuse is never sent.
export const bandErrors = (band: IRefinementBandSetting): BandErrors => {
	const { lowPercentile, highPercentile } = band;
	const highInRange = isInRange(highPercentile);
	let lowError: string | null = null;
	if (!isInRange(lowPercentile)) {
		lowError = OUT_OF_RANGE_ERROR;
	} else if (isBandInverted(band)) {
		lowError = `The low end (${lowPercentile}%) must be below the high end (${highPercentile}%).`;
	}
	return {
		lowPercentile: lowError,
		highPercentile: highInRange ? null : OUT_OF_RANGE_ERROR,
	};
};

export const hasBandErrors = (band: IRefinementBandSetting): boolean =>
	Object.values(bandErrors(band)).some((error) => error !== null);
