import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import { testTheme } from "../../../tests/testTheme";
import SystemWIPLimitDisplay from "./SystemWipLimitDisplay";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

describe("SystemWIPLimitDisplay", () => {
	const createMockFeatureOwner = (systemWIPLimit: number): IFeatureOwner => ({
		name: "Test Owner",
		id: 1,
		lastUpdated: new Date(),
		features: [],
		remainingFeatures: 0,
		tags: [],
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit,
		remainingWork: 0,
		totalWork: 0,
	});

	it("should not render when hide prop is true", () => {
		const featureOwner = createMockFeatureOwner(5);
		const { container } = render(
			<SystemWIPLimitDisplay featureOwner={featureOwner} hide={true} />,
		);

		expect(container.firstChild).toBeNull();
	});

	it("should not render when systemWIPLimit is 0", () => {
		const featureOwner = createMockFeatureOwner(0);
		const { container } = render(
			<SystemWIPLimitDisplay featureOwner={featureOwner} />,
		);

		expect(container.firstChild).toBeNull();
	});

	it("should not render when systemWIPLimit is negative", () => {
		const featureOwner = createMockFeatureOwner(-1);
		const { container } = render(
			<SystemWIPLimitDisplay featureOwner={featureOwner} />,
		);

		expect(container.firstChild).toBeNull();
	});

	it("should render with correct WIP limit when systemWIPLimit is 1", () => {
		const featureOwner = createMockFeatureOwner(1);
		render(<SystemWIPLimitDisplay featureOwner={featureOwner} />);

		// Check that the icon button with tooltip is rendered
		const iconButton = screen.getByRole("button");
		expect(iconButton).toBeInTheDocument();
		expect(iconButton).toHaveAttribute(
			"aria-label",
			"System WIP Limit: 1 Work Item",
		);

		// Check that the WorkIcon is present
		expect(screen.getByTestId("WorkIcon")).toBeInTheDocument();
	});

	it("should render with correct WIP limit when systemWIPLimit is greater than 1", () => {
		const featureOwner = createMockFeatureOwner(5);
		render(<SystemWIPLimitDisplay featureOwner={featureOwner} />);

		// Check that the icon button with tooltip is rendered
		const iconButton = screen.getByRole("button");
		expect(iconButton).toBeInTheDocument();
		expect(iconButton).toHaveAttribute(
			"aria-label",
			"System WIP Limit: 5 Work Items",
		);

		// Check that the WorkIcon is present
		expect(screen.getByTestId("WorkIcon")).toBeInTheDocument();
	});

	it("should use default hide value when not provided", () => {
		const featureOwner = createMockFeatureOwner(5);
		render(<SystemWIPLimitDisplay featureOwner={featureOwner} />);

		// Check that the icon button with tooltip is rendered
		const iconButton = screen.getByRole("button");
		expect(iconButton).toBeInTheDocument();
		expect(iconButton).toHaveAttribute(
			"aria-label",
			"System WIP Limit: 5 Work Items",
		);
	});
});
