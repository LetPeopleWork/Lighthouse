import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import ServiceLevelExpectation from "./ServiceLevelExpectation";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => ({
			palette: {
				primary: {
					main: "#1976d2",
					light: "#42a5f5",
					contrastText: "#ffffff",
				},
				mode: "light",
			},
		}),
	};
});

describe("ServiceLevelExpectation component", () => {
	// Create a mock featureOwner with valid SLE values
	const createMockFeatureOwner = (
		probability: number,
		range: number,
	): IFeatureOwner => ({
		name: "Test Team",
		id: 1,
		lastUpdated: new Date(),
		features: [],
		remainingFeatures: 0,
		tags: [],
		serviceLevelExpectationProbability: probability,
		serviceLevelExpectationRange: range,
		remainingWork: 0,
		totalWork: 0,
	});

	it("renders service level expectation with correct values", () => {
		const mockOwner = createMockFeatureOwner(85, 14);

		render(<ServiceLevelExpectation featureOwner={mockOwner} />);

		// Check that the component title is rendered
		expect(screen.getByText("Service Level Expectation")).toBeInTheDocument();

		// Check that the formatted text displays with correct values
		// 85% rounds to 85, 14 days remains as 14
		expect(
			screen.getByText("85% of items within 14 days or less"),
		).toBeInTheDocument();
	});

	it("rounds probability percentage correctly", () => {
		const mockOwner = createMockFeatureOwner(85.7, 10);

		render(<ServiceLevelExpectation featureOwner={mockOwner} />);

		// 85.7% should round to 86%
		expect(
			screen.getByText("86% of items within 10 days or less"),
		).toBeInTheDocument();
	});

	it("returns null when probability is missing", () => {
		const mockOwner = createMockFeatureOwner(0, 14);
		const { container } = render(
			<ServiceLevelExpectation featureOwner={mockOwner} />,
		);

		// Container should be empty as component returns null
		expect(container).toBeEmptyDOMElement();
	});

	it("returns null when range is missing", () => {
		const mockOwner = {
			...createMockFeatureOwner(85, 0),
			serviceLevelExpectationRange: 0,
		};
		const { container } = render(
			<ServiceLevelExpectation featureOwner={mockOwner} />,
		);

		// Container should be empty as component returns null
		expect(container).toBeEmptyDOMElement();
	});

	it("returns null when probability is negative", () => {
		const mockOwner = {
			...createMockFeatureOwner(0, 14),
			serviceLevelExpectationProbability: -10,
		};
		const { container } = render(
			<ServiceLevelExpectation featureOwner={mockOwner} />,
		);

		// Container should be empty as component returns null
		expect(container).toBeEmptyDOMElement();
	});

	it("returns null when range is negative", () => {
		const mockOwner = {
			...createMockFeatureOwner(85, 0),
			serviceLevelExpectationRange: -5,
		};
		const { container } = render(
			<ServiceLevelExpectation featureOwner={mockOwner} />,
		);

		// Container should be empty as component returns null
		expect(container).toBeEmptyDOMElement();
	});
});
