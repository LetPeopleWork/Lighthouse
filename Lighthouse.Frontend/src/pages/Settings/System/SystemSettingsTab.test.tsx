import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IOptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import type { ISettingsService } from "../../../services/Api/SettingsService";
import {
	createMockApiServiceContext,
	createMockConfigurationService,
	createMockOptionalFeatureService,
	createMockSettingsService,
} from "../../../tests/MockApiServiceProvider";
import SystemSettingsTab from "./SystemSettingsTab";

const mockOptionalFeatureService: IOptionalFeatureService =
	createMockOptionalFeatureService();

const mockGetAllFeatures = vi.fn();
const mockUpdateFeature = vi.fn();

mockOptionalFeatureService.getAllFeatures = mockGetAllFeatures;
mockOptionalFeatureService.updateFeature = mockUpdateFeature;

const mockGetDataRetentionSettings = vi.fn();
const mockUpdateDataRetentionSettings = vi.fn();

const mockSettingsService: ISettingsService = createMockSettingsService();
mockSettingsService.getDataRetentionSettings = mockGetDataRetentionSettings;
mockSettingsService.updateDataRetentionSettings =
	mockUpdateDataRetentionSettings;

const mockExportConfiguration = vi.fn();

const mockConfigurationService = createMockConfigurationService();
mockConfigurationService.exportConfiguration = mockExportConfiguration;

const MockApiServiceProvider = ({
	children,
}: { children: React.ReactNode }) => {
	const mockContext = createMockApiServiceContext({
		settingsService: mockSettingsService,
		optionalFeatureService: mockOptionalFeatureService,
		configurationService: mockConfigurationService,
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
			<SystemSettingsTab />
		</MockApiServiceProvider>,
	);
};

describe("SystemSettingsTab Component", () => {
	beforeEach(() => {
		vi.resetAllMocks();

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
		vi.restoreAllMocks();
	});

	it("should fetch and display the data retention settings", async () => {
		const mockData = { maxStorageTimeInDays: 30 };
		mockGetDataRetentionSettings.mockResolvedValue(Promise.resolve(mockData));

		renderWithMockApiProvider();

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		expect(screen.getByDisplayValue("30")).toBeInTheDocument();
	});

	it("should handle input changes", async () => {
		const mockData = { maxStorageTimeInDays: 30 };
		mockGetDataRetentionSettings.mockResolvedValue(Promise.resolve(mockData));

		renderWithMockApiProvider();

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		fireEvent.change(
			await screen.findByLabelText("Maximum Data Retention Time (Days)"),
			{ target: { value: "60" } },
		);

		expect(screen.getByDisplayValue("60")).toBeInTheDocument();
	});

	it("should call updateSettings with new values when button is clicked", async () => {
		const mockData = { maxStorageTimeInDays: 30 };
		const updatedData = { maxStorageTimeInDays: 60 };
		mockGetDataRetentionSettings.mockResolvedValue(Promise.resolve(mockData));
		mockUpdateDataRetentionSettings.mockResolvedValue(Promise.resolve());

		renderWithMockApiProvider();

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		fireEvent.change(
			await screen.findByLabelText("Maximum Data Retention Time (Days)"),
			{ target: { value: updatedData.maxStorageTimeInDays.toString() } },
		);

		fireEvent.click(screen.getByText(/Update Data Retention Settings/));

		expect(mockUpdateDataRetentionSettings).toHaveBeenCalledWith(updatedData);
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

	it("should call exportConfiguration when Export Configuration button is clicked", async () => {
		mockGetDataRetentionSettings.mockResolvedValue({
			maxStorageTimeInDays: 30,
		});
		mockGetAllFeatures.mockResolvedValue([
			{
				id: 1,
				name: "Feature 1",
				key: "feature1",
				description: "Description 1",
				enabled: false,
				isPreview: true,
			},
		]);

		renderWithMockApiProvider();

		// Wait for components to load
		await waitFor(() => {
			expect(screen.getByText("Export Configuration")).toBeVisible();
		});

		// Click the Export Configuration button
		fireEvent.click(screen.getByText("Export Configuration"));

		// Verify the exportConfiguration function was called
		expect(mockExportConfiguration).toHaveBeenCalled();
	});
});
