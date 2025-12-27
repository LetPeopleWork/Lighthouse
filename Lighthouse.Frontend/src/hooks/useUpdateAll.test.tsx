import { renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IApiServiceContext } from "../services/Api/ApiServiceContext";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { useUpdateAll } from "./useUpdateAll";

// Mock data
const mockTeams = [
	{ id: 1, name: "Team A" },
	{ id: 2, name: "Team B" },
];

const mockPortfolios = [
	{ id: 1, name: "Portfolio A" },
	{ id: 2, name: "Portfolio B" },
];

const mockGlobalUpdateStatus = {
	hasActiveUpdates: false,
	activeCount: 0,
};

// Mock services
const mockTeamService = {
	getTeams: vi.fn(),
	updateAllTeamData: vi.fn(),
};

const mockPortfolioService = {
	getPortfolios: vi.fn(),
	refreshFeaturesForAllPortfolios: vi.fn(),
};

const mockUpdateSubscriptionService = {
	getGlobalUpdateStatus: vi.fn().mockResolvedValue({
		hasActiveUpdates: false,
		activeCount: 0,
	}),
	subscribeToAllUpdates: vi.fn().mockResolvedValue(undefined),
	unsubscribeFromAllUpdates: vi.fn().mockResolvedValue(undefined),
	subscribeToTeamUpdates: vi.fn(),
	unsubscribeFromTeamUpdates: vi.fn(),
	subscribeToFeatureUpdates: vi.fn(),
	unsubscribeFromFeatureUpdates: vi.fn(),
};

const mockApiServiceContext: IApiServiceContext = {
	teamService: mockTeamService,
	portfolioService: mockPortfolioService,
	updateSubscriptionService: mockUpdateSubscriptionService,
} as unknown as IApiServiceContext;

const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
	<ApiServiceContext.Provider value={mockApiServiceContext}>
		{children}
	</ApiServiceContext.Provider>
);

describe("useUpdateAll", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mockTeamService.getTeams.mockResolvedValue(mockTeams);
		mockPortfolioService.getPortfolios.mockResolvedValue(mockPortfolios);
		mockUpdateSubscriptionService.getGlobalUpdateStatus.mockResolvedValue(
			mockGlobalUpdateStatus,
		);
	});

	it("should fetch global update status on mount", async () => {
		renderHook(() => useUpdateAll(), {
			wrapper: TestWrapper,
		});

		await waitFor(() => {
			expect(
				mockUpdateSubscriptionService.getGlobalUpdateStatus,
			).toHaveBeenCalledOnce();
		});
	});

	it("should subscribe to global updates", async () => {
		renderHook(() => useUpdateAll(), {
			wrapper: TestWrapper,
		});

		await waitFor(() => {
			expect(
				mockUpdateSubscriptionService.subscribeToAllUpdates,
			).toHaveBeenCalledOnce();
		});
	});

	it("should handle update all operation", async () => {
		mockTeamService.updateAllTeamData.mockResolvedValue(undefined);
		mockPortfolioService.refreshFeaturesForAllPortfolios.mockResolvedValue(
			undefined,
		);

		const { result } = renderHook(() => useUpdateAll(), {
			wrapper: TestWrapper,
		});

		await result.current.handleUpdateAll();

		expect(mockTeamService.updateAllTeamData).toHaveBeenCalledOnce();
		expect(
			mockPortfolioService.refreshFeaturesForAllPortfolios,
		).toHaveBeenCalledOnce();
	});

	it("should trigger update services without manually setting status", async () => {
		const { result } = renderHook(() => useUpdateAll(), {
			wrapper: TestWrapper,
		});

		// Mock the status service to return active updates
		mockUpdateSubscriptionService.getGlobalUpdateStatus.mockResolvedValue({
			hasActiveUpdates: true,
			activeCount: 4,
		});

		const updatePromise = result.current.handleUpdateAll();

		// The hook should not set status manually anymore - it relies on backend
		expect(result.current.globalUpdateStatus.hasActiveUpdates).toBe(false);

		await updatePromise;

		// Verify that the services were called
		expect(mockTeamService.updateAllTeamData).toHaveBeenCalledOnce();
		expect(
			mockPortfolioService.refreshFeaturesForAllPortfolios,
		).toHaveBeenCalledOnce();
	});

	it("should handle global update status errors gracefully", async () => {
		const consoleErrorSpy = vi
			.spyOn(console, "error")
			.mockImplementation(() => {});
		mockUpdateSubscriptionService.getGlobalUpdateStatus.mockRejectedValue(
			new Error("Service error"),
		);

		const { result } = renderHook(() => useUpdateAll(), {
			wrapper: TestWrapper,
		});

		await waitFor(() => {
			expect(consoleErrorSpy).toHaveBeenCalledWith(
				"Error fetching global update status:",
				expect.any(Error),
			);
		});

		expect(result.current.hasError).toBe(false); // Should not set hasError for status fetch failures

		consoleErrorSpy.mockRestore();
	});

	it("should handle update operation errors", async () => {
		const consoleErrorSpy = vi
			.spyOn(console, "error")
			.mockImplementation(() => {});
		mockTeamService.updateAllTeamData.mockRejectedValue(
			new Error("Update error"),
		);

		const { result } = renderHook(() => useUpdateAll(), {
			wrapper: TestWrapper,
		});

		await result.current.handleUpdateAll();

		await waitFor(() => {
			expect(result.current.hasError).toBe(true);
		});
		expect(consoleErrorSpy).toHaveBeenCalledWith(
			"Error updating all teams and portfolios:",
			expect.any(Error),
		);

		consoleErrorSpy.mockRestore();
	});

	it("should cleanup global subscription on unmount", async () => {
		const { unmount } = renderHook(() => useUpdateAll(), {
			wrapper: TestWrapper,
		});

		await waitFor(() => {
			expect(
				mockUpdateSubscriptionService.subscribeToAllUpdates,
			).toHaveBeenCalledOnce();
		});

		unmount();

		expect(
			mockUpdateSubscriptionService.unsubscribeFromAllUpdates,
		).toHaveBeenCalledOnce();
	});
});
