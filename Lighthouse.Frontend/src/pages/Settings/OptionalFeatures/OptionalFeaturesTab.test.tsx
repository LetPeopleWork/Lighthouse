import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IOptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import {
	createMockApiServiceContext,
	createMockOptionalFeatureService,
} from "../../../tests/MockApiServiceProvider";
import OptionalFeaturesTab from "./OptionalFeaturesTab";

// Mocking the Loading Animation component
vi.mock("../../../components/Common/LoadingAnimation/LoadingAnimation", () => ({
	default: ({
		hasError,
		isLoading,
		children,
	}: { hasError: boolean; isLoading: boolean; children: React.ReactNode }) => (
		<>
			{isLoading && <div>Loading...</div>}
			{hasError && <div>Error loading data</div>}
			{!isLoading && !hasError && children}
		</>
	),
}));

const mockOptionalFeatureService: IOptionalFeatureService =
	createMockOptionalFeatureService();

const mockGetAllFeatures = vi.fn();
const mockUpdateFeature = vi.fn();

mockOptionalFeatureService.getAllFeatures = mockGetAllFeatures;
mockOptionalFeatureService.updateFeature = mockUpdateFeature;

const MockApiServiceProvider = ({
	children,
}: { children: React.ReactNode }) => {
	const mockContext = createMockApiServiceContext({
		optionalFeatureService: mockOptionalFeatureService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

const renderWithMockApiProvider = () => {
	render(
		<MockApiServiceProvider>
			<OptionalFeaturesTab />
		</MockApiServiceProvider>,
	);
};

describe("OptionalFeaturesTab component", () => {
	beforeEach(() => {
		mockGetAllFeatures.mockResolvedValue([
			{
				id: 1,
				name: "Feature 1",
				key: "feature1",
				description: "Description 1",
				enabled: false,
				isPreview: true,
			},
			{
				id: 2,
				name: "Feature 2",
				key: "feature2",
				description: "Description 2",
				enabled: true,
				isPreview: false,
			},
		]);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it("should fetch and display optional features", async () => {
		renderWithMockApiProvider();

		await waitFor(() => {
			expect(screen.getByText("Feature 1")).toBeVisible();
		});

		const switches = screen.getAllByRole("checkbox");
		expect(switches[0]).not.toBeChecked();
		expect(switches[1]).toBeChecked();
	});

	it("should toggle the enabled state of a feature", async () => {
		renderWithMockApiProvider();

		// Wait for the features to load
		await waitFor(() => {
			expect(screen.getByText("Feature 1")).toBeVisible();
		});

		const switchElement = screen.getAllByRole("checkbox")[0];
		fireEvent.click(switchElement);

		expect(mockUpdateFeature).toHaveBeenCalledWith({
			id: 1,
			name: "Feature 1",
			key: "feature1",
			description: "Description 1",
			enabled: true,
			isPreview: true,
		});

		// Wait for the state to update and check if the switch reflects the new state
		await waitFor(() => {
			expect(switchElement).toBeChecked();
		});
	});

	it("should display preview indicator for preview features", async () => {
		renderWithMockApiProvider();

		// Wait for the features to load
		await waitFor(() => {
			expect(screen.getByText("Feature 1")).toBeVisible();
		});

		// Check if preview indicator exists for Feature 1 (which is a preview feature)
		const previewIndicator = screen.getByTestId("feature1-preview-indicator");
		expect(previewIndicator).toBeInTheDocument();
		expect(screen.getByText("Preview")).toBeInTheDocument();
	});

	it("should not display preview indicator for non-preview features", async () => {
		renderWithMockApiProvider();

		// Wait for the features to load
		await waitFor(() => {
			expect(screen.getByText("Feature 2")).toBeVisible();
		});

		// Check that there's no preview indicator for Feature 2
		const previewIndicators = screen.queryByTestId(
			"feature2-preview-indicator",
		);
		expect(previewIndicators).not.toBeInTheDocument();
	});
});
