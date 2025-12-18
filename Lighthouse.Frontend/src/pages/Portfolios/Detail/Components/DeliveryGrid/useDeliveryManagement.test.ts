import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import { Feature } from "../../../../../models/Feature";
import { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import { useDeliveryManagement } from "./useDeliveryManagement";

// Mock the context
const mockDeliveryService = {
	getByPortfolio: vi.fn(),
	create: vi.fn(),
	delete: vi.fn(),
};

const mockFeatureService = createMockFeatureService();
const mockGetFeaturesByIds = vi.fn();
mockFeatureService.getFeaturesByIds = mockGetFeaturesByIds;

const mockApiServiceContext = createMockApiServiceContext({
	deliveryService: {
		getByPortfolio: mockDeliveryService.getByPortfolio,
		create: mockDeliveryService.create,
		update: vi.fn(),
		delete: mockDeliveryService.delete,
	},
	featureService: mockFeatureService,
});

// Mock the error handler
const mockShowError = vi.fn();
vi.mock(
	"../../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler",
	() => ({
		useErrorSnackbar: () => ({ showError: mockShowError }),
	}),
);

// Mock useContext
vi.mock("react", async () => {
	const actual = await vi.importActual("react");
	return {
		...actual,
		useContext: () => mockApiServiceContext,
	};
});

describe("useDeliveryManagement", () => {
	const getMockPortfolio = (overrides?: Partial<Portfolio>): Portfolio => {
		const portfolio = new Portfolio();
		portfolio.id = 1;
		portfolio.name = "Test Portfolio";
		portfolio.features = [];
		portfolio.involvedTeams = [];
		portfolio.tags = [];
		portfolio.totalWorkItems = 0;
		portfolio.remainingWorkItems = 0;
		portfolio.forecasts = [];

		return Object.assign(portfolio, overrides);
	};

	const getMockDelivery = (overrides?: Partial<Delivery>): Delivery => {
		const delivery = new Delivery();
		delivery.id = 1;
		delivery.name = "Test Delivery";
		delivery.date = "2024-12-31";
		delivery.portfolioId = 1;
		delivery.features = [1, 2];
		delivery.likelihoodPercentage = 85;
		delivery.progress = 60;
		delivery.remainingWork = 20;
		delivery.totalWork = 50;
		delivery.featureLikelihoods = [];

		return Object.assign(delivery, overrides);
	};

	const getMockFeature = (overrides?: Partial<Feature>): Feature => {
		const feature = new Feature();
		feature.id = 1;
		feature.name = "Test Feature";
		feature.lastUpdated = new Date();
		feature.isUsingDefaultFeatureSize = false;
		feature.size = 5;
		feature.owningTeam = "Team A";
		feature.remainingWork = {};
		feature.totalWork = {};
		feature.projects = [];
		feature.forecasts = [];
		feature.state = "Active";
		feature.stateCategory = "Doing";
		feature.url = "http://example.com";

		return Object.assign(feature, overrides);
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Initial State", () => {
		it("should initialize with correct default state", async () => {
			const portfolio = getMockPortfolio();
			mockDeliveryService.getByPortfolio.mockResolvedValue([]);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			expect(result.current.deliveries).toEqual([]);
			expect(result.current.isLoading).toBe(true);
			expect(result.current.showCreateModal).toBe(false);
			expect(result.current.selectedDelivery).toBeNull();
			expect(result.current.deleteDialogOpen).toBe(false);
			expect(result.current.deliveryToDelete).toBeNull();
			expect(result.current.expandedDeliveries).toEqual(new Set());
			expect(result.current.loadedFeatures).toEqual(new Map());
			expect(result.current.loadingFeaturesByDelivery).toEqual(new Set());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});
		});

		it("should fetch deliveries on mount", async () => {
			const portfolio = getMockPortfolio();
			const deliveries = [
				getMockDelivery({ id: 1 }),
				getMockDelivery({ id: 2 }),
			];
			mockDeliveryService.getByPortfolio.mockResolvedValue(deliveries);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(mockDeliveryService.getByPortfolio).toHaveBeenCalledWith(
				portfolio.id,
			);
			expect(result.current.deliveries).toEqual(deliveries);
		});

		it("should handle fetch deliveries error", async () => {
			const portfolio = getMockPortfolio();
			mockDeliveryService.getByPortfolio.mockRejectedValue(
				new Error("Network error"),
			);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(mockShowError).toHaveBeenCalledWith("Failed to fetch deliveries");
			expect(result.current.deliveries).toEqual([]);
		});
	});

	describe("Modal Management", () => {
		it("should open create modal when handleAddDelivery is called", async () => {
			const portfolio = getMockPortfolio();
			mockDeliveryService.getByPortfolio.mockResolvedValue([]);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleAddDelivery();
			});

			expect(result.current.showCreateModal).toBe(true);
		});

		it("should close create modal when handleCloseCreateModal is called", async () => {
			const portfolio = getMockPortfolio();
			mockDeliveryService.getByPortfolio.mockResolvedValue([]);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleAddDelivery();
			});
			expect(result.current.showCreateModal).toBe(true);

			act(() => {
				result.current.handleCloseCreateModal();
			});
			expect(result.current.showCreateModal).toBe(false);
		});

		it("should set selected delivery when handleEditDelivery is called", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery();
			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleEditDelivery(delivery);
			});

			expect(result.current.selectedDelivery).toEqual(delivery);
		});

		it("should clear selected delivery when handleCloseEditModal is called", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery();
			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleEditDelivery(delivery);
			});
			expect(result.current.selectedDelivery).toEqual(delivery);

			act(() => {
				result.current.handleCloseEditModal();
			});
			expect(result.current.selectedDelivery).toBeNull();
		});
	});

	describe("Delete Delivery", () => {
		it("should open delete dialog when handleDeleteDelivery is called", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery();
			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleDeleteDelivery(delivery);
			});

			expect(result.current.deleteDialogOpen).toBe(true);
			expect(result.current.deliveryToDelete).toEqual(delivery);
		});

		it("should delete delivery when confirmation is accepted", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery({ id: 1 });
			const remainingDeliveries = [getMockDelivery({ id: 2 })];

			mockDeliveryService.getByPortfolio
				.mockResolvedValueOnce([delivery, ...remainingDeliveries])
				.mockResolvedValueOnce(remainingDeliveries);
			mockDeliveryService.delete.mockResolvedValue(null);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleDeleteDelivery(delivery);
			});
			expect(result.current.deliveryToDelete).toEqual(delivery);

			await act(async () => {
				await result.current.handleDeleteConfirmation(true);
			});

			expect(mockDeliveryService.delete).toHaveBeenCalledWith(delivery.id);
			expect(result.current.deleteDialogOpen).toBe(false);
			expect(result.current.deliveryToDelete).toBeNull();

			await waitFor(() => {
				expect(result.current.deliveries).toEqual(remainingDeliveries);
			});
		});

		it("should clean up expansion state when delivery is deleted", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery({ id: 1 });
			const features = [getMockFeature({ id: 1 }), getMockFeature({ id: 2 })];

			mockDeliveryService.getByPortfolio
				.mockResolvedValueOnce([delivery])
				.mockResolvedValueOnce([]);
			mockDeliveryService.delete.mockResolvedValue(null);
			mockGetFeaturesByIds.mockResolvedValue(features);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			// Expand the delivery to load features
			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});

			await waitFor(() => {
				expect(result.current.expandedDeliveries.has(delivery.id)).toBe(true);
				expect(result.current.loadedFeatures.has(delivery.id)).toBe(true);
			});

			// Delete the delivery
			act(() => {
				result.current.handleDeleteDelivery(delivery);
			});

			await act(async () => {
				await result.current.handleDeleteConfirmation(true);
			});

			await waitFor(() => {
				expect(result.current.expandedDeliveries.has(delivery.id)).toBe(false);
				expect(result.current.loadedFeatures.has(delivery.id)).toBe(false);
			});
		});

		it("should not delete delivery when confirmation is rejected", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery();
			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleDeleteDelivery(delivery);
			});

			await act(async () => {
				await result.current.handleDeleteConfirmation(false);
			});

			expect(mockDeliveryService.delete).not.toHaveBeenCalled();
			expect(result.current.deleteDialogOpen).toBe(false);
			expect(result.current.deliveryToDelete).toBeNull();
		});

		it("should handle delete delivery error", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery();
			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);
			mockDeliveryService.delete.mockRejectedValue(new Error("Delete failed"));

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleDeleteDelivery(delivery);
			});

			await act(async () => {
				await result.current.handleDeleteConfirmation(true);
			});

			expect(mockShowError).toHaveBeenCalledWith("Failed to delete delivery");
			expect(result.current.deleteDialogOpen).toBe(false);
			expect(result.current.deliveryToDelete).toBeNull();
		});
	});

	describe("Create Delivery", () => {
		it("should create delivery successfully", async () => {
			const portfolio = getMockPortfolio();
			const deliveryData = {
				name: "New Delivery",
				date: "2024-12-31",
				featureIds: [1, 2],
			};
			const newDelivery = getMockDelivery(deliveryData);

			mockDeliveryService.getByPortfolio
				.mockResolvedValueOnce([])
				.mockResolvedValueOnce([newDelivery]);
			mockDeliveryService.create.mockResolvedValue(null);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleAddDelivery();
			});
			expect(result.current.showCreateModal).toBe(true);

			await act(async () => {
				await result.current.handleCreateDelivery(deliveryData);
			});

			expect(mockDeliveryService.create).toHaveBeenCalledWith(
				portfolio.id,
				deliveryData.name,
				new Date(deliveryData.date),
				deliveryData.featureIds,
			);
			expect(result.current.showCreateModal).toBe(false);

			await waitFor(() => {
				expect(result.current.deliveries).toEqual([newDelivery]);
			});
		});

		it("should handle create delivery error", async () => {
			const portfolio = getMockPortfolio();
			const deliveryData = {
				name: "New Delivery",
				date: "2024-12-31",
				featureIds: [1, 2],
			};

			mockDeliveryService.getByPortfolio.mockResolvedValue([]);
			mockDeliveryService.create.mockRejectedValue(new Error("Create failed"));

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			await act(async () => {
				await result.current.handleCreateDelivery(deliveryData);
			});

			expect(mockShowError).toHaveBeenCalledWith("Failed to create delivery");
		});
	});

	describe("Delivery Update", () => {
		it("should update delivery and refresh list", async () => {
			const portfolio = getMockPortfolio();
			const deliveryData = {
				id: 1,
				name: "Updated Delivery",
				date: "2025-12-25",
				featureIds: [1, 2],
			};

			mockDeliveryService.getByPortfolio.mockResolvedValue([]);
			const updateSpy = vi.fn().mockResolvedValue(undefined);
			mockApiServiceContext.deliveryService.update = updateSpy;
			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleEditDelivery(getMockDelivery({ id: 1 }));
			});

			expect(result.current.selectedDelivery).not.toBeNull();

			await act(async () => {
				await result.current.handleUpdateDelivery(deliveryData);
			});

			expect(updateSpy).toHaveBeenCalledWith(
				deliveryData.id,
				deliveryData.name,
				new Date("2025-12-25"),
				[1, 2],
			);

			expect(result.current.selectedDelivery).toBeNull();
			expect(mockDeliveryService.getByPortfolio).toHaveBeenCalledTimes(2); // Initial load + refresh
		});

		it("should handle update delivery error", async () => {
			const portfolio = getMockPortfolio();
			const deliveryData = {
				id: 1,
				name: "Updated Delivery",
				date: "2025-12-25",
				featureIds: [1],
			};

			mockDeliveryService.getByPortfolio.mockResolvedValue([]);
			const updateSpy = vi.fn().mockRejectedValue(new Error("Update failed"));
			mockApiServiceContext.deliveryService.update = updateSpy;
			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleEditDelivery(getMockDelivery({ id: 1 }));
			});

			await act(async () => {
				await result.current.handleUpdateDelivery(deliveryData);
			});

			expect(mockShowError).toHaveBeenCalledWith("Failed to update delivery");
			expect(result.current.selectedDelivery).not.toBeNull(); // Should remain open on error
		});

		it("should clear cached features when updating delivery and reload if expanded", async () => {
			const portfolio = getMockPortfolio();
			const deliveryData = {
				id: 1,
				name: "Updated Delivery",
				date: "2025-12-25",
				featureIds: [3, 4], // Different feature IDs to simulate changed features
			};

			const mockDelivery = getMockDelivery({ id: 1, features: [1, 2] });
			const updatedMockDelivery = getMockDelivery({ id: 1, features: [3, 4] });
			const mockFeatures = [
				getMockFeature({ id: 1 }),
				getMockFeature({ id: 2 }),
			];
			const updatedFeatures = [
				getMockFeature({ id: 3 }),
				getMockFeature({ id: 4 }),
			];

			// Track call counts
			let getByPortfolioCallCount = 0;
			let getFeaturesByIdsCallCount = 0;

			// Setup delivery service
			mockDeliveryService.getByPortfolio.mockImplementation(() => {
				getByPortfolioCallCount++;
				// First call: initial load
				// Second call: fetchDeliveries() in handleUpdateDelivery
				// Third call: direct call in handleUpdateDelivery for feature reload
				if (getByPortfolioCallCount <= 2) {
					return Promise.resolve([mockDelivery]);
				}
				return Promise.resolve([updatedMockDelivery]);
			});

			// Setup feature service to return different features based on IDs
			mockGetFeaturesByIds.mockImplementation((ids) => {
				getFeaturesByIdsCallCount++;
				if (JSON.stringify(ids) === JSON.stringify([1, 2])) {
					return Promise.resolve(mockFeatures);
				}
				if (JSON.stringify(ids) === JSON.stringify([3, 4])) {
					return Promise.resolve(updatedFeatures);
				}
				return Promise.resolve([]);
			});

			const updateSpy = vi.fn().mockResolvedValue(undefined);
			mockApiServiceContext.deliveryService.update = updateSpy;

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			// Expand delivery to load features into cache
			act(() => {
				result.current.handleToggleExpanded(1);
			});

			await waitFor(() => {
				expect(result.current.loadedFeatures.has(1)).toBe(true);
				expect(result.current.loadedFeatures.get(1)).toEqual(mockFeatures);
			});

			expect(getFeaturesByIdsCallCount).toBe(1);
			expect(getByPortfolioCallCount).toBe(1);

			// Update the delivery
			act(() => {
				result.current.handleEditDelivery(mockDelivery);
			});

			await act(async () => {
				await result.current.handleUpdateDelivery(deliveryData);
			});

			// Verify update was called
			expect(updateSpy).toHaveBeenCalledWith(
				deliveryData.id,
				deliveryData.name,
				new Date("2025-12-25"),
				[3, 4],
			);

			// Should have called getByPortfolio twice more (fetchDeliveries + direct call)
			expect(getByPortfolioCallCount).toBe(3);

			// Wait for features to be reloaded with updated feature IDs
			await waitFor(
				() => {
					expect(result.current.loadedFeatures.has(1)).toBe(true);
					const loadedFeatures = result.current.loadedFeatures.get(1);
					expect(loadedFeatures).toEqual(updatedFeatures);
				},
				{ timeout: 3000 },
			);

			// Should have called getFeaturesByIds twice (initial + reload)
			expect(getFeaturesByIdsCallCount).toBe(2);
		});
	});

	describe("Feature Expansion", () => {
		it("should toggle expansion state", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery({ id: 1, features: [1, 2] });
			const features = [getMockFeature({ id: 1 }), getMockFeature({ id: 2 })];

			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);
			mockGetFeaturesByIds.mockResolvedValue(features);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			// Initially not expanded
			expect(result.current.expandedDeliveries.has(delivery.id)).toBe(false);

			// Expand
			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});
			expect(result.current.expandedDeliveries.has(delivery.id)).toBe(true);

			// Collapse
			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});
			expect(result.current.expandedDeliveries.has(delivery.id)).toBe(false);
		});

		it("should load features when delivery is expanded", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery({ id: 1, features: [1, 2] });
			const features = [getMockFeature({ id: 1 }), getMockFeature({ id: 2 })];

			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);
			mockGetFeaturesByIds.mockResolvedValue(features);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});

			expect(mockFeatureService.getFeaturesByIds).toHaveBeenCalledWith(
				delivery.features,
			);

			await waitFor(() => {
				expect(result.current.loadedFeatures.get(delivery.id)).toEqual(
					features,
				);
				expect(result.current.loadingFeaturesByDelivery.has(delivery.id)).toBe(
					false,
				);
			});
		});

		it("should not reload features if already loaded", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery({ id: 1, features: [1, 2] });
			const features = [getMockFeature({ id: 1 }), getMockFeature({ id: 2 })];

			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);
			mockGetFeaturesByIds.mockResolvedValue(features);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			// Expand first time
			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});

			await waitFor(() => {
				expect(result.current.loadedFeatures.get(delivery.id)).toEqual(
					features,
				);
			});

			// Collapse and expand again
			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});
			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});

			// Should only be called once
			expect(mockFeatureService.getFeaturesByIds).toHaveBeenCalledTimes(1);
		});

		it("should handle delivery with no features", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery({ id: 1, features: [] });

			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});

			expect(mockFeatureService.getFeaturesByIds).not.toHaveBeenCalled();

			await waitFor(() => {
				expect(result.current.loadedFeatures.get(delivery.id)).toEqual([]);
			});
		});

		it("should handle feature loading error", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery({ id: 1, features: [1, 2] });

			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);
			mockGetFeaturesByIds.mockRejectedValue(new Error("Feature load failed"));

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});

			await waitFor(() => {
				expect(mockShowError).toHaveBeenCalledWith(
					"Failed to load features for delivery",
				);
				expect(result.current.loadingFeaturesByDelivery.has(delivery.id)).toBe(
					false,
				);
			});
		});

		it("should track feature loading state", async () => {
			const portfolio = getMockPortfolio();
			const delivery = getMockDelivery({ id: 1, features: [1, 2] });
			let resolveFeatures: (value: Feature[]) => void;
			const featuresPromise = new Promise<Feature[]>((resolve) => {
				resolveFeatures = resolve;
			});

			mockDeliveryService.getByPortfolio.mockResolvedValue([delivery]);
			mockGetFeaturesByIds.mockReturnValue(featuresPromise);

			const { result } = renderHook(() => useDeliveryManagement({ portfolio }));

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			act(() => {
				result.current.handleToggleExpanded(delivery.id);
			});

			// Should be loading
			expect(result.current.loadingFeaturesByDelivery.has(delivery.id)).toBe(
				true,
			);

			// Resolve the promise
			const features = [getMockFeature({ id: 1 }), getMockFeature({ id: 2 })];
			act(() => {
				resolveFeatures(features);
			});

			await waitFor(() => {
				expect(result.current.loadingFeaturesByDelivery.has(delivery.id)).toBe(
					false,
				);
				expect(result.current.loadedFeatures.get(delivery.id)).toEqual(
					features,
				);
			});
		});
	});
});
