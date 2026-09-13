import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../models/ILicenseStatus";
import type { SystemInfo } from "../../models/SystemInfo/SystemInfo";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import SurveyNudge from "./SurveyNudge";

/**
 * Slice 02, AC-05.6. describe.skip = RED scaffold; DELIVER enables it (ADR-025).
 *
 * The survey nudge shipped long before there was anything to collide with. Now there are two
 * unsolicited prompts on overlapping clocks, and one of them is a consent request - which may not
 * be put to somebody alongside an unrelated ask. So this component acquires one new obligation:
 * take the session's single slot when it decides to appear, and stand down when something else
 * already holds it.
 */

const FIXED_NOW = new Date("2026-06-01T00:00:00.000Z");

const daysBefore = (days: number): string =>
	new Date(FIXED_NOW.getTime() - days * 24 * 60 * 60 * 1000).toISOString();

const licenseStatus = (overrides?: Partial<ILicenseStatus>): ILicenseStatus => ({
	hasLicense: false,
	isValid: false,
	canUsePremiumFeatures: false,
	...overrides,
});

const systemInfo = (overrides?: Partial<SystemInfo>): SystemInfo =>
	({
		os: "test",
		runtime: "test",
		architecture: "test",
		processId: 0,
		databaseProvider: "sqlite",
		databaseConnection: null,
		logPath: null,
		installTimestamp: daysBefore(30),
		...overrides,
	}) as SystemInfo;

const renderNudge = () => {
	const context = createMockApiServiceContext({
		licensingService: {
			getLicenseStatus: vi.fn().mockResolvedValue(licenseStatus()),
			importLicense: vi.fn(),
			clearLicense: vi.fn(),
		},
		systemInfoService: {
			getSystemInfo: vi.fn().mockResolvedValue(systemInfo()),
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

const queryHeading = () =>
	screen.queryByRole("heading", { name: /help shape lighthouse/i });

describe.skip("SurveyNudge and the session's one prompt slot", () => {
	beforeEach(() => {
		sessionStorage.clear();
	});

	it("takes the slot when it appears", async () => {
		renderNudge();

		await waitFor(() => expect(queryHeading()).toBeInTheDocument());

		expect(sessionStorage.getItem("lighthouse:prompt-slot")).toBe(
			"survey-nudge",
		);
	});

	it("stands down when the usage data dialog already holds the session", async () => {
		sessionStorage.setItem("lighthouse:prompt-slot", "usage-data");

		renderNudge();

		await waitFor(() => expect(queryHeading()).not.toBeInTheDocument());
	});

	// Being eligible and being silenced are different states, and only one of them should leave a
	// claim behind. A nudge that took the slot while standing down would block the other prompt for
	// the rest of the session and nothing at all would be shown.
	it("leaves the holder's claim alone when it stands down", async () => {
		sessionStorage.setItem("lighthouse:prompt-slot", "usage-data");

		renderNudge();

		await waitFor(() => expect(queryHeading()).not.toBeInTheDocument());

		expect(sessionStorage.getItem("lighthouse:prompt-slot")).toBe("usage-data");
	});

	// The slot is for prompts that actually appear. Claiming it while ineligible would let a
	// fourteen-day-old instance silence the usage data dialog on behalf of a nudge nobody sees.
	it("claims nothing when it was never going to appear", async () => {
		const context = createMockApiServiceContext({
			licensingService: {
				getLicenseStatus: vi
					.fn()
					.mockResolvedValue(licenseStatus({ canUsePremiumFeatures: true })),
				importLicense: vi.fn(),
				clearLicense: vi.fn(),
			},
			systemInfoService: {
				getSystemInfo: vi.fn().mockResolvedValue(systemInfo()),
				getRefreshLogs: vi.fn(),
				getBackendSbom: vi.fn(),
				getFrontendSbom: vi.fn(),
			},
		});

		render(
			<ApiServiceContext.Provider value={context}>
				<SurveyNudge now={FIXED_NOW} />
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => expect(queryHeading()).not.toBeInTheDocument());

		expect(sessionStorage.getItem("lighthouse:prompt-slot")).toBeNull();
	});
});
