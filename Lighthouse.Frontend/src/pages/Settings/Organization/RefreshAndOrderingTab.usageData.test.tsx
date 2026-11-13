import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import type { IOptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import { actAndSettle } from "../../../tests/actAndSettle";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
	createMockEncryptionService,
	createMockLicensingService,
	createMockOptionalFeatureService,
	createMockSettingsService,
	createMockSystemInfoService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import UsageDataTab from "../System/UsageDataTab";
import RefreshAndOrderingTab from "./RefreshAndOrderingTab";

/**
 * What the settings screen reports when somebody switches a behaviour setting, and when it reports
 * nothing. Whether this browser agreed is the reporter's business, not this screen's, so the reporter
 * stands in here and every call it receives is one the screen chose to make.
 *
 * The screen flips a switch before the server has answered. The report has to wait for the answer:
 * a switch the server refused did not happen, and counting it would put a change in the numbers
 * that no instance ever made.
 */

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));
vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

const mockGetAllFeatures = vi.fn();
const mockUpdateFeature = vi.fn();
const mockOptionalFeatureService: IOptionalFeatureService =
	createMockOptionalFeatureService();
mockOptionalFeatureService.getAllFeatures = mockGetAllFeatures;
mockOptionalFeatureService.updateFeature = mockUpdateFeature;

const mockGetLicenseStatus = vi.fn();
const mockLicensingService: ILicensingService = createMockLicensingService();
mockLicensingService.getLicenseStatus = mockGetLicenseStatus;

const mockBlackoutPeriodService = createMockBlackoutPeriodService();
mockBlackoutPeriodService.getAll = vi.fn();

const theOrderingSetting = {
	id: 0,
	key: "FeatureOrdering",
	name: "Let Lighthouse own the order of your {{features}}",
	description:
		"While this is on, Lighthouse forecasts your {{features}} in the order you gave them.",
	enabled: false,
	isPremium: true,
	isPreview: false,
};

const theVeto = {
	id: 0,
	key: "UsageData",
	name: "Never send usage data",
	description:
		"While this is on, Lighthouse sends no usage data from this instance.",
	enabled: false,
	isPremium: true,
	isPreview: false,
};

const theOrderingSettingSwitchedOn = {
	name: "OptionalFeatureToggled",
	optionalFeature: "FeatureOrder",
	enabled: true,
};

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockContext = createMockApiServiceContext({
		settingsService: createMockSettingsService(),
		optionalFeatureService: mockOptionalFeatureService,
		terminologyService: createMockTerminologyService(),
		licensingService: mockLicensingService,
		blackoutPeriodService: mockBlackoutPeriodService,
		encryptionService: createMockEncryptionService(),
		systemInfoService: createMockSystemInfoService(),
	});

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	return (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={mockContext}>
				<TerminologyProvider>{children}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);
};

const renderTheOrderingTab = () => {
	render(
		<MockApiServiceProvider>
			<RefreshAndOrderingTab />
		</MockApiServiceProvider>,
	);
};

const renderTheUsageDataTab = () => {
	render(
		<MockApiServiceProvider>
			<UsageDataTab />
		</MockApiServiceProvider>,
	);
};

const theSwitchFor = async (key: string): Promise<HTMLInputElement> =>
	waitFor(() => {
		const input = screen.getByTestId(`${key}-toggle`).querySelector("input");
		expect(input).not.toBeNull();
		return input as HTMLInputElement;
	});

const givenTheseSettings = (...settings: object[]) => {
	mockGetAllFeatures.mockResolvedValue(settings);
};

describe("Reporting a behaviour setting being switched", () => {
	beforeEach(() => {
		vi.resetAllMocks();

		vi.mocked(mockBlackoutPeriodService.getAll).mockResolvedValue([]);
		mockGetLicenseStatus.mockResolvedValue({
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		});
		mockUpdateFeature.mockResolvedValue(undefined);
		givenTheseSettings(theOrderingSetting);
	});

	// the answer is what makes the switch real, so the report
	// waits for it. Holding the answer back is what shows the screen is waiting rather than reporting
	// the click.
	it("reports the ordering setting switched on once the server has accepted it", async () => {
		let acceptTheSwitch: () => void = () => undefined;
		mockUpdateFeature.mockReturnValue(
			new Promise<void>((resolve) => {
				acceptTheSwitch = resolve;
			}),
		);

		renderTheOrderingTab();
		await userEvent.click(await theSwitchFor("FeatureOrdering"));

		expect(mockUpdateFeature).toHaveBeenCalled();
		expect(reportUsage).not.toHaveBeenCalled();

		await actAndSettle(() => {
			acceptTheSwitch();
		});

		await waitFor(() => {
			expect(reportUsage).toHaveBeenCalledTimes(1);
		});
		expect(reportUsage).toHaveBeenCalledWith(theOrderingSettingSwitchedOn);
	});

	// the direction is the new state, read from the row as it was before the
	// click. Reporting the old one would count every switch backwards.
	it("reports it switched back off as one more event", async () => {
		renderTheOrderingTab();

		await userEvent.click(await theSwitchFor("FeatureOrdering"));
		await waitFor(() => expect(reportUsage).toHaveBeenCalledTimes(1));

		await userEvent.click(await theSwitchFor("FeatureOrdering"));
		await waitFor(() => expect(reportUsage).toHaveBeenCalledTimes(2));

		expect(reportUsage.mock.calls).toEqual([
			[theOrderingSettingSwitchedOn],
			[{ ...theOrderingSettingSwitchedOn, enabled: false }],
		]);
	});

	// the veto is never reported, whichever way it goes. Switching it
	// on could never arrive, since nothing leaves from that moment, and counting only the times it
	// was lifted would read as people forever lifting it.
	it.each([
		[false, "on"],
		[true, "off"],
	])(
		"reports nothing when the veto, stored as %s, is switched %s",
		async (storedAs) => {
			givenTheseSettings({ ...theVeto, enabled: storedAs }, theOrderingSetting);

			renderTheUsageDataTab();
			await userEvent.click(await theSwitchFor("UsageData"));
			await waitFor(() => expect(mockUpdateFeature).toHaveBeenCalledTimes(1));

			expect(reportUsage).not.toHaveBeenCalled();
		},
	);

	// a switch the server refused did not happen. The accepted switch
	// that follows is what shows this screen reports at all; without it, a screen that never reports
	// would pass.
	it("reports nothing for a switch the server refused", async () => {
		mockUpdateFeature.mockRejectedValueOnce(new Error("refused"));

		renderTheOrderingTab();
		await userEvent.click(await theSwitchFor("FeatureOrdering"));

		await waitFor(async () =>
			expect(await theSwitchFor("FeatureOrdering")).not.toBeChecked(),
		);
		expect(reportUsage).not.toHaveBeenCalled();

		await userEvent.click(await theSwitchFor("FeatureOrdering"));

		await waitFor(() => expect(reportUsage).toHaveBeenCalledTimes(1));
		expect(reportUsage).toHaveBeenCalledWith(theOrderingSettingSwitchedOn);
	});
});
