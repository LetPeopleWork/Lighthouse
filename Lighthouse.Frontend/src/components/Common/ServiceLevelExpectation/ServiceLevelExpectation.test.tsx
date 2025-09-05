import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
// Mock the Material-UI theme
import { testTheme } from "../../../tests/testTheme";
import ServiceLevelExpectation from "./ServiceLevelExpectation";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
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
		systemWIPLimit: 0,
	});

	it("renders service level expectation with correct values", () => {
		const mockOwner = createMockFeatureOwner(85, 14);

		render(<ServiceLevelExpectation featureOwner={mockOwner} />);

		// Check that the icon button with tooltip is rendered
		const iconButton = screen.getByRole("button");
		expect(iconButton).toBeInTheDocument();
		expect(iconButton).toHaveAttribute(
			"aria-label",
			"Service Level Expectation: 85% of Work Items within 14 days or less",
		);

		// Check that the SpeedIcon is present
		expect(screen.getByTestId("SpeedIcon")).toBeInTheDocument();
	});

	it("rounds probability percentage correctly", () => {
		const mockOwner = createMockFeatureOwner(85.7, 10);

		render(<ServiceLevelExpectation featureOwner={mockOwner} />);

		// 85.7% should round to 86%
		const iconButton = screen.getByRole("button");
		expect(iconButton).toHaveAttribute(
			"aria-label",
			"Service Level Expectation: 86% of Work Items within 10 days or less",
		);
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

	it("returns null when hide prop is true, regardless of valid SLE values", () => {
		const mockOwner = createMockFeatureOwner(85, 14);
		const { container } = render(
			<ServiceLevelExpectation featureOwner={mockOwner} hide={true} />,
		);

		// Container should be empty as component returns null when hide is true
		expect(container).toBeEmptyDOMElement();
	});
});
