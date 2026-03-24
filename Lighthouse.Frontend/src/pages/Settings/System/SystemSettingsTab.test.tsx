import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import type { IOptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import type { ISettingsService } from "../../../services/Api/SettingsService";
import type { ITerminologyService } from "../../../services/Api/TerminologyService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
	createMockLicensingService,
	createMockOptionalFeatureService,
	createMockSettingsService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import SystemSettingsTab from "./SystemSettingsTab";

const mockOptionalFeatureService: IOptionalFeatureService =
	createMockOptionalFeatureService();

const mockGetAllFeatures = vi.fn();
const mockUpdateFeature = vi.fn();

mockOptionalFeatureService.getAllFeatures = mockGetAllFeatures;
mockOptionalFeatureService.updateFeature = mockUpdateFeature;

const mockSettingsService: ISettingsService = createMockSettingsService();

const mockGetAllTerminology = vi.fn();
const mockUpdateTerminology = vi.fn();

const mockTerminologyService: ITerminologyService =
	createMockTerminologyService();
mockTerminologyService.getAllTerminology = mockGetAllTerminology;
mockTerminologyService.updateTerminology = mockUpdateTerminology;

const mockGetLicenseStatus = vi.fn();
const mockLicensingService: ILicensingService = createMockLicensingService();
mockLicensingService.getLicenseStatus = mockGetLicenseStatus;

const mockBlackoutPeriodService = createMockBlackoutPeriodService();
const mockGetAllBlackoutPeriods = vi.fn();
mockBlackoutPeriodService.getAll = mockGetAllBlackoutPeriods;

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockContext = createMockApiServiceContext({
		settingsService: mockSettingsService,
		optionalFeatureService: mockOptionalFeatureService,
		terminologyService: mockTerminologyService,
		licensingService: mockLicensingService,
		blackoutPeriodService: mockBlackoutPeriodService,
	});

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	return (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={mockContext}>
				<TerminologyProvider>{children}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>
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

		mockGetAllBlackoutPeriods.mockResolvedValue([]);

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

		mockGetLicenseStatus.mockResolvedValue({
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		});

		mockGetAllTerminology.mockResolvedValue([
			{
				id: 1,
				key: "Work Item",
				defaultValue: "Work Item",
				description: "Term used for individual work items",
				value: "Work Item",
			},
			{
				id: 2,
				key: "Work Items",
				defaultValue: "Work Items",
				description: "Term used for multiple work items",
				value: "Work Items",
			},
		]);
	});

	afterEach(() => {
		vi.clearAllMocks();
		vi.restoreAllMocks();
	});

	it("should fetch and display optional features", async () => {
		renderWithMockApiProvider();

		await waitFor(() => {
			expect(screen.getByText("Feature 1")).toBeVisible();
		});

		const switches = screen.getAllByRole("switch");
		expect(switches[0]).not.toBeChecked();
		expect(switches[1]).toBeChecked();
	});

	it("should toggle the enabled state of a feature", async () => {
		renderWithMockApiProvider();

		// Wait for the features to load
		await waitFor(() => {
			expect(screen.getByText("Feature 1")).toBeVisible();
		});

		const switchElement = screen.getAllByRole("switch")[0];
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
