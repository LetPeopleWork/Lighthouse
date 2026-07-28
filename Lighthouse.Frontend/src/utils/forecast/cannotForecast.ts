export const CANNOT_FORECAST_SHORT = "Cannot forecast";

export type CannotForecastInput = {
	teamsWithoutForecast?: string[];
};

/**
 * A feature is done only when every contributing team is done, so a team with no throughput history
 * leaves no honest distribution to forecast from (ADR-112). This outranks the insufficient-data
 * signal: that one says the forecast rests on thin history, this one says there is no forecast.
 */
export function cannotBeForecast({
	teamsWithoutForecast,
}: CannotForecastInput): boolean {
	return (teamsWithoutForecast?.length ?? 0) > 0;
}

export function cannotForecastReason(teamsWithoutForecast: string[]): string {
	if (teamsWithoutForecast.length === 0) {
		return "";
	}

	const teams = new Intl.ListFormat("en", {
		style: "long",
		type: "conjunction",
	}).format(teamsWithoutForecast);

	const verb = teamsWithoutForecast.length === 1 ? "has" : "have";

	return `No throughput history for ${teams}. Forecast unavailable until ${teamsWithoutForecast.length === 1 ? "that team" : "those teams"} ${verb} data.`;
}
