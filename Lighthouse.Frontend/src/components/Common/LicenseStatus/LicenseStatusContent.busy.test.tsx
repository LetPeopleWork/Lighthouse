import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "../../../tests/MockApiServiceProvider";
import LicenseStatusPopover from "./LicenseStatusPopover";

const licenseStatus: ILicenseStatus = {
	hasLicense: true,
	isValid: true,
	canUsePremiumFeatures: true,
	name: "Test User",
};

const renderWithLicensing = (licensingService: ILicensingService) => {
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		isRbacEnabled: true,
		isSystemAdmin: true,
		canCreateTeam: false,
		canCreatePortfolio: false,
	});
	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ licensingService, rbacService })}
		>
			<LicenseStatusPopover
				anchorEl={document.createElement("button")}
				onClose={vi.fn()}
				onLicenseImported={vi.fn()}
				isLoading={false}
				error={null}
				licenseStatus={licenseStatus}
			/>
		</ApiServiceContext.Provider>,
	);
};

const neverAnswers = <T,>() => new Promise<T>(() => {});

describe("one license change at a time", () => {
	it("while the license is being cleared, says so and offers no upload", async () => {
		const user = userEvent.setup();
		renderWithLicensing({
			getLicenseStatus: vi.fn(),
			importLicense: vi.fn(),
			clearLicense: vi.fn(() => neverAnswers<void>()),
		});

		await user.click(await screen.findByTestId("license-clear-button"));
		await user.click(
			await screen.findByRole("button", { name: /Clear License/i }),
		);

		await waitFor(() =>
			expect(screen.getByTestId("license-clear-button")).toHaveTextContent(
				"Clearing...",
			),
		);
		expect(screen.getByTestId("license-add-button")).toBeDisabled();
	});

	it("while a license is being uploaded, offers no clearing", async () => {
		const user = userEvent.setup();
		renderWithLicensing({
			getLicenseStatus: vi.fn(),
			importLicense: vi.fn(() => neverAnswers<ILicenseStatus>()),
			clearLicense: vi.fn(),
		});

		await user.click(await screen.findByTestId("license-add-button"));
		const fileInput = document.querySelector(
			'input[type="file"]',
		) as HTMLInputElement;
		await user.upload(
			fileInput,
			new File(['{"license": "data"}'], "license.json", {
				type: "application/json",
			}),
		);

		await waitFor(() =>
			expect(screen.getByTestId("license-clear-button")).toBeDisabled(),
		);
		expect(screen.getByTestId("license-add-button")).toBeDisabled();
	});
});
