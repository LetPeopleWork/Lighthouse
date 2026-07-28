import { describe, expect, it } from "vitest";
import { cannotBeForecast, cannotForecastReason } from "./cannotForecast";

describe("cannotBeForecast", () => {
	it("is false when every contributing team has throughput", () => {
		expect(cannotBeForecast({ teamsWithoutForecast: [] })).toBe(false);
	});

	it("is false when the backend omitted the field entirely", () => {
		expect(cannotBeForecast({})).toBe(false);
	});

	it("is true as soon as one team cannot be forecast", () => {
		expect(cannotBeForecast({ teamsWithoutForecast: ["Team Pulsar"] })).toBe(
			true,
		);
	});
});

describe("cannotForecastReason", () => {
	it("names a single team in the singular", () => {
		expect(cannotForecastReason(["Team Pulsar"])).toBe(
			"No throughput history for Team Pulsar. Forecast unavailable until that team has data.",
		);
	});

	it("joins several teams and switches to the plural", () => {
		expect(cannotForecastReason(["Team Pulsar", "Team Voyager"])).toBe(
			"No throughput history for Team Pulsar and Team Voyager. Forecast unavailable until those teams have data.",
		);
	});

	it("is empty when there is nothing to explain", () => {
		expect(cannotForecastReason([])).toBe("");
	});
});
