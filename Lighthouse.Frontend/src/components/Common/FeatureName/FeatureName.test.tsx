import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { describe, expect } from "vitest";
import type { StateCategory } from "../../../models/Feature";
import { Team } from "../../../models/Team/Team";
import FeatureName from "./FeatureName";

// Mock component for proper rendering with router
const renderWithRouter = (ui: React.ReactElement) => {
	return render(<BrowserRouter>{ui}</BrowserRouter>);
};

describe("FeatureName", () => {
	const defaultProps = {
		name: "Test Feature",
		url: "",
		stateCategory: "Unknown" as StateCategory,
		isUsingDefaultFeatureSize: false,
		teamsWorkIngOnFeature: [],
	};

	test("renders feature name without link when url is empty", () => {
		renderWithRouter(<FeatureName {...defaultProps} />);

		const nameElement = screen.getByText("Test Feature");
		expect(nameElement).toBeInTheDocument();
		expect(nameElement.tagName).not.toBe("A");
	});

	test("renders feature name with link when url is provided", () => {
		renderWithRouter(<FeatureName {...defaultProps} url="/feature/123" />);

		const linkElement = screen.getByText("Test Feature");
		expect(linkElement.closest("a")).toBeInTheDocument();
		expect(linkElement.closest("a")).toHaveAttribute("href", "/feature/123");
	});

	test("displays correct icon for ToDo state", () => {
		renderWithRouter(<FeatureName {...defaultProps} stateCategory="ToDo" />);

		const tooltip = screen.getByLabelText("Feature State: ToDo");
		expect(tooltip).toBeInTheDocument();
	});

	test("displays correct icon for Doing state", () => {
		renderWithRouter(<FeatureName {...defaultProps} stateCategory="Doing" />);

		const tooltip = screen.getByLabelText("Feature State: Doing");
		expect(tooltip).toBeInTheDocument();
	});

	test("displays correct icon for Done state", () => {
		renderWithRouter(<FeatureName {...defaultProps} stateCategory="Done" />);

		const tooltip = screen.getByLabelText("Feature State: Done");
		expect(tooltip).toBeInTheDocument();
	});

	test("displays warning icon when using default feature size", () => {
		renderWithRouter(
			<FeatureName {...defaultProps} isUsingDefaultFeatureSize={true} />,
		);

		const tooltip = screen.getByLabelText(
			"No child items were found for this Feature. The remaining items displayed are based on the default feature size specified in the advanced project settings.",
		);
		expect(tooltip).toBeInTheDocument();
	});

	test("does not display warning icon when not using default feature size", () => {
		renderWithRouter(
			<FeatureName {...defaultProps} isUsingDefaultFeatureSize={false} />,
		);

		const tooltip = screen.queryByLabelText(
			"No child items were found for this Feature. The remaining items displayed are based on the default feature size specified in the advanced project settings.",
		);
		expect(tooltip).not.toBeInTheDocument();
	});

	test("displays team information when teams are working on the feature", () => {
		const teams = [
			new Team(
				"Team A",
				1,
				[],
				[],
				0,
				new Date(),
				false,
				new Date(),
				new Date(),
			),
			new Team(
				"Team B",
				2,
				[],
				[],
				0,
				new Date(),
				false,
				new Date(),
				new Date(),
			),
		];

		renderWithRouter(
			<FeatureName {...defaultProps} teamsWorkIngOnFeature={teams} />,
		);

		// The engineering icon should be present
		const engineeringIcon = document.querySelector(
			'svg[data-testid="EngineeringIcon"]',
		);
		expect(engineeringIcon).toBeInTheDocument();
	});

	test("does not display team information when no teams are working on the feature", () => {
		renderWithRouter(
			<FeatureName {...defaultProps} teamsWorkIngOnFeature={[]} />,
		);

		// The engineering icon should not be present
		const engineeringIcon = document.querySelector(
			'svg[data-testid="EngineeringIcon"]',
		);
		expect(engineeringIcon).not.toBeInTheDocument();
	});
});
