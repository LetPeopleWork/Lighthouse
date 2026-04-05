import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import PredictabilityScoreWidget from "./PredictabilityScoreWidget";

describe("PredictabilityScoreWidget", () => {
	it("renders the score as a percentage", () => {
		render(<PredictabilityScoreWidget score={0.73} />);
		expect(screen.getByTestId("predictability-score-value")).toHaveTextContent(
			"73%",
		);
	});

	it("renders 0% when score is 0", () => {
		render(<PredictabilityScoreWidget score={0} />);
		expect(screen.getByTestId("predictability-score-value")).toHaveTextContent(
			"0%",
		);
	});

	it("renders 100% when score is 1", () => {
		render(<PredictabilityScoreWidget score={1.0} />);
		expect(screen.getByTestId("predictability-score-value")).toHaveTextContent(
			"100%",
		);
	});

	it("renders loading state when score is null", () => {
		render(<PredictabilityScoreWidget score={null} />);
		expect(
			screen.queryByTestId("predictability-score-value"),
		).not.toBeInTheDocument();
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});

	it("rounds fractional percentages", () => {
		render(<PredictabilityScoreWidget score={0.456} />);
		expect(screen.getByTestId("predictability-score-value")).toHaveTextContent(
			"46%",
		);
	});

	it("renders title text", () => {
		render(<PredictabilityScoreWidget score={0.5} />);
		expect(screen.getByText("Predictability Score")).toBeInTheDocument();
	});
});
