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

		expect(screen.getByText("75.12%")).toBeInTheDocument();
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

	describe("never presents a manual forecast above 95% as a certainty", () => {
		it.each([
			{ scenario: "a perfect likelihood with work left", likelihood: 100 },
			{ scenario: "a high likelihood with work left", likelihood: 96.5 },
		])("reads >95% for $scenario", ({ likelihood: computedLikelihood }) => {
			render(
				<ForecastLikelihood
					remainingItems={5}
					targetDate={when}
					likelihood={computedLikelihood}
				/>,
			);

			expect(screen.getByText(">95%")).toBeInTheDocument();
			expect(screen.queryByText("100.00%")).not.toBeInTheDocument();
			expect(screen.queryByText("96.50%")).not.toBeInTheDocument();
		});

		it.each([
			{ likelihood: 95, expected: "95.00%" },
			{ likelihood: 94.8, expected: "94.80%" },
		])(
			"keeps the precise $expected for a likelihood of $likelihood with work left",
			({ likelihood: computedLikelihood, expected }) => {
				render(
					<ForecastLikelihood
						remainingItems={5}
						targetDate={when}
						likelihood={computedLikelihood}
					/>,
				);

				expect(screen.getByText(`${expected}`)).toBeInTheDocument();
				expect(screen.queryByText(">95%")).not.toBeInTheDocument();
			},
		);

		it("still reads 100.00% for a completed forecast with no work left", () => {
			render(
				<ForecastLikelihood
					remainingItems={0}
					targetDate={when}
					likelihood={100}
				/>,
			);

			expect(screen.getByText("100.00%")).toBeInTheDocument();
			expect(screen.queryByText(">95%")).not.toBeInTheDocument();
		});
	});

	describe("suppresses a manual forecast built on too little throughput data", () => {
		it("shows a not-enough-data message instead of a likelihood when the team lacks throughput history", () => {
			render(
				<ForecastLikelihood
					remainingItems={5}
					targetDate={when}
					likelihood={100}
					hasSufficientData={false}
				/>,
			);

			expect(screen.getByText(/not enough.*data/i)).toBeInTheDocument();
			expect(screen.queryByText("100.00%")).not.toBeInTheDocument();
			expect(screen.queryByText(">95%")).not.toBeInTheDocument();
		});

		it("renders the likelihood as usual when the team has enough throughput data", () => {
			render(
				<ForecastLikelihood
					remainingItems={5}
					targetDate={when}
					likelihood={70}
					hasSufficientData={true}
				/>,
			);

			expect(screen.getByText("70.00%")).toBeInTheDocument();
			expect(screen.queryByText(/not enough.*data/i)).not.toBeInTheDocument();
		});

		it("does not suppress a completed forecast with no remaining work even when flagged insufficient", () => {
			render(
				<ForecastLikelihood
					remainingItems={0}
					targetDate={when}
					likelihood={100}
					hasSufficientData={false}
				/>,
			);

			expect(screen.getByText("100.00%")).toBeInTheDocument();
			expect(screen.queryByText(/not enough.*data/i)).not.toBeInTheDocument();
		});
	});
});
