import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../models/ILicenseStatus";
import type { SystemInfo } from "../../models/SystemInfo/SystemInfo";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import type { SurveyNudgeAction } from "../../services/Api/SurveyNudgeService";
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

const renderNudge = (options?: {
	licenseStatus?: ILicenseStatus;
	systemInfo?: SystemInfo;
	nextEligibleAt?: string | null;
}) => {
	const recordAction = vi.fn().mockResolvedValue(undefined);

	const context = createMockApiServiceContext({
		licensingService: {
			getLicenseStatus: vi
				.fn()
				.mockResolvedValue(options?.licenseStatus ?? getMockLicenseStatus()),
			importLicense: vi.fn(),
			clearLicense: vi.fn(),
		},
		systemInfoService: {
			getSystemInfo: vi
				.fn()
				.mockResolvedValue(options?.systemInfo ?? getMockSystemInfo()),
			getRefreshLogs: vi.fn(),
			getBackendSbom: vi.fn(),
			getFrontendSbom: vi.fn(),
		},
		surveyNudgeService: {
			getState: vi
				.fn()
				.mockResolvedValue({ nextEligibleAt: options?.nextEligibleAt ?? null }),
			recordAction,
		},
	});

	const view = render(
		<ApiServiceContext.Provider value={context}>
			<SurveyNudge now={FIXED_NOW} />
		</ApiServiceContext.Provider>,
	);

	return { ...view, recordAction };
};

describe("SurveyNudge cadence and three choices", () => {
	it.each([0, 14, 100, 3650])(
		"never renders for a premium instance at install age %i days",
		async (installAgeInDays) => {
			renderNudge({
				licenseStatus: getMockLicenseStatus({ canUsePremiumFeatures: true }),
				systemInfo: getMockSystemInfo({
					installTimestamp: daysBefore(installAgeInDays),
				}),
			});

			await waitFor(() => {
				expect(
					screen.queryByRole("heading", { name: /help shape lighthouse/i }),
				).not.toBeInTheDocument();
			});
		},
	);

	it("stays quiet while the server-computed next-eligible instant is still in the future", async () => {
		renderNudge({
			nextEligibleAt: new Date(
				FIXED_NOW.getTime() + 7 * 24 * 60 * 60 * 1000,
			).toISOString(),
		});

		await waitFor(() => {
			expect(
				screen.queryByRole("heading", { name: /help shape lighthouse/i }),
			).not.toBeInTheDocument();
		});
	});

	it("renders the opt-in, three-choice link-out card for an eligible non-premium instance", async () => {
		renderNudge();

		const heading = await screen.findByRole("heading", {
			name: /help shape lighthouse/i,
		});
		const card = heading.closest("[role='dialog'], section, div");

		expect(screen.getByText(/completely optional/i)).toBeInTheDocument();
		expect(screen.getByText(/premium trial/i)).toBeInTheDocument();

		const takeSurvey = screen.getByRole("link", { name: /take the survey/i });
		expect(takeSurvey).toHaveAttribute("href", SURVEY_URL);
		expect(card).not.toHaveTextContent(/recommend lighthouse to a colleague/i);

		expect(
			screen.getByRole("button", { name: /remind me later/i }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: /not interested/i }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: /dismiss/i }),
		).toBeInTheDocument();
	});

	it.each<[RegExp, SurveyNudgeAction]>([
		[/take the survey/i, "TakeSurvey"],
		[/remind me later/i, "RemindLater"],
		[/not interested/i, "NoInterest"],
		[/dismiss/i, "RemindLater"],
	])(
		"records the %s choice and hides the card",
		async (label, expectedAction) => {
			const { recordAction } = renderNudge();

			const heading = await screen.findByRole("heading", {
				name: /help shape lighthouse/i,
			});

			const control =
				screen.queryByRole("button", { name: label }) ??
				screen.getByRole("link", { name: label });
			await userEvent.click(control);

			expect(recordAction).toHaveBeenCalledWith(expectedAction);
			await waitFor(() => {
				expect(heading).not.toBeInTheDocument();
			});
		},
	);
});
