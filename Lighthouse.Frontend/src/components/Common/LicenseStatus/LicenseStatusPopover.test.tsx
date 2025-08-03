import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import LicenseStatusPopover from "./LicenseStatusPopover";

describe("LicenseStatusPopover", () => {
	const mockOnClose = vi.fn();
	const mockAnchorEl = document.createElement("button");

	beforeEach(() => {
		mockOnClose.mockClear();
	});

	it("renders loading state", () => {
		render(
			<LicenseStatusPopover
				anchorEl={mockAnchorEl}
				onClose={mockOnClose}
				isLoading={true}
				error={null}
			/>,
		);

		expect(screen.getByText("Loading...")).toBeInTheDocument();
	});

	it("renders error state", () => {
		const error = new Error("Network error");
		render(
			<LicenseStatusPopover
				anchorEl={mockAnchorEl}
				onClose={mockOnClose}
				isLoading={false}
				error={error}
			/>,
		);

		expect(screen.getByText("Error")).toBeInTheDocument();
		expect(
			screen.getByText("Failed to load license information"),
		).toBeInTheDocument();
	});

	it("renders no license status when licenseStatus is undefined", () => {
		render(
			<LicenseStatusPopover
				anchorEl={mockAnchorEl}
				onClose={mockOnClose}
				isLoading={false}
				error={null}
			/>,
		);

		expect(
			screen.getByText("License information unavailable"),
		).toBeInTheDocument();
	});

	it("renders no license state", () => {
		const licenseStatus: ILicenseStatus = {
			hasLicense: false,
			isValid: false,
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

		expect(screen.getByText("No License")).toBeInTheDocument();
		expect(
			screen.getByText("Lighthouse is running without a license. Premium Features are not enabled."),
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
			screen.getByText("License will expire soon. Premium Features will be disabled if not renewed."),
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
