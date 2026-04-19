import { useCallback, useState } from "react";
import {
	type CategoryKey,
	getCategories,
	getDefaultCategoryKey,
} from "./categoryMetadata";

const validKeys = new Set(getCategories().map((c) => c.key));

const retiredKeyMap: Record<string, CategoryKey> = {
	"cycle-time": "flow-metrics",
	throughput: "flow-metrics",
	"wip-aging": "flow-metrics",
};

function resolveStoredKey(value: string): CategoryKey | null {
	if (validKeys.has(value as CategoryKey)) {
		return value as CategoryKey;
	}
	return retiredKeyMap[value] ?? null;
}

export function useCategorySelection(
	ownerType: "team" | "portfolio",
	ownerId: number,
): {
	selectedCategory: CategoryKey;
	setSelectedCategory: (key: CategoryKey) => void;
} {
	const storageKey = `lighthouse:metrics:${ownerType}:${ownerId}:category`;

	const [selectedCategoryState, setSelectedCategoryState] =
		useState<CategoryKey>(() => {
			try {
				const stored = localStorage.getItem(storageKey);
				if (stored) {
					const resolved = resolveStoredKey(stored);
					if (resolved) {
						return resolved;
					}
				}
			} catch {
				/* ignore storage errors */
			}
			return getDefaultCategoryKey();
		});

	const setSelectedCategory = useCallback(
		(key: CategoryKey) => {
			setSelectedCategoryState(key);
			try {
				localStorage.setItem(storageKey, key);
			} catch {
				/* ignore storage errors */
			}
		},
		[storageKey],
	);

	return { selectedCategory: selectedCategoryState, setSelectedCategory };
}
