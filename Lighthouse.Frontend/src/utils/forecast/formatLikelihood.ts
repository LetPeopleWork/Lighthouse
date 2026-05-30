export type LikelihoodPrecision = "round" | "fixed2";

export type FormatLikelihoodOptions = {
	hasRemainingWork: boolean;
	precision: LikelihoodPrecision;
};

const CERTAINTY_CAP_THRESHOLD = 95;

export function formatLikelihood(
	value: number,
	options: FormatLikelihoodOptions,
): string {
	if (value > CERTAINTY_CAP_THRESHOLD && options.hasRemainingWork) {
		return `>${CERTAINTY_CAP_THRESHOLD}%`;
	}

	if (options.precision === "round") {
		return `${Math.round(value)}%`;
	}

	return `${value.toFixed(2)}%`;
}
