import { useCallback, useState } from "react";
import {
	type CategoryKey,
	getCategories,
	getDefaultCategoryKey,
} from "./categoryMetadata";

const validKeys = new Set(getCategories().map((c) => c.key));

function isValidCategoryKey(value: string): value is CategoryKey {
	return validKeys.has(value as CategoryKey);
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
				if (stored && isValidCategoryKey(stored)) {
					return stored;
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
