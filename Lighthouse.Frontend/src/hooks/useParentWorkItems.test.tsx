import { renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../models/Feature";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../tests/MockApiServiceProvider";
import { useParentWorkItems } from "./useParentWorkItems";

const mockFeatureService = createMockFeatureService();
const mockGetFeaturesByReferences = vi.fn();
mockFeatureService.getFeaturesByReferences = mockGetFeaturesByReferences;

const wrapper = ({ children }: { children: React.ReactNode }) => {
	const mockContext = createMockApiServiceContext({
		featureService: mockFeatureService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

describe("useParentWorkItems", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mockGetFeaturesByReferences.mockResolvedValue([]);
	});

	it("should return an empty map when no features are provided", () => {
		const { result } = renderHook(() => useParentWorkItems([]), { wrapper });

		expect(result.current.size).toBe(0);
	});

	it("should return an empty map when features have no parent references", () => {
		const features = [
			(() => {
				const feature = new Feature();
				feature.name = "Feature 1";
				feature.referenceId = "FTR-1";
				feature.parentWorkItemReference = "";
				return feature;
			})(),
		];

		const { result } = renderHook(() => useParentWorkItems(features), {
			wrapper,
		});

		expect(result.current.size).toBe(0);
	});

	it("should fetch and return parent work items", async () => {
		const parentFeature = new Feature();
		parentFeature.name = "Parent Feature";
		parentFeature.referenceId = "PARENT-1";
		parentFeature.url = "http://example.com/parent";

		mockGetFeaturesByReferences.mockResolvedValue([parentFeature]);

		const features = [
			(() => {
				const feature = new Feature();
				feature.name = "Feature 1";
				feature.referenceId = "FTR-1";
				feature.parentWorkItemReference = "PARENT-1";
				return feature;
			})(),
		];

		const { result } = renderHook(() => useParentWorkItems(features), {
			wrapper,
		});

		await waitFor(() => {
			expect(result.current.size).toBe(1);
		});

		const parentInfo = result.current.get("PARENT-1");
		expect(parentInfo).toBeDefined();
		expect(parentInfo?.name).toBe("Parent Feature");
		expect(parentInfo?.referenceId).toBe("PARENT-1");
		expect(parentInfo?.url).toBe("http://example.com/parent");
	});

	it("should handle multiple features with the same parent", async () => {
		const parentFeature = new Feature();
		parentFeature.name = "Parent Feature";
		parentFeature.referenceId = "PARENT-1";
		parentFeature.url = "http://example.com/parent";

		mockGetFeaturesByReferences.mockResolvedValue([parentFeature]);

		const features = [
			(() => {
				const feature = new Feature();
				feature.name = "Feature 1";
				feature.referenceId = "FTR-1";
				feature.parentWorkItemReference = "PARENT-1";
				return feature;
			})(),
			(() => {
				const feature = new Feature();
				feature.name = "Feature 2";
				feature.referenceId = "FTR-2";
				feature.parentWorkItemReference = "PARENT-1";
				return feature;
			})(),
		];

		const { result } = renderHook(() => useParentWorkItems(features), {
			wrapper,
		});

		await waitFor(() => {
			expect(result.current.size).toBe(1);
		});

		expect(mockGetFeaturesByReferences).toHaveBeenCalledWith(["PARENT-1"]);
	});

	it("should handle fetch errors gracefully", async () => {
		mockGetFeaturesByReferences.mockRejectedValue(new Error("Failed to fetch"));

		const features = [
			(() => {
				const feature = new Feature();
				feature.name = "Feature 1";
				feature.referenceId = "FTR-1";
				feature.parentWorkItemReference = "PARENT-1";
				return feature;
			})(),
		];

		const { result } = renderHook(() => useParentWorkItems(features), {
			wrapper,
		});

		await waitFor(() => {
			expect(result.current.size).toBe(0);
		});
	});
});
