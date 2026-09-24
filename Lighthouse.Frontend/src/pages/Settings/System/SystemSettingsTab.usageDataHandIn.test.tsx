import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UsageDataConsentProvider } from "../../../hooks/useUsageDataConsent";
import type { IUsageDataState } from "../../../models/UsageData/UsageData";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IOptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import type { IUsageDataService } from "../../../services/Api/UsageDataService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import { forgetWhatWasNoticed } from "../../../services/UsageData/usageDataBuffer";
import { useUsageDataEventDetector } from "../../../services/UsageData/usageDataEvents";
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
import SystemSettingsTab from "./SystemSettingsTab";

/**
 * The whole way from the switch to what this browser posts: the real consent answer, the real
 * reporter, the real detector that hands things in. Only the server is stood in for.
 *
 * The screen's own test replaces the reporter, and the server's scenarios post the message by hand.
 * Between the two sits everything that decides whether a switch actually leaves this browser, and
 * each side's test passes whatever happens there - so this is the one that can tell.
 *
 * Pending until the event exists.
 */

const TOKEN_STORAGE_KEY = "lighthouse:usagedata:consent";

const mockOptionalFeatureService: IOptionalFeatureService =
	createMockOptionalFeatureService();

const mockLicensingService = createMockLicensingService();

const mockBlackoutPeriodService = createMockBlackoutPeriodService();

const aBrowserThatAgreed: IUsageDataState = {
	sending: true,
	decision: "Granted",
	mayAsk: false,
	reAskAfterDays: 90,
	administratorDisabled: false,
};

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

const TheDetector = () => {
	useUsageDataEventDetector();
	return null;
};

const renderTheSystemSettingsAsTheApplicationDoes = (
	usageDataService: IUsageDataService,
) => {
	const context = createMockApiServiceContext({
		settingsService: createMockSettingsService(),
		optionalFeatureService: mockOptionalFeatureService,
		terminologyService: createMockTerminologyService(),
		licensingService: mockLicensingService,
		blackoutPeriodService: mockBlackoutPeriodService,
		encryptionService: createMockEncryptionService(),
		systemInfoService: createMockSystemInfoService(),
		usageDataService,
	});

	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
	});

	render(
		<MemoryRouter initialEntries={["/settings"]}>
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={context}>
					<UsageDataConsentProvider>
						<TerminologyProvider>
							<TheDetector />
							<SystemSettingsTab />
						</TerminologyProvider>
					</UsageDataConsentProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>
		</MemoryRouter>,
	);
};

const aServerThatAnswers = (
	getState: IUsageDataService["getState"],
): IUsageDataService => ({
	getState,
	recordDecision: vi.fn().mockResolvedValue("a-token"),
	revoke: vi.fn().mockResolvedValue(undefined),
	acknowledgeAsked: vi.fn().mockResolvedValue(undefined),
	postEvents: vi.fn().mockResolvedValue(undefined),
});

const theSwitchFor = async (key: string): Promise<HTMLInputElement> =>
	waitFor(() => {
		const input = screen.getByTestId(`${key}-toggle`).querySelector("input");
		expect(input).not.toBeNull();
		return input as HTMLInputElement;
	});

/**
 * Somebody switching away from the tab, which hands in whatever is waiting. Done on every poll
 * rather than once, so a report that arrives a moment after the switch is still handed in.
 */
const untilHandedIn = async (
	usageDataService: IUsageDataService,
	expected: object,
) => {
	vi.spyOn(document, "visibilityState", "get").mockReturnValue("hidden");

	await waitFor(() => {
		document.dispatchEvent(new Event("visibilitychange"));
		expect(
			vi
				.mocked(usageDataService.postEvents)
				.mock.calls.flatMap(([, events]) => events),
		).toContainEqual(expect.objectContaining(expected));
	});
};

describe("A behaviour setting switch, as this browser hands it in", () => {
	beforeEach(() => {
		forgetWhatWasNoticed();
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");

		mockOptionalFeatureService.updateFeature = vi
			.fn()
			.mockResolvedValue(undefined);
		mockLicensingService.getLicenseStatus = vi.fn().mockResolvedValue({
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		});
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);
	});

	afterEach(() => {
		localStorage.clear();
		vi.restoreAllMocks();
	});

	// @driving_port @AC-1.1 - the parts the server needs have to survive every step between the
	// switch and the post, and nothing on the way is checked against them.
	it.skip("hands in which setting was switched and which way", async () => {
		mockOptionalFeatureService.getAllFeatures = vi
			.fn()
			.mockResolvedValue([theOrderingSetting]);
		const usageDataService = aServerThatAnswers(
			vi.fn().mockResolvedValue(aBrowserThatAgreed),
		);

		renderTheSystemSettingsAsTheApplicationDoes(usageDataService);
		await waitFor(() => expect(usageDataService.getState).toHaveBeenCalled());
		await userEvent.click(await theSwitchFor("FeatureOrdering"));

		await untilHandedIn(usageDataService, {
			name: "OptionalFeatureToggled",
			optionalFeature: "FeatureOrder",
			enabled: true,
		});
	});
});
