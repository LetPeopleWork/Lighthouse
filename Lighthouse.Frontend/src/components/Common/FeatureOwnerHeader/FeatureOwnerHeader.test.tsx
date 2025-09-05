import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import FeatureOwnerHeader from "./FeatureOwnerHeader";

// Mock LocalDateTimeDisplay component since we're just testing the FeatureOwnerHeader component
vi.mock("../LocalDateTimeDisplay/LocalDateTimeDisplay", () => ({
	default: ({ utcDate }: { utcDate: Date; showTime: boolean }) => (
		<span data-testid="mock-date">{utcDate.toISOString()}</span>
	),
}));

describe("FeatureOwnerHeader", () => {
	const mockFeatureOwner: IFeatureOwner = {
		id: 1,
		name: "Test Team",
		lastUpdated: new Date("2025-06-01T12:00:00Z"),
		features: [],
		remainingFeatures: 5,
		tags: ["tag1", "tag2"],
		serviceLevelExpectationProbability: 0.85,
		serviceLevelExpectationRange: 30,
		systemWIPLimit: 0,
	};

	it("renders the feature owner name", () => {
		render(<FeatureOwnerHeader featureOwner={mockFeatureOwner} />);

		expect(screen.getByText("Test Team")).toBeInTheDocument();
	});

	it("renders 'Last Updated on' text", () => {
		render(<FeatureOwnerHeader featureOwner={mockFeatureOwner} />);

		expect(screen.getByText("Last Updated on")).toBeInTheDocument();
	});

	it("passes the lastUpdated date to LocalDateTimeDisplay", () => {
		render(<FeatureOwnerHeader featureOwner={mockFeatureOwner} />);

		const dateElement = screen.getByTestId("mock-date");
		expect(dateElement.textContent).toBe(
			mockFeatureOwner.lastUpdated.toISOString(),
		);
	});

	it("renders with correct heading hierarchy (h3 and h6)", () => {
		render(<FeatureOwnerHeader featureOwner={mockFeatureOwner} />);

		// Check for h3 typography
		const h3Element = screen.getByText("Test Team").closest("h3");
		expect(h3Element).toBeInTheDocument();

		// Check for h6 typography
		const h6Text = screen.getByText("Last Updated on").closest("h6");
		expect(h6Text).toBeInTheDocument();
	});

	it("renders correctly with a different feature owner", () => {
		const anotherOwner: IFeatureOwner = {
			...mockFeatureOwner,
			id: 2,
			name: "Project Alpha",
			lastUpdated: new Date("2025-05-15T08:30:00Z"),
		};

		render(<FeatureOwnerHeader featureOwner={anotherOwner} />);

		expect(screen.getByText("Project Alpha")).toBeInTheDocument();
		const dateElement = screen.getByTestId("mock-date");
		expect(dateElement.textContent).toBe(
			anotherOwner.lastUpdated.toISOString(),
		);
	});
});
