import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IOptionalFeature } from "../models/OptionalFeatures/OptionalFeature";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockOptionalFeatureService,
} from "../tests/MockApiServiceProvider";
import { useBehaviourSettings } from "./useBehaviourSettings";

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));
vi.mock("../services/UsageData/usageDataReporter", async (importOriginal) => ({
	...(await importOriginal<
		typeof import("../services/UsageData/usageDataReporter")
	>()),
	useUsageDataReporter: () => reportUsage,
}));

/** Both rows carry id 0, the identity the seeder really writes, so only the key tells them apart. */
const aSetting = (key: string, isPremium: boolean): IOptionalFeature => ({
	id: 0,
	key,
	name: key,
	description: "",
	enabled: false,
	isPremium,
	isPreview: false,
});

const ordering = aSetting("FeatureOrdering", true);
const veto = aSetting("UsageData", false);
const unnamed = aSetting("NonPremiumExample", false);

const mockGetAllFeatures = vi.fn();
const mockUpdateFeature = vi.fn();
const mockGetLicenseStatus = vi.fn();

const withProviders = ({ children }: { children: ReactNode }) => {
	const optionalFeatureService = createMockOptionalFeatureService();
	optionalFeatureService.getAllFeatures = mockGetAllFeatures;
	optionalFeatureService.updateFeature = mockUpdateFeature;
	const licensingService = createMockLicensingService();
	licensingService.getLicenseStatus = mockGetLicenseStatus;
	return (
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					optionalFeatureService,
					licensingService,
				})}
			>
				{children}
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);
};

const renderTheSettings = (...keys: string[]) =>
	renderHook(() => useBehaviourSettings(keys), { wrapper: withProviders });

const readyOf = (state: ReturnType<typeof useBehaviourSettings>) => {
	if (state.status !== "ready") throw new Error(`still ${state.status}`);
	return state;
};

const enabledKeys = (state: ReturnType<typeof useBehaviourSettings>) =>
	readyOf(state)
		.settings.filter((setting) => setting.enabled)
		.map((setting) => setting.key);

beforeEach(() => {
	vi.resetAllMocks();
	mockGetAllFeatures.mockResolvedValue([unnamed, veto, ordering]);
	mockUpdateFeature.mockResolvedValue(undefined);
	mockGetLicenseStatus.mockResolvedValue({
		hasLicense: true,
		isValid: true,
		canUsePremiumFeatures: true,
	});
});

describe("useBehaviourSettings", () => {
	it("picks rows by key, and switching one leaves the other with the same id alone", async () => {
		const { result } = renderTheSettings("UsageData", "FeatureOrdering");
		await waitFor(() => expect(result.current.status).toBe("ready"));
		expect(readyOf(result.current).settings.map((s) => s.key)).toEqual([
			"UsageData",
			"FeatureOrdering",
		]);

		await act(() => readyOf(result.current).toggle(veto));

		await waitFor(() =>
			expect(enabledKeys(result.current)).toEqual(["UsageData"]),
		);
	});

	it("puts the switch back and reports nothing when the write is refused", async () => {
		mockUpdateFeature.mockRejectedValue(new Error("refused"));
		const { result } = renderTheSettings("FeatureOrdering");
		await waitFor(() => expect(result.current.status).toBe("ready"));

		await act(() => readyOf(result.current).toggle(ordering));

		expect(enabledKeys(result.current)).toEqual([]);
		expect(reportUsage).not.toHaveBeenCalled();
	});

	it("reports a switch the server accepted, and nothing for a setting usage data has no name for", async () => {
		const { result } = renderTheSettings(
			"NonPremiumExample",
			"FeatureOrdering",
		);
		await waitFor(() => expect(result.current.status).toBe("ready"));

		await act(() => readyOf(result.current).toggle(unnamed));
		expect(reportUsage).not.toHaveBeenCalled();

		await act(() => readyOf(result.current).toggle(ordering));
		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith(
			expect.objectContaining({
				optionalFeature: "FeatureOrder",
				enabled: true,
			}),
		);
	});

	it("is loading until the settings arrive and failed when they cannot be read", async () => {
		mockGetAllFeatures.mockRejectedValue(new Error("down"));
		const { result } = renderTheSettings("FeatureOrdering");

		expect(result.current.status).toBe("loading");
		await waitFor(() => expect(result.current.status).toBe("failed"));
	});

	it("never reads an unread license as no Premium for a Premium row", async () => {
		mockGetLicenseStatus.mockRejectedValue(new Error("down"));
		const { result } = renderTheSettings("FeatureOrdering");

		await waitFor(() => expect(result.current.status).toBe("failed"));
	});

	it("needs the license as soon as one of the rows it shows is Premium", async () => {
		mockGetLicenseStatus.mockRejectedValue(new Error("down"));
		const { result } = renderTheSettings("UsageData", "FeatureOrdering");

		await waitFor(() => expect(result.current.status).toBe("failed"));
	});

	it("does not wait for the license when the row it shows is free, and grants no Premium without one", async () => {
		mockGetLicenseStatus.mockRejectedValue(new Error("down"));
		const { result } = renderTheSettings("UsageData");

		await waitFor(() => expect(result.current.status).toBe("ready"));
		expect(readyOf(result.current).canUsePremiumFeatures).toBe(false);
	});
});
