import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockOptionalFeatureService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../tests/renderWithQueryClient";
import UsageDataTab from "./UsageDataTab";

const mockGetAllFeatures = vi.fn();
const mockUpdateFeature = vi.fn();
const mockGetLicenseStatus = vi.fn();

const theVeto = {
	id: 0,
	key: "UsageData",
	name: "Never send usage data",
	description:
		"While this is on, Lighthouse sends no usage data from this instance.",
	enabled: false,
	isPremium: false,
	isPreview: false,
};

const renderTheTab = () => {
	const optionalFeatureService = createMockOptionalFeatureService();
	optionalFeatureService.getAllFeatures = mockGetAllFeatures;
	optionalFeatureService.updateFeature = mockUpdateFeature;
	const licensingService = createMockLicensingService();
	licensingService.getLicenseStatus = mockGetLicenseStatus;

	renderWithQueryClient(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				optionalFeatureService,
				licensingService,
			})}
		>
			<TerminologyProvider>
				<UsageDataTab />
			</TerminologyProvider>
		</ApiServiceContext.Provider>,
	);
};

const theSwitch = async () =>
	(await screen.findByTestId("UsageData-toggle")).querySelector(
		"input",
	) as HTMLInputElement;

describe("Usage Data tab", () => {
	beforeEach(() => {
		vi.resetAllMocks();
		mockGetAllFeatures.mockResolvedValue([theVeto]);
		mockUpdateFeature.mockResolvedValue(undefined);
		mockGetLicenseStatus.mockResolvedValue({
			hasLicense: false,
			isValid: false,
			canUsePremiumFeatures: false,
		});
	});

	it("shows the row without a Premium or preview badge, operable on an instance without a licence", async () => {
		renderTheTab();

		expect(await theSwitch()).not.toBeDisabled();
		expect(screen.getByText("Never send usage data")).toBeVisible();
		expect(
			screen.queryByTestId("UsageData-premium-indicator"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("UsageData-preview-indicator"),
		).not.toBeInTheDocument();
	});

	it("shows a preview badge for a preview setting", async () => {
		mockGetAllFeatures.mockResolvedValue([{ ...theVeto, isPreview: true }]);

		renderTheTab();

		expect(
			await screen.findByTestId("UsageData-preview-indicator"),
		).toBeVisible();
	});

	it("moves the switch when it is clicked and sends the new value", async () => {
		renderTheTab();

		await userEvent.click(await theSwitch());

		await waitFor(async () => expect(await theSwitch()).toBeChecked());
		expect(mockUpdateFeature).toHaveBeenCalledWith(
			expect.objectContaining({ key: "UsageData", enabled: true }),
		);
	});

	it("puts the switch back when the write is refused", async () => {
		mockUpdateFeature.mockRejectedValue(new Error("refused"));

		renderTheTab();

		await userEvent.click(await theSwitch());

		await waitFor(() => expect(mockUpdateFeature).toHaveBeenCalledTimes(1));
		await waitFor(async () => expect(await theSwitch()).not.toBeChecked());
	});
});
