import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { vi } from "vitest";
import type { IDelivery } from "../../../../../models/Delivery";
import type { IFeature } from "../../../../../models/Feature";
import type { IPortfolio } from "../../../../../models/Portfolio/Portfolio";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import { DeliveryCreateModal } from "./DeliveryCreateModal";

// Mock the TerminologyContext to avoid dependency issues
vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			switch (key) {
				case "delivery":
					return "Milestone";
				case "feature":
					return "Feature";
				case "features":
					return "Features";
				default:
					return key;
			}
		},
	}),
}));

// Mock the feature service
const mockFeatureService = createMockFeatureService();
mockFeatureService.getFeaturesByIds = vi.fn().mockResolvedValue([
	{
		id: 1,
		name: "Test Feature 1",
		stateCategory: "ToDo" as const,
		state: "New",
	},
	{
		id: 2,
		name: "Test Feature 2",
		stateCategory: "Doing" as const,
		state: "In Progress",
	},
]);

const mockApiContext = createMockApiServiceContext({
	featureService: mockFeatureService,
});

describe("DeliveryCreateModal - Edit Mode", () => {
	const mockPortfolio: IPortfolio = {
		id: 1,
		name: "Test Portfolio",
		features: [{ id: 1 }, { id: 2 }] as IFeature[],
		involvedTeams: [],
		tags: [],
		totalWorkItems: 0,
		remainingWorkItems: 0,
		forecasts: [],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		remainingFeatures: 0,
	};

	const mockEditingDelivery: IDelivery = {
		id: 10,
		name: "Existing Milestone",
		date: "2025-12-25T00:00:00Z", // Christmas 2025
		features: [1, 2],
		portfolioId: 1,
		likelihoodPercentage: 85,
		progress: 50,
		remainingWork: 10,
		totalWork: 20,
		featureLikelihoods: [],
	};

	const renderModal = (
		editingDelivery?: IDelivery,
		onSave = vi.fn(),
		onUpdate = vi.fn(),
	) => {
		return render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<DeliveryCreateModal
					open={true}
					portfolio={mockPortfolio}
					editingDelivery={editingDelivery}
					onClose={vi.fn()}
					onSave={onSave}
					onUpdate={onUpdate}
				/>
			</ApiServiceContext.Provider>,
		);
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should show 'Edit Milestone' title when editingDelivery is provided", async () => {
		renderModal(mockEditingDelivery);

		await waitFor(() => {
			expect(screen.getByText("Edit Milestone")).toBeInTheDocument();
		});
	});

	it("should show 'Add Milestone' title when no editingDelivery is provided", async () => {
		renderModal();

		await waitFor(() => {
			expect(screen.getByText("Add Milestone")).toBeInTheDocument();
		});
	});

	it("should pre-populate form fields when editing", async () => {
		renderModal(mockEditingDelivery);

		await waitFor(() => {
			const nameInput = screen.getByDisplayValue("Existing Milestone");
			const dateInput = screen.getByDisplayValue("2025-12-25");

			expect(nameInput).toBeInTheDocument();
			expect(dateInput).toBeInTheDocument();
		});
	});

	it("should show 'Update' button text when editing", async () => {
		renderModal(mockEditingDelivery);

		await waitFor(() => {
			expect(screen.getByText("Update")).toBeInTheDocument();
		});
	});

	it("should show 'Save' button text when creating", async () => {
		renderModal();

		await waitFor(() => {
			expect(screen.getByText("Save")).toBeInTheDocument();
		});
	});

	it("should call onUpdate with correct data when editing and form is valid", async () => {
		const mockOnUpdate = vi.fn();
		renderModal(mockEditingDelivery, vi.fn(), mockOnUpdate);

		await waitFor(() => {
			// Features should be loaded and checkboxes available
			expect(screen.getByText("Test Feature 1")).toBeInTheDocument();
		});

		// Modify the date to tomorrow to ensure it's in the future
		const dateInput = screen.getByDisplayValue("2025-12-25");
		fireEvent.change(dateInput, { target: { value: "2025-12-26" } });

		// Click Update button
		const updateButton = screen.getByText("Update");
		fireEvent.click(updateButton);

		await waitFor(() => {
			expect(mockOnUpdate).toHaveBeenCalledWith({
				id: 10,
				name: "Existing Milestone",
				date: "2025-12-26",
				featureIds: [1, 2],
			});
		});
	});

	it("should call onSave when creating new delivery", async () => {
		const mockOnSave = vi.fn();
		renderModal(undefined, mockOnSave);

		await waitFor(() => {
			expect(screen.getByText("Test Feature 1")).toBeInTheDocument();
		});

		// Fill in form
		const nameInput = screen.getByLabelText("Milestone Name");
		fireEvent.change(nameInput, { target: { value: "New Milestone" } });

		const dateInput = screen.getByLabelText("Milestone Date");
		fireEvent.change(dateInput, { target: { value: "2025-12-30" } });

		// Select a feature
		const feature1Checkbox = screen.getByLabelText("Test Feature 1");
		fireEvent.click(feature1Checkbox);

		// Click Save button
		const saveButton = screen.getByText("Save");
		fireEvent.click(saveButton);

		await waitFor(() => {
			expect(mockOnSave).toHaveBeenCalledWith({
				name: "New Milestone",
				date: "2025-12-30",
				featureIds: [1],
			});
		});
	});

	it("should preserve feature selection when editing", async () => {
		renderModal(mockEditingDelivery);

		await waitFor(() => {
			// Both features should be selected since they're in mockEditingDelivery.features
			const feature1Checkbox = screen.getByLabelText(
				"Test Feature 1",
			) as HTMLInputElement;
			const feature2Checkbox = screen.getByLabelText(
				"Test Feature 2",
			) as HTMLInputElement;

			expect(feature1Checkbox.checked).toBe(true);
			expect(feature2Checkbox.checked).toBe(true);
		});
	});
});
