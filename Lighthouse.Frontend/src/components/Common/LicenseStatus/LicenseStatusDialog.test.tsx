import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import LicenseStatusDialog from "./LicenseStatusDialog";

describe("LicenseStatusDialog", () => {
	const mockOnClose = vi.fn();

	beforeEach(() => {
		mockOnClose.mockClear();
	});

	it("renders loading state", () => {
		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				isLoading={true}
				error={null}
			/>,
		);

		expect(
			screen.getByText("Loading license information..."),
		).toBeInTheDocument();
	});

	it("renders error state", () => {
		const error = new Error("Network error");
		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				isLoading={false}
				error={error}
			/>,
		);

		expect(
			screen.getByText("Failed to load license information: Network error"),
		).toBeInTheDocument();
	});

	it("renders no license status message when licenseStatus is undefined", () => {
		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				isLoading={false}
				error={null}
			/>,
		);

		expect(
			screen.getByText("License information is not available."),
		).toBeInTheDocument();
	});

	it("renders no license warning", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: false,
			isValid: false,
		};

		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(
			screen.getByText(
				"No license found. Please contact your administrator to obtain a valid license.",
			),
		).toBeInTheDocument();

		// Check both the label and the value are present
		expect(screen.getByText("Has License")).toBeInTheDocument();
		const allNoTexts = screen.getAllByText("No");
		expect(allNoTexts.length).toBeGreaterThan(0);
	});

	it("renders invalid license error", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: false,
		};

		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(
			screen.getByText(
				"The current license is invalid. Please contact your administrator to resolve this issue.",
			),
		).toBeInTheDocument();

		// Check both the label and the value are present
		expect(screen.getByText("Has License")).toBeInTheDocument();
		const allYesTexts = screen.getAllByText("Yes");
		expect(allYesTexts.length).toBeGreaterThan(0);
	});

	it("renders expiring soon warning", () => {
		const expiryDate = new Date();
		expiryDate.setDate(expiryDate.getDate() + 15); // 15 days from now

		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			expiryDate,
		};

		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(
			screen.getByText(
				"Your license will expire soon. Please contact your administrator to renew it.",
			),
		).toBeInTheDocument();
	});

	it("renders valid license success message", () => {
		const expiryDate = new Date();
		expiryDate.setDate(expiryDate.getDate() + 60); // 60 days from now

		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			expiryDate,
		};

		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(
			screen.getByText("Your license is valid and active."),
		).toBeInTheDocument();
	});

	it("renders complete license information", () => {
		const expiryDate = new Date("2025-12-31");
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
			name: "John Doe",
			email: "john.doe@example.com",
			organization: "Example Corp",
			expiryDate,
		};

		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(screen.getByText("John Doe")).toBeInTheDocument();
		expect(screen.getByText("john.doe@example.com")).toBeInTheDocument();
		expect(screen.getByText("Example Corp")).toBeInTheDocument();

		// More flexible date checking since locale can vary
		const expiryDateElement = screen
			.getByText("Expiry Date")
			.parentElement?.querySelector("p:last-child");
		expect(expiryDateElement?.textContent).toContain("2025");
	});

	it("does not render optional fields when they are not provided", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
		};

		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(screen.queryByText("Licensed To")).not.toBeInTheDocument();
		expect(screen.queryByText("Email")).not.toBeInTheDocument();
		expect(screen.queryByText("Organization")).not.toBeInTheDocument();
		expect(screen.queryByText("Expiry Date")).not.toBeInTheDocument();
	});

	it("calls onClose when close button is clicked", async () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
		};

		render(
			<LicenseStatusDialog
				open={true}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		const closeButton = screen.getByText("Close");
		await userEvent.click(closeButton);

		expect(mockOnClose).toHaveBeenCalledTimes(1);
	});

	it("does not render when open is false", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: true,
			isValid: true,
		};

		render(
			<LicenseStatusDialog
				open={false}
				onClose={mockOnClose}
				licenseStatus={licenseStatus}
				isLoading={false}
				error={null}
			/>,
		);

		expect(
			screen.queryByTestId("license-status-dialog"),
		).not.toBeInTheDocument();
	});
});
