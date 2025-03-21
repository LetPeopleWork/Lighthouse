import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ForecastLevel } from "./ForecastLevel";
import ForecastLikelihood from "./ForecastLikelihood";

vi.mock("../LocalDateTimeDisplay/LocalDateTimeDisplay", () => ({
	default: ({ utcDate }: { utcDate: Date }) => (
		<span data-testid="local-date-time-display">{utcDate.toISOString()}</span>
	),
}));

const colorToRGB = (colorName: string) => {
	const colors: { [key: string]: string } = {
		red: "rgb(255, 0, 0)",
		orange: "rgb(255, 165, 0)",
		lightgreen: "rgb(144, 238, 144)",
		green: "rgb(0, 128, 0)",
	};
	return colors[colorName];
};

describe("ForecastLikelihood component", () => {
	const howMany = 10;
	const when = new Date("2025-01-01");
	const likelihood = 75.1234;

	it("should render the likelihood percentage correctly", () => {
		render(
			<ForecastLikelihood
				remainingItems={howMany}
				targetDate={when}
				likelihood={likelihood}
			/>,
		);
		const formattedLikelihood = likelihood.toFixed(2);

		expect(screen.getByText(`${formattedLikelihood}%`)).toBeInTheDocument();
	});

	it("should render the correct icon and color based on the likelihood", () => {
		const forecastLevel = new ForecastLevel(likelihood);
		render(
			<ForecastLikelihood
				remainingItems={howMany}
				targetDate={when}
				likelihood={likelihood}
			/>,
		);

		const iconElement = screen.getByTestId("forecast-level-icon");
		expect(iconElement).toBeInTheDocument();
		expect(iconElement).toHaveStyle(
			`color: ${colorToRGB(forecastLevel.color)}`,
		);
	});
});
