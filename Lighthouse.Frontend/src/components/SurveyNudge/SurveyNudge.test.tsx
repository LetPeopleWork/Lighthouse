import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../models/ILicenseStatus";
import type { SystemInfo } from "../../models/SystemInfo/SystemInfo";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import SurveyNudge, { SURVEY_URL } from "./SurveyNudge";

const FIXED_NOW = new Date("2026-06-01T00:00:00.000Z");

const daysBefore = (days: number): string =>
	new Date(FIXED_NOW.getTime() - days * 24 * 60 * 60 * 1000).toISOString();

const getMockLicenseStatus = (
	overrides?: Partial<ILicenseStatus>,
): ILicenseStatus => ({
	hasLicense: false,
	isValid: false,
	canUsePremiumFeatures: false,
	...overrides,
});

const getMockSystemInfo = (overrides?: Partial<SystemInfo>): SystemInfo => ({
	os: "test",
	runtime: "test",
	architecture: "test",
	processId: 0,
	databaseProvider: "sqlite",
	databaseConnection: null,
	logPath: null,
	installTimestamp: daysBefore(30),
	...overrides,
});

const renderNudge = (options: {
	licenseStatus: ILicenseStatus;
	systemInfo: SystemInfo;
}) => {
	const context = createMockApiServiceContext({
		licensingService: {
			getLicenseStatus: vi.fn().mockResolvedValue(options.licenseStatus),
			importLicense: vi.fn(),
			clearLicense: vi.fn(),
		},
		systemInfoService: {
			getSystemInfo: vi.fn().mockResolvedValue(options.systemInfo),
			getRefreshLogs: vi.fn(),
			getBackendSbom: vi.fn(),
			getFrontendSbom: vi.fn(),
		},
	});

	return render(
		<ApiServiceContext.Provider value={context}>
			<SurveyNudge now={FIXED_NOW} />
		</ApiServiceContext.Provider>,
	);
};

describe("SurveyNudge", () => {
	it.each([
		0, 14, 100, 3650,
	])("never renders for a premium instance at install age %i days", async (installAgeInDays) => {
		renderNudge({
			licenseStatus: getMockLicenseStatus({ canUsePremiumFeatures: true }),
			systemInfo: getMockSystemInfo({
				installTimestamp: daysBefore(installAgeInDays),
			}),
		});

		await waitFor(() => {
			expect(
				screen.queryByRole("button", { name: /dismiss/i }),
			).not.toBeInTheDocument();
		});
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	it("does not render for a non-premium instance younger than two weeks", async () => {
		renderNudge({
			licenseStatus: getMockLicenseStatus(),
			systemInfo: getMockSystemInfo({ installTimestamp: daysBefore(3) }),
		});

		await waitFor(() => {
			expect(
				screen.queryByRole("button", { name: /dismiss/i }),
			).not.toBeInTheDocument();
		});
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	it("renders a dismissible link-out for a non-premium instance at least two weeks old", async () => {
		renderNudge({
			licenseStatus: getMockLicenseStatus(),
			systemInfo: getMockSystemInfo({ installTimestamp: daysBefore(20) }),
		});

		const alert = await screen.findByRole("alert");
		const surveyLink = within(alert).getByRole("link");

		expect(surveyLink).toHaveAttribute("href", SURVEY_URL);
		expect(alert).not.toHaveTextContent(/how many years/i);

		await userEvent.click(
			within(alert).getByRole("button", { name: /dismiss/i }),
		);

		await waitFor(() => {
			expect(screen.queryByRole("alert")).not.toBeInTheDocument();
		});
	});
});
