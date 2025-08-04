import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import LicenseStatusPopover from "./LicenseStatusPopover";

describe("LicenseStatusPopover", () => {
	const mockOnClose = vi.fn();
	const mockOnLicenseImported = vi.fn();
	const mockAnchorEl = document.createElement("button");
	let mockLicensingService: ILicensingService;

	beforeEach(() => {
		mockOnClose.mockClear();
		mockOnLicenseImported.mockClear();

		mockLicensingService = {
			getLicenseStatus: vi.fn(),
			importLicense: vi.fn(),
		};

		// Mock window.open
		vi.stubGlobal("open", vi.fn());
	});

	const renderComponent = (
		props: Partial<React.ComponentProps<typeof LicenseStatusPopover>> = {},
	) => {
		const mockApiContext = createMockApiServiceContext({
			licensingService: mockLicensingService,
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

	it("shows 'Renew License' when license exists", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
		};

		renderComponent({ licenseStatus });

		expect(screen.getByText("Renew License")).toBeInTheDocument();
	});

	it("shows 'Add License' when no license exists", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: false,
			isValid: false,
		};

		renderComponent({ licenseStatus });

		expect(screen.getByText("Add License")).toBeInTheDocument();
	});

	it("opens Lighthouse website when info button is clicked", async () => {
		const user = userEvent.setup();
		renderComponent();

		const infoButton = screen.getByLabelText("Learn more about Premium Features");
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
		};

		renderComponent({ licenseStatus });

		expect(screen.getByText("No License")).toBeInTheDocument();
		expect(
			screen.getByText(
				"In future versions, some features will become premium and require a valid license to use.",
			),
		).toBeInTheDocument();
	});

	it("renders invalid license state", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: false,
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
});
