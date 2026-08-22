import type { IFeatureLikelihood } from "../../models/Delivery";
import { CANNOT_FORECAST_SHORT, cannotBeForecast } from "./cannotForecast";
import { formatLikelihood } from "./formatLikelihood";
import { INSUFFICIENT_FORECAST_DATA_SHORT } from "./insufficientForecastData";
import { isForecastDataInsufficient } from "./isForecastDataInsufficient";

/**
 * What a Feature's chance of landing is called, in one place, because the chip on the screen and the
 * exported file both say it. Two copies would drift a word at a time, and the reader comparing the
 * file against the screen it came from has no way of telling which of the two is wrong.
 */
export function featureLikelihoodLabel(
	featureLikelihood: IFeatureLikelihood,
	hasRemainingWork: boolean,
): string {
	const likelihood = featureLikelihood.likelihoodPercentage;
	const teamsWithoutForecast = featureLikelihood.teamsWithoutForecast ?? [];

	if (likelihood === null || cannotBeForecast({ teamsWithoutForecast })) {
		return CANNOT_FORECAST_SHORT;
	}

	if (
		isForecastDataInsufficient({
			hasRemainingWork,
			hasSufficientData: featureLikelihood.hasSufficientData,
		})
	) {
		return INSUFFICIENT_FORECAST_DATA_SHORT;
	}

	return formatLikelihood(likelihood, {
		hasRemainingWork,
		precision: "round",
	});
}
