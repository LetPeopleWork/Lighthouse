import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import {
	KEY_CUSTODY_VALUES,
	type KeyCustody,
} from "../../../models/Encryption/EncryptionKeyState";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IEncryptionService } from "../../../services/Api/EncryptionService";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import type { IOptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import type { ISettingsService } from "../../../services/Api/SettingsService";
import type { ISystemInfoService } from "../../../services/Api/SystemInfoService";
import type { ITerminologyService } from "../../../services/Api/TerminologyService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
	createMockEncryptionService,
	createMockLicensingService,
	createMockOptionalFeatureService,
	createMockSettingsService,
	createMockSystemInfoService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import SystemSettingsTab from "./SystemSettingsTab";

const rbac = vi.hoisted(() => ({ isSystemAdmin: true }));

vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => ({
		isLoading: false,
		isRbacEnabled: true,
		isSystemAdmin: rbac.isSystemAdmin,
		canCreateTeam: true,
		canCreatePortfolio: true,
		isTeamAdmin: () => true,
		isPortfolioAdmin: () => true,
		summary: {
			isRbacEnabled: true,
			isSystemAdmin: rbac.isSystemAdmin,
			canCreateTeam: true,
			canCreatePortfolio: true,
			adminTeamIds: [],
			adminPortfolioIds: [],
		},
	}),
}));

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

const mockGetKeyState = vi.fn();
const mockEncryptionService: IEncryptionService = createMockEncryptionService();
mockEncryptionService.getKeyState = mockGetKeyState;

const mockGetSystemInfo = vi.fn();
const mockSystemInfoService: ISystemInfoService = createMockSystemInfoService();
mockSystemInfoService.getSystemInfo = mockGetSystemInfo;

// Spelled out here rather than read from the wording the component uses, so that changing a phrasing
// has to be a decision taken twice.
const CUSTODY_ON_SCREEN: ReadonlyArray<[KeyCustody, string]> = [
	["NoDurableStore", "the key published with the product"],
	["GeneratedForThisInstance", "generated for this instance"],
	["SuppliedByConfiguration", "supplied by configuration"],
	["SuppliedByExternalSecret", "supplied by a mounted secret file"],
];

const keyStateFor = (custody: KeyCustody) => ({
	custody,
	canMint: custody === "GeneratedForThisInstance",
	activeKeyId: "instance-2026-08-15",
	keyIds: ["instance-2026-08-15"],
	keyStorePath: "/app/data/keys",
	legacyDefaultPresent: false,
});

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
		encryptionService: mockEncryptionService,
		systemInfoService: mockSystemInfoService,
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

		rbac.isSystemAdmin = true;
		mockGetKeyState.mockResolvedValue(keyStateFor("GeneratedForThisInstance"));

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

		// Use the test ID you defined in the component: `${feature.key}-toggle`
		const switchElement = screen.getByTestId("feature1-toggle");
		const input = switchElement.querySelector("input");

		if (!input) {
			throw new Error("Toggle input not found");
		}

		fireEvent.click(input);

		expect(mockUpdateFeature).toHaveBeenCalledWith(
			expect.objectContaining({
				key: "feature1",
				enabled: true,
			}),
		);

		await waitFor(() => {
			expect(input).toBeChecked();
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

	it("should not render the Optional Features section when no features are available", async () => {
		// Override the default mock for this specific test
		mockGetAllFeatures.mockResolvedValue([]);

		renderWithMockApiProvider();

		// Wait for initial load
		await waitFor(() => {
			expect(
				screen.queryByText("Blackout Periods & Recurring Rules"),
			).toBeInTheDocument();
		});

		// Verify the "Optional Features" group is not rendered
		const optionalFeaturesTitle = screen.queryByText("Optional Features");
		const table = screen.queryByTestId("optional-features-table");

		expect(optionalFeaturesTitle).not.toBeInTheDocument();
		expect(table).not.toBeInTheDocument();
	});

	it("should disable the toggle if the feature is premium and the user has no premium license", async () => {
		mockGetAllFeatures.mockResolvedValue([
			{
				id: 3,
				name: "Premium Feature",
				key: "premium-feat",
				description: "Premium only",
				enabled: false,
				isPremium: true, // This is premium
			},
		]);

		mockGetLicenseStatus.mockResolvedValue({
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false, // User cannot use premium
		});

		renderWithMockApiProvider();

		await waitFor(() => {
			const premiumSwitch = screen.getByTestId("premium-feat-toggle");
			// The Material UI Switch input is nested, so we check the 'disabled' attribute
			expect(premiumSwitch.querySelector("input")).toBeDisabled();
		});
	});

	describe("secret encryption key", () => {
		it("should show where the key came from", async () => {
			renderWithMockApiProvider();

			await waitFor(() => {
				expect(screen.getByTestId("encryption-key-custody")).toHaveTextContent(
					"generated for this instance",
				);
			});
		});

		it("should show the name of the active key", async () => {
			renderWithMockApiProvider();

			await waitFor(() => {
				expect(
					screen.getByTestId("encryption-active-key-id"),
				).toHaveTextContent("instance-2026-08-15");
			});
		});

		it("should read key state from the encryption surface", async () => {
			renderWithMockApiProvider();

			await waitFor(() => {
				expect(mockGetKeyState).toHaveBeenCalledTimes(1);
			});
		});

		it("should never read key state from the system information surface", async () => {
			renderWithMockApiProvider();

			await waitFor(() => {
				expect(mockGetKeyState).toHaveBeenCalled();
			});
			expect(mockGetSystemInfo).not.toHaveBeenCalled();
		});

		it("should render the key section for a System Administrator", async () => {
			renderWithMockApiProvider();

			await waitFor(() => {
				expect(screen.getByTestId("encryption-key-state")).toBeVisible();
			});
		});

		it("should neither render the key section nor request key state without System Administrator rights", async () => {
			rbac.isSystemAdmin = false;

			renderWithMockApiProvider();

			await waitFor(() => {
				expect(screen.getByText("Feature 1")).toBeVisible();
			});
			expect(
				screen.queryByTestId("encryption-key-state"),
			).not.toBeInTheDocument();
			expect(mockGetKeyState).not.toHaveBeenCalled();
		});

		it.each(CUSTODY_ON_SCREEN)(
			"should describe custody %s in words rather than as an enum name",
			async (custody, wording) => {
				mockGetKeyState.mockResolvedValue(keyStateFor(custody));

				renderWithMockApiProvider();

				await waitFor(() => {
					expect(
						screen.getByTestId("encryption-key-custody"),
					).toHaveTextContent(wording);
				});
				expect(
					screen.queryByText(custody, { exact: false }),
				).not.toBeInTheDocument();
			},
		);

		it("should have words on screen for every custody the API can return", () => {
			expect(CUSTODY_ON_SCREEN.map(([custody]) => custody)).toEqual([
				...KEY_CUSTODY_VALUES,
			]);
		});

		it("should leave the rest of the page working when the key state fetch fails", async () => {
			mockGetKeyState.mockRejectedValue(new Error("forbidden"));

			renderWithMockApiProvider();

			await waitFor(() => {
				expect(screen.getByText("Feature 1")).toBeVisible();
			});
			expect(
				screen.queryByTestId("encryption-key-state"),
			).not.toBeInTheDocument();
		});
	});
});
