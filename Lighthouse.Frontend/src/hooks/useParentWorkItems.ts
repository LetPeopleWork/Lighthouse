import { useContext, useEffect, useState } from "react";
import type { IFeature } from "../models/Feature";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

export interface ParentWorkItem {
	referenceId: string;
	name: string;
	url: string;
}

/**
 * Custom hook to fetch parent work item information from references
 * @param features - Array of features that may have parent references
 * @returns Map of reference IDs to parent work item details
 */
export const useParentWorkItems = (
	features: IFeature[],
): Map<string, ParentWorkItem> => {
	const { featureService } = useContext(ApiServiceContext);
	const [parentMap, setParentMap] = useState<Map<string, ParentWorkItem>>(
		new Map(),
	);

	useEffect(() => {
		const fetchParentWorkItems = async () => {
			// Collect all unique parent references
			const parentReferences = new Set<string>();
			for (const feature of features) {
				if (feature.parentWorkItemReference) {
					parentReferences.add(feature.parentWorkItemReference);
				}
			}

			if (parentReferences.size === 0) {
				// Avoid redundant state updates that can cause re-render loops
				setParentMap((prev) => (prev.size === 0 ? prev : new Map()));
				return;
			}

			try {
				// Fetch parent work items by references
				const parentWorkItems = await featureService.getFeaturesByReferences(
					Array.from(parentReferences),
				);

				// Build map of referenceId to parent info
				const newParentMap = new Map<string, ParentWorkItem>();
				for (const parent of parentWorkItems) {
					newParentMap.set(parent.referenceId, {
						referenceId: parent.referenceId,
						name: parent.name,
						url: parent.url ?? "",
					});
				}

				setParentMap(newParentMap);
			} catch (error) {
				console.error("Failed to fetch parent work items:", error);
				setParentMap(new Map());
			}
		};

		fetchParentWorkItems();
	}, [features, featureService]);

	return parentMap;
};
