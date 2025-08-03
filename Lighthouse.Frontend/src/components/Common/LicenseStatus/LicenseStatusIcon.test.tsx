import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import LicenseStatusIcon from "./LicenseStatusIcon";

// Mock the theme colors
vi.mock("../../../utils/theme/colors", () => ({
	successColor: "#4caf50",
	warningColor: "#ff9800",
	errorColor: "#f44336",
}));

describe("LicenseStatusIcon", () => {
	let queryClient: QueryClient;
	let mockLicensingService: ILicensingService;

	beforeEach(() => {
		queryClient = new QueryClient({
			defaultOptions: {
				queries: {
					retry: false,
				},
			},
		});

		mockLicensingService = {
			getLicenseStatus: vi.fn(),
			importLicense: vi.fn(),
		};
	});

	const renderComponent = () => {
		const mockApiContext = createMockApiServiceContext({
			licensingService: mockLicensingService,
		});

		return render(
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={mockApiContext}>
					<LicenseStatusIcon />
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);
	};

	it("renders license status icon", () => {
		vi.mocked(mockLicensingService.getLicenseStatus).mockResolvedValue({
			hasLicense: true,
			isValid: true,
		});

		renderComponent();

		expect(screen.getByTestId("license-status-button")).toBeInTheDocument();
		expect(screen.getByLabelText("License Status")).toBeInTheDocument();
	});

	it("shows loading tooltip when data is loading", async () => {
		vi.mocked(mockLicensingService.getLicenseStatus).mockImplementation(
			() => new Promise(() => {}), // Never resolves to simulate loading
		);

		renderComponent();

		const button = screen.getByTestId("license-status-button");
		await userEvent.hover(button);

		await waitFor(() => {
			expect(screen.getByText("Loading license status...")).toBeInTheDocument();
		});
	});

	it("shows error tooltip when license status fails to load", async () => {
		vi.mocked(mockLicensingService.getLicenseStatus).mockRejectedValue(
			new Error("Network error"),
		);

		renderComponent();

		await waitFor(() => {
			const button = screen.getByTestId("license-status-button");
			userEvent.hover(button);
		});

		await waitFor(() => {
			expect(
				screen.getByText("Error loading license status"),
			).toBeInTheDocument();
		});
	});

	it("shows no license tooltip when no license is found", async () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: false,
			isValid: false,
		};

		vi.mocked(mockLicensingService.getLicenseStatus).mockResolvedValue(
			licenseStatus,
		);

		renderComponent();

		await waitFor(() => {
			const button = screen.getByTestId("license-status-button");
			userEvent.hover(button);
		});

		await waitFor(() => {
			expect(
				screen.getByText("No license - Click for details"),
			).toBeInTheDocument();
		});
	});

	it("shows invalid license tooltip when license is invalid", async () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: false,
		};

		vi.mocked(mockLicensingService.getLicenseStatus).mockResolvedValue(
			licenseStatus,
		);

		renderComponent();

		await waitFor(() => {
			const button = screen.getByTestId("license-status-button");
			userEvent.hover(button);
		});

		await waitFor(() => {
			expect(
				screen.getByText("Invalid license - Click for details"),
			).toBeInTheDocument();
		});
	});

	it("shows expires soon tooltip when license expires within 30 days", async () => {
		const expiryDate = new Date();
		expiryDate.setDate(expiryDate.getDate() + 15); // 15 days from now

		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			expiryDate,
		};

		vi.mocked(mockLicensingService.getLicenseStatus).mockResolvedValue(
			licenseStatus,
		);

		renderComponent();

		await waitFor(() => {
			const button = screen.getByTestId("license-status-button");
			userEvent.hover(button);
		});

		await waitFor(() => {
			expect(
				screen.getByText("License expires soon - Click for details"),
			).toBeInTheDocument();
		});
	});

	it("shows valid license tooltip when license is valid and not expiring soon", async () => {
		const expiryDate = new Date();
		expiryDate.setDate(expiryDate.getDate() + 60); // 60 days from now

		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			expiryDate,
		};

		vi.mocked(mockLicensingService.getLicenseStatus).mockResolvedValue(
			licenseStatus,
		);

		renderComponent();

		await waitFor(() => {
			const button = screen.getByTestId("license-status-button");
			userEvent.hover(button);
		});

		await waitFor(() => {
			expect(
				screen.getByText("License valid - Click for details"),
			).toBeInTheDocument();
		});
	});

	it("opens popover when icon is clicked", async () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
		};

		vi.mocked(mockLicensingService.getLicenseStatus).mockResolvedValue(
			licenseStatus,
		);

		renderComponent();

		await waitFor(() => {
			const button = screen.getByTestId("license-status-button");
			userEvent.click(button);
		});

		await waitFor(() => {
			expect(screen.getByTestId("license-status-popover")).toBeInTheDocument();
		});
	});
});
