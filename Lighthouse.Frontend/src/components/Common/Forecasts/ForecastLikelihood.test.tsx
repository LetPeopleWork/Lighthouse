import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ForecastLikelihood from "./ForecastLikelihood";

vi.mock("../LocalDateTimeDisplay/LocalDateTimeDisplay", () => ({
	default: ({ utcDate }: { utcDate: Date }) => (
		<span data-testid="local-date-time-display">{utcDate.toISOString()}</span>
	),
}));

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
		render(
			<ForecastLikelihood
				remainingItems={howMany}
				targetDate={when}
				likelihood={likelihood}
			/>,
		);

		const iconElement = screen.getByTestId("forecast-level-icon");
		expect(iconElement).toBeInTheDocument();
		expect(iconElement).toHaveStyle("color: rgb(76, 175, 80)");
	});
});
