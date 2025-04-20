import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";
import { ForecastLevel } from "./ForecastLevel";
import ForecastLikelihood from "./ForecastLikelihood";

vi.mock("../LocalDateTimeDisplay/LocalDateTimeDisplay", () => ({
	default: ({ utcDate }: { utcDate: Date }) => (
		<span data-testid="local-date-time-display">{utcDate.toISOString()}</span>
	),
}));

const colorToRGB = (colorName: string) => {
	const colors: { [key: string]: string } = {
		[riskyColor]: "rgb(255, 0, 0)",
		[realisticColor]: "rgb(255, 165, 0)",
		[confidentColor]: "rgb(144, 238, 144)",
		[certainColor]: "rgb(0, 128, 0)",
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
