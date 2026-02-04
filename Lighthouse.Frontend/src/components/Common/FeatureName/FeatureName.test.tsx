import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { describe, expect } from "vitest";
import { Team } from "../../../models/Team/Team";
import type { StateCategory } from "../../../models/WorkItem";
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

	test("displays warning icon when using default feature size", () => {
		renderWithRouter(
			<FeatureName {...defaultProps} isUsingDefaultFeatureSize={true} />,
		);

		const tooltip = screen.getByLabelText(
			"No child Work Items were found for this Feature. The remaining Work Items displayed are based on the default Feature size specified in the advanced project settings.",
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
		const team1 = new Team();
		team1.name = "Team A";
		team1.id = 1;

		const team2 = new Team();
		team2.name = "Team B";
		team2.id = 2;

		const teams = [team1, team2];

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
