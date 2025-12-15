import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import { Feature } from "../../../../../models/Feature";
import DeliverySection from "./DeliverySection";

// Mock dependencies
vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => key === 'feature' ? 'Feature' : key,
	}),
}));

describe("DeliverySection", () => {
	const mockDelivery = new Delivery();
	mockDelivery.id = 1;
	mockDelivery.name = "Test Delivery";
	mockDelivery.date = new Date("2025-01-31").toISOString();
	mockDelivery.features = [{ id: 1, name: "Test Feature" }];
	mockDelivery.likelihoodPercentage = 75;

	const mockFeature = new Feature();
	mockFeature.id = 1;
	mockFeature.name = "Test Feature";
	mockFeature.remainingWork = { "1": 5 };
	mockFeature.totalWork = { "1": 10 };

	const mockProps = {
		delivery: mockDelivery,
		features: [mockFeature],
		isExpanded: false,
		isLoadingFeatures: false,
		onToggleExpanded: vi.fn(),
		onDelete: vi.fn(),
	};

	it("should render delivery section with correct information", () => {
		render(<DeliverySection {...mockProps} />);

		expect(screen.getByText("Test Delivery")).toBeInTheDocument();
		expect(screen.getByText("75%")).toBeInTheDocument();
		expect(screen.getByText(/1 Feature/i)).toBeInTheDocument();
	});

	it("should show loading state when features are being loaded", () => {
		const loadingProps = {
			...mockProps,
			isExpanded: true,
			isLoadingFeatures: true,
		};

		render(<DeliverySection {...loadingProps} />);

		expect(screen.getByText("Loading features...")).toBeInTheDocument();
	});

	it("should show empty state when no features", () => {
		const emptyProps = {
			...mockProps,
			features: [],
			isExpanded: true,
		};

		render(<DeliverySection {...emptyProps} />);

		expect(screen.getByText("No features in this delivery.")).toBeInTheDocument();
	});
});