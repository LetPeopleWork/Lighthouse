import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import BlockedPage from "./BlockedPage";

const createMockLicensingService = (
	overrides: Partial<ILicensingService> = {},
): ILicensingService => ({
	getLicenseStatus: vi.fn().mockResolvedValue({
		hasLicense: false,
		isValid: false,
		canUsePremiumFeatures: false,
	}),
	importLicense: vi.fn().mockResolvedValue({
		hasLicense: true,
		isValid: true,
		canUsePremiumFeatures: true,
	}),
	clearLicense: vi.fn().mockResolvedValue(undefined),
	...overrides,
});

describe("BlockedPage", () => {
	it("should render the blocked page with explanatory text", () => {
		render(
			<BlockedPage
				licensingService={createMockLicensingService()}
				onLicenseImported={vi.fn()}
				onLogout={vi.fn()}
			/>,
		);

		expect(screen.getByTestId("blocked-page")).toBeInTheDocument();
		expect(screen.getByText("Premium License Required")).toBeInTheDocument();
		expect(
			screen.getByText(/Authentication is a Premium feature/),
		).toBeInTheDocument();
	});

	it("should render the Lighthouse logo", () => {
		render(
			<BlockedPage
				licensingService={createMockLicensingService()}
				onLicenseImported={vi.fn()}
				onLogout={vi.fn()}
			/>,
		);

		expect(screen.getByAltText("Lighthouse logo")).toBeInTheDocument();
	});

	it("should render upload license button", () => {
		render(
			<BlockedPage
				licensingService={createMockLicensingService()}
				onLicenseImported={vi.fn()}
				onLogout={vi.fn()}
			/>,
		);

		expect(screen.getByTestId("upload-license-button")).toBeInTheDocument();
		expect(screen.getByText("Upload License")).toBeInTheDocument();
	});

	it("should render logout button", () => {
		render(
			<BlockedPage
				licensingService={createMockLicensingService()}
				onLicenseImported={vi.fn()}
				onLogout={vi.fn()}
			/>,
		);

		expect(screen.getByTestId("blocked-logout-button")).toBeInTheDocument();
	});

	it("should call onLogout when logout button is clicked", async () => {
		const user = userEvent.setup();
		const onLogout = vi.fn();

		render(
			<BlockedPage
				licensingService={createMockLicensingService()}
				onLicenseImported={vi.fn()}
				onLogout={onLogout}
			/>,
		);

		await user.click(screen.getByTestId("blocked-logout-button"));
		expect(onLogout).toHaveBeenCalledOnce();
	});

	it("should call onLicenseImported after successful upload", async () => {
		const user = userEvent.setup();
		const onLicenseImported = vi.fn();
		const mockService = createMockLicensingService();

		render(
			<BlockedPage
				licensingService={mockService}
				onLicenseImported={onLicenseImported}
				onLogout={vi.fn()}
			/>,
		);

		const file = new File(['{"key": "value"}'], "license.json", {
			type: "application/json",
		});
		const input = screen.getByTestId("license-file-input");
		await user.upload(input, file);

		await waitFor(() => {
			expect(mockService.importLicense).toHaveBeenCalledWith(file);
			expect(onLicenseImported).toHaveBeenCalledOnce();
		});
	});

	it("should show error on upload failure", async () => {
		const user = userEvent.setup();
		const mockService = createMockLicensingService({
			importLicense: vi.fn().mockRejectedValue(new Error("Invalid")),
		});

		render(
			<BlockedPage
				licensingService={mockService}
				onLicenseImported={vi.fn()}
				onLogout={vi.fn()}
			/>,
		);

		const file = new File(['{"key": "value"}'], "license.json", {
			type: "application/json",
		});
		const input = screen.getByTestId("license-file-input");
		await user.upload(input, file);

		await waitFor(() => {
			expect(screen.getByTestId("upload-error")).toBeInTheDocument();
		});
	});

	it("should reject non-JSON files", async () => {
		const user = userEvent.setup({ applyAccept: false });
		const mockService = createMockLicensingService();

		render(
			<BlockedPage
				licensingService={mockService}
				onLicenseImported={vi.fn()}
				onLogout={vi.fn()}
			/>,
		);

		const file = new File(["data"], "license.txt", {
			type: "text/plain",
		});
		const input = screen.getByTestId("license-file-input");
		await user.upload(input, file);

		await waitFor(() => {
			expect(screen.getByTestId("upload-error")).toBeInTheDocument();
			expect(screen.getByText("Please select a JSON file")).toBeInTheDocument();
		});

		expect(mockService.importLicense).not.toHaveBeenCalled();
	});
});
