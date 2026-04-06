import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import PredictabilityScoreOverviewWidget from "./PredictabilityScoreOverviewWidget";

describe("PredictabilityScoreOverviewWidget", () => {
	it("renders the score as a percentage", () => {
		render(<PredictabilityScoreOverviewWidget score={0.73} />);
		expect(screen.getByTestId("predictability-score-value")).toHaveTextContent(
			"73%",
		);
	});

	it("renders 0% when score is 0", () => {
		render(<PredictabilityScoreOverviewWidget score={0} />);
		expect(screen.getByTestId("predictability-score-value")).toHaveTextContent(
			"0%",
		);
	});

	it("renders 100% when score is 1", () => {
		render(<PredictabilityScoreOverviewWidget score={1.0} />);
		expect(screen.getByTestId("predictability-score-value")).toHaveTextContent(
			"100%",
		);
	});

	it("renders loading state when score is null", () => {
		render(<PredictabilityScoreOverviewWidget score={null} />);
		expect(
			screen.queryByTestId("predictability-score-value"),
		).not.toBeInTheDocument();
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});

	it("rounds fractional percentages", () => {
		render(<PredictabilityScoreOverviewWidget score={0.456} />);
		expect(screen.getByTestId("predictability-score-value")).toHaveTextContent(
			"46%",
		);
	});

	it("renders title text", () => {
		render(<PredictabilityScoreOverviewWidget score={0.5} />);
		expect(screen.getByText("Predictability Score")).toBeInTheDocument();
	});
});
