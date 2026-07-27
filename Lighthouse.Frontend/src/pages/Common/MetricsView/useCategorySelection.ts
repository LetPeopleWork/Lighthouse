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

interface VisitedCategoriesState {
	readonly token: string;
	readonly visited: readonly CategoryKey[];
}

function nextVisitedState(
	current: VisitedCategoriesState,
	selectedCategory: CategoryKey,
	resetToken: string,
): VisitedCategoriesState {
	if (current.token !== resetToken) {
		return { token: resetToken, visited: [selectedCategory] };
	}
	if (current.visited.includes(selectedCategory)) {
		return current;
	}
	return {
		token: resetToken,
		visited: [...current.visited, selectedCategory],
	};
}

/** Grows as the user visits categories; resets when the entity or date window changes, which is
 *  the only time a refetch is wanted. Keeps category switching free on re-visit (Bug #5571).
 *  The returned array keeps its identity while nothing changes, so callers can use it as a
 *  useMemo/useEffect dependency without re-triggering on every render. */
export function useVisitedCategories(
	selectedCategory: CategoryKey,
	resetToken: string,
): readonly CategoryKey[] {
	const [state, setState] = useState<VisitedCategoriesState>(() => ({
		token: resetToken,
		visited: [selectedCategory],
	}));

	const next = nextVisitedState(state, selectedCategory, resetToken);
	if (next !== state) {
		setState(next);
	}

	return next.visited;
}
