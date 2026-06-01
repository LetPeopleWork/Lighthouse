export type ForecastDataSufficiencyInput = {
	hasRemainingWork: boolean;
	hasSufficientData?: boolean;
};

export function isForecastDataInsufficient({
	hasRemainingWork,
	hasSufficientData,
}: ForecastDataSufficiencyInput): boolean {
	return hasRemainingWork && hasSufficientData === false;
}
