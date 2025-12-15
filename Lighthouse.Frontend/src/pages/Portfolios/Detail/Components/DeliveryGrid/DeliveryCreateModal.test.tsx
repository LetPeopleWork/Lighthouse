import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import { DeliveryCreateModal } from "./DeliveryCreateModal";

// Mock the useTerminology hook
vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.DELIVERY]: "Delivery",
				[TERMINOLOGY_KEYS.FEATURES]: "Features",
				[TERMINOLOGY_KEYS.FEATURE]: "Feature",
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

describe("DeliveryCreateModal", () => {
	const mockOnClose = vi.fn();
	const mockOnSave = vi.fn();
	const mockFeatureService = createMockFeatureService();

	const createMockFeature = (
		id: number,
		name: string,
		stateCategory: "ToDo" | "Doing" | "Done",
	): IFeature => {
		const feature: IFeature = {
			id,
			name,
			referenceId: `FTR-${id}`,
			stateCategory,
			state: stateCategory,
			type: "Feature",
			size: 5,
			owningTeam: "Team A",
			lastUpdated: new Date(),
			isUsingDefaultFeatureSize: false,
			parentWorkItemReference: "",
			projects: [],
			remainingWork: {},
			totalWork: {},
			forecasts: [],
			startedDate: new Date(),
			closedDate: new Date(),
			cycleTime: 0,
			workItemAge: 0,
			url: "",
			isBlocked: false,
			getRemainingWorkForFeature: () => 0,
			getRemainingWorkForTeam: () => 0,
			getTotalWorkForFeature: () => 0,
			getTotalWorkForTeam: () => 0,
		};
		return feature;
	};

	const mockPortfolio: Portfolio = {
		id: 1,
		name: "Test Portfolio",
		features: [
			{ id: 1, name: "Feature 1" },
			{ id: 2, name: "Feature 2" },
			{ id: 3, name: "Feature 3" },
		],
		involvedTeams: [],
		tags: [],
		totalWorkItems: 0,
		remainingWorkItems: 0,
		forecasts: [],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		remainingFeatures: 3,
		fromBackend: vi.fn(),
	} as Portfolio;

	const mockFeatures: IFeature[] = [
		createMockFeature(1, "Todo Feature 1", "ToDo"),
		createMockFeature(2, "Doing Feature 1", "Doing"),
		createMockFeature(3, "Done Feature 1", "Done"),
		createMockFeature(4, "Todo Feature 2", "ToDo"),
	];

	beforeEach(() => {
		vi.clearAllMocks();
		mockFeatureService.getFeaturesByIds = vi
			.fn()
			.mockResolvedValue(mockFeatures);
	});

	const renderModal = (open = true) => {
		const mockApiContext = createMockApiServiceContext({
			featureService: mockFeatureService,
		});

		return render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<DeliveryCreateModal
					open={open}
					portfolio={mockPortfolio}
					onClose={mockOnClose}
					onSave={mockOnSave}
				/>
			</ApiServiceContext.Provider>,
		);
	};

	it("should render modal when open is true", async () => {
		renderModal(true);

		// Wait for async feature loading to complete
		await waitFor(() => {
			expect(screen.getByText("Add Delivery")).toBeInTheDocument();
		});

		expect(screen.getByLabelText("Delivery Name")).toBeInTheDocument();
		expect(screen.getByLabelText("Delivery Date")).toBeInTheDocument();
		expect(screen.getByText("Select Features")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Save" })).toBeInTheDocument();
	});

	it("should not render modal when open is false", () => {
		renderModal(false);

		expect(screen.queryByText("Add Delivery")).not.toBeInTheDocument();
	});

	it("should show validation error for empty name", async () => {
		const user = userEvent.setup();
		renderModal();

		const saveButton = screen.getByRole("button", { name: "Save" });
		await user.click(saveButton);

		expect(screen.getByText("Delivery name is required")).toBeInTheDocument();
	});

	it("should show validation error for past date", async () => {
		const user = userEvent.setup();
		renderModal();

		// Set name first
		const nameInput = screen.getByLabelText("Delivery Name");
		await user.type(nameInput, "Test Delivery");

		// Try to set past date
		const dateInput = screen.getByLabelText("Delivery Date");
		const yesterday = new Date();
		yesterday.setDate(yesterday.getDate() - 1);
		const pastDateString = yesterday.toISOString().split("T")[0];
		await user.clear(dateInput);
		await user.type(dateInput, pastDateString);

		const saveButton = screen.getByRole("button", { name: "Save" });
		await user.click(saveButton);

		expect(
			screen.getByText("Delivery date must be in the future"),
		).toBeInTheDocument();
	});

	it("should show validation error when no features are selected", async () => {
		const user = userEvent.setup();
		renderModal();

		const nameInput = screen.getByLabelText("Delivery Name");
		await user.type(nameInput, "Test Delivery");

		// Set future date
		const dateInput = screen.getByLabelText("Delivery Date");
		const tomorrow = new Date();
		tomorrow.setDate(tomorrow.getDate() + 1);
		const futureDateString = tomorrow.toISOString().split("T")[0];
		await user.clear(dateInput);
		await user.type(dateInput, futureDateString);

		const saveButton = screen.getByRole("button", { name: "Save" });
		await user.click(saveButton);

		expect(
			screen.getByText("At least one feature must be selected"),
		).toBeInTheDocument();
	});

	it("should call onSave with correct data when form is valid", async () => {
		const user = userEvent.setup();
		renderModal();

		const nameInput = screen.getByLabelText("Delivery Name");
		await user.type(nameInput, "Test Delivery");

		const tomorrow = new Date();
		tomorrow.setDate(tomorrow.getDate() + 1);
		const futureDateString = tomorrow.toISOString().split("T")[0];

		const dateInput = screen.getByLabelText("Delivery Date");
		await user.clear(dateInput);
		await user.type(dateInput, futureDateString);

		// Wait for features to load, then select one
		await waitFor(() => {
			expect(screen.getByText("Todo Feature 1")).toBeInTheDocument();
		});

		// Select a feature by clicking its checkbox
		const checkbox = screen.getByRole("checkbox", { name: /Todo Feature 1/ });
		await user.click(checkbox);

		const saveButton = screen.getByRole("button", { name: "Save" });
		await user.click(saveButton);

		expect(mockOnSave).toHaveBeenCalledWith({
			name: "Test Delivery",
			date: tomorrow.toISOString().split("T")[0],
			featureIds: [1],
		});
	});

	it("should close modal when cancel is clicked", async () => {
		const user = userEvent.setup();
		renderModal();

		const cancelButton = screen.getByRole("button", { name: "Cancel" });
		await user.click(cancelButton);

		expect(mockOnClose).toHaveBeenCalled();
	});

	it("should show only ToDo and Doing features in feature selector", async () => {
		renderModal();

		await waitFor(() => {
			expect(screen.getByText("Todo Feature 1")).toBeInTheDocument();
			expect(screen.getByText("Doing Feature 1")).toBeInTheDocument();
			expect(screen.getByText("Todo Feature 2")).toBeInTheDocument();
		});

		// Done feature should not be visible
		expect(screen.queryByText("Done Feature 1")).not.toBeInTheDocument();
	});
});
