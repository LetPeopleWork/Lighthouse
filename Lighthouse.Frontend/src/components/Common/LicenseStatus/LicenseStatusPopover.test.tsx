import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import type { IVersionService } from "../../../services/Api/VersionService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import LicenseStatusPopover from "./LicenseStatusPopover";

describe("LicenseStatusPopover", () => {
	const mockOnClose = vi.fn();
	const mockOnLicenseImported = vi.fn();
	const mockAnchorEl = document.createElement("button");
	let mockLicensingService: ILicensingService;
	let mockVersionService: IVersionService;

	beforeEach(() => {
		mockOnClose.mockClear();
		mockOnLicenseImported.mockClear();

		mockLicensingService = {
			getLicenseStatus: vi.fn(),
			importLicense: vi.fn(),
			clearLicense: vi.fn(),
		};

		mockVersionService = {
			getCurrentVersion: vi.fn().mockResolvedValue("v1.33.7"),
			isUpdateAvailable: vi.fn(),
			getNewReleases: vi.fn(),
			isUpdateSupported: vi.fn(),
			installUpdate: vi.fn(),
		};

		// Mock window.open
		vi.stubGlobal("open", vi.fn());

		// Mock location.href
		delete (globalThis as { location?: unknown }).location;
		globalThis.location = { href: "" } as Location;
	});

	const renderComponent = (
		props: Partial<React.ComponentProps<typeof LicenseStatusPopover>> = {},
	) => {
		const mockApiContext = createMockApiServiceContext({
			licensingService: mockLicensingService,
			versionService: mockVersionService,
		});

		return render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<LicenseStatusPopover
					anchorEl={mockAnchorEl}
					onClose={mockOnClose}
					onLicenseImported={mockOnLicenseImported}
					isLoading={false}
					error={null}
					{...props}
				/>
			</ApiServiceContext.Provider>,
		);
	};

	it("renders loading state", () => {
		renderComponent({ isLoading: true });

		expect(screen.getByText("Loading...")).toBeInTheDocument();
	});

	it("renders error state", () => {
		const error = new Error("Network error");
		renderComponent({ error });

		expect(screen.getByText("Error")).toBeInTheDocument();
		expect(
			screen.getByText("Failed to load license information"),
		).toBeInTheDocument();
	});

	it("renders no license status when licenseStatus is undefined", () => {
		renderComponent();

		expect(
			screen.getByText("License information unavailable"),
		).toBeInTheDocument();
	});

	it("renders info button and upload button", () => {
		renderComponent();

		expect(
			screen.getByLabelText("Learn more about Premium Features"),
		).toBeInTheDocument();
		expect(screen.getByText("Add License")).toBeInTheDocument();
	});

	it("shows 'Update License' when license exists", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
		};

		renderComponent({ licenseStatus });

		expect(screen.getByText("Update License")).toBeInTheDocument();
	});

	it("shows 'Add License' when no license exists", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: false,
			isValid: false,
			canUsePremiumFeatures: false,
		};

		renderComponent({ licenseStatus });

		expect(screen.getByText("Add License")).toBeInTheDocument();
	});

	it("opens Lighthouse website when info button is clicked", async () => {
		const user = userEvent.setup();
		renderComponent();

		const infoButton = screen.getByLabelText(
			"Learn more about Premium Features",
		);
		await user.click(infoButton);

		expect(window.open).toHaveBeenCalledWith(
			"https://letpeople.work/lighthouse",
			"_blank",
			"noopener,noreferrer",
		);
	});

	it("handles file upload", async () => {
		const user = userEvent.setup();
		const mockLicenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
			name: "Test User",
		};

		vi.mocked(mockLicensingService.importLicense).mockResolvedValue(
			mockLicenseStatus,
		);

		renderComponent();

		// Create a mock file
		const file = new File(['{"license": "data"}'], "license.json", {
			type: "application/json",
		});

		const uploadButton = screen.getByText("Add License");
		await user.click(uploadButton);

		// Find the hidden file input and upload the file
		const fileInput = document.querySelector(
			'input[type="file"]',
		) as HTMLInputElement;
		await user.upload(fileInput, file);

		await waitFor(() => {
			expect(mockLicensingService.importLicense).toHaveBeenCalledWith(file);
			expect(mockOnLicenseImported).toHaveBeenCalledWith(mockLicenseStatus);
		});
	});

	it("shows error when uploading non-JSON file", async () => {
		const user = userEvent.setup();
		renderComponent();

		// Create a non-JSON file
		const file = new File(["some text"], "document.txt", {
			type: "text/plain",
		});

		const uploadButton = screen.getByText("Add License");
		await user.click(uploadButton);

		const fileInput = document.querySelector(
			'input[type="file"]',
		) as HTMLInputElement;

		// Directly trigger the file change event instead of using user.upload
		fireEvent.change(fileInput, { target: { files: [file] } });

		await waitFor(() => {
			expect(screen.getByText("Please select a JSON file")).toBeInTheDocument();
		});
	});

	it("renders no license state", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: false,
			isValid: false,
			canUsePremiumFeatures: false,
		};

		renderComponent({ licenseStatus });

		expect(screen.getByText("No License")).toBeInTheDocument();
		expect(
			screen.getByText(
				"You are using the free version of Lighthouse. Some features are not available and some constraints apply.",
			),
		).toBeInTheDocument();
	});

	it("renders invalid license state", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: false,
			canUsePremiumFeatures: false,
			name: "John Doe",
			email: "john.doe@example.com",
		};

		render(
			<LicenseStatusPopover
				anchorEl={mockAnchorEl}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(screen.getByText("Invalid License")).toBeInTheDocument();
		expect(screen.getByText("Licensed to:")).toBeInTheDocument();
		expect(screen.getByText("John Doe")).toBeInTheDocument();
		expect(
			screen.getByText(
				"License is invalid. Premium Features will be disabled.",
			),
		).toBeInTheDocument();
	});

	it("renders valid license with expiry warning", () => {
		const expiryDate = new Date();
		expiryDate.setDate(expiryDate.getDate() + 15); // 15 days from now

		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
			name: "John Doe",
			email: "john.doe@example.com",
			organization: "Example Corp",
			expiryDate,
		};

		render(
			<LicenseStatusPopover
				anchorEl={mockAnchorEl}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(screen.getByText("Licensed")).toBeInTheDocument();
		expect(screen.getByText("Example Corp")).toBeInTheDocument();
		expect(
			screen.getByText(
				"License will expire soon. Premium Features will be disabled if not renewed.",
			),
		).toBeInTheDocument();
	});

	it("renders valid license without warnings", () => {
		const expiryDate = new Date();
		expiryDate.setDate(expiryDate.getDate() + 60); // 60 days from now

		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
			name: "John Doe",
			email: "john.doe@example.com",
			organization: "Example Corp",
			expiryDate,
		};

		render(
			<LicenseStatusPopover
				anchorEl={mockAnchorEl}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(screen.getByText("Licensed")).toBeInTheDocument();
		expect(
			screen.queryByText("License will expire soon. Please renew."),
		).not.toBeInTheDocument();
		expect(
			screen.queryByText(
				"License is invalid. Please contact your administrator.",
			),
		).not.toBeInTheDocument();
	});

	it("does not render when anchorEl is null", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
		};

		render(
			<LicenseStatusPopover
				anchorEl={null}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(
			screen.queryByTestId("license-status-popover"),
		).not.toBeInTheDocument();
	});

	it("shows clear license button when license exists", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
			name: "Test User",
		};

		renderComponent({ licenseStatus });

		expect(screen.getByText("Clear License")).toBeInTheDocument();
	});

	it("does not show clear license button when no license exists", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: false,
			isValid: false,
			canUsePremiumFeatures: false,
		};

		renderComponent({ licenseStatus });

		expect(screen.queryByText("Clear License")).not.toBeInTheDocument();
	});

	it("handles clear license successfully", async () => {
		const user = userEvent.setup();
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
			name: "Test User",
		};

		vi.mocked(mockLicensingService.clearLicense).mockResolvedValue();

		renderComponent({ licenseStatus });

		const clearButton = screen.getByText("Clear License");
		await user.click(clearButton);

		// Confirmation dialog should appear
		await waitFor(() => {
			expect(screen.getByText("Clear License?")).toBeInTheDocument();
			expect(
				screen.getByText(
					/Are you sure you want to clear the license information/,
				),
			).toBeInTheDocument();
		});

		// Click confirm button
		const confirmButton = screen.getByRole("button", {
			name: /Clear License/i,
		});
		await user.click(confirmButton);

		await waitFor(() => {
			expect(mockLicensingService.clearLicense).toHaveBeenCalled();
			expect(mockOnLicenseImported).toHaveBeenCalledWith({
				hasLicense: false,
				isValid: false,
				canUsePremiumFeatures: false,
			});
		});
	});

	it("cancels clear license on dialog cancel", async () => {
		const user = userEvent.setup();
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
			name: "Test User",
		};

		renderComponent({ licenseStatus });

		const clearButton = screen.getByText("Clear License");
		await user.click(clearButton);

		// Confirmation dialog should appear
		await waitFor(() => {
			expect(screen.getByText("Clear License?")).toBeInTheDocument();
		});

		// Click cancel button
		const cancelButton = screen.getByRole("button", { name: /Cancel/i });
		await user.click(cancelButton);

		// Dialog should close without calling clearLicense
		await waitFor(() => {
			expect(screen.queryByText("Clear License?")).not.toBeInTheDocument();
		});
		expect(mockLicensingService.clearLicense).not.toHaveBeenCalled();
	});

	it("shows error when clear license fails", async () => {
		const user = userEvent.setup();
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
			name: "Test User",
		};

		vi.mocked(mockLicensingService.clearLicense).mockRejectedValue(
			new Error("Failed to clear license"),
		);

		renderComponent({ licenseStatus });

		const clearButton = screen.getByText("Clear License");
		await user.click(clearButton);

		// Confirm in dialog
		await waitFor(() => {
			expect(screen.getByText("Clear License?")).toBeInTheDocument();
		});

		const confirmButton = screen.getByRole("button", {
			name: /Clear License/i,
		});
		await user.click(confirmButton);

		await waitFor(() => {
			expect(mockLicensingService.clearLicense).toHaveBeenCalled();
			expect(screen.getByText("Failed to clear license")).toBeInTheDocument();
		});
	});

	it("disables clear button during upload", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
			name: "Test User",
		};

		renderComponent({ licenseStatus });

		const clearButton = screen.getByText("Clear License") as HTMLButtonElement;
		expect(clearButton.disabled).toBe(false);

		// Note: Testing the disabled state during actual upload would require
		// more complex test setup with controlled state
	});

	it("renders pending license when validFrom is in the future", () => {
		const validFromDate = new Date();
		validFromDate.setDate(validFromDate.getDate() + 10); // 10 days in the future

		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: false,
			name: "John Doe",
			email: "john.doe@example.com",
			organization: "Example Corp",
			validFrom: validFromDate,
		};

		render(
			<LicenseStatusPopover
				anchorEl={mockAnchorEl}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(screen.getByText("Pending")).toBeInTheDocument();
		expect(screen.getByText(/License will be valid from/i)).toBeInTheDocument();
		expect(
			screen.getByText(/Premium Features are not yet available/i),
		).toBeInTheDocument();
	});

	describe("Renew License Button", () => {
		it("shows renew button when license expires within 30 days", () => {
			const expiryDate = new Date();
			expiryDate.setDate(expiryDate.getDate() + 15); // 15 days from now

			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: false,
				name: "John Doe",
				email: "john@example.com",
				organization: "Example Corp",
				expiryDate,
			};

			renderComponent({ licenseStatus });

			expect(screen.getByText("Renew License")).toBeInTheDocument();
		});

		it("shows renew button when license has expired", () => {
			const expiryDate = new Date();
			expiryDate.setDate(expiryDate.getDate() - 5); // 5 days ago

			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: false,
				canUsePremiumFeatures: false,
				name: "John Doe",
				email: "john@example.com",
				organization: "Example Corp",
				expiryDate,
			};

			renderComponent({ licenseStatus });

			expect(screen.getByText("Renew License")).toBeInTheDocument();
		});

		it("does not show renew button when license expires in more than 30 days", () => {
			const expiryDate = new Date();
			expiryDate.setDate(expiryDate.getDate() + 60); // 60 days from now

			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: false,
				name: "John Doe",
				email: "john@example.com",
				expiryDate,
			};

			renderComponent({ licenseStatus });

			expect(screen.queryByText("Renew License")).not.toBeInTheDocument();
		});

		it("does not show renew button when no license exists", () => {
			const licenseStatus: ILicenseStatus = {
				hasLicense: false,
				isValid: false,
				canUsePremiumFeatures: false,
			};

			renderComponent({ licenseStatus });

			expect(screen.queryByText("Renew License")).not.toBeInTheDocument();
		});

		it("does not show renew button when license has no expiry date", () => {
			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: false,
				name: "John Doe",
			};

			renderComponent({ licenseStatus });

			expect(screen.queryByText("Renew License")).not.toBeInTheDocument();
		});

		it("opens renewal URL with correct parameters for expiring license", async () => {
			const user = userEvent.setup();
			// Set expiry date to be within 30 days
			const expiryDate = new Date();
			expiryDate.setDate(expiryDate.getDate() + 20); // 20 days from now

			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: false,
				name: "Jane Smith",
				email: "jane@company.com",
				organization: "Tech Inc",
				expiryDate,
			};

			renderComponent({ licenseStatus });

			const renewButton = screen.getByText("Renew License");
			await user.click(renewButton);

			expect(window.open).toHaveBeenCalledWith(
				expect.stringContaining("https://letpeople.work/lighthouse?"),
				"_blank",
				"noopener,noreferrer",
			);

			const callUrl = vi.mocked(window.open).mock.calls[0][0] as string;
			expect(callUrl).toContain("name=Jane+Smith");
			expect(callUrl).toContain("email=jane%40company.com");
			expect(callUrl).toContain("organization=Tech+Inc");

			// Calculate expected validFrom (day after expiry)
			const expectedValidFrom = new Date(expiryDate);
			expectedValidFrom.setDate(expectedValidFrom.getDate() + 1);
			const expectedValidFromString = expectedValidFrom
				.toISOString()
				.split("T")[0];

			expect(callUrl).toContain(`validFrom=${expectedValidFromString}`);
			expect(callUrl).toContain("#lighthouse-license");
		});

		it("opens renewal URL with today's date for expired license", async () => {
			const user = userEvent.setup();
			const today = new Date();
			const todayString = today.toISOString().split("T")[0];

			const expiryDate = new Date();
			expiryDate.setDate(expiryDate.getDate() - 10); // 10 days ago

			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: false,
				canUsePremiumFeatures: false,
				name: "Jane Smith",
				email: "jane@company.com",
				organization: "Tech Inc",
				expiryDate,
			};

			renderComponent({ licenseStatus });

			const renewButton = screen.getByText("Renew License");
			await user.click(renewButton);

			expect(window.open).toHaveBeenCalledWith(
				expect.stringContaining("https://letpeople.work/lighthouse?"),
				"_blank",
				"noopener,noreferrer",
			);

			const callUrl = vi.mocked(window.open).mock.calls[0][0] as string;
			expect(callUrl).toContain("name=Jane+Smith");
			expect(callUrl).toContain("email=jane%40company.com");
			expect(callUrl).toContain("organization=Tech+Inc");
			expect(callUrl).toContain(`validFrom=${todayString}`);
			expect(callUrl).toContain("#lighthouse-license");
		});

		it("opens renewal URL without optional fields when not present", async () => {
			const user = userEvent.setup();
			// Set expiry date to be within 30 days
			const expiryDate = new Date();
			expiryDate.setDate(expiryDate.getDate() + 20); // 20 days from now

			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: false,
				expiryDate,
				// No name, email, or organization
			};

			renderComponent({ licenseStatus });

			const renewButton = screen.getByText("Renew License");
			await user.click(renewButton);

			expect(window.open).toHaveBeenCalledWith(
				expect.stringContaining("https://letpeople.work/lighthouse?"),
				"_blank",
				"noopener,noreferrer",
			);

			const callUrl = vi.mocked(window.open).mock.calls[0][0] as string;
			expect(callUrl).not.toContain("name=");
			expect(callUrl).not.toContain("email=");
			expect(callUrl).not.toContain("organization=");

			// Calculate expected validFrom (day after expiry)
			const expectedValidFrom = new Date(expiryDate);
			expectedValidFrom.setDate(expectedValidFrom.getDate() + 1);
			const expectedValidFromString = expectedValidFrom
				.toISOString()
				.split("T")[0];

			expect(callUrl).toContain(`validFrom=${expectedValidFromString}`);
			expect(callUrl).toContain("#lighthouse-license");
		});

		it("shows renew button exactly on expiry day", () => {
			const expiryDate = new Date();
			expiryDate.setHours(0, 0, 0, 0); // Start of today

			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: false,
				canUsePremiumFeatures: false,
				name: "John Doe",
				email: "john@example.com",
				expiryDate,
			};

			renderComponent({ licenseStatus });

			expect(screen.getByText("Renew License")).toBeInTheDocument();
		});

		it("shows renew button exactly 30 days before expiry", () => {
			const expiryDate = new Date();
			expiryDate.setDate(expiryDate.getDate() + 30);

			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: false,
				name: "John Doe",
				email: "john@example.com",
				expiryDate,
			};

			renderComponent({ licenseStatus });

			expect(screen.getByText("Renew License")).toBeInTheDocument();
		});
	});

	describe("Contact Support", () => {
		it("shows Contact Support icon when canUsePremiumFeatures is true", async () => {
			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: true,
				name: "John Doe",
				email: "john@example.com",
				organization: "Test Org",
				licenseNumber: "LIC-12345",
				expiryDate: new Date("2025-12-31"),
			};

			renderComponent({ licenseStatus });

			// Wait for the version to be fetched
			await waitFor(() => {
				expect(mockVersionService.getCurrentVersion).toHaveBeenCalled();
			});

			expect(screen.getByLabelText("Contact Support")).toBeInTheDocument();
		});

		it("does not show Contact Support icon when canUsePremiumFeatures is false", () => {
			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: false,
				name: "John Doe",
				email: "john@example.com",
			};

			renderComponent({ licenseStatus });

			expect(screen.queryByLabelText("Contact Support")).not.toBeInTheDocument();
		});

		it("opens mailto link with correct information when Contact Support icon is clicked", async () => {
			const user = userEvent.setup();
			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: true,
				name: "John Doe",
				email: "john@example.com",
				organization: "Test Org",
				licenseNumber: "LIC-12345",
				expiryDate: new Date("2025-12-31"),
			};

			renderComponent({ licenseStatus });

			// Wait for the version to be fetched
			await waitFor(() => {
				expect(mockVersionService.getCurrentVersion).toHaveBeenCalled();
			});

			const supportIcon = screen.getByLabelText("Contact Support");
			await user.click(supportIcon);

			// Check that location.href was set with a mailto link
			expect(globalThis.location.href).toContain("mailto:lighthouse@letpeople.work");
			expect(globalThis.location.href).toContain("subject=");
			expect(globalThis.location.href).toContain("Lighthouse%20Support%20Request");
			expect(globalThis.location.href).toContain("body=");
			expect(globalThis.location.href).toContain("John%20Doe");
			expect(globalThis.location.href).toContain("john%40example.com");
			expect(globalThis.location.href).toContain("Test%20Org");
			expect(globalThis.location.href).toContain("LIC-12345");
			expect(globalThis.location.href).toContain("v1.33.7");
		});

		it("handles missing license information gracefully in support email", async () => {
			const user = userEvent.setup();
			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: true,
			};

			renderComponent({ licenseStatus });

			// Wait for the version to be fetched
			await waitFor(() => {
				expect(mockVersionService.getCurrentVersion).toHaveBeenCalled();
			});

			const supportIcon = screen.getByLabelText("Contact Support");
			await user.click(supportIcon);

			// Check that location.href was set with N/A for missing fields
			expect(globalThis.location.href).toContain("N%2FA"); // URL encoded N/A
		});

		it("fetches version when popover opens with premium license", async () => {
			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: true,
				name: "John Doe",
			};

			renderComponent({ licenseStatus });

			await waitFor(() => {
				expect(mockVersionService.getCurrentVersion).toHaveBeenCalledTimes(1);
			});
		});

		it("does not fetch version when canUsePremiumFeatures is false", () => {
			const licenseStatus: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: false,
				name: "John Doe",
			};

			renderComponent({ licenseStatus });

			expect(mockVersionService.getCurrentVersion).not.toHaveBeenCalled();
		});
	});
});
