import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import DeliverySection from "./DeliverySection";

// Mock dependencies
vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => (key === "feature" ? "Feature" : key),
	}),
}));

describe("DeliverySection", () => {
	const mockDelivery = new Delivery();
	mockDelivery.id = 1;
	mockDelivery.name = "Test Delivery";
	mockDelivery.date = new Date("2025-01-31").toISOString();
	mockDelivery.features = [1]; // Feature IDs
	mockDelivery.likelihoodPercentage = 75;
	mockDelivery.progress = 60;
	mockDelivery.remainingWork = 4;
	mockDelivery.totalWork = 10;
	mockDelivery.featureLikelihoods = [
		{ featureId: 1, likelihoodPercentage: 75 },
	];

	const mockFeature = new Feature();
	mockFeature.id = 1;
	mockFeature.name = "Test Feature";
	mockFeature.remainingWork = { "1": 5 };
	mockFeature.totalWork = { "1": 10 };
	mockFeature.forecasts = [];
	mockFeature.forecasts = [];

	const mockTeams: IEntityReference[] = [
		{ id: 1, name: "Team Alpha" },
		{ id: 2, name: "Team Beta" },
	];

	const mockProps = {
		delivery: mockDelivery,
		features: [mockFeature],
		isExpanded: false,
		isLoadingFeatures: false,
		onToggleExpanded: vi.fn(),
		onDelete: vi.fn(),
		onEdit: vi.fn(),
		teams: mockTeams,
	};

	it("should render delivery section with correct information", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		expect(screen.getByText("Test Delivery")).toBeInTheDocument();
		expect(screen.getByText("Likelihood: 75%")).toBeInTheDocument();
		expect(screen.getByText("Target Date: 1/31/2025")).toBeInTheDocument();
		expect(screen.getByText(/1 Feature/i)).toBeInTheDocument();
	});

	it("should show loading state when features are being loaded", () => {
		const loadingProps = {
			...mockProps,
			isExpanded: true,
			isLoadingFeatures: true,
		};

		render(
			<MemoryRouter>
				<DeliverySection {...loadingProps} />
			</MemoryRouter>,
		);

		expect(screen.getByText("Loading features...")).toBeInTheDocument();
	});

	it("should show empty state when no features", () => {
		const emptyProps = {
			...mockProps,
			features: [],
			isExpanded: true,
		};

		render(
			<MemoryRouter>
				<DeliverySection {...emptyProps} />
			</MemoryRouter>,
		);

		expect(
			screen.getByText("No features in this delivery."),
		).toBeInTheDocument();
	});

	it("should call onEdit when edit button is clicked", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		const editButton = screen.getByLabelText("edit");
		fireEvent.click(editButton);

		expect(mockProps.onEdit).toHaveBeenCalledWith(mockDelivery);
		expect(mockProps.onEdit).toHaveBeenCalledTimes(1);
	});

	it("should call onDelete when delete button is clicked", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		const deleteButton = screen.getByLabelText("delete");
		fireEvent.click(deleteButton);

		expect(mockProps.onDelete).toHaveBeenCalledWith(mockDelivery);
		expect(mockProps.onDelete).toHaveBeenCalledTimes(1);
	});

	it("should have both edit and delete buttons", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		expect(screen.getByLabelText("edit")).toBeInTheDocument();
		expect(screen.getByLabelText("delete")).toBeInTheDocument();
	});

	it("should not call onToggleExpanded when edit button is clicked", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		const editButton = screen.getByLabelText("edit");
		fireEvent.click(editButton);

		// onToggleExpanded should not be called when clicking edit button
		expect(mockProps.onToggleExpanded).not.toHaveBeenCalled();
	});
});
