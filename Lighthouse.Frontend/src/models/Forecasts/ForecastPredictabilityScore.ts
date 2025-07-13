import type { IPercentileValue } from "../PercentileValue";

export interface IForecastPredictabilityScore {
	predictabilityScore: number;
	percentiles: IPercentileValue[];
	forecastResults: Map<number, number>;
}

export class ForecastPredictabilityScore
	implements IForecastPredictabilityScore
{
	percentiles: IPercentileValue[];
	predictabilityScore: number;
	forecastResults: Map<number, number>;

	constructor(
		percentiles: IPercentileValue[],
		predictabilityScore: number,
		forecastResults: Map<number, number>,
	) {
		this.predictabilityScore = predictabilityScore;
		this.forecastResults = forecastResults;
		this.percentiles = percentiles;
	}
}
